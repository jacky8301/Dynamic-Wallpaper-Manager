using Microsoft.Data.Sqlite;
using Serilog;
using System.Data.Common;
using System.IO;
using WallpaperEngine.Models;

namespace WallpaperEngine.Data {
    public sealed class DatabaseManager : IDisposable {
        private SqliteConnection m_connection;
        private readonly string m_dbPath;
        public DatabaseManager(string databasePath = "wallpapers.db")
        {
            m_dbPath = databasePath;
            InitializeDatabase();
        }

        public class ScanInfo {
            public string ScanPath { get; set; } = string.Empty;
            public DateTime LastScanTime { get; set; } = DateTime.MinValue;
            public int TotalScanned { get; set; }
            public int NewFound { get; set; }
        }

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
                favoritedDate = reader["FavFavoritedDate"] != DBNull.Value
                    ? DateTime.Parse(reader["FavFavoritedDate"].ToString())
                    : DateTime.MinValue;
            } else {
                isFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1;
                favoritedDate = string.IsNullOrEmpty(reader["FavoritedDate"]?.ToString())
                    ? DateTime.MinValue
                    : DateTime.Parse(reader["FavoritedDate"].ToString());
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
                AddedDate = DateTime.Parse(reader["AddedDate"].ToString()),
                IsFavorite = isFavorite,
                FavoritedDate = favoritedDate,
                LastUpdated = reader["LastUpdated"]?.ToString(),
                LastScanned = string.IsNullOrEmpty(reader["LastScanned"]?.ToString())
                        ? DateTime.MinValue
                        : DateTime.Parse(reader["LastScanned"].ToString()),
                FolderLastModified = string.IsNullOrEmpty(reader["FolderLastModified"]?.ToString())
                        ? DateTime.MinValue
                        : DateTime.Parse(reader["FolderLastModified"].ToString())
            };
        }

        // 根据文件夹路径获取已存在的壁纸记录
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

        // 专门用于切换收藏状态的方法
        public void ToggleFavorite(string folderPath, bool isFavorite)
        {
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

        private void InitializeDatabase()
        {
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
        }
        // 插入或更新壁纸记录
        public void SaveWallpaper(WallpaperItem wallpaper)
        {
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

        public List<WallpaperItem> SearchWallpapers(string searchTerm, string category = "", bool favoritesOnly = false)
        {
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

        public void DeleteWallpaper(string wallpaperId)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Favorites WHERE FolderPath IN (SELECT FolderPath FROM Wallpapers WHERE Id = $id)";
            command.Parameters.AddWithValue("$id", wallpaperId);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM Wallpapers WHERE Id = $id";
            command.ExecuteNonQuery();
        }

        public void DeleteWallpaperByPath(string folderPath)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Favorites WHERE FolderPath = @path";
            command.Parameters.AddWithValue("@path", folderPath);
            command.ExecuteNonQuery();

            command.CommandText = "DELETE FROM Wallpapers WHERE FolderPath = @path";
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            m_connection?.Close();
            m_connection?.Dispose();
        }

        // 保存扫描记录
        public void SaveScanRecord(string scanPath, int newFound, int updated, int skipped)
        {
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

        // 检查壁纸是否需要更新
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

            var dbLastModified = DateTime.Parse(reader["FolderLastModified"].ToString());
            var dbFileSize = Convert.ToInt64(reader["FileSize"]);

            // 如果最后修改时间或文件大小变化，则需要更新
            return dbLastModified != lastModified || dbFileSize != fileSize;
        }

        // 获取扫描历史
        public List<ScanInfo> GetScanHistory(string scanPath = null)
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
                    LastScanTime = DateTime.Parse(reader["LastScanTime"].ToString()),
                    TotalScanned = Convert.ToInt32(reader["TotalFolders"]),
                    NewFound = Convert.ToInt32(reader["NewFound"])
                });
            }

            return history;
        }

        public void ClearScanHistory()
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM ScanHistory";
            command.ExecuteNonQuery();
        }

        // 更新壁纸信息
        public void UpdateWallpaper(WallpaperItem wallpaper)
        {
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

        // 获取壁纸文件统计信息
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

        // 获取所有自定义分类
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

        // 添加自定义分类
        public void AddCategory(string name)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Categories (Name) VALUES (@name)";
            command.Parameters.AddWithValue("@name", name);
            command.ExecuteNonQuery();
        }
    }
}