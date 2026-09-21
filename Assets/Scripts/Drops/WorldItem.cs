using System.Collections;
using UnityEngine;

// Đại diện cho vật phẩm nằm trong thế giới game (WorldItem).
// Quản lý hiển thị hình ảnh, hiệu ứng nảy/văng khi rơi, delay cho phép nhặt,
// khả năng gộp chồng (merge) dưới đất và hiệu ứng hút (magnet) về phía người chơi.
[RequireComponent(typeof(SpriteRenderer))]
public class WorldItem : MonoBehaviour
{
    [Header("Visual & Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D itemCollider;

    [Header("Drop Animation Settings")]
    [Tooltip("Độ cao tối đa của đường cong Parabol khi văng ra.")]
    [SerializeField] private float bounceHeight = 0.6f;

    [Tooltip("Thời gian bay từ vị trí rơi tới điểm tiếp đất (giây).")]
    [SerializeField] private float tossDuration = 0.35f;

    [Tooltip("Thời gian chờ thêm sau khi tiếp đất trước khi cho phép người chơi nhặt (tránh nhặt tức thì).")]
    [SerializeField] private float pickupDelay = 0.2f;

    [Header("Magnet & Merging Settings")]
    [Tooltip("Tốc độ bay tối thiểu khi bị nam châm hút.")]
    [SerializeField] private float baseMagnetSpeed = 6f;

    [Tooltip("Gia tốc tăng tốc khi hút về phía người chơi.")]
    [SerializeField] private float magnetAcceleration = 8f;

    [Tooltip("Khoảng cách tối đa để tự động gộp các vật phẩm cùng loại trên mặt đất.")]
    [SerializeField] private float mergeRadius = 0.5f;

    private ItemData item;
    private int amount;
    private bool canPickup = false;
    private bool isCollected = false;

    private Transform magnetTarget;
    private float currentMagnetSpeed;
    private Coroutine tossCoroutine;

    #region Properties

    public ItemData Item => item;
    public int Amount => amount;
    public bool CanPickup => canPickup && !isCollected;

    #endregion

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (itemCollider == null)
        {
            itemCollider = GetComponent<Collider2D>();
        }

        // Đảm bảo Collider là dạng Trigger để không cản trở di chuyển của Player
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // Xử lý di chuyển hút về phía Player khi đang trong trạng thái Magnet
        if (canPickup && magnetTarget != null)
        {
            currentMagnetSpeed += magnetAcceleration * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                magnetTarget.position,
                currentMagnetSpeed * Time.deltaTime
            );
        }
    }

    // Khởi tạo thông tin vật phẩm và kích hoạt hiệu ứng rơi văng nảy
    public void Initialize(ItemData itemData, int itemAmount, Vector3? landingPosition = null)
    {
        item = itemData;
        amount = itemAmount;
        isCollected = false;
        magnetTarget = null;
        currentMagnetSpeed = baseMagnetSpeed;

        if (spriteRenderer != null && item != null)
        {
            spriteRenderer.sprite = item.Icon;
        }

        if (landingPosition.HasValue)
        {
            if (tossCoroutine != null) StopCoroutine(tossCoroutine);
            tossCoroutine = StartCoroutine(TossAnimationRoutine(transform.position, landingPosition.Value));
        }
        else
        {
            canPickup = true;
        }
    }

    // Coroutine mô phỏng đường cong Parabol và cú nảy phụ khi tiếp đất (Zero Dependencies)
    private IEnumerator TossAnimationRoutine(Vector3 startPos, Vector3 targetPos)
    {
        canPickup = false;
        float elapsed = 0f;

        // Pha 1: Văng Parabol chính
        while (elapsed < tossDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / tossDuration);

            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
            float arcHeight = 4f * bounceHeight * t * (1f - t); // Parabol đỉnh tại t = 0.5
            transform.position = linearPos + new Vector3(0f, arcHeight, 0f);

            yield return null;
        }

        transform.position = targetPos;

        // Pha 2: Cú nảy nhỏ thứ hai (mini bounce) tạo cảm giác vật lý nảy tự nhiên
        float miniBounceDuration = 0.15f;
        float miniBounceHeight = bounceHeight * 0.25f;
        elapsed = 0f;

        while (elapsed < miniBounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / miniBounceDuration);
            float arcHeight = 4f * miniBounceHeight * t * (1f - t);
            transform.position = targetPos + new Vector3(0f, arcHeight, 0f);

            yield return null;
        }

        transform.position = targetPos;

        // Pha 3: Chờ thêm một khoảng nhỏ trước khi cho phép hút / nhặt
        if (pickupDelay > 0f)
        {
            yield return new WaitForSeconds(pickupDelay);
        }

        canPickup = true;

        // Quét thử xem có vật phẩm cùng loại nào gần đó để gộp lại không
        TryMergeWithNearby();
    }

    // Kích hoạt bay hút về phía mục tiêu (Player)
    public void AttractTo(Transform target)
    {
        if (!CanPickup) return;
        magnetTarget = target;
    }

    // Đánh dấu đã được nhặt thành công (ngăn các tương tác thừa)
    public void MarkAsCollected()
    {
        isCollected = true;
        canPickup = false;
    }

    // Cập nhật lại số lượng vật phẩm (dùng khi người chơi chỉ nhặt được một phần)
    public void SetAmount(int newAmount)
    {
        amount = newAmount;
        if (amount <= 0)
        {
            Destroy(gameObject);
        }
    }

    #region Ground Merge Logic

    // Kiểm tra xem có thể gộp với vật phẩm khác không
    public bool CanMergeWith(WorldItem other)
    {
        if (other == null || other == this || isCollected || other.isCollected)
            return false;

        if (!canPickup || !other.canPickup)
            return false;

        if (item == null || other.Item == null || item.ItemID != other.Item.ItemID)
            return false;

        return (amount + other.Amount) <= item.MaxStack;
    }

    // Thực hiện gộp vật phẩm khác vào vật phẩm này
    public void MergeWith(WorldItem other)
    {
        if (!CanMergeWith(other)) return;

        amount += other.Amount;
        other.MarkAsCollected();
        Destroy(other.gameObject);

        // Hiệu ứng nảy nhẹ khi gộp
        StartCoroutine(PopScaleRoutine());
    }

    // Quét các WorldItem lân cận trên mặt đất và tiến hành gộp
    private void TryMergeWithNearby()
    {
        if (!CanPickup || item == null) return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, mergeRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].TryGetComponent<WorldItem>(out WorldItem nearbyItem))
            {
                if (CanMergeWith(nearbyItem))
                {
                    MergeWith(nearbyItem);
                    break;
                }
            }
        }
    }

    private IEnumerator PopScaleRoutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 popScale = originalScale * 1.25f;

        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, popScale, t / 0.1f);
            yield return null;
        }

        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(popScale, originalScale, t / 0.1f);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mergeRadius);
    }
}
