#if UNITY_EDITOR
namespace AngelPanel.Editor
{
    public static class AP_ProbeToolsBridgeContract
    {
        public const string ModuleId = "ap.tools.probetools.free";
        public const string LocProviderId = "ap.tools.probetools.loc";
        public const string DisplayNameLocKey = "AP_PT_NAME";
        public const string SummaryLocKey = "AP_PT_SUMMARY";
        public const string DefaultVersion = AP_CoreInfo.Version;
        public const string DefaultAuthor = AP_CoreInfo.AuthorName;
        public const string DefaultProductUrl = "";
        public const int DefaultSortOrder = 420;
        public const AP_HostSection DefaultHostSection = AP_HostSection.Tools;
    }
}
#endif
