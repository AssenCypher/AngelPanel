#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    [Serializable]
    public sealed partial class AP_QuickOps
    {
        public enum ConfirmAction
        {
            None = 0,
            RemoveMissingScripts = 1,
            RemoveMonoBehaviourScripts = 2
        }

        public enum LayoutMode
        {
            Advanced = 0,
            SimpleCompact = 1
        }

        private delegate void SectionDrawer(Context context);
        private delegate void ConfirmHandler(Context context);

        private static SectionDrawer colliderSectionDrawer;
        private static SectionDrawer scriptCleanupSectionDrawer;
        private static SectionDrawer lodSectionDrawer;

        private static readonly Dictionary<ConfirmAction, ConfirmHandler> ConfirmHandlers = new Dictionary<ConfirmAction, ConfirmHandler>();

        [SerializeField] private State state = new State();

        public State SerializedState => state;

        public static void RegisterColliderSection(Action<Context> drawer)
        {
            colliderSectionDrawer = drawer == null ? null : new SectionDrawer(drawer);
        }

        public static void RegisterScriptCleanupSection(Action<Context> drawer)
        {
            scriptCleanupSectionDrawer = drawer == null ? null : new SectionDrawer(drawer);
        }

        public static void RegisterLodSection(Action<Context> drawer)
        {
            lodSectionDrawer = drawer == null ? null : new SectionDrawer(drawer);
        }

        public static void RegisterConfirmHandler(ConfirmAction action, Action<Context> handler)
        {
            if (action == ConfirmAction.None)
            {
                return;
            }

            if (handler == null)
            {
                ConfirmHandlers.Remove(action);
                return;
            }

            ConfirmHandlers[action] = new ConfirmHandler(handler);
        }

        public void Draw(float windowWidth)
        {
            Draw(windowWidth, LayoutMode.Advanced);
        }

        public void Draw(float windowWidth, LayoutMode layoutMode)
        {
            AP_Loc.Init();
            EnsureState();

            float contentWidth = Mathf.Max(160f, windowWidth);
            Context context = BuildContext(windowWidth, contentWidth, layoutMode);

            if (layoutMode == LayoutMode.SimpleCompact)
            {
                DrawDenseHeader(context);
                GUILayout.Space(4f);
            }
            else
            {
                DrawHeader(context);
                GUILayout.Space(8f);
                DrawSelectionScope(context);
                GUILayout.Space(8f);
            }

            DrawOperationSections(context);
            GUILayout.Space(layoutMode == LayoutMode.SimpleCompact ? 4f : 8f);
            DrawConfirmationArea(context);
            DrawStatusArea(context);
        }

        private void EnsureState()
        {
            if (state == null)
            {
                state = new State();
            }
        }

        private Context BuildContext(float windowWidth, float contentWidth, LayoutMode layoutMode)
        {
            SelectionSnapshot snapshot = SelectionSnapshot.Build(state.includeChildren);
            return new Context(this, state, snapshot, windowWidth, contentWidth, layoutMode);
        }

        private void DrawHeader(Context context)
        {
            AP_EUI.SectionTitle(AP_Loc.T("AP_QO_TITLE"));

            using (AP_EUI.Card(AP_Loc.T("AP_QO_CARD_SUMMARY")))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    AP_EUI.DrawStatusPill(context.HasSelection ? AP_Loc.T("AP_QO_SELECTION_READY") : AP_Loc.T("AP_QO_SELECTION_EMPTY"),
                        context.HasSelection ? MessageType.Info : MessageType.Warning);
                    GUILayout.Space(6f);
                    AP_EUI.DrawStatusPill(context.IncludeChildren ? AP_Loc.T("AP_QO_SCOPE_INCLUDE_CHILDREN") : AP_Loc.T("AP_QO_SCOPE_ROOT_ONLY"), MessageType.Info);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(AP_Loc.T("AP_QO_TARGET_OBJECTS") + ": " + context.TargetCount.ToString("N0"), EditorStyles.boldLabel);
                }

            }
        }

        private void DrawDenseHeader(Context context)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(AP_Loc.T("AP_QO_TITLE"), EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    state.includeChildren = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_QO_INCLUDE_CHILDREN"), state.includeChildren, GUILayout.Width(126f));
                }

                GUILayout.Space(1f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    AP_EUI.DrawStatusPill(context.HasSelection ? AP_Loc.T("AP_QO_SELECTION_READY") : AP_Loc.T("AP_QO_SELECTION_EMPTY"),
                        context.HasSelection ? MessageType.Info : MessageType.Warning);
                    GUILayout.Space(4f);
                    GUILayout.Label(AP_Loc.T("AP_QO_TARGET_OBJECTS") + ": " + context.TargetCount.ToString("N0"), EditorStyles.miniLabel);
                    GUILayout.Space(8f);
                    GUILayout.Label(AP_Loc.T("AP_QO_COLLIDER_COMPONENTS") + ": " + context.ColliderCount.ToString("N0"), EditorStyles.miniLabel);
                    GUILayout.Space(8f);
                    GUILayout.Label(AP_Loc.T("AP_QO_MONOBEHAVIOUR_COMPONENTS") + ": " + context.MonoBehaviourCount.ToString("N0"), EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                }

                if (!string.IsNullOrWhiteSpace(context.RootNamesPreview))
                {
                    GUILayout.Space(2f);
                    AP_EUI.DrawMiniNote(context.RootNamesPreview);
                }
            }
        }

        private void DrawSelectionScope(Context context)
        {
            using (AP_EUI.Card(AP_Loc.T("AP_QO_CARD_SCOPE")))
            {
                state.includeChildren = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_QO_INCLUDE_CHILDREN"), state.includeChildren);
                GUILayout.Space(4f);
                AP_EUI.DrawKeyValue(AP_Loc.T("AP_QO_SELECTED_ROOTS"), context.RootCount.ToString("N0"));
                AP_EUI.DrawKeyValue(AP_Loc.T("AP_QO_TARGET_OBJECTS"), context.TargetCount.ToString("N0"));
                AP_EUI.DrawKeyValue(AP_Loc.T("AP_QO_RENDERER_COMPONENTS"), context.RendererCount.ToString("N0"));
                AP_EUI.DrawKeyValue(AP_Loc.T("AP_QO_COLLIDER_COMPONENTS"), context.ColliderCount.ToString("N0"));
                AP_EUI.DrawKeyValue(AP_Loc.T("AP_QO_MONOBEHAVIOUR_COMPONENTS"), context.MonoBehaviourCount.ToString("N0"));

                if (!string.IsNullOrWhiteSpace(context.RootNamesPreview))
                {
                    GUILayout.Space(4f);
                    EditorGUILayout.HelpBox(context.RootNamesPreview, MessageType.None);
                }
            }
        }

        private void DrawOperationSections(Context context)
        {
            if (context.IsSimpleCompact)
            {
                DrawDenseSection(context, AP_Loc.T("AP_QO_SECTION_COLLIDER"), colliderSectionDrawer);
                GUILayout.Space(3f);
                DrawDenseSection(context, AP_Loc.T("AP_QO_SECTION_SCRIPT_CLEANUP"), scriptCleanupSectionDrawer);
                GUILayout.Space(3f);
                DrawDenseSection(context, AP_Loc.T("AP_QO_SECTION_LOD"), lodSectionDrawer);
                return;
            }

            DrawSectionCard(context, ref state.showColliderSection, AP_Loc.T("AP_QO_SECTION_COLLIDER"), string.Empty, colliderSectionDrawer);
            GUILayout.Space(8f);
            DrawSectionCard(context, ref state.showScriptCleanupSection, AP_Loc.T("AP_QO_SECTION_SCRIPT_CLEANUP"), string.Empty, scriptCleanupSectionDrawer);
            GUILayout.Space(8f);
            DrawSectionCard(context, ref state.showLodSection, AP_Loc.T("AP_QO_SECTION_LOD"), string.Empty, lodSectionDrawer);
        }

        private void DrawDenseSection(Context context, string title, SectionDrawer drawer)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(title, EditorStyles.boldLabel);
                GUILayout.Space(2f);
                if (drawer != null)
                {
                    drawer(context);
                }
                else
                {
                    AP_EUI.DrawMiniNote(AP_Loc.T("AP_QO_SECTION_PENDING"));
                }
            }
        }

        private void DrawSectionCard(Context context, ref bool expanded, string title, string subtitle, SectionDrawer drawer)
        {
            using (AP_EUI.Card(title, subtitle))
            {
                expanded = EditorGUILayout.Foldout(expanded, title, true);
                if (!expanded)
                {
                    return;
                }

                GUILayout.Space(4f);
                if (drawer != null)
                {
                    drawer(context);
                }
                else
                {
                    EditorGUILayout.HelpBox(AP_Loc.T("AP_QO_SECTION_PENDING"), MessageType.Info);
                }
            }
        }

        private void DrawConfirmationArea(Context context)
        {
            if (state.pendingConfirmAction == ConfirmAction.None)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T(state.pendingConfirmTitleKey), EditorStyles.boldLabel);
                GUILayout.Space(2f);
                EditorGUILayout.HelpBox(AP_Loc.T(state.pendingConfirmBodyKey), state.pendingConfirmMessageType);
                GUILayout.Space(3f);

                using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(4f).Pad(0f))
                {
                    if (context.IsSimpleCompact)
                    {
                        if (row.MiniButton(AP_Loc.T("AP_QO_CONFIRM_EXECUTE")))
                        {
                            ExecutePendingConfirmation(context);
                        }

                        if (row.MiniButton(AP_Loc.T("AP_QO_CANCEL")))
                        {
                            context.ClearConfirmation();
                        }
                    }
                    else
                    {
                        if (row.Button(AP_Loc.T("AP_QO_CONFIRM_EXECUTE")))
                        {
                            ExecutePendingConfirmation(context);
                        }

                        if (row.MiniButton(AP_Loc.T("AP_QO_CANCEL")))
                        {
                            context.ClearConfirmation();
                        }
                    }
                }
            }
        }

        private void DrawStatusArea(Context context)
        {
            if (string.IsNullOrWhiteSpace(state.statusText))
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_QO_LAST_RESULT"), EditorStyles.boldLabel);
                GUILayout.Space(2f);
                EditorGUILayout.HelpBox(state.statusText, state.statusMessageType);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AP_Loc.T("AP_QO_DISMISS_STATUS"), EditorStyles.miniButton, GUILayout.Width(120f)))
                    {
                        context.ClearStatus();
                    }
                }
            }
        }

        private void ExecutePendingConfirmation(Context context)
        {
            if (ConfirmHandlers.TryGetValue(state.pendingConfirmAction, out ConfirmHandler handler) && handler != null)
            {
                handler(context);
            }
            else
            {
                context.PushStatus(AP_Loc.T("AP_QO_CONFIRM_HANDLER_MISSING"), MessageType.Warning);
            }

            context.ClearConfirmation();
        }

        [Serializable]
        public sealed class State
        {
            public bool includeChildren = true;
            public bool showColliderSection = true;
            public bool showScriptCleanupSection = true;
            public bool showLodSection = true;
            public ConfirmAction pendingConfirmAction = ConfirmAction.None;
            public string pendingConfirmTitleKey = string.Empty;
            public string pendingConfirmBodyKey = string.Empty;
            public MessageType pendingConfirmMessageType = MessageType.Warning;
            public string statusText = string.Empty;
            public MessageType statusMessageType = MessageType.Info;
        }

        public sealed class Context
        {
            private readonly State ownerState;
            private readonly SelectionSnapshot snapshot;

            internal Context(AP_QuickOps owner, State ownerState, SelectionSnapshot snapshot, float windowWidth, float contentWidth, LayoutMode layoutMode)
            {
                this.ownerState = ownerState;
                this.snapshot = snapshot;
                WindowWidth = windowWidth;
                ContentWidth = contentWidth;
                Mode = layoutMode;
                IsCompact = AP_EUI.IsCompact(windowWidth);
            }

            public float WindowWidth { get; private set; }
            public float ContentWidth { get; private set; }
            public bool IsCompact { get; private set; }
            public LayoutMode Mode { get; private set; }
            public bool IsSimpleCompact => Mode == LayoutMode.SimpleCompact;
            public bool HasSelection => snapshot.RootCount > 0;
            public bool IncludeChildren => ownerState.includeChildren;
            public int RootCount => snapshot.RootCount;
            public int TargetCount => snapshot.TargetCount;
            public int ColliderCount => snapshot.ColliderCount;
            public int RendererCount => snapshot.RendererCount;
            public int MonoBehaviourCount => snapshot.MonoBehaviourCount;
            public IReadOnlyList<GameObject> Roots => snapshot.Roots;
            public IReadOnlyList<Transform> Targets => snapshot.Targets;
            public string RootNamesPreview => snapshot.RootNamesPreview;
            public State UIState => ownerState;

            public void PushStatus(string text, MessageType type)
            {
                ownerState.statusText = text ?? string.Empty;
                ownerState.statusMessageType = type;
            }

            public void PushStatusFormat(string locKey, MessageType type, params object[] args)
            {
                string format = AP_Loc.T(locKey);
                PushStatus(args == null || args.Length == 0 ? format : string.Format(format, args), type);
            }

            public void ClearStatus()
            {
                ownerState.statusText = string.Empty;
                ownerState.statusMessageType = MessageType.Info;
            }

            public void RequestConfirmation(ConfirmAction action, string titleKey, string bodyKey, MessageType type)
            {
                ownerState.pendingConfirmAction = action;
                ownerState.pendingConfirmTitleKey = titleKey ?? string.Empty;
                ownerState.pendingConfirmBodyKey = bodyKey ?? string.Empty;
                ownerState.pendingConfirmMessageType = type;
            }

            public void ClearConfirmation()
            {
                ownerState.pendingConfirmAction = ConfirmAction.None;
                ownerState.pendingConfirmTitleKey = string.Empty;
                ownerState.pendingConfirmBodyKey = string.Empty;
                ownerState.pendingConfirmMessageType = MessageType.Warning;
            }

            public string T(string key)
            {
                return AP_Loc.T(key);
            }
        }

        internal sealed class SelectionSnapshot
        {
            public List<GameObject> Roots { get; private set; }
            public List<Transform> Targets { get; private set; }
            public int RootCount => Roots.Count;
            public int TargetCount => Targets.Count;
            public int ColliderCount { get; private set; }
            public int RendererCount { get; private set; }
            public int MonoBehaviourCount { get; private set; }
            public string RootNamesPreview { get; private set; }

            public static SelectionSnapshot Build(bool includeChildren)
            {
                SelectionSnapshot snapshot = new SelectionSnapshot
                {
                    Roots = new List<GameObject>(Selection.gameObjects.Length),
                    Targets = new List<Transform>(Selection.transforms.Length),
                    RootNamesPreview = string.Empty
                };

                HashSet<int> rootIds = new HashSet<int>();
                GameObject[] selectedObjects = Selection.gameObjects;
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    GameObject gameObject = selectedObjects[i];
                    if (gameObject == null || EditorUtility.IsPersistent(gameObject) || !gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    if (rootIds.Add(gameObject.GetInstanceID()))
                    {
                        snapshot.Roots.Add(gameObject);
                    }
                }

                HashSet<int> targetIds = new HashSet<int>();
                for (int i = 0; i < snapshot.Roots.Count; i++)
                {
                    Transform root = snapshot.Roots[i].transform;
                    if (root == null)
                    {
                        continue;
                    }

                    if (!includeChildren)
                    {
                        AddTarget(root, snapshot, targetIds);
                        continue;
                    }

                    Transform[] children = root.GetComponentsInChildren<Transform>(true);
                    for (int j = 0; j < children.Length; j++)
                    {
                        AddTarget(children[j], snapshot, targetIds);
                    }
                }

                snapshot.BuildStats();
                snapshot.RootNamesPreview = BuildPreview(snapshot.Roots);
                return snapshot;
            }

            private static void AddTarget(Transform transform, SelectionSnapshot snapshot, HashSet<int> targetIds)
            {
                if (transform == null)
                {
                    return;
                }

                if (!targetIds.Add(transform.GetInstanceID()))
                {
                    return;
                }

                snapshot.Targets.Add(transform);
            }

            private void BuildStats()
            {
                int colliderCount = 0;
                int rendererCount = 0;
                int monoBehaviourCount = 0;

                for (int i = 0; i < Targets.Count; i++)
                {
                    Transform target = Targets[i];
                    if (target == null)
                    {
                        continue;
                    }

                    colliderCount += target.GetComponents<Collider>().Length;
                    rendererCount += target.GetComponents<Renderer>().Length;
                    monoBehaviourCount += target.GetComponents<MonoBehaviour>().Length;
                }

                ColliderCount = colliderCount;
                RendererCount = rendererCount;
                MonoBehaviourCount = monoBehaviourCount;
            }

            private static string BuildPreview(List<GameObject> roots)
            {
                if (roots == null || roots.Count == 0)
                {
                    return string.Empty;
                }

                const int previewLimit = 4;
                List<string> names = new List<string>(previewLimit);
                int count = Mathf.Min(previewLimit, roots.Count);
                for (int i = 0; i < count; i++)
                {
                    GameObject root = roots[i];
                    if (root != null)
                    {
                        names.Add(root.name);
                    }
                }

                string joined = string.Join(", ", names.ToArray());
                if (roots.Count > count)
                {
                    joined += " ...";
                }

                return joined;
            }
        }
    }
}
#endif
