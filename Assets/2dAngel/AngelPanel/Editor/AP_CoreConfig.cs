#if UNITY_EDITOR
using System;

namespace AngelPanel.Editor
{
    public enum AP_HostNavigationPlacement
    {
        Left = 0,
        Right = 1,
        Top = 2,
        Bottom = 3
    }

    public enum AP_HostChromeScaleMode
    {
        Adaptive = 0,
        Fixed = 1
    }

    public enum AP_HostNavigationOverflowMode
    {
        Compact = 0,
        Scroll = 1
    }

    [Serializable]
    public sealed class AP_CoreConfigData
    {
        public string lastPageId = AP_ModuleIds.Home;
        public bool hasSeenWelcome;
        public bool showInstalledFoldout = true;
        public bool showMissingFoldout = true;
        public bool showCapabilityFoldout = true;
        public bool showPathFoldout;

        public AP_HostNavigationPlacement navigationPlacement = AP_HostNavigationPlacement.Left;
        public AP_HostChromeScaleMode chromeScaleMode = AP_HostChromeScaleMode.Adaptive;
        public AP_HostNavigationOverflowMode navigationOverflowMode = AP_HostNavigationOverflowMode.Compact;
        public float navigationPanelWidth = 216f;
        public int navigationButtonHeight = 26;
        public int navigationFontSize = 11;

        public void Sanitize()
        {
            navigationPanelWidth = UnityEngine.Mathf.Clamp(navigationPanelWidth, 132f, 320f);
            navigationButtonHeight = UnityEngine.Mathf.Clamp(navigationButtonHeight, 20, 42);
            navigationFontSize = UnityEngine.Mathf.Clamp(navigationFontSize, 10, 18);

            if ((int)navigationPlacement < 0 || (int)navigationPlacement > 3)
            {
                navigationPlacement = AP_HostNavigationPlacement.Left;
            }

            if ((int)chromeScaleMode < 0 || (int)chromeScaleMode > 1)
            {
                chromeScaleMode = AP_HostChromeScaleMode.Adaptive;
            }

            if ((int)navigationOverflowMode < 0 || (int)navigationOverflowMode > 1)
            {
                navigationOverflowMode = AP_HostNavigationOverflowMode.Compact;
            }
        }
    }
}
#endif
