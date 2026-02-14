using Serilog;
using System.IO;

namespace WallpaperEngine.Services {
    public class WallpaperFileService : IWallpaperFileService {
        public bool DeleteWallpaperFiles(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) {
                // 文件夹不存在，认为删除成功（或者可能是数据库记录残留）
                return true;
            }

            try {
                Directory.Delete(folderPath, true);
                return true;
            } catch (UnauthorizedAccessException) {
                // 权限不足，尝试强制删除
                return ForceDelete(folderPath);
            } catch (Exception ex) {
                Log.Error($"删除失败 {folderPath}: {ex.Message}");
                return false;
            }
        }

        public bool ForceDelete(string folderPath)
        {
            try {
                var directory = new DirectoryInfo(folderPath);

                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories)) {
                    file.Attributes = FileAttributes.Normal;
                    file.Delete();
                }

                foreach (var subDir in directory.GetDirectories()) {
                    ForceDelete(subDir.FullName);
                }

                directory.Delete();
                return true;
            } catch {
                return false;
            }
        }
    }
}
