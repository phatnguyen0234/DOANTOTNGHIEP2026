using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Rải cây, bụi, gốc cây, hàng rào, hoa cỏ... lên map Main theo layout (kiểu rừng Stardew Valley mùa xuân).
// Dùng lại prefab có sẵn: cây chặt được (Pine / Maple / Birch / Mahogany), ForestTree, Bush, Decor, Stairs.
// Toàn bộ object sinh ra nằm dưới GameObject "Generated_Decor", chạy lại sẽ xoá và sinh mới.
public static class MainMapDecorator
{
    private const string ParentName = "Generated_Decor";
    private const string PrefabFolder = "Assets/Prefabs/";
    private const string TreePrefabFolder = PrefabFolder + "Tree";
    private const string ObjectsFolder = MainMapGenerator.AssetPackFolder + "Objects/";
    private const string PropsSheet = MainMapGenerator.AssetPackFolder + "Tileset/ALL props seasons.png";
    private const string FenceSheet = ObjectsFolder + "Exterior/Fence and Bridge/Fence Wood.png";

    private const int FlatSortingOrder = -80;   // hoa cỏ nằm dưới chân Player
    private const int StairsSortingOrder = -86;

    private static System.Random rng;
    private static readonly List<Vector2> bigObjects = new List<Vector2>();

    // Prefab + sprite dùng khi rải
    private static List<GameObject> pineTrees, otherTrees, bushPrefabs;
    private static GameObject forestTreePrefab, decorPrefab, stairsPrefab;
    private static List<Sprite> bushSprites, stumps, logs, twigs, stones, flowers, smallProps, forestProps;
    private static Dictionary<Vector2Int, Sprite> fenceSprites;

    [MenuItem("Tools/Map/Regenerate Decor (random)")]
    public static void RegenerateRandom()
    {
        if (EditorSceneManager.GetActiveScene().name != "Main" || !MainMapGenerator.LoadLayout()) return;

        Tilemap ground = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "Ground");
        if (ground == null) return;

