#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_AboutPage
    {
        public static void Draw(AP_HostContext context)
        {
            if (context == null || context.Config == null || context.HostWindow == null)
            {
                return;
            }

            AboutStyles styles = AboutStyles.Create(context.HostWindow);
            if (!context.Config.hasSeenWelcome)
            {
                DrawWelcome(context, styles);
                return;
            }

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_AB_TITLE"), styles.SectionTitle);
            DrawOverview(context, styles);
            GUILayout.Space(8f);
            DrawInstalledModules(context, styles);
            GUILayout.Space(8f);
            DrawSuggestedModules(context, styles);
            GUILayout.Space(8f);
            DrawCapabilities(context, styles);
            GUILayout.Space(8f);
            DrawPaths(context, styles);
        }

        private static void DrawWelcome(AP_HostContext context, AboutStyles styles)
        {
            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_AB_WELCOME_TITLE"), styles.SectionTitle);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_AB_WELCOME_TITLE"), styles.CardTitle);
                GUILayout.Space(4f);
                GUILayout.Label(AP_Loc.T("AP_AB_WELCOME_BODY"), styles.Body);
                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AP_Loc.T("AP_AB_WELCOME_CONTINUE"), styles.ActionButton, GUILayout.Width(172f), GUILayout.Height(styles.ButtonHeight)))
                    {
                        context.HostWindow.MarkWelcomeSeen();
                    }
                }
            }
        }

        private static void DrawOverview(AP_HostContext context, AboutStyles styles)
        {
            IReadOnlyList<AP_ModuleManifest> installed = AP_ModuleRegistry.GetInstalledModules();
            List<AP_ModuleCatalogEntry> missing = AP_ModuleCatalog.GetMissing(installed);
            bool compact = context.ContentWidth < 720f;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_AB_OVERVIEW_TITLE"), styles.CardTitle);
                GUILayout.Space(6f);
                if (compact)
                {
                    DrawIdentityBlock(styles);
                    GUILayout.Space(8f);
                    DrawProductSummaryBlock(installed.Count, missing.Count, styles);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(280f)))
                        {
                            DrawIdentityBlock(styles);
                        }

                        GUILayout.Space(10f);
                        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(220f)))
                        {
                            DrawProductSummaryBlock(installed.Count, missing.Count, styles);
                        }
                    }
                }
            }
        }

        private static void DrawIdentityBlock(AboutStyles styles)
        {
            DrawKeyValue(AP_Loc.T("AP_AB_VERSION"), AP_CoreInfo.HostVersionLabel, styles);
            DrawKeyValue(AP_Loc.T("AP_AB_AUTHOR"), AP_CoreInfo.AuthorName, styles);
            DrawKeyValue(AP_Loc.T("AP_AB_COMMUNITY"), AP_CoreInfo.ProductOwnerName, styles);
            DrawKeyValue(AP_Loc.T("AP_AB_STORE"), AP_CoreInfo.StoreName, styles);
            DrawKeyValue(AP_Loc.T("AP_AB_SUPPORT"), AP_CoreInfo.SupportLine, styles);
            DrawKeyValue(AP_Loc.T("AP_AB_RELEASE"), AP_CoreInfo.ReleaseName, styles);
            GUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AP_Loc.T("AP_AB_OPEN_GITHUB"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                {
                    Application.OpenURL(AP_CoreInfo.PublicRepoUrl);
                }

                if (GUILayout.Button(AP_Loc.T("AP_AB_OPEN_VPM_REPO"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                {
                    Application.OpenURL(AP_CoreInfo.VpmRepoUrl);
                }

                if (GUILayout.Button(AP_Loc.T("AP_AB_OPEN_BOOTH"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                {
                    Application.OpenURL(AP_CoreInfo.BoothUrl);
                }
            }
        }

        private static void DrawProductSummaryBlock(int installedCount, int missingCount, AboutStyles styles)
        {
            GUILayout.Label(AP_Loc.T("AP_AB_PRODUCT_SUMMARY"), styles.SubsectionTitle);
            GUILayout.Space(2f);
            DrawKeyValue(AP_Loc.T("AP_AB_INSTALLED_COUNT"), installedCount.ToString(), styles);
            DrawKeyValue(AP_Loc.T("AP_AB_MISSING_COUNT"), missingCount.ToString(), styles);
            DrawKeyValue(AP_Loc.T("AP_AB_SUMMARY_FREE"), AP_ModuleCatalog.CountByLine("AP_AB_LINE_FREE_MODULES").ToString(), styles);
            DrawKeyValue(AP_Loc.T("AP_AB_SUMMARY_PAID"), AP_ModuleCatalog.CountByLine("AP_AB_LINE_PAID_PRODUCTS").ToString(), styles);
            DrawKeyValue(AP_Loc.T("AP_AB_SUMMARY_STANDALONE"), AP_ModuleCatalog.CountByLine("AP_AB_LINE_STANDALONE_PRODUCTS").ToString(), styles);
            DrawKeyValue(AP_Loc.T("AP_AB_PROVIDER_COUNT"), AP_Loc.ProviderCount.ToString(), styles);
        }

        private static void DrawInstalledModules(AP_HostContext context, AboutStyles styles)
        {
            IReadOnlyList<AP_ModuleManifest> coreProducts = AP_ModuleRegistry.GetInstalledCoreProducts();
            IReadOnlyList<AP_ModuleManifest> externalTools = AP_ModuleRegistry.GetInstalledExternalTools();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawFoldoutHeader(ref context.Config.showInstalledFoldout, AP_Loc.T("AP_AB_INSTALLED_MODULES"), styles);
                if (!context.Config.showInstalledFoldout)
                {
                    return;
                }

                GUILayout.Space(4f);
                GUILayout.Label(AP_Loc.T("AP_AB_CORE_COMPONENTS"), styles.SubsectionTitle);
                GUILayout.Space(4f);
                DrawCoreComponentList(coreProducts, styles);

                GUILayout.Space(10f);
                GUILayout.Label(AP_Loc.T("AP_AB_CONNECTED_TOOLS"), styles.SubsectionTitle);
                GUILayout.Space(4f);
                AP_ExternalModuleCards.DrawInstalledToolGrid(externalTools, context);
            }
        }

        private static void DrawCoreComponentList(IReadOnlyList<AP_ModuleManifest> coreProducts, AboutStyles styles)
        {
            if (coreProducts == null || coreProducts.Count == 0)
            {
                GUILayout.Label(AP_Loc.T("AP_AB_INSTALLED_NONE"), styles.Note);
                return;
            }

            for (int i = 0; i < coreProducts.Count; i++)
            {
                AP_ModuleManifest manifest = coreProducts[i];
                if (manifest == null)
                {
                    continue;
                }

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(AP_Loc.T(manifest.displayNameLocKey), styles.CardTitle);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(AP_Loc.T("AP_AB_BADGE_CORE"), styles.Badge);
                    }

                    string description = string.IsNullOrWhiteSpace(manifest.descriptionLocKey) ? string.Empty : AP_Loc.T(manifest.descriptionLocKey);
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        GUILayout.Space(2f);
                        GUILayout.Label(description, styles.Note);
                    }

                    GUILayout.Space(4f);
                    DrawKeyValue(AP_Loc.T("AP_AB_MODULE_ID"), manifest.moduleId, styles);
                    DrawKeyValue(AP_Loc.T("AP_AB_VERSION"), string.IsNullOrWhiteSpace(manifest.version) ? "-" : manifest.version, styles);
                }

                if (i < coreProducts.Count - 1)
                {
                    GUILayout.Space(6f);
                }
            }
        }

        private static void DrawSuggestedModules(AP_HostContext context, AboutStyles styles)
        {
            List<AP_ModuleCatalogEntry> missing = AP_ModuleCatalog.GetMissing(AP_ModuleRegistry.GetInstalledModules());
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawFoldoutHeader(ref context.Config.showMissingFoldout, AP_Loc.T("AP_AB_MISSING_MODULES"), styles);
                if (!context.Config.showMissingFoldout)
                {
                    return;
                }

                GUILayout.Space(4f);
                if (missing.Count == 0)
                {
                    GUILayout.Label(AP_Loc.T("AP_AB_MISSING_NONE"), styles.Note);
                    return;
                }

                int currentLineOrder = int.MinValue;
                string currentLineLocKey = string.Empty;
                for (int i = 0; i < missing.Count; i++)
                {
                    AP_ModuleCatalogEntry entry = missing[i];
                    if (entry.lineOrder != currentLineOrder || !string.Equals(entry.lineLocKey, currentLineLocKey, StringComparison.Ordinal))
                    {
                        if (i > 0)
                        {
                            GUILayout.Space(8f);
                        }

                        GUILayout.Label(AP_Loc.T(entry.lineLocKey), styles.SubsectionTitle);
                        currentLineOrder = entry.lineOrder;
                        currentLineLocKey = entry.lineLocKey;
                        GUILayout.Space(4f);
                    }

                    DrawSuggestedModuleCard(entry, styles);
                    if (i < missing.Count - 1)
                    {
                        GUILayout.Space(6f);
                    }
                }
            }
        }

        private static void DrawSuggestedModuleCard(AP_ModuleCatalogEntry entry, AboutStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T(entry.displayNameLocKey), styles.CardTitle);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T(entry.badgeLocKey), styles.Badge);
                }

                if (!string.IsNullOrWhiteSpace(entry.subtitleLocKey))
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(AP_Loc.T(entry.subtitleLocKey), styles.Note);
                }

                GUILayout.Space(4f);
                DrawKeyValue(AP_Loc.T("AP_AB_MODULE_ID"), entry.moduleId, styles);
                if (!string.IsNullOrWhiteSpace(entry.targetLocKey))
                {
                    DrawKeyValue(AP_Loc.T("AP_AB_PRODUCT_TARGET"), AP_Loc.T(entry.targetLocKey), styles);
                }

                DrawKeyValue(AP_Loc.T("AP_AB_STATUS"), string.IsNullOrWhiteSpace(entry.storeUrl) ? AP_Loc.T("AP_AB_STATUS_UNRELEASED") : AP_Loc.T("AP_AB_OPEN_PRODUCT_PAGE"), styles);

                if (!string.IsNullOrWhiteSpace(entry.storeUrl))
                {
                    GUILayout.Space(6f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(AP_Loc.T("AP_AB_OPEN_PRODUCT_PAGE"), styles.SecondaryButton, GUILayout.Width(172f), GUILayout.Height(styles.ButtonHeight)))
                        {
                            Application.OpenURL(entry.storeUrl);
                        }
                    }
                }
            }
        }

        private static void DrawCapabilities(AP_HostContext context, AboutStyles styles)
        {
            IReadOnlyDictionary<string, HashSet<string>> snapshot = AP_CapabilityRegistry.GetSnapshot();
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawFoldoutHeader(ref context.Config.showCapabilityFoldout, AP_Loc.T("AP_AB_CAPABILITIES"), styles);
                if (!context.Config.showCapabilityFoldout)
                {
                    return;
                }

                GUILayout.Space(4f);
                if (snapshot.Count == 0)
                {
                    GUILayout.Label(AP_Loc.T("AP_AB_CAPABILITY_NONE"), styles.Note);
                    return;
                }

                foreach (KeyValuePair<string, HashSet<string>> pair in snapshot)
                {
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        GUILayout.Label(pair.Key, styles.SubsectionTitle);
                        GUILayout.Space(2f);
                        GUILayout.Label(GetProvidersText(pair.Value), styles.Body);
                    }
                }
            }
        }

        private static void DrawPaths(AP_HostContext context, AboutStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawFoldoutHeader(ref context.Config.showPathFoldout, AP_Loc.T("AP_AB_PATHS"), styles);
                if (!context.Config.showPathFoldout)
                {
                    return;
                }

                GUILayout.Space(4f);
                DrawKeyValue(AP_Loc.T("AP_AB_ASSET_ROOT"), AP_CorePaths.AssetRoot, styles);
                DrawKeyValue(AP_Loc.T("AP_AB_APPDATA_ROOT"), AP_CorePaths.AppDataRoot, styles);
                DrawKeyValue(AP_Loc.T("AP_AB_CONFIG_ROOT"), AP_CorePaths.ConfigRoot, styles);
                DrawKeyValue(AP_Loc.T("AP_AB_LOC_ROOT"), AP_CorePaths.LocalizationRoot, styles);
                DrawKeyValue(AP_Loc.T("AP_AB_LOC_OVERLAY"), AP_Loc.IsOverlayLoaded ? AP_Loc.T("AP_MP_LOC_OVERLAY_LOADED") : AP_Loc.T("AP_MP_LOC_OVERLAY_NOT_LOADED"), styles);
                DrawKeyValue(AP_Loc.T("AP_AB_PROVIDER_COUNT"), AP_Loc.ProviderCount.ToString(), styles);
                GUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_AB_REVEAL_ASSET_ROOT"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        EditorUtility.RevealInFinder(Path.GetFullPath(AP_CorePaths.AssetRoot));
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_AB_REVEAL_CONFIG_ROOT"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        EditorUtility.RevealInFinder(Path.GetFullPath(AP_CorePaths.ConfigRoot));
                    }
                }
            }
        }

        private static void DrawFoldoutHeader(ref bool expanded, string title, AboutStyles styles)
        {
            string prefix = expanded ? "▼" : "▶";
            if (GUILayout.Button($"{prefix} {title}", styles.FoldoutButton, GUILayout.Height(styles.ButtonHeight + 2f)))
            {
                expanded = !expanded;
            }
        }

        private static void DrawKeyValue(string key, string value, AboutStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(key, styles.Label, GUILayout.Width(styles.LabelWidth));
                GUILayout.Label(value ?? string.Empty, styles.Body);
            }
        }

        private static string GetProvidersText(HashSet<string> providers)
        {
            if (providers == null || providers.Count == 0)
            {
                return AP_Loc.T("AP_AB_STATUS") + ": -";
            }

            string[] items = new string[providers.Count];
            providers.CopyTo(items);
            Array.Sort(items, StringComparer.Ordinal);
            return string.Join(", ", items);
        }

        private readonly struct AboutStyles
        {
            public readonly GUIStyle SectionTitle;
            public readonly GUIStyle CardTitle;
            public readonly GUIStyle SubsectionTitle;
            public readonly GUIStyle Label;
            public readonly GUIStyle Body;
            public readonly GUIStyle Note;
            public readonly GUIStyle Badge;
            public readonly GUIStyle FoldoutButton;
            public readonly GUIStyle ActionButton;
            public readonly GUIStyle SecondaryButton;
            public readonly float LabelWidth;
            public readonly float ButtonHeight;

            public AboutStyles(
                GUIStyle sectionTitle,
                GUIStyle cardTitle,
                GUIStyle subsectionTitle,
                GUIStyle label,
                GUIStyle body,
                GUIStyle note,
                GUIStyle badge,
                GUIStyle foldoutButton,
                GUIStyle actionButton,
                GUIStyle secondaryButton,
                float labelWidth,
                float buttonHeight)
            {
                SectionTitle = sectionTitle;
                CardTitle = cardTitle;
                SubsectionTitle = subsectionTitle;
                Label = label;
                Body = body;
                Note = note;
                Badge = badge;
                FoldoutButton = foldoutButton;
                ActionButton = actionButton;
                SecondaryButton = secondaryButton;
                LabelWidth = labelWidth;
                ButtonHeight = buttonHeight;
            }

            public static AboutStyles Create(AP_Main hostWindow)
            {
                int bodySize = Mathf.Clamp(hostWindow.GetHostContentFontSize(), 10, 18);
                int titleSize = Mathf.Clamp(bodySize + 2, 12, 21);
                int sectionSize = Mathf.Clamp(bodySize + 3, 13, 23);
                float buttonHeight = Mathf.Clamp(22f + (bodySize - 11) * 1.6f, 22f, 34f);
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
                GUIStyle badge = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fontSize = Mathf.Max(bodySize - 1, 9),
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 3, 3),
                    fixedHeight = buttonHeight
                };
                GUIStyle foldoutButton = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = Mathf.Max(bodySize, 10)
                };
                GUIStyle actionButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleCenter
                };
                GUIStyle secondaryButton = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = Mathf.Max(bodySize - 1, 9),
                    alignment = TextAnchor.MiddleCenter
                };
                float labelWidth = Mathf.Clamp(126f + (bodySize - 11) * 8f, 126f, 220f);
                return new AboutStyles(sectionTitle, cardTitle, subsectionTitle, label, body, note, badge, foldoutButton, actionButton, secondaryButton, labelWidth, buttonHeight);
            }
        }
    }
}
#endif
