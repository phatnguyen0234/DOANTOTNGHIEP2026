using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Script tiện ích hỗ trợ kiểm thử và demo các tính năng của hệ thống Inventory / Hotbar trong Unity Editor / Runtime.
// Tự động nạp danh sách công cụ vào Hotbar (tràn sang túi đồ nếu vượt quá số slot Hotbar) và nạp danh sách vật phẩm stackable.
public class InventoryTester : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Inventory chính.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới HotbarController để lấy ô đang active.")]
    [SerializeField] private HotbarController hotbarController;

    [Header("Item Test Data Assets")]
    [Tooltip("Danh sách các công cụ test (Cuốc, Bình tưới, Rìu, Xẻng, Búa, Cần câu,...).")]
    [SerializeField] private List<ItemData> toolList = new List<ItemData>();

    [Tooltip("Danh sách các vật phẩm test có thể stack được (Hạt giống, Nông sản, Tài nguyên, Gỗ, Đá,...).")]
    [SerializeField] private List<ItemData> stackableItemList = new List<ItemData>();

    [Tooltip("Hạt giống Blueberry / BubbleBerry dùng để test bấm phím B (tự tìm nếu để trống).")]
    [SerializeField] private ItemData blueberrySeed;

    [Header("Test Settings")]
    [Tooltip("Số lượng vật phẩm stack thêm mỗi lần nhấn Q hoặc nạp test ban đầu.")]
    [FormerlySerializedAs("seedAmount")]
    [SerializeField, Min(1)] private int stackableItemAmount = 10;

    [Tooltip("Tự động thêm công cụ và vật phẩm test khi bắt đầu game.")]
    [FormerlySerializedAs("autoAddToolsOnStart")]
    [SerializeField] private bool autoAddTestDataOnStart = true;

    [Tooltip("Tự động nạp luôn danh sách vật phẩm stackable khi bắt đầu game.")]
    [SerializeField] private bool autoAddStackableItemsOnStart = true;

    [Tooltip("Ưu tiên đưa vật phẩm stackable vào túi đồ (Bag) thay vì các ô trống còn lại trên Hotbar.")]
    [SerializeField] private bool placeStackablesInBagFirst = false;

    [Header("Item Drop Testing")]
    [Tooltip("Phím tắt để thử nghiệm rơi vật phẩm tại vị trí người chơi (Mặc định: G).")]
    [SerializeField] private KeyCode dropTestKey = KeyCode.G;

    #region Legacy Fields (Hỗ trợ tương thích ngược cho Scene/Prefab cũ)
    [HideInInspector] [SerializeField] private ItemData hoe;
    [HideInInspector] [SerializeField] private ItemData wateringCan;
    [HideInInspector] [SerializeField] private ItemData axe;
    [HideInInspector] [SerializeField] private ItemData pickaxe;
    [HideInInspector] [SerializeField] private ItemData shovel;
    [HideInInspector] [SerializeField] private ItemData hammer;
    [HideInInspector] [SerializeField] private ItemData seed;
    #endregion

    #region Properties
    public List<ItemData> ToolList => toolList;
    public List<ItemData> StackableItemList => stackableItemList;
    #endregion

    private void Awake()
    {
        EnsureDependencies();
        MigrateLegacyFields();
        ResolveItems();
    }

    private void Start()
    {
        EnsureDependencies();
        MigrateLegacyFields();
        ResolveItems();

        if (autoAddTestDataOnStart)
        {
            AutoPopulateAllTestData();
        }

        EnsureDropComponentsExist();
    }

    private void Update()
    {
        // Nhấn phím Q: Thêm hạt giống Carrot (hoặc vật phẩm stackable đầu tiên)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AddSeed();
        }

        // Nhấn phím B: Thêm hạt giống Blueberry (BubbleBerry)
        if (Input.GetKeyDown(KeyCode.B))
        {
            AddBlueberrySeed();
        }

        // Nhấn phím G: Thử nghiệm ném rơi vật phẩm tại vị trí người chơi
        if (Input.GetKeyDown(dropTestKey))
        {
            TestSpawnDropAtPlayer();
        }
    }

    private void EnsureDependencies()
    {
        if (inventory == null)
        {
            inventory = Inventory.Instance ?? FindAnyObjectByType<Inventory>();
        }

        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }
    }

    #region Auto Add Test Data (Tools First, Overflow to Bag)

    // Tự động nạp toàn bộ dữ liệu test: Ưu tiên công cụ vào Hotbar trước, nếu nhiều hơn thì vào túi đồ; sau đó nạp vật phẩm stackable.
    [ContextMenu("Test Data/Add All Test Data (Tools First, Then Items)")]
    public void AutoPopulateAllTestData()
    {
        AutoAddTools();

        if (autoAddStackableItemsOnStart)
        {
            AutoAddStackableItems();
        }
    }

    // Đưa danh sách công cụ vào trước. Nếu danh sách nhiều hơn slot trong HotBar thì đưa vào túi đồ inventory.
    [ContextMenu("Tools/Add All Tools (Hotbar First, Overflow to Bag)")]
    public void AutoAddTools()
    {
        EnsureDependencies();
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryTester] Không tìm thấy Inventory để thêm công cụ!", this);
            return;
        }

        int hotbarSize = GetHotbarSize();

        foreach (var tool in toolList)
        {
            if (tool == null) continue;

            // Nếu công cụ này đã có trong Inventory, không thêm lặp lại
            if (inventory.HasItem(tool, 1)) continue;

            // 1. Tìm ô trống trong Hotbar trước (index 0 đến hotbarSize - 1)
            int hotbarSlot = FindFirstEmptySlot(0, hotbarSize);
            if (hotbarSlot != -1)
            {
                inventory.AddItemToSlot(hotbarSlot, tool, 1);
                Debug.Log($"<color=green>[InventoryTester] Đã thêm công cụ '{tool.ItemName}' vào Hotbar [Slot {hotbarSlot + 1}].</color>");
            }
            else
            {
                // 2. Nếu danh sách công cụ nhiều hơn slot trong HotBar thì đưa vào túi đồ inventory (từ hotbarSize trở đi)
                int bagSlot = FindFirstEmptySlot(hotbarSize, inventory.Capacity);
                if (bagSlot != -1)
                {
                    inventory.AddItemToSlot(bagSlot, tool, 1);
                    Debug.Log($"<color=cyan>[InventoryTester] Hotbar đã đầy slot. Đã đưa công cụ '{tool.ItemName}' vào Túi đồ Inventory [Slot {bagSlot + 1}].</color>");
                }
                else
                {
                    // Fallback nếu cả túi đồ cũng kín
                    if (!inventory.TryAddItem(tool, 1))
                    {
                        Debug.LogWarning($"<color=red>[InventoryTester] Không thể thêm công cụ '{tool.ItemName}', toàn bộ kho đồ đã đầy!</color>");
                    }
                }
            }
        }
    }

    // Nạp danh sách vật phẩm stack được vào Inventory
    [ContextMenu("Items/Add All Stackable Items")]
    public void AutoAddStackableItems()
    {
        EnsureDependencies();
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryTester] Không tìm thấy Inventory để thêm vật phẩm!", this);
            return;
        }

        int hotbarSize = GetHotbarSize();

        foreach (var item in stackableItemList)
        {
            if (item == null) continue;

            // Tránh thêm trùng lặp nếu item đã có trong kho khi nạp ban đầu
            if (inventory.HasItem(item, 1)) continue;

            int amount = stackableItemAmount > 0 ? Mathf.Min(stackableItemAmount, item.MaxStack) : item.MaxStack;

            if (placeStackablesInBagFirst)
            {
                int bagSlot = FindFirstEmptySlot(hotbarSize, inventory.Capacity);
                if (bagSlot != -1)
                {
                    inventory.AddItemToSlot(bagSlot, item, amount);
                    Debug.Log($"<color=green>[InventoryTester] Đã thêm vật phẩm '{item.ItemName}' (x{amount}) vào Túi đồ Inventory [Slot {bagSlot + 1}].</color>");
                    continue;
                }
            }

            // Mặc định: Điền vào các ô trống khả dụng còn lại (Hotbar nếu còn, sau đó tràn vào Túi đồ)
            int remaining = inventory.AddItem(item, amount);
            int added = amount - remaining;
            if (added > 0)
            {
                Debug.Log($"<color=green>[InventoryTester] Đã thêm {added}x '{item.ItemName}' vào kho đồ.</color>");
            }
            else
            {
                Debug.LogWarning($"<color=red>[InventoryTester] Kho đồ đã đầy, không thể thêm '{item.ItemName}'!</color>");
            }
        }
    }

    // Alias để giữ tương thích ngược với code/context menu cũ
    [ContextMenu("Tools/Add All Tools to Hotbar")]
    public void AutoAddToolsToHotbar()
    {
        AutoAddTools();
    }

    #endregion

    #region Slot Finding Helpers

    // Lấy số lượng slot của Hotbar (từ HotbarController hoặc mặc định là 10)
    public int GetHotbarSize()
    {
        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }

        if (hotbarController != null)
        {
            return hotbarController.GetEffectiveHotbarSize();
        }

        return 10;
    }

    // Tìm ô trống đầu tiên trong phạm vi [startIndex, endIndex)
    private int FindFirstEmptySlot(int startIndex, int endIndex)
    {
        if (inventory == null) return -1;

        int max = Mathf.Min(endIndex, inventory.Capacity);
        for (int i = startIndex; i < max; i++)
        {
            InventorySlot slot = inventory.GetSlot(i);
            if (slot != null && slot.IsEmpty())
            {
                return i;
            }
        }

        return -1;
    }

    #endregion

    #region Item Resolution & Migration

    // Tự động chuyển đổi dữ liệu từ các biến cũ (hoe, axe, seed,...) sang 2 danh sách mới nếu danh sách đang rỗng
    private void MigrateLegacyFields()
    {
        if (toolList == null) toolList = new List<ItemData>();
        if (stackableItemList == null) stackableItemList = new List<ItemData>();

        ItemData[] legacyTools = new ItemData[] { hoe, wateringCan, axe, pickaxe, shovel, hammer };
        foreach (var legacyTool in legacyTools)
        {
            if (legacyTool != null && !toolList.Contains(legacyTool))
            {
                toolList.Add(legacyTool);
            }
        }

        if (seed != null && !stackableItemList.Contains(seed))
        {
            stackableItemList.Add(seed);
        }
    }

    private void ResolveItems()
    {
#if UNITY_EDITOR
        if (toolList.Count == 0 || stackableItemList.Count == 0)
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

        if (toolList.Count == 0 || stackableItemList.Count == 0)
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

        bool isTool = item.IsTool || item.ToolType != ToolType.None ||
                      id.Contains("hoe") || name.Contains("hoe") ||
                      id.Contains("water") || name.Contains("water") ||
                      id.Contains("axe") || name.Contains("axe") ||
                      id.Contains("pick") || name.Contains("pick") ||
                      id.Contains("shovel") || name.Contains("shovel") ||
                      id.Contains("hammer") || name.Contains("hammer") ||
                      id.Contains("rod") || name.Contains("rod");

        if (isTool)
        {
            if (!toolList.Contains(item))
            {
                toolList.Add(item);
            }
        }
        else if (item.MaxStack > 1 || item.IsSeed || item.IsCrop || item.ItemType == ItemType.Resource)
        {
            if (!stackableItemList.Contains(item))
            {
                stackableItemList.Add(item);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        MigrateLegacyFields();
        ResolveItems();
    }
#endif

    #endregion

    #region Context Menu & Public Test Actions

    // Lấy vật phẩm stackable đầu tiên (ưu tiên hạt giống seed nếu có)
    private ItemData GetFirstStackableItem()
    {
        if (stackableItemList != null && stackableItemList.Count > 0)
        {
            var seedItem = stackableItemList.Find(x => x != null && x.IsSeed);
            if (seedItem != null) return seedItem;

            var firstValid = stackableItemList.Find(x => x != null);
            if (firstValid != null) return firstValid;
        }

        if (seed != null) return seed;
        return null;
    }

    [ContextMenu("Q. Add Stackable Item to Active Slot")]
    public void AddSeed()
    {
        ItemData itemToAdd = GetFirstStackableItem();
        if (itemToAdd == null)
        {
            Debug.LogError("[InventoryTester] Danh sách vật phẩm stackable đang rỗng!", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory != null ? inventory.GetSlot(activeIndex) : null;

        // Nếu ô đang active trống hoặc cùng loại thì thêm thẳng vào ô này
        if (slot != null && (slot.IsEmpty() || slot.CanStack(itemToAdd)))
        {
            ExecuteAddItemToActiveSlot(itemToAdd, stackableItemAmount);
        }
        else
        {
            // Nếu ô đang chọn đã bận (ví dụ đang chọn ô chứa công cụ), tìm ô trống đầu tiên để thêm
            if (inventory != null)
            {
                int remaining = inventory.AddItem(itemToAdd, stackableItemAmount);
                int added = stackableItemAmount - remaining;
                if (added > 0)
                {
                    Debug.Log($"<color=green>[InventoryTester] Ô đang chọn (Slot {activeIndex + 1}) đã có '{slot?.ItemData?.ItemName}', tự động thêm {added}x '{itemToAdd.ItemName}' vào ô trống khả dụng.</color>");
                }
                else
                {
                    Debug.LogWarning($"[InventoryTester] Kho đồ đã đầy, không thể thêm '{itemToAdd.ItemName}'!", this);
                }
            }
        }
    }

    // Lấy ItemData của Blueberry / BubbleBerry (nếu chưa gán sẽ tự động tìm kiếm)
    private ItemData GetBlueberryItem()
    {
        if (blueberrySeed != null) return blueberrySeed;

        if (stackableItemList != null)
        {
            var found = stackableItemList.Find(x => x != null && (
                (!string.IsNullOrEmpty(x.CropId) && x.CropId.ToLower().Contains("berry")) ||
                x.name.ToLower().Contains("berry") ||
                (!string.IsNullOrEmpty(x.ItemID) && x.ItemID.ToLower().Contains("berry")) ||
                (x.CropData != null && !string.IsNullOrEmpty(x.CropData.cropName) && x.CropData.cropName.ToLower().Contains("berry"))
            ));
            if (found != null)
            {
                blueberrySeed = found;
                return blueberrySeed;
            }
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && (
                item.name.ToLower().Contains("bubbleberry") ||
                item.name.ToLower().Contains("blueberry") ||
                (!string.IsNullOrEmpty(item.CropId) && item.CropId.ToLower().Contains("blueberry"))
            ))
            {
                blueberrySeed = item;
                return blueberrySeed;
            }
        }
#endif

        return null;
    }

    [ContextMenu("B. Add Blueberry Seed to Active Slot")]
    public void AddBlueberrySeed()
    {
        ItemData itemToAdd = GetBlueberryItem();
        if (itemToAdd == null)
        {
            Debug.LogError("[InventoryTester] Không tìm thấy ItemData của Blueberry / BubbleBerry! Hãy gán vào trường 'Blueberry Seed' trong Inspector.", this);
            return;
        }

        int activeIndex = GetActiveSlotIndex();
        InventorySlot slot = inventory != null ? inventory.GetSlot(activeIndex) : null;

        // Nếu ô đang active trống hoặc cùng loại thì thêm thẳng vào ô này
        if (slot != null && (slot.IsEmpty() || slot.CanStack(itemToAdd)))
        {
            ExecuteAddItemToActiveSlot(itemToAdd, stackableItemAmount);
        }
        else
        {
            // Nếu ô đang chọn đã bận, tự động thêm vào ô trống đầu tiên
            if (inventory != null)
            {
                int remaining = inventory.AddItem(itemToAdd, stackableItemAmount);
                int added = stackableItemAmount - remaining;
                if (added > 0)
                {
                    string itemName = !string.IsNullOrEmpty(itemToAdd.ItemName) ? itemToAdd.ItemName : itemToAdd.name;
                    Debug.Log($"<color=green>[InventoryTester] Ô đang chọn (Slot {activeIndex + 1}) đã có '{slot?.ItemData?.ItemName}', tự động thêm {added}x '{itemName}' vào ô trống khả dụng.</color>");
                }
                else
                {
                    string itemName = !string.IsNullOrEmpty(itemToAdd.ItemName) ? itemToAdd.ItemName : itemToAdd.name;
                    Debug.LogWarning($"[InventoryTester] Kho đồ đã đầy, không thể thêm '{itemName}'!", this);
                }
            }
        }
    }

    [ContextMenu("Tools/Add First Tool to Active Slot")]
    public void AddFirstToolToActiveSlot()
    {
        if (toolList != null && toolList.Count > 0 && toolList[0] != null)
        {
            ExecuteAddItemToActiveSlot(toolList[0], 1);
        }
        else
        {
            Debug.LogWarning("[InventoryTester] Danh sách công cụ đang rỗng!", this);
        }
    }

    [ContextMenu("Tools/Add Hoe to Active Slot")]
    public void AddHoe() => AddToolByType(ToolType.Hoe, "hoe");

    [ContextMenu("Tools/Add Watering Can to Active Slot")]
    public void AddWateringCan() => AddToolByType(ToolType.WateringCan, "water");

    [ContextMenu("Tools/Add Axe to Active Slot")]
    public void AddAxe() => AddToolByType(ToolType.Axe, "axe");

    [ContextMenu("Tools/Add Pickaxe to Active Slot")]
    public void AddPickaxe() => AddToolByType(ToolType.Pickaxe, "pick");

    [ContextMenu("Tools/Add Shovel to Active Slot")]
    public void AddShovel() => AddToolByType(ToolType.Shovel, "shovel");

    private void AddToolByType(ToolType type, string keyword)
    {
        ItemData tool = toolList.Find(t => t != null && (t.ToolType == type || t.name.ToLower().Contains(keyword)));
        if (tool != null)
        {
            ExecuteAddItemToActiveSlot(tool, 1);
        }
        else
        {
            Debug.LogWarning($"[InventoryTester] Không tìm thấy công cụ loại {type} ({keyword}) trong danh sách toolList!", this);
        }
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
        EnsureDependencies();
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
        EnsureDependencies();
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

    private ItemData GetDefaultTestItem()
    {
        ItemData item = GetFirstStackableItem();
        if (item != null) return item;

        if (toolList != null && toolList.Count > 0)
        {
            var firstTool = toolList.Find(x => x != null);
            if (firstTool != null) return firstTool;
        }

        return hoe != null ? hoe : null;
    }

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

        ItemData testItem = GetDefaultTestItem();
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
