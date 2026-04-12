#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace AngelPanel.Editor
{
    public static class AP_PackageDetector
    {
        public static bool HasVRCSDKWorlds { get; private set; }
        public static bool HasUdonSharp { get; private set; }
        public static bool HasBakery { get; private set; }
        public static bool HasMagicLightProbes { get; private set; }
        public static bool HasVRCLightVolumes { get; private set; }
        public static bool HasVRCLightVolumesManager { get; private set; }

        public static bool IsScanning { get; private set; }
        public static DateTime LastScanTime { get; private set; }

        private static bool initialized;

        public static void Ensure()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            QuickProbe();
            EditorApplication.delayCall += FullScanSafe;
        }

        public static void RefreshNow()
        {
            if (IsScanning)
            {
                return;
            }

            FullScanSafe();
        }

        private static void QuickProbe()
        {
            HasVRCSDKWorlds = Type.GetType("VRC.SDK3.Components.VRCSceneDescriptor, VRCSDK3") != null
                              || Type.GetType("VRC.SDK3.Components.VRCSceneDescriptor") != null;

            HasUdonSharp = Type.GetType("UdonSharp.UdonSharpBehaviour, UdonSharp") != null
                           || Type.GetType("UdonSharp.UdonSharpBehaviour") != null;

            HasMagicLightProbes = Type.GetType("MagicLightProbes.MagicLightProbes, MagicLightProbes") != null
                                  || Type.GetType("MagicLightProbes.MagicLightProbes") != null
                                  || Type.GetType("MagicLightProbes") != null;

            HasVRCLightVolumes = Type.GetType("REDSIM.LightVolume") != null
                                 || Type.GetType("LightVolume") != null;

            HasVRCLightVolumesManager = Type.GetType("REDSIM.LightVolumesManager") != null
                                        || Type.GetType("LightVolumesManager") != null;

            HasBakery = Type.GetType("ftLight") != null
                        || Type.GetType("ftRenderLightmap") != null
                        || Type.GetType("BakeryLightMesh") != null;
        }

        private static void FullScanSafe()
        {
            if (IsScanning)
            {
                return;
            }

            IsScanning = true;
            try
            {
                bool hasWorlds = false;
                bool hasUdonSharp = false;
                bool hasBakery = false;
                bool hasMLP = false;
                bool hasLV = false;
                bool hasLVManager = false;

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type[] types;
                    try
                    {
                        types = assemblies[i].GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types;
                    }

                    if (types == null)
                    {
                        continue;
                    }

                    for (int j = 0; j < types.Length; j++)
                    {
                        Type type = types[j];
                        if (type == null)
                        {
                            continue;
                        }

                        string fullName = type.FullName ?? type.Name ?? string.Empty;

                        if (!hasWorlds && fullName == "VRC.SDK3.Components.VRCSceneDescriptor")
                        {
                            hasWorlds = true;
                        }

                        if (!hasUdonSharp && fullName == "UdonSharp.UdonSharpBehaviour")
                        {
                            hasUdonSharp = true;
                        }

                        if (!hasBakery && (fullName == "ftLight" || fullName == "ftRenderLightmap" || fullName.EndsWith(".BakeryLightMesh", StringComparison.Ordinal)))
                        {
                            hasBakery = true;
                        }

                        if (!hasMLP && fullName.IndexOf("MagicLightProbes", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasMLP = true;
                        }

                        if (!hasLV && (fullName == "REDSIM.LightVolume" || fullName == "LightVolume" || fullName.EndsWith(".LightVolume", StringComparison.Ordinal)))
                        {
                            hasLV = true;
                        }

                        if (!hasLVManager && (fullName == "REDSIM.LightVolumesManager" || fullName == "LightVolumesManager" || fullName.EndsWith(".LightVolumesManager", StringComparison.Ordinal)))
                        {
                            hasLVManager = true;
                        }

                        if (hasWorlds && hasUdonSharp && hasBakery && hasMLP && hasLV && hasLVManager)
                        {
                            break;
                        }
                    }

                    if (hasWorlds && hasUdonSharp && hasBakery && hasMLP && hasLV && hasLVManager)
                    {
                        break;
                    }
                }

                HasVRCSDKWorlds = hasWorlds;
                HasUdonSharp = hasUdonSharp;
                HasBakery = hasBakery;
                HasMagicLightProbes = hasMLP;
                HasVRCLightVolumes = hasLV;
                HasVRCLightVolumesManager = hasLVManager;
                LastScanTime = DateTime.Now;
            }
            finally
            {
                IsScanning = false;
            }
        }
    }
}
#endif
