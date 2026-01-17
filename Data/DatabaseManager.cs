// DatabaseManager.cs
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using WallpaperEngine.Models;

namespace WallpaperEngine.Data
{
    public class DatabaseManager : IDisposable
    {
        private SqliteConnection m_connection;
        private readonly string m_dbPath;

        public DatabaseManager(string databasePath = "wallpapers.db")
        {
            m_dbPath = databasePath;
            InitializeDatabase();
        }

        public class ScanInfo
        {
            public string ScanPath { get; set; } = string.Empty;
            public DateTime LastScanTime { get; set; } = DateTime.MinValue;
            public int TotalScanned { get; set; }
            public int NewFound { get; set; }
        }

        // 根据文件夹路径获取已存在的壁纸记录
        public WallpaperItem GetWallpaperByFolderPath(string folderPath)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = "SELECT * FROM Wallpapers WHERE FolderPath = @folderPath";
            command.Parameters.AddWithValue("@folderPath", folderPath);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    // 将数据库记录转换为 WallpaperItem 对象
                    return new WallpaperItem
                    {
                        Id = reader["Id"].ToString(),
                        FolderPath = reader["FolderPath"].ToString(),
                        Project = new WallpaperProject
                        {
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
                        IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1,
                        FavoritedDate = string.IsNullOrEmpty(reader["FavoritedDate"]?.ToString())
                                    ? DateTime.MinValue
                                    : DateTime.Parse(reader["FavoritedDate"].ToString()),
                        LastScanned = string.IsNullOrEmpty(reader["LastScanned"]?.ToString())
                                ? DateTime.MinValue
                                : DateTime.Parse(reader["LastScanned"].ToString()),
                        FolderLastModified = string.IsNullOrEmpty(reader["FolderLastModified"]?.ToString())
                                ? DateTime.MinValue
                                : DateTime.Parse(reader["FolderLastModified"].ToString())
                    };
                }
            }
            return null; // 没找到
        }

        // 专门用于切换收藏状态的方法
        public void ToggleFavorite(string wallpaperId, bool isFavorite)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = @"
            UPDATE Wallpapers 
            SET IsFavorite = @isFavorite, 
                FavoritedDate = @favoritedDate 
            WHERE Id = @id";

            command.Parameters.AddWithValue("@id", wallpaperId);
            command.Parameters.AddWithValue("@isFavorite", isFavorite ? 1 : 0);
            command.Parameters.AddWithValue("@favoritedDate", isFavorite ? DateTime.Now.ToString("O") : (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        private void InitializeDatabase()
        {
            var connectionString = $"Data Source={m_dbPath}";
            m_connection = new SqliteConnection(connectionString);
            m_connection.Open();

            // 创建壁纸表
            var command = m_connection.CreateCommand();
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
        }
        // 插入或更新壁纸记录
        public void SaveWallpaper(WallpaperItem wallpaper)
        {
            var command = m_connection.CreateCommand();
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

            try
            {
                var mainFile = Path.Combine(wallpaper.FolderPath, wallpaper.Project.File);
                if (File.Exists(mainFile))
                {
                    var fileInfo = new FileInfo(mainFile);
                    lastModified = fileInfo.LastWriteTime;
                    fileSize = fileInfo.Length;
                    //fileHash = CalculateFileHash(mainFile);
                }
            }
            catch
            {
                // 如果无法获取文件信息，使用当前时间
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
            command.Parameters.AddWithValue("$tags", string.Join(",", wallpaper.Project.Tags));
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
            var command = m_connection.CreateCommand();

            string whereClause = "WHERE 1=1";
            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClause += " AND (Title LIKE @search OR Description LIKE @search OR Tags LIKE @search)";
                command.Parameters.AddWithValue("@search", $"%{searchTerm}%");
            }
            if (!string.IsNullOrEmpty(category))
            {
                whereClause += " AND Category = @category";
                command.Parameters.AddWithValue("@category", category);
            }
            if (favoritesOnly)
            {
                whereClause += " AND IsFavorite = 1";
            }

            command.CommandText = $@"
                SELECT * FROM Wallpapers 
                {whereClause}
                ORDER BY IsFavorite DESC, AddedDate DESC
                LIMIT 1000";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                wallpapers.Add(new WallpaperItem
                {
                    Id = reader["Id"].ToString(),
                    FolderPath = reader["FolderPath"].ToString(),
                    Project = new WallpaperProject
                    {
                        Title = reader["Title"].ToString(),
                        Description = reader["Description"].ToString(),
                        File = reader["FileName"].ToString(),
                        Preview = reader["PreviewFile"].ToString(),
                        Type = reader["WallpaperType"].ToString(),
                        Tags = reader["Tags"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        ContentRating = reader["ContentRating"].ToString(),
                        Visibility = reader["Visibility"].ToString()
                    },
                    IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1,
                    FavoritedDate = string.IsNullOrEmpty(reader["FavoritedDate"]?.ToString())
                                    ? DateTime.MinValue
                                    : DateTime.Parse(reader["FavoritedDate"].ToString()),
                    Category = reader["Category"].ToString(),
                    AddedDate = DateTime.Parse(reader["AddedDate"].ToString()),
                    LastUpdated = reader["LastUpdated"].ToString(),
                    LastScanned = string.IsNullOrEmpty(reader["LastScanned"]?.ToString())
                                ? DateTime.MinValue
                                : DateTime.Parse(reader["LastScanned"].ToString()),
                    FolderLastModified = string.IsNullOrEmpty(reader["FolderLastModified"]?.ToString())
                                ? DateTime.MinValue
                                : DateTime.Parse(reader["FolderLastModified"].ToString())
                });
            }

            return wallpapers;
        }

        public void DeleteWallpaper(string wallpaperId)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Wallpapers WHERE Id = $id";
            command.Parameters.AddWithValue("$id", wallpaperId);
            command.ExecuteNonQuery();

            // 同时删除相关的收藏记录
            var favoriteCommand = m_connection.CreateCommand();
            favoriteCommand.CommandText = "DELETE FROM Favorites WHERE WallpaperId = @id";
            favoriteCommand.Parameters.AddWithValue("@id", wallpaperId);
            favoriteCommand.ExecuteNonQuery();
        }

        public void DeleteWallpaperByPath(string folderPath)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Wallpapers WHERE FolderPath = @path";
            command.Parameters.AddWithValue("@path", folderPath);
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            m_connection?.Close();
            m_connection?.Dispose();
        }

        // 新增：专门更新收藏状态的方法
        public void UpdateFavoriteStatus(string wallpaperId, bool isFavorite)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = @"
            UPDATE Wallpapers 
            SET IsFavorite = @isFavorite, 
                FavoritedDate = @favoritedDate,
                LastUpdated = @lastUpdated
            WHERE Id = @id";

            command.Parameters.AddWithValue("@id", wallpaperId);
            command.Parameters.AddWithValue("@isFavorite", isFavorite ? 1 : 0);
            command.Parameters.AddWithValue("@favoritedDate", isFavorite ? DateTime.Now.ToString("O") : (object)DBNull.Value);
            command.Parameters.AddWithValue("@lastUpdated", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();
        }

        // 保存扫描记录
        public void SaveScanRecord(string scanPath, int totalFolders, int newFound,
                                   long durationMs, string status = "Success")
        {
            var command = m_connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO ScanHistory 
            (ScanPath, LastScanTime, TotalFolders, NewFound, DurationMs, Status)
            VALUES 
            (@scanPath, @lastScanTime, @totalFolders, @newFound, @durationMs, @status)";

            command.Parameters.AddWithValue("@scanPath", scanPath);
            command.Parameters.AddWithValue("@lastScanTime", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@totalFolders", totalFolders);
            command.Parameters.AddWithValue("@newFound", newFound);
            command.Parameters.AddWithValue("@durationMs", durationMs);
            command.Parameters.AddWithValue("@status", status);

            command.ExecuteNonQuery();
        }

        // 检查壁纸是否需要更新
        public bool NeedsUpdate(string folderPath, DateTime lastModified, long fileSize)
        {
            var command = m_connection.CreateCommand();
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

        // 获取文件夹最后修改时间
        public DateTime? GetFolderLastModified(string folderPath)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = "SELECT $FolderLastModified FROM Wallpapers WHERE FolderPath = @folderPath";
            command.Parameters.AddWithValue("@folderPath", folderPath);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return DateTime.Parse(reader["FolderLastModified"].ToString());
            }
            return null;
        }

        // 计算文件哈希（用于检测内容变化）
        private string CalculateFileHash(string filePath)
        {
            try
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        // 获取扫描历史
        public List<ScanInfo> GetScanHistory(string scanPath = null)
        {
            var history = new List<ScanInfo>();
            var command = m_connection.CreateCommand();

            if (string.IsNullOrEmpty(scanPath))
            {
                command.CommandText = @"
                SELECT ScanPath, LastScanTime, TotalFolders, NewFound 
                FROM ScanHistory 
                ORDER BY LastScanTime DESC 
                LIMIT 50";
            }
            else
            {
                command.CommandText = @"
                SELECT ScanPath, LastScanTime, TotalFolders, NewFound 
                FROM ScanHistory 
                WHERE ScanPath = @scanPath 
                ORDER BY LastScanTime DESC 
                LIMIT 20";
                command.Parameters.AddWithValue("@scanPath", scanPath);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new ScanInfo
                {
                    ScanPath = reader["ScanPath"].ToString(),
                    LastScanTime = DateTime.Parse(reader["LastScanTime"].ToString()),
                    TotalScanned = Convert.ToInt32(reader["TotalFolders"]),
                    NewFound = Convert.ToInt32(reader["NewFound"])
                });
            }

            return history;
        }
    }
}