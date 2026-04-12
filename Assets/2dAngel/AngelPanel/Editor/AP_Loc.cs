#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_Loc
    {
        public enum Language
        {
            English = 0,
            ChineseSimplified = 1,
            ChineseTraditional = 2,
            Japanese = 3
        }

        private const string PrefLangIndex = "AngelPanel_Lang";
        private const string PrefLangId = "AngelPanel_LangId";
        private const string LegacyPrefLangIndex = "DemonPanel_Lang";
        private const string OverlayProviderId = "ap.overlay";
        private const string DefaultLanguageNameEnglish = "English";
        private const string DefaultLanguageNameChineseSimplified = "简体中文";
        private const string DefaultLanguageNameChineseTraditional = "繁體中文";
        private const string DefaultLanguageNameJapanese = "日本語";

        public const string DefaultLanguageId = LangEnglishId;
        public const string LangEnglishId = "en";
        public const string LangChineseSimplifiedId = "zh-CN";
        public const string LangChineseTraditionalId = "zh-TW";
        public const string LangJapaneseId = "ja-JP";

        private static readonly string[] BuiltInLanguageIds =
        {
            LangEnglishId,
            LangChineseSimplifiedId,
            LangChineseTraditionalId,
            LangJapaneseId
        };

        private static readonly string[] BuiltInLanguageNames =
        {
            DefaultLanguageNameEnglish,
            DefaultLanguageNameChineseSimplified,
            DefaultLanguageNameChineseTraditional,
            DefaultLanguageNameJapanese
        };

        private static readonly Dictionary<string, AP_LocProvider> Providers = new Dictionary<string, AP_LocProvider>(StringComparer.Ordinal);
        private static readonly List<AP_LocProvider> OrderedProviders = new List<AP_LocProvider>(16);

        private static bool initialized;
        private static bool isReady;
        private static bool isOverlayLoaded;
        private static bool hasOverlayFile;
        private static string overlayError = string.Empty;
        private static int langIndex;
        private static string langId = DefaultLanguageId;

        public static bool IsReady => isReady;
        public static bool IsOverlayLoaded => isOverlayLoaded;
        public static bool HasOverlayFile => hasOverlayFile;
        public static string OverlayError => overlayError ?? string.Empty;
        public static string OverlayRootPath => AP_CorePaths.LocalizationRoot;
        public static string OverlayFilePath => AP_CorePaths.OverlayFilePath;
        public static int ProviderCount => OrderedProviders.Count;
        public static int LangIndex => langIndex;
        public static string LangId => langId;
        public static string[] LangNames => BuiltInLanguageNames;

        public static void Init()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            langIndex = LoadInitialLanguageIndex();
            langId = BuiltInLanguageIds[langIndex];
            isReady = true;
            ReloadOverlay();
        }

        public static void RegisterProvider(AP_LocProvider provider)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.providerId))
            {
                return;
            }

            Init();
            Providers[provider.providerId.Trim()] = provider;
            RebuildProviderOrder();
        }

        public static bool HasProvider(string providerId)
        {
            Init();
            return !string.IsNullOrWhiteSpace(providerId) && Providers.ContainsKey(providerId.Trim());
        }

        public static void UnregisterProvider(string providerId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
            {
                return;
            }

            Init();
            if (Providers.Remove(providerId.Trim()))
            {
                RebuildProviderOrder();
            }
        }

        public static void ReloadOverlay()
        {
            Init();
            overlayError = string.Empty;
            hasOverlayFile = File.Exists(OverlayFilePath);
            isOverlayLoaded = false;
            UnregisterProvider(OverlayProviderId);

            AP_CoreStorage.EnsureDirectory(OverlayRootPath);
            if (!hasOverlayFile)
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(OverlayFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                AP_OverlayFile overlayFile = JsonUtility.FromJson<AP_OverlayFile>(json);
                if (overlayFile == null || overlayFile.entries == null || overlayFile.entries.Length == 0)
                {
                    return;
                }

                AP_LocProvider provider = new AP_LocProvider(OverlayProviderId, 10000);
                for (int i = 0; i < overlayFile.entries.Length; i++)
                {
                    AP_OverlayEntry entry = overlayFile.entries[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.values == null)
                    {
                        continue;
                    }

                    AP_LocEntry locEntry = new AP_LocEntry();
                    for (int j = 0; j < entry.values.Length; j++)
                    {
                        AP_OverlayValue value = entry.values[j];
                        if (value == null)
                        {
                            continue;
                        }

                        locEntry.Set(NormalizeLanguageId(value.lang), value.text);
                    }

                    provider.entries[entry.key.Trim()] = locEntry;
                }

                if (provider.entries.Count > 0)
                {
                    Providers[provider.providerId] = provider;
                    RebuildProviderOrder();
                    isOverlayLoaded = true;
                }
            }
            catch (Exception exception)
            {
                overlayError = exception.Message;
            }
        }

        public static void SetLangIndex(int index)
        {
            Init();
            int clamped = Math.Max(0, Math.Min(BuiltInLanguageIds.Length - 1, index));
            if (clamped == langIndex)
            {
                return;
            }

            langIndex = clamped;
            langId = BuiltInLanguageIds[langIndex];
            EditorPrefs.SetInt(PrefLangIndex, langIndex);
            EditorPrefs.SetString(PrefLangId, langId);
        }

        public static string T(string key)
        {
            Init();
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string normalizedKey = key.Trim();
            for (int i = 0; i < OrderedProviders.Count; i++)
            {
                AP_LocProvider provider = OrderedProviders[i];
                if (provider == null || provider.entries == null || !provider.entries.TryGetValue(normalizedKey, out AP_LocEntry entry) || entry == null)
                {
                    continue;
                }

                if (entry.TryGet(langId, out string value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                if (!string.Equals(langId, DefaultLanguageId, StringComparison.OrdinalIgnoreCase)
                    && entry.TryGet(DefaultLanguageId, out value)
                    && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }

                if (entry.TryGetAny(out value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return normalizedKey;
        }

        private static void RebuildProviderOrder()
        {
            OrderedProviders.Clear();
            foreach (KeyValuePair<string, AP_LocProvider> pair in Providers)
            {
                OrderedProviders.Add(pair.Value);
            }

            OrderedProviders.Sort((a, b) => b.priority.CompareTo(a.priority));
        }

        private static int LoadInitialLanguageIndex()
        {
            if (EditorPrefs.HasKey(PrefLangIndex))
            {
                int storedIndex = EditorPrefs.GetInt(PrefLangIndex, 0);
                return Math.Max(0, Math.Min(BuiltInLanguageIds.Length - 1, storedIndex));
            }

            if (EditorPrefs.HasKey(PrefLangId))
            {
                string storedLangId = NormalizeLanguageId(EditorPrefs.GetString(PrefLangId, DefaultLanguageId));
                int index = Array.IndexOf(BuiltInLanguageIds, storedLangId);
                if (index >= 0)
                {
                    return index;
                }
            }

            if (EditorPrefs.HasKey(LegacyPrefLangIndex))
            {
                int legacyIndex = EditorPrefs.GetInt(LegacyPrefLangIndex, 0);
                return Math.Max(0, Math.Min(BuiltInLanguageIds.Length - 1, legacyIndex));
            }

            return 0;
        }

        private static string NormalizeLanguageId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DefaultLanguageId;
            }

            string trimmed = value.Trim();
            if (string.Equals(trimmed, "zh-cn", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "zh-hans", StringComparison.OrdinalIgnoreCase))
            {
                return LangChineseSimplifiedId;
            }

            if (string.Equals(trimmed, "zh-tw", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "zh-hant", StringComparison.OrdinalIgnoreCase))
            {
                return LangChineseTraditionalId;
            }

            if (string.Equals(trimmed, "ja", StringComparison.OrdinalIgnoreCase))
            {
                return LangJapaneseId;
            }

            if (string.Equals(trimmed, "en-us", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "en-gb", StringComparison.OrdinalIgnoreCase))
            {
                return LangEnglishId;
            }

            return trimmed;
        }

        [Serializable]
        private sealed class AP_OverlayFile
        {
            public AP_OverlayLanguage[] languages;
            public AP_OverlayEntry[] entries;
        }

        [Serializable]
        private sealed class AP_OverlayLanguage
        {
            public string id;
            public string name;
        }

        [Serializable]
        private sealed class AP_OverlayEntry
        {
            public string key;
            public AP_OverlayValue[] values;
        }

        [Serializable]
        private sealed class AP_OverlayValue
        {
            public string lang;
            public string text;
        }
    }
}
#endif
