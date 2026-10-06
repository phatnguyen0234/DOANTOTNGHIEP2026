using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tạo map 4 mùa từ chính map đang có trong scene (kể cả phần vẽ tay):
// 1. Quét mọi tilemap, lấy các tile dùng sprite của tileset mùa Xuân (tên file có "Spring") và ô thân vách CliffMid.
// 2. Tự cắt 16x16 các tileset Hạ / Thu / Đông nếu chưa cắt.
// 3. Tạo tile tương ứng cho từng mùa (cùng vị trí ô trong tileset của mùa đó) vào Assets/Tiles/Generated/Seasons/.
// 4. Ghi bảng SeasonTileSet và gán cho SeasonMapManager trong scene.
// Menu Tools > Map > Preview Season để xem trước map từng mùa ngay trong Editor (Ctrl+Z để hoàn tác).
public static class SeasonTileBuilder
{
    private const string SeasonFolder = MainMapGenerator.GeneratedFolder + "/Seasons";
    private const string TileSetPath = SeasonFolder + "/SeasonTileSet.asset";

    // Mùa trong game -> chữ mùa trong tên file tileset của asset pack.
    private static readonly (Season season, string sheetWord)[] OtherSeasons =
    {
        (Season.Summer, "Summer"), (Season.Autumn, "Fall"), (Season.Winter, "Winter")
    };

    [MenuItem("Tools/Map/Build Season Tiles")]
    public static void Build()
    {
        Dictionary<TileBase, Tile> springTiles = CollectSpringTiles(AssetDatabase.LoadAssetAtPath<SeasonTileSet>(TileSetPath));
        if (springTiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Build Season Tiles", "Không tìm thấy tile mùa Xuân nào trong scene đang mở.", "OK");
            return;
        }

        EnsureFolder(SeasonFolder);
        SeasonTileSet tileSet = AssetDatabase.LoadAssetAtPath<SeasonTileSet>(TileSetPath);
        if (tileSet == null)
        {
            tileSet = ScriptableObject.CreateInstance<SeasonTileSet>();
            AssetDatabase.CreateAsset(tileSet, TileSetPath);
        }
        tileSet.entries.Clear();

        Dictionary<string, Dictionary<Vector2Int, Sprite>> sheetCache = new Dictionary<string, Dictionary<Vector2Int, Sprite>>();
        List<string> missing = new List<string>();

        foreach (Tile spring in springTiles.Values)
        {
            SeasonTileSet.Entry entry = new SeasonTileSet.Entry { spring = spring };
            foreach ((Season season, string word) in OtherSeasons)
            {
                Sprite sprite = SeasonSprite(spring.sprite, word, sheetCache);
                if (sprite == null)
                {
                    missing.Add($"{spring.name} ({word})");
                    continue;
                }

                TileBase variant = CreateVariant(spring, sprite, word);
                if (season == Season.Summer) entry.summer = variant;
                else if (season == Season.Autumn) entry.autumn = variant;
                else entry.winter = variant;
            }
            tileSet.entries.Add(entry);
        }

        EditorUtility.SetDirty(tileSet);
        AssetDatabase.SaveAssets();
        AssignToManager(tileSet);

        string message = $"Đã tạo bảng {tileSet.entries.Count} tile x 4 mùa.";
        if (missing.Count > 0)
        {
            message += $"\nThiếu {missing.Count} tile (giữ nguyên hình mùa Xuân), xem Console.";
            Debug.LogWarning("[SeasonTileBuilder] Không tìm thấy tile mùa tương ứng cho:\n" + string.Join("\n", missing));
        }
        Debug.Log("[SeasonTileBuilder] " + message);
        EditorUtility.DisplayDialog("Build Season Tiles", message, "OK");
    }

    // ---------- Xem trước trong Editor ----------

    [MenuItem("Tools/Map/Preview Season/Spring (Xuân)")] private static void PreviewSpring() => Preview(Season.Spring);
    [MenuItem("Tools/Map/Preview Season/Summer (Hạ)")] private static void PreviewSummer() => Preview(Season.Summer);
    [MenuItem("Tools/Map/Preview Season/Autumn (Thu)")] private static void PreviewAutumn() => Preview(Season.Autumn);
    [MenuItem("Tools/Map/Preview Season/Winter (Đông)")] private static void PreviewWinter() => Preview(Season.Winter);

    private static void Preview(Season season)
    {
        SeasonTileSet tileSet = AssetDatabase.LoadAssetAtPath<SeasonTileSet>(TileSetPath);
        if (tileSet == null)
        {
            EditorUtility.DisplayDialog("Preview Season", "Chưa có bảng tile mùa. Hãy chạy Tools > Map > Build Season Tiles trước.", "OK");
            return;
        }

        Dictionary<TileBase, TileBase> lookup = tileSet.BuildLookup(season);
        Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        Undo.RegisterCompleteObjectUndo(tilemaps, $"Preview Season {season}");

        int changed = 0;
        foreach (Tilemap tilemap in tilemaps)
        {
            changed += SeasonMapManager.ApplyToTilemap(tilemap, lookup);
        }

        // Cây, bụi có SeasonalSprite.
        foreach (SeasonalSprite seasonal in Object.FindObjectsByType<SeasonalSprite>(FindObjectsInactive.Include))
        {
            SpriteRenderer spriteRenderer = seasonal.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) Undo.RecordObject(spriteRenderer, $"Preview Season {season}");
            seasonal.Apply(season);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[SeasonTileBuilder] Xem trước mùa {season}: đổi {changed} ô. Nên chuyển về Spring trước khi lưu scene.");
    }

