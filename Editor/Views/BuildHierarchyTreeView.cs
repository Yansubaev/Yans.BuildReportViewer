using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System;
using Yans.BuildAnalyser;

namespace Yans.BuildAnalyser
{
    // Custom TreeView with three columns for IBuildEntry
    public class BuildHierarchyTreeView : TreeView
    {
        private BuildDisplayMode displayMode;
        #region public fields
        public IBuildEntry rootEntry;
        public ulong totalSize;
        // Currently selected build entry
        public IBuildEntry SelectedEntry { get; private set; }
        #endregion
        #region private fields
        private bool sortBySize = true;
        private bool sortDescending = true;
        #endregion

        public BuildHierarchyTreeView(TreeViewState state, IBuildEntry rootEntry, BuildDisplayMode mode) : base(state)
        {
            this.displayMode = mode;
            this.rootEntry = rootEntry;
            this.totalSize = rootEntry.SizeBytes;
            showAlternatingRowBackgrounds = true;
            useScrollView = true;

            var headerState = CreateHeaderState(displayMode);
            multiColumnHeader = new MultiColumnHeader(headerState);
            multiColumnHeader.sortingChanged += OnSortingChanged;

            Reload();
        }

        private static MultiColumnHeaderState CreateHeaderState(BuildDisplayMode mode)
        {
            MultiColumnHeaderState.Column[] columns = mode switch
            {
                BuildDisplayMode.BuildSteps => new[]
                                    {
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Name"), width = 300, minWidth = 200, autoResize = true, allowToggleVisibility = false },
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Duration"), width = 100, minWidth = 80, autoResize = false, allowToggleVisibility = false },
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Depth"), width = 60, minWidth = 40, autoResize = false, allowToggleVisibility = false }
                    },
                BuildDisplayMode.StrippingInfo => new[]
                    {
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Name"), width = 600, minWidth = 300, autoResize = true, allowToggleVisibility = false },
                    },
                _ => new[]
                    {
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Name"), width = 300, minWidth = 200, autoResize = true, allowToggleVisibility = false },
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Size (MB)"), width = 100, minWidth = 80, autoResize = false, allowToggleVisibility = false },
                        new MultiColumnHeaderState.Column { headerContent = new GUIContent("Share %"), width = 80, minWidth = 60, autoResize = false, allowToggleVisibility = false }
                    },
            };
            return new MultiColumnHeaderState(columns);
        }

        #region protected methods

