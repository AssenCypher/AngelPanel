#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_OcclusionPage
    {
        private enum SuggestMode
        {
            Safe,
            Balanced,
            Fast
        }

        private struct OcclusionSample
        {
            public Bounds Bounds;
            public bool IsStatic;
            public bool HasRendererLikeSurface;
            public bool IsOccluderCandidate;
            public float MinDim;
            public float MidDim;
            public float MaxDim;
            public float MajorDim;
        }

        private static float expandPercent = 2f;
        private static float smallestOccluder = 0.5f;
        private static float smallestHole = 0.1f;
        private static int backfaceThreshold = 100;
        private static bool selectionOnly;
        private static SuggestMode suggestMode = SuggestMode.Safe;
        private static string lastReport = "No analysis yet.";

        public static void Draw(AP_HostContext context)
        {
            int fontSize = context?.HostWindow != null ? context.HostWindow.GetHostContentFontSize() : 11;
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.Clamp(fontSize + 1, 11, 20), wordWrap = true };
            GUIStyle note = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = Mathf.Clamp(fontSize - 1, 9, 16), wordWrap = true };
            float buttonHeight = Mathf.Clamp(22f + (fontSize - 11f) * 2f, 22f, 34f);

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_Opt_TAB_OCCLUSION"), title);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCC_PAGE_TITLE"), title);
                GUILayout.Space(2f);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCC_SUGGEST_TITLE"), title);
                suggestMode = (SuggestMode)EditorGUILayout.EnumPopup(AP_Loc.T("AP_OCC_PROFILE"), suggestMode);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_OCC_ANALYZE"), GUILayout.Height(buttonHeight)))
                    {
                        AnalyzeAndSuggest();
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_OCC_COPY_REPORT"), GUILayout.Height(buttonHeight)))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            $"Occlusion Suggestion\n- Profile: {suggestMode}\n- Smallest Occluder: {smallestOccluder:0.###} m\n- Smallest Hole: {smallestHole:0.###} m\n- Backface Threshold: {backfaceThreshold}%\n- Report: {lastReport}";
                    }
                }

                smallestOccluder = EditorGUILayout.Slider(AP_Loc.T("AP_OCC_SMALLEST_OCCLUDER"), smallestOccluder, 0.05f, 10f);
                smallestHole = EditorGUILayout.Slider(AP_Loc.T("AP_OCC_SMALLEST_HOLE"), smallestHole, 0.02f, 2f);
                backfaceThreshold = EditorGUILayout.IntSlider(AP_Loc.T("AP_OCC_BACKFACE"), backfaceThreshold, 0, 100);
                EditorGUILayout.HelpBox(lastReport, MessageType.Info);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCC_STATIC_TITLE"), title);
                selectionOnly = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_OCC_SELECTION_ONLY"), selectionOnly);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_OCC_MARK_OCCLUDER"), GUILayout.Height(buttonHeight)))
                    {
                        SetStaticFlagsSmart(StaticEditorFlags.OccluderStatic, true);
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_OCC_MARK_OCCLUDEE"), GUILayout.Height(buttonHeight)))
                    {
                        SetStaticFlagsSmart(StaticEditorFlags.OccludeeStatic, true);
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_OCC_CLEAR_FLAGS"), GUILayout.Height(buttonHeight)))
                    {
                        SetStaticFlagsSmart(StaticEditorFlags.OccluderStatic, false);
                        SetStaticFlagsSmart(StaticEditorFlags.OccludeeStatic, false);
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCC_AREAS_TITLE"), title);
                expandPercent = EditorGUILayout.Slider(AP_Loc.T("AP_OCC_EXPAND_PERCENT"), expandPercent, 0f, 20f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_OCC_CREATE_FROM_SELECTION"), GUILayout.Height(buttonHeight)))
                    {
                        CreateAreasFromSelection();
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_OCC_CLEAR_AREAS"), GUILayout.Height(buttonHeight)))
                    {
                        ClearAllAreas();
                    }
                }
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCC_BAKE_TITLE"), title);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_OCC_OPEN_WINDOW"), GUILayout.Height(buttonHeight)))
                    {
                        OpenOcclusionWindow();
                    }

                    if (GUILayout.Button(AP_Loc.T("AP_OCC_BAKE_BG"), GUILayout.Height(buttonHeight)))
                    {
                        TryBakeInBackground();
                    }
                }
            }
        }

        private static void AnalyzeAndSuggest()
        {
            List<GameObject> targets = CollectTargetGameObjects(selectionOnly);
            List<OcclusionSample> allStaticSamples = new List<OcclusionSample>();
            List<OcclusionSample> occluderSamples = new List<OcclusionSample>();

            for (int i = 0; i < targets.Count; i++)
            {
                if (!TryBuildSample(targets[i], out OcclusionSample sample) || !sample.IsStatic)
                {
                    continue;
                }

                allStaticSamples.Add(sample);
                if (sample.IsOccluderCandidate)
                {
                    occluderSamples.Add(sample);
                }
            }

            if (allStaticSamples.Count == 0)
            {
                smallestOccluder = 0.5f;
                smallestHole = 0.1f;
                backfaceThreshold = 100;
                lastReport = AP_Loc.T("AP_OCC_REPORT_EMPTY");
                return;
            }

            List<OcclusionSample> source = occluderSamples.Count > 0 ? occluderSamples : allStaticSamples;
            List<float> minDims = source.Select(s => Mathf.Max(0.01f, s.MinDim)).OrderBy(v => v).ToList();
            List<float> majorDims = source.Select(s => Mathf.Max(0.05f, s.MajorDim)).OrderBy(v => v).ToList();

            float occQuantile;
            float occScale;
            float holeQuantile;
            float holeScale;

            switch (suggestMode)
            {
                default:
                case SuggestMode.Safe:
                    occQuantile = 0.30f;
                    occScale = 0.85f;
                    holeQuantile = 0.03f;
                    holeScale = 0.18f;
                    break;
                case SuggestMode.Balanced:
                    occQuantile = 0.45f;
                    occScale = 1.00f;
                    holeQuantile = 0.07f;
                    holeScale = 0.24f;
                    break;
                case SuggestMode.Fast:
                    occQuantile = 0.65f;
                    occScale = 1.15f;
                    holeQuantile = 0.15f;
                    holeScale = 0.35f;
                    break;
            }

            smallestOccluder = Mathf.Clamp(Quantile(minDims, occQuantile) * occScale, 0.05f, 3.0f);
            smallestHole = Mathf.Clamp(Quantile(majorDims, holeQuantile) * holeScale, 0.03f, 0.50f);
            backfaceThreshold = 100;
            lastReport = string.Format(AP_Loc.T("AP_OCC_REPORT_TEMPLATE"), allStaticSamples.Count, occluderSamples.Count, suggestMode.ToString());
        }

        private static float Quantile(IList<float> values, float p)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            float[] arr = values.OrderBy(v => v).ToArray();
            float idx = Mathf.Clamp01(p) * (arr.Length - 1);
            int i0 = Mathf.FloorToInt(idx);
            int i1 = Mathf.Min(arr.Length - 1, i0 + 1);
            return Mathf.Lerp(arr[i0], arr[i1], idx - i0);
        }

        private static List<GameObject> CollectTargetGameObjects(bool selection)
        {
            IEnumerable<GameObject> objects;
            if (selection && Selection.transforms != null && Selection.transforms.Length > 0)
            {
                objects = Selection.transforms.SelectMany(t => t.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);
            }
            else
            {
                objects = UnityEngine.Object.FindObjectsOfType<Transform>(true).Select(t => t.gameObject);
            }

            return objects.Distinct().ToList();
        }

        private static bool TryBuildSample(GameObject go, out OcclusionSample sample)
        {
            sample = default;
            if (go == null || !go.scene.IsValid())
            {
                return false;
            }

            Bounds bounds = default;
            bool hasBounds = false;
            bool hasRendererLikeSurface = false;
            bool hasOpaqueRenderable = false;

            Terrain terrain = go.GetComponent<Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                bounds = new Bounds(terrain.transform.position + terrain.terrainData.size * 0.5f, terrain.terrainData.size);
                hasBounds = true;
                hasRendererLikeSurface = true;
                hasOpaqueRenderable = true;
            }

            Renderer[] renderers = go.GetComponents<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !IsRendererSupportedForOcclusion(renderer))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                hasRendererLikeSurface = true;
                if (RendererLooksOpaque(renderer))
                {
                    hasOpaqueRenderable = true;
                }
            }

            Collider[] colliders = go.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            float[] dims = new[] { bounds.size.x, bounds.size.y, bounds.size.z }.OrderBy(v => v).ToArray();
            float minDim = Mathf.Max(0.001f, dims[0]);
            float midDim = Mathf.Max(0.001f, dims[1]);
            float maxDim = Mathf.Max(0.001f, dims[2]);
            float majorDim = Mathf.Max(midDim, maxDim);
            bool structuralScale = minDim >= 0.05f && ((midDim >= 0.60f && maxDim >= 0.60f) || (midDim >= 0.35f && maxDim >= 1.25f));

            sample = new OcclusionSample
            {
                Bounds = bounds,
                IsStatic = IsStaticish(go),
                HasRendererLikeSurface = hasRendererLikeSurface,
                IsOccluderCandidate = hasOpaqueRenderable && structuralScale,
                MinDim = minDim,
                MidDim = midDim,
                MaxDim = maxDim,
                MajorDim = majorDim
            };
            return true;
        }

        private static bool IsRendererSupportedForOcclusion(Renderer renderer)
        {
            return !(renderer is ParticleSystemRenderer) && !(renderer is TrailRenderer) && !(renderer is LineRenderer);
        }

        private static bool RendererLooksOpaque(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return true;
            }

            bool hasOpaque = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                if (material.renderQueue >= 3000)
                {
                    return false;
                }

                hasOpaque = true;
            }

            return hasOpaque;
        }

        private static bool IsStaticish(GameObject go)
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
            return (flags & (StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic)) != 0;
        }

        private static void SetStaticFlagsSmart(StaticEditorFlags flag, bool on)
        {
            List<GameObject> targets = CollectTargetGameObjects(selectionOnly);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            int changed = 0;
            int skipped = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                GameObject go = targets[i];
                if (go == null)
                {
                    continue;
                }

                bool shouldApply = true;
                if (on)
                {
                    if (!TryBuildSample(go, out OcclusionSample sample))
                    {
                        skipped++;
                        continue;
                    }

                    if (flag == StaticEditorFlags.OccluderStatic)
                    {
                        shouldApply = sample.IsOccluderCandidate;
                    }
                    else if (flag == StaticEditorFlags.OccludeeStatic)
                    {
                        shouldApply = sample.HasRendererLikeSurface;
                    }
                }

                if (!shouldApply)
                {
                    skipped++;
                    continue;
                }

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                StaticEditorFlags newFlags = on ? (flags | flag) : (flags & ~flag);
                if (newFlags == flags)
                {
                    continue;
                }

                Undo.RecordObject(go, on ? "Set Static Flag" : "Clear Static Flag");
                GameObjectUtility.SetStaticEditorFlags(go, newFlags);
                changed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_STATIC_TITLE"), string.Format(AP_Loc.T("AP_OCC_FLAG_RESULT"), on ? AP_Loc.T("AP_OCC_ACTION_SET") : AP_Loc.T("AP_OCC_ACTION_CLEAR"), flag, changed, skipped), AP_Loc.T("AP_DBG_OK"));
        }

        private static void CreateAreasFromSelection()
        {
            if (Selection.transforms == null || Selection.transforms.Length == 0)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_AREAS_TITLE"), AP_Loc.T("AP_OCC_NEED_SELECTION"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            Bounds? boundsNullable = AP_OptimizingUtils.CollectBounds(Selection.transforms, true, true);
            if (!boundsNullable.HasValue)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_AREAS_TITLE"), AP_Loc.T("AP_OCC_NO_BOUNDS"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            Bounds bounds = boundsNullable.Value;
            bounds.Expand(bounds.size * (expandPercent / 100f));

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            GameObject root = new GameObject("__OcclusionAreas");
            Undo.RegisterCreatedObjectUndo(root, "Create OcclusionAreas Root");
            root.transform.position = bounds.center;

            GameObject go = new GameObject("OcclusionArea");
            Undo.RegisterCreatedObjectUndo(go, "Create OcclusionArea");
            go.transform.SetParent(root.transform, false);

            OcclusionArea area = go.AddComponent<OcclusionArea>();
            area.center = Vector3.zero;
            area.size = bounds.size;
            TryEnableViewVolume(area);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private static void TryEnableViewVolume(OcclusionArea area)
        {
            if (area == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(area);
            SerializedProperty property = serialized.FindProperty("m_IsViewVolume") ?? serialized.FindProperty("isViewVolume");
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ClearAllAreas()
        {
            OcclusionArea[] areas = UnityEngine.Object.FindObjectsOfType<OcclusionArea>(true);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            int removed = 0;
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i] == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(areas[i].gameObject);
                removed++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_AREAS_TITLE"), string.Format(AP_Loc.T("AP_OCC_CLEAR_RESULT"), removed), AP_Loc.T("AP_DBG_OK"));
        }

        private static void OpenOcclusionWindow()
        {
            Type windowType = Type.GetType("UnityEditor.OcclusionCullingWindow,UnityEditor");
            if (windowType != null)
            {
                EditorWindow.GetWindow(windowType, true, "Occlusion Culling").Show();
                return;
            }

            EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_BAKE_TITLE"), AP_Loc.T("AP_OCC_WINDOW_FALLBACK"), AP_Loc.T("AP_DBG_OK"));
        }

        private static void TryBakeInBackground()
        {
            Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;
            Type type = editorAssembly.GetType("UnityEditor.SceneManagement.StaticOcclusionCulling");
            if (type == null)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_BAKE_TITLE"), AP_Loc.T("AP_OCC_BAKE_UNAVAILABLE"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            MethodInfo method = type.GetMethod("GenerateInBackground", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_BAKE_TITLE"), AP_Loc.T("AP_OCC_BAKE_UNAVAILABLE"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            try
            {
                method.Invoke(null, null);
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_BAKE_TITLE"), AP_Loc.T("AP_OCC_BAKE_STARTED"), AP_Loc.T("AP_DBG_OK"));
            }
            catch
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCC_BAKE_TITLE"), AP_Loc.T("AP_OCC_BAKE_FAILED"), AP_Loc.T("AP_DBG_OK"));
            }
        }
    }
}
#endif
