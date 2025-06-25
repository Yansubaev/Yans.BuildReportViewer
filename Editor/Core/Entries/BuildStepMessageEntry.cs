using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using UnityEngine;

namespace Yans.BuildAnalyser
{
    // Entry for a build step message
    public class BuildStepMessageEntry : IBuildEntry
    {
        public string Name { get; set; }
        public ulong SizeBytes { get; set; }
        public LogType LogType { get; set; }
        public IReadOnlyList<IBuildEntry> Children => _children;
        public bool IsDirectory => _children.Count > 0;
        public string AssetPath { get; private set; }

        private List<IBuildEntry> _children = new List<IBuildEntry>();

        public BuildStepMessageEntry(string name, LogType logType)
        {
            Name = name;
            LogType = logType;
            SizeBytes = 0;
        }

        public void AddChild(IBuildEntry child)
        {
            _children.Add(child);
        }
    }
}
