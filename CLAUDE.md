# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Dynamic Wallpaper Manager is a WPF desktop application for managing wallpapers from the Wallpaper Engine ecosystem. It scans wallpaper directories, parses `project.json` files, and provides a UI for previewing, organizing (favorites/collections), and applying wallpapers.

## Build Commands

```bash
# Build
dotnet build DynamicWallpaperManager.csproj

# Build release
dotnet build DynamicWallpaperManager.csproj -c Release

# Build for x86 (legacy compatibility)
dotnet build DynamicWallpaperManager.csproj -c Release -p:Platform=x86
```

For installer packaging, use NSIS with `DynamicWallpaperManager.nsi`.

### Running the Application

- **Debug mode**: Use `dotnet run` from the project directory, or run the built executable from `bin/Debug/net8.0-windows/`.
- **Release mode**: Run the executable from `bin/Release/net8.0-windows/` or `bin/x86/Release/net8.0-windows/` for x86 builds.
- **Command line arguments**:
  - `-autostart`: Starts the application minimized (used for auto-start registration).
  - `-Show`: Shows the main window (used by desktop shortcut).

### Packaging with NSIS

The installer script `DynamicWallpaperManager.nsi` packages the x86 release build for distribution. To build the installer:

1. Ensure NSIS (Nullsoft Scriptable Install System) is installed.
2. Build the x86 release version: `dotnet build -c Release -p:Platform=x86`
3. Run NSIS on the `.nsi` file, or use the command line:
   ```bash
   makensis DynamicWallpaperManager.nsi
   ```
   The output executable will be named `DynamicWallpaperManager-{version}.exe`.

## Development Workflow

- **Code style**: The project uses a detailed `.editorconfig` to enforce coding conventions (4‑space indentation, explicit types, expression‑bodied members, etc.). No additional linter or formatter is required; Visual Studio / dotnet format will respect these settings.
- **Dependency injection**: All services and view‑models are registered as singletons in `App.xaml.cs` using `CommunityToolkit.Mvvm.DependencyInjection`. View‑models are split into partial classes for maintainability (see Architecture).
- **Logging**: Serilog is configured to write to the console and a rolling file (`log/dynamic_wallpaper_manager.log`). Log levels can be adjusted in `App.xaml.cs`.
- **Database**: SQLite database is automatically created at `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` on first run. No manual migration steps are needed; schema updates are handled by `DatabaseManager.InitializeDatabase()`.
- **Testing**: The project does not currently include unit tests.

## Architecture

### MVVM Pattern with Partial Classes

The application follows strict MVVM separation. ViewModels are split into partial class files for organization:

- `MainViewModel` → `MainViewModel.{Actions,Filtering,Scanning,Deletion}.cs`
- `WallpaperDetailViewModel` → `WallpaperDetailViewModel.{Editing,FileSelection}.cs`

ViewModels use `CommunityToolkit.Mvvm` source generators:
- `ObservableObject` base class for properties
- `[ObservableProperty]` generates property-changed notifications
- `[RelayCommand]` and `[AsyncRelayCommand]` generate command handlers

### Dependency Injection

Services are registered in `App.xaml.cs` using `Ioc.Default.ConfigureServices()`:

| Service | Scope | Purpose |
|---------|-------|---------|
| `DatabaseManager` | Singleton | SQLite data access |
| `ISettingsService` | Singleton | Settings persistence |
| `IDataContextService` | Singleton | Cross-ViewModel data sharing via events |
| `IWallpaperFileService` | Singleton | File system operations |
| `MaterialDialogService` | Singleton | Material Design dialog wrapper |

ViewModels are also registered as singletons for state persistence.

### Event-Based Communication

- `IDataContextService.CurrentWallpaperChanged` — Broadcasts when the currently selected wallpaper changes
- `WallpaperDetailViewModel.CategoryAdded` — Notifies when a new category is created
- `MainViewModel.LoadWallpapersCompleted` — Signals when wallpaper loading is finished

### Wallpaper Scanner Workflow

`WallpaperScanner.ScanWallpapersAsync()` performs directory enumeration looking for numbered subdirectories (e.g., `1/`, `2/`):

