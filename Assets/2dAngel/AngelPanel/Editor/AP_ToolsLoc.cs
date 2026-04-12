#if UNITY_EDITOR
namespace AngelPanel.Editor
{
    public static class AP_ToolsLoc
    {
        public static AP_LocProvider Build()
        {
            AP_LocProvider p = new AP_LocProvider("ap.tools", 5);

            p.Add("AP_TL_TAB", "Tools", "工具区", "工具區", "Tools");
            p.Add("AP_TL_HINT", "", "", "", "");
            p.Add("AP_TL_TITLE", "Tools", "工具区", "工具區", "Tools");
            p.Add("AP_TL_INSTALLED", "Installed Tools", "已接入工具", "已接入工具", "Installed Tools");
            p.Add("AP_TL_RECOMMENDED", "Recommended", "推荐工具", "推薦工具", "Recommended");
            p.Add("AP_TL_EMPTY", "No tools detected.", "未检测到可用工具。", "未偵測到可用工具。", "No tools detected.");
            p.Add("AP_TL_RECOMMENDED_NONE", "No additional tools to recommend.", "当前没有额外推荐工具。", "目前沒有額外推薦工具。", "No additional tools to recommend.");
            p.Add("AP_TL_OPEN_IN_AP", "Open in AP", "在 AP 中打开", "在 AP 中開啟", "Open in AP");
            p.Add("AP_TL_OPEN_TOOL", "Open Tool", "打开工具", "開啟工具", "Open Tool");
            p.Add("AP_TL_OPEN_PAGE", "Product Page", "商品页", "商品頁", "Product Page");
            p.Add("AP_TL_CAPABILITIES", "Capabilities", "能力", "能力", "Capabilities");
            p.Add("AP_TL_TARGET", "Line", "归位", "歸位", "Line");
            p.Add("AP_TL_STATUS", "Status", "状态", "狀態", "Status");
            p.Add("AP_TL_STATUS_NOT_INSTALLED", "Not installed", "未安装", "未安裝", "Not installed");
            p.Add("AP_TL_VIEW_DETAILS", "View Details", "查看详情", "查看詳情", "View Details");
            p.Add("AP_TL_INSTALLED_COUNT", "Installed", "已接入", "已接入", "Installed");
            p.Add("AP_TL_SUGGESTED_COUNT", "Recommended", "推荐", "推薦", "Recommended");
            p.Add("AP_TL_BADGE_TOOL", "Tool", "工具", "工具", "Tool");
            p.Add("AP_TL_CAP_NONE", "None", "无", "無", "None");

            return p;
        }
    }
}
#endif
