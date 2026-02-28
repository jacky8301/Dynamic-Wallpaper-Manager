using Microsoft.Data.Sqlite;
using Serilog;
using System.Data.Common;
using System.IO;
using WallpaperEngine.Models;

namespace WallpaperEngine.Data {
    /// <summary>
    /// 数据库管理器，使用 SQLite 存储和管理壁纸、收藏、合集、分类及扫描历史等数据
    /// </summary>
    public sealed class DatabaseManager : IDisposable {
        private SqliteConnection m_connection;
        private readonly string m_dbPath;

        /// <summary>
        /// 初始化数据库管理器，连接或创建指定路径的 SQLite 数据库
        /// </summary>
        /// <param name="databasePath">数据库文件路径，默认为 wallpapers.db</param>
        public DatabaseManager(string databasePath = "wallpapers.db")
        {
            m_dbPath = databasePath;
            InitializeDatabase();
        }

        /// <summary>
        /// 扫描信息记录，包含扫描路径、时间及统计数据
        /// </summary>
        public class ScanInfo {
            public string ScanPath { get; set; } = string.Empty;
            public DateTime LastScanTime { get; set; } = DateTime.MinValue;
            public int TotalScanned { get; set; }
            public int NewFound { get; set; }
        }

        /// <summary>
        /// 从数据库读取器中解析并构建壁纸项对象，自动处理收藏状态的 JOIN 列判断
        /// </summary>
        /// <param name="reader">数据库数据读取器</param>
        /// <returns>解析后的壁纸项对象</returns>
        private WallpaperItem ReadWallpaperItem(DbDataReader reader)
        {
            // 判断是否有 Favorites 表的 JOIN 列
            bool hasFavoriteJoin = false;
            for (int i = 0; i < reader.FieldCount; i++) {
                if (reader.GetName(i) == "FavFolderPath") {
                    hasFavoriteJoin = true;
                    break;
                }
            }

            bool isFavorite;
            DateTime favoritedDate;
            if (hasFavoriteJoin) {
                isFavorite = reader["FavFolderPath"] != DBNull.Value;
                favoritedDate = reader["FavFavoritedDate"] != DBNull.Value && DateTime.TryParse(reader["FavFavoritedDate"].ToString(), out var parsedDate)
                    ? parsedDate
                    : DateTime.MinValue;
            } else {
                isFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1;
                favoritedDate = reader["FavoritedDate"] != DBNull.Value && DateTime.TryParse(reader["FavoritedDate"].ToString(), out var parsedDate)
                    ? parsedDate
                    : DateTime.MinValue;
            }

            return new WallpaperItem {
                Id = reader["Id"].ToString(),
                FolderPath = reader["FolderPath"].ToString(),
                Project = new WallpaperProject {
                    Title = reader["Title"].ToString(),
                    Description = reader["Description"].ToString(),
                    File = reader["FileName"].ToString(),
                    Preview = reader["PreviewFile"].ToString(),
                    Type = reader["WallpaperType"].ToString(),
                    Tags = reader["Tags"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    ContentRating = reader["ContentRating"].ToString(),
                    Visibility = reader["Visibility"].ToString()
                },
                Category = reader["Category"].ToString(),
                AddedDate = reader["AddedDate"] != DBNull.Value && DateTime.TryParse(reader["AddedDate"].ToString(), out var addedDateParsed)
                    ? addedDateParsed
                    : DateTime.MinValue,
                IsFavorite = isFavorite,
                FavoritedDate = favoritedDate,
                LastUpdated = reader["LastUpdated"]?.ToString(),
                LastScanned = reader["LastScanned"] != DBNull.Value && DateTime.TryParse(reader["LastScanned"].ToString(), out var lastScannedParsed)
                        ? lastScannedParsed
                        : DateTime.MinValue,
                FolderLastModified = reader["FolderLastModified"] != DBNull.Value && DateTime.TryParse(reader["FolderLastModified"].ToString(), out var folderLastModifiedParsed)
                        ? folderLastModifiedParsed
                        : DateTime.MinValue
            };
        }

        /// <summary>
        /// 根据文件夹路径获取已存在的壁纸记录
        /// </summary>
        /// <param name="folderPath">壁纸所在文件夹的完整路径</param>
        /// <returns>匹配的壁纸项，未找到时返回 null</returns>
        public WallpaperItem GetWallpaperByFolderPath(string folderPath)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                SELECT w.*, f.FolderPath AS FavFolderPath, f.FavoritedDate AS FavFavoritedDate
                FROM Wallpapers w
                LEFT JOIN Favorites f ON w.FolderPath = f.FolderPath
                WHERE w.FolderPath = @folderPath";
            command.Parameters.AddWithValue("@folderPath", folderPath);

            using (var reader = command.ExecuteReader()) {
                if (reader.Read()) {
                    return ReadWallpaperItem(reader);
                }
            }
            return null; // 没找到
        }

