using UnityEngine;
using UnityEngine.Tilemaps;

// Thành phần kiểm tra vị trí xem có phải là mặt nước hợp lệ để câu cá hay không
public class WaterDetector : MonoBehaviour
{
    [Header("Detection Modes")]
    [Tooltip("LayerMask dùng để nhận diện vùng nước bằng Collider2D / Trigger.")]
    [SerializeField] private LayerMask waterLayer;

    [Tooltip("Tilemap chứa các ô nước (nếu dùng Tilemap).")]
    [SerializeField] private Tilemap waterTilemap;

    [Tooltip("Bán kính hình cầu để kiểm tra va chạm tại điểm rơi.")]
    [SerializeField] private float checkRadius = 0.25f;

    // Kiểm tra xem vị trí worldPosition có nằm trong vùng nước câu được không
    public bool IsInWater(Vector2 worldPosition, out FishingSpotData detectedSpot)
    {
        detectedSpot = null;

        // 1. Kiểm tra qua Collider2D (Physics2D)
        Collider2D hitCollider = Physics2D.OverlapCircle(worldPosition, checkRadius, waterLayer);
        if (hitCollider != null)
        {
            // Kiểm tra xem vùng va chạm có gắn FishingSpotZone / FishingSpotData không
            FishingSpotZone spotZone = hitCollider.GetComponent<FishingSpotZone>();
            if (spotZone != null)
            {
                detectedSpot = spotZone.SpotData;
            }
            return true;
        }

        // 2. Kiểm tra qua Water Tilemap (nếu có cấu hình)
        if (waterTilemap != null)
        {
            Vector3Int cellPos = waterTilemap.WorldToCell(worldPosition);
            if (waterTilemap.HasTile(cellPos))
            {
                return true;
            }
        }

        return false;
    }

    public void SetWaterTilemap(Tilemap tilemap)
    {
        waterTilemap = tilemap;
    }

    public void SetWaterLayer(LayerMask layer)
    {
        waterLayer = layer;
    }
}
