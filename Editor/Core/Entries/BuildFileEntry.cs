using System;
using System.Collections.Generic;

namespace Yans.BuildAnalyser
{
    // Wrapper for BuildFile
    public class BuildFileEntry : IBuildEntry
    {
        #region public properties
        public string Name { get; set; }
        public ulong SizeBytes { get; set; }
        public IReadOnlyList<IBuildEntry> Children => _children;
        public bool IsDirectory => _children.Count > 0;
        public string AssetPath { get; private set; }
        
        // Additional BuildFile information for detailed view
        public uint? FileId { get; private set; }
        public string Role { get; private set; }
        public string FullPath { get; private set; }
        #endregion

        private List<IBuildEntry> _children = new List<IBuildEntry>();

        public BuildFileEntry(string name, ulong sizeBytes, string filePath = null, uint? fileId = null, string role = null)
        {
            Name = name;
            SizeBytes = sizeBytes;
            FileId = fileId;
            Role = role;
            FullPath = filePath;

            // Set AssetPath only if it's a valid Unity asset path
            if (!string.IsNullOrEmpty(filePath) && filePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                AssetPath = filePath;
            }
            else
            {
                AssetPath = null;
            }
        }

        public void AddChild(IBuildEntry child)
        {
            _children.Add(child);
        }
    }
}