        /// <summary>
        /// 切换壁纸的收藏状态，收藏时插入记录，取消收藏时删除记录
        /// </summary>
        /// <param name="folderPath">壁纸文件夹路径</param>
        /// <param name="isFavorite">true 表示收藏，false 表示取消收藏</param>
        public void ToggleFavorite(string folderPath, bool isFavorite)
        {
            Log.Information("切换收藏状态: {FolderPath}, 收藏: {IsFavorite}", folderPath, isFavorite);
            using var command = m_connection.CreateCommand();
            if (isFavorite) {
                command.CommandText = @"
                INSERT OR IGNORE INTO Favorites (FolderPath, FavoritedDate)
                VALUES (@folderPath, @favoritedDate)";
                command.Parameters.AddWithValue("@folderPath", folderPath);
                command.Parameters.AddWithValue("@favoritedDate", DateTime.Now.ToString("O"));
            } else {
                command.CommandText = @"
                DELETE FROM Favorites WHERE FolderPath = @folderPath";
                command.Parameters.AddWithValue("@folderPath", folderPath);
            }
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 初始化数据库，创建所有必要的表、索引，并执行数据迁移
        /// </summary>
        private void InitializeDatabase()
        {
            Log.Information("正在初始化数据库: {DbPath}", m_dbPath);
            var connectionString = $"Data Source={m_dbPath}";
            m_connection = new SqliteConnection(connectionString);
            m_connection.Open();

            // 创建壁纸表
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Wallpapers (
                    Id TEXT PRIMARY KEY,
                    FolderPath TEXT UNIQUE,
                    FolderName TEXT,
                    Title TEXT,
                    Description TEXT,
                    FileName TEXT,
                    PreviewFile TEXT,
                    WallpaperType TEXT,
                    Tags TEXT,
                    IsFavorite INTEGER DEFAULT 0,
                    Category TEXT DEFAULT '未分类',
                    AddedDate TEXT,
                    ContentRating TEXT,
                    Visibility TEXT,
                    FavoritedDate TEXT,
                    LastUpdated TEXT,
                    LastScanned TEXT,
                    FolderLastModified TEXT,
                    FileSize INTEGER,
                    FileHash TEXT
                )";
            command.ExecuteNonQuery();

            // 创建索引以提高搜索性能
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Title ON Wallpapers(Title);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Tags ON Wallpapers(Tags);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Category ON Wallpapers(Category);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_IsFavorite ON Wallpapers(IsFavorite);";
            command.ExecuteNonQuery();

            // 创建扫描历史表
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ScanHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ScanPath TEXT NOT NULL,
                LastScanTime TEXT NOT NULL,
                TotalFolders INTEGER DEFAULT 0,
                NewFound INTEGER DEFAULT 0,
                DurationMs INTEGER DEFAULT 0,
                Status TEXT DEFAULT 'Success'
            )";
            command.ExecuteNonQuery();

            // 创建索引
            command.CommandText = @"
            CREATE INDEX IF NOT EXISTS IX_ScanHistory_Path ON ScanHistory(ScanPath);
            CREATE INDEX IF NOT EXISTS IX_ScanHistory_Time ON ScanHistory(LastScanTime DESC);
            CREATE INDEX IF NOT EXISTS IX_Wallpapers_LastModified ON Wallpapers(FolderLastModified DESC)";
            command.ExecuteNonQuery();

            // 创建收藏表
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Favorites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FolderPath TEXT NOT NULL UNIQUE,
                FavoritedDate TEXT NOT NULL
            )";
            command.ExecuteNonQuery();

