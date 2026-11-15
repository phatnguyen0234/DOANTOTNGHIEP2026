using System.Collections.Generic;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName;

    [Header("Shop Pool & Rotation Settings")]
    [Tooltip("All possible items this NPC can sell over time. Order must match between Server and Client.")]
    [SerializeField] private List<ShopItem> possibleShopItems = new();

    [Tooltip("Number of random items displayed per rotation.")]
    [SerializeField] private int itemsPerRotation = 4;

    [Tooltip("Number of in-game days before the shop refreshes its stock.")]
    [Min(1)]
    [SerializeField] private int daysBetweenRefresh = 1;

    // Runtime state (Owned by Server in Multiplayer)
    private readonly List<ShopItem> currentDailyStock = new();
    private int currentRotationSeed;
    private int daysSinceLastRefresh = 0;
    private bool isPlayerInRange;
    private bool isSubscribedToTimeManager;

    private void OnEnable()
    {
        TrySubscribeToTimeManager();
    }

    private void Start()
    {
        // Fallback subscription in case TimeManager.Awake() ran after ShopNPC.OnEnable()
        TrySubscribeToTimeManager();

        // Generate initial stock when the scene loads
        // Note: In Multiplayer, wrap this in: if (IsServer) { ... }
        ServerGenerateNewRotation();
    }

    private void OnDisable()
    {
        if (isSubscribedToTimeManager && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay -= HandleNewDay;
            isSubscribedToTimeManager = false;
        }
    }

    private void TrySubscribeToTimeManager()
    {
        if (!isSubscribedToTimeManager && TimeManager.Instance != null)
        {
            TimeManager.Instance.onNewDay += HandleNewDay;
            isSubscribedToTimeManager = true;
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
                shopManager.OpenShop(this, currentDailyStock);
            }
        }
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

    /// <summary>
    /// [SERVER AUTHORITY] Generates a new seed and builds the shop stock.
    /// When migrating to Multiplayer, sync 'currentRotationSeed' to Clients via NetworkVariable or ClientRpc.
    /// </summary>
    [ContextMenu("Force Refresh Shop Stock")]
    public void ServerGenerateNewRotation()
    {
        daysSinceLastRefresh = 0;
        currentRotationSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        ApplyRotationFromSeed(currentRotationSeed);
    }

    /// <summary>
    /// [DETERMINISTIC CLIENT/SERVER LOGIC] Reconstructs the exact same shop items given the same seed.
    /// </summary>
    public void ApplyRotationFromSeed(int seed)
    {
        currentDailyStock.Clear();

        if (possibleShopItems == null || possibleShopItems.Count == 0)
            return;

        List<int> validIndices = new();
        for (int i = 0; i < possibleShopItems.Count; i++)
        {
            if (possibleShopItems[i] != null && possibleShopItems[i].itemData != null)
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
            currentDailyStock.Add(new ShopItem(possibleShopItems[selectedPoolIndex]));
        }

        // Refresh UI immediately if the local player currently has this shop open when midnight hits
        if (ShopManager.instance != null &&
            ShopManager.instance.IsShopOpen &&
            ShopManager.instance.CurrentShopNpc == this)
        {
            ShopManager.instance.OpenShop(this, currentDailyStock);
        }

        Debug.Log($"[ShopNPC] {npcName} rotated stock (Seed: {seed}, Next refresh in {daysBetweenRefresh} day(s)).");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // TODO [Multiplayer]: Check LocalPlayer ownership (e.g., if (!pc.IsLocalPlayer) return;)
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = true;
        Debug.Log($"Press F to buy from {npcName}");
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