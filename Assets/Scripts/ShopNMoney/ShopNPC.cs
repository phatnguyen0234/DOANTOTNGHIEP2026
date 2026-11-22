using System.Collections.Generic;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName;

    [Header("1. Permanent Stock (Always Available)")]
    [Tooltip("Items permanently sold year-round regardless of season or day (subject to unlock conditions).")]
    [SerializeField] private List<ShopItem> permanentItems = new();

    [Header("2. Seasonal Stock (Refreshes Every Season)")]
    [Tooltip("Items available throughout matching seasons. Refreshes only when the season changes.")]
    [SerializeField] private List<ShopItem> seasonalItems = new();

    [Header("3. Daily Rotating Pool (Refreshes Every N Days)")]
    [Tooltip("Candidate pool for daily rotating items. Order must match between Server and Client.")]
    [SerializeField] private List<ShopItem> dailyPoolItems = new();

    [Tooltip("Number of random daily items displayed per rotation.")]
    [SerializeField] private int itemsPerRotation = 4;

    [Tooltip("Number of in-game days before the daily stock refreshes.")]
    [Min(1)]
    [SerializeField] private int daysBetweenRefresh = 1;

    [Header("Runtime Unlock Tracking")]
    [Tooltip("Caches material ItemIDs obtained by the player so unlocked items remain unlocked even if materials are later spent.")]
    [SerializeField] private List<string> obtainedMaterialIds = new();

    // Runtime state split by lifecycle bucket (Owned by Server in Multiplayer)
    private readonly List<ShopItem> permanentStock = new();
    private readonly List<ShopItem> seasonalStock = new();
    private readonly List<ShopItem> currentDailyStock = new();
    private readonly List<ShopItem> combinedDisplayStock = new();
    private readonly HashSet<string> activeHolidayIds = new();

    private Season currentSeason = Season.Spring;
    private int currentRotationSeed;
    private int daysSinceLastRefresh = 0;
    private bool isPlayerInRange;
    private bool isSubscribedToDayEvent;
    private bool isSubscribedToSeasonEvent;
    private bool isSubscribedToHolidayEvent;
    private bool isSubscribedToInventoryEvent;

    public string NpcName => string.IsNullOrEmpty(npcName) ? gameObject.name : npcName;

    private void OnEnable()
    {
        TrySubscribeToExternalEvents();
    }

    private void Start()
    {
        // Fallback subscription in case Managers initialized after ShopNPC.OnEnable()
        TrySubscribeToExternalEvents();

        Season initialSeason = TimeManager.Instance != null
            ? TimeManager.Instance.currentSeason
            : Season.Spring;

        // Note: In Multiplayer, wrap this in: if (IsServer) { ... }
        InitializeAllBuckets(initialSeason);
        SyncObtainedMaterialsFromInventory();
    }

    private void OnDisable()
    {
        if (isSubscribedToSeasonEvent)
        {
            TimeManager.OnSeasonChanged -= HandleSeasonChanged;
            isSubscribedToSeasonEvent = false;
        }

        if (isSubscribedToHolidayEvent)
        {
            TimeManager.OnHolidayTriggered -= HandleHolidayTriggered;
            isSubscribedToHolidayEvent = false;
        }

        if (isSubscribedToDayEvent && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay -= HandleNewDay;
            isSubscribedToDayEvent = false;
        }

        if (isSubscribedToInventoryEvent && Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
            isSubscribedToInventoryEvent = false;
        }
    }

    private void TrySubscribeToExternalEvents()
    {
        if (!isSubscribedToSeasonEvent)
        {
            TimeManager.OnSeasonChanged += HandleSeasonChanged;
            isSubscribedToSeasonEvent = true;
        }

        if (!isSubscribedToHolidayEvent)
        {
            TimeManager.OnHolidayTriggered += HandleHolidayTriggered;
            isSubscribedToHolidayEvent = true;
        }

        if (!isSubscribedToDayEvent && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += HandleNewDay;
            isSubscribedToDayEvent = true;
        }

        if (!isSubscribedToInventoryEvent && Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;
            isSubscribedToInventoryEvent = true;
        }
    }

    private void Update()
    {
        if (!isPlayerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            TrySubscribeToExternalEvents();

            ShopManager shopManager = ShopManager.instance;
            if (shopManager == null)
                return;

            if (shopManager.IsShopOpen && shopManager.CurrentShopNpc == this)
            {
                shopManager.CloseShop();
            }
            else
            {
                shopManager.OpenShop(this, GetCombinedShopStock());
            }
        }
    }

    /// <summary>
    /// Initializes Permanent, Seasonal, and Daily buckets when starting a session.
    /// </summary>
    public void InitializeAllBuckets(Season startSeason)
    {
        currentSeason = startSeason;
        BuildPermanentStock();
        BuildSeasonalStock(currentSeason);
        ServerGenerateNewRotation();
    }

    /// <summary>
    /// Triggered by TimeManager.OnSeasonChanged before onNewDay fires.
    /// Rebuilds seasonal stock and re-evaluates daily items for the new season without resetting permanent stock.
    /// </summary>
    private void HandleSeasonChanged(Season newSeason)
    {
        currentSeason = newSeason;
        BuildSeasonalStock(newSeason);
        ApplyRotationFromSeed(currentRotationSeed, newSeason);
    }

    /// <summary>
    /// Triggered by TimeManager.OnHolidayTriggered right after onNewDay on matching holiday dates.
    /// </summary>
    private void HandleHolidayTriggered(HolidayEventData holiday)
    {
        if (holiday == null || string.IsNullOrEmpty(holiday.eventId))
            return;

        activeHolidayIds.Add(holiday.eventId);

        // Re-apply current daily rotation seed so holiday-restricted daily pool items become eligible today
        ApplyRotationFromSeed(currentRotationSeed, currentSeason);
    }

    /// <summary>
    /// Triggered automatically by TimeManager.Instance.onNewDay.
    /// Note: TimeManager invokes onNewDay BEFORE CheckAndTriggerHoliday(), so activeHolidayIds is cleared here.
    /// </summary>
    private void HandleNewDay()
    {
        activeHolidayIds.Clear();
        daysSinceLastRefresh++;

        if (daysSinceLastRefresh >= daysBetweenRefresh)
        {
            ServerGenerateNewRotation();
        }
        else
        {
            RefreshOpenShopUIIfNeeded();
        }
    }

    private void HandleInventoryChanged()
    {
        SyncObtainedMaterialsFromInventory();
    }

    private void BuildPermanentStock()
    {
        permanentStock.Clear();
        if (permanentItems == null)
            return;

        for (int i = 0; i < permanentItems.Count; i++)
        {
            if (permanentItems[i] != null && permanentItems[i].itemData != null)
            {
                permanentStock.Add(new ShopItem(permanentItems[i], StockCycleType.Permanent));
            }
        }
    }

    private void BuildSeasonalStock(Season season)
    {
        seasonalStock.Clear();
        if (seasonalItems == null)
            return;

        for (int i = 0; i < seasonalItems.Count; i++)
        {
            ShopItem template = seasonalItems[i];
            if (template != null && template.itemData != null && template.IsValidForSeason(season))
            {
                seasonalStock.Add(new ShopItem(template, StockCycleType.Seasonal));
            }
        }
    }

    /// <summary>
    /// [SERVER AUTHORITY] Generates a new seed and builds the daily rotating shop stock.
    /// When migrating to Multiplayer, sync 'currentRotationSeed' and 'currentSeason' to Clients.
    /// </summary>
    [ContextMenu("Force Refresh Daily Shop Stock")]
    public void ServerGenerateNewRotation()
    {
        SyncObtainedMaterialsFromInventory();

        daysSinceLastRefresh = 0;
        currentRotationSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        ApplyRotationFromSeed(currentRotationSeed, currentSeason);
    }

    /// <summary>
    /// [DETERMINISTIC CLIENT/SERVER LOGIC] Reconstructs the exact same daily items given the same seed and season.
    /// </summary>
    public void ApplyRotationFromSeed(int seed, Season season)
    {
        currentDailyStock.Clear();

        if (dailyPoolItems != null && dailyPoolItems.Count > 0)
        {
            List<int> validIndices = new();
            for (int i = 0; i < dailyPoolItems.Count; i++)
            {
                ShopItem candidate = dailyPoolItems[i];
                if (candidate != null &&
                    candidate.itemData != null &&
                    candidate.IsValidForSeason(season) &&
                    IsItemUnlocked(candidate))
                {
                    validIndices.Add(i);
                }
            }

            // Deterministic RNG ensures Server and all Clients get the exact same item rotation
            System.Random deterministicRng = new System.Random(seed);

            for (int i = validIndices.Count - 1; i > 0; i--)
            {
                int randomIndex = deterministicRng.Next(0, i + 1);
                (validIndices[i], validIndices[randomIndex]) = (validIndices[randomIndex], validIndices[i]);
            }

            int countToPick = Mathf.Min(itemsPerRotation, validIndices.Count);
            for (int i = 0; i < countToPick; i++)
            {
                int selectedPoolIndex = validIndices[i];
                currentDailyStock.Add(new ShopItem(dailyPoolItems[selectedPoolIndex], StockCycleType.Daily));
            }
        }

        RefreshOpenShopUIIfNeeded();

        Debug.Log($"[ShopNPC] {NpcName} rotated daily stock (Season: {season}, Seed: {seed}, Next refresh in {daysBetweenRefresh} day(s)).");
    }

    /// <summary>
    /// Evaluates whether a ShopItem satisfies its configured unlock condition.
    /// </summary>
    public bool IsItemUnlocked(ShopItem shopItem)
    {
        if (shopItem == null || shopItem.itemData == null)
            return false;

        ShopUnlockCondition condition = shopItem.unlockCondition;
        if (condition == null || condition.conditionType == ShopUnlockConditionType.None)
            return true;

        switch (condition.conditionType)
        {
            case ShopUnlockConditionType.None:
                return true;

            case ShopUnlockConditionType.MinDaysPlayed:
                return GetTotalDaysPlayed() >= condition.requiredDaysPlayed;

            case ShopUnlockConditionType.RequiredMaterialObtained:
                return HasObtainedRequiredMaterial(condition.requiredMaterial);

            case ShopUnlockConditionType.HolidayOnly:
                return IsHolidayConditionMet(condition.requiredHolidayEventId);

            default:
                return true;
        }
    }

    /// <summary>
    /// Returns total in-game days elapsed.
    /// </summary>
    private int GetTotalDaysPlayed()
    {
        // TODO [SaveManager & Cross-Scene Persistence]: Connect this to SaveManager or a persistent world progression tracker
        // so playtime/day milestones persist accurately across scene transitions and loaded save files.
        return TimeManager.Instance != null ? TimeManager.Instance.totalDaysElapsed : 0;
    }

    /// <summary>
    /// Checks whether the player has obtained the specified material item (cached history or currently in Inventory).
    /// </summary>
    private bool HasObtainedRequiredMaterial(ItemData requiredMaterial)
    {
        if (requiredMaterial == null)
            return true;

        string key = GetMaterialTrackingKey(requiredMaterial);
        if (obtainedMaterialIds.Contains(key))
            return true;

        Inventory playerInv = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        if (playerInv != null && playerInv.HasItem(requiredMaterial, 1))
        {
            obtainedMaterialIds.Add(key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Scans configured shop buckets and records any required materials currently held in Inventory.Instance.
    /// Refreshes open shop UI without re-rolling daily stock counts.
    /// </summary>
    public void SyncObtainedMaterialsFromInventory()
    {
        Inventory playerInv = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        if (playerInv == null)
            return;

        bool unlockedNewMaterial = false;
        unlockedNewMaterial |= CacheObtainedMaterialsInBucket(permanentStock, playerInv);
        unlockedNewMaterial |= CacheObtainedMaterialsInBucket(seasonalStock, playerInv);
        unlockedNewMaterial |= CacheObtainedMaterialsInBucket(dailyPoolItems, playerInv);

        if (unlockedNewMaterial)
        {
            RefreshOpenShopUIIfNeeded();
        }
    }

    private bool CacheObtainedMaterialsInBucket(List<ShopItem> bucket, Inventory playerInv)
    {
        if (bucket == null)
            return false;

        bool foundNew = false;
        for (int i = 0; i < bucket.Count; i++)
        {
            ShopItem item = bucket[i];
            if (item?.unlockCondition == null ||
                item.unlockCondition.conditionType != ShopUnlockConditionType.RequiredMaterialObtained)
            {
                continue;
            }

            ItemData requiredMat = item.unlockCondition.requiredMaterial;
            if (requiredMat == null)
                continue;

            string key = GetMaterialTrackingKey(requiredMat);
            if (!obtainedMaterialIds.Contains(key) && playerInv.HasItem(requiredMat, 1))
            {
                obtainedMaterialIds.Add(key);
                foundNew = true;
            }
        }

        return foundNew;
    }

    private static string GetMaterialTrackingKey(ItemData item)
    {
        return !string.IsNullOrEmpty(item.ItemID) ? item.ItemID : item.name;
    }

    /// <summary>
    /// Registers an obtained material ItemID manually (e.g., from external systems or save loading).
    /// </summary>
    public void NotifyMaterialObtained(string materialItemId)
    {
        if (string.IsNullOrEmpty(materialItemId) || obtainedMaterialIds.Contains(materialItemId))
            return;

        obtainedMaterialIds.Add(materialItemId);
        RefreshOpenShopUIIfNeeded();
    }

    private bool IsHolidayConditionMet(string requiredEventId)
    {
        if (activeHolidayIds.Count == 0)
            return false;

        // Empty eventId means any active holiday unlocks the item
        if (string.IsNullOrEmpty(requiredEventId))
            return true;

        return activeHolidayIds.Contains(requiredEventId);
    }

    /// <summary>
    /// Merges unlocked Permanent, Seasonal, and Daily buckets into a single list for ShopManager UI rendering.
    /// </summary>
    public List<ShopItem> GetCombinedShopStock()
    {
        SyncObtainedMaterialsFromInventory();
        combinedDisplayStock.Clear();

        AppendUnlockedItems(permanentStock, combinedDisplayStock);
        AppendUnlockedItems(seasonalStock, combinedDisplayStock);
        AppendUnlockedItems(currentDailyStock, combinedDisplayStock);

        return combinedDisplayStock;
    }

    private void AppendUnlockedItems(List<ShopItem> sourceBucket, List<ShopItem> destination)
    {
        for (int i = 0; i < sourceBucket.Count; i++)
        {
            if (IsItemUnlocked(sourceBucket[i]))
            {
                destination.Add(sourceBucket[i]);
            }
        }
    }

    private void RefreshOpenShopUIIfNeeded()
    {
        if (ShopManager.instance != null &&
            ShopManager.instance.IsShopOpen &&
            ShopManager.instance.CurrentShopNpc == this)
        {
            ShopManager.instance.OpenShop(this, GetCombinedShopStock());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // TODO [Multiplayer]: Check LocalPlayer ownership (e.g., if (!pc.IsLocalPlayer) return;)
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = true;
        TrySubscribeToExternalEvents();
        Debug.Log($"Press F to buy from {NpcName}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // TODO [Multiplayer]: Check LocalPlayer ownership
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = false;

        ShopManager shopManager = ShopManager.instance;
        if (shopManager != null && shopManager.CurrentShopNpc == this)
        {
            shopManager.CloseShop();
        }
    }
}