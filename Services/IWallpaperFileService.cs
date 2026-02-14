namespace WallpaperEngine.Services {
    public interface IWallpaperFileService {
        /// <summary>
        /// 删除壁纸文件夹
        /// </summary>
        bool DeleteWallpaperFiles(string folderPath);

        /// <summary>
        /// 强制删除（处理只读文件）
        /// </summary>
        bool ForceDelete(string folderPath);
    }
}
