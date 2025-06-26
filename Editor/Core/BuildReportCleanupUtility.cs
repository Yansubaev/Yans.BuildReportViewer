using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yans.BuildAnalyser
{
    public static class BuildReportCleanupUtility
    {
        [MenuItem("Tools/BuildReport Viewer/Open BuildReport Viewer")]
        public static void OpenBuildReportViewer()
        {
            BuildFilesHierarchyWindow.ShowWindow();
        }

        [MenuItem("Tools/BuildReport Viewer/Cleanup Old Files")]
        public static void CleanupOldFiles()
        {
            int cleanedFiles = 0;
            
            // Clean up old temp directory
            string oldTempDir = "Assets/BuildReport";
            if (Directory.Exists(oldTempDir))
            {
                try
                {
                    string[] files = Directory.GetFiles(oldTempDir, "*", SearchOption.AllDirectories);
                    cleanedFiles += files.Length;
                    
                    Directory.Delete(oldTempDir, true);
                    string metaFile = oldTempDir + ".meta";
                    if (File.Exists(metaFile))
                    {
                        File.Delete(metaFile);
                    }
                    
                    Debug.Log($"Cleaned up old BuildReport temp directory with {cleanedFiles} files");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to cleanup old temp directory: {ex.Message}");
                }
            }
            
            // Clean up any temporary build report files in BuildReports directory
            string buildReportsDir = "Assets/BuildReports";
            if (Directory.Exists(buildReportsDir))
            {
                try
                {
                    string[] tempFiles = Directory.GetFiles(buildReportsDir, "temp_*.buildreport", SearchOption.TopDirectoryOnly);
                    foreach (string tempFile in tempFiles)
                    {
                        File.Delete(tempFile);
                        cleanedFiles++;
                        Debug.Log($"Cleaned up temp file: {Path.GetFileName(tempFile)}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to cleanup temp files: {ex.Message}");
                }
            }
            
            AssetDatabase.Refresh();
            
            if (cleanedFiles > 0)
            {
                EditorUtility.DisplayDialog("Cleanup Complete", 
                    $"Cleaned up {cleanedFiles} old build report files.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Cleanup Complete", 
                    "No old files found to clean up.", "OK");
            }
        }
    }
}
