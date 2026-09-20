using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Script tiện ích hỗ trợ kiểm thử và demo các tính năng của hệ thống Inventory / Hotbar trong Unity Editor / Runtime.
// Tự động nạp các công cụ vào Hotbar khi bắt đầu game và hỗ trợ phím tắt Q để thêm hạt giống.
public class InventoryTester : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Inventory chính.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới HotbarController để lấy ô đang active.")]
    [SerializeField] private HotbarController hotbarController;

    [Header("Item Test Data Assets")]
    [FormerlySerializedAs("dog")]
    [SerializeField] private ItemData hoe;
    [SerializeField] private ItemData wateringCan;
    [SerializeField] private ItemData axe;
    [SerializeField] private ItemData pickaxe;
    [SerializeField] private ItemData shovel;
    [SerializeField] private ItemData hammer;
    [SerializeField] private ItemData seed;

    [Header("Test Settings")]
    [Tooltip("Số lượng hạt giống thêm mỗi lần nhấn Q.")]
    [SerializeField, Min(1)] private int seedAmount = 10;

    [Tooltip("Tự động thêm công cụ vào Hotbar khi bắt đầu game.")]
    [SerializeField] private bool autoAddToolsOnStart = true;

    [Header("Item Drop Testing")]
    [Tooltip("Phím tắt để thử nghiệm rơi vật phẩm tại vị trí người chơi (Mặc định: G).")]
    [SerializeField] private KeyCode dropTestKey = KeyCode.G;

    private void Awake()
    {
        // Tự động tìm kiếm nếu chưa gán tham chiếu trong Inspector
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }

        ResolveItems();
    }

    private void Start()
    {
        ResolveItems();

        if (autoAddToolsOnStart)
        {
            AutoAddToolsToHotbar();
        }

        EnsureDropComponentsExist();
    }

    private void Update()
    {
        // Nhấn phím Q: Thêm hạt giống vào ô đang chọn (hoặc ô trống kế tiếp nếu ô active không hợp lệ)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AddSeed();
        }

        // Nhấn phím G: Thử nghiệm ném rơi vật phẩm tại vị trí người chơi
        if (Input.GetKeyDown(dropTestKey))
        {
            TestSpawnDropAtPlayer();
        }
    }

    #region Auto Add Tools

    // Tự động thêm lần lượt các công cụ vào các ô hotbar đầu tiên khi vào game.
    [ContextMenu("Tools/Add All Tools to Hotbar")]
    public void AutoAddToolsToHotbar()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
            if (inventory == null)
            {
                Debug.LogWarning("[InventoryTester] Không tìm thấy Inventory để thêm công cụ vào Hotbar!", this);
                return;
            }
        }

        // Danh sách công cụ cần thêm vào Hotbar
        ItemData[] defaultTools = new ItemData[] { hoe, wateringCan, axe, pickaxe, shovel, hammer };
        int slotIndex = 0;

        foreach (var tool in defaultTools)
        {
            if (tool == null) continue;

            // Nếu công cụ này đã có trong Inventory, không thêm lặp lại
            if (inventory.HasItem(tool, 1))
            {
                slotIndex++;
                continue;
            }

            // Đặt vào ô slotIndex của Hotbar nếu hợp lệ và đang trống
            if (slotIndex < inventory.Capacity)
            {
                InventorySlot slot = inventory.GetSlot(slotIndex);
                if (slot != null && slot.IsEmpty())
                {
                    inventory.AddItemToSlot(slotIndex, tool, 1);
                }
                else
                {
                    inventory.TryAddItem(tool, 1);
                }
            }
            else
            {
                inventory.TryAddItem(tool, 1);
            }

            slotIndex++;
        }
    }

    #endregion

    #region Item Resolution

    private void ResolveItems()
    {
#if UNITY_EDITOR
        if (hoe == null || seed == null || wateringCan == null || axe == null || pickaxe == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null) continue;

                MatchAndAssignItem(item);
            }
        }
