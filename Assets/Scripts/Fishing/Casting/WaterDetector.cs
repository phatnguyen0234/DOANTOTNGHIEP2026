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

    [Tooltip("Bán kính hình tròn để kiểm tra va chạm tại điểm rơi.")]
    [SerializeField] private float checkRadius = 0.25f;

    [Tooltip("Nếu chưa cài đặt LayerMask hoặc Tilemap nước trong Editor, tự động cho phép mọi vị trí (tiện lợi cho test).")]
    [SerializeField] private bool fallbackIfNoConfig = true;

    [Tooltip("In ra Console vùng câu (Biển / Sông / Ao hồ) mỗi khi phao rơi xuống nước.")]
    [SerializeField] private bool logDetectedSpot = true;

    // Kiểm tra xem vị trí worldPosition có nằm trong vùng nước câu được không
    public bool IsInWater(Vector2 worldPosition, out FishingSpotData detectedSpot)
    {
        detectedSpot = null;

        // 1. Kiểm tra qua Collider2D với waterLayer (nếu có cài đặt layer).
        // Một điểm có thể trúng nhiều collider nước (tilemap Water chặn đường + vùng câu Biển / Sông / Ao hồ)
        // -> ưu tiên collider có FishingSpotZone để lấy đúng loại cá của vùng.
        if (waterLayer.value != 0)
        {
            Collider2D[] waterHits = Physics2D.OverlapCircleAll(worldPosition, checkRadius, waterLayer);
            if (waterHits.Length > 0)
            {
                foreach (Collider2D hit in waterHits)
                {
                    FishingSpotZone spotZone = hit.GetComponent<FishingSpotZone>();
                    if (spotZone != null && spotZone.SpotData != null)
                    {
                        detectedSpot = spotZone.SpotData;
                        break;
                    }
                }

                if (logDetectedSpot)
                {
                    Debug.Log(detectedSpot != null
                        ? $"[WaterDetector] Phao rơi vào vùng: {detectedSpot.SpotName} ({detectedSpot.name})"
                        : "[WaterDetector] Phao rơi vào nước không thuộc vùng câu nào -> dùng bảng cá mặc định");
                }
                return true;
            }
        }

        // 2. Kiểm tra qua Tag "Water" hoặc tên Object chứa "Water"
        Collider2D[] allHits = Physics2D.OverlapCircleAll(worldPosition, checkRadius);
        foreach (var col in allHits)
        {
            if (col.CompareTag("Water") || col.gameObject.name.ToLower().Contains("water"))
            {
                FishingSpotZone spotZone = col.GetComponent<FishingSpotZone>();
                if (spotZone != null)
                {
                    detectedSpot = spotZone.SpotData;
                }
                return true;
            }
        }

        // 3. Kiểm tra qua Water Tilemap (nếu có cấu hình)
        if (waterTilemap != null)
        {
            Vector3Int cellPos = waterTilemap.WorldToCell(worldPosition);
            if (waterTilemap.HasTile(cellPos))
            {
                return true;
            }
        }

        // 4. Nếu hoàn toàn chưa gán cấu hình waterLayer & waterTilemap, cho phép câu để tiện test
        if (fallbackIfNoConfig && waterLayer.value == 0 && waterTilemap == null)
        {
            return true;
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
