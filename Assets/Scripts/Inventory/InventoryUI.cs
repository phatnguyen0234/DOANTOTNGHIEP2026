using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BlitzyUI;

// Quản lý giao diện tổng thể của Inventory, kế thừa BlitzyUI.Screen để quản lý vòng đời và điều hướng tập trung.
// Lắng nghe sự kiện OnInventoryChanged từ Inventory để cập nhật toàn bộ các ô UI.
// Điều phối tương tác Kéo Thả (Drag & Drop) và tạo hiệu ứng trực quan Drag Ghost.
[RequireComponent(typeof(Canvas))]
public class InventoryUI : BlitzyUI.Screen
{
    [Header("Inventory & UI References")]
    [Tooltip("Tham chiếu tới Component Inventory cần hiển thị (tự động tìm kiếm nếu để trống).")]
    [SerializeField] private Inventory inventory;

    [Header("Slot Containers")]
    [Tooltip("Transform chứa các ô slot thuộc Hotbar trong túi đồ (Slot index từ 0 đến hotbarSize - 1).")]
    [SerializeField] private Transform hotbarSlotContainer;

    [Tooltip("Transform chứa các ô slot còn lại của túi đồ (Slot index từ hotbarSize đến Capacity - 1).")]
    [SerializeField] private Transform mainSlotContainer;

    [Tooltip("Số lượng ô Hotbar (mặc định: 10).")]
    [SerializeField, Min(1)] private int hotbarSize = 10;

