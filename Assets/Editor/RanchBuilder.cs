using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

// Dựng chức năng chăn nuôi bằng một menu: Tools > Map > Build Ranch (mở scene Main trước).
// 1. Cắt sprite các sheet con vật đã chuẩn hoá trong Assets/Tiles/Generated/Ranch (5 hàng x 4 khung), chuồng, máng, người bán.
// 2. Tạo / cập nhật các loài (Assets/Data/Ranch) và vật phẩm con giống bán ở cửa hàng (Assets/Data/InventoryItem/Animal/Ranch).
// 3. Tạo prefab con vật nuôi, thú hoang, chuồng gà, chuồng bò, máng ăn, người bán (Assets/Prefabs/Ranch).
// 4. Đặt trang trại vào khoảng đất trống phía tây nam nhà (đã quét layout: cùng tầng, chỉ cỏ / đất), dọn decor đè lên,
//    thêm RanchManager và WildAnimalSpawner. Chạy lại bao nhiêu lần cũng được: trang trại cũ trong scene được thay mới.
// Gốc tên "Ranch_Exterior" để MainMapDecorator tự chừa trống khu này khi sinh lại decor.
public static class RanchBuilder
{
    private const string ArtFolder = MainMapGenerator.GeneratedFolder + "/Ranch";
    private const string AnimalFolder = ArtFolder + "/Animals";
    private const string SpeciesFolder = "Assets/Data/Ranch";
    private const string ItemFolder = "Assets/Data/InventoryItem/Animal/Ranch";
    private const string PrefabFolder = "Assets/Prefabs/Ranch";
    private const string FenceSheet = MainMapGenerator.AssetPackFolder + "Objects/Exterior/Fence and Bridge/Fence Wood.png";
    private const string PlateSheet = MainMapGenerator.AssetPackFolder + "Objects/Exterior/animal plates.png";   // đã cắt sẵn: _0 gà, _1 bò
    private const string ShopSensorPrefab = "Assets/Prefabs/Shop/ShopSensor.prefab";
    private const string RootName = "Ranch_Exterior";
    private const int InteractableLayer = 8;

    // Bố cục (toạ độ ô thế giới). Vùng x -38..-11, y -32..-20 cùng tầng 0, chỉ có cỏ / đất, không có vật đặt tay.
    // Hai bãi cách nhau lối đi rộng 4 ô (x -27..-24), cổng mỗi bãi mở ra lối đi này.
    private static readonly Rect ClearArea = new Rect(-39f, -33f, 30f, 15f);
    private static readonly RectInt CoopPen = new RectInt(-37, -31, 10, 7);   // hàng rào x -37..-28, y -31..-25
    private static readonly RectInt BarnPen = new RectInt(-23, -31, 10, 7);   // hàng rào x -23..-14
    private static readonly Vector2 CoopBase = new Vector2(-32.5f, -25f);
    private static readonly Vector2 BarnBase = new Vector2(-18.5f, -25f);
    private static readonly int[] GateRows = { -28, -27 };

#pragma warning disable 0649   // các trường dưới đây do JsonUtility gán khi đọc manifest.json
    [System.Serializable]
    private class Manifest { public SheetInfo[] sheets; }

    [System.Serializable]
    private class SheetInfo { public string name; public int cell; public float pivotY; }
#pragma warning restore 0649

    private static Dictionary<string, ItemData> items;

