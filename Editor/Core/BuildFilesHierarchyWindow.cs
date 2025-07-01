using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yans.BuildAnalyser
{
    // Editor window for displaying build hierarchy
    public class BuildFilesHierarchyWindow : EditorWindow
    {
        private const string PREF_DISPLAY_MODE = "BuildHierarchy_DisplayMode";
        private const string PREF_LAST_REPORT_PATH = "BuildHierarchy_LastReportPath";
        private const string BUILD_REPORTS_DIR = "Assets/BuildReports";

        #region private fields
        private BuildHierarchyTreeView treeView;
        private TreeViewState treeViewState;
        private IBuildEntry rootEntry;
        private string lastLoadedReportPath;
        private BuildDisplayMode currentMode = BuildDisplayMode.Files;
        private BuildReport currentReport;
        private bool needsReload;
        // Scroll position for message details
        private Vector2 messageScrollPos;
        // Scroll position for file info details
        private Vector2 fileInfoScrollPos;
        // Keep track of current temp file to clean up when loading new reports
        private string currentTempAssetPath;
        // Static flag to prevent auto-loading when opening with specific report
        private static bool isOpeningWithSpecificReport = false;
        #endregion

        public static void ShowWindow()
        {
            var window = GetWindow<BuildFilesHierarchyWindow>();
            window.titleContent = new GUIContent("BuildReport Viewer");
            window.Show();
        }

        /// <summary>
        /// Opens the BuildReport Viewer window and loads the specified build report
        /// </summary>
        /// <param name="buildReportPath">Absolute path to the build report file</param>
        /// <returns>The BuildFilesHierarchyWindow instance</returns>
        public static BuildFilesHierarchyWindow ShowWindowWithReport(string buildReportPath)
        {
            // Set static flag before creating window to prevent auto-loading in OnEnable
            isOpeningWithSpecificReport = true;
            
            var window = GetWindow<BuildFilesHierarchyWindow>();
            window.titleContent = new GUIContent("BuildReport Viewer");
            window.Show();
            window.Focus();
            
            // Load the specific build report
            window.LoadBuildReportFromPath(buildReportPath);
            
            // Reset the flag after loading
            isOpeningWithSpecificReport = false;
            
            return window;
        }

        #region private methods

        private void OnEnable()
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            // Load saved display mode
            currentMode = (BuildDisplayMode)EditorPrefs.GetInt(PREF_DISPLAY_MODE, (int)BuildDisplayMode.Files);

            // Ensure BuildReports directory exists
            if (!Directory.Exists(BUILD_REPORTS_DIR))
            {
                Directory.CreateDirectory(BUILD_REPORTS_DIR);
                AssetDatabase.Refresh();
            }

            // Clean up old temporary directory structure
            CleanupOldTempDirectory();

            // Only auto-load if we're not opening with a specific report
            if (!isOpeningWithSpecificReport)
            {
                LoadBuildReport();
            }
        }

        private void OnDisable()
        {
            CleanupCurrentTempFile();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Load Build Report", EditorStyles.toolbarButton))
            {
                LoadBuildReportFromFile();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                LoadBuildReport();
            }

            GUILayout.Space(10);

            // Display mode selector
            EditorGUI.BeginChangeCheck();
            currentMode = (BuildDisplayMode)EditorGUILayout.EnumPopup(currentMode, EditorStyles.toolbarPopup, GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt(PREF_DISPLAY_MODE, (int)currentMode);
                RefreshHierarchy();
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(lastLoadedReportPath))
            {
                string fileName = Path.GetFileName(lastLoadedReportPath);
                bool isFromBuildReports = lastLoadedReportPath.StartsWith(BUILD_REPORTS_DIR, StringComparison.OrdinalIgnoreCase);
                string prefix = isFromBuildReports ? "📁 " : "📂 ";
                GUILayout.Label($"{prefix}{fileName}", EditorStyles.toolbarButton);
            }

            EditorGUILayout.EndHorizontal();

            // Calculate content start Y (below toolbar and optional summary)
            float contentStartY = EditorStyles.toolbar.fixedHeight;

            // Integrated Build Summary at top
            if (currentReport != null)
            {
                var summary = currentReport.summary;
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Build Summary", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                
                // Use consistent two-column layout for all fields
                const float labelWidth = 120f;
                
                // Result
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Result:", GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField(summary.result.ToString());
                EditorGUILayout.EndHorizontal();
                
                // Platform
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Platform:", GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField(summary.platform.ToString());
                EditorGUILayout.EndHorizontal();
                
                // Options with text wrapping
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Options:", GUILayout.Width(labelWidth));
                var optionsStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true
                };
                EditorGUILayout.LabelField(summary.options.ToString(), optionsStyle);
                EditorGUILayout.EndHorizontal();
                
                // Clickable output path
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Output Path:", GUILayout.Width(labelWidth));
                if (GUILayout.Button(summary.outputPath, EditorStyles.linkLabel))
                {
                    EditorUtility.RevealInFinder(summary.outputPath);
                }
                EditorGUILayout.EndHorizontal();
                
                // Output Size: compute file/folder size
                {
                    var outPath = summary.outputPath;
                    ulong outBytes = 0;
                    if (File.Exists(outPath))
                        outBytes = (ulong)new FileInfo(outPath).Length;
                    else if (Directory.Exists(outPath))
                        outBytes = (ulong)Directory.GetFiles(outPath, "*", SearchOption.AllDirectories)
                            .Sum(f => new FileInfo(f).Length);
                    string outHuman = FormatBytes(outBytes);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Output Size:", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField($"{outBytes:N0} bytes ({outHuman})");
                    EditorGUILayout.EndHorizontal();
                }
                
                // Total size with human-readable format
                {
                    ulong totalBytes = summary.totalSize;
                    string human = FormatBytes(totalBytes);
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Total Size:", GUILayout.Width(labelWidth));
                    EditorGUILayout.LabelField($"{totalBytes:N0} bytes ({human})");
                    EditorGUILayout.EndHorizontal();
                }
                
                // Total Time
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total Time:", GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField(summary.totalTime.ToString());
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                // After drawing summary, adjust content start Y
                var summaryRect = GUILayoutUtility.GetLastRect();
                contentStartY = summaryRect.yMax;
            }

            if (rootEntry == null || rootEntry.Children.Count == 0)
            {
                var rect = new Rect(0, contentStartY, position.width, position.height - contentStartY);

                string message = currentMode switch
                {
                    BuildDisplayMode.Files => "No files found in this build report.\nTry building your project first, then refresh.",
                    BuildDisplayMode.PackedAssets => "No packed assets in this build report.\nTry building your project first, then refresh.",
                    BuildDisplayMode.AssetUsage => "No asset usage data in this build report.\nTry building your project first, then refresh.",
                    BuildDisplayMode.BuildSteps => "No build steps found in this build report.\nTry building your project first, then refresh.",
                    BuildDisplayMode.StrippingInfo => "No stripping info found in this build report.\nTry building your project first, then refresh.",
                    _ => "Build report not found or empty.\nTry building your project first, then refresh."
                };

                GUI.Label(rect, message, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (treeView != null)
            {
                float totalHeight = position.height - contentStartY;
                bool showMessage = currentMode == BuildDisplayMode.BuildSteps && treeView.SelectedEntry is BuildStepMessageEntry;
                bool showFileInfo = currentMode == BuildDisplayMode.Files && treeView.SelectedEntry is BuildFileEntry fileEntry && !fileEntry.IsDirectory;
                
                float detailsHeight = 0;
                if (showMessage || showFileInfo)
                {
                    detailsHeight = Mathf.Min(200, totalHeight * 0.3f);
                }
                
                float treeHeight = totalHeight - detailsHeight;
                var treeRect = new Rect(0, contentStartY, position.width, treeHeight);
                treeView.OnGUI(treeRect);
                
                // Show message details for BuildSteps mode
                if (showMessage && treeView.SelectedEntry is BuildStepMessageEntry msgEntry)
                {
                    var msgRect = new Rect(0, contentStartY + treeHeight, position.width, detailsHeight);
                    RenderMessagePanel(msgRect, msgEntry);
                }
                // Show file details for Files mode
                else if (showFileInfo)
                {
                    var fileInfoRect = new Rect(0, contentStartY + treeHeight, position.width, detailsHeight);
                    RenderFileInfoPanel(fileInfoRect, (BuildFileEntry)treeView.SelectedEntry);
                }
            }
        }

        private void RefreshHierarchy()
        {
            if (currentReport == null) return;

            try
            {
                switch (currentMode)
                {
                    case BuildDisplayMode.Files:
                        rootEntry = BuildReportHierarchyBuilder.BuildFromFiles(currentReport);
                        break;
                    case BuildDisplayMode.PackedAssets:
                        rootEntry = BuildReportHierarchyBuilder.BuildFromPackedAssets(currentReport);
                        break;
                    case BuildDisplayMode.AssetUsage:
                        rootEntry = BuildReportHierarchyBuilder.BuildFromAssetUsage(currentReport);
                        break;
                    case BuildDisplayMode.BuildSteps:
                        rootEntry = BuildReportHierarchyBuilder.BuildFromSteps(currentReport);
                        break;
                    case BuildDisplayMode.StrippingInfo:
                        rootEntry = BuildReportHierarchyBuilder.BuildFromStrippingInfo(currentReport);
                        break;
                }

                if (rootEntry != null)
                {
                    treeView = new BuildHierarchyTreeView(treeViewState, rootEntry, currentMode);
                    Debug.Log($"Switched to {currentMode} mode, total size: {rootEntry.SizeBytes / (1024f * 1024f):F2} MB");
                }

                // Force repaint to update the view immediately
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to refresh hierarchy: {ex.Message}");
            }
        }

        private void LoadBuildReport()
        {
            // Try to load the last opened report first
            string savedReportPath = EditorPrefs.GetString(PREF_LAST_REPORT_PATH, "");
            
            if (!string.IsNullOrEmpty(savedReportPath) && File.Exists(savedReportPath))
            {
                LoadBuildReportFromPath(savedReportPath);
                return;
            }
            
            // Try to find the most recent build report in BuildReports directory
            string mostRecentReport = GetMostRecentBuildReport();
            if (!string.IsNullOrEmpty(mostRecentReport))
            {
                LoadBuildReportFromPath(mostRecentReport);
                return;
            }
            
            // Fallback to the default Library location
            string defaultPath = Path.Combine("Library", "LastBuild.buildreport");
            LoadBuildReportFromPath(defaultPath);
        }

        private void LoadBuildReportFromFile()
        {
            // Default to BuildReports directory, fallback to Library if it doesn't exist
            string defaultDirectory = Directory.Exists(BUILD_REPORTS_DIR) ? BUILD_REPORTS_DIR : "Library";
            
            string path = EditorUtility.OpenFilePanel("Select Build Report", defaultDirectory, "buildreport");

            Debug.Log($"Selected build report path: {path}");
            
            if (!string.IsNullOrEmpty(path))
            {
                LoadBuildReportFromPath(path);
            }
        }

        public void LoadBuildReportFromPath(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"Build report not found at: {path}");
                    rootEntry = new BuildFileEntry("Empty", 0);
                    lastLoadedReportPath = null;
                    currentReport = null;
                    CleanupCurrentTempFile();
                    return;
                }

                // Clean up previous temp file before creating a new one
                CleanupCurrentTempFile();

                // If the file is not already in our BuildReports directory, copy it there with a proper name
                string cacheFilePath = path;
                string absBuildReportsDir = Path.GetFullPath(BUILD_REPORTS_DIR);
                string absPath = Path.GetFullPath(path);

                if (!absPath.StartsWith(absBuildReportsDir, StringComparison.OrdinalIgnoreCase))
                {
                    cacheFilePath = CopyToBuildReportsCache(path);
                    if (string.IsNullOrEmpty(cacheFilePath))
                    {
                        Debug.LogError("Failed to cache build report file");
                        return;
                    }
                }

                BuildReport report;

                // Convert to relative Unity asset path for loading
                string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), cacheFilePath).Replace('\\', '/');
                report = AssetDatabase.LoadAssetAtPath<BuildReport>(relativePath);

                if (report != null)
                {
                    currentReport = report;
                    lastLoadedReportPath = cacheFilePath; // Use the cached file path
                    
                    // Save the successfully loaded report path to preferences
                    EditorPrefs.SetString(PREF_LAST_REPORT_PATH, cacheFilePath);
                    
                    RefreshHierarchy();

                    string modeInfo = currentMode switch
                    {
                        BuildDisplayMode.Files => $"{report.files?.Length ?? 0} files",
                        BuildDisplayMode.PackedAssets => $"{(report.packedAssets?.Length ?? 0)} packed assets",
                        BuildDisplayMode.AssetUsage => $"{GetAssetUsageCount(report)} asset usages",
                        _ => "unknown"
                    };

                    Debug.Log($"Loaded build report with {modeInfo}, total size: {(rootEntry?.SizeBytes ?? 0) / (1024f * 1024f):F2} MB");
                }
                else
                {
                    Debug.LogWarning("Build report is null or invalid");
                    rootEntry = new BuildFileEntry("Empty", 0);
                    lastLoadedReportPath = null;
                    currentReport = null;
                    CleanupCurrentTempFile();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load build report: {ex.Message}\n{ex.StackTrace}");
                rootEntry = new BuildFileEntry("Empty", 0);
                lastLoadedReportPath = null;
                currentReport = null;
                CleanupCurrentTempFile();
            }
        }

        private int GetAssetUsageCount(BuildReport report)
        {
            try
            {
                if (report?.scenesUsingAssets == null) return 0;

                int count = 0;
                foreach (var scenesUsingAssetsObj in report.scenesUsingAssets)
                {
                    if (scenesUsingAssetsObj?.list != null)
                    {
                        count += scenesUsingAssetsObj.list.Length;
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error getting asset usage count: {ex.Message}");
                return 0;
            }
        }

        private string FormatBytes(ulong bytes)
        {
            // Convert bytes to a human-readable format
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int i = 0;
            while (i < suffixes.Length - 1 && size >= 1024.0)
            {
                size /= 1024.0;
                i++;
            }
            return string.Format("{0:0.##} {1}", size, suffixes[i]);
        }

        private string GetMostRecentBuildReport()
        {
            try
            {
                if (!Directory.Exists(BUILD_REPORTS_DIR))
                    return null;

                var buildReportFiles = Directory.GetFiles(BUILD_REPORTS_DIR, "*.buildreport", SearchOption.TopDirectoryOnly);
                
                if (buildReportFiles.Length == 0)
                    return null;

                // Get the most recently modified file
                return buildReportFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .First()
                    .FullName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error finding most recent build report: {ex.Message}");
                return null;
            }
        }

        private void CleanupOldBuildReports(int maxReports = 10)
        {
            try
            {
                if (!Directory.Exists(BUILD_REPORTS_DIR))
                    return;

                var buildReportFiles = Directory.GetFiles(BUILD_REPORTS_DIR, "*.buildreport", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .ToArray();

                if (buildReportFiles.Length <= maxReports)
                    return;

                // Delete old reports beyond the limit
                for (int i = maxReports; i < buildReportFiles.Length; i++)
                {
                    try
                    {
                        buildReportFiles[i].Delete();
                        Debug.Log($"Cleaned up old build report: {buildReportFiles[i].Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to delete old build report {buildReportFiles[i].Name}: {ex.Message}");
                    }
                }

                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during build report cleanup: {ex.Message}");
            }
        }

        private void CleanupOldTempDirectory()
        {
            try
            {
                string oldTempDir = "Assets/BuildReport";
                if (Directory.Exists(oldTempDir))
                {
                    Directory.Delete(oldTempDir, true);
                    string metaFile = oldTempDir + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Delete(metaFile);
                    }
                    AssetDatabase.Refresh();
                    Debug.Log("Cleaned up old temporary BuildReport directory");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Could not clean up old temp directory: {ex.Message}");
            }
        }

        private string CopyToBuildReportsCache(string originalPath)
        {
            try
            {
                if (!File.Exists(originalPath))
                    return null;

                // If the file is already in BuildReports directory, don't copy it - just return the original path
                if (originalPath.StartsWith(BUILD_REPORTS_DIR, StringComparison.OrdinalIgnoreCase))
                {
                    return originalPath;
                }

                // First, load the report to get the output path for naming
                byte[] reportData = File.ReadAllBytes(originalPath);
                string tempFile = Path.Combine(Path.GetTempPath(), "temp_buildreport.buildreport");
                File.WriteAllBytes(tempFile, reportData);

                // Create a temporary asset to read the BuildReport
                string tempAssetPath = Path.Combine(BUILD_REPORTS_DIR, "temp_for_naming.buildreport");
                File.WriteAllBytes(tempAssetPath, reportData);
                AssetDatabase.Refresh();

                var report = AssetDatabase.LoadAssetAtPath<BuildReport>(tempAssetPath);
                string targetFileName;

                if (report?.summary != null)
                {
                    // Generate filename from output path and timestamp
                    string outputPath = report.summary.outputPath;
                    string projectName = Path.GetFileNameWithoutExtension(outputPath);
                    if (string.IsNullOrEmpty(projectName))
                    {
                        projectName = "Unknown";
                    }

                    // Get timestamp from file modification time
                    var fileInfo = new FileInfo(originalPath);
                    string timestamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                    string platform = report.summary.platform.ToString();
                    
                    targetFileName = $"BuildReport_{projectName}_{platform}_{timestamp}.buildreport";
                }
                else
                {
                    // Fallback naming if we can't read the report
                    var fileInfo = new FileInfo(originalPath);
                    string timestamp = fileInfo.LastWriteTime.ToString("yyyy-MM-dd_HH-mm-ss");
                    targetFileName = $"BuildReport_{timestamp}.buildreport";
                }

                // Clean up temporary asset
                if (File.Exists(tempAssetPath))
                {
                    File.Delete(tempAssetPath);
                    AssetDatabase.Refresh();
                }

                // Copy to target location with generated name
                string targetPath = Path.Combine(BUILD_REPORTS_DIR, targetFileName);
                
                // Ensure unique filename
                int counter = 1;
                string baseFileName = Path.GetFileNameWithoutExtension(targetFileName);
                string extension = Path.GetExtension(targetFileName);
                
                while (File.Exists(targetPath))
                {
                    string newFileName = $"{baseFileName}_{counter}{extension}";
                    targetPath = Path.Combine(BUILD_REPORTS_DIR, newFileName);
                    counter++;
                }

                File.Copy(originalPath, targetPath);
                AssetDatabase.Refresh();
                
                Debug.Log($"Build report cached to: {targetPath}");
                return targetPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to copy build report to cache: {ex.Message}");
                return null;
            }
        }

        private void CleanupCurrentTempFile()
        {
            if (!string.IsNullOrEmpty(currentTempAssetPath) && File.Exists(currentTempAssetPath))
            {
                try
                {
                    File.Delete(currentTempAssetPath);
                    AssetDatabase.Refresh();
                    currentTempAssetPath = null;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to cleanup temp file: {ex.Message}");
                }
            }
        }

        private void OnFocus()
        {
            // Called when the window gains focus
            // Force a repaint to ensure smooth rendering after focus change
            Repaint();
            
            // Refresh TreeView to prevent laggy behavior
            if (treeView != null)
            {
                treeView.Repaint();
            }
        }

        private void OnLostFocus()
        {
            // Called when the window loses focus
            // Nothing needed here since the issue is Unity engine-level
        }

        private void RenderMessagePanel(Rect rect, BuildStepMessageEntry msgEntry)
        {
            // Draw background
            var bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // Title
            float titleHeight = EditorGUIUtility.singleLineHeight + 4;
            var titleRect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, "Message Details", EditorStyles.boldLabel);
            
            // Scroll area
            var scrollRect = new Rect(rect.x + 4, rect.y + titleHeight, rect.width - 8, rect.height - titleHeight - 4);
            
            // Estimate content height
            float contentW = scrollRect.width - 16;
            var style = EditorStyles.wordWrappedLabel;
            float contentH = style.CalcHeight(new GUIContent(msgEntry.Name), contentW);
            
            messageScrollPos = GUI.BeginScrollView(scrollRect, messageScrollPos, new Rect(0, 0, contentW, contentH));
            GUI.Label(new Rect(0, 0, contentW, contentH), msgEntry.Name, style);
            GUI.EndScrollView();
        }

        private void RenderFileInfoPanel(Rect rect, BuildFileEntry fileEntry)
        {
            // Draw background
            var bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // Title
            float titleHeight = EditorGUIUtility.singleLineHeight + 4;
            var titleRect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, "File Info", EditorStyles.boldLabel);
            
            // Content area
            var contentRect = new Rect(rect.x + 4, rect.y + titleHeight, rect.width - 8, rect.height - titleHeight - 4);
            
            // Calculate content height needed
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            int fieldCount = 0;
            
            // Count fields to show
            fieldCount++; // Name
            fieldCount++; // Size
            if (fileEntry.FileId.HasValue) fieldCount++; // File ID
            if (!string.IsNullOrEmpty(fileEntry.Role)) fieldCount++; // Role
            if (!string.IsNullOrEmpty(fileEntry.FullPath)) fieldCount++; // Full Path
            if (!string.IsNullOrEmpty(fileEntry.AssetPath)) fieldCount++; // Asset Path
            
            float contentHeight = fieldCount * lineHeight;
            
            // Render fields with scroll view if needed
            fileInfoScrollPos = GUI.BeginScrollView(contentRect, fileInfoScrollPos, new Rect(0, 0, contentRect.width - 16, contentHeight));
            
            float yPos = 0;
            const float labelWidth = 120f;
            
            // Name
            RenderInfoField("Name", fileEntry.Name, yPos, contentRect.width - 16, labelWidth);
            yPos += lineHeight;
            
            // Size
            string sizeText = FormatBytes(fileEntry.SizeBytes);
            RenderInfoField("Size", sizeText, yPos, contentRect.width - 16, labelWidth);
            yPos += lineHeight;
            
            // File ID (if available)
            if (fileEntry.FileId.HasValue)
            {
                RenderInfoField("File ID", fileEntry.FileId.Value.ToString(), yPos, contentRect.width - 16, labelWidth);
                yPos += lineHeight;
            }
            
            // Role (if available)
            if (!string.IsNullOrEmpty(fileEntry.Role))
            {
                RenderInfoField("Role", fileEntry.Role, yPos, contentRect.width - 16, labelWidth);
                yPos += lineHeight;
            }
            
            // Full Path (if available)
            if (!string.IsNullOrEmpty(fileEntry.FullPath))
            {
                RenderClickablePathField("Path", fileEntry.FullPath, yPos, contentRect.width - 16, labelWidth);
                yPos += lineHeight;
            }
            
            // Asset Path (if it's a Unity asset)
            if (!string.IsNullOrEmpty(fileEntry.AssetPath))
            {
                RenderClickablePathField("Asset Path", fileEntry.AssetPath, yPos, contentRect.width - 16, labelWidth);
                yPos += lineHeight;
            }
            
            GUI.EndScrollView();
        }
        
        private void RenderInfoField(string label, string value, float yPos, float width, float labelWidth)
        {
            var labelRect = new Rect(0, yPos, labelWidth, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(labelWidth, yPos, width - labelWidth, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.LabelField(labelRect, $"{label}:", EditorStyles.label);
            EditorGUI.LabelField(valueRect, value, EditorStyles.label);
        }
        
        private void RenderClickablePathField(string label, string path, float yPos, float width, float labelWidth)
        {
            var labelRect = new Rect(0, yPos, labelWidth, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(labelWidth, yPos, width - labelWidth, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.LabelField(labelRect, $"{label}:", EditorStyles.label);
            
            // Make the path clickable using linkLabel style
            if (GUI.Button(valueRect, path, EditorStyles.linkLabel))
            {
                OpenFileInExplorer(path);
            }
        }
        
        private void OpenFileInExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
                
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    // Show the file in explorer
                    EditorUtility.RevealInFinder(fullPath);
                }
                else
                {
                    // If file doesn't exist, try to show the directory
                    string directory = Path.GetDirectoryName(fullPath);
                    if (Directory.Exists(directory))
                    {
                        EditorUtility.RevealInFinder(directory);
                    }
                    else
                    {
                        Debug.LogWarning($"Path does not exist: {fullPath}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to open path in explorer: {path}. Error: {ex.Message}");
            }
        }

        #endregion
    }
}