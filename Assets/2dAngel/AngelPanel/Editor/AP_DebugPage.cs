#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    public static class AP_DebugPage
    {
        private sealed class ShaderBucket
        {
            public Shader Shader;
            public string Name;
            public int SlotCount;
            public readonly HashSet<Material> Materials = new HashSet<Material>();
            public readonly HashSet<GameObject> Objects = new HashSet<GameObject>();
        }

        private sealed class EventDefinition
        {
            public readonly string MethodName;
            public readonly string LocKey;
            public readonly string RiskLocKey;
            public readonly string HintLocKey;
            public readonly Func<MethodInfo, bool> Validator;

            public EventDefinition(string methodName, string locKey, string riskLocKey, string hintLocKey, Func<MethodInfo, bool> validator)
            {
                MethodName = methodName;
                LocKey = locKey;
                RiskLocKey = riskLocKey;
                HintLocKey = hintLocKey;
                Validator = validator;
            }
        }

        private sealed class EventBucket
        {
            public readonly EventDefinition Definition;
            public int ComponentCount;
            public readonly HashSet<GameObject> Objects = new HashSet<GameObject>();
            public readonly HashSet<Type> Types = new HashSet<Type>();
            public readonly HashSet<MonoScript> ScriptAssets = new HashSet<MonoScript>();

            public EventBucket(EventDefinition definition)
            {
                Definition = definition;
            }
        }

        private sealed class ScriptAssetAudit
        {
            public MonoScript Script;
            public string DisplayName;
            public readonly List<string> EventNames = new List<string>();
        }

        private static readonly EventDefinition[] EventDefinitions =
        {
            new EventDefinition("Update", "AP_DBG_EVT_UPDATE", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_UPDATE", m => HasExactVoidSignature(m)),
            new EventDefinition("LateUpdate", "AP_DBG_EVT_LATEUPDATE", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_LATEUPDATE", m => HasExactVoidSignature(m)),
            new EventDefinition("FixedUpdate", "AP_DBG_EVT_FIXEDUPDATE", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_FIXEDUPDATE", m => HasExactVoidSignature(m)),
            new EventDefinition("OnGUI", "AP_DBG_EVT_ONGUI", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_ONGUI", m => HasExactVoidSignature(m)),
            new EventDefinition("OnWillRenderObject", "AP_DBG_EVT_ONWILLRENDEROBJECT", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_ONWILLRENDEROBJECT", m => HasExactVoidSignature(m)),
            new EventDefinition("OnRenderObject", "AP_DBG_EVT_ONRENDEROBJECT", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_ONRENDEROBJECT", m => HasExactVoidSignature(m)),
            new EventDefinition("OnPreCull", "AP_DBG_EVT_ONPRECULL", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_ONPRECULL", m => HasExactVoidSignature(m)),
            new EventDefinition("OnPreRender", "AP_DBG_EVT_ONPRERENDER", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_ONPRERENDER", m => HasExactVoidSignature(m)),
            new EventDefinition("OnPostRender", "AP_DBG_EVT_ONPOSTRENDER", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_ONPOSTRENDER", m => HasExactVoidSignature(m)),
            new EventDefinition("OnRenderImage", "AP_DBG_EVT_ONRENDERIMAGE", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_ONRENDERIMAGE", HasOnRenderImageSignature),
            new EventDefinition("OnAudioFilterRead", "AP_DBG_EVT_ONAUDIOFILTERREAD", "AP_DBG_RISK_HIGH", "AP_DBG_HINT_ONAUDIOFILTERREAD", HasOnAudioFilterReadSignature),
            new EventDefinition("OnAnimatorMove", "AP_DBG_EVT_ONANIMATORMOVE", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_ONANIMATORMOVE", m => HasExactVoidSignature(m)),
            new EventDefinition("OnAnimatorIK", "AP_DBG_EVT_ONANIMATORIK", "AP_DBG_RISK_MEDIUM", "AP_DBG_HINT_ONANIMATORIK", HasOnAnimatorIKSignature)
        };

        private static readonly Dictionary<string, EventDefinition> EventByName = EventDefinitions.ToDictionary(x => x.MethodName, StringComparer.Ordinal);
        private static readonly Dictionary<Type, int> EventMaskCache = new Dictionary<Type, int>();

        private static int currentTab;
        private static int scopeMode = 1;
        private static Transform manualRoot;
        private static bool includeInactive = true;
        private static string shaderFilter = string.Empty;
        private static Vector2 shaderScroll;
        private static Vector2 eventScroll;
        private static Vector2 scriptAssetScroll;

        private static bool hasScan;
        private static int rendererCount;
        private static int materialSlotCount;
        private static int uniqueMaterialCount;
        private static int udonBehaviourCount;
        private static int udonSharpCount;
        private static int pickupCount;
        private static int objectSyncCount;
        private static bool vrcTypesMissing;

        private static bool hasEventScan;
        private static int scannedBehaviourCount;
        private static int scannedTypeCount;
        private static readonly List<EventBucket> EventBuckets = new List<EventBucket>();
        private static readonly Dictionary<string, EventBucket> EventBucketMap = new Dictionary<string, EventBucket>(StringComparer.Ordinal);

        private static bool hasScriptAssetScan;
        private static int selectedScriptAssetCount;
        private static readonly List<ScriptAssetAudit> ScriptAssetAudits = new List<ScriptAssetAudit>();

        private static readonly HashSet<GameObject> UdonObjects = new HashSet<GameObject>();
        private static readonly HashSet<GameObject> UdonSharpObjects = new HashSet<GameObject>();
        private static readonly HashSet<GameObject> PickupObjects = new HashSet<GameObject>();
        private static readonly HashSet<GameObject> ObjectSyncObjects = new HashSet<GameObject>();
        private static readonly List<GameObject> MissingScriptObjects = new List<GameObject>(256);
        private static readonly List<GameObject> MissingPrefabObjects = new List<GameObject>(128);
        private static readonly List<ShaderBucket> Buckets = new List<ShaderBucket>(128);
        private static readonly Dictionary<int, ShaderBucket> BucketMap = new Dictionary<int, ShaderBucket>(256);

        private static Type tUdonBehaviour;
        private static Type tUdonSharpBehaviour;
        private static Type tVRCPickup;
        private static Type tVRCObjectSync;

        public static void Draw(AP_HostContext context)
        {
            if (context == null || context.HostWindow == null)
            {
                return;
            }

            DebugStyles styles = DebugStyles.Create(context.HostWindow);

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_Opt_TAB_DEBUG"), styles.SectionTitle);

            GUILayout.Space(4f);
            DrawTabBar(styles);
            GUILayout.Space(6f);

            switch (currentTab)
            {
                case 0:
                    DrawBasicScanPage(styles);
                    break;

                case 1:
                    DrawEventAuditPage(styles);
                    break;

                default:
                    DrawScriptAssetPage(styles);
                    break;
            }
        }

        private static void DrawTabBar(DebugStyles styles)
        {
            string[] tabs =
            {
                AP_Loc.T("AP_DBG_TAB_BASIC"),
                AP_Loc.T("AP_DBG_TAB_EVENTS"),
                AP_Loc.T("AP_DBG_TAB_SCRIPT_ASSETS")
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    bool selected = currentTab == i;
                    Color old = GUI.backgroundColor;
                    if (selected)
                    {
                        GUI.backgroundColor = new Color(0.20f, 0.42f, 0.74f, 1f);
                    }

                    if (GUILayout.Button(tabs[i], styles.TabButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        currentTab = i;
                        GUI.FocusControl(null);
                    }

                    GUI.backgroundColor = old;
                }
            }
        }

        private static void DrawBasicScanPage(DebugStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_HEADER"), styles.CardTitle);

                GUILayout.Space(8f);
                DrawScopeUI(styles);

                GUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_SCAN"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        Scan();
                    }

                    GUILayout.FlexibleSpace();
                    includeInactive = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_DBG_INCLUDE_INACTIVE"), includeInactive, GUILayout.Width(Mathf.Clamp(180f + styles.FontSize * 6f, 220f, 340f)));
                }

                if (!hasScan)
                {
                    return;
                }

                GUILayout.Space(6f);
                DrawWarnings(styles);
                GUILayout.Space(6f);
                DrawComponentCounts(styles);
                GUILayout.Space(8f);
                DrawShaderSection(styles);
            }
        }

        private static void DrawEventAuditPage(DebugStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_EVT_PAGE_TITLE"), styles.CardTitle);
                GUILayout.Space(8f);

                DrawScopeUI(styles);

                GUILayout.Space(6f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_EVT_SCAN_SCOPE"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        ScanRuntimeEvents();
                    }

                    GUILayout.FlexibleSpace();
                    includeInactive = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_DBG_INCLUDE_INACTIVE"), includeInactive, GUILayout.Width(Mathf.Clamp(180f + styles.FontSize * 6f, 220f, 340f)));
                }

                if (!hasEventScan)
                {
                    return;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(AP_Loc.T("AP_DBG_EVT_SUMMARY"), styles.CardTitle);
                    DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_SCANNED_COMPONENTS"), scannedBehaviourCount.ToString(), styles);
                    DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_SCANNED_TYPES"), scannedTypeCount.ToString(), styles);
                    DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_TOTAL_HOTCALLS"), EventBuckets.Sum(x => x.ComponentCount).ToString(), styles);
                }

                GUILayout.Space(6f);
                eventScroll = EditorGUILayout.BeginScrollView(eventScroll, GUILayout.MinHeight(260f));
                bool anyRow = false;

                for (int i = 0; i < EventBuckets.Count; i++)
                {
                    EventBucket bucket = EventBuckets[i];
                    if (bucket == null || bucket.ComponentCount <= 0)
                    {
                        continue;
                    }

                    anyRow = true;
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(AP_Loc.T(bucket.Definition.LocKey), styles.CardTitle);
                            GUILayout.Space(8f);
                            GUILayout.Label(AP_Loc.T(bucket.Definition.RiskLocKey), styles.Badge);
                            GUILayout.FlexibleSpace();
                            GUILayout.Label($"{AP_Loc.T("AP_DBG_COUNT")} {bucket.ComponentCount}", styles.Value);
                        }

                        GUILayout.Space(4f);
                        DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_OBJECTS"), bucket.Objects.Count.ToString(), styles);
                        DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_TYPES"), bucket.Types.Count.ToString(), styles);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(bucket.Objects.Count == 0))
                            {
                                if (GUILayout.Button(AP_Loc.T("AP_DBG_SELECT_OBJECTS"), styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                                {
                                    Selection.objects = bucket.Objects.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
                                }
                            }

                            using (new EditorGUI.DisabledScope(bucket.Types.Count == 0))
                            {
                                if (GUILayout.Button(AP_Loc.T("AP_DBG_EVT_SELECT_SCRIPTS"), styles.SecondaryButton, GUILayout.Width(styles.MaterialButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                                {
                                    SelectScriptsForBucket(bucket);
                                }
                            }
                        }
                    }
                }

                if (!anyRow)
                {
                    GUILayout.Label(AP_Loc.T("AP_DBG_EVT_NONE"), styles.Note);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawScriptAssetPage(DebugStyles styles)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_SCRIPT_PAGE_TITLE"), styles.CardTitle);
                GUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_SCRIPT_SCAN_SELECTED"), styles.ActionButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        ScanSelectedScriptAssets();
                    }

                    GUILayout.FlexibleSpace();
                }

                if (!hasScriptAssetScan)
                {
                    return;
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawKeyValueRow(AP_Loc.T("AP_DBG_SCRIPT_SELECTED_COUNT"), selectedScriptAssetCount.ToString(), styles);
                    DrawKeyValueRow(AP_Loc.T("AP_DBG_EVT_TOTAL_HOTCALLS"), ScriptAssetAudits.Sum(x => x.EventNames.Count).ToString(), styles);
                }

                GUILayout.Space(6f);
                scriptAssetScroll = EditorGUILayout.BeginScrollView(scriptAssetScroll, GUILayout.MinHeight(240f));
                for (int i = 0; i < ScriptAssetAudits.Count; i++)
                {
                    ScriptAssetAudit audit = ScriptAssetAudits[i];
                    if (audit == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(audit.DisplayName, styles.CardTitle);
                            GUILayout.FlexibleSpace();
                            if (audit.Script != null && GUILayout.Button(AP_Loc.T("AP_DBG_PING_SCRIPT"), styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                            {
                                EditorGUIUtility.PingObject(audit.Script);
                                Selection.activeObject = audit.Script;
                            }
                        }

                        if (audit.EventNames.Count == 0)
                        {
                            GUILayout.Label(AP_Loc.T("AP_DBG_SCRIPT_NONE"), styles.Note);
                        }
                        else
                        {
                            GUILayout.Label(string.Join(", ", audit.EventNames), styles.Note);
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawScopeUI(DebugStyles styles)
        {
            string[] modes =
            {
                AP_Loc.T("AP_DBG_SCOPE_SCENE"),
                AP_Loc.T("AP_DBG_SCOPE_SELECTION"),
                AP_Loc.T("AP_DBG_SCOPE_MANUAL")
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_SCOPE"), styles.Label, GUILayout.Width(styles.LabelWidth));
                scopeMode = EditorGUILayout.Popup(scopeMode, modes, GUILayout.MinWidth(180f));
            }

            if (scopeMode == 2)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_DBG_MANUAL_ROOT"), styles.Label, GUILayout.Width(styles.LabelWidth));
                    manualRoot = (Transform)EditorGUILayout.ObjectField(manualRoot, typeof(Transform), true);
                }
            }
        }

        private static IEnumerable<GameObject> EnumerateRoots()
        {
            if (scopeMode == 0)
            {
                Scene scene = SceneManager.GetActiveScene();
                if (!scene.IsValid())
                {
                    yield break;
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root != null)
                    {
                        yield return root;
                    }
                }

                yield break;
            }

            if (scopeMode == 2)
            {
                if (manualRoot != null)
                {
                    yield return manualRoot.gameObject;
                }

                yield break;
            }

            if (Selection.transforms == null || Selection.transforms.Length == 0)
            {
                yield break;
            }

            HashSet<GameObject> set = new HashSet<GameObject>();
            for (int i = 0; i < Selection.transforms.Length; i++)
            {
                Transform transform = Selection.transforms[i];
                if (transform == null)
                {
                    continue;
                }

                if (set.Add(transform.gameObject))
                {
                    yield return transform.gameObject;
                }
            }
        }

        private static void Scan()
        {
            ResetScanResults();

            GameObject[] roots = EnumerateRoots().ToArray();
            if (roots.Length == 0)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_DBG_PAGE_TITLE"), AP_Loc.T("AP_DBG_NOTHING_TO_SCAN"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            ResolveReflectedTypes();

            HashSet<GameObject> allObjects = CollectScopeObjects(roots);
            ScanMissingReferences(allObjects);
            ScanSpecialComponents(allObjects);
            ScanShaders(allObjects);
            hasScan = true;
        }

        private static void ScanRuntimeEvents()
        {
            ResetEventResults();

            GameObject[] roots = EnumerateRoots().ToArray();
            if (roots.Length == 0)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_DBG_EVT_PAGE_TITLE"), AP_Loc.T("AP_DBG_NOTHING_TO_SCAN"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            HashSet<GameObject> allObjects = CollectScopeObjects(roots);
            HashSet<Type> scannedTypes = new HashSet<Type>();

            foreach (GameObject go in allObjects)
            {
                if (go == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
                if (behaviours == null)
                {
                    continue;
                }

                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    Type type = behaviour.GetType();
                    if (type == null)
                    {
                        continue;
                    }

                    scannedBehaviourCount++;
                    scannedTypes.Add(type);

                    int mask = GetEventMask(type);
                    if (mask == 0)
                    {
                        continue;
                    }

                    for (int bit = 0; bit < EventDefinitions.Length; bit++)
                    {
                        if ((mask & (1 << bit)) == 0)
                        {
                            continue;
                        }

                        EventDefinition definition = EventDefinitions[bit];
                        EventBucket bucket = GetOrCreateEventBucket(definition);
                        bucket.ComponentCount++;
                        bucket.Objects.Add(go);
                        bucket.Types.Add(type);
                    }
                }
            }

            scannedTypeCount = scannedTypes.Count;
            EventBuckets.Sort((a, b) => b.ComponentCount.CompareTo(a.ComponentCount));
            hasEventScan = true;
        }

        private static void ScanSelectedScriptAssets()
        {
            ResetScriptAssetResults();

            MonoScript[] scripts = Selection.objects.OfType<MonoScript>().Distinct().ToArray();
            selectedScriptAssetCount = scripts.Length;
            if (scripts.Length == 0)
            {
                hasScriptAssetScan = true;
                return;
            }

            for (int i = 0; i < scripts.Length; i++)
            {
                MonoScript script = scripts[i];
                if (script == null)
                {
                    continue;
                }

                ScriptAssetAudit audit = new ScriptAssetAudit
                {
                    Script = script,
                    DisplayName = script.name
                };

                Type classType = script.GetClass();
                if (classType != null)
                {
                    int mask = GetEventMask(classType);
                    for (int bit = 0; bit < EventDefinitions.Length; bit++)
                    {
                        if ((mask & (1 << bit)) == 0)
                        {
                            continue;
                        }

                        audit.EventNames.Add(AP_Loc.T(EventDefinitions[bit].LocKey));
                    }
                }
                else
                {
                    string path = AssetDatabase.GetAssetPath(script);
                    string fullPath = string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path);
                    if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
                    {
                        string text = File.ReadAllText(fullPath);
                        for (int bit = 0; bit < EventDefinitions.Length; bit++)
                        {
                            if (LooksLikeSourceContainsEvent(text, EventDefinitions[bit].MethodName))
                            {
                                audit.EventNames.Add(AP_Loc.T(EventDefinitions[bit].LocKey));
                            }
                        }
                    }
                }

                audit.EventNames.Sort(StringComparer.Ordinal);
                ScriptAssetAudits.Add(audit);
            }

            ScriptAssetAudits.Sort((a, b) => string.CompareOrdinal(a.DisplayName, b.DisplayName));
            hasScriptAssetScan = true;
        }

        private static bool LooksLikeSourceContainsEvent(string text, string methodName)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            string pattern = $@"\b(?:public|private|protected|internal)?(?:\s+(?:virtual|override|new|sealed|extern|unsafe|async|static))*\s+[\w<>,\[\]\.?]+\s+{Regex.Escape(methodName)}\s*\(";
            return Regex.IsMatch(text, pattern);
        }

        private static EventBucket GetOrCreateEventBucket(EventDefinition definition)
        {
            if (!EventBucketMap.TryGetValue(definition.MethodName, out EventBucket bucket))
            {
                bucket = new EventBucket(definition);
                EventBucketMap.Add(definition.MethodName, bucket);
                EventBuckets.Add(bucket);
            }

            return bucket;
        }

        private static HashSet<GameObject> CollectScopeObjects(GameObject[] roots)
        {
            HashSet<GameObject> allObjects = new HashSet<GameObject>();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive);
                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform tr = transforms[j];
                    if (tr != null)
                    {
                        allObjects.Add(tr.gameObject);
                    }
                }
            }

            return allObjects;
        }

        private static void ScanMissingReferences(HashSet<GameObject> allObjects)
        {
            foreach (GameObject go in allObjects)
            {
                if (go == null)
                {
                    continue;
                }

                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0)
                {
                    MissingScriptObjects.Add(go);
                }

                PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(go);
                if (instanceStatus == PrefabInstanceStatus.MissingAsset || assetType == PrefabAssetType.MissingAsset)
                {
                    MissingPrefabObjects.Add(go);
                }
            }
        }

        private static void ScanSpecialComponents(HashSet<GameObject> allObjects)
        {
            foreach (GameObject go in allObjects)
            {
                if (go == null)
                {
                    continue;
                }

                CountComponentBucket(go, tUdonBehaviour, ref udonBehaviourCount, UdonObjects);
                CountComponentBucket(go, tUdonSharpBehaviour, ref udonSharpCount, UdonSharpObjects);
                CountComponentBucket(go, tVRCPickup, ref pickupCount, PickupObjects);
                CountComponentBucket(go, tVRCObjectSync, ref objectSyncCount, ObjectSyncObjects);
            }
        }

        private static void ScanShaders(HashSet<GameObject> allObjects)
        {
            HashSet<Material> uniqueMaterials = new HashSet<Material>();
            foreach (GameObject go in allObjects)
            {
                if (go == null)
                {
                    continue;
                }

                Renderer[] renderers = go.GetComponents<Renderer>();
                if (renderers == null || renderers.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    rendererCount++;
                    Material[] mats = renderer.sharedMaterials;
                    if (mats == null)
                    {
                        continue;
                    }

                    for (int m = 0; m < mats.Length; m++)
                    {
                        Material material = mats[m];
                        if (material == null)
                        {
                            continue;
                        }

                        materialSlotCount++;
                        uniqueMaterials.Add(material);

                        Shader shader = material.shader;
                        int key = shader != null ? shader.GetInstanceID() : 0;
                        string name = shader != null ? shader.name : "<Missing Shader>";

                        if (!BucketMap.TryGetValue(key, out ShaderBucket bucket))
                        {
                            bucket = new ShaderBucket
                            {
                                Shader = shader,
                                Name = name,
                                SlotCount = 0
                            };
                            BucketMap.Add(key, bucket);
                            Buckets.Add(bucket);
                        }

                        bucket.SlotCount++;
                        bucket.Materials.Add(material);
                        bucket.Objects.Add(renderer.gameObject);
                    }
                }
            }

            uniqueMaterialCount = uniqueMaterials.Count;
            Buckets.Sort((a, b) => b.SlotCount.CompareTo(a.SlotCount));
        }

        private static void DrawWarnings(DebugStyles styles)
        {
            if (MissingScriptObjects.Count > 0)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label($"{AP_Loc.T("AP_DBG_MISSING_SCRIPTS")}  {MissingScriptObjects.Count}", styles.WarningTitle);
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_SELECT_MISSING_SCRIPTS"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        Selection.objects = MissingScriptObjects.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
                    }
                }
            }

            if (MissingPrefabObjects.Count > 0)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label($"{AP_Loc.T("AP_DBG_MISSING_PREFABS")}  {MissingPrefabObjects.Count}", styles.WarningTitle);
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_SELECT_MISSING_PREFABS"), styles.SecondaryButton, GUILayout.Height(styles.ButtonHeight)))
                    {
                        Selection.objects = MissingPrefabObjects.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
                    }
                }
            }
        }

        private static void DrawComponentCounts(DebugStyles styles)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_COUNTS_HEADER"), styles.CardTitle);

                DrawKeyValueRow(AP_Loc.T("AP_DBG_RENDERERS"), rendererCount.ToString(), styles);
                DrawKeyValueRow(AP_Loc.T("AP_DBG_MATERIAL_SLOTS"), materialSlotCount.ToString(), styles);
                DrawKeyValueRow(AP_Loc.T("AP_DBG_UNIQUE_MATERIALS"), uniqueMaterialCount.ToString(), styles);
                GUILayout.Space(6f);

                DrawSelectableCountRow(AP_Loc.T("AP_DBG_UDON"), udonBehaviourCount, UdonObjects, AP_Loc.T("AP_DBG_SELECT_UDON"), styles);
                DrawSelectableCountRow(AP_Loc.T("AP_DBG_UDONSHARP"), udonSharpCount, UdonSharpObjects, AP_Loc.T("AP_DBG_SELECT_UDONSHARP"), styles);
                DrawSelectableCountRow(AP_Loc.T("AP_DBG_PICKUP"), pickupCount, PickupObjects, AP_Loc.T("AP_DBG_SELECT_PICKUP"), styles);
                DrawSelectableCountRow(AP_Loc.T("AP_DBG_OBJECTSYNC"), objectSyncCount, ObjectSyncObjects, AP_Loc.T("AP_DBG_SELECT_OBJECTSYNC"), styles);
            }
        }

        private static void DrawSelectableCountRow(string label, int count, HashSet<GameObject> objects, string buttonLabel, DebugStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, styles.Label, GUILayout.Width(styles.LabelWidth));
                GUILayout.Label(count.ToString(), styles.Value, GUILayout.Width(styles.ValueWidth));
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(objects == null || objects.Count == 0))
                {
                    if (GUILayout.Button(buttonLabel, styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                    {
                        Selection.objects = objects.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
                    }
                }
            }
        }

        private static void DrawShaderSection(DebugStyles styles)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(AP_Loc.T("AP_DBG_SHADERS_HEADER"), styles.CardTitle);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_DBG_FILTER"), styles.Label, GUILayout.Width(56f));
                    shaderFilter = EditorGUILayout.TextField(shaderFilter);
                    if (GUILayout.Button(AP_Loc.T("AP_DBG_CLEAR"), styles.SecondaryButton, GUILayout.Width(72f), GUILayout.Height(styles.ButtonHeight)))
                    {
                        shaderFilter = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                GUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_DBG_SHADER_NAME"), styles.SubLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T("AP_DBG_COUNT"), styles.SubLabel, GUILayout.Width(60f));
                }

                shaderScroll = EditorGUILayout.BeginScrollView(shaderScroll, GUILayout.MinHeight(220f));

                string filter = (shaderFilter ?? string.Empty).Trim();
                bool useFilter = filter.Length > 0;
                string filterLower = useFilter ? filter.ToLowerInvariant() : string.Empty;

                for (int i = 0; i < Buckets.Count; i++)
                {
                    ShaderBucket bucket = Buckets[i];
                    if (bucket == null)
                    {
                        continue;
                    }

                    if (useFilter && (bucket.Name == null || !bucket.Name.ToLowerInvariant().Contains(filterLower)))
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(bucket.Name, styles.ShaderNameButton))
                        {
                            SelectObjects(bucket);
                        }

                        GUILayout.FlexibleSpace();
                        GUILayout.Label(bucket.SlotCount.ToString(), styles.Value, GUILayout.Width(60f));

                        if (GUILayout.Button(AP_Loc.T("AP_DBG_SELECT_OBJECTS"), styles.SecondaryButton, GUILayout.Width(styles.SelectButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                        {
                            SelectObjects(bucket);
                        }

                        if (GUILayout.Button(AP_Loc.T("AP_DBG_SELECT_MATERIALS"), styles.SecondaryButton, GUILayout.Width(styles.MaterialButtonWidth), GUILayout.Height(styles.ButtonHeight)))
                        {
                            SelectMaterials(bucket);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private static void DrawKeyValueRow(string label, string value, DebugStyles styles)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, styles.Label, GUILayout.Width(styles.LabelWidth));
                GUILayout.Label(value, styles.Value);
            }
        }

        private static void ResetScanResults()
        {
            hasScan = false;
            rendererCount = 0;
            materialSlotCount = 0;
            uniqueMaterialCount = 0;
            udonBehaviourCount = 0;
            udonSharpCount = 0;
            pickupCount = 0;
            objectSyncCount = 0;
            vrcTypesMissing = false;

            UdonObjects.Clear();
            UdonSharpObjects.Clear();
            PickupObjects.Clear();
            ObjectSyncObjects.Clear();
            MissingScriptObjects.Clear();
            MissingPrefabObjects.Clear();
            Buckets.Clear();
            BucketMap.Clear();
        }

        private static void ResetEventResults()
        {
            hasEventScan = false;
            scannedBehaviourCount = 0;
            scannedTypeCount = 0;
            EventBuckets.Clear();
            EventBucketMap.Clear();
        }

        private static void ResetScriptAssetResults()
        {
            hasScriptAssetScan = false;
            selectedScriptAssetCount = 0;
            ScriptAssetAudits.Clear();
        }

        private static void ResolveReflectedTypes()
        {
            tUdonBehaviour = FindTypeByFullName("VRC.Udon.UdonBehaviour");
            tUdonSharpBehaviour = FindTypeByFullName("UdonSharp.UdonSharpBehaviour");
            tVRCPickup = FindTypeByFullName("VRC.SDK3.Components.VRCPickup");
            tVRCObjectSync = FindTypeByFullName("VRC.SDK3.Components.VRCObjectSync");
            vrcTypesMissing = tUdonBehaviour == null && tVRCPickup == null && tVRCObjectSync == null;
        }

        private static void CountComponentBucket(GameObject go, Type type, ref int totalCount, HashSet<GameObject> bucket)
        {
            int count = CountComponents(go, type);
            if (count <= 0)
            {
                return;
            }

            totalCount += count;
            bucket.Add(go);
        }

        private static int CountComponents(GameObject go, Type type)
        {
            if (go == null || type == null)
            {
                return 0;
            }

            try
            {
                Component[] comps = go.GetComponents(type);
                return comps != null ? comps.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static Type FindTypeByFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return null;
            }

            Type direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null)
                {
                    continue;
                }

                try
                {
                    Type found = assembly.GetType(fullName, false);
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static int GetEventMask(Type type)
        {
            if (type == null)
            {
                return 0;
            }

            if (EventMaskCache.TryGetValue(type, out int cached))
            {
                return cached;
            }

            int mask = 0;
            for (int i = 0; i < EventDefinitions.Length; i++)
            {
                MethodInfo method = FindLifecycleMethod(type, EventDefinitions[i]);
                if (method != null)
                {
                    mask |= (1 << i);
                }
            }

            EventMaskCache[type] = mask;
            return mask;
        }

        private static MethodInfo FindLifecycleMethod(Type type, EventDefinition definition)
        {
            if (type == null || definition == null)
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            Type current = type;
            while (current != null && current != typeof(MonoBehaviour) && current != typeof(Behaviour) && current != typeof(Component))
            {
                MethodInfo[] methods;
                try
                {
                    methods = current.GetMethods(flags);
                }
                catch
                {
                    methods = Array.Empty<MethodInfo>();
                }

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || !string.Equals(method.Name, definition.MethodName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (definition.Validator == null || definition.Validator(method))
                    {
                        return method;
                    }
                }

                current = current.BaseType;
            }

            return null;
        }

        private static bool HasExactVoidSignature(MethodInfo method)
        {
            return method != null && method.ReturnType == typeof(void) && method.GetParameters().Length == 0;
        }

        private static bool HasOnRenderImageSignature(MethodInfo method)
        {
            if (method == null || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(RenderTexture) && parameters[1].ParameterType == typeof(RenderTexture);
        }

        private static bool HasOnAudioFilterReadSignature(MethodInfo method)
        {
            if (method == null || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(float[]) && parameters[1].ParameterType == typeof(int);
        }

        private static bool HasOnAnimatorIKSignature(MethodInfo method)
        {
            if (method == null || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
        }

        private static void SelectScriptsForBucket(EventBucket bucket)
        {
            if (bucket == null || bucket.Types.Count == 0)
            {
                return;
            }

            List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
            MonoScript[] scripts = MonoImporter.GetAllRuntimeMonoScripts();
            for (int i = 0; i < scripts.Length; i++)
            {
                MonoScript script = scripts[i];
                if (script == null)
                {
                    continue;
                }

                Type type = script.GetClass();
                if (type != null && bucket.Types.Contains(type))
                {
                    assets.Add(script);
                }
            }

            Selection.objects = assets.Distinct().ToArray();
        }

        private static void SelectObjects(ShaderBucket bucket)
        {
            if (bucket == null)
            {
                return;
            }

            Selection.objects = bucket.Objects.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
        }

        private static void SelectMaterials(ShaderBucket bucket)
        {
            if (bucket == null)
            {
                return;
            }

            Selection.objects = bucket.Materials.Where(x => x != null).Cast<UnityEngine.Object>().ToArray();
        }

        private readonly struct DebugStyles
        {
            public readonly GUIStyle SectionTitle;
            public readonly GUIStyle CardTitle;
            public readonly GUIStyle Note;
            public readonly GUIStyle Label;
            public readonly GUIStyle SubLabel;
            public readonly GUIStyle Value;
            public readonly GUIStyle Badge;
            public readonly GUIStyle TabButton;
            public readonly GUIStyle ActionButton;
            public readonly GUIStyle SecondaryButton;
            public readonly GUIStyle ShaderNameButton;
            public readonly GUIStyle WarningTitle;
            public readonly int FontSize;
            public readonly float ButtonHeight;
            public readonly float LabelWidth;
            public readonly float ValueWidth;
            public readonly float SelectButtonWidth;
            public readonly float MaterialButtonWidth;

            private DebugStyles(
                GUIStyle sectionTitle,
                GUIStyle cardTitle,
                GUIStyle note,
                GUIStyle label,
                GUIStyle subLabel,
                GUIStyle value,
                GUIStyle badge,
                GUIStyle tabButton,
                GUIStyle actionButton,
                GUIStyle secondaryButton,
                GUIStyle shaderNameButton,
                GUIStyle warningTitle,
                int fontSize,
                float buttonHeight,
                float labelWidth,
                float valueWidth,
                float selectButtonWidth,
                float materialButtonWidth)
            {
                SectionTitle = sectionTitle;
                CardTitle = cardTitle;
                Note = note;
                Label = label;
                SubLabel = subLabel;
                Value = value;
                Badge = badge;
                TabButton = tabButton;
                ActionButton = actionButton;
                SecondaryButton = secondaryButton;
                ShaderNameButton = shaderNameButton;
                WarningTitle = warningTitle;
                FontSize = fontSize;
                ButtonHeight = buttonHeight;
                LabelWidth = labelWidth;
                ValueWidth = valueWidth;
                SelectButtonWidth = selectButtonWidth;
                MaterialButtonWidth = materialButtonWidth;
            }

            public static DebugStyles Create(AP_Main hostWindow)
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

                GUIStyle subLabel = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = bodySize,
                    wordWrap = false
                };

                GUIStyle value = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleLeft
                };

                GUIStyle badge = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    fontSize = Mathf.Clamp(bodySize - 1, 9, 15),
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(6, 6, 3, 3)
                };
                badge.normal.textColor = new Color(0.83f, 0.89f, 1f);

                GUIStyle tabButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
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

                GUIStyle shaderNameButton = new GUIStyle(EditorStyles.label)
                {
                    fontSize = bodySize,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    stretchWidth = true
                };

                GUIStyle warningTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = cardSize
                };
                warningTitle.normal.textColor = new Color(1f, 0.55f, 0.55f);

                float buttonHeight = Mathf.Clamp(22f + (bodySize - 11f) * 2f, 22f, 36f);
                float labelWidth = Mathf.Clamp(168f + (bodySize - 11f) * 10f, 168f, 300f);
                float valueWidth = Mathf.Clamp(72f + (bodySize - 11f) * 6f, 72f, 120f);
                float selectButtonWidth = Mathf.Clamp(132f + (bodySize - 11f) * 8f, 132f, 220f);
                float materialButtonWidth = Mathf.Clamp(148f + (bodySize - 11f) * 8f, 148f, 236f);

                return new DebugStyles(sectionTitle, cardTitle, note, label, subLabel, value, badge, tabButton, actionButton, secondaryButton, shaderNameButton, warningTitle, bodySize, buttonHeight, labelWidth, valueWidth, selectButtonWidth, materialButtonWidth);
            }
        }
    }
}
#endif
