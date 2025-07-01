using System;
using UnityEditor;
using UnityEngine;

namespace Yans.BuildAnalyser
{
    /// <summary>
    /// Reusable component for rendering info panels with consistent styling.
    /// Used for file details, message details, and other information panels.
    /// </summary>
    public static class InfoPanelRenderer
    {
        private const float LABEL_WIDTH = 120f;
        
        /// <summary>
        /// Renders a message details panel with scrollable text content.
        /// </summary>
        /// <param name="rect">The rectangle to draw the panel in</param>
        /// <param name="title">The title of the panel</param>
        /// <param name="message">The message content to display</param>
        /// <param name="scrollPosition">Current scroll position</param>
        /// <returns>Updated scroll position</returns>
        public static Vector2 RenderMessagePanel(Rect rect, string title, string message, Vector2 scrollPosition)
        {
            // Draw background
            var bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // Title
            float titleHeight = EditorGUIUtility.singleLineHeight + 4;
            var titleRect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
            
            // Scroll area
            var scrollRect = new Rect(rect.x + 4, rect.y + titleHeight, rect.width - 8, rect.height - titleHeight - 4);
            
            // Estimate content height
            float contentW = scrollRect.width - 16;
            var style = EditorStyles.wordWrappedLabel;
            float contentH = style.CalcHeight(new GUIContent(message), contentW);
            
            scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, new Rect(0, 0, contentW, contentH));
            GUI.Label(new Rect(0, 0, contentW, contentH), message, style);
            GUI.EndScrollView();
            
            return scrollPosition;
        }
        
        /// <summary>
        /// Renders a field-based info panel with key-value pairs.
        /// </summary>
        /// <param name="rect">The rectangle to draw the panel in</param>
        /// <param name="title">The title of the panel</param>
        /// <param name="renderFields">Action to render the fields content</param>
        /// <param name="scrollPosition">Current scroll position</param>
        /// <param name="contentHeight">Total height of the content</param>
        /// <returns>Updated scroll position</returns>
        public static Vector2 RenderFieldsPanel(Rect rect, string title, System.Action<float> renderFields, Vector2 scrollPosition, float contentHeight)
        {
            // Draw background
            var bgColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.76f, 0.76f, 0.76f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            
            // Title
            float titleHeight = EditorGUIUtility.singleLineHeight + 4;
            var titleRect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, title, EditorStyles.boldLabel);
            
            // Content area
            var contentRect = new Rect(rect.x + 4, rect.y + titleHeight, rect.width - 8, rect.height - titleHeight - 4);
            
            // Render fields with scroll view if needed
            scrollPosition = GUI.BeginScrollView(contentRect, scrollPosition, new Rect(0, 0, contentRect.width - 16, contentHeight));
            
            renderFields(contentRect.width - 16);
            
            GUI.EndScrollView();
            
            return scrollPosition;
        }
        
        /// <summary>
        /// Renders a single field using the consistent two-column layout.
        /// </summary>
        /// <param name="label">The field label</param>
        /// <param name="value">The field value</param>
        /// <param name="yPos">Y position for the field</param>
        /// <param name="width">Total width available</param>
        /// <param name="labelWidth">Width of the label column (optional, uses default if not specified)</param>
        public static void RenderField(string label, string value, float yPos, float width, float? labelWidth = null)
        {
            float actualLabelWidth = labelWidth ?? LABEL_WIDTH;
            var labelRect = new Rect(0, yPos, actualLabelWidth, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(actualLabelWidth, yPos, width - actualLabelWidth, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.LabelField(labelRect, $"{label}:", EditorStyles.label);
            EditorGUI.LabelField(valueRect, value, EditorStyles.label);
        }
        
        /// <summary>
        /// Formats bytes to human-readable format (e.g., "80530473 bytes (76.8 MB)")
        /// </summary>
        public static string FormatBytes(ulong bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int i = 0;
            while (i < suffixes.Length - 1 && size >= 1024.0)
            {
                size /= 1024.0;
                i++;
            }
            string humanReadable = string.Format("{0:0.##} {1}", size, suffixes[i]);
            return $"{bytes:N0} bytes ({humanReadable})";
        }
    }
}
