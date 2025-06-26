# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
