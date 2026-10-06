using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Cây và bụi theo mùa: tạo sprite từng mùa rồi gắn SeasonalSprite vào các prefab trong Assets/Prefabs/Tree.
// - Cây Maple / Mahogany / Birch / Pine (lớn và non): cắt từng cây (ô 32x48) từ sheet tĩnh "Tree/Common/Shadow/<Loại> Tree.png",
//   căn gốc cây (đáy bóng dưới chân) trùng vị trí cũ để không lệch collider. Các prefab đang dùng sprite dải 4 khung
//   (Pine / Birch / Mahogany Tree 2) cũng được đưa về 1 cây.
// - Bụi (Bush, Bush 1): Xuân xanh lá, Hạ xanh đậm, Thu cam / đỏ, Đông xanh đậm đốm trắng.
// - ForestTree (cây Deep Forest): xanh lá / xanh ngọc / hồng / xanh băng.
public static class SeasonObjectBuilder
{
    private const string TreePrefabFolder = "Assets/Prefabs/Tree";
    private const string OutputFolder = MainMapGenerator.GeneratedFolder + "/Seasons/Trees";
    private const string TreeSheetFolder = MainMapGenerator.AssetPackFolder + "Objects/Tree/Common/Shadow/";
    private const string DeepForestFolder = MainMapGenerator.AssetPackFolder + "Objects/Tree/Deep Forest/";

    private const int CellWidth = 32, CellHeight = 48;
    private static readonly string[] SeasonNames = { "Spring", "Summer", "Autumn", "Winter" };

    // Ô (x, hàng trên cùng tính từ trên xuống) của từng mùa Xuân / Hạ / Thu / Đông trong sheet tĩnh.
    private static readonly Dictionary<(string type, bool big), Vector2Int[]> TreeCells = new Dictionary<(string, bool), Vector2Int[]>
    {
        { ("Maple", true),     new[] { new Vector2Int(0, 48),  new Vector2Int(128, 48), new Vector2Int(64, 48),  new Vector2Int(96, 48) } },
        { ("Maple", false),    new[] { new Vector2Int(64, 0),  new Vector2Int(192, 0),  new Vector2Int(128, 0),  new Vector2Int(160, 0) } },
        { ("Mahogany", true),  new[] { new Vector2Int(96, 0),  new Vector2Int(192, 0),  new Vector2Int(128, 0),  new Vector2Int(160, 0) } },
        { ("Mahogany", false), new[] { new Vector2Int(64, 0),  new Vector2Int(64, 0),   new Vector2Int(128, 48), new Vector2Int(64, 0) } },
        { ("Birch", true),     new[] { new Vector2Int(96, 0),  new Vector2Int(96, 0),   new Vector2Int(128, 0),  new Vector2Int(160, 0) } },
        { ("Birch", false),    new[] { new Vector2Int(64, 0),  new Vector2Int(64, 0),   new Vector2Int(96, 48),  new Vector2Int(64, 0) } },
        { ("Pine", true),      new[] { new Vector2Int(96, 0),  new Vector2Int(96, 0),   new Vector2Int(96, 0),   new Vector2Int(128, 0) } },
        { ("Pine", false),     new[] { new Vector2Int(64, 0),  new Vector2Int(64, 0),   new Vector2Int(64, 0),   new Vector2Int(64, 0) } },
    };

    // Bụi (tên sprite trong bushes.png). Các danh sách dài bằng nhau để mỗi bụi giữ biến thể của mình qua các mùa.
    private static readonly string[][] BushSprites =
    {
        new[] { "bushes_20", "bushes_19", "bushes_18", "bushes_17", "bushes_16", "bushes_15", "bushes_14", "bushes_13", "bushes_1" },
        new[] { "bushes_12", "bushes_7", "bushes_8", "bushes_6", "bushes_0", "bushes_12", "bushes_7", "bushes_8", "bushes_6" },
        new[] { "bushes_2", "bushes_3", "bushes_2", "bushes_3", "bushes_2", "bushes_3", "bushes_2", "bushes_3", "bushes_2" },
        new[] { "bushes_10", "bushes_9", "bushes_5", "bushes_4", "bushes_10", "bushes_9", "bushes_5", "bushes_4", "bushes_10" },
    };

    // Cây Deep Forest (tên sprite trong Tree.png): xanh lá, xanh ngọc, hồng, xanh băng.
    private static readonly string[] ForestTreeSprites = { "Tree_2", "Tree_3", "Tree_9", "Tree_12" };

