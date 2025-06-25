using System.Collections.Generic;

namespace Yans.BuildAnalyser
{
    // Simple wrapper for arbitrary entry
    public class SimpleEntry : IBuildEntry
    {
        public string Name { get; set; }
        public ulong SizeBytes { get; set; }
        public IReadOnlyList<IBuildEntry> Children => _children;
        public bool IsDirectory => _children.Count > 0;
        public string AssetPath { get; private set; }

        private List<IBuildEntry> _children = new List<IBuildEntry>();

        public SimpleEntry(string name, ulong sizeBytes = 0)
        {
            Name = name;
            SizeBytes = sizeBytes;
        }

        public void AddChild(IBuildEntry child)
        {
            _children.Add(child);
        }
    }
}
