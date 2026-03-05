# Dynamic Wallpaper Manager

[中文](./README.md) | English

Dynamic Wallpaper Manager is a desktop wallpaper management tool designed for Wallpaper Engine. Built with WPF and featuring a modern Material Design interface.

## Features

- **Wallpaper Scanning**: Automatically scan Wallpaper Engine wallpaper directories and parse `project.json` for metadata
- **Category Management**: Custom categories (Nature, Abstract, Games, Anime, etc.) with add, delete, rename support
- **Favorites**: One-click favorite your preferred wallpapers
- **Collections**: Create custom collections to organize wallpapers
- **Preview**: Built-in preview window with wallpaper details and file information
- **Quick Apply**: Set wallpapers as desktop background with one click
- **System Tray**: Minimize to system tray, support background running
- **Single Instance**: Ensures only one instance runs to avoid duplicates

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Wallpaper Engine (for applying wallpapers)

## Installation

### Option 1: Download Installer

Download the latest installer from the [Releases](https://github.com/your-repo/DynamicWallpaperManager/releases) page.

### Option 2: Build from Source

```bash
# Clone repository
git clone https://github.com/your-repo/DynamicWallpaperManager.git
cd DynamicWallpaperManager

# Restore dependencies
dotnet restore

# Build
dotnet build DynamicWallpaperManager.csproj -c Release

# Run
dotnet run --project DynamicWallpaperManager.csproj
```

## Getting Started

### First Time Use

1. Launch the application and click "Scan Wallpapers" in the toolbar
2. Select the Wallpaper Engine wallpaper directory (typically `steamapps/common/wallpaper_engine`)
3. Wait for the scan to complete, wallpapers will appear in the main window

### Browse by Category

- Click categories in the left panel to view wallpapers in each category
- "All Categories" shows all wallpapers
- "Uncategorized" shows wallpapers without a category
- Search by keyword in title and tags

### Favorite Wallpapers

- Select a wallpaper and click "Favorite" in the toolbar
- Favorited wallpapers can be viewed in the "Favorites" category

### Create Collections

- Select wallpapers in the list
- Right-click and select "Add to Collection" or click "Add to Collection" in the toolbar
- Choose an existing collection or create a new one

### Apply Wallpaper

- Select a wallpaper and click "Set as Wallpaper" in the toolbar
- Or right-click and select "Set as Desktop Wallpaper"

## Project Structure

```
DynamicWallpaperManager/
├── App.xaml.cs              # Application entry, DI configuration
├── MainWindow.xaml          # Main window
├── ViewModels/              # MVVM ViewModels
├── Views/                   # XAML Views
├── Services/                # Business services
├── Models/                  # Data models
├── Database/                # SQLite database management
├── Assets/                  # Static resources
└── Styles/                 # Style definitions
```

## Tech Stack

- **.NET 8.0** - Runtime framework
- **WPF** - UI framework
- **MaterialDesignThemes** - Material Design component library
- **CommunityToolkit.Mvvm** - MVVM framework
- **SQLite** - Local data storage
- **Serilog** - Logging framework

## Data Storage

| Type | Location |
|------|----------|
| Database | `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` |
| Logs | `%USERPROFILE%\DynamicWallpaperManager\log\` |
| Settings | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| Thumbnail Cache | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## License

MIT License

## Contributing

Feel free to submit Issues and Pull Requests!