#endif

        if (hoe == null || seed == null || wateringCan == null || axe == null || pickaxe == null)
        {
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var item in allItems)
            {
                if (item == null) continue;
                MatchAndAssignItem(item);
            }
        }
    }

    private void MatchAndAssignItem(ItemData item)
    {
        if (item == null) return;
        string id = item.ItemID != null ? item.ItemID.ToLower() : "";
        string name = item.name.ToLower();

        if (hoe == null && (item.ToolType == ToolType.Hoe || id.Contains("hoe") || name.Contains("hoe"))) hoe = item;
        if (wateringCan == null && (item.ToolType == ToolType.WateringCan || id.Contains("water") || name.Contains("water"))) wateringCan = item;
        if (axe == null && (item.ToolType == ToolType.Axe || id.Contains("axe") || name.Contains("axe"))) axe = item;
        if (pickaxe == null && (item.ToolType == ToolType.Pickaxe || id.Contains("pickaxe") || name.Contains("pick") || name.Contains("pickaxe"))) pickaxe = item;
        if (shovel == null && (item.ToolType == ToolType.Shovel || id.Contains("shovel") || name.Contains("shovel"))) shovel = item;
        if (hammer == null && (id.Contains("hammer") || name.Contains("hammer"))) hammer = item;
        if (seed == null && (item.ItemType == ItemType.Seed || id.Contains("seed") || name.Contains("seed"))) seed = item;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveItems();
    }
