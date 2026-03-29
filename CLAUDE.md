# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Dynamic Wallpaper Manager is a WPF desktop application for managing wallpapers from the Wallpaper Engine ecosystem. It scans wallpaper directories, parses `project.json` files, and provides a UI for previewing, organizing (favorites/collections), and applying wallpapers.

The application features a comprehensive category system with intelligent suggestions, centralized category management service, and 10 sensible default categories to help users get started.

## Development Environment

**Prerequisites:**
- **.NET 8 SDK** - Required for building and running the application
- **Visual Studio 2022+** (optional) - Recommended for WPF development with full debugging support
- **NSIS** - Required for building the installer package

**First-time setup:**
1. Install .NET 8 SDK from https://dotnet.microsoft.com
2. Clone the repository
3. Run `dotnet restore` to restore dependencies

## Build Commands

```bash
# Build
dotnet build DynamicWallpaperManager.csproj

# Build release
dotnet build DynamicWallpaperManager.csproj -c Release

# Build for x86 (legacy compatibility)
dotnet build DynamicWallpaperManager.csproj -c Release -p:Platform=x86

# Clean build
dotnet clean
dotnet build DynamicWallpaperManager.csproj

# Restore dependencies
dotnet restore DynamicWallpaperManager.csproj
```

For installer packaging, use NSIS with `DynamicWallpaperManager.nsi`.

### Running the Application

- **Debug mode**: Use `dotnet run` from the project directory, or run the built executable from `bin/Debug/net8.0-windows/`. In Visual Studio, press F5 to debug.
- **Release mode**: Run the executable from `bin/Release/net8.0-windows/` or `bin/x86/Release/net8.0-windows/` for x86 builds.
- **Command line arguments**:
  - `-autostart`: Starts the application minimized (used for auto-start registration).
  - `-Show`: Shows the main window (used by desktop shortcut).
- **Single instance behavior**: The application uses `SingleInstanceManager` to ensure only one instance runs. Subsequent launches forward arguments to the first instance.

### Packaging with NSIS

The installer script `DynamicWallpaperManager.nsi` packages the x86 release build for distribution. To build the installer:

1. Ensure NSIS (Nullsoft Scriptable Install System) is installed.
2. Build the x86 release version: `dotnet build -c Release -p:Platform=x86`
3. The build automatically generates `version.nsh` from `version.json` for NSIS version information
4. Run NSIS on the `.nsi` file, or use the command line:
   ```bash
   makensis DynamicWallpaperManager.nsi
   ```
   The output executable will be named `DynamicWallpaperManager-{version}.exe`.

## Development Workflow

- **Code style**: The project uses a detailed `.editorconfig` to enforce coding conventions (4‑space indentation, explicit types, expression‑bodied members, etc.). No additional linter or formatter is required; Visual Studio / dotnet format will respect these settings.
- **Key dependencies**:
  - `CommunityToolkit.Mvvm` - MVVM source generators and observable properties
  - `MaterialDesignThemes` - Modern WPF UI components
  - `Serilog` - Structured logging with file and console sinks
  - `Microsoft.Data.Sqlite` - SQLite database access
  - `VirtualizingWrapPanel` - Performance-optimized grid layout
- **Dependency injection**: All services and view‑models are registered as singletons in `App.xaml.cs` using `CommunityToolkit.Mvvm.DependencyInjection`. View‑models are split into partial classes for maintainability (see Architecture).
- **Logging**: Serilog is configured to write to the console and a rolling file (`log/dynamic_wallpaper_manager.log`). Log levels can be adjusted in `App.xaml.cs`.
- **Database**: SQLite database is automatically created at `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` on first run. No manual migration steps are needed; schema updates are handled by `DatabaseManager.InitializeDatabase()`, which performs automatic schema migration (e.g., adding missing columns, rebuilding tables if necessary).
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
| `ICategoryService` | Singleton | Centralized category management with caching and events |
| `ISettingsService` | Singleton | Settings persistence |
| `IDataContextService` | Singleton | Cross-ViewModel data sharing via events |
| `IWallpaperFileService` | Singleton | File system operations |

ViewModels are also registered as singletons for state persistence: `MainViewModel`, `WallpaperDetailViewModel`, `FavoriteViewModel`, `CollectionViewModel`, `CategoryManagementViewModel`, `StaticWallpaperViewModel`, `SettingsViewModel`.

`MaterialDialogService` is a static utility class (not registered in DI) that provides standardized dialog boxes (confirmation, input, error).

### Event-Based Communication

- `IDataContextService.CurrentWallpaperChanged` — Broadcasts when the currently selected wallpaper changes
- `ICategoryService.CategoryChanged` — Notifies when categories are added, renamed, deleted, or refreshed (centralized event system)
- `WallpaperDetailViewModel.CategoryAdded` — Notifies when a new category is created
- `MainViewModel.LoadWallpapersCompleted` — Signals when wallpaper loading is finished

### Favorite State Synchronization

When a wallpaper's favorite status changes (via `MainViewModel.ToggleFavoriteCommand`):
1. The `DatabaseManager.ToggleFavorite()` method is called with the wallpaper's `Id`
2. The `Favorites` table is updated using `WallpaperId` as the foreign key
3. The `Wallpapers.IsFavorite` column is updated as a denormalized cache
4. The change is synchronized to the `CollectionViewModel` by matching `WallpaperItem.Id` (not `FolderPath`)
5. All instances of the same wallpaper (in main view, collection view, etc.) are updated to maintain consistency

### Wallpaper Scanner Workflow

`WallpaperScanner.ScanWallpapersAsync()` performs directory enumeration looking for numbered subdirectories (e.g., `1/`, `2/`):

