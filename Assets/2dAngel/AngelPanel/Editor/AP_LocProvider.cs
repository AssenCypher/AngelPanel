#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AngelPanel.Editor
{
    public sealed class AP_LocProvider
    {
        public string providerId;
        public int priority;
        public Dictionary<string, AP_LocEntry> entries = new Dictionary<string, AP_LocEntry>(StringComparer.Ordinal);

        public AP_LocProvider(string providerId, int priority)
        {
            this.providerId = providerId ?? string.Empty;
            this.priority = priority;
        }

        public AP_LocProvider Add(string key, string en, string zhCN = null, string zhTW = null, string jaJP = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return this;
            }

            AP_LocEntry entry = new AP_LocEntry();
            entry.Set(AP_Loc.LangEnglishId, en);
            entry.Set(AP_Loc.LangChineseSimplifiedId, zhCN);
            entry.Set(AP_Loc.LangChineseTraditionalId, zhTW);
            entry.Set(AP_Loc.LangJapaneseId, jaJP);
            entries[key.Trim()] = entry;
            return this;
        }
    }

    public sealed class AP_LocEntry
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Set(string langId, string text)
        {
            if (string.IsNullOrWhiteSpace(langId) || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            values[langId.Trim()] = text;
        }

        public bool TryGet(string langId, out string text)
        {
            return values.TryGetValue(langId ?? string.Empty, out text);
        }

        public bool TryGetAny(out string text)
        {
            foreach (KeyValuePair<string, string> pair in values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    text = pair.Value;
                    return true;
                }
            }

            text = string.Empty;
            return false;
        }
    }
}
#endif
