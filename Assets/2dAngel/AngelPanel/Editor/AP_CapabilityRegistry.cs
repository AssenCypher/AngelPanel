#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AngelPanel.Editor
{
    public static class AP_CapabilityRegistry
    {
        private static readonly Dictionary<string, HashSet<string>> ModuleCapabilities = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private static readonly Dictionary<string, HashSet<string>> CapabilityProviders = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        public static void SyncModule(string moduleId, string[] capabilities)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                return;
            }

            RemoveModule(moduleId);

            if (capabilities == null || capabilities.Length == 0)
            {
                return;
            }

            string normalizedModuleId = moduleId.Trim();
            HashSet<string> moduleSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < capabilities.Length; i++)
            {
                string capability = Normalize(capabilities[i]);
                if (string.IsNullOrWhiteSpace(capability) || !moduleSet.Add(capability))
                {
                    continue;
                }

                if (!CapabilityProviders.TryGetValue(capability, out HashSet<string> providers))
                {
                    providers = new HashSet<string>(StringComparer.Ordinal);
                    CapabilityProviders.Add(capability, providers);
                }

                providers.Add(normalizedModuleId);
            }

            if (moduleSet.Count > 0)
            {
                ModuleCapabilities[normalizedModuleId] = moduleSet;
            }
        }

        public static void RemoveModule(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                return;
            }

            string normalizedModuleId = moduleId.Trim();
            if (!ModuleCapabilities.TryGetValue(normalizedModuleId, out HashSet<string> oldCapabilities))
            {
                return;
            }

            foreach (string capability in oldCapabilities)
            {
                if (!CapabilityProviders.TryGetValue(capability, out HashSet<string> providers))
                {
                    continue;
                }

                providers.Remove(normalizedModuleId);
                if (providers.Count == 0)
                {
                    CapabilityProviders.Remove(capability);
                }
            }

            ModuleCapabilities.Remove(normalizedModuleId);
        }

        public static bool Has(string capability)
        {
            string normalized = Normalize(capability);
            return !string.IsNullOrWhiteSpace(normalized)
                && CapabilityProviders.TryGetValue(normalized, out HashSet<string> providers)
                && providers.Count > 0;
        }

        public static IReadOnlyList<string> GetProviders(string capability)
        {
            string normalized = Normalize(capability);
            if (string.IsNullOrWhiteSpace(normalized) || !CapabilityProviders.TryGetValue(normalized, out HashSet<string> providers) || providers.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> list = new List<string>(providers);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public static IReadOnlyDictionary<string, HashSet<string>> GetSnapshot()
        {
            return CapabilityProviders;
        }

        private static string Normalize(string capability)
        {
            return string.IsNullOrWhiteSpace(capability) ? string.Empty : capability.Trim().ToLowerInvariant();
        }
    }
}
#endif
