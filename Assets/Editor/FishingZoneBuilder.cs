using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Chia nước trên map thành các vùng câu cá riêng để mỗi vùng có loại cá khác nhau:
// - Biển: vùng nước thông ra mép map.
// - Sông: vùng nước trong đất liền lớn nhất (sông + hồ nối với sông).
// - Ao hồ: các vùng nước nhỏ còn lại.
// Mỗi vùng là một tilemap trigger vô hình (layer Water) có FishingSpotZone; tilemap Water gốc giữ nguyên để chặn người chơi.
// FishingSpotData được tự gán theo tên điểm câu (spotName chứa "Biển" / "Sông" / "Ao"), có thể đổi tay trong Inspector.
// Chạy qua menu Tools > Map > Build Fishing Zones (chạy lại sau khi sửa / generate lại map).
public static class FishingZoneBuilder
{
    private const string ZoneTilePath = MainMapGenerator.GeneratedFolder + "/FishingZone.asset";

    private struct ZoneInfo
    {
        public string objectName;
        public string label;
        public string[] keywords;
    }

    // Từ khoá so với tên điểm câu đã bỏ dấu (xem Simplify).
    private static readonly ZoneInfo Sea = new ZoneInfo { objectName = "FishingZone_Sea", label = "Biển", keywords = new[] { "bien", "sea", "ocean" } };
    private static readonly ZoneInfo River = new ZoneInfo { objectName = "FishingZone_River", label = "Sông", keywords = new[] { "song", "river" } };
    private static readonly ZoneInfo Pond = new ZoneInfo { objectName = "FishingZone_Pond", label = "Ao hồ (gồm hồ lớn nối sông)", keywords = new[] { "ao ", "ao", "pond", "lake" } };

    [MenuItem("Tools/Map/Build Fishing Zones")]
    public static void Build()
    {
        Tilemap water = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "Water");
        if (water == null)
        {
            EditorUtility.DisplayDialog("Build Fishing Zones", "Không tìm thấy tilemap 'Water' trong scene đang mở.", "OK");
            return;
        }

        // 1. Tách các vùng nước liền nhau (4 hướng).
        BoundsInt bounds = water.cellBounds;
        TileBase[] tiles = water.GetTilesBlock(bounds);
        int width = bounds.size.x, height = bounds.size.y;
        int[] component = Enumerable.Repeat(-1, tiles.Length).ToArray();
        List<List<int>> components = new List<List<int>>();
        List<bool> touchesEdge = new List<bool>();

