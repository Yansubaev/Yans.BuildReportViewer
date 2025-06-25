using System;
using System.Collections.Generic;

namespace Yans.BuildAnalyser
{
    // Wrapper for Scene reference
    public class SceneEntry : IBuildEntry
    {
        #region public properties
        public string Name { get; set; }
        public ulong SizeBytes { get; set; }
        public IReadOnlyList<IBuildEntry> Children => _emptyChildren;
        public bool IsDirectory => false;
        public string AssetPath { get; private set; }
        #endregion

        private static readonly List<IBuildEntry> _emptyChildren = new List<IBuildEntry>();

        public SceneEntry(string name, string scenePath = null)
        {
            Name = name;
            SizeBytes = 0;

            // Set AssetPath only if it's a valid Unity asset path
            if (!string.IsNullOrEmpty(scenePath) && scenePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                AssetPath = scenePath;
            }
            else
            {
                AssetPath = null;
            }
        }
    }
}