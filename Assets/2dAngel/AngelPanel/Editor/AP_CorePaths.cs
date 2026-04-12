#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace AngelPanel.Editor
{
    public static class AP_CorePaths
    {
        public const string AssetRoot = "Assets/2dAngel/AngelPanel";
        public const string CompanyFolder = "2dAngel";
        public const string ProductFolder = "AngelPanel";
        public const string MainConfigFileName = "ap.core.main.json";
        public const string PolyCountConfigFileName = "ap.core.polycount.json";
        public const string OverlayFileName = "ap.loc.overlay.json";

        public static string AppDataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyFolder, ProductFolder);
        public static string ConfigRoot => Path.Combine(AppDataRoot, "Config");
        public static string LocalizationRoot => Path.Combine(AppDataRoot, "Localization");
        public static string MainConfigFilePath => Path.Combine(ConfigRoot, MainConfigFileName);
        public static string PolyCountConfigFilePath => Path.Combine(ConfigRoot, PolyCountConfigFileName);
        public static string OverlayFilePath => Path.Combine(LocalizationRoot, OverlayFileName);
        public static bool AssetRootExists => AssetDatabase.IsValidFolder(AssetRoot);
    }
}
#endif
