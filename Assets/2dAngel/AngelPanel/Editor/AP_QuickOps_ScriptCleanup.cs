#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public sealed partial class AP_QuickOps
    {
        [InitializeOnLoadMethod]
        private static void RegisterScriptCleanupQuickOps()
        {
            RegisterScriptCleanupSection(DrawScriptCleanupSection);
            RegisterConfirmHandler(ConfirmAction.RemoveMissingScripts, ExecuteConfirmedRemoveMissingScripts);
            RegisterConfirmHandler(ConfirmAction.RemoveMonoBehaviourScripts, ExecuteConfirmedRemoveMonoBehaviourScripts);
        }

        private static void DrawScriptCleanupSection(Context context)
        {
            int missingScriptCount = CountMissingScripts(context);

            if (context.IsSimpleCompact)
            {
                GUILayout.Label(
                    context.T("AP_QO_SCRIPT_MISSING_SLOTS") + ": " + missingScriptCount.ToString("N0")
                    + "   |   " + context.T("AP_QO_SCRIPT_LIVE_MONOBEHAVIOURS") + ": " + context.MonoBehaviourCount.ToString("N0"),
                    EditorStyles.miniLabel);
                GUILayout.Space(3f);

                EditorGUI.BeginDisabledGroup(!context.HasSelection);
                using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(3f).Pad(0f))
                {
                    if (row.MiniButton(context.T("AP_QO_SCRIPT_BUTTON_REMOVE_MISSING")))
                    {
                        context.RequestConfirmation(ConfirmAction.RemoveMissingScripts, "AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS", "AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS_BODY", MessageType.Warning);
                    }
                    if (row.MiniButton(context.T("AP_QO_SCRIPT_BUTTON_REMOVE_MONOBEHAVIOURS")))
                    {
                        context.RequestConfirmation(ConfirmAction.RemoveMonoBehaviourScripts, "AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS", "AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS_BODY", MessageType.Error);
                    }
                }
                EditorGUI.EndDisabledGroup();
                return;
            }

            AP_EUI.DrawKeyValue(context.T("AP_QO_SCRIPT_MISSING_SLOTS"), missingScriptCount.ToString("N0"));
            AP_EUI.DrawKeyValue(context.T("AP_QO_SCRIPT_LIVE_MONOBEHAVIOURS"), context.MonoBehaviourCount.ToString("N0"));
            GUILayout.Space(6f);

            EditorGUI.BeginDisabledGroup(!context.HasSelection);
            using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(6f).Pad(0f))
            {
                if (row.Button(context.T("AP_QO_SCRIPT_BUTTON_REMOVE_MISSING")))
                {
                    context.RequestConfirmation(ConfirmAction.RemoveMissingScripts, "AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS", "AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS_BODY", MessageType.Warning);
                }
                if (row.Button(context.T("AP_QO_SCRIPT_BUTTON_REMOVE_MONOBEHAVIOURS")))
                {
                    context.RequestConfirmation(ConfirmAction.RemoveMonoBehaviourScripts, "AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS", "AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS_BODY", MessageType.Error);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private static int CountMissingScripts(Context context)
        {
            int missing = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(targets[i].gameObject);
                }
            }
            return missing;
        }

        private static void ExecuteConfirmedRemoveMissingScripts(Context context)
        {
            const string undoName = "AngelPanel QuickOps Remove Missing Scripts";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            IReadOnlyList<GameObject> roots = context.Roots;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] != null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(roots[i], undoName);
                }
            }

            int removed = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(targets[i].gameObject);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat("AP_QO_SCRIPT_RESULT_REMOVE_MISSING", removed > 0 ? MessageType.Warning : MessageType.Info, removed, context.TargetCount);
        }

        private static void ExecuteConfirmedRemoveMonoBehaviourScripts(Context context)
        {
            const string undoName = "AngelPanel QuickOps Remove MonoBehaviours";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int removed = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = targets[i].GetComponents<MonoBehaviour>();
                for (int j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] == null)
                    {
                        continue;
                    }

                    Undo.DestroyObjectImmediate(behaviours[j]);
                    removed++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat("AP_QO_SCRIPT_RESULT_REMOVE_MONOBEHAVIOURS", removed > 0 ? MessageType.Error : MessageType.Info, removed, context.TargetCount);
        }
    }
}
#endif
