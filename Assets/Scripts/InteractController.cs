using UnityEngine;

public class InteractController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Inventory inventory;
    [SerializeField] private HotbarController hotbarController;
    [SerializeField] private PlayerMoney playerMoney;

    [Header("Settings")]
    [SerializeField] private LayerMask interactableMask;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (inventory == null) inventory = FindFirstObjectByType<Inventory>();
        if (hotbarController == null) hotbarController = FindFirstObjectByType<HotbarController>();
        if (playerMoney == null) playerMoney = FindFirstObjectByType<PlayerMoney>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity, interactableMask);

        if (hit.collider != null)
        {
            Debug.Log("Hit interactable object: " + hit.collider.gameObject.name);

            // 1. Check for Trade Receivers
            ITradeReceiver receiver = hit.collider.GetComponent<ITradeReceiver>();
            if (receiver != null)
            {
                Debug.Log("Found ITradeReceiver on: " + hit.collider.gameObject.name);

                InventorySlot activeSlot = hotbarController.SelectedSlot;
                if (activeSlot != null && !activeSlot.IsEmpty())
                {
                    Debug.Log("Attempting to transaction with item: " + activeSlot.ItemData.ItemName);
                    receiver.ProcessTransaction(activeSlot.ItemData, 1, inventory, playerMoney);
                }
                else
                {
                    Debug.LogWarning("Cannot transact: Active hotbar slot is empty.");
                }
                return;
            }

            // 2. Check for Crafting Table
            CraftingTable craftingTable = hit.collider.GetComponent<CraftingTable>();
            if (craftingTable != null)
            {
                Debug.Log("Opening Crafting Table.");
                craftingTable.OpenCraftingTable();
                return;
            }
        }
    }
}