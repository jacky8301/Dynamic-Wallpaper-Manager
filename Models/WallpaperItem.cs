using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using Serilog;

namespace WallpaperEngine.Models {
    public partial class WallpaperItem : ObservableObject {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FolderPath { get; set; } = string.Empty;
        public string FolderName => Path.GetFileName(FolderPath);
        public WallpaperProject Project { get; set; } = new WallpaperProject();
        public string Category { get; set; } = "未分类";
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public string PreviewImagePath => Path.Combine(FolderPath, Project.Preview);
        public string ContentPath => Path.Combine(FolderPath, Project.File);
        [ObservableProperty]
        private bool _isSelected;
        public string LastUpdated { get; set; } = DateTime.Now.ToString("O");
        // 一个便利属性，用于判断是否为新添加的壁纸（非数据库中存在）
        [ObservableProperty]
        private bool _isFavorite;
        [ObservableProperty]
        private DateTime _favoritedDate;
        public bool IsNewlyAdded { get; set; } = false;
        [ObservableProperty]
        private DateTime _lastScanned;
        [ObservableProperty]
        private DateTime _folderLastModified;
        [ObservableProperty]
        private bool _isMarkedForDeletion;
        [ObservableProperty]
        private string _deletionStatus;

        // 检查壁纸文件是否存在
        public bool FilesExist {
            get {
                if (string.IsNullOrEmpty(FolderPath)) return false;
                return Directory.Exists(FolderPath);
            }
        }

        // 获取壁纸文件夹中的文件列表
        public List<string> GetContainedFiles()
        {
            if (!Directory.Exists(FolderPath))
                return new List<string>();

            return Directory.GetFiles(FolderPath, "*.*", SearchOption.AllDirectories).ToList();
        }

        // 计算文件夹大小
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

        // 壁纸详情编辑
        [ObservableProperty]
        private ObservableCollection<WallpaperFileInfo> _fileInfoList = new();

        [ObservableProperty]
        private ObservableCollection<string> _fileNameList = new();

        [ObservableProperty]
        private string _contentFileName = string.Empty;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private WallpaperItem _editBackup;

        // 文件信息类
        public class WallpaperFileInfo : ObservableObject {
            public string FileName { get; set; } = string.Empty;
            public string FullPath { get; set; } = string.Empty;
            public long FileSize { get; set; }
            public DateTime LastModified { get; set; }
            public string FileType { get; set; } = string.Empty;
            public bool IsImageFile => FileType.ToLower() switch {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp" => true,
                _ => false
            };
        }

        // 加载文件列表的方法
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

        // 识别主要内容文件
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