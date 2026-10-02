using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Công cụ Editor: vẽ lại map Main từ file layout text, tự ghép viền (autotile 47 ô).
// MainLayout.txt - loại đất (hàng đầu tiên là phía Bắc):
//   g = cỏ, d = đất, w = nước, b = cầu trên nước, B = đầu cầu trên đất,
//   f = cỏ rừng, e = đất rừng, S = chân cầu thang (ô đất ngay dưới mặt vách), x = hàng rào gỗ (nền cỏ).
// MainLevels.txt - độ cao từng ô (0 = thấp nhất, 1, 2... cao dần). Mép phía Nam của mỗi tầng là vách cao FaceRows ô.
// Giữ nguyên logic cũ: Ground (tag "Ground") vẫn chứa cỏ/đất để cuốc - trồng; nước và vách đá có collider riêng.
// Chạy qua menu Tools > Map > Generate Main Map. Có thể Ctrl+Z để hoàn tác.
public static class MainMapGenerator
{
    private const string LayoutPath = "Assets/MapLayout/MainLayout.txt";
    private const string LevelsPath = "Assets/MapLayout/MainLevels.txt";
    public const string GeneratedFolder = "Assets/Tiles/Generated";
    private const string SeaTilePath = "Assets/Tiles/Sea.asset";
    public const string AssetPackFolder = "Assets/3rdAssetImport/Farm RPG - Tiny Asset Pack - (All in One)/";
    private const string TilesetFolder = AssetPackFolder + "Tileset/";
    private const string GrassTexture = TilesetFolder + "Tileset Grass Spring.png";
    private const string WaterTexture = TilesetFolder + "Tileset Grass Water Spring.png";
    private const string BridgeTexture = TilesetFolder + "Bridge Beach Tileset.png";
    private const string CliffTexture = TilesetFolder + "Tileset Grass Cliff Tileset Spring.png";
    private const string StairsSourceTexture = TilesetFolder + "Extra Village Tilesets.png";

    // Ô dưới-trái của layout trong toạ độ cell. Map 230x170 có viền biển bao ngoài (generator tự vẽ Sea);
    // các phần map cũ giữ nguyên toạ độ.
    public static readonly Vector2Int Origin = new Vector2Int(-115, -95);

    // Vị trí khối autotile trong từng tileset (cột, hàng tính từ trên xuống, đơn vị 16px).
    private static readonly Vector2Int GrassFullCell = new Vector2Int(9, 2);
    private const int DirtBlockRow = 8;
    private const int WaterBlockRow = 4;
    private const int CliffFaceRow = 4;
    private const int FullMask = 255;

    // Rừng dùng bộ cỏ xuân màu đậm nằm bên phải cùng tileset (lệch 12 cột so với bộ cỏ sáng).
    private const int DarkGrassColumnOffset = 12;

    // Số hàng mặt vách (FaceRows - 1 ô thân + 1 ô chân), nằm trong phần phía Nam của mỗi tầng.
    public const int FaceRows = 3;

    // Bit hàng xóm cùng loại: N=1, E=2, S=4, W=8, NE=16, SE=32, SW=64, NW=128 (góc chỉ tính khi 2 cạnh kề cùng loại).
    private static readonly Dictionary<int, Vector2Int> BlobTable = new Dictionary<int, Vector2Int>
    {
        {0, new Vector2Int(0, 3)}, {1, new Vector2Int(0, 2)}, {2, new Vector2Int(1, 3)}, {3, new Vector2Int(1, 2)},
        {4, new Vector2Int(0, 0)}, {5, new Vector2Int(0, 1)}, {6, new Vector2Int(1, 0)}, {7, new Vector2Int(1, 1)},
        {8, new Vector2Int(3, 3)}, {9, new Vector2Int(3, 2)}, {10, new Vector2Int(2, 3)}, {11, new Vector2Int(2, 2)},
        {12, new Vector2Int(3, 0)}, {13, new Vector2Int(3, 1)}, {14, new Vector2Int(2, 0)}, {15, new Vector2Int(2, 1)},
        {19, new Vector2Int(8, 3)}, {23, new Vector2Int(4, 2)}, {27, new Vector2Int(5, 3)}, {31, new Vector2Int(7, 0)},
        {38, new Vector2Int(8, 0)}, {39, new Vector2Int(4, 1)}, {46, new Vector2Int(5, 0)}, {47, new Vector2Int(7, 3)},
        {55, new Vector2Int(8, 1)}, {63, new Vector2Int(8, 2)}, {76, new Vector2Int(11, 0)}, {77, new Vector2Int(7, 1)},
        {78, new Vector2Int(6, 0)}, {79, new Vector2Int(4, 3)}, {95, new Vector2Int(9, 1)}, {110, new Vector2Int(10, 0)},
        {111, new Vector2Int(9, 0)}, {127, new Vector2Int(5, 1)}, {137, new Vector2Int(11, 3)}, {139, new Vector2Int(6, 3)},
        {141, new Vector2Int(7, 2)}, {143, new Vector2Int(4, 0)}, {155, new Vector2Int(9, 3)}, {159, new Vector2Int(10, 3)},
        {175, new Vector2Int(10, 2)}, {191, new Vector2Int(5, 2)}, {205, new Vector2Int(11, 2)}, {207, new Vector2Int(11, 1)},
        {223, new Vector2Int(6, 2)}, {239, new Vector2Int(6, 1)}, {255, new Vector2Int(9, 2)}
    };

