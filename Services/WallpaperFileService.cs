using Serilog;
using System.IO;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 壁纸文件服务实现，处理壁纸文件夹的删除操作，包括只读文件的强制删除
    /// </summary>
    public class WallpaperFileService : IWallpaperFileService {
        /// <summary>
        /// 删除指定壁纸文件夹及其所有内容，权限不足时自动尝试强制删除
        /// </summary>
        /// <param name="folderPath">壁纸文件夹路径</param>
        /// <returns>删除成功返回 true，否则返回 false</returns>
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

        /// <summary>
        /// 强制删除文件夹，先将所有文件属性设为 Normal 以移除只读标记，再递归删除
        /// </summary>
        /// <param name="folderPath">要强制删除的文件夹路径</param>
        /// <returns>删除成功返回 true，否则返回 false</returns>
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
            } catch (Exception ex) {
                Log.Warning($"强制删除失败 {folderPath}: {ex.Message}");
                return false;
            }
        }
    }
}
