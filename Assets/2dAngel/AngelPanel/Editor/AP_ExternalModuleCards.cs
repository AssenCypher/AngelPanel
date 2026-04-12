#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_ExternalModuleCards
    {
        public static void DrawInstalledToolGrid(IReadOnlyList<AP_ModuleManifest> tools, AP_HostContext context)
        {
            if (context == null)
            {
                return;
            }

            int count = tools != null ? tools.Count : 0;
            if (count == 0)
            {
                DrawEmptyCard(AP_Loc.T("AP_TL_EMPTY"), context);
                return;
            }

            CardStyles styles = new CardStyles(context.HostWindow);
            float width = Mathf.Max(260f, context.ContentWidth);
            int columns = width >= 980f ? 3 : (width >= 640f ? 2 : 1);
            int index = 0;
            while (index < count)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (index >= count)
                        {
                            GUILayout.FlexibleSpace();
                            break;
                        }

                        DrawInstalledToolCard(tools[index], context, styles, width, columns);
                        if (column < columns - 1)
                        {
                            GUILayout.Space(6f);
                        }
                        index++;
                    }
                }

                GUILayout.Space(6f);
            }
        }

        public static void DrawRecommendedToolGrid(IReadOnlyList<AP_ModuleCatalogEntry> entries, AP_HostContext context)
        {
            if (context == null)
            {
                return;
            }

            int count = entries != null ? entries.Count : 0;
            if (count == 0)
            {
                DrawEmptyCard(AP_Loc.T("AP_TL_RECOMMENDED_NONE"), context);
                return;
            }

            CardStyles styles = new CardStyles(context.HostWindow);
            float width = Mathf.Max(260f, context.ContentWidth);
            int columns = width >= 980f ? 3 : (width >= 640f ? 2 : 1);
            int index = 0;
            while (index < count)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (index >= count)
                        {
                            GUILayout.FlexibleSpace();
                            break;
                        }

                        DrawRecommendedToolCard(entries[index], context, styles, width, columns);
                        if (column < columns - 1)
                        {
                            GUILayout.Space(6f);
                        }
                        index++;
                    }
                }

                GUILayout.Space(6f);
            }
        }

        public static void DrawDefaultToolPage(AP_HostContext context, string moduleId)
        {
            if (context == null || string.IsNullOrWhiteSpace(moduleId))
            {
                return;
            }

            if (!AP_ModuleRegistry.TryGet(moduleId, out AP_ModuleManifest manifest) || manifest == null)
            {
                return;
            }

            CardStyles styles = new CardStyles(context.HostWindow);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T(manifest.displayNameLocKey), styles.HeroTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(GetBadgeText(manifest), styles.Badge);
                }

                string description = GetModuleDescription(manifest);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    GUILayout.Space(4f);
                    GUILayout.Label(description, styles.Body);
                }

                GUILayout.Space(8f);
                DrawKeyValue(AP_Loc.T("AP_AB_VERSION"), string.IsNullOrWhiteSpace(manifest.version) ? "-" : manifest.version, styles);
                DrawKeyValue(AP_Loc.T("AP_TL_CAPABILITIES"), GetCapabilitiesText(manifest.capabilities), styles);
                if (AP_ModuleCatalog.TryGetEntry(manifest.moduleId, out AP_ModuleCatalogEntry entry) && !string.IsNullOrWhiteSpace(entry.targetLocKey))
                {
                    DrawKeyValue(AP_Loc.T("AP_TL_TARGET"), AP_Loc.T(entry.targetLocKey), styles);
                }

                GUILayout.Space(8f);
                DrawButtonRow(styles, context.ContentWidth < 520f,
                    new ButtonSpec(AP_Loc.T("AP_TL_OPEN_TOOL"), manifest.openStandaloneWindow, true),
                    new ButtonSpec(AP_Loc.T("AP_TL_OPEN_PAGE"), !string.IsNullOrWhiteSpace(manifest.productUrl) ? (() => Application.OpenURL(manifest.productUrl)) : null, false));
            }
        }

        private static void DrawInstalledToolCard(AP_ModuleManifest manifest, AP_HostContext context, CardStyles styles, float contentWidth, int columns)
        {
            float width = GetCardWidth(contentWidth, columns);
            bool stackButtons = width < 320f || context.ContentWidth < 540f;
            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(width), GUILayout.MinHeight(styles.CardMinHeight)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T(manifest.displayNameLocKey), styles.CardTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(GetBadgeText(manifest), styles.Badge);
                }

                string description = GetModuleDescription(manifest);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(description, styles.Body, GUILayout.MinHeight(styles.DescriptionMinHeight));
                }
                else
                {
                    GUILayout.Space(styles.DescriptionMinHeight);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(6f);
                DrawKeyValue(AP_Loc.T("AP_AB_VERSION"), string.IsNullOrWhiteSpace(manifest.version) ? "-" : manifest.version, styles);
                DrawKeyValue(AP_Loc.T("AP_TL_CAPABILITIES"), GetCapabilitiesText(manifest.capabilities), styles);

                if (AP_ModuleCatalog.TryGetEntry(manifest.moduleId, out AP_ModuleCatalogEntry entry) && !string.IsNullOrWhiteSpace(entry.targetLocKey))
                {
                    DrawKeyValue(AP_Loc.T("AP_TL_TARGET"), AP_Loc.T(entry.targetLocKey), styles);
                }

                GUILayout.Space(8f);
                DrawButtonRow(styles, stackButtons,
                    new ButtonSpec(AP_Loc.T("AP_TL_OPEN_IN_AP"), () => AP_Main.OpenTab(manifest.moduleId), true),
                    new ButtonSpec(AP_Loc.T("AP_TL_OPEN_TOOL"), manifest.openStandaloneWindow, false));
            }
        }

        private static void DrawRecommendedToolCard(AP_ModuleCatalogEntry entry, AP_HostContext context, CardStyles styles, float contentWidth, int columns)
        {
            float width = GetCardWidth(contentWidth, columns);
            bool stackButtons = width < 320f || context.ContentWidth < 540f;
            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(width), GUILayout.MinHeight(styles.CardMinHeight - 8f)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T(entry.displayNameLocKey), styles.CardTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T(entry.badgeLocKey), styles.Badge);
                }

                string description = AP_Loc.T(entry.subtitleLocKey);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(description, styles.Body, GUILayout.MinHeight(styles.DescriptionMinHeight));
                }
                else
                {
                    GUILayout.Space(styles.DescriptionMinHeight);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Space(6f);
                DrawKeyValue(AP_Loc.T("AP_TL_TARGET"), AP_Loc.T(entry.targetLocKey), styles);
                DrawKeyValue(AP_Loc.T("AP_TL_STATUS"), AP_Loc.T("AP_TL_STATUS_NOT_INSTALLED"), styles);

                GUILayout.Space(8f);
                DrawButtonRow(styles, stackButtons,
                    new ButtonSpec(AP_Loc.T("AP_TL_VIEW_DETAILS"), () => AP_Main.OpenTab(AP_ModuleIds.About), true),
                    new ButtonSpec(AP_Loc.T("AP_TL_OPEN_PAGE"), !string.IsNullOrWhiteSpace(entry.storeUrl) ? (() => Application.OpenURL(entry.storeUrl)) : null, false));
            }
        }

        private static void DrawButtonRow(CardStyles styles, bool stack, params ButtonSpec[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
            {
                return;
            }

            if (stack)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].action == null)
                    {
                        continue;
                    }

                    GUIStyle style = buttons[i].primary ? styles.PrimaryButton : styles.SecondaryButton;
                    if (GUILayout.Button(buttons[i].label, style, GUILayout.Height(styles.ButtonHeight)))
                    {
                        buttons[i].action.Invoke();
                    }

                    if (i < buttons.Length - 1)
                    {
                        GUILayout.Space(4f);
                    }
                }

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].action == null)
                    {
                        continue;
                    }

                    GUIStyle style = buttons[i].primary ? styles.PrimaryButton : styles.SecondaryButton;
                    if (GUILayout.Button(buttons[i].label, style, GUILayout.Height(styles.ButtonHeight)))
                    {
                        buttons[i].action.Invoke();
                    }
                }
            }
        }

        private static void DrawEmptyCard(string text, AP_HostContext context)
        {
            CardStyles styles = new CardStyles(context != null ? context.HostWindow : null);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(text, styles.BodyCenter);
            }
        }

        private static void DrawKeyValue(string key, string value, CardStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, styles.KeyLabel, GUILayout.Width(styles.KeyWidth));
                GUILayout.Label(string.IsNullOrWhiteSpace(value) ? "-" : value, styles.Mini, GUILayout.ExpandWidth(true));
            }
        }

        private static float GetCardWidth(float contentWidth, int columnCount)
        {
            float gap = 6f * Mathf.Max(0, columnCount - 1);
            return Mathf.Max(220f, (contentWidth - gap) / Mathf.Max(1, columnCount));
        }

        private static string GetModuleDescription(AP_ModuleManifest manifest)
        {
            if (manifest == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(manifest.descriptionLocKey))
            {
                string loc = AP_Loc.T(manifest.descriptionLocKey);
                if (!string.IsNullOrWhiteSpace(loc) && !string.Equals(loc, manifest.descriptionLocKey, StringComparison.Ordinal))
                {
                    return loc;
                }
            }

            if (AP_ModuleCatalog.TryGetEntry(manifest.moduleId, out AP_ModuleCatalogEntry entry) && !string.IsNullOrWhiteSpace(entry.subtitleLocKey))
            {
                return AP_Loc.T(entry.subtitleLocKey);
            }

            return string.Empty;
        }

        private static string GetBadgeText(AP_ModuleManifest manifest)
        {
            if (manifest != null && AP_ModuleCatalog.TryGetEntry(manifest.moduleId, out AP_ModuleCatalogEntry entry) && !string.IsNullOrWhiteSpace(entry.badgeLocKey))
            {
                return AP_Loc.T(entry.badgeLocKey);
            }

            return AP_Loc.T("AP_TL_BADGE_TOOL");
        }

        private static string GetCapabilitiesText(string[] capabilities)
        {
            if (capabilities == null || capabilities.Length == 0)
            {
                return AP_Loc.T("AP_TL_CAP_NONE");
            }

            return string.Join(", ", capabilities);
        }

        private readonly struct ButtonSpec
        {
            public readonly string label;
            public readonly Action action;
            public readonly bool primary;

            public ButtonSpec(string label, Action action, bool primary)
            {
                this.label = label ?? string.Empty;
                this.action = action;
                this.primary = primary;
            }
        }

        private sealed class CardStyles
        {
            public readonly GUIStyle HeroTitle;
            public readonly GUIStyle CardTitle;
            public readonly GUIStyle Body;
            public readonly GUIStyle BodyCenter;
            public readonly GUIStyle Mini;
            public readonly GUIStyle KeyLabel;
            public readonly GUIStyle Badge;
            public readonly GUIStyle PrimaryButton;
            public readonly GUIStyle SecondaryButton;
            public readonly float ButtonHeight;
            public readonly float CardMinHeight;
            public readonly float DescriptionMinHeight;
            public readonly float KeyWidth;

            public CardStyles(AP_Main hostWindow)
            {
                int contentSize = Mathf.Clamp(hostWindow != null ? hostWindow.GetHostContentFontSize() : 12, 10, 18);
                HeroTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = Mathf.Clamp(contentSize + 2, 12, 20),
                    wordWrap = true
                };

                CardTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = Mathf.Clamp(contentSize + 1, 11, 18),
                    wordWrap = true
                };

                Body = new GUIStyle(EditorStyles.label)
                {
                    fontSize = contentSize,
                    wordWrap = true
                };

                BodyCenter = new GUIStyle(Body)
                {
                    alignment = TextAnchor.MiddleCenter
                };

                Mini = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = Mathf.Clamp(contentSize - 1, 9, 14),
                    wordWrap = true
                };

                KeyLabel = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    fontSize = Mathf.Clamp(contentSize - 1, 9, 14)
                };

                Badge = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = Mathf.Clamp(contentSize - 1, 9, 13),
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 3, 3),
                    fixedHeight = Mathf.Clamp(18f + (contentSize - 10f), 18f, 24f)
                };

                PrimaryButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = contentSize,
                    alignment = TextAnchor.MiddleCenter
                };

                SecondaryButton = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = Mathf.Clamp(contentSize - 1, 9, 14),
                    alignment = TextAnchor.MiddleCenter
                };

                ButtonHeight = Mathf.Clamp(22f + (contentSize - 10f) * 1.5f, 22f, 34f);
                CardMinHeight = Mathf.Clamp(154f + (contentSize - 10f) * 8f, 154f, 232f);
                DescriptionMinHeight = Mathf.Clamp(36f + (contentSize - 10f) * 6f, 36f, 84f);
                KeyWidth = Mathf.Clamp(86f + (contentSize - 10f) * 7f, 86f, 148f);
            }
        }
    }
}
#endif
