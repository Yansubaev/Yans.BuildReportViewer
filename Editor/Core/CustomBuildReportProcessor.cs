using UnityEditor.Build;
using UnityEngine;
using System.IO;

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
        public static void AnalyzeBuildReport(UnityEditor.Build.Reporting.BuildReport report)
        {
            Debug.Log($"Build completed. Total size: {report.summary.totalSize} bytes");
            Debug.Log($"Build files count: {report.files.Length}");
            Debug.Log($"Packed assets count: {report.packedAssets?.Length ?? 0}");
            Debug.Log($"Asset usage entries count: {report.scenesUsingAssets?.Length ?? 0}");
            
            // Save build report for later analysis
            string reportPath = Path.Combine("Library", "LastBuild.buildreport");
            try
            {
                // The build report is automatically saved by Unity, we just log the analysis
                ulong totalSize = 0;
                foreach (var file in report.files)
                {
                    totalSize += (ulong)file.size;
                }
                Debug.Log($"Analyzed {report.files.Length} files, total analyzed size: {totalSize} bytes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to analyze build report: {ex.Message}");
            }
        }
    }
}