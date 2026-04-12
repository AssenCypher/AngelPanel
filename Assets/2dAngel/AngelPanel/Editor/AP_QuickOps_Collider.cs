#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public sealed partial class AP_QuickOps
    {
        [InitializeOnLoadMethod]
        private static void RegisterColliderQuickOps()
        {
            RegisterColliderSection(DrawColliderSection);
        }

        private static void DrawColliderSection(Context context)
        {
            int objectsWithColliders = CountObjectsWithColliders(context);
            int meshEligible = CountMeshColliderEligibleTargets(context);
            int boxEligible = CountBoxColliderEligibleTargets(context);

            if (context.IsSimpleCompact)
            {
                GUILayout.Label(
                    context.T("AP_QO_COLLIDER_TARGETS_WITH_COLLIDERS") + ": " + objectsWithColliders.ToString("N0")
                    + "   |   " + context.T("AP_QO_COLLIDER_ELIGIBLE_MESH_TARGETS") + ": " + meshEligible.ToString("N0")
                    + "   |   " + context.T("AP_QO_COLLIDER_ELIGIBLE_BOX_TARGETS") + ": " + boxEligible.ToString("N0"),
                    EditorStyles.miniLabel);
                GUILayout.Space(3f);

                EditorGUI.BeginDisabledGroup(!context.HasSelection);
                using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(3f).Pad(0f))
                {
                    if (row.MiniButton(context.T("AP_QO_COLLIDER_BUTTON_COUNT")))
                    {
                        ExecuteCountColliders(context);
                    }
                    if (row.MiniButton(context.T("AP_QO_COLLIDER_BUTTON_REMOVE_ALL")))
                    {
                        ExecuteRemoveAllColliders(context);
                    }
                    if (row.MiniButton(context.T("AP_QO_COLLIDER_BUTTON_ADD_BOX")))
                    {
                        ExecuteAddBoxColliders(context);
                    }
                    if (row.MiniButton(context.T("AP_QO_COLLIDER_BUTTON_ADD_MESH")))
                    {
                        ExecuteAddMeshColliders(context, false);
                    }
                    if (row.MiniButton(context.T("AP_QO_COLLIDER_BUTTON_ADD_MESH_CONVEX")))
                    {
                        ExecuteAddMeshColliders(context, true);
                    }
                }
                EditorGUI.EndDisabledGroup();
                return;
            }

            AP_EUI.DrawKeyValue(context.T("AP_QO_COLLIDER_TARGETS_WITH_COLLIDERS"), objectsWithColliders.ToString("N0"));
            AP_EUI.DrawKeyValue(context.T("AP_QO_COLLIDER_ELIGIBLE_MESH_TARGETS"), meshEligible.ToString("N0"));
            AP_EUI.DrawKeyValue(context.T("AP_QO_COLLIDER_ELIGIBLE_BOX_TARGETS"), boxEligible.ToString("N0"));
            GUILayout.Space(6f);

            EditorGUI.BeginDisabledGroup(!context.HasSelection);
            using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(6f).Pad(0f))
            {
                if (row.Button(context.T("AP_QO_COLLIDER_BUTTON_COUNT")))
                {
                    ExecuteCountColliders(context);
                }
                if (row.Button(context.T("AP_QO_COLLIDER_BUTTON_REMOVE_ALL")))
                {
                    ExecuteRemoveAllColliders(context);
                }
            }

            GUILayout.Space(4f);
            using (AP_EUI.ResponsiveRow row = AP_EUI.Row(context.ContentWidth).Gap(6f).Pad(0f))
            {
                if (row.Button(context.T("AP_QO_COLLIDER_BUTTON_ADD_BOX")))
                {
                    ExecuteAddBoxColliders(context);
                }
                if (row.Button(context.T("AP_QO_COLLIDER_BUTTON_ADD_MESH")))
                {
                    ExecuteAddMeshColliders(context, false);
                }
                if (row.Button(context.T("AP_QO_COLLIDER_BUTTON_ADD_MESH_CONVEX")))
                {
                    ExecuteAddMeshColliders(context, true);
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private static int CountObjectsWithColliders(Context context)
        {
            int count = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target != null && target.GetComponent<Collider>() != null)
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountMeshColliderEligibleTargets(Context context)
        {
            int count = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (CanAddMeshCollider(targets[i]))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountBoxColliderEligibleTargets(Context context)
        {
            int count = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (CanAddBoxCollider(targets[i]))
                {
                    count++;
                }
            }
            return count;
        }

        private static void ExecuteCountColliders(Context context)
        {
            int objectsWithColliders = CountObjectsWithColliders(context);
            context.PushStatusFormat("AP_QO_COLLIDER_RESULT_COUNT", MessageType.Info, context.ColliderCount, objectsWithColliders, context.TargetCount);
        }

        private static void ExecuteRemoveAllColliders(Context context)
        {
            const string undoName = "AngelPanel QuickOps Remove Colliders";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int removed = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null)
                {
                    continue;
                }

                Collider[] colliders = target.GetComponents<Collider>();
                for (int j = 0; j < colliders.Length; j++)
                {
                    if (colliders[j] == null)
                    {
                        continue;
                    }

                    Undo.DestroyObjectImmediate(colliders[j]);
                    removed++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat("AP_QO_COLLIDER_RESULT_REMOVE_ALL", removed > 0 ? MessageType.Warning : MessageType.Info, removed, context.TargetCount);
        }

        private static void ExecuteAddMeshColliders(Context context, bool convex)
        {
            string undoName = convex ? "AngelPanel QuickOps Add Convex MeshCollider" : "AngelPanel QuickOps Add MeshCollider";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int added = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (!CanAddMeshCollider(target))
                {
                    continue;
                }

                Mesh sharedMesh = GetColliderSourceMesh(target);
                if (sharedMesh == null)
                {
                    continue;
                }

                MeshCollider collider = Undo.AddComponent<MeshCollider>(target.gameObject);
                collider.sharedMesh = sharedMesh;
                collider.convex = convex;
                added++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat(convex ? "AP_QO_COLLIDER_RESULT_ADD_MESH_CONVEX" : "AP_QO_COLLIDER_RESULT_ADD_MESH", added > 0 ? MessageType.Info : MessageType.Warning, added, context.TargetCount);
        }

        private static void ExecuteAddBoxColliders(Context context)
        {
            const string undoName = "AngelPanel QuickOps Add BoxCollider";
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);

            int added = 0;
            IReadOnlyList<Transform> targets = context.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (!CanAddBoxCollider(target))
                {
                    continue;
                }

                Bounds localBounds;
                if (!TryBuildLocalBoxBounds(target, out localBounds))
                {
                    continue;
                }

                BoxCollider collider = Undo.AddComponent<BoxCollider>(target.gameObject);
                collider.center = localBounds.center;
                collider.size = localBounds.size;
                added++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            context.PushStatusFormat("AP_QO_COLLIDER_RESULT_ADD_BOX", added > 0 ? MessageType.Info : MessageType.Warning, added, context.TargetCount);
        }

        private static bool CanAddMeshCollider(Transform target)
        {
            if (target == null || target.GetComponent<Collider>() != null)
            {
                return false;
            }

            return GetColliderSourceMesh(target) != null;
        }

        private static bool CanAddBoxCollider(Transform target)
        {
            if (target == null || target.GetComponent<Collider>() != null)
            {
                return false;
            }

            return target.GetComponent<Renderer>() != null || target.GetComponent<MeshFilter>() != null;
        }

        private static Mesh GetColliderSourceMesh(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                return meshFilter.sharedMesh;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = target.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            return null;
        }

        private static bool TryBuildLocalBoxBounds(Transform target, out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.one);
            if (target == null)
            {
                return false;
            }

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                bounds = meshFilter.sharedMesh.bounds;
                return true;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return false;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 localCenter = target.InverseTransformPoint(worldBounds.center);
            Vector3 lossy = target.lossyScale;
            bounds = new Bounds(localCenter, new Vector3(
                SafeDivide(worldBounds.size.x, Mathf.Abs(lossy.x)),
                SafeDivide(worldBounds.size.y, Mathf.Abs(lossy.y)),
                SafeDivide(worldBounds.size.z, Mathf.Abs(lossy.z))));
            return true;
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor <= 0.0001f ? value : value / divisor;
        }
    }
}
#endif
