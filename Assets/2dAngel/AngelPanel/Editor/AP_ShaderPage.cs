#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    public static class AP_ShaderPage
    {
        private enum ScopeMode
        {
            Scene = 0,
            Selection = 1,
            Both = 2
        }

        private enum SupportState
        {
            Unknown = 0,
            Yes = 1,
            No = 2
        }

        private sealed class MaterialUsage
        {
            public Material material;
            public Shader sourceShader;
            public int useCount;
        }

        private sealed class ShaderUsage
        {
            public Shader shader;
            public int useCount;
        }

        private sealed class SelectionSummary
        {
            public int rootCount;
            public int rendererCount;
            public int materialSlotCount;
            public int uniqueMaterialCount;
            public int uniqueShaderCount;
            public readonly List<ShaderUsage> shaderUsage = new List<ShaderUsage>(32);
        }

        private sealed class MaterialSnapshot
        {
            public readonly Dictionary<string, Color> colors = new Dictionary<string, Color>(16);
            public readonly Dictionary<string, float> floats = new Dictionary<string, float>(24);
            public readonly Dictionary<string, Texture> textures = new Dictionary<string, Texture>(16);
            public readonly Dictionary<string, Vector2> textureScales = new Dictionary<string, Vector2>(16);
            public readonly Dictionary<string, Vector2> textureOffsets = new Dictionary<string, Vector2>(16);
        }

        private sealed class ShaderInstallEntry
        {
            public string id;
            public string nameLocKey;
            public string hintLocKey;
            public string descLocKey;
            public string repoUrl;
            public string guideUrl;
            public string listingUrl;
            public string upmGitUrl;
            public string storeUrl;
            public string[] pathHints;
            public SupportState vrclvSupport;
            public SupportState monoShSupport;
            public SupportState shRnmSupport;
            public bool installed;
        }

        private static readonly string[] CommonColorProperties =
        {
            "_Color",
            "_EmissionColor",
            "_SpecColor"
        };

        private static readonly string[] CommonFloatProperties =
        {
            "_Metallic",
            "_Glossiness",
            "_Smoothness",
            "_Cutoff",
            "_BumpScale",
            "_Parallax",
            "_OcclusionStrength",
            "_GlossMapScale",
            "_Mode",
            "_Cull",
            "_SrcBlend",
            "_DstBlend",
            "_ZWrite"
        };

        private static readonly string[] CommonTextureProperties =
        {
            "_MainTex",
            "_BumpMap",
            "_ParallaxMap",
            "_MetallicGlossMap",
            "_EmissionMap",
            "_OcclusionMap",
            "_DetailMask",
            "_DetailAlbedoMap",
            "_DetailNormalMap",
            "_SpecGlossMap"
        };

        private static readonly List<MaterialUsage> ScanResults = new List<MaterialUsage>(128);
        private static readonly SelectionSummary CurrentSelectionSummary = new SelectionSummary();
        private static readonly List<ShaderInstallEntry> InstallEntries = new List<ShaderInstallEntry>(13)
        {
            new ShaderInstallEntry
            {
                id = "poiyomi",
                nameLocKey = "AP_Shd_VENDOR_POIYOMI",
                hintLocKey = "AP_Shd_VENDOR_POIYOMI_HINT",
                descLocKey = "AP_Shd_VENDOR_POIYOMI_DESC",
                guideUrl = "https://www.poiyomi.com/download/",
                listingUrl = "https://poiyomi.github.io/vpm/index.json",
                pathHints = new [] { "poiyomi", "com.poiyomi.toon" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "liltoon",
                nameLocKey = "AP_Shd_VENDOR_LILTOON",
                hintLocKey = "AP_Shd_VENDOR_LILTOON_HINT",
                descLocKey = "AP_Shd_VENDOR_LILTOON_DESC",
                repoUrl = "https://github.com/lilxyzw/lilToon",
                guideUrl = "https://github.com/lilxyzw/lilToon",
                upmGitUrl = "https://github.com/lilxyzw/lilToon.git?path=Assets/lilToon#master",
                pathHints = new [] { "liltoon", "jp.lilxyzw.liltoon" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "graphlit",
                nameLocKey = "AP_Shd_VENDOR_GRAPHLIT",
                hintLocKey = "AP_Shd_VENDOR_GRAPHLIT_HINT",
                descLocKey = "AP_Shd_VENDOR_GRAPHLIT_DESC",
                repoUrl = "https://github.com/z3y/Graphlit",
                guideUrl = "https://z3y.github.io/Graphlit/",
                listingUrl = "https://z3y.github.io/vpm-package-listing/",
                pathHints = new [] { "graphlit", "z3y" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "filamented",
                nameLocKey = "AP_Shd_VENDOR_FILAMENTED",
                hintLocKey = "AP_Shd_VENDOR_FILAMENTED_HINT",
                descLocKey = "AP_Shd_VENDOR_FILAMENTED_DESC",
                repoUrl = "https://gitlab.com/s-ilent/filamented",
                guideUrl = "https://gitlab.com/s-ilent/filamented",
                storeUrl = "https://booth.pm/ja/items/3250389",
                pathHints = new [] { "filamented", "s-ilent" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Yes,
                shRnmSupport = SupportState.Yes
            },
            new ShaderInstallEntry
            {
                id = "scss",
                nameLocKey = "AP_Shd_VENDOR_SCSS",
                hintLocKey = "AP_Shd_VENDOR_SCSS_HINT",
                descLocKey = "AP_Shd_VENDOR_SCSS_DESC",
                repoUrl = "https://github.com/s-ilent/scss",
                guideUrl = "https://s-ilent.gitlab.io/generic.html",
                pathHints = new [] { "scss", "silent cel", "silentcel" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "mochies",
                nameLocKey = "AP_Shd_VENDOR_MOCHIES",
                hintLocKey = "AP_Shd_VENDOR_MOCHIES_HINT",
                descLocKey = "AP_Shd_VENDOR_MOCHIES_DESC",
                repoUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders",
                guideUrl = "https://github.com/MochiesCode/Mochies-Unity-Shaders",
                pathHints = new [] { "mochies", "mochiesshaders" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "orels",
                nameLocKey = "AP_Shd_VENDOR_ORELS",
                hintLocKey = "AP_Shd_VENDOR_ORELS_HINT",
                descLocKey = "AP_Shd_VENDOR_ORELS_DESC",
                repoUrl = "https://github.com/orels1/orels-Unity-Shaders",
                guideUrl = "https://shaders.orels.sh/docs/installation",
                listingUrl = "https://orels1.github.io/orels-Unity-Shaders/index.json",
                pathHints = new [] { "orels", "orl shaders", "orels1" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Yes,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "unlitwf",
                nameLocKey = "AP_Shd_VENDOR_UNLITWF",
                hintLocKey = "AP_Shd_VENDOR_UNLITWF_HINT",
                descLocKey = "AP_Shd_VENDOR_UNLITWF_DESC",
                repoUrl = "https://github.com/whiteflare/Unlit_WF_ShaderSuite",
                guideUrl = "https://whiteflare.github.io/vpm-repos/docs/unlitwf",
                pathHints = new [] { "unlit_wf", "unlitwf", "whiteflare" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "acls",
                nameLocKey = "AP_Shd_VENDOR_ACLS",
                hintLocKey = "AP_Shd_VENDOR_ACLS_HINT",
                descLocKey = "AP_Shd_VENDOR_ACLS_DESC",
                repoUrl = "https://github.com/ACIIL/ACLS-Shader-1.0",
                storeUrl = "https://aciil.booth.pm/items/1779615",
                pathHints = new [] { "acls", "aciil" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "realtoon",
                nameLocKey = "AP_Shd_VENDOR_REALTOON",
                hintLocKey = "AP_Shd_VENDOR_REALTOON_HINT",
                descLocKey = "AP_Shd_VENDOR_REALTOON_DESC",
                storeUrl = "https://assetstore.unity.com/packages/vfx/shaders/realtoon-pro-anime-toon-shader-65518",
                pathHints = new [] { "realtoon", "mjq" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "genelit",
                nameLocKey = "AP_Shd_VENDOR_GENELIT",
                hintLocKey = "AP_Shd_VENDOR_GENELIT_HINT",
                descLocKey = "AP_Shd_VENDOR_GENELIT_DESC",
                repoUrl = "https://github.com/momoma-null/GeneLit",
                guideUrl = "https://github.com/momoma-null/GeneLit",
                pathHints = new [] { "genelit", "momoma" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "unityshadersplus",
                nameLocKey = "AP_Shd_VENDOR_USP",
                hintLocKey = "AP_Shd_VENDOR_USP_HINT",
                descLocKey = "AP_Shd_VENDOR_USP_DESC",
                repoUrl = "https://github.com/ShingenPizza/UnityShadersPlus",
                guideUrl = "https://github.com/ShingenPizza/UnityShadersPlus",
                pathHints = new [] { "unityshadersplus", "shingenpizza" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            },
            new ShaderInstallEntry
            {
                id = "quantum",
                nameLocKey = "AP_Shd_VENDOR_QUANTUM",
                hintLocKey = "AP_Shd_VENDOR_QUANTUM_HINT",
                descLocKey = "AP_Shd_VENDOR_QUANTUM_DESC",
                repoUrl = "https://github.com/SaphiBlue/quantumshader",
                guideUrl = "https://github.com/SaphiBlue/quantumshader",
                pathHints = new [] { "quantumshader", "saphi" },
                vrclvSupport = SupportState.Yes,
                monoShSupport = SupportState.Unknown,
                shRnmSupport = SupportState.Unknown
            }
        };

        private static ScopeMode scopeMode = ScopeMode.Both;
        private static Shader targetShader;
        private static Shader sourceShader;
        private static bool dryRun = true;
        private static Vector2 resultsScroll;
        private static Vector2 selectionShaderScroll;
        private static double lastStatusRefreshTime = -100d;
        private static double lastSelectionSummaryTime = -100d;
        private static int lastSelectionHash;
        private static AddRequest addRequest;
        private static ShaderInstallEntry activeInstallEntry;
        private static string installStatus = string.Empty;
        private static string lastScanContext = string.Empty;
        private static int selectedInstallIndex;

        public static void Draw(AP_HostContext context)
        {
            if (context == null || context.HostWindow == null)
            {
                return;
            }

            RefreshInstallStateIfNeeded();
            RefreshSelectionSummaryIfNeeded();
            ShaderStyles styles = ShaderStyles.Create(context.HostWindow);

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_Opt_TAB_SHADER"), styles.SectionTitle);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_Shd_PAGE_TITLE"), styles.CardTitle);
            }

            GUILayout.Space(8f);
            DrawReplaceCard(styles);
            GUILayout.Space(8f);
            DrawInstallerCard(styles);
        }

        private static void DrawReplaceCard(ShaderStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_Shd_CARD_REPLACE_TITLE"), styles.CardTitle);

                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_SCOPE"), styles.Label, GUILayout.Width(styles.LabelWidth));
                    scopeMode = (ScopeMode)EditorGUILayout.Popup((int)scopeMode, new[]
                    {
                        AP_Loc.T("AP_Shd_SCOPE_SCENE"),
                        AP_Loc.T("AP_Shd_SCOPE_SELECTION"),
                        AP_Loc.T("AP_Shd_SCOPE_BOTH")
                    });
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_SOURCE_SHADER"), styles.Label, GUILayout.Width(styles.LabelWidth));
                    sourceShader = (Shader)EditorGUILayout.ObjectField(sourceShader, typeof(Shader), false);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_TARGET"), styles.Label, GUILayout.Width(styles.LabelWidth));
                    targetShader = (Shader)EditorGUILayout.ObjectField(targetShader, typeof(Shader), false);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    dryRun = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_Shd_DRY_RUN"), dryRun, GUILayout.Width(styles.ToggleWidth));
                }

                GUILayout.Space(8f);
                DrawSelectionSummary(styles);

                GUILayout.Space(8f);
                GUILayout.Label(AP_Loc.T("AP_Shd_SCAN_ACTIONS"), styles.SubTitle);
                GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_Shd_SCAN_STANDARD"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        ScanStandardMaterials();
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_Shd_SCAN_SELECTED_ALL"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        ScanSelectedHierarchyMaterials(false);
                    }

                    using (new EditorGUI.DisabledScope(sourceShader == null))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_SCAN_SELECTED_MATCHING"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            ScanSelectedHierarchyMaterials(true);
                        }
                    }
                }

                GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_Shd_CLEAR_RESULTS"), styles.SecondaryButton, GUILayout.Width(styles.SmallButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                    {
                        ScanResults.Clear();
                        lastScanContext = string.Empty;
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_Shd_SELECT_RESULTS"), styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                    {
                        SelectScannedMaterials();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(4f);
                GUILayout.Label(AP_Loc.T("AP_Shd_REPLACE_SELECTION_ACTIONS"), styles.SubTitle);
                GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_REPLACE_SELECTED_ALL"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            ReplaceSelectedHierarchyNow(false);
                        }
                    }

                    using (new EditorGUI.DisabledScope(Selection.transforms == null || Selection.transforms.Length == 0 || sourceShader == null))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_REPLACE_SELECTED_MATCHING"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            ReplaceSelectedHierarchyNow(true);
                        }
                    }
                }

                GUILayout.Space(8f);
                GUILayout.Label(AP_Loc.T("AP_Shd_RESULTS"), styles.SubTitle);
                if (!string.IsNullOrWhiteSpace(lastScanContext))
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(lastScanContext, styles.Note);
                }

                if (ScanResults.Count == 0)
                {
                    EditorGUILayout.HelpBox(AP_Loc.T("AP_Shd_RESULTS_NONE"), MessageType.Info);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label(AP_Loc.T("AP_Shd_RESULTS_COUNT"), styles.Label, GUILayout.Width(styles.LabelWidth));
                        GUILayout.Label(ScanResults.Count.ToString(), styles.Value);
                    }

                    float listHeight = Mathf.Clamp(152f + (styles.FontSize - 11f) * 14f, 152f, 300f);
                    resultsScroll = EditorGUILayout.BeginScrollView(resultsScroll, GUILayout.Height(listHeight));
                    for (int i = 0; i < ScanResults.Count; i++)
                    {
                        MaterialUsage usage = ScanResults[i];
                        if (usage == null || usage.material == null)
                        {
                            continue;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(usage.material, typeof(Material), false);
                            GUILayout.Label((usage.sourceShader != null ? usage.sourceShader.name : AP_Loc.T("AP_Shd_SHADER_UNKNOWN")), styles.Value, GUILayout.Width(styles.SourceShaderWidth));
                            GUILayout.Label(AP_Loc.T("AP_Shd_USES") + ": " + usage.useCount, styles.Value, GUILayout.Width(styles.UseWidth));
                            if (GUILayout.Button(AP_Loc.T("AP_Shd_PING"), styles.SecondaryButton, GUILayout.Width(styles.PingWidth), GUILayout.Height(styles.ButtonHeight)))
                            {
                                EditorGUIUtility.PingObject(usage.material);
                                Selection.activeObject = usage.material;
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }

                GUILayout.Space(8f);
                if (GUILayout.Button(AP_Loc.T("AP_Shd_APPLY"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                {
                    ApplyReplacement();
                }
            }
        }

        private static void DrawSelectionSummary(ShaderStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_SELECTION_CARD_TITLE"), styles.SubTitle);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AP_Loc.T("AP_Shd_SELECTION_REFRESH"), styles.MiniButton, GUILayout.Width(styles.RefreshWidth), GUILayout.Height(styles.SmallButtonHeight)))
                    {
                        RefreshSelectionSummary();
                    }
                }

                GUILayout.Space(2f);
                GUILayout.Space(6f);

                DrawSummaryLine(styles, "AP_Shd_SELECTION_ROOTS", CurrentSelectionSummary.rootCount.ToString());
                DrawSummaryLine(styles, "AP_Shd_SELECTION_RENDERERS", CurrentSelectionSummary.rendererCount.ToString());
                DrawSummaryLine(styles, "AP_Shd_SELECTION_SLOTS", CurrentSelectionSummary.materialSlotCount.ToString());
                DrawSummaryLine(styles, "AP_Shd_SELECTION_UNIQUE_MATERIALS", CurrentSelectionSummary.uniqueMaterialCount.ToString());
                DrawSummaryLine(styles, "AP_Shd_SELECTION_UNIQUE_SHADERS", CurrentSelectionSummary.uniqueShaderCount.ToString());

                GUILayout.Space(6f);
                GUILayout.Label(AP_Loc.T("AP_Shd_SELECTION_SHADER_LIST"), styles.Label);

                if (CurrentSelectionSummary.shaderUsage.Count == 0)
                {
                    GUILayout.Space(2f);
                    GUILayout.Label(AP_Loc.T("AP_Shd_SELECTION_SHADER_LIST_EMPTY"), styles.Note);
                }
                else
                {
                    float height = Mathf.Clamp(104f + (styles.FontSize - 11f) * 14f, 104f, 220f);
                    selectionShaderScroll = EditorGUILayout.BeginScrollView(selectionShaderScroll, GUILayout.Height(height));
                    for (int i = 0; i < CurrentSelectionSummary.shaderUsage.Count; i++)
                    {
                        ShaderUsage usage = CurrentSelectionSummary.shaderUsage[i];
                        if (usage == null || usage.shader == null)
                        {
                            continue;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(usage.shader, typeof(Shader), false);
                            GUILayout.Label(AP_Loc.T("AP_Shd_USES") + ": " + usage.useCount, styles.Value, GUILayout.Width(styles.UseWidth));
                            if (GUILayout.Button(AP_Loc.T("AP_Shd_PING"), styles.SecondaryButton, GUILayout.Width(styles.PingWidth), GUILayout.Height(styles.ButtonHeight)))
                            {
                                EditorGUIUtility.PingObject(usage.shader);
                                Selection.activeObject = usage.shader;
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private static void DrawSummaryLine(ShaderStyles styles, string labelKey, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(AP_Loc.T(labelKey), styles.Label, GUILayout.Width(styles.LabelWidth));
                GUILayout.Label(value, styles.Value);
            }
        }

        private static void DrawInstallerCard(ShaderStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_Shd_INSTALL_CARD_TITLE"), styles.CardTitle);

                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_INSTALL_PICKER"), styles.Label, GUILayout.Width(styles.LabelWidth));
                    selectedInstallIndex = Mathf.Clamp(selectedInstallIndex, 0, Mathf.Max(0, InstallEntries.Count - 1));
                    string[] names = InstallEntries.Select(x => AP_Loc.T(x.nameLocKey)).ToArray();
                    selectedInstallIndex = EditorGUILayout.Popup(selectedInstallIndex, names);
                }

                GUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_REFRESH"), styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                    {
                        RefreshInstallState();
                    }

                    GUILayout.FlexibleSpace();
                }

                if (!string.IsNullOrWhiteSpace(installStatus))
                {
                    EditorGUILayout.HelpBox(installStatus, MessageType.Info);
                }

                if (InstallEntries.Count == 0)
                {
                    return;
                }

                ShaderInstallEntry entry = InstallEntries[Mathf.Clamp(selectedInstallIndex, 0, InstallEntries.Count - 1)];
                DrawInstallerEntry(entry, styles);
            }
        }

        private static void DrawInstallerEntry(ShaderInstallEntry entry, ShaderStyles styles)
        {
            if (entry == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T(entry.nameLocKey), styles.SubTitle);
                    GUILayout.FlexibleSpace();
                    AP_EUI.DrawStatusPill(AP_Loc.T(entry.installed ? "AP_Shd_INSTALL_STATUS_INSTALLED" : "AP_Shd_INSTALL_STATUS_MISSING"), entry.installed ? MessageType.Info : MessageType.Warning);
                }

                GUILayout.Space(2f);
                GUILayout.Label(AP_Loc.T(entry.descLocKey), styles.Note);
                GUILayout.Space(8f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_Shd_SUPPORT_VRCLV"), styles.Label, GUILayout.Width(styles.SupportLabelWidth));
                    DrawSupportPill(entry.vrclvSupport);
                    GUILayout.Space(6f);
                    GUILayout.Label(AP_Loc.T("AP_Shd_SUPPORT_MONOSH"), styles.Label, GUILayout.Width(styles.SupportLabelWidth));
                    DrawSupportPill(entry.monoShSupport);
                    GUILayout.Space(6f);
                    GUILayout.Label(AP_Loc.T("AP_Shd_SUPPORT_SHRNM"), styles.Label, GUILayout.Width(styles.SupportLabelWidth));
                    DrawSupportPill(entry.shRnmSupport);
                }

                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!string.IsNullOrWhiteSpace(entry.upmGitUrl))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_UPM"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            StartUpmInstall(entry);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(entry.listingUrl))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_LISTING"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            Application.OpenURL(entry.listingUrl);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(entry.repoUrl))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_REPO"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            Application.OpenURL(entry.repoUrl);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(entry.guideUrl))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_GUIDE"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            Application.OpenURL(entry.guideUrl);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(entry.storeUrl))
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_STORE"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                        {
                            Application.OpenURL(entry.storeUrl);
                        }
                    }
                }

                string preferredUrl = !string.IsNullOrWhiteSpace(entry.upmGitUrl)
                    ? entry.upmGitUrl
                    : (!string.IsNullOrWhiteSpace(entry.listingUrl)
                        ? entry.listingUrl
                        : (!string.IsNullOrWhiteSpace(entry.repoUrl)
                            ? entry.repoUrl
                            : (!string.IsNullOrWhiteSpace(entry.storeUrl) ? entry.storeUrl : entry.guideUrl)));

                if (!string.IsNullOrWhiteSpace(preferredUrl))
                {
                    GUILayout.Space(6f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(AP_Loc.T("AP_Shd_INSTALL_COPY_URL"), styles.MiniButton, GUILayout.Width(styles.UrlLabelWidth), GUILayout.Height(styles.SmallButtonHeight)))
                        {
                            CopyUrl(preferredUrl);
                        }

                        EditorGUILayout.SelectableLabel(preferredUrl, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                }
            }
        }

        private static void DrawSupportPill(SupportState state)
        {
            string text;
            MessageType type;
            switch (state)
            {
                case SupportState.Yes:
                    text = AP_Loc.T("AP_Shd_SUPPORT_YES");
                    type = MessageType.Info;
                    break;
                case SupportState.No:
                    text = AP_Loc.T("AP_Shd_SUPPORT_NO");
                    type = MessageType.Error;
                    break;
                default:
                    text = AP_Loc.T("AP_Shd_SUPPORT_UNKNOWN");
                    type = MessageType.Warning;
                    break;
            }

            AP_EUI.DrawStatusPill(text, type);
        }

        private static void ScanStandardMaterials()
        {
            Dictionary<Material, MaterialUsage> map = new Dictionary<Material, MaterialUsage>();
            foreach (Renderer renderer in EnumerateRenderers(scopeMode, false))
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                {
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || material.shader == null)
                    {
                        continue;
                    }

                    if (!string.Equals(material.shader.name, "Standard", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!map.TryGetValue(material, out MaterialUsage usage))
                    {
                        usage = new MaterialUsage
                        {
                            material = material,
                            sourceShader = material.shader,
                            useCount = 0
                        };
                        map.Add(material, usage);
                    }

                    usage.useCount++;
                }
            }

            ReplaceScanResults(map.Values, AP_Loc.T("AP_Shd_SCAN_CONTEXT_STANDARD"));
        }

        private static void ScanSelectedHierarchyMaterials(bool onlyMatchingSourceShader)
        {
            Dictionary<Material, MaterialUsage> map = new Dictionary<Material, MaterialUsage>();
            foreach (Renderer renderer in EnumerateRenderers(ScopeMode.Selection, true))
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                {
                    continue;
                }

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || material.shader == null)
                    {
                        continue;
                    }

                    if (onlyMatchingSourceShader && sourceShader != null && material.shader != sourceShader)
                    {
                        continue;
                    }

                    if (!map.TryGetValue(material, out MaterialUsage usage))
                    {
                        usage = new MaterialUsage
                        {
                            material = material,
                            sourceShader = material.shader,
                            useCount = 0
                        };
                        map.Add(material, usage);
                    }

                    usage.useCount++;
                }
            }

            string context = onlyMatchingSourceShader
                ? string.Format(AP_Loc.T("AP_Shd_SCAN_CONTEXT_SELECTION_FILTER"), sourceShader != null ? sourceShader.name : AP_Loc.T("AP_Shd_SHADER_UNKNOWN"))
                : AP_Loc.T("AP_Shd_SCAN_CONTEXT_SELECTION_ALL");
            ReplaceScanResults(map.Values, context);
        }

        private static void ReplaceScanResults(IEnumerable<MaterialUsage> usages, string context)
        {
            ScanResults.Clear();
            if (usages != null)
            {
                ScanResults.AddRange(usages.Where(x => x != null && x.material != null).OrderBy(x => x.material.name, StringComparer.OrdinalIgnoreCase));
            }

            lastScanContext = string.Format(AP_Loc.T("AP_Shd_SCAN_CONTEXT_FORMAT"), context, ScanResults.Count);
        }

        private static IEnumerable<Renderer> EnumerateRenderers(ScopeMode mode, bool selectionOnlyRoots)
        {
            HashSet<Renderer> seen = new HashSet<Renderer>();
            bool includeScene = mode == ScopeMode.Scene || mode == ScopeMode.Both;
            bool includeSelection = mode == ScopeMode.Selection || mode == ScopeMode.Both;

            if (includeSelection && Selection.transforms != null && Selection.transforms.Length > 0)
            {
                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    Transform root = Selection.transforms[i];
                    if (root == null)
                    {
                        continue;
                    }

                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    for (int j = 0; j < renderers.Length; j++)
                    {
                        Renderer renderer = renderers[j];
                        if (renderer != null && seen.Add(renderer))
                        {
                            yield return renderer;
                        }
                    }
                }

                if (selectionOnlyRoots)
                {
                    yield break;
                }
            }

            if (includeScene)
            {
                Scene scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                {
                    yield break;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    Renderer[] renderers = roots[i].GetComponentsInChildren<Renderer>(true);
                    for (int j = 0; j < renderers.Length; j++)
                    {
                        Renderer renderer = renderers[j];
                        if (renderer != null && seen.Add(renderer))
                        {
                            yield return renderer;
                        }
                    }
                }
            }
        }

        private static void SelectScannedMaterials()
        {
            if (ScanResults.Count == 0)
            {
                return;
            }

            Selection.objects = ScanResults.Where(x => x != null && x.material != null).Select(x => (UnityEngine.Object)x.material).ToArray();
        }

        private static void ReplaceSelectedHierarchyNow(bool onlyMatchingSourceShader)
        {
            ScanSelectedHierarchyMaterials(onlyMatchingSourceShader);
            ApplyReplacement();
        }

        private static void ApplyReplacement()
        {
            if (targetShader == null)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_Shd_PAGE_TITLE"), AP_Loc.T("AP_Shd_TARGET_MISSING"), "OK");
                return;
            }

            if (ScanResults.Count == 0)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_Shd_PAGE_TITLE"), AP_Loc.T("AP_Shd_NOTHING_SCANNED"), "OK");
                return;
            }

            if (dryRun)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_Shd_PAGE_TITLE"), string.Format(AP_Loc.T("AP_Shd_DRYRUN_DONE"), ScanResults.Count), "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(AP_Loc.T("AP_Shd_PAGE_TITLE"), string.Format(AP_Loc.T("AP_Shd_APPLY_CONFIRM"), ScanResults.Count), AP_Loc.T("AP_Shd_APPLY"), "Cancel"))
            {
                return;
            }

            int changed = 0;
            try
            {
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                for (int i = 0; i < ScanResults.Count; i++)
                {
                    MaterialUsage usage = ScanResults[i];
                    Material material = usage != null ? usage.material : null;
                    if (material == null)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(AP_Loc.T("AP_Shd_PAGE_TITLE"), material.name, (i + 1f) / Mathf.Max(1f, ScanResults.Count));
                    MaterialSnapshot snapshot = CaptureSnapshot(material);
                    Undo.RecordObject(material, "AP Replace Shader");
                    material.shader = targetShader;
                    ApplySnapshot(material, snapshot);
                    EditorUtility.SetDirty(material);
                    changed++;
                }

                Undo.CollapseUndoOperations(undoGroup);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(AP_Loc.T("AP_Shd_PAGE_TITLE"), string.Format(AP_Loc.T("AP_Shd_APPLY_DONE"), changed), "OK");
            RefreshSelectionSummary();
        }

        private static MaterialSnapshot CaptureSnapshot(Material material)
        {
            MaterialSnapshot snapshot = new MaterialSnapshot();
            if (material == null)
            {
                return snapshot;
            }

            for (int i = 0; i < CommonColorProperties.Length; i++)
            {
                string property = CommonColorProperties[i];
                if (material.HasProperty(property))
                {
                    snapshot.colors[property] = material.GetColor(property);
                }
            }

            for (int i = 0; i < CommonFloatProperties.Length; i++)
            {
                string property = CommonFloatProperties[i];
                if (material.HasProperty(property))
                {
                    snapshot.floats[property] = material.GetFloat(property);
                }
            }

            for (int i = 0; i < CommonTextureProperties.Length; i++)
            {
                string property = CommonTextureProperties[i];
                if (!material.HasProperty(property))
                {
                    continue;
                }

                snapshot.textures[property] = material.GetTexture(property);
                snapshot.textureScales[property] = material.GetTextureScale(property);
                snapshot.textureOffsets[property] = material.GetTextureOffset(property);
            }

            return snapshot;
        }

        private static void ApplySnapshot(Material material, MaterialSnapshot snapshot)
        {
            if (material == null || snapshot == null)
            {
                return;
            }

            foreach (KeyValuePair<string, Color> pair in snapshot.colors)
            {
                if (material.HasProperty(pair.Key))
                {
                    material.SetColor(pair.Key, pair.Value);
                }
            }

            foreach (KeyValuePair<string, float> pair in snapshot.floats)
            {
                if (material.HasProperty(pair.Key))
                {
                    material.SetFloat(pair.Key, pair.Value);
                }
            }

            foreach (KeyValuePair<string, Texture> pair in snapshot.textures)
            {
                if (!material.HasProperty(pair.Key))
                {
                    continue;
                }

                material.SetTexture(pair.Key, pair.Value);
                if (snapshot.textureScales.TryGetValue(pair.Key, out Vector2 scale))
                {
                    material.SetTextureScale(pair.Key, scale);
                }

                if (snapshot.textureOffsets.TryGetValue(pair.Key, out Vector2 offset))
                {
                    material.SetTextureOffset(pair.Key, offset);
                }
            }
        }

        private static void StartUpmInstall(ShaderInstallEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.upmGitUrl))
            {
                return;
            }

            if (addRequest != null)
            {
                installStatus = AP_Loc.T("AP_Shd_INSTALL_BUSY");
                return;
            }

            activeInstallEntry = entry;
            installStatus = string.Format(AP_Loc.T("AP_Shd_INSTALL_ADDING"), AP_Loc.T(entry.nameLocKey));
            addRequest = Client.Add(entry.upmGitUrl);
            EditorApplication.update -= PollAddRequest;
            EditorApplication.update += PollAddRequest;
        }

        private static void PollAddRequest()
        {
            if (addRequest == null)
            {
                EditorApplication.update -= PollAddRequest;
                return;
            }

            if (!addRequest.IsCompleted)
            {
                return;
            }

            if (addRequest.Status == StatusCode.Success)
            {
                installStatus = string.Format(AP_Loc.T("AP_Shd_INSTALL_SUCCESS"), activeInstallEntry != null ? AP_Loc.T(activeInstallEntry.nameLocKey) : string.Empty);
            }
            else
            {
                installStatus = string.Format(AP_Loc.T("AP_Shd_INSTALL_FAIL"), activeInstallEntry != null ? AP_Loc.T(activeInstallEntry.nameLocKey) : string.Empty);
            }

            addRequest = null;
            activeInstallEntry = null;
            EditorApplication.update -= PollAddRequest;
            RefreshInstallState();
        }

        private static void CopyUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            EditorGUIUtility.systemCopyBuffer = url;
            installStatus = string.Format(AP_Loc.T("AP_Shd_INSTALL_URL_COPIED"), url);
        }

        private static void RefreshInstallStateIfNeeded()
        {
            if (EditorApplication.timeSinceStartup - lastStatusRefreshTime > 4d)
            {
                RefreshInstallState();
            }
        }

        private static void RefreshInstallState()
        {
            lastStatusRefreshTime = EditorApplication.timeSinceStartup;
            string[] paths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < InstallEntries.Count; i++)
            {
                ShaderInstallEntry entry = InstallEntries[i];
                entry.installed = DetectInstalled(entry, paths);
            }
        }

        private static bool DetectInstalled(ShaderInstallEntry entry, string[] paths)
        {
            if (entry == null)
            {
                return false;
            }

            if (paths != null && entry.pathHints != null)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    string path = paths[i];
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    string lowerPath = path.ToLowerInvariant();
                    for (int j = 0; j < entry.pathHints.Length; j++)
                    {
                        string hint = entry.pathHints[j];
                        if (!string.IsNullOrWhiteSpace(hint) && lowerPath.Contains(hint.ToLowerInvariant()))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void RefreshSelectionSummaryIfNeeded()
        {
            int hash = BuildSelectionHash();
            if (hash != lastSelectionHash || EditorApplication.timeSinceStartup - lastSelectionSummaryTime > 1.2d)
            {
                RefreshSelectionSummary();
            }
        }

        private static int BuildSelectionHash()
        {
            int hash = 17;
            Transform[] transforms = Selection.transforms;
            if (transforms == null)
            {
                return hash;
            }

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                hash = (hash * 31) + (transform != null ? transform.GetInstanceID() : 0);
            }

            return hash;
        }

        private static void RefreshSelectionSummary()
        {
            lastSelectionSummaryTime = EditorApplication.timeSinceStartup;
            lastSelectionHash = BuildSelectionHash();

            CurrentSelectionSummary.rootCount = 0;
            CurrentSelectionSummary.rendererCount = 0;
            CurrentSelectionSummary.materialSlotCount = 0;
            CurrentSelectionSummary.uniqueMaterialCount = 0;
            CurrentSelectionSummary.uniqueShaderCount = 0;
            CurrentSelectionSummary.shaderUsage.Clear();

            Transform[] roots = Selection.transforms;
            if (roots == null || roots.Length == 0)
            {
                return;
            }

            CurrentSelectionSummary.rootCount = roots.Length;
            HashSet<Renderer> seenRenderers = new HashSet<Renderer>();
            HashSet<Material> uniqueMaterials = new HashSet<Material>();
            Dictionary<Shader, int> shaderUses = new Dictionary<Shader, int>();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform root = roots[i];
                if (root == null)
                {
                    continue;
                }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    Renderer renderer = renderers[j];
                    if (renderer == null || !seenRenderers.Add(renderer))
                    {
                        continue;
                    }

                    CurrentSelectionSummary.rendererCount++;
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null)
                    {
                        continue;
                    }

                    for (int k = 0; k < materials.Length; k++)
                    {
                        Material material = materials[k];
                        if (material == null)
                        {
                            continue;
                        }

                        CurrentSelectionSummary.materialSlotCount++;
                        uniqueMaterials.Add(material);
                        Shader shader = material.shader;
                        if (shader == null)
                        {
                            continue;
                        }

                        shaderUses.TryGetValue(shader, out int uses);
                        shaderUses[shader] = uses + 1;
                    }
                }
            }

            CurrentSelectionSummary.uniqueMaterialCount = uniqueMaterials.Count;
            CurrentSelectionSummary.uniqueShaderCount = shaderUses.Count;
            foreach (KeyValuePair<Shader, int> pair in shaderUses.OrderByDescending(x => x.Value).ThenBy(x => x.Key != null ? x.Key.name : string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                CurrentSelectionSummary.shaderUsage.Add(new ShaderUsage
                {
                    shader = pair.Key,
                    useCount = pair.Value
                });
            }
        }

        private readonly struct ShaderStyles
        {
            public readonly GUIStyle SectionTitle;
            public readonly GUIStyle CardTitle;
            public readonly GUIStyle SubTitle;
            public readonly GUIStyle Note;
            public readonly GUIStyle Label;
            public readonly GUIStyle Value;
            public readonly GUIStyle ActionButton;
            public readonly GUIStyle SecondaryButton;
            public readonly GUIStyle MiniButton;
            public readonly int FontSize;
            public readonly float ButtonHeight;
            public readonly float SmallButtonHeight;
            public readonly float LabelWidth;
            public readonly float ToggleWidth;
            public readonly float SelectButtonWidth;
            public readonly float SmallButtonWidth;
            public readonly float UseWidth;
            public readonly float PingWidth;
            public readonly float UrlLabelWidth;
            public readonly float SourceShaderWidth;
            public readonly float RefreshWidth;
            public readonly float SupportLabelWidth;

            private ShaderStyles(
                GUIStyle sectionTitle,
                GUIStyle cardTitle,
                GUIStyle subTitle,
                GUIStyle note,
                GUIStyle label,
                GUIStyle value,
                GUIStyle actionButton,
                GUIStyle secondaryButton,
                GUIStyle miniButton,
                int fontSize,
                float buttonHeight,
                float smallButtonHeight,
                float labelWidth,
                float toggleWidth,
                float selectButtonWidth,
                float smallButtonWidth,
                float useWidth,
                float pingWidth,
                float urlLabelWidth,
                float sourceShaderWidth,
                float refreshWidth,
                float supportLabelWidth)
            {
                SectionTitle = sectionTitle;
                CardTitle = cardTitle;
                SubTitle = subTitle;
                Note = note;
                Label = label;
                Value = value;
                ActionButton = actionButton;
                SecondaryButton = secondaryButton;
                MiniButton = miniButton;
                FontSize = fontSize;
                ButtonHeight = buttonHeight;
                SmallButtonHeight = smallButtonHeight;
                LabelWidth = labelWidth;
                ToggleWidth = toggleWidth;
                SelectButtonWidth = selectButtonWidth;
                SmallButtonWidth = smallButtonWidth;
                UseWidth = useWidth;
                PingWidth = pingWidth;
                UrlLabelWidth = urlLabelWidth;
                SourceShaderWidth = sourceShaderWidth;
                RefreshWidth = refreshWidth;
                SupportLabelWidth = supportLabelWidth;
            }

            public static ShaderStyles Create(AP_Main hostWindow)
            {
                int baseSize = hostWindow != null ? hostWindow.GetHostContentFontSize() : 11;
                int bodySize = Mathf.Clamp(baseSize, 10, 18);
                int sectionSize = Mathf.Clamp(bodySize + 3, 12, 21);
                int cardSize = Mathf.Clamp(bodySize + 1, 11, 18);

                GUIStyle sectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = sectionSize,
                    wordWrap = true
                };

                GUIStyle cardTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = cardSize,
                    wordWrap = true
                };

                GUIStyle subTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = bodySize,
                    wordWrap = true
                };

                GUIStyle note = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    fontSize = Mathf.Clamp(bodySize - 1, 9, 16),
                    wordWrap = true
                };

                GUIStyle label = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    wordWrap = false
                };

                GUIStyle value = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleLeft
                };

                GUIStyle actionButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleCenter
                };

                GUIStyle secondaryButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.Clamp(bodySize - 1, 9, 16),
                    alignment = TextAnchor.MiddleCenter
                };

                GUIStyle miniButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.Clamp(bodySize - 2, 8, 15),
                    alignment = TextAnchor.MiddleCenter
                };

                float buttonHeight = Mathf.Clamp(22f + (bodySize - 11f) * 2f, 22f, 36f);
                float smallButtonHeight = Mathf.Clamp(19f + (bodySize - 11f) * 1.5f, 19f, 30f);
                float labelWidth = Mathf.Clamp(168f + (bodySize - 11f) * 10f, 168f, 300f);
                float toggleWidth = Mathf.Clamp(124f + (bodySize - 11f) * 8f, 124f, 220f);
                float selectButtonWidth = Mathf.Clamp(128f + (bodySize - 11f) * 8f, 128f, 220f);
                float smallButtonWidth = Mathf.Clamp(112f + (bodySize - 11f) * 7f, 112f, 188f);
                float useWidth = Mathf.Clamp(110f + (bodySize - 11f) * 7f, 110f, 180f);
                float pingWidth = Mathf.Clamp(58f + (bodySize - 11f) * 6f, 58f, 96f);
                float urlLabelWidth = Mathf.Clamp(96f + (bodySize - 11f) * 6f, 96f, 160f);
                float sourceShaderWidth = Mathf.Clamp(150f + (bodySize - 11f) * 8f, 150f, 280f);
                float refreshWidth = Mathf.Clamp(90f + (bodySize - 11f) * 6f, 90f, 160f);
                float supportLabelWidth = Mathf.Clamp(58f + (bodySize - 11f) * 4f, 58f, 120f);

                return new ShaderStyles(sectionTitle, cardTitle, subTitle, note, label, value, actionButton, secondaryButton, miniButton, bodySize, buttonHeight, smallButtonHeight, labelWidth, toggleWidth, selectButtonWidth, smallButtonWidth, useWidth, pingWidth, urlLabelWidth, sourceShaderWidth, refreshWidth, supportLabelWidth);
            }
        }
    }
}
#endif
