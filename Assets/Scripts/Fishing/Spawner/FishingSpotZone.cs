using UnityEngine;

// Component gắn vào Trigger Collider2D của vùng nước để cung cấp FishingSpotData riêng cho khu vực đó
[RequireComponent(typeof(Collider2D))]
public class FishingSpotZone : MonoBehaviour
{
    [Tooltip("Dữ liệu điểm câu và danh sách cá tương ứng của khu vực này.")]
    [SerializeField] private FishingSpotData spotData;

    public FishingSpotData SpotData => spotData;

    private void Awake()
    {
        // Đảm bảo collider là Trigger để không cản trở di chuyển vật lý nếu cần
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}
