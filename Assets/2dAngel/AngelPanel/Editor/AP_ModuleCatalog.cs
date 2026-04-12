#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace AngelPanel.Editor
{
    public static class AP_ModuleCatalog
    {
        private static readonly AP_ModuleCatalogEntry[] Entries =
        {
            new AP_ModuleCatalogEntry("ap.tools.probetools", "AP_AB_PROD_PROBETOOLS_NAME", "AP_AB_PROD_PROBETOOLS_DESC", "AP_AB_BADGE_FREE", "AP_AB_LINE_FREE_MODULES", "AP_AB_TARGET_TOOLS", string.Empty, 0, 10),
            new AP_ModuleCatalogEntry("ap.apk.free", "AP_AB_PROD_APK_FREE_NAME", "AP_AB_PROD_APK_FREE_DESC", "AP_AB_BADGE_FREE", "AP_AB_LINE_FREE_MODULES", "AP_AB_TARGET_TOOLS", string.Empty, 0, 20),

            new AP_ModuleCatalogEntry("ap.lightingtools", "AP_AB_PROD_LIGHTINGTOOLS_NAME", "AP_AB_PROD_LIGHTINGTOOLS_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_LIGHTING", string.Empty, 1, 10),
            new AP_ModuleCatalogEntry("ap.isozone", "AP_AB_PROD_ISOZONE_NAME", "AP_AB_PROD_ISOZONE_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_RUNTIME", string.Empty, 1, 20),
            new AP_ModuleCatalogEntry("ap.areatools", "AP_AB_PROD_AREATOOLS_NAME", "AP_AB_PROD_AREATOOLS_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_RUNTIME", string.Empty, 1, 30),
            new AP_ModuleCatalogEntry("ap.locksystem", "AP_AB_PROD_LOCKSYSTEM_NAME", "AP_AB_PROD_LOCKSYSTEM_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_RUNTIME", string.Empty, 1, 40),
            new AP_ModuleCatalogEntry("ap.apk.pro", "AP_AB_PROD_APK_PRO_NAME", "AP_AB_PROD_APK_PRO_DESC", "AP_AB_BADGE_PRO", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_TOOLS", string.Empty, 1, 50),
            new AP_ModuleCatalogEntry("ap.terrainoptimizer", "AP_AB_PROD_TERRAINOPT_NAME", "AP_AB_PROD_TERRAINOPT_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_OPTIMIZING", string.Empty, 1, 60),
            new AP_ModuleCatalogEntry("ap.pointcloud", "AP_AB_PROD_POINTCLOUD_NAME", "AP_AB_PROD_POINTCLOUD_DESC", "AP_AB_BADGE_PAID", "AP_AB_LINE_PAID_PRODUCTS", "AP_AB_TARGET_OPTIMIZING", string.Empty, 1, 70),

            new AP_ModuleCatalogEntry("ap.hierarchytools", "AP_AB_PROD_HIERARCHY_NAME", "AP_AB_PROD_HIERARCHY_DESC", "AP_AB_BADGE_STANDALONE", "AP_AB_LINE_STANDALONE_PRODUCTS", "AP_AB_TARGET_STANDALONE", string.Empty, 2, 10)
        };

        public static IReadOnlyList<AP_ModuleCatalogEntry> GetEntries()
        {
            return Entries;
        }

        public static bool TryGetEntry(string moduleId, out AP_ModuleCatalogEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(moduleId))
            {
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (string.Equals(Entries[i].moduleId, moduleId, StringComparison.Ordinal))
                    {
                        entry = Entries[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        public static List<AP_ModuleCatalogEntry> GetMissing(IReadOnlyList<AP_ModuleManifest> installedModules)
        {
            HashSet<string> installedIds = new HashSet<string>(StringComparer.Ordinal);
            if (installedModules != null)
            {
                for (int i = 0; i < installedModules.Count; i++)
                {
                    AP_ModuleManifest manifest = installedModules[i];
                    if (manifest != null && !string.IsNullOrWhiteSpace(manifest.moduleId))
                    {
                        installedIds.Add(manifest.moduleId);
                    }
                }
            }

            List<AP_ModuleCatalogEntry> missing = new List<AP_ModuleCatalogEntry>();
            for (int i = 0; i < Entries.Length; i++)
            {
                if (!installedIds.Contains(Entries[i].moduleId))
                {
                    missing.Add(Entries[i]);
                }
            }

            missing.Sort(CompareEntries);
            return missing;
        }

        public static List<AP_ModuleCatalogEntry> GetMissingByTarget(IReadOnlyList<AP_ModuleManifest> installedModules, string targetLocKey)
        {
            List<AP_ModuleCatalogEntry> missing = GetMissing(installedModules);
            if (string.IsNullOrWhiteSpace(targetLocKey))
            {
                return missing;
            }

            List<AP_ModuleCatalogEntry> filtered = new List<AP_ModuleCatalogEntry>();
            for (int i = 0; i < missing.Count; i++)
            {
                if (string.Equals(missing[i].targetLocKey, targetLocKey, StringComparison.Ordinal))
                {
                    filtered.Add(missing[i]);
                }
            }

            return filtered;
        }

        public static int CountByLine(string lineLocKey)
        {
            int count = 0;
            for (int i = 0; i < Entries.Length; i++)
            {
                if (string.Equals(Entries[i].lineLocKey, lineLocKey, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CompareEntries(AP_ModuleCatalogEntry a, AP_ModuleCatalogEntry b)
        {
            int lineCompare = a.lineOrder.CompareTo(b.lineOrder);
            if (lineCompare != 0)
            {
                return lineCompare;
            }

            return a.sortOrder.CompareTo(b.sortOrder);
        }
    }

    public readonly struct AP_ModuleCatalogEntry
    {
        public readonly string moduleId;
        public readonly string displayNameLocKey;
        public readonly string subtitleLocKey;
        public readonly string badgeLocKey;
        public readonly string lineLocKey;
        public readonly string targetLocKey;
        public readonly string storeUrl;
        public readonly int lineOrder;
        public readonly int sortOrder;

        public AP_ModuleCatalogEntry(
            string moduleId,
            string displayNameLocKey,
            string subtitleLocKey,
            string badgeLocKey,
            string lineLocKey,
            string targetLocKey,
            string storeUrl,
            int lineOrder,
            int sortOrder)
        {
            this.moduleId = moduleId ?? string.Empty;
            this.displayNameLocKey = displayNameLocKey ?? string.Empty;
            this.subtitleLocKey = subtitleLocKey ?? string.Empty;
            this.badgeLocKey = badgeLocKey ?? string.Empty;
            this.lineLocKey = lineLocKey ?? string.Empty;
            this.targetLocKey = targetLocKey ?? string.Empty;
            this.storeUrl = storeUrl ?? string.Empty;
            this.lineOrder = lineOrder;
            this.sortOrder = sortOrder;
        }
    }
}
#endif
