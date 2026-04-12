#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    public sealed class AP_Main : EditorWindow
    {
        private const string MenuPath = "2dAngel/AngelPanel/Main Panel";
        private const float MinWindowWidth = 460f;
        private const float MinWindowHeight = 420f;

        [SerializeField] private string currentPageId = AP_ModuleIds.Home;
        [SerializeField] private Vector2 contentScroll;
        [SerializeField] private Vector2 navigationScroll;
        [SerializeField] private Vector2 horizontalNavigationScroll;
        [SerializeField] private AP_MainWorkspace mainWorkspace = new AP_MainWorkspace();

        private AP_CoreConfigData hostConfig;
        private bool initialized;

        [MenuItem(MenuPath, false, 10)]
        public static void Open()
        {
            AP_Main window = GetWindow<AP_Main>();
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.InitializeHost();
            window.Show();
            window.Focus();
        }

        public static void OpenTab(string moduleId)
        {
            AP_Main window = GetWindow<AP_Main>();
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.InitializeHost();
            window.OpenPage(MapLegacyPageId(moduleId), true);
            window.Show();
            window.Focus();
        }

        public static bool ExecuteHostCommand(string commandRoute)
        {
            AP_Main window = GetWindow<AP_Main>();
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.InitializeHost();
            bool handled = window.ExecuteHostCommandInternal(commandRoute);
            if (handled)
            {
                window.Show();
                window.Focus();
                window.Repaint();
            }

            return handled;
        }

        private void OnEnable()
        {
            InitializeHost();

            Selection.selectionChanged -= HandleSelectionChanged;
            Selection.selectionChanged += HandleSelectionChanged;

            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;

            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += HandleActiveSceneChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
            EditorSceneManager.activeSceneChangedInEditMode -= HandleActiveSceneChanged;
            SaveHostConfig();
        }

        public void MarkWelcomeSeen()
        {
            InitializeHost();
            if (hostConfig.hasSeenWelcome)
            {
                return;
            }

            hostConfig.hasSeenWelcome = true;
            currentPageId = AP_ModuleIds.Home;
            SaveHostConfig();
            Repaint();
        }

        public void NotifyHostConfigChanged()
        {
            InitializeHost();
            hostConfig.Sanitize();
            SaveHostConfig();
            Repaint();
        }

        public bool IsHorizontalNavigationLayout()
        {
            InitializeHost();
            return hostConfig != null && (hostConfig.navigationPlacement == AP_HostNavigationPlacement.Top || hostConfig.navigationPlacement == AP_HostNavigationPlacement.Bottom);
        }

        public int GetHostContentFontSize()
        {
            InitializeHost();
            return GetNavigationFontSize();
        }

        private void InitializeHost()
        {
            if (initialized && hostConfig != null)
            {
                hostConfig.Sanitize();
                EnsureValidPageSelection();
                return;
            }

            AP_CoreBootstrap.EnsureBootstrapped();
            AP_Loc.Init();

            mainWorkspace = mainWorkspace ?? new AP_MainWorkspace();
            mainWorkspace.Initialize();
            mainWorkspace.RefreshSelection();
            mainWorkspace.MarkSceneCountsDirty();

            hostConfig = new AP_CoreConfigData();
            if (AP_CoreStorage.TryLoad(AP_CorePaths.MainConfigFilePath, out AP_CoreConfigData loaded) && loaded != null)
            {
                hostConfig = loaded;
            }

            hostConfig.Sanitize();
            currentPageId = hostConfig.hasSeenWelcome ? NormalizePageId(hostConfig.lastPageId) : AP_ModuleIds.About;
            EnsureValidPageSelection();
            UpdateWindowTitle();
            initialized = true;
        }

        private void SaveHostConfig()
        {
            if (!initialized || hostConfig == null)
            {
                return;
            }

            hostConfig.Sanitize();
            hostConfig.lastPageId = NormalizePageId(currentPageId);
            AP_CoreStorage.TrySave(AP_CorePaths.MainConfigFilePath, hostConfig);
        }

        private void HandleSelectionChanged()
        {
            mainWorkspace = mainWorkspace ?? new AP_MainWorkspace();
            mainWorkspace.RefreshSelection();
            Repaint();
        }

        private void HandleHierarchyChanged()
        {
            mainWorkspace = mainWorkspace ?? new AP_MainWorkspace();
            mainWorkspace.MarkSceneCountsDirty();
            Repaint();
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            mainWorkspace = mainWorkspace ?? new AP_MainWorkspace();
            mainWorkspace.MarkSceneCountsDirty();
            mainWorkspace.RefreshSelection();
            Repaint();
        }

        private void OnGUI()
        {
            InitializeHost();
            DrawToolbar();
            DrawShell();
        }

        private void OnInspectorUpdate()
        {
            if (mainWorkspace != null && mainWorkspace.TickAutoRefresh())
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Clamp(GetNavigationFontSize() + 1, 11, 18)
            };

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(AP_Loc.T("AP_MP_WINDOW_TITLE"), titleStyle);
                GUILayout.Space(8f);
                GUILayout.Label(AP_CoreInfo.HostVersionLabel, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (IsHorizontalNavigationLayout())
                {
                    DrawInfoPopup();
                    GUILayout.Space(10f);
                }

                GUILayout.Label(AP_Loc.T("AP_AP_LANGUAGE"), GUILayout.Width(52f + Mathf.Clamp(GetNavigationFontSize() - 10, 0, 6) * 6f));

                int currentLanguage = AP_Loc.LangIndex;
                int nextLanguage = EditorGUILayout.Popup(
                    currentLanguage,
                    AP_Loc.LangNames,
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(Mathf.Clamp(84f + GetNavigationFontSize() * 4f, 108f, 156f)));

                if (nextLanguage != currentLanguage)
                {
                    AP_Loc.SetLangIndex(nextLanguage);
                    UpdateWindowTitle();
                    Repaint();
                }
            }
        }

        private void DrawInfoPopup()
        {
            string[] options =
            {
                AP_Loc.T("AP_MP_INFO_PICKER_NONE"),
                AP_Loc.T("AP_MP_TAB_ABOUT"),
                AP_Loc.T("AP_Cfg_TAB")
            };

            int selectedIndex = 0;
            if (string.Equals(currentPageId, AP_ModuleIds.About, StringComparison.Ordinal))
            {
                selectedIndex = 1;
            }
            else if (string.Equals(currentPageId, AP_ModuleIds.Config, StringComparison.Ordinal))
            {
                selectedIndex = 2;
            }

            GUILayout.Label(AP_Loc.T("AP_MP_INFO_PICKER"), GUILayout.Width(40f + Mathf.Clamp(GetNavigationFontSize() - 10, 0, 6) * 8f));
            int nextIndex = EditorGUILayout.Popup(selectedIndex, options, EditorStyles.toolbarPopup, GUILayout.Width(Mathf.Clamp(78f + GetNavigationFontSize() * 5f, 108f, 156f)));
            if (nextIndex == selectedIndex || nextIndex <= 0)
            {
                return;
            }

            OpenPage(nextIndex == 1 ? AP_ModuleIds.About : AP_ModuleIds.Config, true);
            GUI.FocusControl(null);
        }

        private void DrawShell()
        {
            switch (hostConfig.navigationPlacement)
            {
                case AP_HostNavigationPlacement.Right:
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawContent();
                        GUILayout.Space(4f);
                        DrawVerticalNavigationPanel();
                    }
                    break;

                case AP_HostNavigationPlacement.Top:
                    DrawHorizontalNavigationPanel();
                    GUILayout.Space(4f);
                    DrawContent();
                    break;

                case AP_HostNavigationPlacement.Bottom:
                    DrawContent();
                    GUILayout.Space(4f);
                    DrawHorizontalNavigationPanel();
                    break;

                default:
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawVerticalNavigationPanel();
                        GUILayout.Space(4f);
                        DrawContent();
                    }
                    break;
            }
        }

        private void DrawVerticalNavigationPanel()
        {
            float navigationWidth = GetNavigationPanelWidth();
            List<AP_ModuleManifest> corePages = GetPagesBySection(AP_HostSection.Core);
            List<AP_ModuleManifest> optimizingPages = GetPagesBySection(AP_HostSection.Optimizing);
            List<AP_ModuleManifest> toolPages = GetPagesBySection(AP_HostSection.Tools);
            List<AP_ModuleManifest> infoPages = GetPagesBySection(AP_HostSection.Info);

            using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(navigationWidth)))
            {
                navigationScroll = EditorGUILayout.BeginScrollView(navigationScroll);
                DrawNavigationGroupVertical(AP_Loc.T("AP_MP_SIDEBAR_CORE"), corePages);

                if (optimizingPages.Count > 0)
                {
                    GUILayout.Space(6f);
                    DrawNavigationGroupVertical(AP_Loc.T("AP_MP_SIDEBAR_OPTIMIZING"), optimizingPages);
                }

                if (toolPages.Count > 0)
                {
                    GUILayout.Space(6f);
                    DrawNavigationGroupVertical(AP_Loc.T("AP_MP_SIDEBAR_TOOLS"), toolPages);
                }

                if (infoPages.Count > 0)
                {
                    GUILayout.Space(6f);
                    DrawNavigationGroupVertical(AP_Loc.T("AP_MP_SIDEBAR_INFO"), infoPages);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawHorizontalNavigationPanel()
        {
            List<AP_ModuleManifest> pages = GetPrimaryPageGroup();
            if (pages == null || pages.Count == 0)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (hostConfig.navigationOverflowMode == AP_HostNavigationOverflowMode.Scroll)
                {
                    DrawHorizontalScrollStrip(pages);
                }
                else
                {
                    DrawHorizontalCompactStrip(pages);
                }
            }
        }

        private void DrawNavigationGroupVertical(string title, List<AP_ModuleManifest> pages)
        {
            if (pages == null || pages.Count == 0)
            {
                return;
            }

            GUILayout.Label(title, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    DrawNavigationButton(pages[i], GUILayout.ExpandWidth(true), GUILayout.Height(GetNavigationButtonHeight()));
                }
            }
        }

        private void DrawHorizontalCompactStrip(List<AP_ModuleManifest> pages)
        {
            float availableWidth = Mathf.Max(220f, position.width - 20f);
            float gap = 4f;
            float height = GetNavigationButtonHeight();
            int count = Mathf.Max(1, pages.Count);
            float buttonWidth = Mathf.Clamp((availableWidth - gap * (count - 1f)) / count, 56f, 220f);

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (i > 0)
                    {
                        GUILayout.Space(gap);
                    }

                    DrawNavigationButton(pages[i], GUILayout.Width(buttonWidth), GUILayout.Height(height));
                }
            }
        }

        private void DrawHorizontalScrollStrip(List<AP_ModuleManifest> pages)
        {
            float height = GetNavigationButtonHeight() + 8f;
            horizontalNavigationScroll = EditorGUILayout.BeginScrollView(horizontalNavigationScroll, false, false, GUILayout.Height(height));
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    if (i > 0)
                    {
                        GUILayout.Space(4f);
                    }

                    GUIStyle buttonStyle = GetNavigationButtonStyle();
                    GUIContent content = new GUIContent(AP_Loc.T(pages[i].displayNameLocKey));
                    float width = Mathf.Max(84f, buttonStyle.CalcSize(content).x + 22f);
                    DrawNavigationButton(pages[i], GUILayout.Width(width), GUILayout.Height(GetNavigationButtonHeight()));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNavigationButton(AP_ModuleManifest manifest, params GUILayoutOption[] options)
        {
            if (manifest == null)
            {
                return;
            }

            bool selected = IsNavigationSelection(manifest.moduleId);
            Color oldBackground = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.20f, 0.42f, 0.74f, 1f);
            }

            if (GUILayout.Button(AP_Loc.T(manifest.displayNameLocKey), GetNavigationButtonStyle(), options))
            {
                OpenPage(manifest.moduleId, true);
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = oldBackground;
        }

        private bool IsNavigationSelection(string navigationId)
        {
            if (string.Equals(currentPageId, navigationId, StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(navigationId, AP_ModuleIds.Tools, StringComparison.Ordinal) && AP_ModuleRegistry.IsExternalTool(GetCurrentManifest()))
            {
                return true;
            }

            return false;
        }

        private AP_ModuleManifest GetCurrentManifest()
        {
            AP_ModuleRegistry.TryGet(currentPageId, out AP_ModuleManifest manifest);
            return manifest;
        }

        private void DrawContent()
        {
            float widthForContext = Mathf.Max(260f, position.width - GetReservedNavigationWidth() - 20f);

            using (new EditorGUILayout.VerticalScope())
            {
                contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
                DrawCurrentPage(widthForContext);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCurrentPage(float widthForContext)
        {
            if (!AP_ModuleRegistry.TryGet(currentPageId, out AP_ModuleManifest manifest) || manifest == null)
            {
                EditorGUILayout.HelpBox(AP_Loc.T("AP_MP_CONTENT_NOT_FOUND"), MessageType.Warning);
                return;
            }

            AP_HostContext context = new AP_HostContext
            {
                HostWindow = this,
                Workspace = mainWorkspace,
                Config = hostConfig,
                WindowWidth = widthForContext,
                ContentWidth = AP_EUI.GetContentWidth(widthForContext, 18f)
            };

            manifest.drawHostPage?.Invoke(context);
        }

        private List<AP_ModuleManifest> GetPagesBySection(AP_HostSection section)
        {
            List<AP_ModuleManifest> pages = new List<AP_ModuleManifest>();
            IReadOnlyList<AP_ModuleManifest> visible = AP_ModuleRegistry.GetVisibleHostModules();
            for (int i = 0; i < visible.Count; i++)
            {
                AP_ModuleManifest manifest = visible[i];
                if (manifest == null)
                {
                    continue;
                }

                AP_HostSection manifestSection = GetHostSection(manifest);
                if (manifestSection != section)
                {
                    continue;
                }

                if (section == AP_HostSection.Info && !string.Equals(manifest.moduleId, AP_ModuleIds.About, StringComparison.Ordinal) && !string.Equals(manifest.moduleId, AP_ModuleIds.Config, StringComparison.Ordinal))
                {
                    continue;
                }

                if (section != AP_HostSection.Info && (string.Equals(manifest.moduleId, AP_ModuleIds.About, StringComparison.Ordinal) || string.Equals(manifest.moduleId, AP_ModuleIds.Config, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (section == AP_HostSection.Tools)
                {
                    bool isToolsOverview = string.Equals(manifest.moduleId, AP_ModuleIds.Tools, StringComparison.Ordinal);
                    bool isExternalTool = AP_ModuleRegistry.IsExternalTool(manifest);
                    if (!isToolsOverview && !isExternalTool)
                    {
                        continue;
                    }
                }

                pages.Add(manifest);
            }

            return pages;
        }

        private List<AP_ModuleManifest> GetPrimaryPageGroup()
        {
            List<AP_ModuleManifest> pages = new List<AP_ModuleManifest>();
            IReadOnlyList<AP_ModuleManifest> visible = AP_ModuleRegistry.GetVisibleHostModules();
            bool toolsAdded = false;
            for (int i = 0; i < visible.Count; i++)
            {
                AP_ModuleManifest manifest = visible[i];
                if (manifest == null)
                {
                    continue;
                }

                AP_HostSection section = GetHostSection(manifest);
                if (section == AP_HostSection.Info)
                {
                    continue;
                }

                if (section == AP_HostSection.Tools)
                {
                    bool isToolsOverview = string.Equals(manifest.moduleId, AP_ModuleIds.Tools, StringComparison.Ordinal);
                    bool isExternalTool = AP_ModuleRegistry.IsExternalTool(manifest);
                    if (!isToolsOverview && isExternalTool)
                    {
                        continue;
                    }

                    if (isToolsOverview)
                    {
                        toolsAdded = true;
                    }
                }

                pages.Add(manifest);
            }

            if (!toolsAdded && AP_ModuleRegistry.HasExternalToolModules())
            {
                AP_ModuleManifest toolsManifest;
                if (AP_ModuleRegistry.TryGet(AP_ModuleIds.Tools, out toolsManifest) && toolsManifest != null)
                {
                    pages.Add(toolsManifest);
                }
            }

            return pages;
        }

        private static AP_HostSection GetHostSection(AP_ModuleManifest manifest)
        {
            if (manifest == null)
            {
                return AP_HostSection.Tools;
            }

            if (string.Equals(manifest.moduleId, AP_ModuleIds.About, StringComparison.Ordinal) || string.Equals(manifest.moduleId, AP_ModuleIds.Config, StringComparison.Ordinal))
            {
                return AP_HostSection.Info;
            }

            return manifest.hostSection;
        }

        private float GetReservedNavigationWidth()
        {
            AP_HostNavigationPlacement placement = hostConfig.navigationPlacement;
            return placement == AP_HostNavigationPlacement.Left || placement == AP_HostNavigationPlacement.Right
                ? GetNavigationPanelWidth() + 4f
                : 0f;
        }

        private float GetNavigationPanelWidth()
        {
            float configured = Mathf.Clamp(hostConfig.navigationPanelWidth, 132f, 320f);
            float scale = GetChromeScale();
            float scaled = hostConfig.chromeScaleMode == AP_HostChromeScaleMode.Adaptive ? configured * Mathf.Lerp(0.88f, 1.0f, scale) : configured;
            float maxAllowed = Mathf.Max(132f, position.width * 0.42f);
            return Mathf.Clamp(scaled, 132f, maxAllowed);
        }

        private float GetChromeScale()
        {
            if (hostConfig == null || hostConfig.chromeScaleMode == AP_HostChromeScaleMode.Fixed)
            {
                return 1f;
            }

            return Mathf.Clamp(position.width / 960f, 0.72f, 1.08f);
        }

        private float GetNavigationButtonHeight()
        {
            float baseHeight = Mathf.Clamp(hostConfig.navigationButtonHeight, 20, 42);
            float scale = hostConfig.chromeScaleMode == AP_HostChromeScaleMode.Adaptive ? GetChromeScale() : 1f;
            return Mathf.Clamp(Mathf.Round(baseHeight * scale), 20f, 44f);
        }

        private int GetNavigationFontSize()
        {
            int baseSize = Mathf.Clamp(hostConfig.navigationFontSize, 10, 18);
            float scale = hostConfig.chromeScaleMode == AP_HostChromeScaleMode.Adaptive ? GetChromeScale() : 1f;
            return Mathf.Clamp(Mathf.RoundToInt(baseSize * scale), 10, 18);
        }

        private GUIStyle GetNavigationButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = GetNavigationFontSize(),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }

        private void OpenPage(string moduleId, bool saveConfig)
        {
            currentPageId = NormalizePageId(moduleId);
            EnsureValidPageSelection();
            if (saveConfig)
            {
                SaveHostConfig();
            }
        }

        private void EnsureValidPageSelection()
        {
            IReadOnlyList<AP_ModuleManifest> pages = AP_ModuleRegistry.GetVisibleHostModules();
            if (pages.Count == 0)
            {
                currentPageId = AP_ModuleIds.About;
                return;
            }

            for (int i = 0; i < pages.Count; i++)
            {
                if (string.Equals(pages[i].moduleId, currentPageId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            currentPageId = hostConfig != null && !hostConfig.hasSeenWelcome ? AP_ModuleIds.About : pages[0].moduleId;
        }

        private static string NormalizePageId(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                return AP_ModuleIds.Home;
            }

            string normalized = MapLegacyPageId(moduleId.Trim());
            return string.IsNullOrWhiteSpace(normalized) ? AP_ModuleIds.Home : normalized;
        }

        private static string MapLegacyPageId(string moduleId)
        {
            return string.Equals(moduleId, "main", StringComparison.OrdinalIgnoreCase)
                ? AP_ModuleIds.Home
                : moduleId;
        }

        private bool ExecuteHostCommandInternal(string commandRoute)
        {
            if (string.IsNullOrWhiteSpace(commandRoute))
            {
                return false;
            }

            string route = commandRoute.Trim();
            if (string.Equals(route, "HOST.OPEN.HOME", StringComparison.OrdinalIgnoreCase))
            {
                OpenPage(AP_ModuleIds.Home, true);
                return true;
            }

            if (string.Equals(route, "HOST.OPEN.ABOUT", StringComparison.OrdinalIgnoreCase))
            {
                OpenPage(AP_ModuleIds.About, true);
                return true;
            }

            if (string.Equals(route, "HOST.OPEN.CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                OpenPage(AP_ModuleIds.Config, true);
                return true;
            }

            if (string.Equals(route, "HOST.POLYCOUNT.OPEN", StringComparison.OrdinalIgnoreCase))
            {
                AP_MainWorkspaceWindow.Open();
                return true;
            }

            if (route.StartsWith("HOST.OPEN.PAGE.", StringComparison.OrdinalIgnoreCase))
            {
                string moduleId = route.Substring("HOST.OPEN.PAGE.".Length);
                if (AP_ModuleRegistry.TryGet(moduleId, out _))
                {
                    OpenPage(moduleId, true);
                    return true;
                }
            }

            return false;
        }

        private void UpdateWindowTitle()
        {
            titleContent = new GUIContent(AP_Loc.T("AP_MP_WINDOW_TITLE"));
        }
    }
}
#endif
