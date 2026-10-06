using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Lưới ô đi được của map Main, dùng cho tìm đường (A*).
// Một ô đi được khi: có tile Ground (đất / cỏ) hoặc tile Bridge (cầu), và không bị collider cứng nào chiếm.
// Vật cản lấy trực tiếp từ Physics2D nên nước (Water), vách (CliffCollider), thân cây, gốc cây, hàng rào, đá, nhà...
// đều tự được tính; trigger (tán cây, bụi đi xuyên được) và vật thể động (Player) được bỏ qua.
public class WalkableGrid : MonoBehaviour
{
    public static WalkableGrid Instance { get; private set; }

    [Header("Tilemaps")]
    [Tooltip("Tilemap mặt đất (để trống sẽ tự tìm object có tag 'Ground').")]
    [SerializeField] private Tilemap groundTilemap;

    [Tooltip("Tilemap cầu (để trống sẽ tự tìm object tên 'Bridge').")]
    [SerializeField] private Tilemap bridgeTilemap;

    [Header("Obstacles")]
    [Tooltip("Các layer được coi là vật cản (mặc định: tất cả).")]
    [SerializeField] private LayerMask obstacleLayers = ~0;

    [Tooltip("Kích thước vùng kiểm tra trong mỗi ô (1 = cả ô). Nhỏ hơn 1 để không bắt nhầm vật cản của ô bên cạnh.")]
    [SerializeField, Range(0.3f, 1f)] private float checkSize = 0.8f;

    [Header("Debug")]
    [Tooltip("Vẽ các ô bị chặn (đỏ) trong Scene view khi chọn object này.")]
    [SerializeField] private bool drawGizmos = true;

    private bool[] walkable;
    private BoundsInt bounds;
    private readonly List<Collider2D> hits = new List<Collider2D>();

    public bool IsBuilt => walkable != null;
    public BoundsInt Bounds => bounds;
    public Tilemap GroundTilemap => groundTilemap;

    private void Awake()
    {
        Instance = this;
        ResolveTilemaps();
    }

    // Đợi 1 frame + 1 bước vật lý để các cầu thang (Stairs) xoá xong collider vách rồi mới quét.
    private IEnumerator Start()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        Rebuild();
    }

    private void ResolveTilemaps()
    {
        if (groundTilemap == null)
        {
            GameObject ground = GameObject.FindWithTag("Ground");
            if (ground != null) groundTilemap = ground.GetComponent<Tilemap>();
        }

        if (bridgeTilemap == null)
        {
            GameObject bridge = GameObject.Find("Bridge");
            if (bridge != null) bridgeTilemap = bridge.GetComponent<Tilemap>();
        }
    }

    // Quét lại toàn bộ map.
    [ContextMenu("Rebuild (xem trước lưới đi được)")]
    public void Rebuild()
    {
        ResolveTilemaps();
        if (groundTilemap == null)
        {
            Debug.LogWarning("[WalkableGrid] Chưa có Tilemap Ground.", this);
            return;
        }

        bounds = groundTilemap.cellBounds;
        walkable = new bool[bounds.size.x * bounds.size.y];
        Physics2D.SyncTransforms();

        int count = 0;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            bool value = EvaluateCell(cell);
            walkable[Index(cell)] = value;
            if (value) count++;
        }

        Debug.Log($"[WalkableGrid] Đã quét {bounds.size.x}x{bounds.size.y} ô, {count} ô đi được.");
    }

    // Quét lại các ô quanh một vị trí (ví dụ sau khi chặt cây, đặt hàng rào mới).
    public void RefreshArea(Vector3 worldPosition, int radius = 1)
    {
        if (!IsBuilt) return;

        Physics2D.SyncTransforms();
        Vector3Int center = groundTilemap.WorldToCell(worldPosition);
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector3Int cell = new Vector3Int(center.x + x, center.y + y, 0);
                if (InBounds(cell)) walkable[Index(cell)] = EvaluateCell(cell);
            }
        }
    }

    public bool IsWalkable(Vector3Int cell)
    {
        if (!IsBuilt) Rebuild();
        return IsBuilt && InBounds(cell) && walkable[Index(cell)];
    }

    public bool IsWalkable(Vector3 worldPosition) => IsWalkable(WorldToCell(worldPosition));

    // Tìm ô đi được gần nhất quanh một ô (ví dụ click trúng thân cây / nhà -> lấy ô trống sát bên).
    // Quét theo từng vòng vuông nở dần, trong mỗi vòng chọn ô gần nhất theo khoảng cách thật.
    public bool TryGetNearestWalkable(Vector3Int cell, int maxRadius, out Vector3Int result)
    {
        result = cell;
        if (IsWalkable(cell)) return true;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            float bestDistance = float.MaxValue;
            bool found = false;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;   // chỉ xét viền của vòng

                    Vector3Int candidate = new Vector3Int(cell.x + x, cell.y + y, 0);
                    if (!IsWalkable(candidate)) continue;

                    float distance = x * x + y * y;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        result = candidate;
                        found = true;
                    }
                }
            }

            if (found) return true;
        }
        return false;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return groundTilemap.WorldToCell(new Vector3(worldPosition.x, worldPosition.y, 0f));
    }

    public Vector3 CellCenter(Vector3Int cell) => groundTilemap.GetCellCenterWorld(cell);

    public bool InBounds(Vector3Int cell)
    {
        return cell.x >= bounds.xMin && cell.x < bounds.xMax && cell.y >= bounds.yMin && cell.y < bounds.yMax;
    }

    private int Index(Vector3Int cell) => (cell.y - bounds.yMin) * bounds.size.x + (cell.x - bounds.xMin);

    private bool EvaluateCell(Vector3Int cell)
    {
        bool hasFloor = groundTilemap.HasTile(cell) || (bridgeTilemap != null && bridgeTilemap.HasTile(cell));
        if (!hasFloor) return false;

        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayers);

        Vector2 center = groundTilemap.GetCellCenterWorld(cell);
        Vector2 size = (Vector2)groundTilemap.cellSize * checkSize;
        int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);

        for (int i = 0; i < count; i++)
        {
            if (IsObstacle(hits[i])) return false;
        }
        return true;
    }

    // Bỏ qua trigger và vật thể động (Player, sau này là NPC / vật nuôi).
    private static bool IsObstacle(Collider2D collider)
    {
        if (collider.isTrigger) return false;
        if (collider.CompareTag("Player")) return false;

        Rigidbody2D body = collider.attachedRigidbody;
        return body == null || body.bodyType != RigidbodyType2D.Dynamic;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !IsBuilt || groundTilemap == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        Vector3 size = new Vector3(groundTilemap.cellSize.x, groundTilemap.cellSize.y, 0f) * 0.9f;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (walkable[Index(cell)]) continue;
            bool hasFloor = groundTilemap.HasTile(cell) || (bridgeTilemap != null && bridgeTilemap.HasTile(cell));
            if (hasFloor) Gizmos.DrawCube(groundTilemap.GetCellCenterWorld(cell), size);
        }
    }
}
