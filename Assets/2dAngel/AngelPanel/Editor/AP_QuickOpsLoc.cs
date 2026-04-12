#if UNITY_EDITOR
namespace AngelPanel.Editor
{
    public static class AP_QuickOpsLoc
    {
        public static AP_LocProvider Build()
        {
            AP_LocProvider p = new AP_LocProvider("ap.quickops", 10);

            p.Add("AP_QO_TITLE", "QuickOps", "QuickOps", "QuickOps", "QuickOps");
            p.Add("AP_QO_CARD_SUMMARY", "Selection Summary", "选中摘要", "選取摘要", "Selection Summary");
            p.Add("AP_QO_CARD_SUMMARY_HINT", "", "", "", "");
            p.Add("AP_QO_SAFE_EXECUTION_HINT", "", "", "", "");
            p.Add("AP_QO_SELECTION_READY", "Selection Ready", "选中已就绪", "選取已就緒", "Selection Ready");
            p.Add("AP_QO_SELECTION_EMPTY", "No Selection", "未选中物体", "未選取物件", "No Selection");
            p.Add("AP_QO_SCOPE_INCLUDE_CHILDREN", "Including Children", "包含子物体", "包含子物件", "Including Children");
            p.Add("AP_QO_SCOPE_ROOT_ONLY", "Roots Only", "仅根节点", "僅根節點", "Roots Only");
            p.Add("AP_QO_TARGET_OBJECTS", "Target Objects", "目标物体", "目標物件", "Target Objects");
            p.Add("AP_QO_CARD_SCOPE", "Scope", "范围", "範圍", "Scope");
            p.Add("AP_QO_CARD_SCOPE_HINT", "", "", "", "");
            p.Add("AP_QO_INCLUDE_CHILDREN", "Include Children", "包含子物体", "包含子物件", "Include Children");
            p.Add("AP_QO_SELECTED_ROOTS", "Selected Roots", "选中根节点", "選取根節點", "Selected Roots");
            p.Add("AP_QO_RENDERER_COMPONENTS", "Renderer Components", "Renderer 组件", "Renderer 元件", "Renderer Components");
            p.Add("AP_QO_COLLIDER_COMPONENTS", "Collider Components", "Collider 组件", "Collider 元件", "Collider Components");
            p.Add("AP_QO_MONOBEHAVIOUR_COMPONENTS", "MonoBehaviour Components", "MonoBehaviour 组件", "MonoBehaviour 元件", "MonoBehaviour Components");
            p.Add("AP_QO_SELECTION_REQUIRED_HINT", "", "", "", "");
            p.Add("AP_QO_SECTION_COLLIDER", "Collider", "Collider", "Collider", "Collider");
            p.Add("AP_QO_SECTION_COLLIDER_HINT", "", "", "", "");
            p.Add("AP_QO_SECTION_SCRIPT_CLEANUP", "Script Cleanup", "脚本清理", "腳本清理", "Script Cleanup");
            p.Add("AP_QO_SECTION_SCRIPT_CLEANUP_HINT", "", "", "", "");
            p.Add("AP_QO_SECTION_LOD", "LOD", "LOD", "LOD", "LOD");
            p.Add("AP_QO_SECTION_LOD_HINT", "", "", "", "");
            p.Add("AP_QO_SECTION_PENDING", "This section has no registered drawer yet.", "该分区暂时没有注册内容。", "該分區暫時沒有註冊內容。", "This section has no registered drawer yet.");
            p.Add("AP_QO_LAST_RESULT", "Last Result", "上次结果", "上次結果", "Last Result");
            p.Add("AP_QO_DISMISS_STATUS", "Dismiss", "关闭提示", "關閉提示", "Dismiss");
            p.Add("AP_QO_CONFIRM_EXECUTE", "Run", "执行", "執行", "Run");
            p.Add("AP_QO_CANCEL", "Cancel", "取消", "取消", "Cancel");
            p.Add("AP_QO_CONFIRM_HANDLER_MISSING", "The confirmation handler is missing.", "确认处理器不存在。", "確認處理器不存在。", "The confirmation handler is missing.");

            p.Add("AP_QO_COLLIDER_TARGETS_WITH_COLLIDERS", "Objects With Colliders", "已有 Collider 的物体", "已有 Collider 的物件", "Objects With Colliders");
            p.Add("AP_QO_COLLIDER_ELIGIBLE_MESH_TARGETS", "Mesh Collider Targets", "可加 MeshCollider 的目标", "可加 MeshCollider 的目標", "Mesh Collider Targets");
            p.Add("AP_QO_COLLIDER_ELIGIBLE_BOX_TARGETS", "Box Collider Targets", "可加 BoxCollider 的目标", "可加 BoxCollider 的目標", "Box Collider Targets");
            p.Add("AP_QO_COLLIDER_SECTION_NOTE", "", "", "", "");
            p.Add("AP_QO_COLLIDER_BUTTON_COUNT", "Count Colliders", "统计 Collider", "統計 Collider", "Count Colliders");
            p.Add("AP_QO_COLLIDER_BUTTON_REMOVE_ALL", "Remove All Colliders", "移除全部 Collider", "移除全部 Collider", "Remove All Colliders");
            p.Add("AP_QO_COLLIDER_BUTTON_ADD_BOX", "Add BoxCollider", "添加 BoxCollider", "加入 BoxCollider", "Add BoxCollider");
            p.Add("AP_QO_COLLIDER_BUTTON_ADD_MESH", "Add MeshCollider", "添加 MeshCollider", "加入 MeshCollider", "Add MeshCollider");
            p.Add("AP_QO_COLLIDER_BUTTON_ADD_MESH_CONVEX", "Add Convex MeshCollider", "添加 Convex MeshCollider", "加入 Convex MeshCollider", "Add Convex MeshCollider");
            p.Add("AP_QO_COLLIDER_RESULT_COUNT", "Found {0:N0} collider components across {1:N0} objects in {2:N0} targets.", "在 {2:N0} 个目标中，共找到 {0:N0} 个 Collider 组件，分布于 {1:N0} 个物体。", "在 {2:N0} 個目標中，共找到 {0:N0} 個 Collider 元件，分布於 {1:N0} 個物件。", "Found {0:N0} collider components across {1:N0} objects in {2:N0} targets.");
            p.Add("AP_QO_COLLIDER_RESULT_REMOVE_ALL", "Removed {0:N0} collider components from {1:N0} targets.", "已从 {1:N0} 个目标中移除 {0:N0} 个 Collider 组件。", "已從 {1:N0} 個目標中移除 {0:N0} 個 Collider 元件。", "Removed {0:N0} collider components from {1:N0} targets.");
            p.Add("AP_QO_COLLIDER_RESULT_ADD_BOX", "Added {0:N0} BoxColliders to {1:N0} targets.", "已向 {1:N0} 个目标添加 {0:N0} 个 BoxCollider。", "已向 {1:N0} 個目標加入 {0:N0} 個 BoxCollider。", "Added {0:N0} BoxColliders to {1:N0} targets.");
            p.Add("AP_QO_COLLIDER_RESULT_ADD_MESH", "Added {0:N0} MeshColliders to {1:N0} targets.", "已向 {1:N0} 个目标添加 {0:N0} 个 MeshCollider。", "已向 {1:N0} 個目標加入 {0:N0} 個 MeshCollider。", "Added {0:N0} MeshColliders to {1:N0} targets.");
            p.Add("AP_QO_COLLIDER_RESULT_ADD_MESH_CONVEX", "Added {0:N0} convex MeshColliders to {1:N0} targets.", "已向 {1:N0} 个目标添加 {0:N0} 个凸 MeshCollider。", "已向 {1:N0} 個目標加入 {0:N0} 個凸 MeshCollider。", "Added {0:N0} convex MeshColliders to {1:N0} targets.");

            p.Add("AP_QO_SCRIPT_MISSING_SLOTS", "Missing Script Slots", "丢失脚本槽位", "遺失腳本槽位", "Missing Script Slots");
            p.Add("AP_QO_SCRIPT_LIVE_MONOBEHAVIOURS", "Live MonoBehaviours", "现有 MonoBehaviours", "現有 MonoBehaviours", "Live MonoBehaviours");
            p.Add("AP_QO_SCRIPT_WARNING_NOTE", "", "", "", "");
            p.Add("AP_QO_SCRIPT_BUTTON_REMOVE_MISSING", "Remove Missing Scripts", "移除 Missing Scripts", "移除 Missing Scripts", "Remove Missing Scripts");
            p.Add("AP_QO_SCRIPT_BUTTON_REMOVE_MONOBEHAVIOURS", "Remove MonoBehaviours", "移除 MonoBehaviours", "移除 MonoBehaviours", "Remove MonoBehaviours");
            p.Add("AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS", "Remove Missing Scripts", "移除 Missing Scripts", "移除 Missing Scripts", "Remove Missing Scripts");
            p.Add("AP_QO_CONFIRM_REMOVE_MISSING_SCRIPTS_BODY", "This removes broken component slots from the selected scope.", "这会移除当前选中范围内的失效组件槽位。", "這會移除目前選取範圍內的失效元件槽位。", "This removes broken component slots from the selected scope.");
            p.Add("AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS", "Remove MonoBehaviours", "移除 MonoBehaviours", "移除 MonoBehaviours", "Remove MonoBehaviours");
            p.Add("AP_QO_CONFIRM_REMOVE_MONOBEHAVIOUR_SCRIPTS_BODY", "This removes live MonoBehaviour components from the selected scope.", "这会移除当前选中范围内的 MonoBehaviour 组件。", "這會移除目前選取範圍內的 MonoBehaviour 元件。", "This removes live MonoBehaviour components from the selected scope.");
            p.Add("AP_QO_SCRIPT_RESULT_REMOVE_MISSING", "Removed {0:N0} missing script slots from {1:N0} targets.", "已从 {1:N0} 个目标中移除 {0:N0} 个丢失脚本槽位。", "已從 {1:N0} 個目標中移除 {0:N0} 個遺失腳本槽位。", "Removed {0:N0} missing script slots from {1:N0} targets.");
            p.Add("AP_QO_SCRIPT_RESULT_REMOVE_MONOBEHAVIOURS", "Removed {0:N0} MonoBehaviour components from {1:N0} targets.", "已从 {1:N0} 个目标中移除 {0:N0} 个 MonoBehaviour 组件。", "已從 {1:N0} 個目標中移除 {0:N0} 個 MonoBehaviour 元件。", "Removed {0:N0} MonoBehaviour components from {1:N0} targets.");

            p.Add("AP_QO_LOD_GROUPS_IN_SCOPE", "LODGroups In Scope", "范围内 LODGroup 数量", "範圍內 LODGroup 數量", "LODGroups In Scope");
            p.Add("AP_QO_LOD_SECTION_NOTE", "", "", "", "");
            p.Add("AP_QO_LOD_BUTTON_REMOVE_KEEP_HIGHEST", "Remove LODGroup Keep Highest", "移除 LODGroup 并保留最高级", "移除 LODGroup 並保留最高層級", "Remove LODGroup Keep Highest");
            p.Add("AP_QO_LOD_RESULT_REMOVE_KEEP_HIGHEST", "Removed {0:N0} LODGroups. Kept {1:N0} renderers and disabled {2:N0} lower LOD renderers.", "已移除 {0:N0} 个 LODGroup，保留 {1:N0} 个渲染器，并禁用 {2:N0} 个低级 LOD 渲染器。", "已移除 {0:N0} 個 LODGroup，保留 {1:N0} 個渲染器，並停用 {2:N0} 個低層 LOD 渲染器。", "Removed {0:N0} LODGroups. Kept {1:N0} renderers and disabled {2:N0} lower LOD renderers.");

            return p;
        }
    }
}
#endif
