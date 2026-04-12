#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public sealed partial class AP_QuickOps
    {
        [InitializeOnLoadMethod]
        private static void RegisterLodQuickOps()
        {
            RegisterLodSection(DrawLodSection);
        }

        private static void DrawLodSection(Context context)
        {
            int lodGroupCount = CountLodGroups(context);

            if (context.IsSimpleCompact)
            {
                GUILayout.Label(context.T("AP_QO_LOD_GROUPS_IN_SCOPE") + ": " + lodGroupCount.ToString("N0"), EditorStyles.miniLabel);
                GUILayout.Space(3f);

                EditorGUI.BeginDisabledGroup(!context.HasSelection);
                using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(3f).Pad(0f))
                {
                    if (row.MiniButton(context.T("AP_QO_LOD_BUTTON_REMOVE_KEEP_HIGHEST")))
                    {
                        ExecuteRemoveLodGroupsKeepHighest(context);
                    }
                }
                EditorGUI.EndDisabledGroup();
                return;
            }

            AP_EUI.DrawKeyValue(context.T("AP_QO_LOD_GROUPS_IN_SCOPE"), lodGroupCount.ToString("N0"));
            GUILayout.Space(6f);

            EditorGUI.BeginDisabledGroup(!context.HasSelection);
            using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(6f).Pad(0f))
            {
                if (row.Button(context.T("AP_QO_LOD_BUTTON_REMOVE_KEEP_HIGHEST")))
                {
                    ExecuteRemoveLodGroupsKeepHighest(context);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private static int CountLodGroups(Context context)
        {
            int count = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    count += targets[i].GetComponents<LODGroup>().Length;
                }
            }
            return count;
        }

        private static void ExecuteRemoveLodGroupsKeepHighest(Context context)
        {
            const string undoName = "AngelPanel QuickOps Remove LODGroups Keep Highest";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int removedGroups = 0;
            int keptRenderers = 0;
            int disabledRenderers = 0;

            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                LODGroup[] groups = targets[i].GetComponents<LODGroup>();
                for (int j = 0; j < groups.Length; j++)
                {
                    LODGroup group = groups[j];
                    if (group == null)
                    {
                        continue;
                    }

                    LOD[] lods = group.GetLODs();
                    HashSet<Renderer> keepSet = new HashSet<Renderer>();
                    HashSet<Renderer> disableSet = new HashSet<Renderer>();

                    if (lods != null && lods.Length > 0)
                    {
                        Renderer[] lod0 = lods[0].renderers;
                        for (int k = 0; k < lod0.Length; k++)
                        {
                            if (lod0[k] != null)
                            {
                                keepSet.Add(lod0[k]);
                            }
                        }

                        for (int lodIndex = 1; lodIndex < lods.Length; lodIndex++)
                        {
                            Renderer[] renderers = lods[lodIndex].renderers;
                            for (int r = 0; r < renderers.Length; r++)
                            {
                                Renderer renderer = renderers[r];
                                if (renderer != null && !keepSet.Contains(renderer))
                                {
                                    disableSet.Add(renderer);
                                }
                            }
                        }
                    }

                    foreach (Renderer renderer in keepSet)
                    {
                        Undo.RecordObject(renderer, undoName);
                        if (!renderer.enabled)
                        {
                            renderer.enabled = true;
                        }
                        keptRenderers++;
                    }

                    foreach (Renderer renderer in disableSet)
                    {
                        Undo.RecordObject(renderer, undoName);
                        if (renderer.enabled)
                        {
                            renderer.enabled = false;
                        }
                        disabledRenderers++;
                    }

                    Undo.DestroyObjectImmediate(group);
                    removedGroups++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat("AP_QO_LOD_RESULT_REMOVE_KEEP_HIGHEST", removedGroups > 0 ? MessageType.Info : MessageType.Warning, removedGroups, keptRenderers, disabledRenderers);
        }
    }
}
#endif