        protected override TreeViewItem BuildRoot()
        {
            var rootItem = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            int id = 1;

            if (rootEntry != null && rootEntry.Children.Count > 0)
            {
                AddTreeViewItems(rootEntry, rootItem, ref id);
            }
            else
            {
                // Create an empty child to satisfy TreeView requirements
                var emptyItem = new TreeViewItem { id = 1, depth = 0, displayName = "No data" };
                rootItem.AddChild(emptyItem);
            }

            SetupDepthsFromParentsAndChildren(rootItem);
            return rootItem;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            // Reset and store selected build entry
            SelectedEntry = null;
            if (selectedIds.Count > 0)
            {
                var selectedItem = FindItem(selectedIds[0], rootItem) as BuildEntryTreeViewItem;
                if (selectedItem is BuildEntryTreeViewItem entryItem)
                {
                    SelectedEntry = entryItem.buildEntry;
                    var entry = entryItem.buildEntry;

                    if (!string.IsNullOrEmpty(entry.AssetPath))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.AssetPath);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                        }
                    }
                }
            }
        }

        // Add context-click menu for 'Show in Explorer' in Files mode
        protected override void ContextClickedItem(int id)
        {
            if (displayMode != BuildDisplayMode.Files)
            {
                base.ContextClickedItem(id);
                return;
            }
            var item = FindItem(id, rootItem) as BuildEntryTreeViewItem;
            var menu = new GenericMenu();
            if (item != null)
            {
                // Check for any valid path - either AssetPath (Unity assets) or FullPath (build output files)
                string pathToShow = null;
                if (!string.IsNullOrEmpty(item.buildEntry.AssetPath))
                {
                    pathToShow = item.buildEntry.AssetPath;
                }
                else if (item.buildEntry is BuildFileEntry fileEntry && !string.IsNullOrEmpty(fileEntry.FullPath))
                {
                    pathToShow = fileEntry.FullPath;
                }

                if (!string.IsNullOrEmpty(pathToShow))
                {
                    menu.AddItem(new GUIContent("Show in Explorer"), false, () =>
                    {
                        string fullPath = Path.GetFullPath(pathToShow);
                        if (File.Exists(fullPath))
                        {
                            // Show the file in explorer
                            EditorUtility.RevealInFinder(fullPath);
                        }
                        else
                        {
                            // If file doesn't exist, try to show the directory
                            string directory = Path.GetDirectoryName(fullPath);
                            if (Directory.Exists(directory))
                                EditorUtility.RevealInFinder(directory);
                        }
                    });
                }
                else
                {
                    // No valid path
                    menu.AddDisabledItem(new GUIContent("Show in Explorer"));
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Show in Explorer"));
            }
            menu.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            if (displayMode != BuildDisplayMode.Files)
                return;
            var item = FindItem(id, rootItem) as BuildEntryTreeViewItem;
            if (item != null)
            {
                // Check for any valid path - either AssetPath (Unity assets) or FullPath (build output files)
                string pathToShow = null;
                if (!string.IsNullOrEmpty(item.buildEntry.AssetPath))
                {
                    pathToShow = item.buildEntry.AssetPath;
                }
                else if (item.buildEntry is BuildFileEntry fileEntry && !string.IsNullOrEmpty(fileEntry.FullPath))
                {
                    pathToShow = fileEntry.FullPath;
                }

                if (!string.IsNullOrEmpty(pathToShow))
                {
                    string fullPath = Path.GetFullPath(pathToShow);
                    if (File.Exists(fullPath))
                    {
                        // Show the file in explorer
                        EditorUtility.RevealInFinder(fullPath);
                    }
                    else
                    {
                        // If file doesn't exist, try to show the directory
                        string directory = Path.GetDirectoryName(fullPath);
                        if (Directory.Exists(directory))
                            EditorUtility.RevealInFinder(directory);
                    }
                }
            }
        }

        // ...existing RowGUI...
        protected override void RowGUI(RowGUIArgs args)
        {
            // Trim name to first line
            var entryItem = (BuildEntryTreeViewItem)args.item;
            IBuildEntry entry = entryItem.buildEntry;
            string rawName = entry.Name ?? string.Empty;
            string[] lines = rawName.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string displayName = lines.Length > 0 ? lines[0] : rawName;

            for (int i = 0; i < args.GetNumVisibleColumns(); i++)
            {
                var cellRect = args.GetCellRect(i);
                int colIndex = args.GetColumn(i);
                if (displayMode == BuildDisplayMode.BuildSteps)
                {
                    switch (colIndex)
                    {
                        case 0:
                            // Draw step or message with icon and color
                            float indent = GetContentIndent(args.item);
                            var iconRect = new Rect(cellRect.x + indent, cellRect.y, 16, cellRect.height);
                            var labelRect = new Rect(cellRect.x + indent + 18, cellRect.y, cellRect.width - indent - 18, cellRect.height);
                            if (entry is BuildStepMessageEntry msgEntry)
                            {
                                // Choose icon and color based on LogType
                                GUIContent iconContent;
                                Color textColor;
                                switch (msgEntry.LogType)
                                {
                                    case LogType.Error:
                                    case LogType.Exception:
                                        iconContent = EditorGUIUtility.IconContent("console.erroricon");
                                        textColor = Color.red;
                                        break;
                                    case LogType.Warning:
                                        iconContent = EditorGUIUtility.IconContent("console.warnicon");
                                        textColor = Color.yellow;
                                        break;
                                    default:
                                        iconContent = EditorGUIUtility.IconContent("console.infoicon");
                                        textColor = GUI.contentColor;
                                        break;
                                }
                                GUI.DrawTexture(iconRect, (Texture)iconContent.image);
                                var originalColor = GUI.contentColor;
                                GUI.contentColor = textColor;
                                EditorGUI.LabelField(labelRect, displayName);
                                GUI.contentColor = originalColor;
                            }
                            else
                            {
                                // Regular step entry
                                EditorGUI.LabelField(labelRect, displayName);
                            }
                            break;
                        case 1:
                            // Reflectively get Duration property
                            var durProp = entry.GetType().GetProperty("Duration");
                            if (durProp != null)
                            {
                                var durVal = durProp.GetValue(entry);
                                EditorGUI.LabelField(cellRect, durVal != null ? durVal.ToString() : string.Empty);
                            }
                            break;
                        case 2:
                            // Reflectively get Depth property
                            var depthProp = entry.GetType().GetProperty("Depth");
                            if (depthProp != null)
                            {
                                var depthVal = depthProp.GetValue(entry);
                                EditorGUI.LabelField(cellRect, depthVal != null ? depthVal.ToString() : string.Empty);
                            }
                            break;
                    }
                    continue; // skip default handling for other columns
                }

                switch (colIndex)
                {
                    case 0: // Name column
                        float indent = GetContentIndent(args.item);
                        var iconRect = new Rect(cellRect.x + indent, cellRect.y, 16, cellRect.height);
                        var labelRect = new Rect(cellRect.x + indent + 18, cellRect.y, cellRect.width - indent - 18, cellRect.height);
                        var icon = args.item.icon ?? (Texture2D)(entry.IsDirectory ? EditorGUIUtility.IconContent("Folder Icon").image : EditorGUIUtility.IconContent("TextAsset Icon").image);
                        GUI.DrawTexture(iconRect, icon);
                        EditorGUI.LabelField(labelRect, displayName);
                        break;
                    case 1: // Size (MB)
                        float sizeMB = entry.SizeBytes / (1024f * 1024f);
                        var origColor = GUI.contentColor;
                        if (sizeMB > 5f) GUI.contentColor = Color.red;
                        EditorGUI.LabelField(cellRect, sizeMB.ToString("F2"));
                        GUI.contentColor = origColor;
                        break;
                    case 2: // Share %
                        float share = totalSize > 0 ? (entry.SizeBytes / (float)totalSize) * 100 : 0;
                        EditorGUI.LabelField(cellRect, share.ToString("F1") + "%");
                        break;
                }
            }
        }

        #endregion

        #region private methods

        private void AddTreeViewItems(IBuildEntry entry, TreeViewItem parent, ref int id)
        {
            var sortedChildren = sortBySize
                ? (sortDescending ? entry.Children.OrderByDescending(n => n.SizeBytes) : entry.Children.OrderBy(n => n.SizeBytes))
                : (sortDescending ? entry.Children.OrderByDescending(n => n.Name) : entry.Children.OrderBy(n => n.Name));

            foreach (var child in sortedChildren)
            {
                var item = new BuildEntryTreeViewItem(id++, parent.depth + 1, child.Name, child);

                // Set icon based on asset path and type
                if (!string.IsNullOrEmpty(child.AssetPath))
                {
                    var cachedIcon = AssetDatabase.GetCachedIcon(child.AssetPath) as Texture2D;
                    if (cachedIcon != null)
                    {
                        item.icon = cachedIcon;
                    }
                }
                else if (child.IsDirectory)
                {
                    item.icon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                }

                parent.AddChild(item);

                if (child.Children.Count > 0)
                {
                    AddTreeViewItems(child, item, ref id);
                }
            }
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            var sortedColumnIndex = header.sortedColumnIndex;
            sortDescending = header.IsSortedAscending(sortedColumnIndex) == false;

            switch (sortedColumnIndex)
            {
                case 0: // Name
                    sortBySize = false;
                    break;
                case 1: // Size
                case 2: // Share
                    sortBySize = true;
                    break;
            }

            Reload();
        }

        #endregion
    }
}