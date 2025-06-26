using UnityEditor.Build;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

namespace Yans.BuildAnalyser
{
    public class CustomBuildReportProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            BuildReportAnalyzer.AnalyzeBuildReport(report);
        }
    }

    public static class BuildReportAnalyzer
    {
        public const string BUILD_REPORTS_DIR = "Assets/BuildReports";
        
        public static void AnalyzeBuildReport(UnityEditor.Build.Reporting.BuildReport report)
        {
            Debug.Log($"Build completed. Total size: {report.summary.totalSize} bytes");
            Debug.Log($"Build files count: {report.files.Length}");
            Debug.Log($"Packed assets count: {report.packedAssets?.Length ?? 0}");
            Debug.Log($"Asset usage entries count: {report.scenesUsingAssets?.Length ?? 0}");
            
            // Save build report to Assets/BuildReports directory
            SaveBuildReportToAssets(report);
        }
        
        private static void SaveBuildReportToAssets(UnityEditor.Build.Reporting.BuildReport report)
        {
            try
            {
                // Ensure BuildReports directory exists
                if (!Directory.Exists(BUILD_REPORTS_DIR))
                {
                    Directory.CreateDirectory(BUILD_REPORTS_DIR);
                }
                
                // Generate filename from output path and timestamp
                string outputPath = report.summary.outputPath;
                string projectName = Path.GetFileNameWithoutExtension(outputPath);
                if (string.IsNullOrEmpty(projectName))
                {
                    projectName = "Unknown";
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string platform = report.summary.platform.ToString();
                string filename = $"BuildReport_{projectName}_{platform}_{timestamp}.buildreport";
                string reportPath = Path.Combine(BUILD_REPORTS_DIR, filename);
                
                // Copy from Library/LastBuild.buildreport to our directory
                string libraryReportPath = Path.Combine("Library", "LastBuild.buildreport");
                if (File.Exists(libraryReportPath))
                {
                    File.Copy(libraryReportPath, reportPath, true);
                    Debug.Log($"Build report saved to: {reportPath}");
                    
                    // Refresh the asset database so the file appears in Unity
                    UnityEditor.AssetDatabase.Refresh();
                    
                    // Trigger cleanup of old reports (keep last 10)
                    CleanupOldBuildReports();
                }
                else
                {
                    Debug.LogWarning($"Could not find build report at: {libraryReportPath}");
                }
                
                // Analyze the report
                ulong totalSize = 0;
                foreach (var file in report.files)
                {
                    totalSize += (ulong)file.size;
                }
                Debug.Log($"Analyzed {report.files.Length} files, total analyzed size: {totalSize} bytes");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save build report: {ex.Message}");
            }
        }

        public static void CleanupOldBuildReports(int maxReports = 10)
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

                UnityEditor.AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during build report cleanup: {ex.Message}");
            }
        }
    }
}