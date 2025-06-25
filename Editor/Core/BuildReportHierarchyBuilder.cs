using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;

namespace Yans.BuildAnalyser
{
    // Generic hierarchy builder for build reports
    public static class BuildReportHierarchyBuilder
    {
        #region public methods

        public static IBuildEntry BuildFromFiles(BuildReport report)
        {
            var root = new BuildFileEntry("Build Files", 0);

            foreach (var buildFile in report.files)
            {
                string filePath = buildFile.path.Replace('\\', '/');
                ulong fileSize = (ulong)buildFile.size;

                string normalizedPath = NormalizePath(filePath);
                string[] parts = normalizedPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                IBuildEntry current = root;
                string cumulativePath = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    cumulativePath = string.IsNullOrEmpty(cumulativePath) ? parts[i] : cumulativePath + "/" + parts[i];

                    var currentDir = (BuildFileEntry)current;
                    var child = currentDir.Children.FirstOrDefault(n => n.Name == parts[i]);

                    if (child == null)
                    {
                        // For the final file, pass the original file path; for directories, pass null
                        string childPath = (i == parts.Length - 1) ? buildFile.path : null;
                        child = new BuildFileEntry(parts[i], 0, childPath);
                        currentDir.AddChild(child);
                    }

                    if (i == parts.Length - 1)
                    {
                        child.SizeBytes += fileSize;
                    }

                    current = child;
                }
            }

            AggregateSizes(root);
            return root;
        }

        public static IBuildEntry BuildFromPackedAssets(BuildReport report)
        {
            var root = new PackedAssetEntry("Packed Assets", 0);

            if (report.packedAssets == null || report.packedAssets.Length == 0)
            {
                return root;
            }

            foreach (var packedAsset in report.packedAssets)
            {
                string assetName = !string.IsNullOrEmpty(packedAsset.shortPath)
                    ? packedAsset.shortPath
                    : packedAsset.name;

                if (string.IsNullOrEmpty(assetName))
                    assetName = "Unknown Asset";

                var assetEntry = new PackedAssetEntry(assetName, (ulong)packedAsset.overhead, packedAsset.shortPath);

                // Add contents as children
                if (packedAsset.contents != null)
                {
                    foreach (var content in packedAsset.contents)
                    {
                        string contentPath = content.sourceAssetPath.Replace('\\', '/');
                        string normalizedPath = NormalizePath(contentPath);

                        string[] parts = normalizedPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length > 0)
                        {
                            IBuildEntry current = assetEntry;

                            for (int i = 0; i < parts.Length; i++)
                            {
                                var currentDir = (PackedAssetEntry)current;
                                var child = currentDir.Children.FirstOrDefault(n => n.Name == parts[i]);

                                if (child == null)
                                {
                                    // For the final file, pass the source asset path; for directories, pass null
                                    string childPath = (i == parts.Length - 1) ? content.sourceAssetPath : null;
                                    child = new PackedAssetEntry(parts[i], 0, childPath);
                                    currentDir.AddChild(child);
                                }

                                if (i == parts.Length - 1)
                                {
                                    child.SizeBytes += (ulong)content.packedSize;
                                }

                                current = child;
                            }
                        }
                    }
                }

                ((PackedAssetEntry)root).AddChild(assetEntry);
            }