    private static string[] rows;
    private static string[] levelRows;
    private static int width, height;

    public static int Width => width;
    public static int Height => height;

    [MenuItem("Tools/Map/Generate Main Map")]
    public static void Generate()
    {
        if (EditorSceneManager.GetActiveScene().name != "Main")
        {
            EditorUtility.DisplayDialog("Generate Main Map", "Hãy mở scene Main trước khi chạy.", "OK");
            return;
        }

        if (!LoadLayout()) return;

        Tilemap ground = FindTilemap("Ground");
        Tilemap sea = FindTilemap("Sea");
        if (ground == null || sea == null)
        {
            EditorUtility.DisplayDialog("Generate Main Map", "Không tìm thấy Tilemap 'Ground' hoặc 'Sea' trong scene.", "OK");
            return;
        }

        TileBase seaTile = AssetDatabase.LoadAssetAtPath<TileBase>(SeaTilePath);
        Dictionary<Vector2Int, Sprite> grassSprites = LoadSprites(GrassTexture);
        Dictionary<Vector2Int, Sprite> waterSprites = LoadSprites(WaterTexture);
        Dictionary<Vector2Int, Sprite> bridgeSprites = LoadSprites(BridgeTexture);
        Dictionary<Vector2Int, Sprite> cliffSprites = LoadSprites(CliffTexture);

        if (!ValidateSprites(grassSprites, waterSprites, bridgeSprites, cliffSprites)) return;

        if (!EditorUtility.DisplayDialog("Generate Main Map",
                "Sẽ vẽ lại địa hình (Ground / Sea / Water / Bridge / Forest / Cliff) theo layout và rải lại cây + đồ trang trí.\n" +
                "Cây/đá đặt tay nằm trên nước, cầu, vách đá, cầu thang hoặc trong khu rừng sẽ bị xoá.\n\nCó thể Ctrl+Z để hoàn tác.",
                "Generate", "Huỷ"))
        {
            return;
        }

        Transform gridTransform = ground.transform.parent;
        Tilemap water = GetOrCreateTilemap(gridTransform, "Water", -95, true);
        Tilemap forest = GetOrCreateTilemap(gridTransform, "Forest", -89, false);
        Tilemap bridge = GetOrCreateTilemap(gridTransform, "Bridge", -88, false);
        Tilemap cliff = GetOrCreateTilemap(gridTransform, "Cliff", -87, false);
        Tilemap cliffCollider = GetOrCreateTilemap(gridTransform, "CliffCollider", -87, true);

        Undo.RegisterCompleteObjectUndo(new Object[] { ground, sea, water, forest, bridge, cliff, cliffCollider }, "Generate Main Map");

        TileBase grassTile = GetOrCreateTile("Grass", grassSprites[GrassFullCell], Tile.ColliderType.None);
        TileBase blockTile = GetOrCreateTile("Collider_Block", null, Tile.ColliderType.Grid);
        Dictionary<int, TileBase> cliffMidTiles = new Dictionary<int, TileBase>();
        foreach (int col in FaceColumns)
        {
            cliffMidTiles[col] = GetOrCreateTile($"CliffMid_{col}", CliffMidSprite(col), Tile.ColliderType.None);
        }

        water.ClearAllTiles();
        forest.ClearAllTiles();
        bridge.ClearAllTiles();
        cliff.ClearAllTiles();
        cliffCollider.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int cell = new Vector3Int(Origin.x + x, Origin.y + y, 0);
                char c = At(x, y);

                if (c == 'w' || c == 'b')
                {
                    if (!IsPreserved(ground.GetTile(cell))) ground.SetTile(cell, null);
                    sea.SetTile(cell, seaTile);

                    if (c == 'w')
                    {
                        int mask = Mask(x, y, "wb");
                        if (mask == FullMask)
                        {
                            // Ô nước giữa sông: không cần sprite (nhìn thấy Sea bên dưới), chỉ cần collider.
                            water.SetTile(cell, GetOrCreateTile("Water_Full", null, Tile.ColliderType.Grid));
                        }
                        else
                        {
                            water.SetTile(cell, BlobTile("Water", waterSprites, mask, WaterBlockRow, Tile.ColliderType.Grid));
                        }
                    }
                }
                else if (!IsPreserved(ground.GetTile(cell)))
                {
                    if (c == 'd' || c == 'B' || c == 'S')
                    {
                        ground.SetTile(cell, BlobTile("Dirt", grassSprites, Mask(x, y, "dBS"), DirtBlockRow, Tile.ColliderType.None));
                    }
                    else if (c == 'e')
                    {
                        ground.SetTile(cell, BlobTile("ForestDirt", grassSprites, Mask(x, y, "e"), DirtBlockRow, Tile.ColliderType.None, DarkGrassColumnOffset));
                    }
                    else
                    {
                        ground.SetTile(cell, grassTile);
                    }
                }

                // Lớp cỏ rừng phủ lên cỏ thường (viền cỏ rừng ăn dần ra cỏ thường).
                if (c == 'f')
                {
                    forest.SetTile(cell, BlobTile("Forest", grassSprites, Mask(x, y, "fe"), 0, Tile.ColliderType.None, DarkGrassColumnOffset));
                }

                if (c == 'b' || c == 'B')
                {
                    Vector2Int sheetCell = BridgeCell(x, y);
                    bridge.SetTile(cell, GetOrCreateTile($"Bridge_{sheetCell.x}_{sheetCell.y}", bridgeSprites[sheetCell], Tile.ColliderType.None));
                }

                PaintCliff(x, y, cell, cliff, cliffCollider, cliffSprites, cliffMidTiles, blockTile);
            }
        }

        int removed = RemoveHandPlacedObjects(ground);
        MainMapDecorator.Decorate(ground, StairsSprite());

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
        Debug.Log($"[MainMapGenerator] Đã vẽ map {width}x{height}. Đã xoá {removed} cây/đá đặt tay bị chồng lên nước / vách / rừng.");
    }

    // Cột tile mặt vách: 0 = đứng riêng 1 ô, 8 = đầu trái, 9 = giữa, 11 = đầu phải.
    private static readonly int[] FaceColumns = { 0, 8, 9, 11 };

    // Tầng k (vùng có độ cao >= k): FaceRows hàng phía Nam của mỗi cột là mặt vách (thân đá + chân),
    // phần còn lại là mặt trên, viền bằng autotile của tileset Cliff.
    // Mặt vách + viền mặt trên có collider (tilemap CliffCollider), chừa lối ở cột cầu thang.
    private static void PaintCliff(int x, int y, Vector3Int cell, Tilemap cliff, Tilemap cliffCollider,
        Dictionary<Vector2Int, Sprite> cliffSprites, Dictionary<int, TileBase> cliffMidTiles, TileBase blockTile)
    {
        int level = LevelAt(x, y);
        if (level <= 0) return;

        int faceRow = FaceRow(x, y, level);
        if (faceRow == 0)
        {
            int mask = MaskWhere(x, y, (cx, cy) => IsLevelTop(cx, cy, level));
            if (mask == FullMask) return;

            cliff.SetTile(cell, BlobTile("Cliff", cliffSprites, mask, 0, Tile.ColliderType.None));

            // Viền mặt trên chặn đường, trừ ô ngay trên đầu cầu thang.
            bool aboveStairs = FaceRow(x, y - 1, level) > 0 && IsStairsColumn(x, y - 1, level);
            if (!aboveStairs) cliffCollider.SetTile(cell, blockTile);
            return;
        }

        bool left = FaceRow(x - 1, y, level) == faceRow;
        bool right = FaceRow(x + 1, y, level) == faceRow;
        int col = !left && !right ? 0 : (!left ? 8 : (!right ? 11 : 9));

        TileBase faceTile = faceRow == FaceRows
            ? GetOrCreateTile($"CliffFace_{col}_{CliffFaceRow}", cliffSprites[new Vector2Int(col, CliffFaceRow)], Tile.ColliderType.None)
            : cliffMidTiles[col];
        cliff.SetTile(cell, faceTile);

        if (!IsStairsColumn(x, y, level)) cliffCollider.SetTile(cell, blockTile);
    }

    // Độ cao của ô theo MainLevels.txt (không có file / ngoài map = 0).
    public static int LevelAt(int x, int y)
    {
        if (levelRows == null || x < 0 || y < 0 || x >= width || y >= height) return 0;
        string row = levelRows[height - 1 - y];
        return x < row.Length && char.IsDigit(row[x]) ? row[x] - '0' : 0;
    }

    // 0 nếu không phải mặt vách của tầng k; 1..FaceRows nếu là mặt vách (1 = sát mặt trên, FaceRows = chân vách).
    // Nước phía dưới được coi như liền tầng, nên chỗ tầng cao giáp biển / sông chỉ có viền cỏ, không dựng vách.
    public static int FaceRow(int x, int y, int level)
    {
        if (LevelAt(x, y) < level) return 0;

        int below = 0;
        while (below < FaceRows && (LevelAt(x, y - below - 1) >= level || IsWater(x, y - below - 1))) below++;
        return below < FaceRows ? FaceRows - below : 0;
    }

    private static bool IsWater(int x, int y) => At(x, y) == 'w' || At(x, y) == 'b';

    private static bool IsLevelTop(int x, int y, int level) => LevelAt(x, y) >= level && FaceRow(x, y, level) == 0;

    // Cột mặt vách có cầu thang: đi xuống hết phần tầng k thì gặp ô 'S'.
    private static bool IsStairsColumn(int x, int y, int level)
    {
        while (LevelAt(x, y) >= level && y >= 0) y--;
        return At(x, y) == 'S';
    }

    private static TileBase BlobTile(string prefix, Dictionary<Vector2Int, Sprite> sprites, int mask, int blockRow,
        Tile.ColliderType colliderType, int columnOffset = 0)
    {
        Vector2Int blob = BlobTable[mask];
        Vector2Int sheetCell = new Vector2Int(blob.x + columnOffset, blob.y + blockRow);
        return GetOrCreateTile($"{prefix}_{sheetCell.x}_{sheetCell.y}", sprites[sheetCell], colliderType);
    }

    // Kiểm tra spritesheet đã được cắt 16x16 đủ các ô cần dùng chưa, thiếu thì báo cách sửa.
    private static bool ValidateSprites(Dictionary<Vector2Int, Sprite> grass, Dictionary<Vector2Int, Sprite> water,
        Dictionary<Vector2Int, Sprite> bridge, Dictionary<Vector2Int, Sprite> cliff)
    {
        List<string> missing = new List<string>();

        foreach (KeyValuePair<int, Vector2Int> entry in BlobTable)
        {
            Vector2Int top = entry.Value;
            Vector2Int dirtCell = new Vector2Int(top.x, top.y + DirtBlockRow);
            Vector2Int waterCell = new Vector2Int(top.x, top.y + WaterBlockRow);
            Vector2Int darkGrassCell = new Vector2Int(top.x + DarkGrassColumnOffset, top.y);
            Vector2Int darkDirtCell = new Vector2Int(top.x + DarkGrassColumnOffset, top.y + DirtBlockRow);
            if (!grass.ContainsKey(dirtCell) || !grass.ContainsKey(darkGrassCell) || !grass.ContainsKey(darkDirtCell)) AddMissing(missing, GrassTexture);
            if (entry.Key != FullMask && !water.ContainsKey(waterCell)) AddMissing(missing, WaterTexture);
            if (entry.Key != FullMask && !cliff.ContainsKey(top)) AddMissing(missing, CliffTexture);
        }
        if (!grass.ContainsKey(GrassFullCell)) AddMissing(missing, GrassTexture);

        foreach (int col in FaceColumns)
        {
            if (!cliff.ContainsKey(new Vector2Int(col, CliffFaceRow))) AddMissing(missing, CliffTexture);
        }

        foreach (int col in new[] { 0, 2, 3 })
        {
            for (int row = 1; row <= 3; row++)
            {
                if (!bridge.ContainsKey(new Vector2Int(col, row))) AddMissing(missing, BridgeTexture);
            }
        }

        if (missing.Count == 0) return true;

        EditorUtility.DisplayDialog("Generate Main Map",
            "Các spritesheet sau chưa được cắt thành ô 16x16:\n\n" + string.Join("\n", missing) +
            "\n\nChọn file → Inspector: Sprite Mode = Multiple → Apply → Open Sprite Editor → " +
            "Slice → Type: Grid By Cell Size, Pixel Size 16 x 16 → Slice → Apply. Sau đó chạy lại.", "OK");
        return false;
    }

    private static void AddMissing(List<string> missing, string texturePath)
    {
        string fileName = Path.GetFileName(texturePath);
        if (!missing.Contains(fileName)) missing.Add(fileName);
    }

    public static bool LoadLayout()
    {
        TextAsset layout = AssetDatabase.LoadAssetAtPath<TextAsset>(LayoutPath);
        if (layout == null)
        {
            EditorUtility.DisplayDialog("Generate Main Map", $"Không tìm thấy file layout: {LayoutPath}", "OK");
            return false;
        }

        rows = layout.text.Replace("\r", "").Trim('\n').Split('\n');
        height = rows.Length;
        width = rows[0].Length;

        TextAsset levels = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelsPath);
        levelRows = levels != null ? levels.text.Replace("\r", "").Trim('\n').Split('\n') : null;
        return true;
    }

    // x tính từ trái, y tính từ dưới lên; ngoài layout coi như nước (biển).
    public static char At(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return 'w';
        string row = rows[height - 1 - y];
        return x < row.Length ? row[x] : 'w';
    }

    // Ký tự layout tại một cell của Tilemap.
    public static char AtCell(Vector3Int cell)
    {
        return At(cell.x - Origin.x, cell.y - Origin.y);
    }

    // Ô bị chặn bởi vách đá (mặt vách hoặc viền mặt trên) - dùng để không đặt cây / đồ trang trí lên đó.
    public static bool IsCliffBlocked(int x, int y)
    {
        int level = LevelAt(x, y);
        if (level <= 0) return false;
        return FaceRow(x, y, level) > 0 || MaskWhere(x, y, (cx, cy) => IsLevelTop(cx, cy, level)) != FullMask;
    }

    public static int Mask(int x, int y, string same)
    {
        return MaskWhere(x, y, (cx, cy) => same.IndexOf(At(cx, cy)) >= 0);
    }

    private static int MaskWhere(int x, int y, System.Func<int, int, bool> same)
    {
        bool n = same(x, y + 1);
        bool e = same(x + 1, y);
        bool s = same(x, y - 1);
        bool w = same(x - 1, y);

        int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
        if (n && e && same(x + 1, y + 1)) mask |= 16;
        if (s && e && same(x + 1, y - 1)) mask |= 32;
        if (s && w && same(x - 1, y - 1)) mask |= 64;
        if (n && w && same(x - 1, y + 1)) mask |= 128;
        return mask;
    }

    // Cầu là hình chữ nhật: cột 0 / 2 / 3 = trái / giữa / phải, hàng 1 / 2 / 3 = trên / giữa / dưới.
    private static Vector2Int BridgeCell(int x, int y)
    {
        const string bridgeChars = "bB";
        bool n = bridgeChars.IndexOf(At(x, y + 1)) >= 0;
        bool s = bridgeChars.IndexOf(At(x, y - 1)) >= 0;
        bool e = bridgeChars.IndexOf(At(x + 1, y)) >= 0;
        bool w = bridgeChars.IndexOf(At(x - 1, y)) >= 0;

        int col = !w ? 0 : (!e ? 3 : 2);
        int row = !n ? 1 : (!s ? 3 : 2);
        return new Vector2Int(col, row);
    }

    // Giữ lại các tile đặt tay không thuộc terrain (ví dụ tile ngôi nhà trên Ground).
    private static bool IsPreserved(TileBase tile)
    {
        if (tile == null) return false;
        if (tile.name.StartsWith("Tileset Grass")) return false;
        return !AssetDatabase.GetAssetPath(tile).StartsWith(GeneratedFolder);
    }

    private static Tilemap FindTilemap(string name)
    {
        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            if (tilemap.name == name) return tilemap;
        }
        return null;
    }

    private static Tilemap GetOrCreateTilemap(Transform grid, string name, int sortingOrder, bool withCollider)
    {
        Transform existing = grid.Find(name);
        if (existing != null) return existing.GetComponent<Tilemap>();

        GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;

        if (withCollider)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (name == "Water" && waterLayer >= 0) go.layer = waterLayer;

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            TilemapCollider2D tilemapCollider = go.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            CompositeCollider2D composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        Undo.RegisterCreatedObjectUndo(go, "Generate Main Map");
        return go.GetComponent<Tilemap>();
    }

    // Lấy sprite theo (cột, hàng tính từ trên xuống) trong spritesheet đã cắt 16x16.
    public static Dictionary<Vector2Int, Sprite> LoadSprites(string texturePath)
    {
        Dictionary<Vector2Int, Sprite> result = new Dictionary<Vector2Int, Sprite>();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
        {
            if (asset is Sprite sprite && sprite.rect.width == 16 && sprite.rect.height == 16)
            {
                int col = Mathf.RoundToInt(sprite.rect.x / 16f);
                int row = Mathf.RoundToInt((texture.height - sprite.rect.y - sprite.rect.height) / 16f);
                result[new Vector2Int(col, row)] = sprite;
            }
        }
        return result;
    }

    private static TileBase GetOrCreateTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = $"{GeneratedFolder}/{name}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;

        EnsureGeneratedFolder();

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    public static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Tiles", "Generated");
        }
    }

    // Cầu thang gỗ cắt từ Extra Village Tilesets (ô 8,2 - 8,3, cao 32px), kéo dài phần bậc giữa
    // để phủ hết mặt vách: cao (FaceRows + 1) ô, chân đặt trên ô 'S'.
    public static Sprite StairsSprite()
    {
        int height = (FaceRows + 1) * 16;
        return GeneratedSprite($"Stairs_{FaceRows}", 16, height, StairsSourceTexture, (source, x, yTop) =>
        {
            const int sourceX = 8 * 16, sourceTop = 2 * 16, topPart = 10, bottomPart = 10, middle = 12;
            int sy;
            if (yTop < topPart) sy = yTop;
            else if (yTop >= height - bottomPart) sy = 32 - (height - yTop);
            else sy = topPart + (yTop - topPart) % middle;
            return GetPixelTopDown(source, sourceX + x, sourceTop + sy);
        });
    }

    // Ô thân vách: lấy 12 hàng trên của ô mặt vách rồi lặp lại 4 hàng đầu để nối dọc liền mạch.
    private static Sprite CliffMidSprite(int column)
    {
        return GeneratedSprite($"CliffMid_{column}", 16, 16, CliffTexture, (source, x, yTop) =>
        {
            int sy = yTop < 12 ? yTop : yTop - 12;
            return GetPixelTopDown(source, column * 16 + x, CliffFaceRow * 16 + sy);
        });
    }

    private static Color GetPixelTopDown(Texture2D texture, int x, int yTop)
    {
        return texture.GetPixel(x, texture.height - 1 - yTop);
    }

    // Tạo sprite mới từ pixel của một spritesheet có sẵn, lưu PNG vào thư mục Generated (chỉ tạo 1 lần).
    private static Sprite GeneratedSprite(string name, int width, int height, string sourcePath,
        System.Func<Texture2D, int, int, Color> pixelAt)
    {
        string path = $"{GeneratedFolder}/{name}.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        EnsureGeneratedFolder();

        Texture2D source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(sourcePath));
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int yTop = 0; yTop < height; yTop++)
        {
            for (int x = 0; x < width; x++)
            {
                result.SetPixel(x, height - 1 - yTop, pixelAt(source, x, yTop));
            }
        }
        result.Apply();
        File.WriteAllBytes(path, result.EncodeToPNG());
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(result);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Xoá cây (layer Tree) và đá (layer Rock) đặt tay có gốc nằm trên nước, cầu, vách đá, cầu thang hoặc trong rừng.
    private static int RemoveHandPlacedObjects(Tilemap ground)
    {
        int treeLayer = LayerMask.NameToLayer("Tree");
        int rockLayer = LayerMask.NameToLayer("Rock");
        int removed = 0;

        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>())
        {
            GameObject go = renderer.gameObject;
            if (go.transform.parent != null) continue;
            if (go.layer != treeLayer && go.layer != rockLayer) continue;

            Bounds bounds = renderer.bounds;
            Vector3Int cell = ground.WorldToCell(new Vector3(bounds.center.x, bounds.min.y + 0.3f, 0f));
            int x = cell.x - Origin.x, y = cell.y - Origin.y;
            char c = At(x, y);

            if ("wbBSfex".IndexOf(c) >= 0 || IsCliffBlocked(x, y))
            {
                Undo.DestroyObjectImmediate(go);
                removed++;
            }
        }
        return removed;
    }
}
