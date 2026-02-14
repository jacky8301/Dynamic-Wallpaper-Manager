using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using Serilog;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 壁纸项模型，表示一个壁纸资源及其相关元数据
    /// </summary>
    public partial class WallpaperItem : ObservableObject {
        /// <summary>
        /// 壁纸的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 壁纸所在文件夹的完整路径
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 壁纸文件夹名称（从路径中提取）
        /// </summary>
        public string FolderName => Path.GetFileName(FolderPath);

        /// <summary>
        /// 壁纸项目配置信息，对应 project.json
        /// </summary>
        public WallpaperProject Project { get; set; } = new WallpaperProject();

        /// <summary>
        /// 壁纸分类
        /// </summary>
        public string Category { get; set; } = "未分类";

        /// <summary>
        /// 壁纸添加日期
        /// </summary>
        public DateTime AddedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 预览图的完整路径
        /// </summary>
        public string PreviewImagePath => Path.Combine(FolderPath, Project.Preview);

        /// <summary>
        /// 壁纸主要内容文件的完整路径
        /// </summary>
        public string ContentPath => Path.Combine(FolderPath, Project.File);

        /// <summary>
        /// 是否被选中
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// 最后更新时间（ISO 8601 格式字符串）
        /// </summary>
        public string LastUpdated { get; set; } = DateTime.Now.ToString("O");

        /// <summary>
        /// 是否已收藏
        /// </summary>
        [ObservableProperty]
        private bool _isFavorite;

        /// <summary>
        /// 收藏日期
        /// </summary>
        [ObservableProperty]
        private DateTime _favoritedDate;

        /// <summary>
        /// 是否为新添加的壁纸（非数据库中已存在的）
        /// </summary>
        public bool IsNewlyAdded { get; set; } = false;

        /// <summary>
        /// 最后扫描时间
        /// </summary>
        [ObservableProperty]
        private DateTime _lastScanned;

        /// <summary>
        /// 文件夹最后修改时间
        /// </summary>
        [ObservableProperty]
        private DateTime _folderLastModified;

        /// <summary>
        /// 是否标记为待删除
        /// </summary>
        [ObservableProperty]
        private bool _isMarkedForDeletion;

        /// <summary>
        /// 删除状态描述信息
        /// </summary>
        [ObservableProperty]
        private string _deletionStatus;

        /// <summary>
        /// 检查壁纸文件夹是否存在
        /// </summary>
        public bool FilesExist {
            get {
                if (string.IsNullOrEmpty(FolderPath)) return false;
                return Directory.Exists(FolderPath);
            }
        }

        /// <summary>
        /// 获取壁纸文件夹中所有文件的路径列表（递归搜索）
        /// </summary>
        /// <returns>文件路径列表，若文件夹不存在则返回空列表</returns>
        public List<string> GetContainedFiles()
        {
            if (!Directory.Exists(FolderPath))
                return new List<string>();

            return Directory.GetFiles(FolderPath, "*.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// 计算壁纸文件夹的总大小（字节）
        /// </summary>
        /// <returns>文件夹总大小（字节），若文件夹不存在或计算失败则返回 0</returns>
        public long GetFolderSize()
        {
            if (!Directory.Exists(FolderPath)) return 0;

            try {
                return Directory.GetFiles(FolderPath, "*.*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            } catch (Exception ex) {
                Log.Warning($"计算文件夹大小失败 {FolderPath}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 壁纸文件夹中的文件详细信息列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperFileInfo> _fileInfoList = new();

        /// <summary>
        /// 壁纸文件夹中的文件名列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _fileNameList = new();

        /// <summary>
        /// 当前识别的主要内容文件名
        /// </summary>
        [ObservableProperty]
        private string _contentFileName = string.Empty;

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        [ObservableProperty]
        private bool _isEditing;

        /// <summary>
        /// 编辑前的备份数据，用于取消编辑时恢复
        /// </summary>
        [ObservableProperty]
        private WallpaperItem _editBackup;

        /// <summary>
        /// 壁纸文件信息类，描述壁纸文件夹中单个文件的详细信息
        /// </summary>
        public class WallpaperFileInfo : ObservableObject {
            /// <summary>
            /// 文件名（不含路径）
            /// </summary>
            public string FileName { get; set; } = string.Empty;

            /// <summary>
            /// 文件的完整路径
            /// </summary>
            public string FullPath { get; set; } = string.Empty;

            /// <summary>
            /// 文件大小（字节）
            /// </summary>
            public long FileSize { get; set; }

            /// <summary>
            /// 文件最后修改时间
            /// </summary>
            public DateTime LastModified { get; set; }

            /// <summary>
            /// 文件扩展名（如 .jpg、.mp4）
            /// </summary>
            public string FileType { get; set; } = string.Empty;

            /// <summary>
            /// 是否为图片文件（支持 jpg、jpeg、png、bmp、webp 格式）
            /// </summary>
            public bool IsImageFile => FileType.ToLower() switch {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp" => true,
                _ => false
            };
        }

        /// <summary>
        /// 异步加载壁纸文件夹中的文件列表，并更新到 UI 线程
        /// </summary>
        public async Task LoadFileListAsync()
        {
            if (string.IsNullOrEmpty(FolderPath) || !Directory.Exists(FolderPath))
                return;

            await Task.Run(() => {
                try {
                    var files = Directory.GetFiles(FolderPath, "*.*", SearchOption.TopDirectoryOnly);
                    var fileList = new List<WallpaperFileInfo>();

                    foreach (var file in files) {
                        var fileInfo = new FileInfo(file);
                        fileList.Add(new WallpaperFileInfo {
                            FileName = Path.GetFileName(file),
                            FullPath = file,
                            FileSize = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            FileType = Path.GetExtension(file).ToLower()
                        });
                    }

                    // 更新到UI线程
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        FileInfoList?.Clear();
                        FileNameList?.Clear();
                        foreach (var item in fileList.OrderBy(f => f.FileName)) {
                            FileInfoList.Add(item);
                            FileNameList.Add(item.FileName);
                        }

                        // 自动识别主要内容文件
                        IdentifyContentFile();
                    });
                } catch (Exception ex) {
                    Log.Error($"加载文件列表失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 识别壁纸文件夹中的主要内容文件，优先使用 project.json 中指定的文件，其次选择最大的图片文件
        /// </summary>
        private void IdentifyContentFile()
        {
            if (FileInfoList.Count == 0) return;

            // 优先使用project.json中指定的文件
            if (!string.IsNullOrEmpty(Project?.File) &&
                FileInfoList.Any(f => f.FileName.Equals(Project.File, StringComparison.OrdinalIgnoreCase))) {
                ContentFileName = Project.File;
                return;
            }

            // 查找常见的壁纸文件
            var imageFiles = FileInfoList.Where(f => f.IsImageFile).ToList();
            if (imageFiles.Count > 0) {
                // 优先选择较大的图像文件作为主要内容
                var mainFile = imageFiles.OrderByDescending(f => f.FileSize).First();
                ContentFileName = mainFile.FileName;
            }
        }
    }
}