            AggregateSizes(root);
            return root;
        }

        public static IBuildEntry BuildFromAssetUsage(BuildReport report)
        {
            var root = new AssetUsageEntry("Asset Usage", 0);

            if (report.scenesUsingAssets == null || report.scenesUsingAssets.Length == 0)
            {
                return root;
            }

            // Create dictionary of build file sizes for quick lookup
            var buildFileSizes = new Dictionary<string, ulong>();
            foreach (var buildFile in report.files)
            {
                string normalizedPath = buildFile.path.Replace('\\', '/');
                buildFileSizes[normalizedPath] = (ulong)buildFile.size;
            }

            // Iterate through all ScenesUsingAssets objects
            foreach (var scenesUsingAssetsObj in report.scenesUsingAssets)
            {
                if (scenesUsingAssetsObj?.list == null) continue;

                // Iterate through the list of ScenesUsingAsset in each object
                foreach (var usage in scenesUsingAssetsObj.list)
                {
                    string assetPath = usage.assetPath;
                    string assetName = Path.GetFileName(assetPath);

                    if (string.IsNullOrEmpty(assetName))
                        assetName = "Unknown Asset";

                    // Get asset size from build files
                    ulong assetSize = 0;
                    string normalizedAssetPath = assetPath.Replace('\\', '/');
                    buildFileSizes.TryGetValue(normalizedAssetPath, out assetSize);

                    var assetEntry = new AssetUsageEntry(assetName, assetSize, assetPath);

                    // Add scenes as children
                    if (usage.scenePaths != null)
                    {
                        foreach (var scenePath in usage.scenePaths)
                        {
                            string sceneName = Path.GetFileName(scenePath);
                            if (string.IsNullOrEmpty(sceneName))
                                sceneName = "Unknown Scene";

                            var sceneEntry = new SceneEntry(sceneName, scenePath);
                            assetEntry.AddChild(sceneEntry);
                        }
                    }

                    ((AssetUsageEntry)root).AddChild(assetEntry);
                }
            }

            AggregateSizes(root);
            return root;
        }

        public static IBuildEntry BuildFromSteps(BuildReport report)
        {
            var root = new SimpleEntry("Build Steps");
            if (report.steps == null) return root;

            // Track last entry at each depth
            var lastEntries = new Dictionary<int, IBuildEntry>();

            foreach (var step in report.steps)
            {
                var stepEntry = new BuildStepEntry(step.name, step.duration, step.depth);

                // Add messages as children
                if (step.messages != null)
                {
                    foreach (var msg in step.messages)
                    {
                        var msgEntry = new BuildStepMessageEntry(msg.content, msg.type);
                        stepEntry.AddChild(msgEntry);
                    }
                }

                // Determine parent based on depth
                if (step.depth == 0)
                {
                    root.AddChild(stepEntry);
                }
                else if (lastEntries.TryGetValue(step.depth - 1, out var parentEntry))
                {
                    if (parentEntry is SimpleEntry simpleParent)
                        simpleParent.AddChild(stepEntry);
                    else if (parentEntry is BuildStepEntry stepParent)
                        stepParent.AddChild(stepEntry);
                }
                else
                {
                    // Fallback to root
                    root.AddChild(stepEntry);
                }

                // Update last entry for this depth
                lastEntries[step.depth] = stepEntry;
            }

            return root;
        }

        public static IBuildEntry BuildFromStrippingInfo(BuildReport report)
        {
            var root = new SimpleEntry("Stripping Info");
            var info = report.strippingInfo;
            if (info == null) return root;
            // For each included module, add as child and recursively list reasons
            foreach (var module in info.includedModules)
            {
                var moduleEntry = new SimpleEntry(module);
                // Recursively add reason entries
                var visited = new HashSet<string>(StringComparer.Ordinal);
                AddReasonEntries(moduleEntry, info, module, visited);
                root.AddChild(moduleEntry);
            }
            return root;
        }

        // Helper to add reasons recursively under entry, avoiding cycles
        private static void AddReasonEntries(SimpleEntry parent, UnityEditor.Build.Reporting.StrippingInfo info, string key, HashSet<string> visited)
        {
            if (!visited.Add(key)) return;
            foreach (var reason in info.GetReasonsForIncluding(key))
            {
                var reasonEntry = new SimpleEntry(reason);
                parent.AddChild(reasonEntry);
                AddReasonEntries(reasonEntry, info, reason, visited);
            }
        }

        #endregion

        #region private methods

        private static string NormalizePath(string path)
        {
            if (path.Length > 3 && path[1] == ':' && path[2] == '/')
            {
                path = path.Substring(3);
            }

            string[] commonPrefixes = {
                "Users/",
                "Program Files/",
                "Program Files (x86)/",
                "Unity/Hub/Editor/",
                "Applications/Unity/Hub/Editor/"
            };

            foreach (var prefix in commonPrefixes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    int unityIndex = path.IndexOf("Unity/", StringComparison.OrdinalIgnoreCase);
                    if (unityIndex >= 0)
                    {
                        path = path.Substring(unityIndex);
                        break;
                    }
                }
            }

            int assetsIndex = path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return path.Substring(assetsIndex);
            }

            string[] projectFolders = { "Library/", "Packages/", "ProjectSettings/" };
            foreach (var folder in projectFolders)
            {
                int folderIndex = path.IndexOf(folder, StringComparison.OrdinalIgnoreCase);
                if (folderIndex >= 0)
                {
                    return path.Substring(folderIndex);
                }
            }

            string[] parts = path.Split('/');
            if (parts.Length > 5)
            {
                return string.Join("/", parts.Skip(parts.Length - 5));
            }

            return path;
        }

        private static ulong AggregateSizes(IBuildEntry entry)
        {
            ulong totalSize = entry.SizeBytes;

            foreach (var child in entry.Children)
            {
                totalSize += AggregateSizes(child);
            }

            entry.SizeBytes = totalSize;
            return totalSize;
        }

        #endregion
    }
}