    [Tooltip("Prefab UI của một ô InventorySlotUI.")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Bag Toggle Settings")]
    [Tooltip("Panel UI của túi đồ cần bật/tắt (nếu để trống sẽ tự động điều khiển GameObject này).")]
    [SerializeField] private GameObject bagPanel;

    [Tooltip("Trạng thái hiển thị ban đầu khi bắt đầu game (mặc định: false - đóng).")]
    [SerializeField] private bool startOpen = false;

    [Header("Hotbar UI Reference")]
    [Tooltip("Tham chiếu tới GameObject của Hotbar UI (sẽ ẩn đi khi mở túi đồ và hiện lại khi đóng). Tự động tìm kiếm nếu để trống.")]
    [SerializeField] private GameObject hotbarUI;

    [Header("Drag & Drop Settings")]
    [Tooltip("Canvas gốc chứa giao diện (tự động tìm kiếm nếu để trống).")]
    [SerializeField] private Canvas rootCanvas;

    private readonly List<InventorySlotUI> slotUIList = new List<InventorySlotUI>();
    private bool isInitialized = false;

    // Thành phần trực quan hiển thị icon khi đang kéo (Drag Ghost)
    private Image dragGhostImage;
    private TextMeshProUGUI dragGhostAmountText;
    private RectTransform dragGhostRect;
    private CanvasGroup dragGhostCanvasGroup;

    public static InventoryUI Instance { get; private set; }

    // Sự kiện phát ra khi trạng thái túi đồ thay đổi (true: Mở, false: Đóng)
    public static event Action<bool> OnBagToggled;

    public Inventory Inventory => inventory;
    public bool IsOpen => bagPanel != null ? bagPanel.activeSelf : gameObject.activeSelf;
    public Transform HotbarSlotContainer => hotbarSlotContainer;
    public Transform MainSlotContainer => mainSlotContainer;
    public int HotbarSize => hotbarSize;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (bagPanel == null)
        {
            bagPanel = gameObject;
        }

        ResolveHotbarUI();

        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        if (rootCanvas != null && rootCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            rootCanvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
            inventory.OnInventoryChanged += RefreshAllSlots;
        }

        if (isInitialized)
        {
            RefreshAllSlots();
        }
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshAllSlots;
        }
    }

    #endregion

    #region BlitzyUI.Screen Lifecycle

    public override void OnSetup()
    {
        Instance = this;
        keepCached = true;

        if (bagPanel == null)
        {
            bagPanel = gameObject;
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        ResolveInventory();
        ResolveHotbarUI();
        Initialize();
    }

    public override void OnPush(Data data)
    {
        ResolveInventory();
        if (!isInitialized)
        {
            Initialize();
        }
        OpenBagInternal();

        // BẮT BUỘC: Thông báo hoàn tất mở màn hình cho UIManager
        PushFinished();
    }

    public override void OnPop()
    {
        CloseBagInternal();

        // BẮT BUỘC: Thông báo hoàn tất đóng màn hình cho UIManager
        PopFinished();
    }

    public override void OnFocus()
    {
        RefreshAllSlots();
    }

    public override void OnFocusLost()
    {
        EndDragging();
    }

    #endregion

    #region Bag Visibility Control

    private void ResolveInventory()
    {
        if (inventory == null)
        {
            inventory = Inventory.Instance != null ? Inventory.Instance : FindFirstObjectByType<Inventory>();
            if (inventory != null)
            {
                inventory.OnInventoryChanged -= RefreshAllSlots;
                inventory.OnInventoryChanged += RefreshAllSlots;
            }
        }
    }

    private void ResolveHotbarUI()
    {
        if (hotbarUI == null)
        {
            HotbarUI hb = FindAnyObjectByType<HotbarUI>(FindObjectsInactive.Include);
            if (hb != null)
            {
                hotbarUI = hb.gameObject;
            }
        }
    }

    // Bật/tắt trạng thái hiển thị của túi đồ
    public void ToggleBag()
    {
        if (UIManager.Instance != null)
        {
            if (IsOpen)
            {
                CloseBag();
            }
            else
            {
                OpenBag();
            }
        }
        else
        {
            if (IsOpen)
            {
                CloseBagInternal();
            }
            else
            {
                OpenBagInternal();
            }
        }
    }

    // Mở túi đồ (ưu tiên qua UIManager Queue)
    public void OpenBag()
    {
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.GetScreen(ScreenIds.Inventory) == null)
            {
                UIManager.Instance.QueuePush(ScreenIds.Inventory);
            }
        }
        else
        {
            OpenBagInternal();
        }
    }

    // Đóng túi đồ (ưu tiên qua UIManager Queue)
    public void CloseBag()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.Inventory, include: true);
        }
        else
        {
            CloseBagInternal();
        }
    }

    private void OpenBagInternal()
    {
        if (transform.parent != null && !transform.parent.gameObject.activeSelf)
        {
            transform.parent.gameObject.SetActive(true);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        GameObject target = bagPanel != null ? bagPanel : gameObject;
        if (!target.activeSelf)
        {
            target.SetActive(true);
        }

        ResolveHotbarUI();
        if (UIManager.Instance == null && hotbarUI != null && hotbarUI.activeSelf)
        {
            hotbarUI.SetActive(false);
        }

        RefreshAllSlots();
        OnBagToggled?.Invoke(true);
    }

    private void CloseBagInternal()
    {
        EndDragging();

        GameObject target = bagPanel != null ? bagPanel : gameObject;
        if (target.activeSelf)
        {
            target.SetActive(false);
        }

        ResolveHotbarUI();
        if (UIManager.Instance == null && hotbarUI != null && !hotbarUI.activeSelf)
        {
            hotbarUI.SetActive(true);
        }

        OnBagToggled?.Invoke(false);
    }

    #endregion

    #region UI Setup & Generation

    public void Initialize()
    {
        if (isInitialized) return;

        ResolveInventory();

        if (inventory == null)
        {
            return;
        }

        if (hotbarSlotContainer == null && mainSlotContainer == null)
        {
            Debug.LogError("[InventoryUI] Cần gán ít nhất một Container (hotbarSlotContainer hoặc mainSlotContainer) vào Inspector!", this);
            return;
        }

        if (slotPrefab == null)
        {
            // Thử nạp fallback slotPrefab từ Resources nếu thiếu
            slotPrefab = Resources.Load<InventorySlotUI>("Prefabs/SlotPrefab");
            if (slotPrefab == null)
            {
                Debug.LogError("[InventoryUI] Chưa gán Slot Prefab vào Inspector!", this);
                return;
            }
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        CreateSlots();
        isInitialized = true;
        RefreshAllSlots();
    }

    private void CreateSlots()
    {
        if (hotbarSlotContainer != null)
        {
            foreach (Transform child in hotbarSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (mainSlotContainer != null && mainSlotContainer != hotbarSlotContainer)
        {
            foreach (Transform child in mainSlotContainer)
            {
                Destroy(child.gameObject);
            }
        }

        slotUIList.Clear();

        int effectiveHotbarCount = Mathf.Clamp(hotbarSize, 0, inventory.Capacity);

        for (int i = 0; i < inventory.Capacity; i++)
        {
            Transform targetContainer;
            if (i < effectiveHotbarCount && hotbarSlotContainer != null)
            {
                targetContainer = hotbarSlotContainer;
            }
            else
            {
                targetContainer = mainSlotContainer != null ? mainSlotContainer : hotbarSlotContainer;
            }

            if (targetContainer == null)
            {
                Debug.LogError($"[InventoryUI] Không tìm thấy Container hợp lệ cho Slot Index {i}!", this);
                continue;
            }

            InventorySlotUI slotUI = Instantiate(slotPrefab, targetContainer);

            slotUI.gameObject.SetActive(true);
            slotUI.enabled = true;
            slotUI.EnsureActive();

            slotUI.Setup(i, this);
            slotUIList.Add(slotUI);
        }
    }

    #endregion

    #region UI Refresh

    public void RefreshAllSlots()
    {
        if (inventory == null || slotUIList == null || slotUIList.Count == 0)
            return;

        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i < inventory.Capacity)
            {
                InventorySlot slotData = inventory.GetSlot(i);
                slotUIList[i].Refresh(slotData);
            }
        }
    }

    #endregion

    #region Drag & Drop Management

    public bool HasItemInSlot(int slotIndex)
    {
        if (inventory == null || !inventory.IsValidIndex(slotIndex))
            return false;

        InventorySlot slot = inventory.GetSlot(slotIndex);
        return slot != null && !slot.IsEmpty();
    }

    public void StartDragging(Sprite icon, int amount, PointerEventData eventData)
    {
        if (icon == null) return;
        EnsureDragGhost();

        if (dragGhostImage != null)
        {
            dragGhostImage.sprite = icon;
            dragGhostImage.raycastTarget = false;

            if (dragGhostCanvasGroup != null)
            {
                dragGhostCanvasGroup.blocksRaycasts = false;
                dragGhostCanvasGroup.interactable = false;
            }

            if (dragGhostAmountText != null)
            {
                dragGhostAmountText.text = amount > 1 ? amount.ToString() : "";
            }

            dragGhostImage.gameObject.SetActive(true);
            dragGhostRect.SetAsLastSibling();

            UpdateDragPosition(eventData);
        }
    }

    public void StartDragging(int slotIndex, PointerEventData eventData)
    {
        if (!HasItemInSlot(slotIndex)) return;

        InventorySlot slot = inventory.GetSlot(slotIndex);
        if (slot != null && slot.ItemData != null)
        {
            StartDragging(slot.ItemData.Icon, slot.Amount, eventData);
        }
    }

    public void OnDragging(PointerEventData eventData)
    {
        UpdateDragPosition(eventData);
    }

    public void EndDragging()
    {
        if (dragGhostImage != null)
        {
            dragGhostImage.gameObject.SetActive(false);
        }

        RefreshAllSlots();
    }

    public void HandleDrop(int fromSlotIndex, int toSlotIndex)
    {
        if (inventory == null || fromSlotIndex == toSlotIndex)
            return;

        inventory.MoveOrSwapSlots(fromSlotIndex, toSlotIndex);
    }

    private void EnsureDragGhost()
    {
        if (dragGhostImage != null) return;

        Transform parentTransform = rootCanvas != null ? rootCanvas.transform : transform;

        GameObject ghostObj = new GameObject("Inventory_DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        ghostObj.transform.SetParent(parentTransform, false);

        dragGhostImage = ghostObj.GetComponent<Image>();
        dragGhostImage.raycastTarget = false;
        dragGhostImage.color = Color.white;

        dragGhostCanvasGroup = ghostObj.GetComponent<CanvasGroup>();
        dragGhostCanvasGroup.blocksRaycasts = false;
        dragGhostCanvasGroup.interactable = false;

        dragGhostRect = ghostObj.GetComponent<RectTransform>();
        dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
        dragGhostRect.sizeDelta = new Vector2(50f, 50f);

        GameObject textObj = new GameObject("DragAmountText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(ghostObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(1, 0);
        textRect.anchoredPosition = new Vector2(0, 0);
        textRect.sizeDelta = new Vector2(50f, 30f);

        dragGhostAmountText = textObj.GetComponent<TextMeshProUGUI>();
        dragGhostAmountText.fontSize = 16;
        dragGhostAmountText.alignment = TextAlignmentOptions.BottomRight;
        dragGhostAmountText.raycastTarget = false;
        dragGhostAmountText.enableWordWrapping = false;

        if (dragGhostAmountText.fontMaterial != null)
        {
            dragGhostAmountText.fontMaterial.EnableKeyword("OUTLINE_ON");
            dragGhostAmountText.outlineWidth = 0.2f;
            dragGhostAmountText.outlineColor = Color.black;
        }

        ghostObj.SetActive(false);
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || rootCanvas == null) return;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragGhostRect.position = eventData.position;
        }
        else
        {
            Camera eventCamera = eventData.pressEventCamera ?? rootCanvas.worldCamera ?? Camera.main;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                eventData.position,
                eventCamera,
                out Vector2 localPos))
            {
                dragGhostRect.localPosition = localPos;
            }
        }
    }

    #endregion
}