        for (int start = 0; start < tiles.Length; start++)
        {
            if (tiles[start] == null || component[start] >= 0) continue;

            int id = components.Count;
            List<int> cells = new List<int>();
            bool edge = false;
            Stack<int> stack = new Stack<int>();
            stack.Push(start);
            component[start] = id;

            while (stack.Count > 0)
            {
                int index = stack.Pop();
                cells.Add(index);
                int x = index % width, y = index / width;
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1) edge = true;

                foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                    int next = ny * width + nx;
                    if (tiles[next] == null || component[next] >= 0) continue;
                    component[next] = id;
                    stack.Push(next);
                }
            }

            components.Add(cells);
            touchesEdge.Add(edge);
        }

        // 2. Phân loại: chạm mép = biển, vùng trong đất liền lớn nhất = sông, còn lại = ao hồ.
        int riverId = Enumerable.Range(0, components.Count)
            .Where(i => !touchesEdge[i])
            .OrderByDescending(i => components[i].Count)
            .DefaultIfEmpty(-1).First();

        // Trong vùng sông, chỗ nước mở rộng (hồ nối với sông) tính là Ao hồ: ô nằm gần một "lõi" 7x7 toàn nước.
        // Lòng sông chỉ rộng ~4-5 ô nên không có lõi như vậy.
        bool[] isLake = new bool[tiles.Length];
        if (riverId >= 0)
        {
            foreach (int index in components[riverId])
            {
                int x = index % width, y = index / width;
                if (!IsWideWater(tiles, width, height, x, y, LakeCoreRadius)) continue;

                for (int dy = -LakeCoreRadius - 1; dy <= LakeCoreRadius + 1; dy++)
                {
                    for (int dx = -LakeCoreRadius - 1; dx <= LakeCoreRadius + 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                        int next = ny * width + nx;
                        if (component[next] == riverId) isLake[next] = true;
                    }
                }
            }
        }

        Dictionary<ZoneInfo, List<Vector3Int>> zoneCells = new Dictionary<ZoneInfo, List<Vector3Int>>
        {
            { Sea, new List<Vector3Int>() }, { River, new List<Vector3Int>() }, { Pond, new List<Vector3Int>() }
        };
        for (int i = 0; i < components.Count; i++)
        {
            foreach (int index in components[i])
            {
                ZoneInfo zone = touchesEdge[i] ? Sea : (i == riverId && !isLake[index] ? River : Pond);
                zoneCells[zone].Add(new Vector3Int(bounds.xMin + index % width, bounds.yMin + index / width, 0));
            }
        }

        // 3. Vẽ từng vùng vào tilemap trigger riêng + gán điểm câu.
        TileBase zoneTile = ZoneTile();
        List<FishingSpotData> spots = AssetDatabase.FindAssets("t:FishingSpotData")
            .Select(g => AssetDatabase.LoadAssetAtPath<FishingSpotData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).ToList();

        List<string> report = new List<string>();
        foreach (KeyValuePair<ZoneInfo, List<Vector3Int>> pair in zoneCells)
        {
            Tilemap zoneMap = GetOrCreateZone(water.transform.parent, pair.Key.objectName);
            Undo.RegisterCompleteObjectUndo(zoneMap, "Build Fishing Zones");
            zoneMap.ClearAllTiles();
            if (pair.Value.Count > 0)
            {
                zoneMap.SetTiles(pair.Value.ToArray(), Enumerable.Repeat(zoneTile, pair.Value.Count).ToArray());
            }

            string spotName = AssignSpot(zoneMap.GetComponent<FishingSpotZone>(), pair.Key, spots);
            report.Add($"- {pair.Key.label}: {pair.Value.Count} ô, điểm câu: {spotName}");
        }

        EditorSceneManager.MarkSceneDirty(water.gameObject.scene);
        string message = $"Đã chia {components.Count} vùng nước:\n" + string.Join("\n", report);
        Debug.Log("[FishingZoneBuilder] " + message);
        EditorUtility.DisplayDialog("Build Fishing Zones", message, "OK");
    }

    private const int LakeCoreRadius = 3;

    private static bool IsWideWater(TileBase[] tiles, int width, int height, int x, int y, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= height || tiles[ny * width + nx] == null) return false;
            }
        }
        return true;
    }

    // Bỏ dấu tiếng Việt để so khớp tên ("Biển" lưu dạng dấu tách rời vẫn khớp "bien").
    private static string Simplify(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        string decomposed = text.ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        return new string(decomposed.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
    }

    // Gán FishingSpotData theo tên điểm câu nếu vùng chưa có; trả về tên điểm câu đang dùng.
    private static string AssignSpot(FishingSpotZone zone, ZoneInfo info, List<FishingSpotData> spots)
    {
        SerializedObject serialized = new SerializedObject(zone);
        SerializedProperty property = serialized.FindProperty("spotData");

        if (property.objectReferenceValue == null)
        {
            FishingSpotData match = spots.FirstOrDefault(s =>
                info.keywords.Any(k => Simplify(s.SpotName).Contains(k) || Simplify(s.name).Contains(k)));
            property.objectReferenceValue = match;
            serialized.ApplyModifiedProperties();
        }

        FishingSpotData spot = property.objectReferenceValue as FishingSpotData;
        return spot != null ? $"{spot.name} ({spot.SpotName})" : "chưa gán - kéo FishingSpotData vào FishingSpotZone";
    }

    private static Tilemap GetOrCreateZone(Transform grid, string name)
    {
        Transform existing = grid.Find(name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(name, typeof(Tilemap));
            go.transform.SetParent(grid, false);
            Undo.RegisterCreatedObjectUndo(go, "Build Fishing Zones");
        }

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0) go.layer = waterLayer;

        Rigidbody2D body = GetOrAdd<Rigidbody2D>(go);
        body.bodyType = RigidbodyType2D.Static;

        TilemapCollider2D tilemapCollider = GetOrAdd<TilemapCollider2D>(go);
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        tilemapCollider.isTrigger = true;

        CompositeCollider2D composite = GetOrAdd<CompositeCollider2D>(go);
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.isTrigger = true;   // vùng câu chỉ để nhận diện, không chặn di chuyển

        GetOrAdd<FishingSpotZone>(go);
        return go.GetComponent<Tilemap>();
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    // Tile vô hình (không sprite) có collider kín ô.
    private static TileBase ZoneTile()
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(ZoneTilePath);
        if (tile != null) return tile;

        MainMapGenerator.EnsureGeneratedFolder();
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, ZoneTilePath);
        return tile;
    }
}
