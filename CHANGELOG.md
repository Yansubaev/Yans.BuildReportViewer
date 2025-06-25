# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-06-25
### Added
- Initial package release for **Yans BuildReport Viewer**.
- `CustomBuildReportProcessor` for post-build analysis.
- `BuildReportHierarchyBuilder` with support for Files, PackedAssets, AssetUsage, StrippingInfo, and BuildSteps modes.
- `BuildFilesHierarchyWindow` EditorWindow and `BuildHierarchyTreeView` for visualizing reports.
- Entity classes: `BuildFileEntry`, `PackedAssetEntry`, `AssetUsageEntry`, `BuildStepEntry`, `BuildStepMessageEntry`, `SceneEntry`, `SimpleEntry`.
- `IBuildEntry` interface and `BuildDisplayMode` enum for display mode selection.