1. Reads `project.json` in each folder to extract metadata
2. Auto-categorizes based on tags (nature, abstract, game, anime, etc.)
3. Compares with database records for incremental updates
4. Reports progress via `IProgress<ScanProgress>`
5. Saves scan record to `ScanHistory` table

### Image Caching and Thumbnail Generation

- **ImageLoader**: Loads and resizes wallpaper preview images asynchronously, respecting a concurrent limit.
- **ThumbnailDiskCache**: Caches resized thumbnails on disk (`%USERPROFILE%\DynamicWallpaperManager\thumbnails\`) to avoid repeated processing.
- **ImageCache**: In‑memory LRU cache that holds decoded `BitmapImage` objects for fast UI display.

### Single Instance Management

`SingleInstanceManager` ensures only one instance of the application runs:
- Uses a named Mutex (`{80DEC730-14F5-4798-A4A7-EEEB4ADE1672}`) to detect existing instances.
- Subsequent instances send command‑line arguments to the first instance via a named pipe (`SingleInstance_{GUID}`).
- The first instance listens for connections and processes received arguments (e.g., to activate the main window).
- The pipe message format is a pipe‑separated (`|`) list of arguments.

### UI and Styling

- **Material Design for WPF**: The UI uses `MaterialDesignThemes` and `MaterialDesignColors` for a modern look.
- **VirtualizingWrapPanel**: The wallpaper grid uses `VirtualizingWrapPanel` to virtualize items and maintain performance with large collections.
- **Custom styles**: XAML styles are defined in the `Styles/` folder and referenced from `App.xaml`.

## Key File Locations

| Item | Location |
|------|----------|
| Database | `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` |
| Logs | `log/dynamic_wallpaper_manager.log` (rolling daily, 30-day retention) |
| Settings | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| Thumbnail Cache | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## Database Schema

SQLite tables: `Wallpapers`, `Favorites` (normalized), `Collections`, `CollectionItems`, `Categories`, `ScanHistory`.

The `Favorites` table is intentionally normalized — a wallpaper's favorite status is determined by checking this table, not a flag on `Wallpapers`. However, the `Wallpapers` table also contains an `IsFavorite` column that is kept in sync for performance (denormalized cache).

**Table details**:
- `Wallpapers`: Core wallpaper metadata, including folder path, title, tags, category, file hash, etc.
- `Favorites`: Records folder paths and favorited date; unique constraint on `FolderPath`.
- `Categories`: User‑defined category names.
- `Collections` and `CollectionItems`: For grouping wallpapers into user‑defined collections.
- `ScanHistory`: Log of each scan operation with statistics.

Indexes are created on frequently queried columns (`Title`, `Tags`, `Category`, `IsFavorite`, `ScanPath`, etc.).

## Wallpaper Engine Integration

- **Preview**: `PreviewService` launches Wallpaper Engine CLI for web/scene wallpapers
- **Apply**: Uses `wallpaper64.exe -control openWallpaper -file "project.json"`
- Path configured via `SettingsViewModel` (defaults to detecting Wallpaper Engine install)

## Code Style

Follow `.editorconfig` conventions:
- 4-space indentation
- Use explicit types instead of `var` (configured)
- Expression-bodied members for properties, accessors, indexers, lambdas
- Pattern matching preferred over is/cast checks
- `using` directives placed outside namespace

## Project Entry Points

- `App.xaml.cs` — Dependency injection setup, Serilog initialization, single-instance handling, `-autostart` argument processing
- `MainWindow.xaml.cs` — Main window initialization, system tray, loading overlay

### Command Line Argument Processing

- `-autostart`: Causes the main window to start minimized (hidden). Used when the application is launched from the registry `Run` key.
- Arguments are passed via the single‑instance pipe; the first instance activates its window and can process them (e.g., open a specific file). Currently only `-autostart` is implemented.

## Troubleshooting

- **Database locked**: Ensure no other process is accessing `wallpapers.db`. The application holds a single SQLite connection for its lifetime.
- **Wallpaper Engine not detected**: The `PreviewService` attempts to locate Wallpaper Engine via the registry; if not found, previews will fail. The path can be set manually in the settings UI.
- **High memory usage**: The `ImageCache` limits the number of cached images; thumbnails are stored on disk. If memory grows, check for unbounded collections in view‑models.
