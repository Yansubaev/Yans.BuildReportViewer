using UnityEditor.IMGUI.Controls;

namespace Yans.BuildAnalyser
{
    // Custom TreeViewItem to hold IBuildEntry reference
    public class BuildEntryTreeViewItem : TreeViewItem
    {
        public IBuildEntry buildEntry;

        public BuildEntryTreeViewItem(int id, int depth, string displayName, IBuildEntry buildEntry)
            : base(id, depth, displayName)
        {
            this.buildEntry = buildEntry;
        }
    }
}