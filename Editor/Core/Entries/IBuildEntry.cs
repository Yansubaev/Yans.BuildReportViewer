using System.Collections.Generic;

namespace Yans.BuildAnalyser
{
    // Common interface for build entries
    public interface IBuildEntry
    {
        string Name { get; }
        ulong SizeBytes { get; set; }
        IReadOnlyList<IBuildEntry> Children { get; }
        bool IsDirectory { get; }
        string AssetPath { get; }
    }
}