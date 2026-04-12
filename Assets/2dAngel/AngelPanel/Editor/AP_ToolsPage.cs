#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_ToolsPage
    {
        public static void Draw(AP_HostContext context)
        {
            if (context == null)
            {
                return;
            }

            IReadOnlyList<AP_ModuleManifest> installedTools = AP_ModuleRegistry.GetInstalledExternalTools();
            List<AP_ModuleCatalogEntry> suggestedTools = AP_ModuleCatalog.GetMissingByTarget(AP_ModuleRegistry.GetInstalledModules(), "AP_AB_TARGET_TOOLS");
            DrawSummary(installedTools.Count, suggestedTools.Count, context);
            GUILayout.Space(8f);

            GUILayout.Label(AP_Loc.T("AP_TL_INSTALLED"), GetSectionTitleStyle(context));
            GUILayout.Space(4f);
            AP_ExternalModuleCards.DrawInstalledToolGrid(installedTools, context);
            GUILayout.Space(8f);

            GUILayout.Label(AP_Loc.T("AP_TL_RECOMMENDED"), GetSectionTitleStyle(context));
            GUILayout.Space(4f);
            AP_ExternalModuleCards.DrawRecommendedToolGrid(suggestedTools, context);
        }

        public static void DrawExternalToolDefault(AP_HostContext context, string moduleId)
        {
            AP_ExternalModuleCards.DrawDefaultToolPage(context, moduleId);
        }

        private static void DrawSummary(int installedCount, int suggestedCount, AP_HostContext context)
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Clamp(context.HostWindow != null ? context.HostWindow.GetHostContentFontSize() + 2 : 14, 12, 20),
                wordWrap = true
            };

            GUIStyle numberStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Clamp(context.HostWindow != null ? context.HostWindow.GetHostContentFontSize() + 5 : 18, 18, 28),
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle miniCenter = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = Mathf.Clamp(context.HostWindow != null ? context.HostWindow.GetHostContentFontSize() - 1 : 10, 9, 14),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_TL_TITLE"), titleStyle);
                GUILayout.Space(4f);
                bool stack = context.ContentWidth < 480f;
                if (stack)
                {
                    DrawSummaryTile(AP_Loc.T("AP_TL_INSTALLED_COUNT"), installedCount.ToString(), numberStyle, miniCenter);
                    GUILayout.Space(6f);
                    DrawSummaryTile(AP_Loc.T("AP_TL_SUGGESTED_COUNT"), suggestedCount.ToString(), numberStyle, miniCenter);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawSummaryTile(AP_Loc.T("AP_TL_INSTALLED_COUNT"), installedCount.ToString(), numberStyle, miniCenter);
                        GUILayout.Space(6f);
                        DrawSummaryTile(AP_Loc.T("AP_TL_SUGGESTED_COUNT"), suggestedCount.ToString(), numberStyle, miniCenter);
                    }
                }
            }
        }

        private static void DrawSummaryTile(string label, string value, GUIStyle numberStyle, GUIStyle miniCenter)
        {
            using (new EditorGUILayout.VerticalScope("box", GUILayout.MinWidth(120f)))
            {
                GUILayout.Label(label, miniCenter);
                GUILayout.Space(2f);
                GUILayout.Label(value, numberStyle);
            }
        }

        private static GUIStyle GetSectionTitleStyle(AP_HostContext context)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = Mathf.Clamp(context.HostWindow != null ? context.HostWindow.GetHostContentFontSize() + 1 : 13, 11, 18)
            };
        }
    }
}
#endif