            // 迁移旧数据：将 Wallpapers 表中 IsFavorite=1 的记录迁移到 Favorites 表
            command.CommandText = @"
            INSERT OR IGNORE INTO Favorites (FolderPath, FavoritedDate)
            SELECT FolderPath, COALESCE(FavoritedDate, datetime('now'))
            FROM Wallpapers WHERE IsFavorite = 1";
            command.ExecuteNonQuery();

            // 创建自定义分类表
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Name TEXT PRIMARY KEY
            )";
            command.ExecuteNonQuery();

            // 创建合集表
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Collections (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                CreatedDate TEXT NOT NULL
            )";
            command.ExecuteNonQuery();

            // 创建合集-壁纸关联表
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS CollectionItems (
                CollectionId TEXT NOT NULL,
                WallpaperFolderPath TEXT NOT NULL,
                AddedDate TEXT NOT NULL,
                PRIMARY KEY (CollectionId, WallpaperFolderPath)
            )";
            command.ExecuteNonQuery();
            Log.Information("数据库初始化完成");
        }
        /// <summary>
        /// 保存壁纸记录到数据库，若已存在则更新（INSERT OR REPLACE）
        /// </summary>
        /// <param name="wallpaper">要保存的壁纸项</param>
        public void SaveWallpaper(WallpaperItem wallpaper)
        {
            Log.Debug("保存壁纸记录: {FolderPath}", wallpaper.FolderPath);
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Wallpapers 
                (Id, FolderPath, FolderName, Title, Description, FileName, PreviewFile, 
                 WallpaperType, Tags, IsFavorite, Category, AddedDate, ContentRating, Visibility, FavoritedDate, LastUpdated, LastScanned, FolderLastModified, FileSize, FileHash)
                VALUES 
                ($id, $folderPath, $folderName, $title, $description, $fileName, $previewFile,
                 $wallpaperType, $tags, $isFavorite, $category, $addedDate, $contentRating, $visibility, $favoritedDate, $lastUpdated, $lastScanned, $folderLastModified, $fileSize, $fileHash)";

            // 计算文件哈希
            var fileHash = string.Empty;
            var lastModified = DateTime.MinValue;
            var fileSize = 0L;

            try {
                var mainFile = Path.Combine(wallpaper.FolderPath, wallpaper.Project.File);
                if (File.Exists(mainFile)) {
                    var fileInfo = new FileInfo(mainFile);
                    lastModified = fileInfo.LastWriteTime;
                    fileSize = fileInfo.Length;
                }
            } catch (Exception ex) {
                Log.Warning($"获取文件信息失败 {wallpaper.FolderPath}: {ex.Message}");
                lastModified = DateTime.Now;
            }

            command.Parameters.AddWithValue("$id", wallpaper.Id);
            command.Parameters.AddWithValue("$folderPath", wallpaper.FolderPath);
            command.Parameters.AddWithValue("$folderName", wallpaper.FolderName);
            command.Parameters.AddWithValue("$title", wallpaper.Project.Title);
            command.Parameters.AddWithValue("$description", wallpaper.Project.Description);
            command.Parameters.AddWithValue("$fileName", wallpaper.Project.File);
            command.Parameters.AddWithValue("$previewFile", wallpaper.Project.Preview);
            command.Parameters.AddWithValue("$wallpaperType", wallpaper.Project.Type);
            command.Parameters.AddWithValue("$tags", string.Join(",", wallpaper.Project.Tags ?? new List<string>()));
            command.Parameters.AddWithValue("$isFavorite", wallpaper.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("$category", wallpaper.Category);
            command.Parameters.AddWithValue("$addedDate", wallpaper.AddedDate.ToString("O"));
            command.Parameters.AddWithValue("$contentRating", wallpaper.Project.ContentRating);
            command.Parameters.AddWithValue("$visibility", wallpaper.Project.Visibility);
            command.Parameters.AddWithValue("$favoritedDate", wallpaper.FavoritedDate);
            command.Parameters.AddWithValue("$lastUpdated", wallpaper.LastUpdated);
            // 快速扫描新增字段
            command.Parameters.AddWithValue("$lastScanned", wallpaper.LastScanned.ToString("O"));
            command.Parameters.AddWithValue("$folderLastModified", wallpaper.FolderLastModified.ToString("O"));
            command.Parameters.AddWithValue("$fileSize", fileSize);
            command.Parameters.AddWithValue("$fileHash", fileHash);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 根据关键词、分类和收藏状态搜索壁纸，支持标题、描述和标签的模糊匹配
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        /// <param name="category">分类筛选，为空则不限分类</param>
        /// <param name="favoritesOnly">是否仅返回已收藏的壁纸</param>
        /// <returns>匹配的壁纸列表，最多返回 1000 条</returns>
        public List<WallpaperItem> SearchWallpapers(string searchTerm, string category = "", bool favoritesOnly = false)
        {
            Log.Debug("搜索壁纸, 关键词: {SearchTerm}, 分类: {Category}, 仅收藏: {FavoritesOnly}", searchTerm, category, favoritesOnly);
            var wallpapers = new List<WallpaperItem>();
            using var command = m_connection.CreateCommand();

            string whereClause = "WHERE 1=1";
            if (!string.IsNullOrEmpty(searchTerm)) {
                whereClause += " AND (w.Title LIKE @search OR w.Description LIKE @search OR w.Tags LIKE @search)";
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
            }
            if (!string.IsNullOrEmpty(category)) {
                whereClause += " AND w.Category = @category";
                command.Parameters.AddWithValue("@category", category);
            }
            if (favoritesOnly) {
                whereClause += " AND f.FolderPath IS NOT NULL";
            }

            command.CommandText = $@"
                SELECT w.*, f.FolderPath AS FavFolderPath, f.FavoritedDate AS FavFavoritedDate
                FROM Wallpapers w
                LEFT JOIN Favorites f ON w.FolderPath = f.FolderPath
                {whereClause}
                ORDER BY (CASE WHEN f.FolderPath IS NOT NULL THEN 1 ELSE 0 END) DESC, w.AddedDate DESC
                LIMIT 1000";

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                wallpapers.Add(ReadWallpaperItem(reader));
            }

            return wallpapers;
        }

        /// <summary>
        /// 根据壁纸 ID 删除壁纸记录及其关联的收藏记录
        /// </summary>
        /// <param name="wallpaperId">壁纸 ID</param>
        public void DeleteWallpaper(string wallpaperId)
        {
            Log.Information("删除壁纸记录: {Id}", wallpaperId);
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Favorites WHERE FolderPath IN (SELECT FolderPath FROM Wallpapers WHERE Id = $id)";
            command.Parameters.AddWithValue("$id", wallpaperId);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM Wallpapers WHERE Id = $id";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 根据文件夹路径删除壁纸记录及其关联的收藏记录
        /// </summary>
        /// <param name="folderPath">壁纸文件夹路径</param>
        public void DeleteWallpaperByPath(string folderPath)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Favorites WHERE FolderPath = @path";
            command.Parameters.AddWithValue("@path", folderPath);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM Wallpapers WHERE FolderPath = @path";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 释放数据库连接资源
        /// </summary>
        public void Dispose()
        {
            Log.Debug("关闭数据库连接");
            m_connection?.Close();
            m_connection?.Dispose();
        }

        // ===== 合集管理 =====

        /// <summary>
        /// 获取所有壁纸合集，按创建时间倒序排列
        /// </summary>
        /// <returns>合集列表</returns>
        public List<WallpaperCollection> GetAllCollections()
        {
            var collections = new List<WallpaperCollection>();
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, CreatedDate FROM Collections ORDER BY CreatedDate DESC";
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                collections.Add(new WallpaperCollection {
                    Id = reader["Id"].ToString(),
                    Name = reader["Name"].ToString(),
                    CreatedDate = reader["CreatedDate"] != DBNull.Value && DateTime.TryParse(reader["CreatedDate"].ToString(), out var createdDateParsed)
                        ? createdDateParsed
                        : DateTime.MinValue
                });
            }
            return collections;
        }

        /// <summary>
        /// 创建新的壁纸合集
        /// </summary>
        /// <param name="name">合集名称</param>
        /// <returns>创建的合集对象</returns>
        public WallpaperCollection AddCollection(string name)
        {
            Log.Information("创建合集: {Name}", name);
            var collection = new WallpaperCollection {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                CreatedDate = DateTime.Now
            };
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Collections (Id, Name, CreatedDate)
                VALUES (@id, @name, @createdDate)";
            command.Parameters.AddWithValue("@id", collection.Id);
            command.Parameters.AddWithValue("@name", collection.Name);
            command.Parameters.AddWithValue("@createdDate", collection.CreatedDate.ToString("O"));
            command.ExecuteNonQuery();
            return collection;
        }

        /// <summary>
        /// 重命名指定合集
        /// </summary>
        /// <param name="id">合集 ID</param>
        /// <param name="newName">新名称</param>
        public void RenameCollection(string id, string newName)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "UPDATE Collections SET Name = @name WHERE Id = @id";
            command.Parameters.AddWithValue("@name", newName);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除指定合集及其所有关联项
        /// </summary>
        /// <param name="id">合集 ID</param>
        public void DeleteCollection(string id)
        {
            Log.Information("删除合集: {Id}", id);
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM CollectionItems WHERE CollectionId = @id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM Collections WHERE Id = @id";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取指定合集中的所有壁纸文件夹路径，按添加时间倒序排列
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <returns>壁纸文件夹路径列表</returns>
        public List<string> GetCollectionItems(string collectionId)
        {
            var items = new List<string>();
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT WallpaperFolderPath FROM CollectionItems WHERE CollectionId = @id ORDER BY AddedDate DESC";
            command.Parameters.AddWithValue("@id", collectionId);
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                items.Add(reader.GetString(0));
            }
            return items;
        }

        /// <summary>
        /// 检查壁纸是否已在指定合集中
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="folderPath">壁纸文件夹路径</param>
        /// <returns>已存在返回 true，否则返回 false</returns>
        public bool IsInCollection(string collectionId, string folderPath)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM CollectionItems WHERE CollectionId = @collectionId AND WallpaperFolderPath = @folderPath";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@folderPath", folderPath);
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// 将壁纸添加到指定合集，若已存在则忽略
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="folderPath">壁纸文件夹路径</param>
        public void AddToCollection(string collectionId, string folderPath)
        {
            Log.Debug("添加壁纸到合集: {CollectionId}, {FolderPath}", collectionId, folderPath);
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO CollectionItems (CollectionId, WallpaperFolderPath, AddedDate)
                VALUES (@collectionId, @folderPath, @addedDate)";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@folderPath", folderPath);
            command.Parameters.AddWithValue("@addedDate", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 从指定合集中移除壁纸
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="folderPath">壁纸文件夹路径</param>
        public void RemoveFromCollection(string collectionId, string folderPath)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM CollectionItems WHERE CollectionId = @collectionId AND WallpaperFolderPath = @folderPath";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@folderPath", folderPath);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 保存一次扫描操作的记录到扫描历史表
        /// </summary>
        /// <param name="scanPath">扫描路径</param>
        /// <param name="newFound">新发现的壁纸数量</param>
        /// <param name="updated">已更新的壁纸数量</param>
        /// <param name="skipped">跳过的壁纸数量</param>
        public void SaveScanRecord(string scanPath, int newFound, int updated, int skipped)
        {
            Log.Information("保存扫描记录: {ScanPath}", scanPath);
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO ScanHistory
            (ScanPath, LastScanTime, TotalFolders, NewFound, DurationMs, Status)
            VALUES
            (@scanPath, @lastScanTime, @newFound, @updated, @skipped, @status)";

            command.Parameters.AddWithValue("@scanPath", scanPath);
            command.Parameters.AddWithValue("@lastScanTime", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@newFound", newFound);
            command.Parameters.AddWithValue("@updated", updated);
            command.Parameters.AddWithValue("@skipped", skipped);
            command.Parameters.AddWithValue("@status", "Success");

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 检查壁纸是否需要更新，通过比较文件修改时间和大小判断
        /// </summary>
        /// <param name="folderPath">壁纸文件夹路径</param>
        /// <param name="lastModified">文件最后修改时间</param>
        /// <param name="fileSize">文件大小</param>
        /// <returns>需要更新返回 true，否则返回 false</returns>
        public bool NeedsUpdate(string folderPath, DateTime lastModified, long fileSize)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
            SELECT FolderLastModified, FileSize FROM Wallpapers 
            WHERE FolderPath = @folderPath";
            command.Parameters.AddWithValue("@folderPath", folderPath);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return true; // 不存在，需要添加

            if (!DateTime.TryParse(reader["FolderLastModified"].ToString(), out var dbLastModified))
                return true;
            if (!long.TryParse(reader["FileSize"].ToString(), out var dbFileSize))
                return true;

            // 如果最后修改时间或文件大小变化，则需要更新
            return dbLastModified != lastModified || dbFileSize != fileSize;
        }

        /// <summary>
        /// 获取扫描历史记录，可按扫描路径筛选
        /// </summary>
        /// <param name="scanPath">扫描路径筛选条件，为 null 时返回所有记录</param>
        /// <returns>扫描历史列表</returns>
        public List<ScanInfo> GetScanHistory(string? scanPath = null)
        {
            var history = new List<ScanInfo>();
            using var command = m_connection.CreateCommand();

            if (string.IsNullOrEmpty(scanPath)) {
                command.CommandText = @"
                SELECT ScanPath, LastScanTime, TotalFolders, NewFound 
                FROM ScanHistory 
                ORDER BY LastScanTime DESC 
                LIMIT 50";
            } else {
                command.CommandText = @"
                SELECT ScanPath, LastScanTime, TotalFolders, NewFound 
                FROM ScanHistory 
                WHERE ScanPath = @scanPath 
                ORDER BY LastScanTime DESC 
                LIMIT 20";
                command.Parameters.AddWithValue("@scanPath", scanPath);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                history.Add(new ScanInfo {
                    ScanPath = reader["ScanPath"].ToString(),
                    LastScanTime = reader["LastScanTime"] != DBNull.Value && DateTime.TryParse(reader["LastScanTime"].ToString(), out var lastScanTimeParsed)
                        ? lastScanTimeParsed
                        : DateTime.MinValue,
                    TotalScanned = Convert.ToInt32(reader["TotalFolders"]),
                    NewFound = Convert.ToInt32(reader["NewFound"])
                });
            }

            return history;
        }

        /// <summary>
        /// 清空所有扫描历史记录
        /// </summary>
        public void ClearScanHistory()
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM ScanHistory";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 更新已有壁纸的详细信息（标题、描述、标签、分类等）
        /// </summary>
        /// <param name="wallpaper">包含更新数据的壁纸项</param>
        public void UpdateWallpaper(WallpaperItem wallpaper)
        {
            Log.Debug("更新壁纸记录: {Id}", wallpaper.Id);
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
            UPDATE Wallpapers 
            SET Title = @title,
                Description = @description,
                FileName = @fileName,
                PreviewFile = @previewFile,
                WallpaperType = @type,
                Tags = @tags,
                ContentRating = @contentRating,
                Visibility = @visibility,
                Category = @category,
                LastUpdated = @lastUpdated
            WHERE Id = @id";

            command.Parameters.AddWithValue("@id", wallpaper.Id);
            command.Parameters.AddWithValue("@title", wallpaper.Project.Title);
            command.Parameters.AddWithValue("@description", wallpaper.Project.Description);
            command.Parameters.AddWithValue("@fileName", wallpaper.Project.File);
            command.Parameters.AddWithValue("@previewFile", wallpaper.Project.Preview);
            command.Parameters.AddWithValue("@type", wallpaper.Project.Type);
            command.Parameters.AddWithValue("@tags", string.Join(",", wallpaper.Project.Tags ?? new List<string>()));
            command.Parameters.AddWithValue("@contentRating", wallpaper.Project.ContentRating);
            command.Parameters.AddWithValue("@visibility", wallpaper.Project.Visibility);
            command.Parameters.AddWithValue("@category", wallpaper.Category ?? "未分类");
            command.Parameters.AddWithValue("@lastUpdated", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取壁纸的文件统计信息（文件数量和总大小）
        /// </summary>
        /// <param name="wallpaperId">壁纸 ID</param>
        /// <returns>文件数量和总大小的元组</returns>
        public (int FileCount, long TotalSize) GetWallpaperFileStats(string wallpaperId)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT FileCount, TotalSize FROM WallpaperStats WHERE WallpaperId = @id";
            command.Parameters.AddWithValue("@id", wallpaperId);

            using var reader = command.ExecuteReader();
            if (reader.Read()) {
                return (reader.GetInt32(0), reader.GetInt64(1));
            }
            return (0, 0);
        }

        /// <summary>
        /// 获取所有自定义分类名称，按名称排序
        /// </summary>
        /// <returns>分类名称列表</returns>
        public List<string> GetCustomCategories()
        {
            var categories = new List<string>();
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT Name FROM Categories ORDER BY Name";
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                categories.Add(reader.GetString(0));
            }
            return categories;
        }

        /// <summary>
        /// 添加自定义分类，若已存在则忽略
        /// </summary>
        /// <param name="name">分类名称</param>
        public void AddCategory(string name)
        {
            Log.Information("添加分类: {Name}", name);
            using var command = m_connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES (@name)";
            command.Parameters.AddWithValue("@name", name);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除自定义分类，并将该分类下的所有壁纸重置为"未分类"
        /// </summary>
        /// <param name="name">要删除的分类名称</param>
        public void DeleteCategory(string name)
        {
            Log.Information("删除分类: {Name}", name);
            using var transaction = m_connection.BeginTransaction();
            try {
                using var updateCmd = m_connection.CreateCommand();
                updateCmd.CommandText = "UPDATE Wallpapers SET Category = '未分类' WHERE Category = @name";
                updateCmd.Parameters.AddWithValue("@name", name);
                updateCmd.ExecuteNonQuery();

                using var deleteCmd = m_connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM Categories WHERE Name = @name";
                deleteCmd.Parameters.AddWithValue("@name", name);
                deleteCmd.ExecuteNonQuery();

                transaction.Commit();
            } catch {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 重命名分类，同时更新所有使用该分类的壁纸记录
        /// </summary>
        /// <param name="oldName">原分类名称</param>
        /// <param name="newName">新分类名称</param>
        public void RenameCategory(string oldName, string newName)
        {
            Log.Information("重命名分类: {OldName} -> {NewName}", oldName, newName);
            using var transaction = m_connection.BeginTransaction();
            try {
                using var updateCmd = m_connection.CreateCommand();
                updateCmd.CommandText = "UPDATE Wallpapers SET Category = @newName WHERE Category = @oldName";
                updateCmd.Parameters.AddWithValue("@oldName", oldName);
                updateCmd.Parameters.AddWithValue("@newName", newName);
                updateCmd.ExecuteNonQuery();

                using var deleteCmd = m_connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM Categories WHERE Name = @oldName";
                deleteCmd.Parameters.AddWithValue("@oldName", oldName);
                deleteCmd.ExecuteNonQuery();

                using var insertCmd = m_connection.CreateCommand();
                insertCmd.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES (@newName)";
                insertCmd.Parameters.AddWithValue("@newName", newName);
                insertCmd.ExecuteNonQuery();

                transaction.Commit();
            } catch {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// 获取数据库中壁纸的总数量（不考虑筛选条件）
        /// </summary>
        /// <returns>壁纸总数</returns>
        public int GetTotalWallpaperCount()
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Wallpapers";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}