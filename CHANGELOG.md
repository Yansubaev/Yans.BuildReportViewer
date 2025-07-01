# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2025-07-02
### Added
- **Explorer Context Menu Support**: Enabled "Show in Explorer" context menu by implementing robust path detection for both Unity assets and build output files.
- **File Details Panel**: Added comprehensive file information panel that appears at the bottom of the window when selecting individual files in Files mode.
- **Path Handling**: Context menu and double-click actions now check multiple path sources (AssetPath and FullPath) for better file location detection.
- **BuildFile Integration**: Enhanced `BuildFileEntry` class to store additional BuildFile information including file ID, role, and full path for detailed display.
- **Reusable UI Components**: Created `InfoPanelRenderer` utility class for consistent info panel styling across different modes.
- **Clickable File Paths**: File paths in the details panel are now clickable buttons that open the file location in system explorer.
- **Enhanced Explorer Integration**: Improved "Show in Explorer" context menu and double-click functionality to work with both Unity assets and build output files.
- **Smart Path Resolution**: File info panel displays both Unity asset paths (for project files) and full system paths (for build output files).

### Changed
- **Unified Details Panel Logic**: Refactored message details and file details to use consistent rendering approach and reusable components.
- **Enhanced BuildFileEntry Constructor**: Extended constructor to accept optional BuildFile properties (file ID, role) while maintaining backward compatibility.

## [1.0.1] - 2025-06-26
### Added
- **Enhanced Build Report Management**: Automatic saving of build reports to `Assets/BuildReports/` directory after each successful build.
- **Smart File Naming**: Build reports now use descriptive names based on project output path: `BuildReport_{ProjectName}_{Platform}_{Timestamp}.buildreport`.
- **Intelligent Report Loading**: Window remembers last opened report and automatically loads the most appropriate report on startup.
- **Enhanced File Browser**: File dialog opens to `Assets/BuildReports/` by default with automatic copying and proper naming for external files.
- **Custom BuildReport Inspector**: Added custom inspector for BuildReport assets with "Open in BuildReport Viewer" button for direct access.
- **Enhanced Build Summary**: Comprehensive build summary display with professional two-column layout, clickable output paths, and human-readable file sizes.
- **Text Wrapping Support**: Options field in build summary now supports text wrapping for long build configuration strings.
- **"Open BuildReports Folder"** button for quick access to saved reports directory.
- **Automatic Cleanup**: Old build reports are automatically cleaned up (keeps last 10 by default) to prevent directory clutter.
- **BuildReportCleanupUtility**: Manual cleanup utility accessible via `Tools > BuildReport Viewer > Cleanup Old Files`.
- **Menu Integration**: Added `Tools > BuildReport Viewer > Open BuildReport Viewer` menu item for easy access to the window.

### Changed
- **Improved Caching System**: Eliminated persistent temporary files in favor of proper caching in `Assets/BuildReports/`.
- **Enhanced Naming Convention**: Project name extracted from `report.summary.outputPath` for better file organization.
- **Streamlined Loading Logic**: Simplified file loading with automatic cache management and cleanup.
- **Professional UI Layout**: Build summary now uses consistent two-column layout with proper field alignment and text wrapping.
- **Improved Inspector Experience**: BuildReport assets now have cleaner inspector display with reduced clutter.

### Removed
- **Legacy Temporary Files**: Eliminated `Assets/BuildReport/Reports/temp_buildreport.buildreport` and associated temporary file system.
- **Old Directory Structure**: Automatic cleanup of deprecated `Assets/BuildReport/` directory structure.
- **Default Inspector Warnings**: Removed Unity's default inspector warnings and clutter for BuildReport assets.

### Fixed
- **File Persistence Issues**: Build reports now properly cached without persistent temporary files.

## [1.0.0] - 2025-06-25
### Added
- Initial package release for **Yans BuildReport Viewer**.
- `CustomBuildReportProcessor` for post-build analysis.
- `BuildReportHierarchyBuilder` with support for Files, PackedAssets, AssetUsage, StrippingInfo, and BuildSteps modes.
- `BuildFilesHierarchyWindow` EditorWindow and `BuildHierarchyTreeView` for visualizing reports.
- Entity classes: `BuildFileEntry`, `PackedAssetEntry`, `AssetUsageEntry`, `BuildStepEntry`, `BuildStepMessageEntry`, `SceneEntry`, `SimpleEntry`.
- `IBuildEntry` interface and `BuildDisplayMode` enum for display mode selection.
