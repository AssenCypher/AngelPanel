#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace AngelPanel.Editor
{
    public static class AP_OptimizingUtils
    {
        public static Bounds? CollectBounds(Transform[] roots, bool includeRenderers, bool includeColliders)
        {
            if (roots == null || roots.Length == 0)
            {
                return null;
            }

            bool hasBounds = false;
            Bounds bounds = default;
            HashSet<Object> visited = new HashSet<Object>();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform root = roots[i];
                if (root == null)
                {
                    continue;
                }

                if (includeRenderers)
                {
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        Renderer renderer = renderers[r];
                        if (renderer == null || !visited.Add(renderer))
                        {
                            continue;
                        }

                        if (!hasBounds)
                        {
                            bounds = renderer.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                    }

                    Terrain[] terrains = root.GetComponentsInChildren<Terrain>(true);
                    for (int t = 0; t < terrains.Length; t++)
                    {
                        Terrain terrain = terrains[t];
                        if (terrain == null || terrain.terrainData == null || !visited.Add(terrain))
                        {
                            continue;
                        }

                        Bounds terrainBounds = new Bounds(
                            terrain.transform.position + terrain.terrainData.size * 0.5f,
                            terrain.terrainData.size);

                        if (!hasBounds)
                        {
                            bounds = terrainBounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(terrainBounds);
                        }
                    }
                }

                if (includeColliders)
                {
                    Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < colliders.Length; c++)
                    {
                        Collider collider = colliders[c];
                        if (collider == null || collider.isTrigger || !visited.Add(collider))
                        {
                            continue;
                        }

                        if (!hasBounds)
                        {
                            bounds = collider.bounds;
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(collider.bounds);
                        }
                    }
                }
            }

            return hasBounds ? bounds : null;
        }
    }
}
#endif
