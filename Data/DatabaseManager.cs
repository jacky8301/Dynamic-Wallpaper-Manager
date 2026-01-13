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
                    LastUpdated TEXT
                )";
            command.ExecuteNonQuery();

            // 创建索引以提高搜索性能
            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Title ON Wallpapers(Title);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Tags ON Wallpapers(Tags);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_Category ON Wallpapers(Category);
                CREATE INDEX IF NOT EXISTS IX_Wallpapers_IsFavorite ON Wallpapers(IsFavorite);";
            command.ExecuteNonQuery();
        }
        // 插入或更新壁纸记录
        public void SaveWallpaper(WallpaperItem wallpaper)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = @"
                INSERT OR REPLACE INTO Wallpapers 
                (Id, FolderPath, FolderName, Title, Description, FileName, PreviewFile, 
                 WallpaperType, Tags, IsFavorite, Category, AddedDate, ContentRating, Visibility, FavoritedDate, LastUpdated)
                VALUES 
                ($id, $folderPath, $folderName, $title, $description, $fileName, $previewFile,
                 $wallpaperType, $tags, $isFavorite, $category, $addedDate, $contentRating, $visibility, $favoritedDate, $lastUpdated)";

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
                    Category = reader["Category"].ToString(),
                    AddedDate = DateTime.Parse(reader["AddedDate"].ToString()),
                    FavoritedDate = reader["FavoritedDate"].ToString(),
                    LastUpdated = reader["LastUpdated"].ToString()
                });
            }

            return wallpapers;
        }

        public void DeleteWallpaper(string id)
        {
            var command = m_connection.CreateCommand();
            command.CommandText = "DELETE FROM Wallpapers WHERE Id = $id";
            command.Parameters.AddWithValue("$id", id);
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
    }
}