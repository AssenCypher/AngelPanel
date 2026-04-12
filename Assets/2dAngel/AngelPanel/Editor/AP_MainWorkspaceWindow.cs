#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    public sealed class AP_MainWorkspaceWindow : EditorWindow
    {
        private const string MenuPath = "2dAngel/AngelPanel/PolyCount Workspace";

        [SerializeField] private Vector2 scrollPosition;
        [SerializeField] private AP_MainWorkspace workspace = new AP_MainWorkspace();

        public static bool HasOpenWindow()
        {
            return Resources.FindObjectsOfTypeAll<AP_MainWorkspaceWindow>().Length > 0;
        }

        [MenuItem(MenuPath, false, 11)]
        public static void Open()
        {
            AP_MainWorkspaceWindow window = GetWindow<AP_MainWorkspaceWindow>();
            window.minSize = new Vector2(260f, 180f);
            window.InitializeWindow();
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            InitializeWindow();
            workspace.RefreshSelection();

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
        }

        private void InitializeWindow()
        {
            AP_Loc.Init();
            workspace = workspace ?? new AP_MainWorkspace();
            workspace.Initialize();
            titleContent = new GUIContent(AP_Loc.T("AP_MP_POLYCOUNT_WINDOW_TITLE"));
        }

        private void HandleSelectionChanged()
        {
            workspace = workspace ?? new AP_MainWorkspace();
            workspace.RefreshSelection();
            Repaint();
        }

        private void HandleHierarchyChanged()
        {
            workspace = workspace ?? new AP_MainWorkspace();
            workspace.MarkSceneCountsDirty();
            Repaint();
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            workspace = workspace ?? new AP_MainWorkspace();
            workspace.MarkSceneCountsDirty();
            workspace.RefreshSelection();
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (workspace != null && workspace.TickAutoRefresh())
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            InitializeWindow();

            if (position.width < 360f)
            {
                workspace.DrawDetachedPolyCount(position.width, AP_Main.Open);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            workspace.DrawDetachedPolyCount(position.width, AP_Main.Open);
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