#endif

    #endregion

    #region Context Menu & Public Test Actions

    [ContextMenu("Q. Add Seed to Active Slot")]
    public void AddSeed()
    {
        if (seed == null)
        {
            Debug.LogError("[InventoryTester] ItemData seed đang bị null!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory != null ? inventory.GetSlot(activeIndex) : null;

        // Nếu ô đang active trống hoặc cùng loại seed thì thêm thẳng vào ô này
        if (slot != null && (slot.IsEmpty() || slot.CanStack(seed)))
        {
            ExecuteAddItemToActiveSlot(seed, seedAmount);
        }
        else
        {
            // Nếu ô đang chọn đã bận (ví dụ đang chọn ô chứa công cụ), tìm ô trống đầu tiên để thêm hạt giống
            if (inventory != null)
            {
                int remaining = inventory.AddItem(seed, seedAmount);
                int added = seedAmount - remaining;
                if (added > 0)
                {
                    Debug.Log($"<color=green>[InventoryTester] Ô đang chọn (Slot {activeIndex + 1}) đã có '{slot?.ItemData?.ItemName}', tự động thêm {added}x '{seed.ItemName}' vào ô trống khả dụng.</color>");
                }
                else
                {
                    Debug.LogWarning("[InventoryTester] Kho đồ đã đầy, không thể thêm hạt giống!", this);
                }
            }
        }
    }

    [ContextMenu("Tools/Add Hoe to Active Slot")]
    public void AddHoe()
    {
        ExecuteAddItemToActiveSlot(hoe, 1);
    }

    [ContextMenu("Tools/Add Watering Can to Active Slot")]
    public void AddWateringCan()
    {
        ExecuteAddItemToActiveSlot(wateringCan, 1);
    }

    [ContextMenu("Tools/Add Axe to Active Slot")]
    public void AddAxe()
    {
        ExecuteAddItemToActiveSlot(axe, 1);
    }

    [ContextMenu("Tools/Add Pickaxe to Active Slot")]
    public void AddPickaxe()
    {
        ExecuteAddItemToActiveSlot(pickaxe, 1);
    }

    [ContextMenu("Tools/Add Shovel to Active Slot")]
    public void AddShovel()
    {
        ExecuteAddItemToActiveSlot(shovel, 1);
    }

    [ContextMenu("Remove 5 Items from Active Slot")]
    public void RemoveFromActiveSlot(int amount = 5)
    {
        ExecuteRemoveItemFromActiveSlot(amount);
    }

    [ContextMenu("Clear Active Slot")]
    public void ClearActiveSlot()
    {
        if (inventory == null) return;

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null || slot.IsEmpty())
        {
            Debug.Log($"<color=yellow>[InventoryTester] Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) vốn đang rỗng.</color>");
            return;
        }

        string removedName = slot.ItemData.ItemName;
        int removedCount = slot.Amount;

        inventory.ClearSlot(activeIndex);
        Debug.Log($"<color=yellow>[InventoryTester] Đã xóa sạch Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) - Từng chứa {removedCount}x '{removedName}'.</color>");
    }

    [ContextMenu("0. Clear Entire Inventory")]
    public void ClearAll()
    {
        if (inventory == null) return;
        inventory.ClearAll();
        Debug.Log("<color=yellow>[InventoryTester] Đã dọn sạch toàn bộ Inventory.</color>");
    }

    #endregion

    #region Execution Helpers

    private int GetActiveSlotIndex()
    {
        if (hotbarController != null)
        {
            return hotbarController.SelectedSlotIndex;
        }

        return 0;
    }

    private void ExecuteAddItemToActiveSlot(ItemData item, int amount)
    {
        if (inventory == null)
        {
            Debug.LogError("[InventoryTester] Chưa gán tham chiếu Inventory!", this);
            return;
        }

        if (item == null)
        {
            Debug.LogError("[InventoryTester] ItemData đang bị null!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null)
        {
            Debug.LogError($"[InventoryTester] Không tìm thấy Slot tại index {activeIndex}!", this);
            return;
        }

        // Kiểm tra nếu ô đang chứa item loại khác
        if (!slot.IsEmpty() && slot.ItemData.ItemID != item.ItemID)
        {
            Debug.LogWarning($"<color=orange>[InventoryTester] Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) đang chứa '{slot.ItemData.ItemName}'. Không thể thêm '{item.ItemName}' vào ô này!</color>");
            return;
        }

        int remaining = inventory.AddItemToSlot(activeIndex, item, amount);
        int added = amount - remaining;

        if (added > 0)
        {
            Debug.Log($"<color=green>[InventoryTester] Thêm thành công {added}x '{item.ItemName}' vào Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}). Số lượng hiện tại trong ô: {slot.Amount}/{slot.ItemData.MaxStack}.</color>");
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"<color=orange>[InventoryTester] Còn dư {remaining}x '{item.ItemName}' do Slot Hotbar [{activeIndex + 1}] đã đạt MaxStack ({slot.ItemData.MaxStack}).</color>");
        }
    }

    private void ExecuteRemoveItemFromActiveSlot(int amount)
    {
        if (inventory == null)
        {
            Debug.LogError("[InventoryTester] Chưa gán tham chiếu Inventory!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory.GetSlot(activeIndex);

        if (slot == null || slot.IsEmpty())
        {
            Debug.LogWarning($"<color=red>[InventoryTester] Thao tác trừ thất bại! Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}) hiện đang rỗng.</color>");
            return;
        }

        string itemName = slot.ItemData.ItemName;
        int currentAmount = slot.Amount;
        int removed = inventory.RemoveItemFromSlot(activeIndex, amount);

        if (removed > 0)
        {
            int remainingInSlot = slot.IsEmpty() ? 0 : slot.Amount;
            Debug.Log($"<color=cyan>[InventoryTester] Đã trừ {removed}x '{itemName}' khỏi Slot Hotbar [{activeIndex + 1}] (Index {activeIndex}). Còn lại trong ô: {remainingInSlot}.</color>");
        }
    }

    #endregion

    #region Drop Testing & Component Setup

    // Tự động đảm bảo scene có sẵn ItemDropSpawner và Player có ItemCollector
    private void EnsureDropComponentsExist()
    {
        if (FindAnyObjectByType<ItemDropSpawner>() == null)
        {
            GameObject spawnerObj = new GameObject("[ItemDropSpawner]");
            spawnerObj.AddComponent<ItemDropSpawner>();
        }

        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player != null && player.GetComponent<ItemCollector>() == null)
        {
            player.gameObject.AddComponent<ItemCollector>();
        }
    }

    // Thử nghiệm sinh vật phẩm rơi văng ra gần người chơi
    [ContextMenu("Drops/Test Spawn Drop At Player (Key G)")]
    public void TestSpawnDropAtPlayer()
    {
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        Vector3 spawnPos = player != null ? player.transform.position : transform.position;

        ItemData testItem = seed != null ? seed : hoe;
        if (testItem == null)
        {
            Debug.LogWarning("[InventoryTester] Không tìm thấy ItemData để test drop!", this);
            return;
        }

        if (ItemDropSpawner.Instance != null)
        {
            ItemDropSpawner.Instance.SpawnDrop(testItem, 2, spawnPos, 1.2f);
            Debug.Log($"<color=green>[InventoryTester] Đã spawn rơi vật phẩm '{testItem.ItemName}' tại {spawnPos} (Phím G).</color>");
        }
    }

    #endregion
}