    [MenuItem("Tools/Map/Build Ranch")]
    public static void Build()
    {
        if (Object.FindObjectsByType<SoilManager>(FindObjectsInactive.Include).Length == 0)
        {
            EditorUtility.DisplayDialog("Build Ranch", "Hãy mở scene Main trước khi chạy.", "OK");
            return;
        }

        AssetDatabase.Refresh();
        EnsureFolder(SpeciesFolder);
        EnsureFolder(ItemFolder);
        EnsureFolder(PrefabFolder);

        // 1. Sprite
        Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(AnimalFolder + "/manifest.json"));
        foreach (SheetInfo sheet in manifest.sheets)
        {
            ImportGrid($"{AnimalFolder}/{sheet.name}.png", sheet.cell, sheet.cell, 4, 5, new Vector2(0.5f, sheet.pivotY));
        }
        ImportGrid($"{ArtFolder}/Props/RanchKeeper_Idle.png", 32, 32, 16, 1, new Vector2(0.5f, 0f));
        Sprite coop = ImportSingle($"{ArtFolder}/Buildings/Coop.png", SpriteAlignment.BottomCenter);
        Sprite coopWinter = ImportSingle($"{ArtFolder}/Buildings/Coop_Winter.png", SpriteAlignment.BottomCenter);
        Sprite barn = ImportSingle($"{ArtFolder}/Buildings/Barn.png", SpriteAlignment.BottomCenter);
        Sprite barnWinter = ImportSingle($"{ArtFolder}/Buildings/Barn_Winter.png", SpriteAlignment.BottomCenter);
        Sprite troughFull = ImportSingle($"{ArtFolder}/Props/Trough_Full.png", SpriteAlignment.Center);
        Sprite troughEmpty = ImportSingle($"{ArtFolder}/Props/Trough_Empty.png", SpriteAlignment.Center);
        Sprite troughWater = ImportSingle($"{ArtFolder}/Props/Trough_Water.png", SpriteAlignment.Center);
        Sprite hayBale = ImportSingle($"{ArtFolder}/Props/HayBale.png", SpriteAlignment.BottomCenter);