1. Reads `project.json` in each folder to extract metadata
2. Generates or reads existing wallpaper ID from the `project.json` file
3. Uses category from `project.json` file; if empty or "未分类", sets to "未分类". Category names are mapped to IDs via the centralized category service
4. Compares with database records for incremental updates
5. Reports progress via `IProgress<ScanProgress>`
6. Saves scan record to `ScanHistory` table
7. Writes back the wallpaper ID to `project.json` if missing

### Category Management System

The application features a centralized category management system with intelligent suggestions and sensible defaults:

- **Protected Virtual Categories**: `所有分类` (All Categories, ID=0) and `未分类` (Uncategorized, ID=1) are protected virtual categories that cannot be renamed or deleted. These are not stored in the database but are handled programmatically.
- **Default Categories**: The system provides 10 sensible default categories to help users get started: 自然, 抽象, 游戏, 动漫, 科幻, 风景, 建筑, 动物, 人物, 车辆. Each default category includes matching tags for intelligent suggestions (e.g., "自然" matches nature, landscape, forest tags).
- **Custom Categories**: Users can add, rename, and delete custom categories via `CategoryManagementViewModel`. All custom categories are stored in the `Categories` table in the database with `IsDefault` flag to distinguish from user-defined categories.
- **Centralized Category Service**: `ICategoryService` provides a unified interface for all category operations with event-driven synchronization. It manages category caching and broadcasts `CategoryChanged` events to keep all view models synchronized.
- **Category Statistics**: The system tracks wallpaper counts per category via `DatabaseManager.GetCategoryStatistics()` and `ICategoryService.GetCategoryStatisticsAsync()`.
- **Intelligent Suggestions**: `CategoryConstants.SuggestCategoriesByTags()` can recommend categories based on wallpaper tags using the default category matching rules.
- Default categories are automatically created on first run if they don't exist.

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
- **Dialog Services**: `MaterialDialogService` provides standardized dialog boxes (confirmation, input, error). Input dialogs accept optional `defaultText` parameter to pre-populate the text field, commonly used for rename operations.
- **Preview Window**: The wallpaper preview window supports "Always on Top" functionality for convenient comparison while browsing.

### Static Wallpaper Management

`StaticWallpaperViewModel` manages importing and applying static image wallpapers (separate from Wallpaper Engine dynamic wallpapers):
- Users can import image files which are stored in the database as `StaticWallpaperItem` records
- `DesktopWallpaperService` uses Win32 `SystemParametersInfo` to set the desktop wallpaper and can stop Wallpaper Engine before applying
- The last applied wallpaper (static or dynamic) is saved for restoration on startup

## Key File Locations

| Item | Location |
|------|----------|
| Database | `%USERPROFILE%\DynamicWallpaperManager\wallpapers.db` |
| Logs | `log/dynamic_wallpaper_manager.log` (relative to working directory, rolling daily, 30-day retention) |
| Settings | `%APPDATA%\DynamicWallpaperManager\settings.json` |
| Thumbnail Cache | `%USERPROFILE%\DynamicWallpaperManager\thumbnails\` |

## Wallpaper ID System

Each wallpaper is assigned a unique identifier (`Wallpapers.Id`) stored as a TEXT primary key. This ID is:
- **Generated** when a wallpaper is first scanned, based on the `project.json` file's content or folder path
- **Persisted** in the `project.json` file itself (in the `wallpaperId` field) to survive folder moves
- **Used** as the primary foreign key in related tables (`Favorites.WallpaperId`, `CollectionItems.WallpaperId`)

The system maintains backward compatibility: Database initialization automatically migrates existing data from older schema versions.

## Database Schema

SQLite tables: `Wallpapers`, `Favorites` (normalized), `Collections`, `CollectionItems`, `Categories`, `ScanHistory`.

The `Favorites` table is intentionally normalized — a wallpaper's favorite status is determined by checking this table, not a flag on `Wallpapers`. However, the `Wallpapers` table also contains an `IsFavorite` column that is kept in sync for performance (denormalized cache).

**Table details**:
- `Wallpapers`: Core wallpaper metadata, including folder path, title, tags, category, file hash, etc. Primary key is `Id` (TEXT), a unique wallpaper identifier.
- `Favorites`: Records wallpaper favorite status. Uses `WallpaperId` (TEXT) as foreign key to `Wallpapers.Id`. Unique constraint on `WallpaperId`.
- `Categories`: User‑defined category names. Only custom categories are stored; default categories are hardcoded.
- `Collections`: User‑defined collections with name and creation date. Primary key is `Id` (TEXT).
- `CollectionItems`: Junction table linking collections to wallpapers. Contains `CollectionId`, `WallpaperId` (both TEXT foreign keys), and `AddedDate` (TEXT). Primary key is (`CollectionId`, `WallpaperId`).
- `ScanHistory`: Log of each scan operation with statistics.

Indexes are created on frequently queried columns (`Title`, `Tags`, `Category`, `IsFavorite`, `ScanPath`, etc.). An additional index `IX_Favorites_WallpaperId` exists for `Favorites.WallpaperId`.

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

## Troubleshooting

- **Database locked**: The application holds a single SQLite connection for its lifetime. Ensure no other process is accessing `wallpapers.db`.
- **Wallpaper Engine not detected**: `PreviewService` locates Wallpaper Engine via registry; path can be set manually in settings.
- **Category management**: Virtual categories (`所有分类` ID=0, `未分类` ID=1) are protected and cannot be renamed/deleted. Default categories have `IsDefault=1` in the database.
- **Database schema errors**: If migration fails, delete `wallpapers.db` and restart. The database will be recreated with the correct schema.
