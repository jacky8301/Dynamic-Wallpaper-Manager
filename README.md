# Dynamic Wallpaper Manager

<img width="1298" height="930" alt="wallpaper" src="https://github.com/user-attachments/assets/2c0dab37-b366-4b28-b93e-fbe752263408" />

中文 | [English](./readme-en.md)

Dynamic Wallpaper Manager 是一款专为 Wallpaper Engine 设计的桌面壁纸管理工具，采用 WPF 开发，提供现代化的 Material Design 界面。

## 功能特性

- **壁纸扫描**：自动扫描 Wallpaper Engine 壁纸目录，解析 `project.json` 获取元数据，支持增量更新
- **静态壁纸管理**：支持导入本地图片文件（JPG、PNG、BMP、WebP），批量扫描文件夹，自动检测重复，设为桌面壁纸
- **分类管理**：内置 10 个智能默认分类（自然、抽象、游戏、动漫、科幻、风景、建筑、动物、人物、车辆），支持自定义添加、删除、重命名。基于壁纸标签的智能分类建议
- **收藏夹**：一键收藏喜欢的壁纸，独立收藏视图
- **集合管理**：创建自定义集合来组织壁纸，支持添加到集合和移动到集合
- **预览查看**：内置预览窗口，支持查看壁纸详情和文件信息
- **快速应用**：一键将壁纸设为桌面背景，支持记忆上次设置的壁纸并在启动时恢复
- **内容分级过滤**：可选隐藏成人内容（Mature/Questionable 分级）
- **批量操作**：支持 Ctrl+点击、Shift+点击 多选壁纸进行批量操作
- **系统托盘**：最小化到系统托盘，支持后台运行
- **开机自启**：支持开机自动启动，最小化到系统托盘
- **单实例**：确保只有一个实例运行，避免重复启动

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
- Wallpaper Engine（用于应用动态壁纸功能）

## 安装

### 方式一：下载安装包

从 [Releases](https://github.com/jacky8301/Dynamic-Wallpaper-Manager/releases) 页面下载最新版本的安装包运行安装。

### 方式二：自行构建

```bash
# 克隆仓库
git clone https://github.com/jacky8301/Dynamic-Wallpaper-Manager.git
cd Dynamic-Wallpaper-Manager

# 还原依赖
dotnet restore

# 构建
dotnet build DynamicWallpaperManager.csproj -c Release

# 运行
dotnet run --project DynamicWallpaperManager.csproj
```

## 使用说明

### 首次使用

1. 启动程序后，点击工具栏的「扫描壁纸」按钮
2. 选择 Wallpaper Engine 的壁纸目录（通常在 Steam 的 `steamapps/workshop/content/431960` 目录下）
3. 等待扫描完成，壁纸将显示在主界面

### 分类浏览

- 点击左侧分类栏查看不同分类的壁纸
- 「所有分类」显示全部壁纸
- 「未分类」显示未标记分类的壁纸
- 支持按关键词搜索标题和标签

### 静态壁纸

- 切换到「静态壁纸」标签页
- 点击「导入图片」或「扫描文件夹」添加本地图片
- 选中图片后点击「设为壁纸」即可应用

### 收藏壁纸

- 选中壁纸后，点击工具栏的「收藏」按钮
- 收藏的壁纸可在「收藏夹」中查看

### 创建集合

- 在壁纸列表中选择壁纸
- 右键选择「添加到集合」或「移动到集合」
- 选择现有集合或创建新集合

### 应用壁纸

- 选中壁纸后，点击工具栏的「设为壁纸」按钮
- 或右键选择「设为桌面壁纸」

### 命令行参数

| 参数 | 说明 |
|------|------|
| `-autostart` | 启动后最小化到系统托盘（用于开机自启） |
| `-Show` | 启动后显示主窗口（用于桌面快捷方式） |

## 项目结构

```
DynamicWallpaperManager/
├── App.xaml.cs              # 程序入口，依赖注入配置
├── MainWindow.xaml          # 主窗口
├── ViewModels/              # MVVM ViewModels
├── Views/                   # XAML 视图
├── Services/                # 业务服务
├── Models/                  # 数据模型
├── Database/                # SQLite 数据库管理
├── Assets/                  # 静态资源
└── Styles/                  # 样式定义
```

## 技术栈

- **.NET 8.0** - 运行时框架
- **WPF** - UI 框架
- **MaterialDesignThemes** - Material Design 组件库
- **CommunityToolkit.Mvvm** - MVVM 框架
- **SQLite** - 本地数据存储
- **Serilog** - 日志框架

## 数据存储

| 类型 | 位置 |
|------|------|
| 数据库 | `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` |
| 日志 | `log\dynamic_wallpaper_manager.log`（应用目录下，按天滚动，保留 30 天） |
| 设置 | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| 缩略图缓存 | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
