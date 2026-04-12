#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AngelPanel.Editor
{
    public static class AP_ModuleRegistry
    {
        private static readonly Dictionary<string, AP_ModuleManifest> Modules = new Dictionary<string, AP_ModuleManifest>(StringComparer.Ordinal);
        private static readonly List<AP_ModuleManifest> SortedCache = new List<AP_ModuleManifest>(32);
        private static bool dirty = true;

        public static void Register(AP_ModuleManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.moduleId))
            {
                return;
            }

            manifest.moduleId = manifest.moduleId.Trim();
            Modules[manifest.moduleId] = manifest;
            AP_CapabilityRegistry.SyncModule(manifest.moduleId, manifest.capabilities);
            dirty = true;
        }

        public static bool Remove(string moduleId)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                return false;
            }

            string normalized = moduleId.Trim();
            if (!Modules.Remove(normalized))
            {
                return false;
            }

            AP_CapabilityRegistry.RemoveModule(normalized);
            dirty = true;
            return true;
        }

        public static bool TryGet(string moduleId, out AP_ModuleManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(moduleId))
            {
                manifest = null;
                return false;
            }

            return Modules.TryGetValue(moduleId.Trim(), out manifest);
        }

        public static IReadOnlyList<AP_ModuleManifest> GetAllModules()
        {
            RebuildCacheIfNeeded();
            return SortedCache;
        }

        public static IReadOnlyList<AP_ModuleManifest> GetVisibleHostModules()
        {
            return GetVisibleHostModules(null, false);
        }

        public static IReadOnlyList<AP_ModuleManifest> GetVisibleHostModules(AP_HostSection? hostSection, bool externalOnly)
        {
            RebuildCacheIfNeeded();
            List<AP_ModuleManifest> visible = new List<AP_ModuleManifest>(SortedCache.Count);
            for (int i = 0; i < SortedCache.Count; i++)
            {
                AP_ModuleManifest manifest = SortedCache[i];
                if (!IsVisibleInHost(manifest))
                {
                    continue;
                }

                if (hostSection.HasValue && manifest.hostSection != hostSection.Value)
                {
                    continue;
                }

                if (externalOnly && !IsExternalTool(manifest))
                {
                    continue;
                }

                visible.Add(manifest);
            }

            return visible;
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledModules()
        {
            return GetInstalledModules((AP_HostSection?)null, false);
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledModules(bool externalOnly)
        {
            return GetInstalledModules((AP_HostSection?)null, externalOnly);
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledModules(AP_HostSection hostSection)
        {
            return GetInstalledModules(hostSection, false);
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledModules(AP_HostSection? hostSection, bool externalOnly)
        {
            RebuildCacheIfNeeded();
            List<AP_ModuleManifest> installed = new List<AP_ModuleManifest>(SortedCache.Count);
            for (int i = 0; i < SortedCache.Count; i++)
            {
                AP_ModuleManifest manifest = SortedCache[i];
                if (!IsInstalled(manifest))
                {
                    continue;
                }

                if (hostSection.HasValue && manifest.hostSection != hostSection.Value)
                {
                    continue;
                }

                if (externalOnly && !IsExternalTool(manifest))
                {
                    continue;
                }

                installed.Add(manifest);
            }

            return installed;
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledCoreProducts()
        {
            RebuildCacheIfNeeded();
            List<AP_ModuleManifest> installed = new List<AP_ModuleManifest>(SortedCache.Count);
            for (int i = 0; i < SortedCache.Count; i++)
            {
                AP_ModuleManifest manifest = SortedCache[i];
                if (!IsInstalled(manifest) || !manifest.isCore)
                {
                    continue;
                }

                installed.Add(manifest);
            }

            return installed;
        }

        public static List<AP_ModuleManifest> GetInstalledToolModules()
        {
            IReadOnlyList<AP_ModuleManifest> modules = GetInstalledExternalTools();
            List<AP_ModuleManifest> tools = new List<AP_ModuleManifest>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                tools.Add(modules[i]);
            }

            return tools;
        }

        public static IReadOnlyList<AP_ModuleManifest> GetInstalledExternalTools()
        {
            return GetInstalledModules(AP_HostSection.Tools, true);
        }

        public static bool HasExternalToolModules()
        {
            RebuildCacheIfNeeded();
            for (int i = 0; i < SortedCache.Count; i++)
            {
                AP_ModuleManifest manifest = SortedCache[i];
                if (IsExternalTool(manifest) && IsInstalled(manifest))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountExternalToolModules()
        {
            RebuildCacheIfNeeded();
            int count = 0;
            for (int i = 0; i < SortedCache.Count; i++)
            {
                AP_ModuleManifest manifest = SortedCache[i];
                if (IsExternalTool(manifest) && IsInstalled(manifest))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool IsExternalTool(AP_ModuleManifest manifest)
        {
            return manifest != null
                && manifest.hostSection == AP_HostSection.Tools
                && !manifest.isCore
                && !string.Equals(manifest.moduleId, AP_ModuleIds.Tools, StringComparison.Ordinal);
        }

        private static bool IsVisibleInHost(AP_ModuleManifest manifest)
        {
            if (manifest == null || !manifest.showInHost)
            {
                return false;
            }

            return IsAvailable(manifest);
        }

        private static bool IsInstalled(AP_ModuleManifest manifest)
        {
            if (manifest == null || !manifest.showInAbout || !manifest.showInInstalledList)
            {
                return false;
            }

            return IsAvailable(manifest);
        }

        private static bool IsAvailable(AP_ModuleManifest manifest)
        {
            return manifest != null && (manifest.isAvailable == null || manifest.isAvailable());
        }

        private static void RebuildCacheIfNeeded()
        {
            if (!dirty)
            {
                return;
            }

            dirty = false;
            SortedCache.Clear();
            foreach (KeyValuePair<string, AP_ModuleManifest> pair in Modules)
            {
                SortedCache.Add(pair.Value);
            }

            SortedCache.Sort(CompareManifest);
        }

        private static int CompareManifest(AP_ModuleManifest a, AP_ModuleManifest b)
        {
            if (ReferenceEquals(a, b))
            {
                return 0;
            }

            if (a == null)
            {
                return 1;
            }

            if (b == null)
            {
                return -1;
            }

            int sectionCompare = a.hostSection.CompareTo(b.hostSection);
            if (sectionCompare != 0)
            {
                return sectionCompare;
            }

            int sortOrder = a.sortOrder.CompareTo(b.sortOrder);
            if (sortOrder != 0)
            {
                return sortOrder;
            }

            string aKey = string.IsNullOrWhiteSpace(a.displayNameLocKey) ? a.moduleId : a.displayNameLocKey;
            string bKey = string.IsNullOrWhiteSpace(b.displayNameLocKey) ? b.moduleId : b.displayNameLocKey;
            return string.CompareOrdinal(aKey, bKey);
        }
    }
}
#endif