    // ---------- Quét tile ----------

    // Các tile mùa Xuân dùng trong scene: tile có sprite của tileset Xuân / thân vách CliffMid,
    // hoặc tile mùa khác đã có trong bảng cũ (scene đang để ở chế độ xem trước mùa khác) -> quy về tile Xuân gốc.
    private static Dictionary<TileBase, Tile> CollectSpringTiles(SeasonTileSet previous)
    {
        Dictionary<TileBase, Tile> toSpring = new Dictionary<TileBase, Tile>();
        if (previous != null)
        {
            foreach (SeasonTileSet.Entry entry in previous.entries)
            {
                if (!(entry.spring is Tile springTile)) continue;
                foreach (TileBase variant in new[] { entry.summer, entry.autumn, entry.winter })
                {
                    if (variant != null) toSpring[variant] = springTile;
                }
            }
        }

        Dictionary<TileBase, Tile> result = new Dictionary<TileBase, Tile>();
        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        {
            foreach (TileBase tileBase in tilemap.GetTilesBlock(tilemap.cellBounds))
            {
                if (tileBase == null) continue;

                Tile spring = toSpring.TryGetValue(tileBase, out Tile original) ? original : tileBase as Tile;
                if (spring != null && !result.ContainsKey(spring) && IsSpringTile(spring))
                {
                    result[spring] = spring;
                }
            }
        }
        return result;
    }

    private static bool IsSpringTile(Tile tile)
    {
        if (tile.sprite == null) return false;
        string fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(tile.sprite));
        return fileName.Contains("Spring") || CliffMidColumn(fileName) >= 0;
    }

    // "CliffMid_9" -> 9; các tên khác -> -1.
    private static int CliffMidColumn(string fileName)
    {
        string[] parts = fileName.Split('_');
        return parts.Length == 2 && parts[0] == "CliffMid" && int.TryParse(parts[1], out int column) ? column : -1;
    }

    // ---------- Tìm sprite của mùa khác ----------

    // Cùng ô (cột, hàng tính từ trên xuống) trong tileset của mùa đó - các tileset 4 mùa có cùng bố cục.
    private static Sprite SeasonSprite(Sprite springSprite, string word, Dictionary<string, Dictionary<Vector2Int, Sprite>> cache)
    {
        string springPath = AssetDatabase.GetAssetPath(springSprite);
        string fileName = Path.GetFileNameWithoutExtension(springPath);

        int cliffMid = CliffMidColumn(fileName);
        if (cliffMid >= 0) return MainMapGenerator.CliffMidSprite(cliffMid, word);

        string seasonPath = Path.GetDirectoryName(springPath).Replace('\\', '/') + "/" +
                            Path.GetFileName(springPath).Replace("Spring", word);
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(seasonPath) == null) return null;

        if (!cache.TryGetValue(seasonPath, out Dictionary<Vector2Int, Sprite> sprites))
        {
            EnsureSliced(seasonPath);
            sprites = MainMapGenerator.LoadSprites(seasonPath);
            cache[seasonPath] = sprites;
        }

        Rect rect = springSprite.rect;
        int column = Mathf.RoundToInt(rect.x / 16f);
        int rowFromTop = Mathf.RoundToInt((springSprite.texture.height - rect.y - rect.height) / 16f);
        return sprites.TryGetValue(new Vector2Int(column, rowFromTop), out Sprite sprite) ? sprite : null;
    }

    private static TileBase CreateVariant(Tile spring, Sprite sprite, string word)
    {
        string folder = $"{SeasonFolder}/{word}";
        EnsureFolder(folder);

        string name = spring.name.Contains("Spring") ? spring.name.Replace("Spring", word) : spring.name;
        string path = $"{folder}/{name}.asset";

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = spring.colliderType;
        tile.color = spring.color;
        tile.flags = spring.flags;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    // ---------- Cắt sprite 16x16 cho tileset chưa cắt ----------

    private static void EnsureSliced(string texturePath)
    {
        if (MainMapGenerator.LoadSprites(texturePath).Count > 1) return;

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        // Đọc pixel từ file gốc để bỏ qua các ô trong suốt hoàn toàn.
        Texture2D pixels = new Texture2D(2, 2);
        pixels.LoadImage(File.ReadAllBytes(texturePath));
        int width = pixels.width, height = pixels.height;
        string baseName = Path.GetFileNameWithoutExtension(texturePath);

        List<SpriteRect> rects = new List<SpriteRect>();
        for (int rowFromTop = 0; rowFromTop < height / 16; rowFromTop++)
        {
            for (int column = 0; column < width / 16; column++)
            {
                int x = column * 16, y = height - (rowFromTop + 1) * 16;
                if (pixels.GetPixels(x, y, 16, 16).All(c => c.a < 0.01f)) continue;

                rects.Add(new SpriteRect
                {
                    name = $"{baseName}_{rects.Count}",
                    rect = new Rect(x, y, 16, 16),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                });
            }
        }
        Object.DestroyImmediate(pixels);

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());

        ISpriteNameFileIdDataProvider nameFileIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIds?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"[SeasonTileBuilder] Đã cắt {rects.Count} ô 16x16 cho {baseName}.");
    }

    // ---------- Tiện ích ----------

    private static void AssignToManager(SeasonTileSet tileSet)
    {
        SeasonMapManager manager = Object.FindAnyObjectByType<SeasonMapManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.Log("[SeasonTileBuilder] Chưa có SeasonMapManager trong scene - thêm component này rồi gán SeasonTileSet.");
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("tileSet").objectReferenceValue = tileSet;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
