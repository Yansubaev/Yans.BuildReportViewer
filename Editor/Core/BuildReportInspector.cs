using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace Yans.BuildAnalyser
{
    [CustomEditor(typeof(BuildReport))]
    public class BuildReportInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            // Suppress Unity's default object name validation warnings
            serializedObject.Update();
            
            EditorGUILayout.Space(5);
            
            // Custom header
            EditorGUILayout.LabelField("Unity Build Report", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);

            // Add separator line
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            BuildReport buildReport = (BuildReport)target;

            if (buildReport != null)
            {
                // Show detailed build information
                var summary = buildReport.summary;
                
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Build Summary", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField($"Platform: {summary.platform}");
                EditorGUILayout.LabelField($"Result: {summary.result}");
                EditorGUILayout.LabelField($"Total Size: {FormatBytes(summary.totalSize)}");
                EditorGUILayout.LabelField($"Build Time: {summary.totalTime}");
                
                if (buildReport.files != null)
                {
                    EditorGUILayout.LabelField($"Files: {buildReport.files.Length}");
                }
                
                if (buildReport.packedAssets != null)
                {
                    EditorGUILayout.LabelField($"Packed Assets: {buildReport.packedAssets.Length}");
                }
                
                if (!string.IsNullOrEmpty(summary.outputPath))
                {
                    EditorGUILayout.LabelField("Output Path:");
                    EditorGUILayout.SelectableLabel(summary.outputPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(10);

                // Add separator line
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                // Custom section header
                EditorGUILayout.LabelField("Build Report Viewer", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);


                // Main button to open the BuildReport Viewer
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f, 1f); // Light blue background
                if (GUILayout.Button("Open in BuildReport Viewer", GUILayout.Height(30)))
                {
                    OpenInBuildReportViewer(buildReport);
                }
                GUI.backgroundColor = Color.white; // Reset background color

                EditorGUILayout.Space(5);

                // Additional utility buttons
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Reveal in Finder", EditorStyles.miniButton))
                {
                    string assetPath = AssetDatabase.GetAssetPath(buildReport);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        EditorUtility.RevealInFinder(assetPath);
                    }
                }

                if (summary.outputPath != null && GUILayout.Button("Show Build Output", EditorStyles.miniButton))
                {
                    EditorUtility.RevealInFinder(summary.outputPath);
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("BuildReport data is not available.", MessageType.Warning);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }
        
        public override bool UseDefaultMargins()
        {
            return true;
        }
        
        // Override to prevent Unity's default object name validation
        protected override void OnHeaderGUI()
        {
            // Custom header that doesn't show Unity's default object name validation
            EditorGUILayout.Space(5);
        }

        private void OpenInBuildReportViewer(BuildReport buildReport)
        {
            // Get the asset path of the selected BuildReport
            string assetPath = AssetDatabase.GetAssetPath(buildReport);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("Could not get asset path for BuildReport");
                return;
            }

            // Convert to absolute path
            string absolutePath = Path.GetFullPath(assetPath);

            // Open the BuildReport Viewer window with the specific report
            BuildFilesHierarchyWindow.ShowWindowWithReport(absolutePath);

            Debug.Log($"Opened BuildReport in viewer: {Path.GetFileName(assetPath)}");
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
    }
}
