#if UNITY_EDITOR
using System;

namespace AngelPanel.Editor
{
    public enum AP_HostSection
    {
        Core = 0,
        Optimizing = 1,
        Tools = 2,
        Info = 3
    }

    public sealed class AP_ModuleManifest
    {
        public string moduleId;
        public string displayNameLocKey;
        public string descriptionLocKey;
        public string version;
        public string author;
        public string productUrl;
        public int sortOrder;
        public bool isCore;
        public bool isExternalModule;
        public AP_HostSection hostSection = AP_HostSection.Tools;
        public bool showInHost = true;
        public bool showInAbout = true;
        public bool showInInstalledList = true;
        public Func<bool> isAvailable;
        public Action<AP_HostContext> drawHostPage;
        public Action openStandaloneWindow;
        public string[] capabilities;
    }

    public sealed class AP_HostContext
    {
        public AP_Main HostWindow { get; internal set; }
        public AP_MainWorkspace Workspace { get; internal set; }
        public AP_CoreConfigData Config { get; internal set; }
        public float WindowWidth { get; internal set; }
        public float ContentWidth { get; internal set; }
    }
}
#endif
