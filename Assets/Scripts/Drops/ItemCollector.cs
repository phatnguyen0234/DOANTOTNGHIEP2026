using UnityEngine;

// Thành phần gắn trên Player chịu trách nhiệm hút (Magnet) và thu gom (Collect) vật phẩm vào Inventory.
// Phân tách 2 vùng bán kính: Magnet Radius (hút từ xa) và Pickup Radius (thu nhặt khi tới gần).
// Hỗ trợ xử lý thông minh khi túi đồ đầy: chỉ nhặt phần có thể chứa, giữ lại phần dư trên mặt đất.
public class ItemCollector : MonoBehaviour
{
    [Header("Inventory Reference")]
    [Tooltip("Tham chiếu tới Inventory của người chơi (tự động tìm kiếm nếu để trống).")]
    [SerializeField] private Inventory inventory;

    [Header("Range & Detection Settings")]
    [Tooltip("Bán kính hút vật phẩm từ xa về phía người chơi.")]
    [SerializeField, Min(0.1f)] private float magnetRadius = 2.0f;

    [Tooltip("Bán kính tiếp xúc để nhặt vật phẩm vào túi đồ.")]
    [SerializeField, Min(0.1f)] private float pickupRadius = 0.5f;

    [Tooltip("LayerMask quét vật phẩm rơi để tối ưu hiệu năng (Mặc định: Tất cả).")]
    [SerializeField] private LayerMask itemLayerMask = ~0;

    [Header("Audio & Effects (Optional)")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioSource audioSource;

    private readonly Collider2D[] hitBuffer = new Collider2D[20];

    private void Awake()
    {
        ResolveInventory();
    }

    private void Start()
    {
        ResolveInventory();
    }

    private void Update()
    {
        ScanAndAttractItems();
    }

    private void ResolveInventory()
    {
        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }

        if (inventory == null)
        {
            inventory = GetComponentInParent<Inventory>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }
    }

    // Quét các vật phẩm trong tầm Magnet và Pickup
    private void ScanAndAttractItems()
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, magnetRadius, hitBuffer, itemLayerMask);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;

            if (col.TryGetComponent<WorldItem>(out WorldItem worldItem))
            {
                if (!worldItem.CanPickup) continue;

                float distance = Vector2.Distance(transform.position, worldItem.transform.position);

                if (distance <= pickupRadius)
                {
                    Collect(worldItem);
                }
                else
                {
                    worldItem.AttractTo(transform);
                }
            }
        }
    }

    // Xử lý logic nhặt vật phẩm vào Inventory
    public void Collect(WorldItem worldItem)
    {
        if (worldItem == null || !worldItem.CanPickup) return;

        if (inventory == null)
        {
            ResolveInventory();
            if (inventory == null)
            {
                Debug.LogWarning("[ItemCollector] Không tìm thấy Inventory để thêm vật phẩm!", this);
                return;
            }
        }

        // Gọi AddItem: trả về số lượng còn dư không thể chứa hết trong kho
        int remaining = inventory.AddItem(worldItem.Item, worldItem.Amount);

        if (remaining <= 0)
        {
            // Nhặt thành công toàn bộ
            worldItem.MarkAsCollected();
            PlayPickupFeedback();
            Destroy(worldItem.gameObject);
        }
        else
        {
            // Kho đồ chỉ còn chỗ cho một phần, để lại phần dư trên mặt đất
            worldItem.SetAmount(remaining);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<WorldItem>(out WorldItem worldItem))
        {
            if (worldItem.CanPickup)
            {
                Collect(worldItem);
            }
        }
    }

    private void PlayPickupFeedback()
    {
        if (pickupSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vùng Magnet (Xanh lục nhạt)
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, magnetRadius);

        // Vùng Pickup (Xanh lá)
        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
