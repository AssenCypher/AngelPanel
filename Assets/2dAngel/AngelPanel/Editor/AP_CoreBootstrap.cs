#if UNITY_EDITOR
using UnityEditor;

namespace AngelPanel.Editor
{
    [InitializeOnLoad]
    public static class AP_CoreBootstrap
    {
        private static bool bootstrapped;

        static AP_CoreBootstrap()
        {
            EnsureBootstrapped();
        }

        public static void EnsureBootstrapped()
        {
            if (bootstrapped)
            {
                return;
            }

            bootstrapped = true;

            AP_Loc.Init();
            AP_CoreStorage.EnsureDirectory(AP_CorePaths.ConfigRoot);
            AP_CoreStorage.EnsureDirectory(AP_CorePaths.LocalizationRoot);

            if (!AP_Loc.HasProvider("ap.core"))
            {
                AP_Loc.RegisterProvider(AP_CoreLoc.Build());
            }

            if (!AP_Loc.HasProvider("ap.quickops"))
            {
                AP_Loc.RegisterProvider(AP_QuickOpsLoc.Build());
            }

            if (!AP_Loc.HasProvider("ap.tools"))
            {
                AP_Loc.RegisterProvider(AP_ToolsLoc.Build());
            }

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Home,
                displayNameLocKey = "AP_MP_TAB_HOME",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 0,
                isCore = true,
                hostSection = AP_HostSection.Core,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = DrawHomePage,
                capabilities = new[]
                {
                    AP_CapabilityIds.Host,
                    AP_CapabilityIds.Workspace
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Debug,
                displayNameLocKey = "AP_Opt_TAB_DEBUG",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 100,
                isCore = true,
                hostSection = AP_HostSection.Optimizing,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_DebugPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Optimizing,
                    AP_CapabilityIds.Debug
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Shader,
                displayNameLocKey = "AP_Opt_TAB_SHADER",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 120,
                isCore = true,
                hostSection = AP_HostSection.Optimizing,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_ShaderPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Optimizing,
                    AP_CapabilityIds.Shader
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Occlusion,
                displayNameLocKey = "AP_Opt_TAB_OCCLUSION",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 140,
                isCore = true,
                hostSection = AP_HostSection.Optimizing,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_OcclusionPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Optimizing,
                    AP_CapabilityIds.Occlusion
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.OcclusionRooms,
                displayNameLocKey = "AP_Opt_TAB_OCCLUSION_ROOMS",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 160,
                isCore = true,
                hostSection = AP_HostSection.Optimizing,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_OcclusionRoomsPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Optimizing,
                    AP_CapabilityIds.OcclusionRooms
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Tools,
                displayNameLocKey = "AP_TL_TAB",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 300,
                isCore = true,
                hostSection = AP_HostSection.Tools,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                isAvailable = AP_ModuleRegistry.HasExternalToolModules,
                drawHostPage = AP_ToolsPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Tools
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.CoreProduct,
                displayNameLocKey = "AP_MP_WINDOW_TITLE",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 1,
                isCore = true,
                hostSection = AP_HostSection.Info,
                showInHost = false,
                showInAbout = true,
                showInInstalledList = true,
                capabilities = new[]
                {
                    AP_CapabilityIds.Host,
                    AP_CapabilityIds.Workspace,
                    AP_CapabilityIds.Optimizing,
                    AP_CapabilityIds.Tools
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.QuickOps,
                displayNameLocKey = "AP_QO_TITLE",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 10,
                isCore = true,
                hostSection = AP_HostSection.Info,
                showInHost = false,
                showInAbout = true,
                showInInstalledList = true,
                capabilities = new[]
                {
                    AP_CapabilityIds.QuickOps
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.Config,
                displayNameLocKey = "AP_Cfg_TAB",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 9998,
                isCore = true,
                hostSection = AP_HostSection.Info,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_ConfigPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Host
                }
            });

            AP_ModuleRegistry.Register(new AP_ModuleManifest
            {
                moduleId = AP_ModuleIds.About,
                displayNameLocKey = "AP_MP_TAB_ABOUT",
                descriptionLocKey = string.Empty,
                version = AP_CoreInfo.Version,
                author = AP_CoreInfo.AuthorName,
                sortOrder = 9999,
                isCore = true,
                hostSection = AP_HostSection.Info,
                showInHost = true,
                showInAbout = false,
                showInInstalledList = false,
                drawHostPage = AP_AboutPage.Draw,
                capabilities = new[]
                {
                    AP_CapabilityIds.Host
                }
            });
        }

        private static void DrawHomePage(AP_HostContext context)
        {
            if (context == null || context.Workspace == null)
            {
                return;
            }

            context.Workspace.Draw(context.WindowWidth, AP_MainWorkspaceWindow.Open);
        }
    }
}
#endif
