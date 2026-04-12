using System;

namespace AngelPanel.Editor
{
    public static class AP_ModuleBridge
    {
        public static bool TryRegisterModule(AP_ModuleManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.moduleId))
            {
                return false;
            }

            AP_ModuleRegistry.Register(manifest);
            return true;
        }

        public static bool TryRemoveModule(string moduleId)
        {
            return AP_ModuleRegistry.Remove(moduleId);
        }

        public static bool TryRegisterLocProvider(AP_LocProvider provider)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.providerId))
            {
                return false;
            }

            AP_Loc.RegisterProvider(provider);
            return true;
        }

        public static bool HasCapability(string capabilityId)
        {
            return AP_CapabilityRegistry.Has(capabilityId);
        }

        public static bool TryGetModule(string moduleId, out AP_ModuleManifest manifest)
        {
            return AP_ModuleRegistry.TryGet(moduleId, out manifest);
        }

        public static AP_ModuleManifest CreateExternalToolManifest(
            string moduleId,
            string displayNameLocKey,
            string descriptionLocKey,
            int sortOrder,
            string version,
            Action openStandaloneWindow,
            string[] capabilities,
            Action<AP_HostContext> drawHostPage = null,
            string author = AP_CoreInfo.AuthorName,
            string productUrl = "")
        {
            Action<AP_HostContext> draw = drawHostPage;
            if (draw == null)
            {
                draw = context => AP_ExternalModuleCards.DrawDefaultToolPage(context, moduleId);
            }

            return new AP_ModuleManifest
            {
                moduleId = moduleId ?? string.Empty,
                displayNameLocKey = displayNameLocKey ?? string.Empty,
                descriptionLocKey = descriptionLocKey ?? string.Empty,
                version = string.IsNullOrWhiteSpace(version) ? AP_CoreInfo.Version : version,
                author = string.IsNullOrWhiteSpace(author) ? AP_CoreInfo.AuthorName : author,
                productUrl = productUrl ?? string.Empty,
                sortOrder = sortOrder,
                isCore = false,
                isExternalModule = true,
                hostSection = AP_HostSection.Tools,
                showInHost = true,
                showInAbout = true,
                showInInstalledList = true,
                drawHostPage = draw,
                openStandaloneWindow = openStandaloneWindow,
                capabilities = capabilities ?? Array.Empty<string>()
            };
        }

        public static bool TryRegisterExternalTool(
            string moduleId,
            string displayNameLocKey,
            string descriptionLocKey,
            int sortOrder,
            string version,
            Action openStandaloneWindow,
            string[] capabilities,
            Action<AP_HostContext> drawHostPage = null,
            string author = AP_CoreInfo.AuthorName,
            string productUrl = "")
        {
            return TryRegisterModule(CreateExternalToolManifest(
                moduleId,
                displayNameLocKey,
                descriptionLocKey,
                sortOrder,
                version,
                openStandaloneWindow,
                capabilities,
                drawHostPage,
                author,
                productUrl));
        }
    }
}
