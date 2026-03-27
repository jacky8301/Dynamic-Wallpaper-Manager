# Dynamic Wallpaper Manager

[中文](./readme.md) | English

Dynamic Wallpaper Manager is a desktop wallpaper management tool designed for Wallpaper Engine. Built with WPF and featuring a modern Material Design interface.

## Features

- **Wallpaper Scanning**: Automatically scan Wallpaper Engine wallpaper directories, parse `project.json` for metadata, with incremental updates
- **Static Wallpaper Management**: Import local image files (JPG, PNG, BMP, WebP), batch scan folders, automatic duplicate detection, set as desktop wallpaper
- **Category Management**: 10 intelligent default categories (Nature, Abstract, Games, Anime, Sci-Fi, Scenery, Architecture, Animals, People, Vehicles) with custom add/delete/rename support. Smart category suggestions based on wallpaper tags
- **Favorites**: One-click favorite wallpapers with a dedicated favorites view
- **Collections**: Create custom collections to organize wallpapers, with "Add to Collection" and "Move to Collection" support
- **Preview**: Built-in preview window with wallpaper details and file information
- **Quick Apply**: Set wallpapers as desktop background with one click, remembers the last applied wallpaper and restores it on startup
- **Content Rating Filter**: Option to hide adult content (Mature/Questionable ratings)
- **Batch Operations**: Multi-select with Ctrl+Click and Shift+Click for batch operations
- **System Tray**: Minimize to system tray with background running support
- **Auto-Start**: Launch on Windows startup, minimized to system tray
- **Single Instance**: Ensures only one instance runs to avoid duplicates

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Wallpaper Engine (for applying dynamic wallpapers)

## Installation

### Option 1: Download Installer

Download the latest installer from the [Releases](https://github.com/jacky8301/Dynamic-Wallpaper-Manager/releases) page.

### Option 2: Build from Source

```bash
# Clone repository
git clone https://github.com/jacky8301/Dynamic-Wallpaper-Manager.git
cd Dynamic-Wallpaper-Manager

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
2. Select the Wallpaper Engine wallpaper directory (typically under Steam's `steamapps/workshop/content/431960`)
3. Wait for the scan to complete, wallpapers will appear in the main window

### Browse by Category

- Click categories in the left panel to view wallpapers in each category
- "All Categories" shows all wallpapers
- "Uncategorized" shows wallpapers without a category
- Search by keyword in title and tags

### Static Wallpapers

- Switch to the "Static Wallpapers" tab
- Click "Import Images" or "Scan Folder" to add local images
- Select an image and click "Set as Wallpaper" to apply

### Favorite Wallpapers

- Select a wallpaper and click "Favorite" in the toolbar
- Favorited wallpapers can be viewed in "Favorites"

### Create Collections

- Select wallpapers in the list
- Right-click and select "Add to Collection" or "Move to Collection"
- Choose an existing collection or create a new one

### Apply Wallpaper

- Select a wallpaper and click "Set as Wallpaper" in the toolbar
- Or right-click and select "Set as Desktop Wallpaper"

### Command-Line Arguments

| Argument | Description |
|----------|-------------|
| `-autostart` | Start minimized to system tray (used for startup launch) |
| `-Show` | Show the main window on launch (used for desktop shortcuts) |

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
└── Styles/                  # Style definitions
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
| Logs | `log\dynamic_wallpaper_manager.log` (relative to app directory, daily rolling, 30-day retention) |
| Settings | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| Thumbnail Cache | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## License

MIT License

## Contributing

Feel free to submit Issues and Pull Requests!