        Decorate(ground, MainMapGenerator.StairsSprite(), System.Environment.TickCount);
        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
    }

    public static void Decorate(Tilemap ground, Sprite stairsSprite, int seed = 2026)
    {
        rng = new System.Random(seed);
        bigObjects.Clear();
        LoadAssets();

        GameObject old = GameObject.Find(ParentName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        GameObject parent = new GameObject(ParentName);
        Undo.RegisterCreatedObjectUndo(parent, "Generate Decor");
        Transform root = parent.transform;

        List<Rect> keepClear = PreservedTileAreas(ground);
        keepClear.AddRange(GameplayAreas());

        int width = MainMapGenerator.Width, height = MainMapGenerator.Height;

        // 1) Cầu thang + hàng rào
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = MainMapGenerator.At(x, y);
                Vector3 center = CellCenter(ground, x, y);

                if (c == 'S')
                {
                    // Chân cầu thang ở đáy ô 'S', phủ lên toàn bộ mặt vách phía trên.
                    Vector3 position = center + new Vector3(0f, MainMapGenerator.FaceRows / 2f, 0f);
                    GameObject stairs = CreateFlat("Stairs", stairsPrefab, stairsSprite, position, StairsSortingOrder, root);
                    stairs.GetComponent<SpriteRenderer>().sprite = stairsSprite;
                }
                else if (c == 'x')
                {
                    CreateFence(x, y, center, root);
                }
            }
        }

        // 2) Cây, bụi, gốc cây, khúc gỗ (có collider / làm mờ) - đặt trước để giữ khoảng cách
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = MainMapGenerator.At(x, y);
                Vector3 center = CellCenter(ground, x, y);
                if (InAreas(keepClear, center) || !IsOpenForBig(x, y)) continue;

                if (c == 'f')
                {
                    if (Chance(0.4f) && HasSpace(center, 1.9f))
                    {
                        double roll = rng.NextDouble();
                        if (roll < 0.5) PlacePrefab(Pick(pineTrees), center, root);
                        else if (roll < 0.65) PlacePrefab(Pick(otherTrees), center, root);
                        else if (roll < 0.75) PlacePrefab(forestTreePrefab, center, root);
                        else PlaceBushCluster(x, y, ground, root);
                        bigObjects.Add(center);
                    }
                    else if (Chance(0.03f) && HasSpace(center, 1.5f))
                    {
                        CreateSolid("Stump", Pick(stumps), center, root, 0.8f, 0.5f);
                        bigObjects.Add(center);
                    }
                    else if (Chance(0.02f) && HasSpace(center, 1.5f))
                    {
                        CreateSolid("Log", Pick(logs), center, root, 0.9f, 0.5f);
                        bigObjects.Add(center);
                    }
                }
                else if (c == 'g')
                {
                    if (Chance(0.02f) && HasSpace(center, 3f))
                    {
                        PlacePrefab(Chance(0.6f) ? Pick(pineTrees) : Pick(otherTrees), center, root);
                        bigObjects.Add(center);
                    }
                    else if (Chance(0.015f) && HasSpace(center, 2.5f))
                    {
                        PlaceBushCluster(x, y, ground, root);
                        bigObjects.Add(center);
                    }
                }
            }
        }

        // 3) Hoa, cỏ dại, nấm, cành khô, đá nhỏ (không collider, nằm dưới chân Player)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = MainMapGenerator.At(x, y);
                Vector3 center = CellCenter(ground, x, y);
                if (InAreas(keepClear, center) || MainMapGenerator.IsCliffBlocked(x, y) || !HasSpace(center, 0.9f)) continue;

                Sprite sprite = null;
                if (c == 'f' && Chance(0.15f))
                {
                    double roll = rng.NextDouble();
                    sprite = roll < 0.45 ? Pick(forestProps) : (roll < 0.75 ? Pick(smallProps) : Pick(twigs));
                }
                else if (c == 'e' && Chance(0.06f)) sprite = Chance(0.5f) ? Pick(twigs) : Pick(stones);
                else if (c == 'g' && Chance(0.04f)) sprite = Chance(0.5f) ? Pick(flowers) : Pick(smallProps);
                else if (c == 'd' && Chance(0.012f)) sprite = Pick(stones);

                if (sprite == null) continue;
                Vector3 jitter = new Vector3(Range(-0.3f, 0.3f), Range(-0.3f, 0.3f), 0f);
                CreateFlat("Decor", decorPrefab, sprite, center + jitter, FlatSortingOrder, root);
            }
        }
    }

    // ---------- Nạp asset ----------

    private static void LoadAssets()
    {
        List<GameObject> treeFolder = AssetDatabase.FindAssets("t:Prefab", new[] { TreePrefabFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(prefab => prefab != null).ToList();

        bushPrefabs = treeFolder.Where(p => p.name.StartsWith("Bush")).ToList();
        forestTreePrefab = treeFolder.FirstOrDefault(p => p.name == "ForestTree");
        List<GameObject> choppable = treeFolder.Where(p => !p.name.StartsWith("Bush") && p.name != "ForestTree").ToList();
        pineTrees = choppable.Where(p => p.name.StartsWith("Pine")).ToList();
        otherTrees = choppable.Where(p => !p.name.StartsWith("Pine")).ToList();
        if (pineTrees.Count == 0) pineTrees = choppable;

        decorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "Decor.prefab");
        stairsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "Stairs.prefab");

        // Mùa xuân: chỉ lấy bụi xanh lá (bỏ xanh ngọc, cam, đỏ).
        bushSprites = GreenSprites(ObjectsFolder + "Tree/Deep Forest/bushes.png", s => s.rect.height < 40);
        stumps = Sprites(ObjectsFolder + "Tree/Common/Shadow/Pine Tree.png", s => s.name == "Pine Tree_5");
        logs = Sprites(ObjectsFolder + "Tree/TREE TRUNKS copiar.png", s => s.rect.x < 32 || (s.rect.x >= 64 && s.rect.x < 96));
        logs.AddRange(Sprites(ObjectsFolder + "Props/Extras.png", s => s.rect.width >= 20));
        twigs = Sprites(ObjectsFolder + "Props/Extras.png", s => s.rect.width < 20);
        stones = Sprites(ObjectsFolder + "Props/Spring/Ground stones.png", s => s.rect.x < 64);
        flowers = Sprites(MainMapGenerator.AssetPackFolder + "Tileset/ALL props 2.png", s => true);
        flowers.AddRange(Sprites(MainMapGenerator.AssetPackFolder + "Tileset/ALL props 3.png", s => true));
        smallProps = PropsByRow(new[] { 0, 5, 8 }, 0);
        forestProps = PropsByRow(new[] { 4 }, 272);     // nấm nhỏ cuối hàng 4
        forestProps.AddRange(PropsByRow(new[] { 5, 8 }, 0));
        fenceSprites = MainMapGenerator.LoadSprites(FenceSheet);
    }

    // ---------- Tạo object ----------

    // Đặt prefab sao cho gốc (collider không phải trigger) nằm giữa ô; không có collider thì đặt đáy sprite sát đáy ô.
    private static void PlacePrefab(GameObject prefab, Vector3 cellCenter, Transform root)
    {
        if (prefab == null) return;

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        BoxCollider2D trunk = go.GetComponents<BoxCollider2D>().FirstOrDefault(b => !b.isTrigger);
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();

        float offsetY;
        if (trunk != null) offsetY = -trunk.offset.y * go.transform.localScale.y;
        else if (renderer != null && renderer.sprite != null) offsetY = renderer.sprite.bounds.size.y / 2f - 0.45f;
        else offsetY = 0f;

        go.transform.position = cellCenter + new Vector3(Range(-0.2f, 0.2f), offsetY, 0f);
    }

    // Cụm 1-3 bụi xanh (prefab Bush có trigger + làm mờ), đổi sprite ngẫu nhiên cho đa dạng.
    private static void PlaceBushCluster(int x, int y, Tilemap ground, Transform root)
    {
        int count = 1 + rng.Next(3);
        Vector2Int[] offsets = { Vector2Int.zero, Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        for (int i = 0; i < count; i++)
        {
            Vector2Int offset = i == 0 ? Vector2Int.zero : offsets[1 + rng.Next(4)];
            int cx = x + offset.x, cy = y + offset.y;
            char c = MainMapGenerator.At(cx, cy);
            if ((c != 'f' && c != 'g') || !IsOpenForBig(cx, cy)) continue;

            Vector3 center = CellCenter(ground, cx, cy);
            if (i > 0 && !HasSpace(center, 0.9f)) continue;

            GameObject prefab = Pick(bushPrefabs);
            if (prefab == null)
            {
                CreateSolid("Bush", Pick(bushSprites), center, root, 0.8f, 0.4f);
                continue;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            Sprite sprite = Pick(bushSprites);
            if (renderer != null && sprite != null) renderer.sprite = sprite;
            float half = renderer != null && renderer.sprite != null ? renderer.sprite.bounds.size.y / 2f : 1f;
            go.transform.position = center + new Vector3(Range(-0.25f, 0.25f), half - 0.45f, 0f);
            if (i > 0) bigObjects.Add(center);
        }
    }

    // Hàng rào gỗ: chọn mảnh theo hàng xóm cùng là rào (đầu / giữa / góc / cột dọc), có collider chắn đường.
    private static void CreateFence(int x, int y, Vector3 center, Transform root)
    {
        bool n = MainMapGenerator.At(x, y + 1) == 'x';
        bool s = MainMapGenerator.At(x, y - 1) == 'x';
        bool e = MainMapGenerator.At(x + 1, y) == 'x';
        bool w = MainMapGenerator.At(x - 1, y) == 'x';

        Vector2Int cell;
        if (e && s && !w && !n) cell = new Vector2Int(0, 0);
        else if (w && s && !e && !n) cell = new Vector2Int(2, 0);
        else if (e && n && !w && !s) cell = new Vector2Int(0, 2);
        else if (w && n && !e && !s) cell = new Vector2Int(2, 2);
        else if (e && w) cell = new Vector2Int(1, 2);
        else if (e) cell = new Vector2Int(1, 3);
        else if (w) cell = new Vector2Int(2, 3);
        else if (n || s) cell = new Vector2Int(0, 1);
        else cell = new Vector2Int(1, 4);

        if (!fenceSprites.TryGetValue(cell, out Sprite sprite)) return;

        GameObject go = CreateSprite("Fence", sprite, center, 0, root);
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 0.4f);
        box.offset = new Vector2(0f, -0.25f);
    }

    // Vật cản thấp (gốc cây, khúc gỗ): collider phủ phần dưới sprite.
    private static void CreateSolid(string name, Sprite sprite, Vector3 cellCenter, Transform root, float widthRatio, float heightRatio)
    {
        if (sprite == null) return;
        Vector3 position = cellCenter + new Vector3(Range(-0.15f, 0.15f), sprite.bounds.size.y / 2f - 0.45f, 0f);
        GameObject go = CreateSprite(name, sprite, position, 0, root);
        Vector2 size = sprite.bounds.size;

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(size.x * widthRatio, size.y * heightRatio);
        box.offset = new Vector2(0f, -size.y * (1f - heightRatio) / 2f);
    }

    // Object phẳng (hoa cỏ, cầu thang): dùng prefab nếu có, không thì tạo SpriteRenderer mới.
    private static GameObject CreateFlat(string name, GameObject prefab, Sprite sprite, Vector3 position, int sortingOrder, Transform root)
    {
        if (prefab == null) return CreateSprite(name, sprite, position, sortingOrder, root);

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
        go.transform.position = position;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sprite = sprite;
        return go;
    }

    private static GameObject CreateSprite(string name, Sprite sprite, Vector3 position, int sortingOrder, Transform root)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = position;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    // ---------- Điều kiện đặt ----------

    // Không đặt vật to sát đường đi, nước, cầu, cầu thang, hàng rào hay vách đá.
    private static bool IsOpenForBig(int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if ("wbBSedx".IndexOf(MainMapGenerator.At(x + dx, y + dy)) >= 0) return false;
                if (MainMapGenerator.IsCliffBlocked(x + dx, y + dy)) return false;
            }
        }
        return true;
    }

    private static bool HasSpace(Vector3 position, float minDistance)
    {
        foreach (Vector2 other in bigObjects)
        {
            if (Vector2.Distance(other, position) < minDistance) return false;
        }
        return true;
    }

    // Vùng của các tile đặt tay trên Ground (ví dụ ngôi nhà) để không rải hoa cỏ đè lên.
    private static List<Rect> PreservedTileAreas(Tilemap ground)
    {
        List<Rect> areas = new List<Rect>();
        foreach (Vector3Int cell in ground.cellBounds.allPositionsWithin)
        {
            TileBase tile = ground.GetTile(cell);
            if (tile == null || tile.name.StartsWith("Tileset Grass")) continue;
            if (AssetDatabase.GetAssetPath(tile).StartsWith(MainMapGenerator.GeneratedFolder)) continue;

            Sprite sprite = ground.GetSprite(cell);
            Vector2 size = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
            Vector3 center = ground.GetCellCenterWorld(cell);
            areas.Add(new Rect(center.x - size.x / 2f - 1f, center.y - size.y / 2f - 1f, size.x + 2f, size.y + 2f));
        }
        return areas;
    }

    // Chừa trống quanh nhà (object tên có "Exterior"), cửa chuyển scene (AreaSwitch) và Player để không bị cây chắn.
    private static List<Rect> GameplayAreas()
    {
        List<Rect> areas = new List<Rect>();

        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>())
        {
            if (renderer.transform.root.name.Contains("Exterior"))
            {
                Bounds b = renderer.bounds;
                areas.Add(new Rect(b.min.x - 2f, b.min.y - 3f, b.size.x + 4f, b.size.y + 5f));
            }
        }

        List<Transform> points = Object.FindObjectsByType<AreaSwitch>().Select(a => a.transform).ToList();
        points.AddRange(Object.FindObjectsByType<PlayerMovement>().Select(p => p.transform));

        foreach (Transform point in points)
        {
            areas.Add(new Rect(point.position.x - 3f, point.position.y - 3f, 6f, 6f));
        }
        return areas;
    }

    private static bool InAreas(List<Rect> areas, Vector3 position)
    {
        foreach (Rect area in areas)
        {
            if (area.Contains(position)) return true;
        }
        return false;
    }

    // ---------- Tiện ích ----------

    private static Vector3 CellCenter(Tilemap ground, int x, int y)
    {
        return ground.GetCellCenterWorld(new Vector3Int(MainMapGenerator.Origin.x + x, MainMapGenerator.Origin.y + y, 0));
    }

    private static List<Sprite> Sprites(string path, System.Func<Sprite, bool> filter)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Where(filter).ToList();
    }

    // Sprite nhỏ trong "ALL props seasons" theo hàng 16px (tính từ trên xuống): hàng 0 hoa xuân, 4 nấm, 5 lá xanh, 8 cây cỏ...
    private static List<Sprite> PropsByRow(int[] rowsFromTop, float minX)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PropsSheet);
        return Sprites(PropsSheet, s =>
        {
            int row = Mathf.FloorToInt((texture.height - s.rect.center.y) / 16f);
            return rowsFromTop.Contains(row) && s.rect.x >= minX;
        });
    }

    // Lọc sprite theo màu trung bình: giữ những sprite xanh lá (G nổi trội hơn hẳn R và B).
    private static List<Sprite> GreenSprites(string path, System.Func<Sprite, bool> filter)
    {
        Texture2D pixels = new Texture2D(2, 2);
        pixels.LoadImage(System.IO.File.ReadAllBytes(path));

        List<Sprite> result = Sprites(path, s =>
        {
            if (!filter(s)) return false;

            Rect rect = s.rect;
            Color[] colors = pixels.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
            float r = 0f, g = 0f, b = 0f;
            int count = 0;
            foreach (Color color in colors)
            {
                if (color.a < 0.5f || color.r + color.g + color.b < 0.47f) continue;   // bỏ nền trong suốt và viền tối
                r += color.r; g += color.g; b += color.b;
                count++;
            }
            return count > 0 && g > r * 1.3f && g > b * 1.6f;
        });

        Object.DestroyImmediate(pixels);
        return result;
    }

    private static T Pick<T>(List<T> list) where T : class
    {
        return list == null || list.Count == 0 ? null : list[rng.Next(list.Count)];
    }

    private static bool Chance(float probability) => rng.NextDouble() < probability;

    private static float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
