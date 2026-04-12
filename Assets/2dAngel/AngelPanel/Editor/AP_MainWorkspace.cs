#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    [Serializable]
    public sealed class AP_MainWorkspace
    {
        public enum WorkspaceMode
        {
            Simple = 0,
            Advanced = 1
        }

        public enum SimpleMetricAnchor
        {
            Center = 0,
            TopLeft = 1,
            TopRight = 2
        }

        [Serializable]
        public sealed class AP_PolyCountThreshold
        {
            public int minTriangles;
            public Color color = Color.white;
        }

        [Serializable]
        public sealed class AP_PolyCountSettingsData
        {
            public int selectedFontSize = 34;
            public int totalFontSize = 34;
            public Color selectedBaseColor = new Color(0.15f, 0.75f, 0.25f);
            public Color totalBaseColor = new Color(0.20f, 0.72f, 0.92f);

            public Color normalMetricColor = new Color(0.15f, 0.75f, 0.25f);
            public List<AP_PolyCountThreshold> thresholds = new List<AP_PolyCountThreshold>();

            public SimpleMetricAnchor simpleMetricAnchor = SimpleMetricAnchor.Center;
            public bool includeInactiveInTotal = true;
            public bool includeDisabledRenderersInTotal = true;
            public bool autoRefreshTotal;
            public float autoRefreshIntervalSeconds = 2f;
            public List<AP_PolyCountThreshold> selectedThresholds = new List<AP_PolyCountThreshold>();
            public List<AP_PolyCountThreshold> totalThresholds = new List<AP_PolyCountThreshold>();

            public static AP_PolyCountSettingsData CreateDefault()
            {
                AP_PolyCountSettingsData settings = new AP_PolyCountSettingsData();
                settings.selectedThresholds.Add(new AP_PolyCountThreshold { minTriangles = 100000, color = new Color(0.9f, 0.8f, 0.15f) });
                settings.selectedThresholds.Add(new AP_PolyCountThreshold { minTriangles = 500000, color = new Color(1f, 0.6f, 0f) });
                settings.selectedThresholds.Add(new AP_PolyCountThreshold { minTriangles = 1000000, color = Color.red });

                settings.totalThresholds.Add(new AP_PolyCountThreshold { minTriangles = 200000, color = new Color(0.9f, 0.8f, 0.15f) });
                settings.totalThresholds.Add(new AP_PolyCountThreshold { minTriangles = 400000, color = new Color(1f, 0.6f, 0f) });
                settings.totalThresholds.Add(new AP_PolyCountThreshold { minTriangles = 1000000, color = Color.red });

                settings.Sanitize();
                return settings;
            }

            public void Sanitize()
            {
                selectedFontSize = Mathf.Clamp(selectedFontSize, 16, 96);
                totalFontSize = Mathf.Clamp(totalFontSize, 16, 96);
                autoRefreshIntervalSeconds = Mathf.Clamp(autoRefreshIntervalSeconds, 0.25f, 60f);

                if ((selectedThresholds == null || selectedThresholds.Count == 0) && thresholds != null && thresholds.Count > 0)
                {
                    selectedThresholds = CloneThresholds(thresholds);
                }

                if ((totalThresholds == null || totalThresholds.Count == 0) && thresholds != null && thresholds.Count > 0)
                {
                    totalThresholds = CloneThresholds(thresholds);
                }

                selectedThresholds = selectedThresholds ?? new List<AP_PolyCountThreshold>();
                totalThresholds = totalThresholds ?? new List<AP_PolyCountThreshold>();

                SanitizeThresholds(selectedThresholds);
                SanitizeThresholds(totalThresholds);
            }

            private static List<AP_PolyCountThreshold> CloneThresholds(List<AP_PolyCountThreshold> source)
            {
                List<AP_PolyCountThreshold> clone = new List<AP_PolyCountThreshold>();
                if (source == null)
                {
                    return clone;
                }

                for (int i = 0; i < source.Count; i++)
                {
                    AP_PolyCountThreshold item = source[i];
                    if (item == null)
                    {
                        continue;
                    }

                    clone.Add(new AP_PolyCountThreshold
                    {
                        minTriangles = item.minTriangles,
                        color = item.color
                    });
                }

                return clone;
            }

            private static void SanitizeThresholds(List<AP_PolyCountThreshold> items)
            {
                if (items == null)
                {
                    return;
                }

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    if (items[i] == null)
                    {
                        items.RemoveAt(i);
                        continue;
                    }

                    items[i].minTriangles = Mathf.Max(0, items[i].minTriangles);
                }

                items.Sort((a, b) => a.minTriangles.CompareTo(b.minTriangles));
            }
        }

        [SerializeField] private AP_QuickOps quickOps = new AP_QuickOps();
        [SerializeField] private WorkspaceMode mode = WorkspaceMode.Simple;
        [SerializeField] private AP_PolyCountSettingsData polyCountSettings = AP_PolyCountSettingsData.CreateDefault();
        [SerializeField] private int selectedTriangleCount;
        [SerializeField] private int totalTriangleCount = -1;
        [SerializeField] private int activeOnlyTriangleCount = -1;
        [SerializeField] private long totalRefreshTicks;
        [SerializeField] private long activeOnlyRefreshTicks;
        [SerializeField] private bool isTotalCountDirty = true;
        [SerializeField] private bool isActiveOnlyCountDirty = true;
        [SerializeField] private bool showAdvancedSettings = true;
        [SerializeField] private bool showSelectedThresholdSettings = true;
        [SerializeField] private bool showTotalThresholdSettings = true;

        [NonSerialized] private bool settingsLoaded;
        [NonSerialized] private double nextRealtimeRefreshTime;

        public int SelectedTriangleCount => selectedTriangleCount;
        public int TotalTriangleCount => totalTriangleCount;
        public int ActiveOnlyTriangleCount => activeOnlyTriangleCount;
        public bool IsTotalCountDirty => isTotalCountDirty;
        public bool IsActiveOnlyCountDirty => isActiveOnlyCountDirty;
        public AP_QuickOps QuickOps => quickOps;
        public WorkspaceMode Mode => mode;
        public AP_PolyCountSettingsData PolyCountSettings => polyCountSettings;

        public void Initialize()
        {
            AP_Loc.Init();
            quickOps = quickOps ?? new AP_QuickOps();
            LoadSettingsIfNeeded();
        }

        public void RefreshSelection()
        {
            selectedTriangleCount = CountSelection(Selection.transforms);
        }

        public void MarkSceneCountsDirty()
        {
            isTotalCountDirty = true;
            isActiveOnlyCountDirty = true;

            if (polyCountSettings != null && polyCountSettings.autoRefreshTotal)
            {
                nextRealtimeRefreshTime = 0d;
            }
        }

        public void RefreshSceneTriangles()
        {
            RefreshTotalTriangles();
        }

        public void RefreshTotalTriangles()
        {
            LoadSettingsIfNeeded();
            totalTriangleCount = CountScene(polyCountSettings.includeInactiveInTotal, polyCountSettings.includeDisabledRenderersInTotal);
            totalRefreshTicks = DateTime.UtcNow.Ticks;
            isTotalCountDirty = false;
            ScheduleNextRealtimeRefresh();
        }

        public void RefreshActiveOnlyTriangles()
        {
            activeOnlyTriangleCount = CountScene(false, polyCountSettings.includeDisabledRenderersInTotal);
            activeOnlyRefreshTicks = DateTime.UtcNow.Ticks;
            isActiveOnlyCountDirty = false;
        }

        public void ClearCachedCounts()
        {
            totalTriangleCount = -1;
            activeOnlyTriangleCount = -1;
            totalRefreshTicks = 0L;
            activeOnlyRefreshTicks = 0L;
            MarkSceneCountsDirty();
        }

        public bool TickAutoRefresh()
        {
            LoadSettingsIfNeeded();
            if (!polyCountSettings.autoRefreshTotal)
            {
                return false;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < nextRealtimeRefreshTime)
            {
                return false;
            }

            RefreshTotalTriangles();
            return true;
        }

        public string GetStatusSummary()
        {
            if (selectedTriangleCount <= 0 && totalTriangleCount < 0 && activeOnlyTriangleCount < 0)
            {
                return AP_Loc.T("AP_MP_WORKSPACE_STATUS_IDLE");
            }

            if (isTotalCountDirty || isActiveOnlyCountDirty)
            {
                return AP_Loc.T("AP_MP_WORKSPACE_STATUS_DIRTY");
            }

            return AP_Loc.T("AP_MP_WORKSPACE_STATUS_READY");
        }

        public void Draw(float windowWidth, Action openDetachedWindow)
        {
            Initialize();

            float contentWidth = AP_EUI.GetContentWidth(windowWidth, 18f);
            bool detachedPolyCountOpen = AP_MainWorkspaceWindow.HasOpenWindow();

            DrawHostHeader(contentWidth, openDetachedWindow, detachedPolyCountOpen);
            GUILayout.Space(6f);

            if (detachedPolyCountOpen)
            {
                DrawQuickOpsHost(contentWidth, true);
                return;
            }

            if (mode == WorkspaceMode.Simple)
            {
                DrawSimpleStackedHost(contentWidth, openDetachedWindow);
            }
            else
            {
                DrawAdvancedStackedHost(contentWidth, openDetachedWindow);
            }
        }

        public void DrawDetachedPolyCount(float windowWidth, Action openHostPanel)
        {
            Initialize();

            float contentWidth = AP_EUI.GetContentWidth(windowWidth, 18f);
            DrawDetachedHeader(contentWidth, openHostPanel);
            GUILayout.Space(6f);

            if (mode == WorkspaceMode.Simple)
            {
                DrawSimpleDetachedPolyCount(contentWidth);
            }
            else
            {
                DrawAdvancedDetachedPolyCount(contentWidth);
            }
        }

        private void DrawHostHeader(float contentWidth, Action openDetachedWindow, bool detachedPolyCountOpen)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_MAIN_WORKSPACE_HEADER"), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T("AP_MP_WORKSPACE_MODE"), EditorStyles.miniLabel, GUILayout.Width(44f));
                    int nextModeIndex = EditorGUILayout.Popup((int)mode, GetModeDisplayNames(), GUILayout.Width(108f));
                    mode = (WorkspaceMode)Mathf.Clamp(nextModeIndex, 0, 1);
                }

                GUILayout.Space(2f);

                GUILayout.Space(3f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    AP_EUI.DrawStatusPill(GetStatusSummary(), HasDirtyCache() ? MessageType.Warning : MessageType.Info);
                    GUILayout.FlexibleSpace();

                    if (openDetachedWindow != null)
                    {
                        string buttonKey = detachedPolyCountOpen ? "AP_MP_FOCUS_DETACHED_POLYCOUNT" : "AP_MP_OPEN_DETACHED_POLYCOUNT";
                        if (GUILayout.Button(AP_Loc.T(buttonKey), EditorStyles.miniButton, GUILayout.Width(144f)))
                        {
                            openDetachedWindow();
                        }
                    }
                }
            }
        }

        private void DrawDetachedHeader(float contentWidth, Action openHostPanel)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_POLYCOUNT_WINDOW_TITLE"), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T("AP_MP_WORKSPACE_MODE"), EditorStyles.miniLabel, GUILayout.Width(44f));
                    int nextModeIndex = EditorGUILayout.Popup((int)mode, GetModeDisplayNames(), GUILayout.Width(108f));
                    mode = (WorkspaceMode)Mathf.Clamp(nextModeIndex, 0, 1);
                }

                GUILayout.Space(2f);

                if (openHostPanel != null)
                {
                    GUILayout.Space(3f);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(AP_Loc.T("AP_MP_OPEN_HOST_PANEL"), EditorStyles.miniButton, GUILayout.Width(128f)))
                        {
                            openHostPanel();
                        }
                    }
                }
            }
        }

        private void DrawSimpleStackedHost(float contentWidth, Action openDetachedWindow)
        {
            DrawSimplePolyCountStrip(contentWidth, openDetachedWindow, false);
            GUILayout.Space(5f);
            DrawQuickOpsHost(contentWidth, false);
        }

        private void DrawAdvancedStackedHost(float contentWidth, Action openDetachedWindow)
        {
            DrawAdvancedPolyCountCard(contentWidth, openDetachedWindow, true);
            GUILayout.Space(8f);
            DrawAdvancedSettingsCard();
            GUILayout.Space(8f);
            DrawQuickOpsHost(contentWidth, false);
        }

        private void DrawQuickOpsHost(float contentWidth, bool detachedPolyCountOpen)
        {
            if (detachedPolyCountOpen)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_MAIN_QUICKOPS_ONLY"), EditorStyles.boldLabel);
                    GUILayout.Space(2f);
                }

                GUILayout.Space(5f);
            }

            quickOps.Draw(contentWidth, mode == WorkspaceMode.Simple ? AP_QuickOps.LayoutMode.SimpleCompact : AP_QuickOps.LayoutMode.Advanced);
        }

        private void DrawSimpleDetachedPolyCount(float contentWidth)
        {
            DrawSimplePolyCountStrip(contentWidth, null, true);
        }

        private void DrawAdvancedDetachedPolyCount(float contentWidth)
        {
            DrawAdvancedPolyCountCard(contentWidth, null, false);
            GUILayout.Space(8f);
            DrawAdvancedSettingsCard();
            GUILayout.Space(8f);
            DrawDetailedCacheMetrics();
        }

        private void DrawSimplePolyCountStrip(float contentWidth, Action openDetachedWindow, bool detachedWindow)
        {
            bool compactOnly = contentWidth < 320f;
            bool hideButtons = contentWidth < 360f;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_POLYCOUNT_HEADER"), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    DrawCompactRealtimeBadge();
                }

                GUILayout.Space(4f);
                DrawSimpleMetricCluster(contentWidth);

                if (!compactOnly)
                {
                    GUILayout.Space(4f);
                }

                if (!hideButtons)
                {
                    GUILayout.Space(4f);
                    using (AP_EUI.ResponsiveRow row = AP_EUI.Row(contentWidth).Gap(3f).Pad(0f))
                    {
                        if (row.MiniButton(AP_Loc.T("AP_MP_REFRESH_SELECTION")))
                        {
                            RefreshSelection();
                        }

                        if (row.MiniButton(AP_Loc.T("AP_MP_REFRESH_TOTAL")))
                        {
                            RefreshTotalTriangles();
                        }

                        if (row.MiniButton(AP_Loc.T("AP_MP_COUNT_ACTIVE_ONLY")))
                        {
                            RefreshActiveOnlyTriangles();
                        }

                        if (row.MiniButton(AP_Loc.T("AP_MP_CLEAR_SCENE_CACHE")))
                        {
                            ClearCachedCounts();
                        }

                        if (!detachedWindow && openDetachedWindow != null && row.MiniButton(AP_Loc.T("AP_MP_OPEN_DETACHED_POLYCOUNT")))
                        {
                            openDetachedWindow();
                        }
                    }
                }

                if (!compactOnly)
                {
                    GUILayout.Space(4f);
                    GUILayout.Label(BuildCompactCacheSummary(), EditorStyles.miniLabel);
                }
            }
        }

        private void DrawSimpleMetricCluster(float contentWidth)
        {
            bool stacked = contentWidth < 460f;
            float metricWidth = stacked ? Mathf.Max(160f, contentWidth - 18f) : Mathf.Max(150f, Mathf.Min(280f, (contentWidth - 12f) * 0.5f));
            float unifiedHeight = stacked ? 0f : GetHorizontalUnifiedMetricHeight(polyCountSettings.selectedFontSize, polyCountSettings.totalFontSize, 112f, 176f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (polyCountSettings.simpleMetricAnchor == SimpleMetricAnchor.Center || polyCountSettings.simpleMetricAnchor == SimpleMetricAnchor.TopRight)
                {
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(metricWidth * (stacked ? 1f : 2f) + (stacked ? 0f : 8f))))
                {
                    if (stacked)
                    {
                        DrawSimpleMetricBlock(AP_Loc.T("AP_MP_SELECTED_TOTAL"), selectedTriangleCount, GetSelectedTriangleColor(selectedTriangleCount), polyCountSettings.selectedFontSize, metricWidth, 0f);
                        GUILayout.Space(5f);
                        DrawSimpleMetricBlock(AP_Loc.T("AP_MP_SCENE_TOTAL"), totalTriangleCount, GetTotalTriangleColor(totalTriangleCount), polyCountSettings.totalFontSize, metricWidth, 0f);
                    }
                    else
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            DrawSimpleMetricBlock(AP_Loc.T("AP_MP_SELECTED_TOTAL"), selectedTriangleCount, GetSelectedTriangleColor(selectedTriangleCount), polyCountSettings.selectedFontSize, metricWidth, unifiedHeight);
                            GUILayout.Space(8f);
                            DrawSimpleMetricBlock(AP_Loc.T("AP_MP_SCENE_TOTAL"), totalTriangleCount, GetTotalTriangleColor(totalTriangleCount), polyCountSettings.totalFontSize, metricWidth, unifiedHeight);
                        }
                    }
                }

                if (polyCountSettings.simpleMetricAnchor == SimpleMetricAnchor.Center || polyCountSettings.simpleMetricAnchor == SimpleMetricAnchor.TopLeft)
                {
                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawSimpleMetricBlock(string label, int value, Color color, int fontSize, float width, float fixedHeight)
        {
            GUILayoutOption[] layout = fixedHeight > 0f
                ? new[] { GUILayout.Width(width), GUILayout.Height(fixedHeight) }
                : new[] { GUILayout.Width(width) };

            using (new EditorGUILayout.VerticalScope("box", layout))
            {
                GUILayout.Label(label, EditorStyles.miniBoldLabel);
                GUILayout.Space(2f);
                GUILayout.FlexibleSpace();
                DrawLargeMetricValue(value, color, fontSize, Mathf.Max(36f, fontSize + 8f), TextAnchor.MiddleCenter);
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawAdvancedPolyCountCard(float contentWidth, Action openDetachedWindow, bool showDetachButton)
        {
            using (AP_EUI.Card(AP_Loc.T("AP_MP_POLYCOUNT_HEADER")))
            {
                bool stacked = contentWidth < 620f;
                float unifiedHeight = stacked ? 0f : GetHorizontalUnifiedMetricHeight(polyCountSettings.selectedFontSize, polyCountSettings.totalFontSize, 120f, 210f);
                if (stacked)
                {
                    DrawAdvancedMetricBlock(AP_Loc.T("AP_MP_SELECTED_TOTAL"), selectedTriangleCount, GetSelectedTriangleColor(selectedTriangleCount), polyCountSettings.selectedFontSize, 0f);
                    GUILayout.Space(6f);
                    DrawAdvancedMetricBlock(AP_Loc.T("AP_MP_SCENE_TOTAL"), totalTriangleCount, GetTotalTriangleColor(totalTriangleCount), polyCountSettings.totalFontSize, 0f);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawAdvancedMetricBlock(AP_Loc.T("AP_MP_SELECTED_TOTAL"), selectedTriangleCount, GetSelectedTriangleColor(selectedTriangleCount), polyCountSettings.selectedFontSize, unifiedHeight);
                        GUILayout.Space(8f);
                        DrawAdvancedMetricBlock(AP_Loc.T("AP_MP_SCENE_TOTAL"), totalTriangleCount, GetTotalTriangleColor(totalTriangleCount), polyCountSettings.totalFontSize, unifiedHeight);
                    }
                }

                GUILayout.Space(4f);
                GUILayout.Space(2f);
                DrawTotalModeSummary();
                GUILayout.Space(6f);

                using (AP_EUI.ResponsiveRow row = AP_EUI.Row(contentWidth).Gap(6f).Pad(0f))
                {
                    if (row.Button(AP_Loc.T("AP_MP_REFRESH_SELECTION")))
                    {
                        RefreshSelection();
                    }

                    if (row.Button(AP_Loc.T("AP_MP_REFRESH_TOTAL")))
                    {
                        RefreshTotalTriangles();
                    }

                    if (row.Button(AP_Loc.T("AP_MP_COUNT_ACTIVE_ONLY")))
                    {
                        RefreshActiveOnlyTriangles();
                    }

                    if (row.MiniButton(AP_Loc.T("AP_MP_CLEAR_SCENE_CACHE")))
                    {
                        ClearCachedCounts();
                    }

                    if (showDetachButton && openDetachedWindow != null && row.MiniButton(AP_Loc.T("AP_MP_OPEN_DETACHED_POLYCOUNT")))
                    {
                        openDetachedWindow();
                    }
                }
            }
        }

        private void DrawAdvancedMetricBlock(string label, int value, Color color, int fontSize, float fixedHeight)
        {
            float autoHeight = Mathf.Max(84f, fontSize + 34f);
            GUILayoutOption[] layout = fixedHeight > 0f
                ? new[] { GUILayout.Height(fixedHeight) }
                : new[] { GUILayout.MinHeight(autoHeight) };

            using (new EditorGUILayout.VerticalScope("box", layout))
            {
                GUILayout.Label(label, EditorStyles.boldLabel);
                GUILayout.Space(2f);
                GUILayout.FlexibleSpace();
                DrawLargeMetricValue(value, color, fontSize, Mathf.Max(40f, fontSize + 10f), TextAnchor.MiddleCenter);
                GUILayout.FlexibleSpace();
            }
        }


        private static float GetHorizontalUnifiedMetricHeight(int primaryFontSize, int secondaryFontSize, float minHeight, float maxHeight)
        {
            int dominantFontSize = Mathf.Max(primaryFontSize, secondaryFontSize);
            float computedHeight = dominantFontSize + 64f;
            return Mathf.Clamp(computedHeight, minHeight, maxHeight);
        }

        private void DrawTotalModeSummary()
        {
            string totalModeText = polyCountSettings.autoRefreshTotal ? AP_Loc.T("AP_MP_TOTAL_MODE_REALTIME") : AP_Loc.T("AP_MP_TOTAL_MODE_MANUAL");
            string inactiveText = polyCountSettings.includeInactiveInTotal ? AP_Loc.T("AP_MP_TOTAL_INCLUDES_INACTIVE") : AP_Loc.T("AP_MP_TOTAL_EXCLUDES_INACTIVE");
            string refreshText = GetRefreshText(totalRefreshTicks);
            GUILayout.Label(totalModeText + "  |  " + inactiveText + "  |  " + AP_Loc.T("AP_MP_LAST_REFRESH") + ": " + refreshText, EditorStyles.miniLabel);
        }

        private void DrawAdvancedSettingsCard()
        {
            using (AP_EUI.Card(AP_Loc.T("AP_MP_POLYCOUNT_SETTINGS_HEADER")))
            {
                showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, AP_Loc.T("AP_MP_POLYCOUNT_SETTINGS_HEADER"), true);
                if (!showAdvancedSettings)
                {
                    return;
                }

                bool thresholdLayoutChanged = false;
                EditorGUI.BeginChangeCheck();

                polyCountSettings.selectedFontSize = EditorGUILayout.IntSlider(AP_Loc.T("AP_MP_SELECTED_FONT_SIZE"), polyCountSettings.selectedFontSize, 16, 96);
                polyCountSettings.totalFontSize = EditorGUILayout.IntSlider(AP_Loc.T("AP_MP_TOTAL_FONT_SIZE"), polyCountSettings.totalFontSize, 16, 96);
                polyCountSettings.simpleMetricAnchor = (SimpleMetricAnchor)EditorGUILayout.EnumPopup(AP_Loc.T("AP_MP_SIMPLE_ANCHOR"), polyCountSettings.simpleMetricAnchor);

                GUILayout.Space(4f);
                polyCountSettings.selectedBaseColor = EditorGUILayout.ColorField(AP_Loc.T("AP_MP_SELECTED_BASE_COLOR"), polyCountSettings.selectedBaseColor);
                polyCountSettings.totalBaseColor = EditorGUILayout.ColorField(AP_Loc.T("AP_MP_TOTAL_BASE_COLOR"), polyCountSettings.totalBaseColor);

                GUILayout.Space(4f);
                polyCountSettings.includeInactiveInTotal = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_MP_TOTAL_INCLUDE_INACTIVE_TOGGLE"), polyCountSettings.includeInactiveInTotal);
                polyCountSettings.includeDisabledRenderersInTotal = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_MP_TOTAL_INCLUDE_DISABLED_RENDERER_TOGGLE"), polyCountSettings.includeDisabledRenderersInTotal);

                GUILayout.Space(4f);
                polyCountSettings.autoRefreshTotal = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_MP_REALTIME_TOTAL_TOGGLE"), polyCountSettings.autoRefreshTotal);
                using (new EditorGUI.DisabledScope(!polyCountSettings.autoRefreshTotal))
                {
                    polyCountSettings.autoRefreshIntervalSeconds = EditorGUILayout.Slider(AP_Loc.T("AP_MP_REALTIME_INTERVAL"), polyCountSettings.autoRefreshIntervalSeconds, 0.25f, 15f);
                }

                if (polyCountSettings.autoRefreshTotal)
                {
                    EditorGUILayout.HelpBox(AP_Loc.T("AP_MP_REALTIME_WARNING"), polyCountSettings.autoRefreshIntervalSeconds <= 1f ? MessageType.Warning : MessageType.Info);
                }

                GUILayout.Space(4f);
                showSelectedThresholdSettings = EditorGUILayout.Foldout(showSelectedThresholdSettings, AP_Loc.T("AP_MP_SELECTED_THRESHOLD_SETTINGS"), true);
                if (showSelectedThresholdSettings)
                {
                    thresholdLayoutChanged |= DrawThresholdEditor(polyCountSettings.selectedThresholds, polyCountSettings.selectedBaseColor);
                }

                GUILayout.Space(2f);
                showTotalThresholdSettings = EditorGUILayout.Foldout(showTotalThresholdSettings, AP_Loc.T("AP_MP_TOTAL_THRESHOLD_SETTINGS"), true);
                if (showTotalThresholdSettings)
                {
                    thresholdLayoutChanged |= DrawThresholdEditor(polyCountSettings.totalThresholds, polyCountSettings.totalBaseColor);
                }

                if (EditorGUI.EndChangeCheck() || thresholdLayoutChanged)
                {
                    polyCountSettings.Sanitize();
                    SaveSettings();
                    MarkSceneCountsDirty();
                    if (polyCountSettings.autoRefreshTotal)
                    {
                        nextRealtimeRefreshTime = 0d;
                    }
                }
            }
        }

        private bool DrawThresholdEditor(List<AP_PolyCountThreshold> thresholds, Color fallbackColor)
        {
            bool changed = false;
            if (thresholds == null)
            {
                return false;
            }

            for (int i = 0; i < thresholds.Count; i++)
            {
                AP_PolyCountThreshold threshold = thresholds[i];
                if (threshold == null)
                {
                    threshold = new AP_PolyCountThreshold();
                    thresholds[i] = threshold;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_THRESHOLD_VALUE"), GUILayout.Width(52f));
                    threshold.minTriangles = EditorGUILayout.DelayedIntField(threshold.minTriangles, GUILayout.Width(90f));
                    GUILayout.Label(AP_Loc.T("AP_MP_THRESHOLD_COLOR"), GUILayout.Width(38f));
                    threshold.color = EditorGUILayout.ColorField(threshold.color, GUILayout.Width(96f));

                    if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(24f)))
                    {
                        thresholds.Insert(i + 1, new AP_PolyCountThreshold
                        {
                            minTriangles = threshold.minTriangles + 100000,
                            color = threshold.color
                        });
                        changed = true;
                    }

                    if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(24f)))
                    {
                        thresholds.RemoveAt(i);
                        i--;
                        changed = true;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(AP_Loc.T("AP_MP_THRESHOLD_ADD"), EditorStyles.miniButton, GUILayout.Width(128f)))
                {
                    thresholds.Add(new AP_PolyCountThreshold
                    {
                        minTriangles = thresholds.Count == 0 ? 100000 : thresholds[thresholds.Count - 1].minTriangles + 100000,
                        color = fallbackColor
                    });
                    changed = true;
                }
            }

            return changed;
        }

        private void DrawDetailedCacheMetrics()
        {
            using (AP_EUI.Card(AP_Loc.T("AP_MP_POLYCOUNT_CACHE_HEADER")))
            {
                DrawMetricRow(AP_Loc.T("AP_MP_SCENE_TOTAL"), totalTriangleCount, isTotalCountDirty, totalRefreshTicks);
                GUILayout.Space(4f);
                DrawMetricRow(AP_Loc.T("AP_MP_ACTIVE_TOTAL"), activeOnlyTriangleCount, isActiveOnlyCountDirty, activeOnlyRefreshTicks);
            }
        }

        private string BuildCompactCacheSummary()
        {
            return AP_Loc.T("AP_MP_SCENE_TOTAL") + ": " + GetMetricText(totalTriangleCount)
                + "   |   " + AP_Loc.T("AP_MP_ACTIVE_TOTAL") + ": " + GetMetricText(activeOnlyTriangleCount);
        }

        private void DrawMetricRow(string label, int value, bool isDirty, long refreshTicks)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(160f));

                if (value < 0)
                {
                    GUILayout.Label(AP_Loc.T("AP_MP_NO_SCENE_CACHE"));
                }
                else
                {
                    GUIStyle valueStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold
                    };
                    valueStyle.normal.textColor = GetTotalTriangleColor(value);
                    GUILayout.Label(value.ToString("N0"), valueStyle, GUILayout.Width(120f));
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(AP_Loc.T("AP_MP_LAST_REFRESH"), GUILayout.Width(92f));
                GUILayout.Label(GetRefreshText(refreshTicks), GUILayout.Width(148f));
                GUILayout.Label(isDirty ? AP_Loc.T("AP_MP_DIRTY") : AP_Loc.T("AP_MP_CLEAN"), GUILayout.Width(56f));
            }
        }

        private void DrawCompactRealtimeBadge()
        {
            string label = polyCountSettings.autoRefreshTotal
                ? AP_Loc.T("AP_MP_TOTAL_MODE_REALTIME")
                : AP_Loc.T("AP_MP_TOTAL_MODE_MANUAL");

            AP_EUI.DrawStatusPill(label, polyCountSettings.autoRefreshTotal ? MessageType.Info : MessageType.None);
        }

        private void LoadSettingsIfNeeded()
        {
            if (settingsLoaded)
            {
                polyCountSettings = polyCountSettings ?? AP_PolyCountSettingsData.CreateDefault();
                polyCountSettings.Sanitize();
                return;
            }

            polyCountSettings = AP_PolyCountSettingsData.CreateDefault();
            if (AP_CoreStorage.TryLoad(AP_CorePaths.PolyCountConfigFilePath, out AP_PolyCountSettingsData loaded) && loaded != null)
            {
                polyCountSettings = loaded;
            }

            polyCountSettings.Sanitize();
            settingsLoaded = true;
            ScheduleNextRealtimeRefresh();
        }

        private void SaveSettings()
        {
            polyCountSettings = polyCountSettings ?? AP_PolyCountSettingsData.CreateDefault();
            polyCountSettings.Sanitize();
            AP_CoreStorage.TrySave(AP_CorePaths.PolyCountConfigFilePath, polyCountSettings);
            ScheduleNextRealtimeRefresh();
        }

        private void ScheduleNextRealtimeRefresh()
        {
            nextRealtimeRefreshTime = EditorApplication.timeSinceStartup + Math.Max(0.25d, polyCountSettings.autoRefreshIntervalSeconds);
        }

        private bool HasDirtyCache()
        {
            return isTotalCountDirty || isActiveOnlyCountDirty;
        }

        private string[] GetModeDisplayNames()
        {
            return new[]
            {
                AP_Loc.T("AP_MP_WORKSPACE_MODE_SIMPLE"),
                AP_Loc.T("AP_MP_WORKSPACE_MODE_ADVANCED")
            };
        }

        private string GetMetricText(int value)
        {
            return value >= 0 ? value.ToString("N0") : AP_Loc.T("AP_MP_NO_SCENE_CACHE");
        }

        private static string GetRefreshText(long refreshTicks)
        {
            return refreshTicks > 0L
                ? new DateTime(refreshTicks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : AP_Loc.T("AP_AP_NOT_READY");
        }

        private void DrawLargeMetricValue(int value, Color color, int fontSize, float minHeight, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Clamp(fontSize, 16, 96),
                alignment = alignment,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = color;
            GUILayout.Label(value >= 0 ? value.ToString("N0") : "--", style, GUILayout.MinHeight(minHeight));
        }

        private Color GetSelectedTriangleColor(int value)
        {
            return GetTriangleColor(value, polyCountSettings.selectedBaseColor, polyCountSettings.selectedThresholds);
        }

        private Color GetTotalTriangleColor(int value)
        {
            return GetTriangleColor(value, polyCountSettings.totalBaseColor, polyCountSettings.totalThresholds);
        }

        private static Color GetTriangleColor(int value, Color baseColor, List<AP_PolyCountThreshold> thresholds)
        {
            if (value < 0)
            {
                return baseColor;
            }

            Color result = baseColor;
            if (thresholds == null)
            {
                return result;
            }

            for (int i = 0; i < thresholds.Count; i++)
            {
                AP_PolyCountThreshold threshold = thresholds[i];
                if (threshold != null && value >= threshold.minTriangles)
                {
                    result = threshold.color;
                }
            }

            return result;
        }

        private static int CountSelection(Transform[] selection)
        {
            if (selection == null || selection.Length == 0)
            {
                return 0;
            }

            List<Transform> roots = GetDistinctRoots(selection);
            return CountTransforms(roots, false, true);
        }

        private static int CountScene(bool includeInactiveObjects, bool includeDisabledRenderers)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return 0;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            int total = 0;

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (!includeInactiveObjects && !root.activeInHierarchy)
                {
                    continue;
                }

                total += CountTransformRecursive(root.transform, includeInactiveObjects, includeDisabledRenderers);
            }

            return total;
        }

        private static int CountTransforms(List<Transform> roots, bool includeInactiveObjects, bool includeDisabledRenderers)
        {
            int total = 0;
            for (int i = 0; i < roots.Count; i++)
            {
                Transform root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (!includeInactiveObjects && !root.gameObject.activeInHierarchy)
                {
                    continue;
                }

                total += CountTransformRecursive(root, includeInactiveObjects, includeDisabledRenderers);
            }

            return total;
        }

        private static int CountTransformRecursive(Transform root, bool includeInactiveObjects, bool includeDisabledRenderers)
        {
            int total = 0;
            Stack<Transform> stack = new Stack<Transform>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                Transform current = stack.Pop();
                if (current == null)
                {
                    continue;
                }

                if (includeInactiveObjects || current.gameObject.activeInHierarchy)
                {
                    total += CountMeshTriangles(current.gameObject, includeDisabledRenderers);
                }

                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child != null)
                    {
                        stack.Push(child);
                    }
                }
            }

            return total;
        }

        private static int CountMeshTriangles(GameObject gameObject, bool includeDisabledRenderers)
        {
            int total = 0;

            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
                if (includeDisabledRenderers || meshRenderer == null || meshRenderer.enabled)
                {
                    total += CountMeshTriangles(meshFilter.sharedMesh);
                }
            }

            SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                if (includeDisabledRenderers || skinnedMeshRenderer.enabled)
                {
                    total += CountMeshTriangles(skinnedMeshRenderer.sharedMesh);
                }
            }

            return total;
        }

        private static int CountMeshTriangles(Mesh mesh)
        {
            if (mesh == null)
            {
                return 0;
            }

            long indexTotal = 0L;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) != MeshTopology.Triangles)
                {
                    continue;
                }

                indexTotal += (long)mesh.GetIndexCount(i);
            }

            return (int)(indexTotal / 3L);
        }

        private static List<Transform> GetDistinctRoots(Transform[] selection)
        {
            List<Transform> roots = new List<Transform>(selection.Length);
            HashSet<Transform> selectedSet = new HashSet<Transform>(selection);

            for (int i = 0; i < selection.Length; i++)
            {
                Transform current = selection[i];
                if (current == null)
                {
                    continue;
                }

                Transform parent = current.parent;
                bool hasSelectedAncestor = false;
                while (parent != null)
                {
                    if (selectedSet.Contains(parent))
                    {
                        hasSelectedAncestor = true;
                        break;
                    }

                    parent = parent.parent;
                }

                if (!hasSelectedAncestor)
                {
                    roots.Add(current);
                }
            }

            return roots;
        }
    }
}
#endif
