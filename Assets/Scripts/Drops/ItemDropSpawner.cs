using System.Collections.Generic;
using UnityEngine;

// Quản lý việc khởi tạo và phân tán các vật phẩm rơi (WorldItem) trong game.
// Đảm bảo không đối tượng nào (Tree, Crop, Rock...) phải tự Instantiate vật phẩm trực tiếp.
public class ItemDropSpawner : MonoBehaviour
{
    public static ItemDropSpawner Instance { get; private set; }

    [Header("Prefab & Spawning Settings")]
    [Tooltip("Prefab chuẩn đại diện cho WorldItem trên mặt đất.")]
    [SerializeField] private WorldItem worldItemPrefab;

    [Tooltip("Bán kính phân tán ngẫu nhiên mặc định xung quanh điểm rơi.")]
    [SerializeField] private float defaultSpreadRadius = 0.8f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolvePrefab();
    }

    private void ResolvePrefab()
    {
        if (worldItemPrefab == null)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("WorldItem t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                worldItemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldItem>(path);
            }
#endif
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        ResolvePrefab();
    }
#endif

    // Sinh một loại vật phẩm tại vị trí origin với bán kính văng ngẫu nhiên
    public WorldItem SpawnDrop(ItemData item, int amount, Vector3 origin, float spreadRadius = -1f)
    {
        if (item == null || amount <= 0)
            return null;

        if (spreadRadius < 0f)
        {
            spreadRadius = defaultSpreadRadius;
        }

        // Tính toán điểm tiếp đất ngẫu nhiên trong vòng tròn bán kính spreadRadius
        Vector2 randomOffset = Random.insideUnitCircle * spreadRadius;
        Vector3 landingPos = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);

        WorldItem worldItemInstance = CreateWorldItemInstance(origin);
        worldItemInstance.Initialize(item, amount, landingPos);

        return worldItemInstance;
    }

    // Sinh danh sách nhiều loại vật phẩm rơi từ bảng kết quả DropGenerator
    public void SpawnDrops(IEnumerable<ItemDropResult> drops, Vector3 origin, float spreadRadius = -1f)
    {
        if (drops == null) return;

        if (spreadRadius < 0f)
        {
            spreadRadius = defaultSpreadRadius;
        }

        foreach (ItemDropResult drop in drops)
        {
            if (drop.item != null && drop.amount > 0)
            {
                SpawnDrop(drop.item, drop.amount, origin, spreadRadius);
            }
        }
    }

    // Tiện ích sinh vật phẩm trực tiếp từ một DropTable và DropContext
    public void SpawnFromTable(DropTable table, Vector3 origin, DropContext context = default)
    {
        if (table == null) return;

        List<ItemDropResult> drops = DropGenerator.Generate(table, context);
        SpawnDrops(drops, origin);
    }

    // Khởi tạo instance từ prefab hoặc fallback runtime an toàn nếu prefab chưa được gán
    private WorldItem CreateWorldItemInstance(Vector3 spawnPosition)
    {
        if (worldItemPrefab != null)
        {
            return Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity);
        }

        // Fallback: Tự động khởi tạo GameObject nếu chưa gán prefab trong Inspector
        GameObject fallbackObj = new GameObject("WorldItem_RuntimeInstance");
        fallbackObj.transform.position = spawnPosition;

        SpriteRenderer sr = fallbackObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10; // Đảm bảo nổi trên Tilemap Ground

        CircleCollider2D col = fallbackObj.AddComponent<CircleCollider2D>();
        col.radius = 0.25f;
        col.isTrigger = true;

        WorldItem worldItem = fallbackObj.AddComponent<WorldItem>();
        return worldItem;
    }
}