        // 2. Dữ liệu
        items = AssetDatabase.FindAssets("t:ItemData")
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && !string.IsNullOrEmpty(i.ItemID))
            .GroupBy(i => i.ItemID).ToDictionary(g => g.Key, g => g.First());

        AnimalSpecies chicken = Species("Chicken", "chicken", "Gà", AnimalHome.Coop, 0.9f, 0.15f, 6f, "chicken_egg", "large_chicken_egg", 1, 3,
            V("Trắng", "Chicken_White"), V("Nâu", "Chicken_Brown", "brown_chicken_egg", "large_brown_chicken_egg"));
        AnimalSpecies duck = Species("Duck", "duck", "Vịt", AnimalHome.Coop, 0.8f, 0.15f, 6f, "duck_egg", null, 2, 4, V("Trắng", "Duck_White"));
        AnimalSpecies rabbit = Species("Rabbit", "rabbit", "Thỏ", AnimalHome.Coop, 1.3f, 0.15f, 8f, "white_rabbit_fur", null, 3, 0,
            V("Trắng", "Rabbit_White", "white_rabbit_fur"), V("Đen", "Rabbit_Black", "black_rabbit_fur"), V("Hồng", "Rabbit_Pink", "pink_rabbit_fur"));
        AnimalSpecies cow = Species("Cow", "cow", "Bò", AnimalHome.Barn, 0.7f, 0.35f, 5f, "small_cow_milk", "large_cow_milk", 1, 5,
            V("Nâu", "Cow_Brown"), V("Đen", "Cow_Black"));
        AnimalSpecies goat = Species("Goat", "goat", "Dê", AnimalHome.Barn, 0.8f, 0.3f, 5f, "small_goat_milk", "large_goat_milk", 2, 5, V("Vàng", "Goat_Blonde"));
        AnimalSpecies sheep = Species("Sheep", "sheep", "Cừu", AnimalHome.Barn, 0.7f, 0.32f, 5f, "wool", null, 3, 4, V("Trắng", "Sheep_White"));
        AnimalSpecies deer = Species("Deer", "deer", "Hươu", AnimalHome.WildOnly, 1.4f, 0.3f, 6f, null, null, 1, 0, V("Đực", "Deer_Male"));
        AnimalSpecies fox = Species("Fox", "fox", "Cáo", AnimalHome.WildOnly, 1.5f, 0.28f, 7f, null, null, 1, 0, V("Đỏ", "Fox_Red"));

        // Thức ăn (thứ tự = ưu tiên khi ấn J). Thức ăn gia súc bán ở người bán trang trại, loài nào cũng ăn được.
        chicken.foods = Foods("rice", "wheat", "corn", "cabbage", "animal_feed");
        duck.foods = Foods("rice", "corn", "cabbage", "wheat", "animal_feed");
        rabbit.foods = Foods("carrot", "cabbage", "animal_feed");
        cow.foods = Foods("carrot", "cabbage", "wheat", "corn", "animal_feed");
        goat.foods = Foods("carrot", "cabbage", "wheat", "animal_feed");
        sheep.foods = Foods("cabbage", "carrot", "wheat", "animal_feed");

        rabbit.tameFood = Item("carrot");
        rabbit.tameFeedsNeeded = 3;
        rabbit.fleeDistance = 3.5f;
        deer.fleeDistance = 5f;
        fox.fleeDistance = 5f;

        chicken.purchaseItem = PurchaseItem("animal_chicken", "Gà Con (Chick)", chicken, 800, "Cầm trên tay rồi chuột phải vào chuồng gà để thả. Ăn thóc, lúa mì, ngô, bắp cải. Lớn sau 3 ngày được ăn, ăn xong thì đẻ trứng.");
        duck.purchaseItem = PurchaseItem("animal_duck", "Vịt Con (Duckling)", duck, 1200, "Thả vào chuồng gà. Ăn thóc, ngô, bắp cải. Lớn sau 4 ngày được ăn, cứ 2 bữa đẻ 1 trứng vịt.");
        cow.purchaseItem = PurchaseItem("animal_cow", "Bê Con (Calf)", cow, 1500, "Thả vào chuồng bò. Thích ăn cà rốt. Lớn sau 5 ngày được ăn, ăn xong thì cho sữa; thân thiết thì cho sữa lớn.");
        goat.purchaseItem = PurchaseItem("animal_goat", "Dê Con (Kid Goat)", goat, 2000, "Thả vào chuồng bò. Ăn cà rốt, bắp cải. Lớn sau 5 ngày được ăn, cứ 2 bữa cho 1 sữa dê.");
        sheep.purchaseItem = PurchaseItem("animal_sheep", "Cừu Con (Lamb)", sheep, 2500, "Thả vào chuồng bò. Ăn bắp cải, cà rốt. Lớn sau 4 ngày được ăn, cứ 3 bữa cho 1 len.");
        foreach (AnimalSpecies s in new[] { chicken, duck, rabbit, cow, goat, sheep, deer, fox }) EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();

        // 3. Prefab
        FarmAnimal farmPrefab = BuildAnimalPrefab<FarmAnimal>("FarmAnimal");
        WildAnimal wildPrefab = BuildAnimalPrefab<WildAnimal>("WildAnimal");
        GameObject coopPrefab = BuildHousePrefab("Coop", coop, coopWinter, "Coop", "Chuồng gà", AnimalHome.Coop, 8, new Vector2(3.1f, 1.9f));
        GameObject barnPrefab = BuildHousePrefab("Barn", barn, barnWinter, "Barn", "Chuồng bò", AnimalHome.Barn, 6, new Vector2(4.4f, 2.0f));
        GameObject troughPrefab = BuildTroughPrefab(troughFull, troughEmpty);
        GameObject keeperPrefab = BuildKeeperPrefab();

        // 4. Scene
        int removed = PlaceInScene(coopPrefab, barnPrefab, troughPrefab, keeperPrefab, troughWater, hayBale, farmPrefab, wildPrefab,
            new[] { chicken, duck, rabbit, cow, goat, sheep, deer, fox }, chicken, duck, cow, goat, sheep, rabbit, deer, fox);

        RefreshItemDatabase();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        string missing = string.Join(", ", new[] { "chicken_egg", "duck_egg", "small_cow_milk", "small_goat_milk", "wool", "white_rabbit_fur", "carrot", "animal_feed", "wheat", "rice", "corn", "cabbage" }.Where(id => Item(id) == null));
        string message = "Đã dựng trang trại: chuồng gà, chuồng bò, 2 bãi quây, máng ăn, bảng tên, người bán gia súc, 8 loài con vật."
                         + "\nTrong game: J = cho cả trang trại ăn, K = thu hoạch tất cả, chuột phải con vật = thu / cho ăn / vuốt ve."
                         + $"\nĐã dọn {removed} cây / decor đè lên khu trang trại."
                         + (missing.Length > 0 ? $"\nThiếu vật phẩm: {missing}" : "")
                         + "\n\nNhớ Ctrl+S để lưu scene.";
        Debug.Log("[RanchBuilder] " + message);
        EditorUtility.DisplayDialog("Build Ranch", message, "OK");
    }

    // ---------------------------------------------------------------- sprite

    private static void ImportGrid(string path, int cellW, int cellH, int columns, int rows, Vector2 pivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[RanchBuilder] Không tìm thấy {path}");
            return;
        }
        Configure(importer, SpriteImportMode.Multiple);

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        // Giữ spriteID cũ theo tên để chạy lại tool không làm hỏng tham chiếu.
        Dictionary<string, GUID> oldIds = provider.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        string baseName = Path.GetFileNameWithoutExtension(path);
        int height = rows * cellH;
        List<SpriteRect> rects = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                string name = $"{baseName}_{r}_{c}";
                rects.Add(new SpriteRect
                {
                    name = name,
                    rect = new Rect(c * cellW, height - (r + 1) * cellH, cellW, cellH),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = oldIds.TryGetValue(name, out GUID id) ? id : GUID.Generate()
                });
            }
        }
        provider.SetSpriteRects(rects.ToArray());
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite ImportSingle(string path, SpriteAlignment alignment)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[RanchBuilder] Không tìm thấy {path}");
            return null;
        }
        Configure(importer, SpriteImportMode.Single);
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)alignment;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Configure(TextureImporter importer, SpriteImportMode mode)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    private static Sprite[] Frames(string sheetName)
    {
        string path = $"{AnimalFolder}/{sheetName}.png";
        Dictionary<string, Sprite> byName = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        Sprite[] frames = new Sprite[AnimalSpecies.FrameCount];
        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                byName.TryGetValue($"{sheetName}_{r}_{c}", out frames[r * 4 + c]);
            }
        }
        return frames;
    }

    // ---------------------------------------------------------------- dữ liệu

    private struct VariantSpec
    {
        public string name, sheet, product, largeProduct;
    }

    private static VariantSpec V(string name, string sheet, string product = null, string largeProduct = null)
    {
        return new VariantSpec { name = name, sheet = sheet, product = product, largeProduct = largeProduct };
    }

    private static ItemData Item(string id) => id != null && items.TryGetValue(id, out ItemData item) ? item : null;

    private static ItemData[] Foods(params string[] ids) => ids.Select(Item).Where(i => i != null).ToArray();

    private static AnimalSpecies Species(string file, string id, string displayName, AnimalHome home, float speed, float radius, float fps,
        string product, string largeProduct, int everyDays, int daysToAdult, params VariantSpec[] variants)
    {
        string path = $"{SpeciesFolder}/{file}.asset";
        AnimalSpecies species = AssetDatabase.LoadAssetAtPath<AnimalSpecies>(path);
        if (species == null)
        {
            species = ScriptableObject.CreateInstance<AnimalSpecies>();
            AssetDatabase.CreateAsset(species, path);
        }

        species.id = id;
        species.displayName = displayName;
        species.home = home;
        species.moveSpeed = speed;
        species.colliderRadius = radius;
        species.framesPerSecond = fps;
        species.product = Item(product);
        species.largeProduct = Item(largeProduct);
        species.productEveryDays = everyDays;
        species.daysToAdult = daysToAdult;
        species.variants = variants.Select(v => new AnimalSpecies.Variant
        {
            name = v.name,
            frames = Frames(v.sheet),
            babyFrames = File.Exists($"{AnimalFolder}/{v.sheet}_Baby.png") ? Frames(v.sheet + "_Baby") : new Sprite[0],
            product = Item(v.product),
            largeProduct = Item(v.largeProduct)
        }).ToArray();
        return species;
    }

    // Vật phẩm con giống (mua ở cửa hàng trang trại), icon là khung đầu của con non.
    private static ItemData PurchaseItem(string id, string displayName, AnimalSpecies species, int price, string description)
    {
        string path = $"{ItemFolder}/{id}.asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        Sprite[] icons = species.FramesFor(0, false);
        SerializedObject so = new SerializedObject(item);
        so.FindProperty("itemID").stringValue = id;
        so.FindProperty("itemName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("icon").objectReferenceValue = icons != null && icons.Length > 0 ? icons[0] : null;
        so.FindProperty("itemType").enumValueIndex = (int)ItemType.AnimalProduct;
        so.FindProperty("maxStack").intValue = 10;
        so.FindProperty("buyPrice").intValue = price;
        so.FindProperty("sellPrice").intValue = 0;
        so.FindProperty("canSell").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        items[id] = item;
        return item;
    }

    private static void RefreshItemDatabase()
    {
        foreach (ItemDatabase database in Object.FindObjectsByType<ItemDatabase>(FindObjectsInactive.Include))
        {
            database.GetItemData();
            EditorUtility.SetDirty(database);
        }
    }

    // ---------------------------------------------------------------- prefab

    private static T BuildAnimalPrefab<T>(string name) where T : Component
    {
        GameObject go = new GameObject(name) { layer = InteractableLayer };
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 0;

        // Kinematic + collider trigger: người chơi đi xuyên qua, không đẩy được; AnimalMotor tự dò vật cản.
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D bodyCollider = go.AddComponent<CircleCollider2D>();
        bodyCollider.isTrigger = true;
        bodyCollider.radius = 0.25f;
        bodyCollider.offset = new Vector2(0f, 0.25f);
        BoxCollider2D clickArea = go.AddComponent<BoxCollider2D>();
        clickArea.isTrigger = true;

        go.AddComponent<AnimalMotor>();
        T component = go.AddComponent<T>();

        GameObject bubbleGo = new GameObject("Bubble");
        bubbleGo.transform.SetParent(go.transform, false);
        bubbleGo.transform.localPosition = new Vector3(0f, 1f, 0f);
        SpriteRenderer background = new GameObject("Background").AddComponent<SpriteRenderer>();
        background.transform.SetParent(bubbleGo.transform, false);
        background.sortingOrder = 300;
        SpriteRenderer icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
        icon.transform.SetParent(bubbleGo.transform, false);
        icon.sortingOrder = 301;
        AnimalBubble bubble = bubbleGo.AddComponent<AnimalBubble>();
        SetRefs(bubble, ("background", background), ("icon", icon));

        SetRefs(component, ("bubble", bubble), ("bodyCollider", bodyCollider), ("clickArea", clickArea));
        return SavePrefab(go, name).GetComponent<T>();
    }

    private static GameObject BuildHousePrefab(string name, Sprite normal, Sprite winter, string houseId, string displayName, AnimalHome home, int capacity, Vector2 footprint)
    {
        // Gốc ở layer Interactable để chuột phải được; hình nằm ở object con layer Default vì camera minimap /
        // bản đồ lớn không render layer Interactable (nếu để hình ở gốc thì chuồng không hiện trên minimap).
        GameObject go = new GameObject(name) { layer = InteractableLayer };
        GameObject visual = new GameObject("Sprite");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = normal;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;

        BoxCollider2D solid = go.AddComponent<BoxCollider2D>();
        solid.size = footprint;
        solid.offset = new Vector2(0f, footprint.y / 2f);
        BoxCollider2D click = go.AddComponent<BoxCollider2D>();
        click.isTrigger = true;
        click.size = normal.bounds.size;
        click.offset = new Vector2(0f, normal.bounds.size.y / 2f);

        visual.AddComponent<SeasonalSprite>().SetSprites(renderer, new[] { normal }, new[] { normal }, new[] { normal }, new[] { winter != null ? winter : normal });

        AnimalHouse house = go.AddComponent<AnimalHouse>();
        SerializedObject so = new SerializedObject(house);
        so.FindProperty("houseId").stringValue = houseId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("home").enumValueIndex = (int)home;
        so.FindProperty("capacity").intValue = capacity;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(go, name);
    }

    private static GameObject BuildTroughPrefab(Sprite full, Sprite empty)
    {
        GameObject go = new GameObject("FeedTrough") { layer = InteractableLayer };
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = empty;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(empty.bounds.size.x * 0.9f, 0.6f);
        box.offset = new Vector2(0f, -0.1f);

        FeedTrough trough = go.AddComponent<FeedTrough>();
        SerializedObject so = new SerializedObject(trough);
        so.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        so.FindProperty("fullSprite").objectReferenceValue = full;
        so.FindProperty("emptySprite").objectReferenceValue = empty;
        so.ApplyModifiedPropertiesWithoutUndo();
        return SavePrefab(go, "FeedTrough");
    }

    // Người bán gia súc: ghép từ NPC Blacksmith, đứng ở sân cạnh chuồng gà, ấn F để mua (dùng ShopSensor có sẵn).
    private static GameObject BuildKeeperPrefab()
    {
        Sprite[] idle = AssetDatabase.LoadAllAssetsAtPath($"{ArtFolder}/Props/RanchKeeper_Idle.png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).Take(4).ToArray();

        GameObject go = new GameObject("RanchKeeper");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = idle.FirstOrDefault();
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        SpriteFrameLoop loop = go.AddComponent<SpriteFrameLoop>();
        SerializedObject so = new SerializedObject(loop);
        SerializedProperty frames = so.FindProperty("frames");
        frames.arraySize = idle.Length;
        for (int i = 0; i < idle.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = idle[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        BoxCollider2D feet = go.AddComponent<BoxCollider2D>();
        feet.size = new Vector2(0.6f, 0.3f);
        feet.offset = new Vector2(0f, 0.15f);
        return SavePrefab(go, "RanchKeeper");
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void SetRefs(Object target, params (string field, Object value)[] refs)
    {
        SerializedObject so = new SerializedObject(target);
        foreach ((string field, Object value) in refs) so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- scene

    private static int PlaceInScene(GameObject coopPrefab, GameObject barnPrefab, GameObject troughPrefab, GameObject keeperPrefab,
        Sprite troughWater, Sprite hayBale, FarmAnimal farmPrefab, WildAnimal wildPrefab, AnimalSpecies[] allSpecies,
        AnimalSpecies chicken, AnimalSpecies duck, AnimalSpecies cow, AnimalSpecies goat, AnimalSpecies sheep,
        AnimalSpecies rabbit, AnimalSpecies deer, AnimalSpecies fox)
    {
        foreach (string old in new[] { RootName, "RanchManager", "WildAnimalSpawner" })
        {
            GameObject existing = GameObject.Find(old);
            if (existing != null) Undo.DestroyObjectImmediate(existing);
        }
        int removed = ClearDecor();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Ranch");

        AnimalHouse coopHouse = Instance(coopPrefab, root.transform, CoopBase).GetComponent<AnimalHouse>();
        AnimalHouse barnHouse = Instance(barnPrefab, root.transform, BarnBase).GetComponent<AnimalHouse>();
        FeedTrough coopTrough = Trough(troughPrefab, root.transform, new Vector2(-35.2f, -25.6f), "Coop", coopHouse);
        FeedTrough barnTrough = Trough(troughPrefab, root.transform, new Vector2(-15.2f, -25.6f), "Barn", barnHouse);
        ConfigureHouse(coopHouse, coopTrough, CoopPen);
        ConfigureHouse(barnHouse, barnTrough, BarnPen);

        Transform fences = new GameObject("Fences").transform;
        fences.SetParent(root.transform, false);
        Dictionary<Vector2Int, Sprite> fenceSprites = MainMapGenerator.LoadSprites(FenceSheet);
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        AddPenFences(cells, CoopPen, CoopBase.x, coopHouse.GetComponentInChildren<SpriteRenderer>().bounds.size.x, true);
        AddPenFences(cells, BarnPen, BarnBase.x, barnHouse.GetComponentInChildren<SpriteRenderer>().bounds.size.x, false);
        foreach (Vector2Int cell in cells) CreateFence(cell, cells, fenceSprites, fences);

        Decor("WaterTrough", troughWater, new Vector2(-29.2f, -25.6f), root.transform, true);
        Decor("HayBale", hayBale, new Vector2(-36.0f, -23.6f), root.transform, true);
        Decor("HayBale", hayBale, new Vector2(-14.8f, -23.6f), root.transform, true);
        Decor("HayBale", hayBale, new Vector2(-14.6f, -22.7f), root.transform, false);

        // Bảng tên con vật treo giữa hàng rào phía trước mỗi bãi.
        Sprite[] plates = AssetDatabase.LoadAllAssetsAtPath(PlateSheet).OfType<Sprite>().ToArray();
        Decor("Plate_Chicken", plates.FirstOrDefault(p => p.name == "animal plates_0"), new Vector2(CoopPen.center.x, CoopPen.yMin + 0.45f), root.transform, false);
        Decor("Plate_Cow", plates.FirstOrDefault(p => p.name == "animal plates_1"), new Vector2(BarnPen.center.x, BarnPen.yMin + 0.45f), root.transform, false);

        GameObject keeper = Instance(keeperPrefab, root.transform, new Vector2(-28.6f, -23.2f));
        AddShop(keeper);

        GameObject managerGo = new GameObject("RanchManager");
        Undo.RegisterCreatedObjectUndo(managerGo, "Build Ranch");
        RanchManager manager = managerGo.AddComponent<RanchManager>();
        SerializedObject ms = new SerializedObject(manager);
        SetList(ms.FindProperty("species"), allSpecies);
        SetList(ms.FindProperty("houses"), new Object[] { coopHouse, barnHouse });
        ms.FindProperty("animalPrefab").objectReferenceValue = farmPrefab;
        SerializedProperty starters = ms.FindProperty("starterAnimals");
        (AnimalSpecies species, int variant, string house, bool adult)[] starterList =
        {
            (chicken, 0, "Coop", true), (chicken, 0, "Coop", true), (chicken, 1, "Coop", true), (chicken, 0, "Coop", false), (duck, 0, "Coop", true),
            (cow, 0, "Barn", true), (cow, 1, "Barn", false), (sheep, 0, "Barn", true), (goat, 0, "Barn", true)
        };
        starters.arraySize = starterList.Length;
        for (int i = 0; i < starterList.Length; i++)
        {
            SerializedProperty e = starters.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("species").objectReferenceValue = starterList[i].species;
            e.FindPropertyRelative("variant").intValue = starterList[i].variant;
            e.FindPropertyRelative("houseId").stringValue = starterList[i].house;
            e.FindPropertyRelative("adult").boolValue = starterList[i].adult;
        }
        ms.ApplyModifiedPropertiesWithoutUndo();

        GameObject spawnerGo = new GameObject("WildAnimalSpawner");
        Undo.RegisterCreatedObjectUndo(spawnerGo, "Build Ranch");
        WildAnimalSpawner spawner = spawnerGo.AddComponent<WildAnimalSpawner>();
        SerializedObject ss = new SerializedObject(spawner);
        ss.FindProperty("wildPrefab").objectReferenceValue = wildPrefab;
        ss.FindProperty("forestTilemap").objectReferenceValue = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == "Forest");
        SerializedProperty entries = ss.FindProperty("entries");
        (AnimalSpecies species, int count, float radius)[] wild = { (rabbit, 6, 5f), (deer, 3, 8f), (fox, 2, 8f) };
        entries.arraySize = wild.Length;
        for (int i = 0; i < wild.Length; i++)
        {
            SerializedProperty e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("species").objectReferenceValue = wild[i].species;
            e.FindPropertyRelative("count").intValue = wild[i].count;
            e.FindPropertyRelative("wanderRadius").floatValue = wild[i].radius;
        }
        ss.ApplyModifiedPropertiesWithoutUndo();
        return removed;
    }

    private static GameObject Instance(GameObject prefab, Transform parent, Vector2 position)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = position;
        return go;
    }

    private static FeedTrough Trough(GameObject prefab, Transform parent, Vector2 position, string id, AnimalHouse house)
    {
        FeedTrough trough = Instance(prefab, parent, position).GetComponent<FeedTrough>();
        trough.name = $"FeedTrough_{id}";
        SerializedObject so = new SerializedObject(trough);
        so.FindProperty("troughId").stringValue = id;
        so.FindProperty("house").objectReferenceValue = house;
        so.ApplyModifiedPropertiesWithoutUndo();
        return trough;
    }

    // Vùng con vật đi lại = phần trong hàng rào, chừa 0,35 ô.
    private static void ConfigureHouse(AnimalHouse house, FeedTrough trough, RectInt pen)
    {
        SerializedObject so = new SerializedObject(house);
        so.FindProperty("trough").objectReferenceValue = trough;
        so.FindProperty("penRect").rectValue = new Rect(pen.xMin + 1.35f, pen.yMin + 1.35f, pen.width - 2.7f, pen.height - 2.7f);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Hàng rào quanh bãi: bỏ đoạn sát chân chuồng (hàng trên) và chừa cổng 2 ô ở phía lối đi giữa hai bãi.
    private static void AddPenFences(HashSet<Vector2Int> cells, RectInt pen, float buildingCenterX, float buildingWidth, bool gateOnEast)
    {
        int top = pen.yMax - 1, right = pen.xMax - 1;
        for (int x = pen.xMin; x <= right; x++)
        {
            for (int y = pen.yMin; y <= top; y++)
            {
                bool border = x == pen.xMin || x == right || y == pen.yMin || y == top;
                if (!border) continue;
                // Ô có tâm nằm dưới chân chuồng thì bỏ; ô sát mép vẫn giữ để rào khít vào tường, không để hở.
                float center = x + 0.5f;
                if (y == top && center > buildingCenterX - buildingWidth / 2f && center < buildingCenterX + buildingWidth / 2f) continue;
                int gateX = gateOnEast ? right : pen.xMin;
                if (x == gateX && GateRows.Contains(y)) continue;
                cells.Add(new Vector2Int(x, y));
            }
        }
    }

    // Chọn mảnh hàng rào theo 4 ô kề (cùng bảng mảnh với MainMapDecorator).
    private static void CreateFence(Vector2Int cell, HashSet<Vector2Int> cells, Dictionary<Vector2Int, Sprite> sprites, Transform parent)
    {
        bool n = cells.Contains(cell + Vector2Int.up), s = cells.Contains(cell + Vector2Int.down);
        bool e = cells.Contains(cell + Vector2Int.right), w = cells.Contains(cell + Vector2Int.left);

        Vector2Int piece;
        if (e && s && !w && !n) piece = new Vector2Int(0, 0);
        else if (w && s && !e && !n) piece = new Vector2Int(2, 0);
        else if (e && n && !w && !s) piece = new Vector2Int(0, 2);
        else if (w && n && !e && !s) piece = new Vector2Int(2, 2);
        else if (e && w) piece = new Vector2Int(1, 2);
        else if (e) piece = new Vector2Int(1, 3);
        else if (w) piece = new Vector2Int(2, 3);
        else if (n || s) piece = new Vector2Int(0, 1);
        else piece = new Vector2Int(1, 4);
        if (!sprites.TryGetValue(piece, out Sprite sprite)) return;

        GameObject go = new GameObject("Fence");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        go.AddComponent<SpriteRenderer>().sprite = sprite;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(1f, 0.4f);
        box.offset = new Vector2(0f, -0.25f);
    }

    private static void Decor(string name, Sprite sprite, Vector2 position, Transform parent, bool solid)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        if (!solid) return;
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(sprite.bounds.size.x * 0.9f, Mathf.Min(0.6f, sprite.bounds.size.y));
        bool bottomPivot = sprite.pivot.y < 1f;   // pivot tính bằng điểm ảnh
        box.offset = new Vector2(0f, bottomPivot ? 0.3f : -0.1f);
    }

    // Gắn ShopSensor có sẵn vào người bán, bán con giống + thức ăn + cà rốt (để thuần hoá thỏ).
    private static void AddShop(GameObject keeper)
    {
        GameObject sensorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShopSensorPrefab);
        if (sensorPrefab == null)
        {
            Debug.LogWarning("[RanchBuilder] Không tìm thấy ShopSensor.prefab, người bán chưa có cửa hàng.");
            return;
        }
        GameObject sensor = (GameObject)PrefabUtility.InstantiatePrefab(sensorPrefab, keeper.transform);
        sensor.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        ShopNPC shop = sensor.GetComponent<ShopNPC>();
        SerializedObject so = new SerializedObject(shop);
        so.FindProperty("npcName").stringValue = "Trang trại";
        SerializedProperty list = so.FindProperty("permanentItems");
        ItemData[] goods = new[] { "animal_chicken", "animal_duck", "animal_cow", "animal_goat", "animal_sheep", "animal_feed", "carrot" }
            .Select(Item).Where(i => i != null).ToArray();
        list.arraySize = goods.Length;
        for (int i = 0; i < goods.Length; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("itemData").objectReferenceValue = goods[i];
            e.FindPropertyRelative("stock").intValue = -1;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Dọn cây / bụi / hoa do MainMapDecorator sinh nằm trong khu trang trại.
    private static int ClearDecor()
    {
        GameObject decorRoot = GameObject.Find("Generated_Decor");
        if (decorRoot == null) return 0;

        List<GameObject> toRemove = new List<GameObject>();
        foreach (Transform child in decorRoot.transform)
        {
            if (ClearArea.Contains((Vector2)child.position)) toRemove.Add(child.gameObject);
        }
        foreach (GameObject go in toRemove) Undo.DestroyObjectImmediate(go);
        return toRemove.Count;
    }

    private static void SetList(SerializedProperty list, Object[] values)
    {
        list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
