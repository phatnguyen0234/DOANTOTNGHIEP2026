using UnityEngine;
using UnityEngine.Tilemaps;

// Cầu thang lên vách đá: khi vào game, mở lối đi bằng cách xoá các ô collider vách (tilemap "CliffCollider")
// nằm dưới hình cầu thang, cộng thêm 1 ô viền phía trên đầu cầu thang.
// Vách trong scene luôn chặn kín, nên dời cầu thang đi đâu thì lối đi theo đó, chỗ cũ tự kín lại.
// Chỉ thay đổi tilemap lúc chạy game, không lưu vào scene.
[RequireComponent(typeof(SpriteRenderer))]
public class Stairs : MonoBehaviour
{
    [Tooltip("Tilemap collider của vách (để trống sẽ tự tìm object tên 'CliffCollider').")]
    [SerializeField] private Tilemap cliffCollider;

    // Thu nhỏ vùng xét theo chiều ngang để không ăn sang ô bên cạnh do sai số toạ độ.
    private const float EdgeInset = 0.1f;

    private void Start()
    {
        if (cliffCollider == null)
        {
            GameObject found = GameObject.Find("CliffCollider");
            if (found != null) cliffCollider = found.GetComponent<Tilemap>();
        }

        if (cliffCollider == null)
        {
            Debug.LogWarning("[Stairs] Không tìm thấy tilemap 'CliffCollider', cầu thang không mở được lối.", this);
            return;
        }

        OpenPassage();
    }

    private void OpenPassage()
    {
        Bounds bounds = GetComponent<SpriteRenderer>().bounds;
        Vector3Int min = cliffCollider.WorldToCell(new Vector3(bounds.min.x + EdgeInset, bounds.min.y + EdgeInset, 0f));
        Vector3Int max = cliffCollider.WorldToCell(new Vector3(bounds.max.x - EdgeInset, bounds.max.y + 0.5f, 0f));

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                cliffCollider.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    // Hiển thị vùng sẽ được mở lối khi chọn cầu thang trong Scene view.
    private void OnDrawGizmosSelected()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        Bounds bounds = spriteRenderer.bounds;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawCube(bounds.center + new Vector3(0f, 0.5f, 0f), bounds.size + new Vector3(0f, 1f, 0f));
    }
}