    [MenuItem("Tools/Map/Build Season Trees & Bushes")]
    public static void Build()
    {
        EnsureFolder(OutputFolder);
        List<string> report = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { TreePrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SpriteRenderer renderer = MainRenderer(root);
                if (renderer == null || renderer.sprite == null) continue;

                Sprite[][] seasons = root.name.StartsWith("Bush") ? BushSeasons()
                    : root.name == "ForestTree" ? ForestTreeSeasons()
                    : TreeSeasons(root.name, renderer.sprite);
                if (seasons == null)
                {
                    report.Add($"- {root.name}: bỏ qua (không nhận ra loại cây)");
                    continue;
                }

                SeasonalSprite seasonal = renderer.GetComponent<SeasonalSprite>();
                if (seasonal == null) seasonal = renderer.gameObject.AddComponent<SeasonalSprite>();
                seasonal.SetSprites(renderer, seasons[0], seasons[1], seasons[2], seasons[3]);

                // Cây: đặt luôn sprite Xuân làm sprite mặc định (sửa các prefab đang dùng dải 4 khung).
                if (!root.name.StartsWith("Bush")) renderer.sprite = seasons[0][0];

                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.Add($"- {root.name}: OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        string message = "Đã gắn SeasonalSprite cho các prefab:\n" + string.Join("\n", report);
        Debug.Log("[SeasonObjectBuilder] " + message);
        EditorUtility.DisplayDialog("Build Season Trees & Bushes", message, "OK");
    }

    // ---------- Cây ----------

    private static Sprite[][] TreeSeasons(string prefabName, Sprite current)
    {
        string sheetName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(current));
        string type = new[] { "Maple", "Mahogany", "Birch", "Pine" }.FirstOrDefault(t => sheetName.StartsWith(t));
        if (type == null) return null;

        Texture2D sheet = LoadPixels(TreeSheetFolder + $"{type} Tree.png");
        Texture2D currentPixels = LoadPixels(AssetDatabase.GetAssetPath(current));

        // Vị trí gốc cây so với pivot của sprite hiện tại (đơn vị pixel). Sprite dải 4 khung: chỉ xét khung đầu,
        // và đặt gốc thẳng dưới pivot (prefab vốn thiết kế cho 1 cây ở giữa).
        RectInt currentRect = ToRectInt(current.rect);
        bool strip = currentRect.width > CellWidth * 2;
        RectInt anchorArea = strip ? new RectInt(currentRect.x, currentRect.y, CellWidth, currentRect.height) : currentRect;

        // Cây lớn cao ~46px, cây non ~34px (đo phần có hình, nên chạy lại tool nhiều lần vẫn nhận đúng).
        bool big = OpaqueHeight(currentPixels, anchorArea) >= 42;
        Vector2Int[] cells = TreeCells[(type, big)];
        Vector2 anchor = FindAnchor(currentPixels, anchorArea) + new Vector2(anchorArea.x - currentRect.x, 0f);
        Vector2 offset = anchor - current.pivot;
        if (strip) offset.x = 0f;

        Sprite[][] result = new Sprite[4][];
        for (int season = 0; season < 4; season++)
        {
            Vector2Int cell = cells[season];
            RectInt crop = new RectInt(cell.x, sheet.height - cell.y - CellHeight, CellWidth, CellHeight);
            Vector2 cropAnchor = FindAnchor(sheet, crop);
            Vector2 pivot = cropAnchor - offset;

            string name = $"{Sanitize(prefabName)}_{SeasonNames[season]}";
            result[season] = new[] { CreateSprite(name, sheet, crop, new Vector2(pivot.x / CellWidth, pivot.y / CellHeight)) };
        }

        Object.DestroyImmediate(sheet);
        Object.DestroyImmediate(currentPixels);
        return result;
    }

    // Gốc cây = giữa hàng pixel thấp nhất (đáy bóng dưới chân cây), toạ độ tính từ góc dưới-trái của vùng.
    private static Vector2 FindAnchor(Texture2D texture, RectInt area)
    {
        Color[] pixels = texture.GetPixels(area.x, area.y, area.width, area.height);
        for (int y = 0; y < area.height; y++)
        {
            int count = 0, sum = 0;
            for (int x = 0; x < area.width; x++)
            {
                if (pixels[y * area.width + x].a > 0.15f)
                {
                    count++;
                    sum += x;
                }
            }
            if (count > 0) return new Vector2(sum / (float)count + 0.5f, y);
        }
        return new Vector2(area.width / 2f, 0f);
    }

    private static int OpaqueHeight(Texture2D texture, RectInt area)
    {
        Color[] pixels = texture.GetPixels(area.x, area.y, area.width, area.height);
        int bottom = -1, top = -1;
        for (int y = 0; y < area.height; y++)
        {
            for (int x = 0; x < area.width; x++)
            {
                if (pixels[y * area.width + x].a <= 0.15f) continue;
                if (bottom < 0) bottom = y;
                top = y;
                break;
            }
        }
        return bottom < 0 ? 0 : top - bottom + 1;
    }

    // Cắt một vùng của sheet ra PNG riêng với pivot tuỳ chỉnh (mỗi lần build ghi đè để cập nhật pivot).
    private static Sprite CreateSprite(string name, Texture2D sheet, RectInt crop, Vector2 pivot)
    {
        string path = $"{OutputFolder}/{name}.png";
        Texture2D texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
        texture.SetPixels(sheet.GetPixels(crop.x, crop.y, crop.width, crop.height));
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Bụi và cây Deep Forest ----------

    private static Sprite[][] BushSeasons()
    {
        Dictionary<string, Sprite> sprites = SpritesByName(DeepForestFolder + "bushes.png");
        return BushSprites.Select(names => names.Select(n => sprites.TryGetValue(n, out Sprite s) ? s : null).ToArray()).ToArray();
    }

    private static Sprite[][] ForestTreeSeasons()
    {
        Dictionary<string, Sprite> sprites = SpritesByName(DeepForestFolder + "Tree.png");
        return ForestTreeSprites.Select(n => new[] { sprites.TryGetValue(n, out Sprite s) ? s : null }).ToArray();
    }

    // ---------- Tiện ích ----------

    // SpriteRenderer chính của prefab: cái nằm cùng object với Tree / TreeFade, không phải bóng (Shandow).
    private static SpriteRenderer MainRenderer(GameObject root)
    {
        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        return renderers.FirstOrDefault(r => r.GetComponent<TreeFade>() != null || r.GetComponent<Tree>() != null)
               ?? renderers.FirstOrDefault(r => !r.name.StartsWith("Shandow"));
    }

    private static Dictionary<string, Sprite> SpritesByName(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().GroupBy(s => s.name).ToDictionary(g => g.Key, g => g.First());
    }

    private static Texture2D LoadPixels(string path)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(path));
        return texture;
    }

    private static RectInt ToRectInt(Rect rect)
    {
        return new RectInt(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height));
    }

    private static string Sanitize(string name) => name.Trim().Replace(' ', '_');

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
