# Dynamic Wallpaper Manager

[English](./README-en.md) | 中文

Dynamic Wallpaper Manager 是一款专为 Wallpaper Engine 设计的桌面壁纸管理工具，采用 WPF 开发，提供现代化的 Material Design 界面。

## 功能特性

- **壁纸扫描**：自动扫描 Wallpaper Engine 壁纸目录，解析 `project.json` 获取元数据
- **分类管理**：支持自定义分类（自然、抽象、游戏、动漫等），可添加、删除、重命名分类
- **收藏夹**：一键收藏喜欢的壁纸
- **集合管理**：创建自定义集合来组织壁纸
- **预览查看**：内置预览窗口，支持查看壁纸详情和文件信息
- **快速应用**：一键将壁纸设为桌面背景
- **系统托盘**：最小化到系统托盘，支持后台运行
- **单实例**：确保只有一个实例运行，避免重复

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
- Wallpaper Engine（用于应用壁纸功能）

## 安装

### 方式一：下载安装包

从 [Releases](https://github.com/your-repo/DynamicWallpaperManager/releases) 页面下载最新版本的安装包运行安装。

### 方式二：自行构建

```bash
# 克隆仓库
git clone https://github.com/your-repo/DynamicWallpaperManager.git
cd DynamicWallpaperManager

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
2. 选择 Wallpaper Engine 的壁纸目录（通常是 `steamapps/common/wallpaper_engine`）
3. 等待扫描完成，壁纸将显示在主界面

### 分类浏览

- 点击左侧分类栏查看不同分类的壁纸
- 「所有分类」显示全部壁纸
- 「未分类」显示未标记分类的壁纸
- 支持按关键词搜索标题和标签

### 收藏壁纸

- 选中壁纸后，点击工具栏的「收藏」按钮
- 收藏的壁纸可在「收藏夹」分类中查看

### 创建集合

- 在壁纸列表中选择壁纸
- 右键选择「添加到集合」或点击工具栏「添加到集合」按钮
- 选择现有集合或创建新集合

### 应用壁纸

- 选中壁纸后，点击工具栏的「设为壁纸」按钮
- 或右键选择「设为桌面壁纸」

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
| 日志 | `%USERPROFILE%\DynamicWallpaperManager\log\` |
| 设置 | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| 缩略图缓存 | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
