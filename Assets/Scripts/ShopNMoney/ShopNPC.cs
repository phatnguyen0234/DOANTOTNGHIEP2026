using System.Collections.Generic;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName;

    [Header("1. Permanent Stock (Always Available)")]
    [Tooltip("Items permanently sold year-round regardless of season or day.")]
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

    // Runtime state split by lifecycle bucket (Owned by Server in Multiplayer)
    private readonly List<ShopItem> permanentStock = new();
    private readonly List<ShopItem> seasonalStock = new();
    private readonly List<ShopItem> currentDailyStock = new();
    private readonly List<ShopItem> combinedDisplayStock = new();

    private Season currentSeason = Season.Spring;
    private int currentRotationSeed;
    private int daysSinceLastRefresh = 0;
    private bool isPlayerInRange;
    private bool isSubscribedToDayEvent;
    private bool isSubscribedToSeasonEvent;

    public string NpcName => string.IsNullOrEmpty(npcName) ? gameObject.name : npcName;

    private void OnEnable()
    {
        TrySubscribeToTimeEvents();
    }

    private void Start()
    {
        // Fallback subscription in case TimeManager.Awake() ran after ShopNPC.OnEnable()
        TrySubscribeToTimeEvents();

        Season initialSeason = TimeManager.Instance != null
            ? TimeManager.Instance.currentSeason
            : Season.Spring;

        // Note: In Multiplayer, wrap this in: if (IsServer) { ... }
        InitializeAllBuckets(initialSeason);
    }

    private void OnDisable()
    {
        if (isSubscribedToSeasonEvent)
        {
            TimeManager.OnSeasonChanged -= HandleSeasonChanged;
            isSubscribedToSeasonEvent = false;
        }

        if (isSubscribedToDayEvent && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay -= HandleNewDay;
            isSubscribedToDayEvent = false;
        }
    }

    private void TrySubscribeToTimeEvents()
    {
        if (!isSubscribedToSeasonEvent)
        {
            TimeManager.OnSeasonChanged += HandleSeasonChanged;
            isSubscribedToSeasonEvent = true;
        }

        if (!isSubscribedToDayEvent && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += HandleNewDay;
            isSubscribedToDayEvent = true;
        }
    }

    private void Update()
    {
        if (!isPlayerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
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
    /// Triggered automatically by TimeManager.Instance.onNewDay.
    /// In Multiplayer, ensure this logic only executes on the Server.
    /// </summary>
    private void HandleNewDay()
    {
        daysSinceLastRefresh++;

        if (daysSinceLastRefresh >= daysBetweenRefresh)
        {
            ServerGenerateNewRotation();
        }
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
                if (candidate != null && candidate.itemData != null && candidate.IsValidForSeason(season))
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

        // Refresh UI immediately if the local player currently has this shop open when midnight hits
        if (ShopManager.instance != null &&
            ShopManager.instance.IsShopOpen &&
            ShopManager.instance.CurrentShopNpc == this)
        {
            ShopManager.instance.OpenShop(this, GetCombinedShopStock());
        }

        Debug.Log($"[ShopNPC] {NpcName} rotated daily stock (Season: {season}, Seed: {seed}, Next refresh in {daysBetweenRefresh} day(s)).");
    }

    /// <summary>
    /// Merges Permanent, Seasonal, and Daily buckets into a single list for ShopManager UI rendering.
    /// </summary>
    public List<ShopItem> GetCombinedShopStock()
    {
        combinedDisplayStock.Clear();
        combinedDisplayStock.AddRange(permanentStock);
        combinedDisplayStock.AddRange(seasonalStock);
        combinedDisplayStock.AddRange(currentDailyStock);
        return combinedDisplayStock;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // TODO [Multiplayer]: Check LocalPlayer ownership (e.g., if (!pc.IsLocalPlayer) return;)
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = true;
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