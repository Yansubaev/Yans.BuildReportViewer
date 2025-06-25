using System;
using System.Collections.Generic;

namespace Yans.BuildAnalyser
{
    // Entry for a build step with duration and depth
    public class BuildStepEntry : IBuildEntry
    {
        public string Name { get; set; }
        public ulong SizeBytes { get; set; }
        public TimeSpan Duration { get; set; }
        public int Depth { get; set; }
        public IReadOnlyList<IBuildEntry> Children => _children;
        public bool IsDirectory => _children.Count > 0;
        public string AssetPath { get; private set; }

        private List<IBuildEntry> _children = new List<IBuildEntry>();

        public BuildStepEntry(string name, TimeSpan duration, int depth)
        {
            Name = name;
            Duration = duration;
            Depth = depth;
            SizeBytes = 0;
        }

        public void AddChild(IBuildEntry child)
        {
            _children.Add(child);
        }
    }
}
