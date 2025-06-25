using System;
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
        #endregion

        [MenuItem("Tools/BuildReport Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildFilesHierarchyWindow>();
            window.titleContent = new GUIContent("BuildReport Viewer");
            window.Show();
        }

        #region private methods

        private void OnEnable()
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            // Load saved display mode
            currentMode = (BuildDisplayMode)EditorPrefs.GetInt(PREF_DISPLAY_MODE, (int)BuildDisplayMode.Files);

            LoadBuildReport();
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
                LoadBuildReport();
                RefreshHierarchy();
            }

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(lastLoadedReportPath))
            {
                GUILayout.Label($"Loaded: {Path.GetFileName(lastLoadedReportPath)}", EditorStyles.toolbarButton);
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
                EditorGUILayout.LabelField("Result:", summary.result.ToString());
                EditorGUILayout.LabelField("Platform:", summary.platform.ToString());
                // Display build options flags
                EditorGUILayout.LabelField("Options:", summary.options.ToString());
                // Clickable output path
                if (GUILayout.Button(summary.outputPath, EditorStyles.linkLabel))
                {
                    EditorUtility.RevealInFinder(summary.outputPath);
                }
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
                    EditorGUILayout.LabelField("Output Size:", $"{outBytes} ({outHuman})");
                }
                // Total size with human-readable format
                {
                    ulong totalBytes = summary.totalSize;
                    string human = FormatBytes(totalBytes);
                    EditorGUILayout.LabelField("Total Size (bytes):", $"{totalBytes} ({human})");
                }
                EditorGUILayout.LabelField("Total Time:", summary.totalTime.ToString());
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
                float messageHeight = showMessage ? Mathf.Min(200, totalHeight * 0.3f) : 0;
                float treeHeight = totalHeight - messageHeight;
                var treeRect = new Rect(0, contentStartY, position.width, treeHeight);
                treeView.OnGUI(treeRect);
                if (showMessage && treeView.SelectedEntry is BuildStepMessageEntry msgEntry)
                {
                    var msgRect = new Rect(0, contentStartY + treeHeight, position.width, messageHeight);
                    // Draw background
                    var bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
                    EditorGUI.DrawRect(msgRect, bgColor);
                    // Title
                    float titleH = EditorGUIUtility.singleLineHeight + 4;
                    var titleRect = new Rect(msgRect.x + 4, msgRect.y + 2, msgRect.width - 8, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(titleRect, "Message Details", EditorStyles.boldLabel);
                    // Scroll area
                    var scrollRect = new Rect(msgRect.x + 4, msgRect.y + titleH, msgRect.width - 8, msgRect.height - titleH - 4);
                    // Estimate content height
                    float contentW = scrollRect.width - 16;
                    var style = EditorStyles.wordWrappedLabel;
                    float contentH = style.CalcHeight(new GUIContent(msgEntry.Name), contentW);
                    messageScrollPos = GUI.BeginScrollView(scrollRect, messageScrollPos, new Rect(0, 0, contentW, contentH));
                    GUI.Label(new Rect(0, 0, contentW, contentH), msgEntry.Name, style);
                    GUI.EndScrollView();
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
            string defaultPath = Path.Combine("Library", "LastBuild.buildreport");
            LoadBuildReportFromPath(defaultPath);
        }

        private void LoadBuildReportFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Select Build Report", "Library", "buildreport");
            if (!string.IsNullOrEmpty(path))
            {
                LoadBuildReportFromPath(path);
            }
        }

        private void LoadBuildReportFromPath(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"Build report not found at: {path}");
                    rootEntry = new BuildFileEntry("Empty", 0);
                    lastLoadedReportPath = null;
                    currentReport = null;
                    return;
                }

                byte[] reportData = File.ReadAllBytes(path);

                // Ensure BuildReportView directory exists and update temp asset path
                string reportDir = "Assets/BuildReport/Reports";
                if (!Directory.Exists(reportDir))
                    Directory.CreateDirectory(reportDir);
                var tempAssetPath = reportDir + "/temp_buildreport.buildreport";

                File.WriteAllBytes(tempAssetPath, reportData);
                AssetDatabase.Refresh();

                var report = AssetDatabase.LoadAssetAtPath<BuildReport>(tempAssetPath);

                if (report != null)
                {
                    currentReport = report;
                    lastLoadedReportPath = path;
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
                }

                if (File.Exists(tempAssetPath))
                {
                    // Preserve temp asset so the loaded BuildReport reference stays valid
                    // File.Delete(tempAssetPath);
                    // AssetDatabase.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load build report: {ex.Message}\n{ex.StackTrace}");
                rootEntry = new BuildFileEntry("Empty", 0);
                lastLoadedReportPath = null;
                currentReport = null;
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

        #endregion
    }
}