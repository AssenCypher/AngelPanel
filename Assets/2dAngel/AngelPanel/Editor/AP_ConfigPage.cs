#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_ConfigPage
    {
        public static void Draw(AP_HostContext context)
        {
            if (context == null || context.Config == null || context.HostWindow == null)
            {
                return;
            }

            AP_CoreConfigData config = context.Config;
            config.Sanitize();
            AP_PackageDetector.Ensure();

            HostStyles styles = HostStyles.Create(context.HostWindow);

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_Cfg_TITLE"), styles.SectionTitle);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawCardHeader(AP_Loc.T("AP_Cfg_TITLE"), string.Empty, styles);

                EditorGUI.BeginChangeCheck();

                int placementIndex = DrawPopupRow(
                    AP_Loc.T("AP_Cfg_NAV_PLACEMENT"),
                    (int)config.navigationPlacement,
                    new[]
                    {
                        AP_Loc.T("AP_Cfg_NAV_LEFT"),
                        AP_Loc.T("AP_Cfg_NAV_RIGHT"),
                        AP_Loc.T("AP_Cfg_NAV_TOP"),
                        AP_Loc.T("AP_Cfg_NAV_BOTTOM")
                    },
                    styles);

                int scaleModeIndex = DrawPopupRow(
                    AP_Loc.T("AP_Cfg_SCALE_MODE"),
                    (int)config.chromeScaleMode,
                    new[]
                    {
                        AP_Loc.T("AP_Cfg_SCALE_ADAPTIVE"),
                        AP_Loc.T("AP_Cfg_SCALE_FIXED")
                    },
                    styles);

                int overflowModeIndex = DrawPopupRow(
                    AP_Loc.T("AP_Cfg_OVERFLOW_MODE"),
                    (int)config.navigationOverflowMode,
                    new[]
                    {
                        AP_Loc.T("AP_Cfg_OVERFLOW_COMPACT"),
                        AP_Loc.T("AP_Cfg_OVERFLOW_SCROLL")
                    },
                    styles);

                config.navigationPlacement = (AP_HostNavigationPlacement)Mathf.Clamp(placementIndex, 0, 3);
                config.chromeScaleMode = (AP_HostChromeScaleMode)Mathf.Clamp(scaleModeIndex, 0, 1);
                config.navigationOverflowMode = (AP_HostNavigationOverflowMode)Mathf.Clamp(overflowModeIndex, 0, 1);

                GUILayout.Space(4f);
                config.navigationPanelWidth = DrawSliderRow(AP_Loc.T("AP_Cfg_NAV_WIDTH"), config.navigationPanelWidth, 132f, 320f, styles);
                config.navigationButtonHeight = Mathf.RoundToInt(DrawSliderRow(AP_Loc.T("AP_Cfg_BUTTON_HEIGHT"), config.navigationButtonHeight, 20f, 42f, styles, true));
                config.navigationFontSize = Mathf.RoundToInt(DrawSliderRow(AP_Loc.T("AP_Cfg_FONT_SIZE"), config.navigationFontSize, 10f, 18f, styles, true));


                if (EditorGUI.EndChangeCheck())
                {
                    config.Sanitize();
                    context.HostWindow.NotifyHostConfigChanged();
                }
            }

            GUILayout.Space(8f);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawCardHeader(AP_Loc.T("AP_Cfg_PREVIEW_TITLE"), string.Empty, styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_NAV_PLACEMENT"), GetPlacementLabel(config.navigationPlacement), styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_SCALE_MODE"), GetScaleModeLabel(config.chromeScaleMode), styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_OVERFLOW_MODE"), GetOverflowModeLabel(config.navigationOverflowMode), styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_BUTTON_HEIGHT"), config.navigationButtonHeight.ToString(), styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_FONT_SIZE"), config.navigationFontSize.ToString(), styles);
                DrawKeyValue(AP_Loc.T("AP_Cfg_NAV_WIDTH"), Mathf.RoundToInt(config.navigationPanelWidth).ToString(), styles);
            }

            GUILayout.Space(8f);
            DrawIntegrationBlock(styles);

            if (context.HostWindow.IsHorizontalNavigationLayout())
            {
                GUILayout.Space(8f);
                DrawHorizontalLayoutStatus(context, styles);
            }
        }

        private static void DrawHorizontalLayoutStatus(AP_HostContext context, HostStyles styles)
        {
            IReadOnlyList<AP_ModuleManifest> installed = AP_ModuleRegistry.GetInstalledModules();
            List<AP_ModuleCatalogEntry> missing = AP_ModuleCatalog.GetMissing(installed);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawCardHeader(AP_Loc.T("AP_Cfg_STATUS_TITLE"), string.Empty, styles);

                GUILayout.Label(AP_Loc.T("AP_AB_INSTALLED_MODULES"), styles.SubsectionTitle);
                if (installed.Count == 0)
                {
                    GUILayout.Label(AP_Loc.T("AP_AB_INSTALLED_NONE"), styles.Note);
                }
                else
                {
                    for (int i = 0; i < installed.Count; i++)
                    {
                        AP_ModuleManifest manifest = installed[i];
                        if (manifest == null)
                        {
                            continue;
                        }

                        GUILayout.Label($"• {AP_Loc.T(manifest.displayNameLocKey)}", styles.Body);
                    }
                }

                GUILayout.Space(6f);
                GUILayout.Label(AP_Loc.T("AP_AB_MISSING_MODULES"), styles.SubsectionTitle);
                if (missing.Count == 0)
                {
                    GUILayout.Label(AP_Loc.T("AP_AB_MISSING_NONE"), styles.Note);
                }
                else
                {
                    for (int i = 0; i < missing.Count; i++)
                    {
                        AP_ModuleCatalogEntry entry = missing[i];
                        GUILayout.Label($"• {AP_Loc.T(entry.displayNameLocKey)}", styles.Body);
                    }
                }
            }
        }

        private static void DrawIntegrationBlock(HostStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawCardHeader(AP_Loc.T("AP_Cfg_INTEGRATION_TITLE"), string.Empty, styles);

                using (new EditorGUILayout.HorizontalScope())
                {
                    string timeText = AP_PackageDetector.LastScanTime == default
                        ? AP_Loc.T("AP_Cfg_INTEGRATION_NEVER")
                        : AP_PackageDetector.LastScanTime.ToString("yyyy-MM-dd HH:mm:ss");

                    GUILayout.Label(AP_Loc.T("AP_Cfg_INTEGRATION_LAST_SCAN") + timeText, styles.Note);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(AP_PackageDetector.IsScanning))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Cfg_INTEGRATION_REFRESH"), GUILayout.Height(22f), GUILayout.Width(110f)))
                        {
                            AP_PackageDetector.RefreshNow();
                        }
                    }
                }

                GUILayout.Space(4f);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_VRCSDK"), AP_PackageDetector.HasVRCSDKWorlds, string.Empty, styles);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_UDONSHARP"), AP_PackageDetector.HasUdonSharp, string.Empty, styles);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_BAKERY"), AP_PackageDetector.HasBakery, string.Empty, styles);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_MLP"), AP_PackageDetector.HasMagicLightProbes, string.Empty, styles);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_VRCLV"), AP_PackageDetector.HasVRCLightVolumes, string.Empty, styles);
                DrawIntegrationRow(AP_Loc.T("AP_Cfg_INT_VRCLV_MANAGER"), AP_PackageDetector.HasVRCLightVolumesManager, string.Empty, styles);

            }
        }

        private static void DrawIntegrationRow(string label, bool installed, string note, HostStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(installed ? "√" : "×", styles.StatusGlyph, GUILayout.Width(20f));
                    GUILayout.Label(label, styles.SubsectionTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(installed ? AP_Loc.T("AP_Cfg_INT_STATE_READY") : AP_Loc.T("AP_Cfg_INT_STATE_MISSING"), styles.Value, GUILayout.Width(92f));
                }

            }
        }

        private static int DrawPopupRow(string label, int selectedIndex, string[] options, HostStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, styles.Label, GUILayout.Width(styles.LabelWidth));
                return EditorGUILayout.Popup(selectedIndex, options, styles.Popup);
            }
        }

        private static float DrawSliderRow(string label, float value, float min, float max, HostStyles styles, bool roundToInt = false)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, styles.Label, GUILayout.Width(styles.LabelWidth));
                float next = GUILayout.HorizontalSlider(value, min, max);
                if (roundToInt)
                {
                    next = Mathf.Round(next);
                }

                string valueText = roundToInt ? Mathf.RoundToInt(next).ToString() : next.ToString("0.0");
                GUILayout.Label(valueText, styles.Value, GUILayout.Width(styles.ValueWidth));
                return Mathf.Clamp(next, min, max);
            }
        }

        private static void DrawCardHeader(string title, string subtitle, HostStyles styles)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                GUILayout.Label(title, styles.CardTitle);
            }

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                GUILayout.Space(2f);
                GUILayout.Label(subtitle, styles.Note);
                GUILayout.Space(4f);
            }
        }

        private static void DrawKeyValue(string key, string value, HostStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, styles.Label, GUILayout.Width(styles.LabelWidth));
                GUILayout.Label(value ?? string.Empty, styles.Value);
            }
        }

        private static string GetPlacementLabel(AP_HostNavigationPlacement placement)
        {
            switch (placement)
            {
                case AP_HostNavigationPlacement.Right:
                    return AP_Loc.T("AP_Cfg_NAV_RIGHT");
                case AP_HostNavigationPlacement.Top:
                    return AP_Loc.T("AP_Cfg_NAV_TOP");
                case AP_HostNavigationPlacement.Bottom:
                    return AP_Loc.T("AP_Cfg_NAV_BOTTOM");
                default:
                    return AP_Loc.T("AP_Cfg_NAV_LEFT");
            }
        }

        private static string GetScaleModeLabel(AP_HostChromeScaleMode scaleMode)
        {
            return scaleMode == AP_HostChromeScaleMode.Fixed
                ? AP_Loc.T("AP_Cfg_SCALE_FIXED")
                : AP_Loc.T("AP_Cfg_SCALE_ADAPTIVE");
        }

        private static string GetOverflowModeLabel(AP_HostNavigationOverflowMode overflowMode)
        {
            return overflowMode == AP_HostNavigationOverflowMode.Scroll
                ? AP_Loc.T("AP_Cfg_OVERFLOW_SCROLL")
                : AP_Loc.T("AP_Cfg_OVERFLOW_COMPACT");
        }

        private readonly struct HostStyles
        {
            public readonly GUIStyle SectionTitle;
            public readonly GUIStyle CardTitle;
            public readonly GUIStyle SubsectionTitle;
            public readonly GUIStyle Label;
            public readonly GUIStyle Body;
            public readonly GUIStyle Note;
            public readonly GUIStyle Value;
            public readonly GUIStyle Popup;
            public readonly GUIStyle StatusGlyph;
            public readonly float LabelWidth;
            public readonly float ValueWidth;

            private HostStyles(GUIStyle sectionTitle, GUIStyle cardTitle, GUIStyle subsectionTitle, GUIStyle label, GUIStyle body, GUIStyle note, GUIStyle value, GUIStyle popup, GUIStyle statusGlyph, float labelWidth, float valueWidth)
            {
                SectionTitle = sectionTitle;
                CardTitle = cardTitle;
                SubsectionTitle = subsectionTitle;
                Label = label;
                Body = body;
                Note = note;
                Value = value;
                Popup = popup;
                StatusGlyph = statusGlyph;
                LabelWidth = labelWidth;
                ValueWidth = valueWidth;
            }

            public static HostStyles Create(AP_Main hostWindow)
            {
                int bodySize = Mathf.Clamp(hostWindow.GetHostContentFontSize(), 10, 18);
                int titleSize = Mathf.Clamp(bodySize + 2, 12, 20);
                int sectionSize = Mathf.Clamp(bodySize + 3, 13, 22);

                GUIStyle sectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = sectionSize,
                    wordWrap = true
                };

                GUIStyle cardTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = titleSize,
                    wordWrap = true
                };

                GUIStyle subsectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = Mathf.Max(bodySize, 11),
                    wordWrap = true
                };

                GUIStyle label = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    wordWrap = true
                };

                GUIStyle body = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    wordWrap = true
                };

                GUIStyle note = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = Mathf.Max(bodySize - 1, 9),
                    wordWrap = true,
                    richText = true
                };

                GUIStyle value = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft
                };

                GUIStyle popup = new GUIStyle(EditorStyles.popup)
                {
                    fontSize = bodySize
                };

                GUIStyle statusGlyph = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = Mathf.Clamp(bodySize + 1, 11, 20),
                    alignment = TextAnchor.UpperLeft
                };

                float labelWidth = Mathf.Clamp(150f + (bodySize - 11) * 10f, 136f, 230f);
                float valueWidth = Mathf.Clamp(44f + (bodySize - 11) * 4f, 44f, 96f);
                return new HostStyles(sectionTitle, cardTitle, subsectionTitle, label, body, note, value, popup, statusGlyph, labelWidth, valueWidth);
            }
        }
    }
}
#endif
