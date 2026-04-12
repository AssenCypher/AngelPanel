#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AngelPanel.Editor
{
    public static class AP_OcclusionRoomsPage
    {
        private struct FloorCell
        {
            public int X;
            public int Z;
            public float FloorY;
            public float CeilingY;
        }

        private static bool selectionOnly;
        private static float voxel = 2.0f;
        private static float eyeHeight = 1.6f;
        private static float minHeadroom = 2.0f;
        private static float maxCeilingSearch = 6.0f;
        private static float floorNormalMin = 0.6f;
        private static float floorMergeTolerance = 0.40f;
        private static float ceilingMergeTolerance = 0.80f;
        private static int minCellsPerVolume = 2;
        private static bool requireCeiling = true;
        private static float paddingXZ = 0.25f;
        private static float paddingY = 0.15f;

        public static void Draw(AP_HostContext context)
        {
            int fontSize = context?.HostWindow != null ? context.HostWindow.GetHostContentFontSize() : 11;
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.Clamp(fontSize + 1, 11, 20), wordWrap = true };
            GUIStyle note = new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontSize = Mathf.Clamp(fontSize - 1, 9, 16), wordWrap = true };
            float buttonHeight = Mathf.Clamp(22f + (fontSize - 11f) * 2f, 22f, 34f);

            GUILayout.Space(6f);
            GUILayout.Label(AP_Loc.T("AP_Opt_TAB_OCCLUSION_ROOMS"), title);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                GUILayout.Label(AP_Loc.T("AP_OCCR_PAGE_TITLE"), title);
                GUILayout.Space(2f);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                selectionOnly = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_OCC_SELECTION_ONLY"), selectionOnly);
                voxel = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_VOXEL"), voxel, 0.5f, 5f);

                GUILayout.Space(2f);
                GUILayout.Label(AP_Loc.T("AP_OCCR_CAMERA_SECTION"), title);
                eyeHeight = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_EYE_HEIGHT"), eyeHeight, 1.0f, 2.2f);
                minHeadroom = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_MIN_HEADROOM"), minHeadroom, 1.6f, 4.0f);
                maxCeilingSearch = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_MAX_CEILING"), maxCeilingSearch, 2.0f, 12.0f);
                floorNormalMin = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_MIN_FLOOR_NORMAL"), floorNormalMin, 0.3f, 1.0f);
                requireCeiling = EditorGUILayout.ToggleLeft(AP_Loc.T("AP_OCCR_REQUIRE_CEILING"), requireCeiling);

                GUILayout.Space(2f);
                GUILayout.Label(AP_Loc.T("AP_OCCR_MERGE_SECTION"), title);
                floorMergeTolerance = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_FLOOR_TOLERANCE"), floorMergeTolerance, 0.05f, 1.0f);
                ceilingMergeTolerance = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_CEILING_TOLERANCE"), ceilingMergeTolerance, 0.10f, 2.0f);
                minCellsPerVolume = EditorGUILayout.IntSlider(AP_Loc.T("AP_OCCR_MIN_CELLS"), minCellsPerVolume, 1, 16);
                paddingXZ = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_PADDING_XZ"), paddingXZ, 0.0f, 1.0f);
                paddingY = EditorGUILayout.Slider(AP_Loc.T("AP_OCCR_PADDING_Y"), paddingY, 0.0f, 0.75f);

                GUILayout.Space(4f);
                if (GUILayout.Button(AP_Loc.T("AP_OCCR_GENERATE"), GUILayout.Height(buttonHeight)))
                {
                    GenerateRooms();
                }
            }
        }

        private static void GenerateRooms()
        {
            List<Transform> roots = new List<Transform>();
            if (selectionOnly && Selection.transforms != null && Selection.transforms.Length > 0)
            {
                roots.AddRange(Selection.transforms);
            }
            else
            {
                foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    roots.Add(go.transform);
                }
            }

            Bounds? boundsNullable = AP_OptimizingUtils.CollectBounds(roots.ToArray(), true, true);
            if (!boundsNullable.HasValue)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCCR_PAGE_TITLE"), AP_Loc.T("AP_OCC_NO_BOUNDS"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            Bounds bounds = boundsNullable.Value;
            float step = Mathf.Max(0.5f, voxel);
            float minX = bounds.min.x;
            float maxX = bounds.max.x;
            float minY = bounds.min.y;
            float maxY = bounds.max.y;
            float minZ = bounds.min.z;
            float maxZ = bounds.max.z;
            int xCount = Mathf.CeilToInt((maxX - minX) / step);
            int zCount = Mathf.CeilToInt((maxZ - minZ) / step);
            List<FloorCell> cells = new List<FloorCell>();

            bool cancel = false;
            for (int xi = 0; xi < xCount && !cancel; xi++)
            {
                float x = minX + (xi + 0.5f) * step;
                for (int zi = 0; zi < zCount && !cancel; zi++)
                {
                    float z = minZ + (zi + 0.5f) * step;
                    float progress = ((xi * zCount) + zi) / (float)Mathf.Max(1, xCount * zCount);
                    cancel = EditorUtility.DisplayCancelableProgressBar(AP_Loc.T("AP_OCCR_PROGRESS_TITLE"), string.Format(AP_Loc.T("AP_OCCR_PROGRESS_BODY"), xi + 1, xCount, zi + 1, zCount), progress);
                    GatherColumnCells(xi, zi, x, z, minY, maxY, step, cells);
                }
            }

            EditorUtility.ClearProgressBar();
            if (cancel)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCCR_PAGE_TITLE"), AP_Loc.T("AP_OCCR_CANCELLED"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            if (cells.Count == 0)
            {
                EditorUtility.DisplayDialog(AP_Loc.T("AP_OCCR_PAGE_TITLE"), AP_Loc.T("AP_OCCR_NONE"), AP_Loc.T("AP_DBG_OK"));
                return;
            }

            List<List<FloorCell>> clusters = BuildClusters(cells);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();

            GameObject parent = new GameObject("__OcclusionRooms");
            Undo.RegisterCreatedObjectUndo(parent, "Rooms Root");

            int created = 0;
            foreach (List<FloorCell> cluster in clusters)
            {
                if (cluster == null || cluster.Count < minCellsPerVolume)
                {
                    continue;
                }

                int minXi = cluster.Min(c => c.X);
                int maxXi = cluster.Max(c => c.X);
                int minZi = cluster.Min(c => c.Z);
                int maxZi = cluster.Max(c => c.Z);
                float avgFloor = cluster.Average(c => c.FloorY);
                float avgCeiling = cluster.Average(c => c.CeilingY);

                float sizeX = ((maxXi - minXi + 1) * step) + paddingXZ * 2f;
                float sizeZ = ((maxZi - minZi + 1) * step) + paddingXZ * 2f;
                float sizeY = Mathf.Max(minHeadroom, avgCeiling - avgFloor) + paddingY * 2f;

                Vector3 center = new Vector3(
                    minX + ((minXi + maxXi + 1) * 0.5f * step),
                    (avgFloor + avgCeiling) * 0.5f,
                    minZ + ((minZi + maxZi + 1) * 0.5f * step));

                GameObject room = new GameObject($"Room_{created:0000}");
                Undo.RegisterCreatedObjectUndo(room, "Create Room");
                room.transform.SetParent(parent.transform, false);
                room.transform.position = Vector3.zero;

                OcclusionArea area = room.AddComponent<OcclusionArea>();
                area.center = center;
                area.size = new Vector3(sizeX, sizeY, sizeZ);
                TryEnableViewVolume(area);
                created++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.DisplayDialog(AP_Loc.T("AP_OCCR_PAGE_TITLE"), string.Format(AP_Loc.T("AP_OCCR_DONE"), cells.Count, created), AP_Loc.T("AP_DBG_OK"));
        }

        private static void GatherColumnCells(int xi, int zi, float x, float z, float minY, float maxY, float step, List<FloorCell> cells)
        {
            Vector3 rayOrigin = new Vector3(x, maxY + 0.5f, z);
            float rayLength = (maxY - minY) + 1.0f;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, rayLength, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            hits = hits.OrderBy(h => h.distance).ToArray();
            List<float> acceptedFloors = new List<float>();

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.collider.isTrigger || hit.normal.y < floorNormalMin)
                {
                    continue;
                }

                float floorY = hit.point.y;
                if (acceptedFloors.Any(v => Mathf.Abs(v - floorY) <= 0.25f))
                {
                    continue;
                }

                Vector3 eyeCenter = new Vector3(x, floorY + eyeHeight, z);
                if (IsBlockedAt(eyeCenter, step))
                {
                    continue;
                }

                Vector3 ceilOrigin = new Vector3(x, floorY + 0.05f, z);
                bool hasCeiling = Physics.Raycast(ceilOrigin, Vector3.up, out RaycastHit ceilHit, maxCeilingSearch, ~0, QueryTriggerInteraction.Ignore);
                if (requireCeiling && !hasCeiling)
                {
                    continue;
                }

                float ceilingY = hasCeiling ? ceilHit.point.y : floorY + maxCeilingSearch;
                if (ceilingY - floorY < minHeadroom)
                {
                    continue;
                }

                acceptedFloors.Add(floorY);
                cells.Add(new FloorCell { X = xi, Z = zi, FloorY = floorY, CeilingY = ceilingY });
            }
        }

        private static bool IsBlockedAt(Vector3 eyeCenter, float step)
        {
            Vector3 halfExtents = new Vector3(step * 0.30f, 0.20f, step * 0.30f);
            Collider[] overlaps = Physics.OverlapBox(eyeCenter, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            return overlaps != null && overlaps.Length > 0;
        }

        private static List<List<FloorCell>> BuildClusters(List<FloorCell> cells)
        {
            Dictionary<Vector2Int, List<int>> map = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int key = new Vector2Int(cells[i].X, cells[i].Z);
                if (!map.TryGetValue(key, out List<int> list))
                {
                    list = new List<int>();
                    map[key] = list;
                }

                list.Add(i);
            }

            bool[] visited = new bool[cells.Count];
            List<List<FloorCell>> clusters = new List<List<FloorCell>>();
            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

            for (int i = 0; i < cells.Count; i++)
            {
                if (visited[i])
                {
                    continue;
                }

                Queue<int> queue = new Queue<int>();
                List<FloorCell> cluster = new List<FloorCell>();
                visited[i] = true;
                queue.Enqueue(i);

                while (queue.Count > 0)
                {
                    int currentIndex = queue.Dequeue();
                    FloorCell current = cells[currentIndex];
                    cluster.Add(current);

                    for (int d = 0; d < dirs.Length; d++)
                    {
                        Vector2Int key = new Vector2Int(current.X + dirs[d].x, current.Z + dirs[d].y);
                        if (!map.TryGetValue(key, out List<int> neighborIndices))
                        {
                            continue;
                        }

                        for (int n = 0; n < neighborIndices.Count; n++)
                        {
                            int neighbor = neighborIndices[n];
                            if (visited[neighbor] || !CanMerge(current, cells[neighbor]))
                            {
                                continue;
                            }

                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        private static bool CanMerge(FloorCell a, FloorCell b)
        {
            return Mathf.Abs(a.FloorY - b.FloorY) <= floorMergeTolerance && Mathf.Abs(a.CeilingY - b.CeilingY) <= ceilingMergeTolerance;
        }

        private static void TryEnableViewVolume(OcclusionArea area)
        {
            if (area == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(area);
            SerializedProperty property = serialized.FindProperty("m_IsViewVolume") ?? serialized.FindProperty("isViewVolume");
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
