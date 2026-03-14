using Microsoft.Data.Sqlite;
using Serilog;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using WallpaperEngine.Models;

namespace WallpaperEngine.Data {
    /// <summary>
    /// 数据库管理器，使用 SQLite 存储和管理壁纸、收藏、合集、分类及扫描历史等数据
    /// </summary>
    public sealed class DatabaseManager : IDisposable {
        private SqliteConnection m_connection;
        private readonly string m_dbPath;
        private readonly object m_lock = new object();

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
        /// 确保数据库连接处于打开状态，如果连接已关闭则重新打开
        /// </summary>
        private void EnsureConnectionOpen()
        {
            if (m_connection == null || m_connection.State != System.Data.ConnectionState.Open) {
                Log.Warning("数据库连接已关闭，正在重新打开");
                m_connection?.Dispose();
                var connectionString = $"Data Source={m_dbPath}";
                m_connection = new SqliteConnection(connectionString);
                m_connection.Open();
            }
        }

        /// <summary>
        /// 在锁内执行数据库操作，确保连接处于打开状态
        /// </summary>
        /// <param name="action">要执行的数据库操作，接收当前连接作为参数</param>
        private void ExecuteInLock(Action<SqliteConnection> action)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                action(m_connection);
            }
        }

        /// <summary>
        /// 在锁内执行数据库查询操作，确保连接处于打开状态
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="func">要执行的数据库查询，接收当前连接作为参数并返回结果</param>
        /// <returns>查询结果</returns>
        private T ExecuteInLock<T>(Func<SqliteConnection, T> func)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                return func(m_connection);
            }
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

            var categoryId = reader["CategoryId"] != DBNull.Value ? Convert.ToInt32(reader["CategoryId"]) : CategoryConstants.UNCATEGORIZED_ID;
            var categoryName = GetCategoryNameById(categoryId) ?? "未分类";

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
                    ContentRating = reader["ContentRating"] != DBNull.Value ? reader["ContentRating"].ToString() : "Everyone",
                    Visibility = reader["Visibility"] != DBNull.Value ? reader["Visibility"].ToString() : "public"
                },
                CategoryId = categoryId,
                Category = categoryName,
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
            lock (m_lock) {
                EnsureConnectionOpen();
                using var command = m_connection.CreateCommand();
                command.CommandText = @"
                SELECT w.*, f.WallpaperId AS FavFolderPath, f.FavoritedDate AS FavFavoritedDate
                FROM Wallpapers w
                LEFT JOIN Favorites f ON w.Id = f.WallpaperId
                WHERE w.FolderPath = @folderPath";
                command.Parameters.AddWithValue("@folderPath", folderPath);

                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        return ReadWallpaperItem(reader);
                    }
                }
                return null; // 没找到
            }
        }

        /// <summary>
        /// 根据壁纸ID获取壁纸项
        /// </summary>
        /// <param name="wallpaperId">壁纸ID</param>
        /// <returns>壁纸项，如果未找到则返回null</returns>
        public WallpaperItem GetWallpaperById(string wallpaperId)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                using var command = m_connection.CreateCommand();
                command.CommandText = @"
                SELECT w.*, f.WallpaperId AS FavFolderPath, f.FavoritedDate AS FavFavoritedDate
                FROM Wallpapers w
                LEFT JOIN Favorites f ON w.Id = f.WallpaperId
                WHERE w.Id = @wallpaperId";
                command.Parameters.AddWithValue("@wallpaperId", wallpaperId);

                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        return ReadWallpaperItem(reader);
                    }
                }
                return null; // 没找到
            }
        }

        /// <summary>
        /// 切换壁纸的收藏状态，收藏时插入记录，取消收藏时删除记录
        /// </summary>
        /// <param name="wallpaperId">壁纸ID</param>
        /// <param name="isFavorite">true 表示收藏，false 表示取消收藏</param>
        public void ToggleFavorite(string wallpaperId, bool isFavorite)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                Log.Information("切换收藏状态: {WallpaperId}, 收藏: {IsFavorite}", wallpaperId, isFavorite);
                using var command = m_connection.CreateCommand();
                if (isFavorite) {
                    // 检查壁纸是否存在
                    command.CommandText = "SELECT 1 FROM Wallpapers WHERE Id = @wallpaperId";
                    command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
                    var exists = command.ExecuteScalar();
                    if (exists == null) {
                        Log.Warning("未找到壁纸ID对应的记录: {WallpaperId}", wallpaperId);
                        return;
                    }
                    command.Parameters.Clear();
                    command.CommandText = @"
                INSERT OR IGNORE INTO Favorites (WallpaperId, FavoritedDate)
                VALUES (@wallpaperId, @favoritedDate)";
                    command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
                    command.Parameters.AddWithValue("@favoritedDate", DateTime.Now.ToString("O"));
                } else {
                    command.CommandText = @"
                DELETE FROM Favorites WHERE WallpaperId = @wallpaperId";
                    command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
                }
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 迁移分类到ID系统
        /// </summary>
        private void MigrateCategoriesToIdSystem()
        {
            Log.Information("开始迁移分类到ID系统");

            using var transaction = m_connection.BeginTransaction();
            try {
                // 1. 检查是否需要迁移Categories表（检查是否有Id列）
                bool needsCategoryMigration = false;
                using (var checkCmd = m_connection.CreateCommand()) {
                    checkCmd.CommandText = "PRAGMA table_info(Categories)";
                    using var reader = checkCmd.ExecuteReader();
                    while (reader.Read()) {
                        if (reader["name"].ToString() == "Id") {
                            needsCategoryMigration = false;
                            break;
                        }
                    }
                    // 如果表不存在或没有Id列，需要迁移
                    needsCategoryMigration = true;
                }

                if (needsCategoryMigration) {
                    // 备份旧Categories表（如果存在）
                    using var backupCmd = m_connection.CreateCommand();
                    backupCmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Categories_Backup AS SELECT * FROM Categories;
                        DROP TABLE IF EXISTS Categories;
                    ";
                    backupCmd.ExecuteNonQuery();

                    // 创建新Categories表
                    backupCmd.CommandText = @"
                        CREATE TABLE Categories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT UNIQUE NOT NULL,
                            IsDefault BOOLEAN DEFAULT 0
                        )";
                    backupCmd.ExecuteNonQuery();

                    // 插入硬编码默认分类（当前列表为空，因为已移除硬编码默认分类）
                    // 注意：ID 1 是虚拟分类"未分类"，但虚拟分类不存储在数据库中
                    // 实际的自定义分类ID从1开始自动递增
                    int defaultCategoryId = 2; // 为硬编码默认分类预留的起始ID（如果将来需要）
                    foreach (var defaultCategory in CategoryConstants.DefaultCategories) {
                        using var insertCmd = m_connection.CreateCommand();
                        insertCmd.CommandText = @"
                            INSERT OR IGNORE INTO Categories (Id, Name, IsDefault)
                            VALUES (@id, @name, 1)";
                        insertCmd.Parameters.AddWithValue("@id", defaultCategoryId);
                        insertCmd.Parameters.AddWithValue("@name", defaultCategory.Name);
                        insertCmd.ExecuteNonQuery();
                        defaultCategoryId++;
                    }

                    // 从备份恢复自定义分类
                    backupCmd.CommandText = @"
                        INSERT OR IGNORE INTO Categories (Name, IsDefault)
                        SELECT Name, 0 FROM Categories_Backup
                        WHERE Name NOT IN (SELECT Name FROM Categories)
                    ";
                    backupCmd.ExecuteNonQuery();

                    // 删除备份表
                    backupCmd.CommandText = "DROP TABLE IF EXISTS Categories_Backup";
                    backupCmd.ExecuteNonQuery();

                    Log.Information("Categories表迁移完成");
                }

                // 2. 迁移Wallpapers表
                // 检查是否有Category列（旧列）
                bool hasOldCategoryColumn = false;
                using (var checkCmd = m_connection.CreateCommand()) {
                    checkCmd.CommandText = "PRAGMA table_info(Wallpapers)";
                    using var reader = checkCmd.ExecuteReader();
                    while (reader.Read()) {
                        if (reader["name"].ToString() == "Category") {
                            hasOldCategoryColumn = true;
                        }
                    }
                }

                if (hasOldCategoryColumn) {
                    // 添加CategoryId列（如果不存在）
                    using var alterCmd = m_connection.CreateCommand();
                    alterCmd.CommandText = @"
                        ALTER TABLE Wallpapers ADD COLUMN CategoryId INTEGER DEFAULT 1;
                        CREATE INDEX IF NOT EXISTS IX_Wallpapers_CategoryId ON Wallpapers(CategoryId);
                    ";
                    alterCmd.ExecuteNonQuery();

                    // 迁移Category到CategoryId
                    // 首先更新虚拟分类
                    alterCmd.CommandText = @"
                        UPDATE Wallpapers SET CategoryId = 0 WHERE Category = '所有分类';
                        UPDATE Wallpapers SET CategoryId = 1 WHERE Category = '未分类';
                    ";
                    alterCmd.ExecuteNonQuery();

                    // 更新默认分类
                    foreach (var defaultCategory in CategoryConstants.DefaultCategories) {
                        alterCmd.CommandText = @"
                            UPDATE Wallpapers
                            SET CategoryId = (SELECT Id FROM Categories WHERE Name = @name)
                            WHERE Category = @name AND CategoryId = 1";
                        alterCmd.Parameters.Clear();
                        alterCmd.Parameters.AddWithValue("@name", defaultCategory.Name);
                        alterCmd.ExecuteNonQuery();
                    }

                    // 更新自定义分类
                    alterCmd.CommandText = @"
                        UPDATE Wallpapers
                        SET CategoryId = (SELECT Id FROM Categories WHERE Name = Wallpapers.Category)
                        WHERE CategoryId = 1 AND Category NOT IN ('所有分类', '未分类')
                          AND Category IN (SELECT Name FROM Categories)
                    ";
                    alterCmd.ExecuteNonQuery();

                    Log.Information("Wallpapers表Category列迁移完成");
                }

                transaction.Commit();
                Log.Information("分类ID系统迁移完成");
            } catch (Exception ex) {
                transaction.Rollback();
                Log.Error(ex, "分类ID系统迁移失败");
                throw;
            }
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
                    CategoryId INTEGER DEFAULT 1,
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
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_CategoryId ON Wallpapers(CategoryId);
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
            bool wallpaperIdColumnExists = false;
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Favorites (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                WallpaperId TEXT NOT NULL UNIQUE,
                FavoritedDate TEXT NOT NULL
            )";
            command.ExecuteNonQuery();

            // 确保WallpaperId列存在（兼容旧版本数据库）
            try {
                // 首先检查WallpaperId列是否已存在
                command.CommandText = "PRAGMA table_info(Favorites)";
                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var columnName = reader.GetString(1); // name column
                        if (columnName.Equals("WallpaperId", StringComparison.OrdinalIgnoreCase)) {
                            wallpaperIdColumnExists = true;
                            break;
                        }
                    }
                }

                if (!wallpaperIdColumnExists) {
                    Log.Information("Favorites表缺少WallpaperId列，正在添加...");
                    command.CommandText = "ALTER TABLE Favorites ADD COLUMN WallpaperId TEXT";
                    command.ExecuteNonQuery();
                    Log.Information("已成功添加WallpaperId列到Favorites表");
                    wallpaperIdColumnExists = true;
                } else {
                    Log.Debug("WallpaperId列已存在");
                }
            } catch (Exception ex) {
                Log.Warning("检查或添加WallpaperId列时出错: {Error}", ex.Message);
                // 尝试重建Favorites表
                try {
                    Log.Information("尝试重建Favorites表...");

                    // 备份现有收藏数据（从Wallpapers表获取，因为Favorites表可能结构不兼容）
                    var backupData = new List<(string FolderPath, string WallpaperId)>();
                    command.CommandText = "SELECT FolderPath, Id FROM Wallpapers WHERE IsFavorite = 1";
                    using (var reader = command.ExecuteReader()) {
                        while (reader.Read()) {
                            backupData.Add((reader.GetString(0), reader.GetString(1)));
                        }
                    }

                    Log.Information("找到 {Count} 个收藏记录需要迁移", backupData.Count);

                    // 删除旧的Favorites表
                    command.CommandText = "DROP TABLE IF EXISTS Favorites";
                    command.ExecuteNonQuery();

                    // 重新创建Favorites表（不包含FolderPath列）
                    command.CommandText = @"
                    CREATE TABLE Favorites (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        WallpaperId TEXT NOT NULL UNIQUE,
                        FavoritedDate TEXT NOT NULL
                    )";
                    command.ExecuteNonQuery();

                    // 恢复数据
                    foreach (var (folderPath, wallpaperId) in backupData) {
                        command.CommandText = @"
                        INSERT OR IGNORE INTO Favorites (WallpaperId, FavoritedDate)
                        VALUES (@wallpaperId, datetime('now'))";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
                        command.ExecuteNonQuery();
                    }

                    Log.Information("Favorites表重建完成，包含WallpaperId列");
                    wallpaperIdColumnExists = true;
                } catch (Exception rebuildEx) {
                    Log.Error(rebuildEx, "重建Favorites表失败");
                    // 继续执行，后续操作可能会失败
                }
            }

            // 迁移旧数据：将 Wallpapers 表中 IsFavorite=1 的记录迁移到 Favorites 表
            if (wallpaperIdColumnExists) {
                command.CommandText = @"
                INSERT OR IGNORE INTO Favorites (WallpaperId, FavoritedDate)
                SELECT Id, COALESCE(FavoritedDate, datetime('now'))
                FROM Wallpapers WHERE IsFavorite = 1";
                command.ExecuteNonQuery();
                Log.Information("已迁移旧收藏数据到Favorites表");
            } else {
                Log.Warning("无法迁移旧收藏数据，因为Favorites表缺少WallpaperId列");
            }
            // 为WallpaperId创建索引
            try {
                command.CommandText = "CREATE INDEX IF NOT EXISTS IX_Favorites_WallpaperId ON Favorites(WallpaperId)";
                command.ExecuteNonQuery();
            } catch (Exception ex) {
                Log.Warning("创建WallpaperId索引失败: {Error}", ex.Message);
            }

            // 创建分类表（使用ID作为主键）
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL,
                IsDefault BOOLEAN DEFAULT 0
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
            // 检查CollectionItems表是否存在，如果不存在则创建
            command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='CollectionItems'";
            var tableExists = Convert.ToInt64(command.ExecuteScalar()) > 0;

            if (!tableExists) {
                // 创建新表
                command.CommandText = @"
                    CREATE TABLE CollectionItems (
                        CollectionId TEXT NOT NULL,
                        WallpaperId TEXT NOT NULL,
                        AddedDate TEXT NOT NULL,
                        PRIMARY KEY (CollectionId, WallpaperId)
                    )";
                command.ExecuteNonQuery();
                Log.Information("已创建CollectionItems表（新结构）");
            } else {
                // 表已存在且没有WallpaperFolderPath列，可能是新结构
                // 确保WallpaperId列存在（理论上应该存在）
                // 如果表存在但结构可能不完整，我们信任现有结构
                Log.Debug("CollectionItems表已存在，跳过创建");
            }


            // 为WallpaperId创建索引
            try {
                command.CommandText = "CREATE INDEX IF NOT EXISTS IX_CollectionItems_WallpaperId ON CollectionItems(WallpaperId)";
                command.ExecuteNonQuery();
                Log.Information("已创建CollectionItems.WallpaperId索引");
            } catch (Exception ex) {
                Log.Warning("创建CollectionItems.WallpaperId索引失败: {Error}", ex.Message);
            }

            // 迁移分类到ID系统
            MigrateCategoriesToIdSystem();

            // 将所有默认分类标记转为普通分类（IsDefault = 0）
            command.CommandText = "UPDATE Categories SET IsDefault = 0 WHERE IsDefault = 1";
            command.ExecuteNonQuery();

            Log.Information("数据库初始化完成");
        }
        /// <summary>
        /// 保存壁纸记录到数据库，若已存在则更新（INSERT OR REPLACE）
        /// </summary>
        /// <param name="wallpaper">要保存的壁纸项</param>
        public void SaveWallpaper(WallpaperItem wallpaper)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                Log.Debug("保存壁纸记录: {FolderPath}", wallpaper.FolderPath);
                using var command = m_connection.CreateCommand();
                command.CommandText = @"
                INSERT OR REPLACE INTO Wallpapers
                (Id, FolderPath, FolderName, Title, Description, FileName, PreviewFile,
                 WallpaperType, Tags, IsFavorite, CategoryId, AddedDate, ContentRating, Visibility, FavoritedDate, LastUpdated, LastScanned, FolderLastModified, FileSize, FileHash)
                VALUES
                ($id, $folderPath, $folderName, $title, $description, $fileName, $previewFile,
                 $wallpaperType, $tags, $isFavorite, $categoryId, $addedDate, $contentRating, $visibility, $favoritedDate, $lastUpdated, $lastScanned, $folderLastModified, $fileSize, $fileHash)";

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
                command.Parameters.AddWithValue("$categoryId", wallpaper.CategoryId);
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
        }

        /// <summary>
        /// 根据关键词、分类和收藏状态搜索壁纸，支持标题、描述和标签的模糊匹配
        /// </summary>
        /// <param name="searchTerm">搜索关键词</param>
        /// <param name="categoryId">分类筛选ID，为0则不限分类（0表示"所有分类"）</param>
        /// <param name="favoritesOnly">是否仅返回已收藏的壁纸</param>
        /// <param name="hideAdultContent">是否隐藏成人内容（ContentRating 为 Mature 或 Questionable）</param>
        /// <returns>匹配的壁纸列表，最多返回 1000 条</returns>
        public List<WallpaperItem> SearchWallpapers(string searchTerm, int categoryId = 0, bool favoritesOnly = false, bool hideAdultContent = false)
        {
            Log.Debug("搜索壁纸, 关键词: {SearchTerm}, 分类ID: {CategoryId}, 仅收藏: {FavoritesOnly}, 隐藏成人内容: {HideAdultContent}", searchTerm, categoryId, favoritesOnly, hideAdultContent);
            var wallpapers = new List<WallpaperItem>();
            using var command = m_connection.CreateCommand();

            string whereClause = "WHERE 1=1";
            if (!string.IsNullOrEmpty(searchTerm)) {
                whereClause += " AND (w.Title LIKE @search OR w.Description LIKE @search OR w.Tags LIKE @search)";
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
            }
            if (categoryId != 0) {
                whereClause += " AND w.CategoryId = @categoryId";
                command.Parameters.AddWithValue("@categoryId", categoryId);
            }
            if (favoritesOnly) {
                whereClause += " AND f.WallpaperId IS NOT NULL";
            }
            if (hideAdultContent) {
                whereClause += " AND w.ContentRating NOT IN ('Mature', 'Questionable')";
            }

            command.CommandText = $@"
                SELECT w.*, f.WallpaperId AS FavFolderPath, f.FavoritedDate AS FavFavoritedDate
                FROM Wallpapers w
                LEFT JOIN Favorites f ON w.Id = f.WallpaperId
                {whereClause}
                ORDER BY (CASE WHEN f.WallpaperId IS NOT NULL THEN 1 ELSE 0 END) DESC, w.AddedDate DESC
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
            command.CommandText = "DELETE FROM Favorites WHERE WallpaperId = $id";
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
            // 先根据folderPath获取WallpaperId
            command.CommandText = "SELECT Id FROM Wallpapers WHERE FolderPath = @path";
            command.Parameters.AddWithValue("@path", folderPath);
            var wallpaperId = command.ExecuteScalar() as string;
            if (!string.IsNullOrEmpty(wallpaperId)) {
                // 删除收藏记录（通过WallpaperId）
                command.CommandText = "DELETE FROM Favorites WHERE WallpaperId = @wallpaperId";
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
                command.ExecuteNonQuery();
                command.Parameters.Clear();
            }
            // 删除壁纸记录
            command.CommandText = "DELETE FROM Wallpapers WHERE FolderPath = @path";
            command.Parameters.AddWithValue("@path", folderPath);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 释放数据库连接资源
        /// </summary>
        public void Dispose()
        {
            lock (m_lock) {
                Log.Debug("关闭数据库连接");
                m_connection?.Close();
                m_connection?.Dispose();
            }
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
        /// 获取指定合集中的所有壁纸ID，按添加时间倒序排列
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <returns>壁纸ID列表</returns>
        public List<string> GetCollectionItems(string collectionId)
        {
            var items = new List<string>();
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT WallpaperId FROM CollectionItems WHERE CollectionId = @id ORDER BY AddedDate DESC";
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
        /// <param name="wallpaperId">壁纸ID</param>
        /// <returns>已存在返回 true，否则返回 false</returns>
        public bool IsInCollection(string collectionId, string wallpaperId)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM CollectionItems WHERE CollectionId = @collectionId AND WallpaperId = @wallpaperId";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
            return Convert.ToInt64(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// 将壁纸添加到指定合集，若已存在则忽略
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="wallpaperId">壁纸ID</param>
        public void AddToCollection(string collectionId, string wallpaperId)
        {
            Log.Debug("添加壁纸到合集: {CollectionId}, {WallpaperId}", collectionId, wallpaperId);
            using var command = m_connection.CreateCommand();

            // 直接插入，不再需要文件夹路径
            command.CommandText = @"
                INSERT OR IGNORE INTO CollectionItems (CollectionId, WallpaperId, AddedDate)
                VALUES (@collectionId, @wallpaperId, @addedDate)";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
            command.Parameters.AddWithValue("@addedDate", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 从指定合集中移除壁纸
        /// </summary>
        /// <param name="collectionId">合集 ID</param>
        /// <param name="wallpaperId">壁纸ID</param>
        public void RemoveFromCollection(string collectionId, string wallpaperId)
        {
            using var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM CollectionItems WHERE CollectionId = @collectionId AND WallpaperId = @wallpaperId";
            command.Parameters.AddWithValue("@collectionId", collectionId);
            command.Parameters.AddWithValue("@wallpaperId", wallpaperId);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取包含指定壁纸的所有合集
        /// </summary>
        /// <param name="wallpaperId">壁纸ID</param>
        /// <returns>包含该壁纸的合集列表</returns>
        public List<WallpaperCollection> GetCollectionsForWallpaper(string wallpaperId)
        {
            var collections = new List<WallpaperCollection>();
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                SELECT c.Id, c.Name, c.CreatedDate
                FROM Collections c
                INNER JOIN CollectionItems ci ON c.Id = ci.CollectionId
                WHERE ci.WallpaperId = @wallpaperId
                ORDER BY c.CreatedDate DESC";
            command.Parameters.AddWithValue("@wallpaperId", wallpaperId);

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
                CategoryId = @categoryId,
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
            command.Parameters.AddWithValue("@categoryId", wallpaper.CategoryId);
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
            command.CommandText = "SELECT Name FROM Categories WHERE IsDefault = 0 ORDER BY Name";
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
        /// <returns>新分类的ID，如果已存在则返回现有分类的ID</returns>
        public int AddCategory(string name)
        {
            Log.Information("添加分类: {Name}", name);
            using var command = m_connection.CreateCommand();
            command.CommandText = @"
                INSERT OR IGNORE INTO Categories (Name, IsDefault) VALUES (@name, 0);
                SELECT Id FROM Categories WHERE Name = @name";
            command.Parameters.AddWithValue("@name", name);
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        /// <summary>
        /// 删除自定义分类，并将该分类下的所有壁纸重置为"未分类"
        /// </summary>
        /// <param name="id">要删除的分类ID</param>
        public void DeleteCategory(int id)
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                Log.Information("删除分类ID: {Id}", id);
                using var transaction = m_connection.BeginTransaction();
                try {
                    using var updateCmd = m_connection.CreateCommand();
                    updateCmd.CommandText = "UPDATE Wallpapers SET CategoryId = 1 WHERE CategoryId = @id";
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.ExecuteNonQuery();

                    using var deleteCmd = m_connection.CreateCommand();
                    deleteCmd.CommandText = "DELETE FROM Categories WHERE Id = @id AND IsDefault = 0";
                    deleteCmd.Parameters.AddWithValue("@id", id);
                    var rowsAffected = deleteCmd.ExecuteNonQuery();
                    if (rowsAffected == 0) {
                        Log.Warning("尝试删除默认分类或分类不存在: {Id}", id);
                    }

                    transaction.Commit();
                } catch {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// 重命名分类，同时更新所有使用该分类的壁纸记录
        /// </summary>
        /// <param name="id">要重命名的分类ID</param>
        /// <param name="newName">新分类名称</param>
        public void RenameCategory(int id, string newName)
        {
            Log.Information("重命名分类ID: {Id} -> {NewName}", id, newName);
            using var transaction = m_connection.BeginTransaction();
            try {
                // 更新分类名称
                using var updateCmd = m_connection.CreateCommand();
                updateCmd.CommandText = "UPDATE Categories SET Name = @newName WHERE Id = @id AND IsDefault = 0";
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.Parameters.AddWithValue("@newName", newName);
                var rowsAffected = updateCmd.ExecuteNonQuery();
                if (rowsAffected == 0) {
                    Log.Warning("尝试重命名默认分类或分类不存在: {Id}", id);
                    // 对于默认分类，不重命名
                    transaction.Commit();
                    return;
                }

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

        /// <summary>
        /// 获取所有分类的壁纸数量统计
        /// </summary>
        /// <param name="allCategories">所有需要统计的分类列表（包括虚拟分类和自定义分类）</param>
        /// <returns>分类名称到壁纸数量的字典</returns>
        public Dictionary<string, int> GetCategoryStatistics(List<string> allCategories)
        {
            var stats = new Dictionary<string, int>();

            // 为每个分类查询壁纸数量
            foreach (var category in allCategories) {
                var categoryId = GetCategoryIdByName(category);
                int count;
                if (categoryId >= 0) {
                    count = GetCategoryWallpaperCount(categoryId);
                } else {
                    // 虚拟分类或不存在
                    count = 0;
                }
                stats[category] = count;
            }

            return stats;
        }

        /// <summary>
        /// 获取指定分类的壁纸数量
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>该分类下的壁纸数量</returns>
        public int GetCategoryWallpaperCount(int categoryId)
        {
            // 虚拟分类处理
            if (CategoryConstants.IsVirtualCategoryId(categoryId)) {
                using var virtualCmd = m_connection.CreateCommand();
                if (categoryId == CategoryConstants.ALL_CATEGORIES_ID) {
                    // "所有分类" - 返回所有壁纸总数
                    virtualCmd.CommandText = "SELECT COUNT(*) FROM Wallpapers";
                } else if (categoryId == CategoryConstants.UNCATEGORIZED_ID) {
                    // "未分类" - 返回 CategoryId = 1 的壁纸数量
                    virtualCmd.CommandText = "SELECT COUNT(*) FROM Wallpapers WHERE CategoryId = @categoryId";
                    virtualCmd.Parameters.AddWithValue("@categoryId", categoryId);
                } else {
                    // 其他虚拟分类（目前没有）
                    return 0;
                }
                return Convert.ToInt32(virtualCmd.ExecuteScalar());
            }

            // 正常分类
            using var cmd = m_connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Wallpapers WHERE CategoryId = @categoryId";
            cmd.Parameters.AddWithValue("@categoryId", categoryId);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// 获取所有数据库中的分类（自定义分类）
        /// 注意：不包含虚拟分类（所有分类、未分类），调用者需要手动添加虚拟分类
        /// </summary>
        /// <returns>分类项列表</returns>
        public List<CategoryItem> GetAllCategories()
        {
            var categories = new List<CategoryItem>();
            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, IsDefault FROM Categories ORDER BY IsDefault DESC, Name";
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                var id = Convert.ToInt32(reader["Id"]);
                var name = reader["Name"].ToString();
                var isDefault = Convert.ToBoolean(reader["IsDefault"]);
                var isProtected = CategoryConstants.IsProtectedCategory(name);
                // 获取壁纸数量
                var count = GetCategoryWallpaperCount(id);
                categories.Add(new CategoryItem(id, name, count, isProtected, isDefault));
            }
            return categories;
        }

        /// <summary>
        /// 根据分类ID获取分类名称
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns>分类名称，如果不存在则返回null</returns>
        public string? GetCategoryNameById(int id)
        {
            // 检查虚拟分类
            var virtualName = CategoryConstants.GetVirtualCategoryName(id);
            if (virtualName != null)
                return virtualName;

            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT Name FROM Categories WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            var result = command.ExecuteScalar();
            return result?.ToString();
        }

        /// <summary>
        /// 根据分类名称获取分类ID
        /// </summary>
        /// <param name="name">分类名称</param>
        /// <returns>分类ID，如果不存在则返回-1</returns>
        public int GetCategoryIdByName(string name)
        {
            // 检查虚拟分类
            var virtualId = CategoryConstants.GetVirtualCategoryId(name);
            if (virtualId >= 0)
                return virtualId;

            using var command = m_connection.CreateCommand();
            command.CommandText = "SELECT Id FROM Categories WHERE Name = @name";
            command.Parameters.AddWithValue("@name", name);
            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        /// <summary>
        /// 迁移壁纸ID：检查所有壁纸的project.json文件，如果wallpaperId为空，则将数据库中的Id回写进去
        /// </summary>
        public void MigrateWallpaperIds()
        {
            lock (m_lock) {
                EnsureConnectionOpen();
                Log.Information("开始迁移壁纸ID");

                // 获取所有壁纸记录
                var wallpapers = new List<WallpaperItem>();
                using var command = m_connection.CreateCommand();
                command.CommandText = "SELECT Id, FolderPath FROM Wallpapers";
                using var reader = command.ExecuteReader();
                while (reader.Read()) {
                    wallpapers.Add(new WallpaperItem {
                        Id = reader["Id"].ToString(),
                        FolderPath = reader["FolderPath"].ToString()
                    });
                }

                int updatedCount = 0;
                foreach (var wallpaper in wallpapers) {
                    try {
                        var projectFile = Path.Combine(wallpaper.FolderPath, "project.json");
                        if (!File.Exists(projectFile))
                            continue;

                        var jsonContent = File.ReadAllText(projectFile);
                        var project = Newtonsoft.Json.JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);
                        if (project == null)
                            continue;

                        // 如果wallpaperId为空，则使用数据库中的Id
                        if (string.IsNullOrEmpty(project.WallpaperId)) {
                            project.WallpaperId = wallpaper.Id;
                            var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(project, Newtonsoft.Json.Formatting.Indented);
                            File.WriteAllText(projectFile, updatedJson);
                            updatedCount++;
                            Log.Debug("更新壁纸ID: {FolderPath} -> {Id}", wallpaper.FolderPath, wallpaper.Id);
                        }
                    } catch (Exception ex) {
                        Log.Warning("迁移壁纸ID失败 {FolderPath}: {Message}", wallpaper.FolderPath, ex.Message);
                    }
                }

                Log.Information("壁纸ID迁移完成，更新了 {UpdatedCount} 个文件", updatedCount);
            }
        }
    }
}