#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_EUI
    {
        public static float ReferenceWidth = 760f;
        public static float MinimumScale = 0.76f;
        public static float MaximumScale = 1.00f;
        public static float CompactBreakpoint = 780f;

        private static GUIStyle sectionTitleStyle;
        private static GUIStyle cardTitleStyle;
        private static GUIStyle cardSubtitleStyle;
        private static GUIStyle statusPillStyle;
        private static GUIStyle richMiniLabelStyle;

        public static bool IsCompact(float windowWidth)
        {
            return windowWidth <= CompactBreakpoint;
        }

        public static float GetContentWidth(float windowWidth, float horizontalPadding = 18f)
        {
            return Mathf.Max(120f, windowWidth - Mathf.Max(0f, horizontalPadding));
        }

        public static ScaleScope Auto(float windowWidth)
        {
            return new ScaleScope(windowWidth);
        }

        public static ResponsiveRow Row(float contentWidth)
        {
            return new ResponsiveRow(contentWidth);
        }

        public static CardScope Card(string title)
        {
            return new CardScope(title, null);
        }

        public static CardScope Card(string title, string subtitle)
        {
            return new CardScope(title, subtitle);
        }

        public static void SectionTitle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            EnsureStyles();
            GUILayout.Space(8f);
            GUILayout.Label(text, sectionTitleStyle);
        }

        public static void Divider(float topSpace = 6f, float bottomSpace = 6f)
        {
            GUILayout.Space(topSpace);
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.12f));
            GUILayout.Space(bottomSpace);
        }

        public static void DrawKeyValue(string key, string value, float keyWidth = 176f)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key ?? string.Empty, GUILayout.Width(keyWidth));
                EditorGUILayout.SelectableLabel(value ?? string.Empty, EditorStyles.label, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        public static void DrawMiniNote(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            EnsureStyles();
            GUILayout.Label(text, richMiniLabelStyle);
        }

        public static void DrawStatusPill(string text, MessageType type)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            EnsureStyles();
            GUIContent content = new GUIContent(text);
            Vector2 size = statusPillStyle.CalcSize(content);
            Rect rect = GUILayoutUtility.GetRect(size.x + 12f, 22f, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, GetMessageColor(type));
            GUI.Label(rect, content, statusPillStyle);
        }

        private static void EnsureStyles()
        {
            if (sectionTitleStyle == null)
            {
                sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    wordWrap = true
                };
            }

            if (cardTitleStyle == null)
            {
                cardTitleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    wordWrap = true
                };
            }

            if (cardSubtitleStyle == null)
            {
                cardSubtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    richText = true
                };
            }

            if (statusPillStyle == null)
            {
                statusPillStyle = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(8, 8, 3, 3),
                    fixedHeight = 22f,
                    stretchWidth = false
                };
            }

            if (richMiniLabelStyle == null)
            {
                richMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    richText = true,
                    wordWrap = true
                };
            }
        }

        private static Color GetMessageColor(MessageType type)
        {
            switch (type)
            {
                case MessageType.Warning:
                    return new Color(0.88f, 0.64f, 0.16f, 0.95f);
                case MessageType.Error:
                    return new Color(0.80f, 0.24f, 0.24f, 0.95f);
                default:
                    return new Color(0.25f, 0.56f, 0.90f, 0.95f);
            }
        }

        public sealed class ScaleScope : IDisposable
        {
            private readonly float previousLabelWidth;
            private readonly float previousFieldWidth;

            public float Scale { get; private set; }
            public float ContentWidth { get; private set; }
            public bool UsesMatrixScale => false;

            internal ScaleScope(float windowWidth)
            {
                previousLabelWidth = EditorGUIUtility.labelWidth;
                previousFieldWidth = EditorGUIUtility.fieldWidth;

                Scale = CalculateScale(windowWidth);
                ContentWidth = Mathf.Max(160f, windowWidth);

                // IMGUI layout and GUI.matrix do not stay stable together inside scroll views,
                // nested cards, and aggressively resized docked windows. AP keeps the scale
                // value for spacing decisions but avoids matrix scaling here.
                EditorGUIUtility.labelWidth = Mathf.Clamp(ContentWidth * 0.34f, 96f, 240f);
                EditorGUIUtility.fieldWidth = Mathf.Clamp(ContentWidth * 0.20f, 56f, 140f);
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = previousLabelWidth;
                EditorGUIUtility.fieldWidth = previousFieldWidth;
            }
        }

        public sealed class CardScope : IDisposable
        {
            private readonly EditorGUILayout.VerticalScope verticalScope;

            internal CardScope(string title, string subtitle)
            {
                EnsureStyles();
                verticalScope = new EditorGUILayout.VerticalScope("box");

                if (!string.IsNullOrWhiteSpace(title))
                {
                    GUILayout.Label(title, cardTitleStyle);
                }

                if (!string.IsNullOrWhiteSpace(subtitle))
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(subtitle, cardSubtitleStyle);
                }

                if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(subtitle))
                {
                    GUILayout.Space(4f);
                }
            }

            public void Dispose()
            {
                verticalScope.Dispose();
            }
        }

        public sealed class ResponsiveRow : IDisposable
        {
            private readonly float contentWidth;
            private readonly GUIStyle buttonStyle;
            private readonly GUIStyle miniButtonStyle;
            private readonly GUIStyle toolbarButtonStyle;

            private float cursorX;
            private float gap = 6f;
            private float pad = 2f;
            private bool rowOpen;

            internal ResponsiveRow(float contentWidth)
            {
                this.contentWidth = Mathf.Max(0f, contentWidth);
                buttonStyle = new GUIStyle(GUI.skin.button);
                miniButtonStyle = new GUIStyle(EditorStyles.miniButton);
                toolbarButtonStyle = new GUIStyle(EditorStyles.miniButtonMid);
                BeginRow();
            }

            public ResponsiveRow Gap(float value)
            {
                gap = Mathf.Max(0f, value);
                return this;
            }

            public ResponsiveRow Pad(float value)
            {
                pad = Mathf.Max(0f, value);
                return this;
            }

            public bool Button(string text)
            {
                return Button(new GUIContent(text ?? string.Empty), buttonStyle, 0f, false);
            }

            public bool Button(string text, float width)
            {
                return Button(new GUIContent(text ?? string.Empty), buttonStyle, width, false);
            }

            public bool MiniButton(string text)
            {
                return Button(new GUIContent(text ?? string.Empty), miniButtonStyle, 0f, false);
            }

            public bool ToolbarButton(string text)
            {
                return Button(new GUIContent(text ?? string.Empty), toolbarButtonStyle, 0f, false);
            }

            public bool ToggleButton(bool active, string text)
            {
                GUIStyle style = active ? toolbarButtonStyle : buttonStyle;
                return Button(new GUIContent(text ?? string.Empty), style, 0f, active);
            }

            public void Custom(float pixelWidth, Action drawAction)
            {
                float width = Mathf.Max(0f, pixelWidth);
                EnsureSpace(width);
                drawAction?.Invoke();
                cursorX += width;
            }

            public void FlexibleSpace()
            {
                GUILayout.FlexibleSpace();
            }

            public void NewRow()
            {
                EndRow();
                BeginRow();
            }

            public void Dispose()
            {
                EndRow();
            }

            private bool Button(GUIContent content, GUIStyle style, float fixedWidth, bool highlighted)
            {
                float width = CalculateWidth(content, style, fixedWidth);
                EnsureSpace(width);

                Color oldBackground = GUI.backgroundColor;
                if (highlighted)
                {
                    GUI.backgroundColor = new Color(0.28f, 0.58f, 0.96f, 1f);
                }

                bool clicked = GUILayout.Button(content, style, GUILayout.Width(width));

                GUI.backgroundColor = oldBackground;
                cursorX += width;
                return clicked;
            }

            private void BeginRow()
            {
                if (rowOpen)
                {
                    return;
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(pad);
                cursorX = pad;
                rowOpen = true;
            }

            private void EndRow()
            {
                if (!rowOpen)
                {
                    return;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(pad);
                EditorGUILayout.EndHorizontal();
                rowOpen = false;
            }

            private void EnsureSpace(float widthNeeded)
            {
                float available = Mathf.Max(0f, contentWidth - cursorX - pad);
                if (cursorX > pad && widthNeeded > available)
                {
                    NewRow();
                }
                else if (cursorX > pad)
                {
                    GUILayout.Space(gap);
                    cursorX += gap;
                }
            }

            private static float CalculateWidth(GUIContent content, GUIStyle style, float fixedWidth)
            {
                if (fixedWidth > 0f)
                {
                    return fixedWidth;
                }

                Vector2 size = style.CalcSize(content);
                return Mathf.Ceil(size.x + 8f);
            }
        }

        private static float CalculateScale(float windowWidth)
        {
            if (windowWidth <= 1f)
            {
                return 1f;
            }

            float scale = windowWidth / Mathf.Max(ReferenceWidth, 1f);
            if (scale > MaximumScale)
            {
                scale = MaximumScale;
            }

            if (scale < MinimumScale)
            {
                scale = MinimumScale;
            }

            return scale;
        }
    }
}
#endif
