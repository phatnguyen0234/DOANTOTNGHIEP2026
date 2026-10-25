using System;
using UnityEngine;

// Đại diện cho thực thể Rương đồ (Chest) trong thế giới game.
// Quản lý trạng thái mở/đóng nắp, hình ảnh hiển thị, kiểm tra cự ly tương tác với người chơi.
// Tích hợp với IInteractable để nhận lệnh tương tác từ InteractController.
[RequireComponent(typeof(Inventory), typeof(Collider2D))]
public class Chest : MonoBehaviour, IInteractable
{
    [Header("Chest Identification")]
    [Tooltip("Mã định danh duy nhất của rương (tự sinh GUID nếu để trống, phục vụ Save/Load).")]
    [SerializeField] private string chestId = "";

    [Tooltip("Tên hiển thị trên tiêu đề giao diện khi mở rương.")]
    [SerializeField] private string chestName = "Rương Đồ";

    [Header("Dependencies")]
    [Tooltip("Inventory gắn liền với rương này để lưu trữ vật phẩm.")]
    [SerializeField] private Inventory chestInventory;

    [Header("Visual Settings")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh của rương.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Sprite hiển thị khi rương đang mở nắp.")]
    [SerializeField] private Sprite openSprite;

    [Tooltip("Sprite hiển thị khi rương đang đóng nắp.")]
    [SerializeField] private Sprite closedSprite;

    [Header("Interaction Settings")]
    [Tooltip("Khoảng cách tối đa người chơi có thể đứng tương tác và duy trì mở rương.")]
    [SerializeField, Min(0.5f)] private float maxInteractDistance = 2.5f;

    private GameObject currentInteractor;
    private bool isOpen = false;

    #region Properties

    public string ChestId => chestId;
    public string ChestName => chestName;
    public Inventory Inventory => chestInventory;
    public bool IsOpen => isOpen;

    public string InteractionPrompt => $"Mở {chestName}";

    #endregion

    private void Awake()
    {
        ResolveReferences();

        if (string.IsNullOrWhiteSpace(chestId))
        {
            chestId = Guid.NewGuid().ToString();
        }

        if (chestInventory != null)
        {
            chestInventory.SetAsPlayerInventory(false);
        }
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (chestInventory == null)
        {
            chestInventory = GetComponent<Inventory>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        // Tự động đóng rương nếu người chơi di chuyển ra quá xa
        if (isOpen && currentInteractor != null)
        {
            float distance = Vector2.Distance(currentInteractor.transform.position, transform.position);
            if (distance > maxInteractDistance)
            {
                if (ChestUI.Instance != null && ChestUI.Instance.CurrentChest == this)
                {
                    ChestUI.Instance.CloseChest();
                }
                else
                {
                    OnChestClosed();
                }
            }
        }
    }

    #region IInteractable Implementation

    public bool CanInteract(GameObject interactor)
    {
        if (interactor == null) return false;
        return Vector2.Distance(interactor.transform.position, transform.position) <= maxInteractDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        currentInteractor = interactor;
        isOpen = true;

        UpdateVisual(true);

        if (ChestUI.Instance != null)
        {
            ChestUI.Instance.OpenChest(this);
        }
        else
        {
            Debug.LogWarning("[Chest] Không tìm thấy ChestUI.Instance trong Scene!", this);
        }
    }

    #endregion

    // Được gọi bởi ChestUI khi người chơi đóng rương
    public void OnChestClosed()
    {
        isOpen = false;
        currentInteractor = null;
        UpdateVisual(false);
    }

    private void UpdateVisual(bool open)
    {
        if (spriteRenderer == null) return;

        if (open && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }
        else if (!open && closedSprite != null)
        {
            spriteRenderer.sprite = closedSprite;
        }
    }
}
