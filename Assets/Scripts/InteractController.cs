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
        // Right click to interact
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
            ITradeReceiver receiver = hit.collider.GetComponent<ITradeReceiver>();

            if (receiver != null)
            {
                InventorySlot activeSlot = hotbarController.SelectedSlot;

                if (activeSlot != null && !activeSlot.IsEmpty())
                {
                    receiver.ProcessTransaction(activeSlot.ItemData, 1, inventory, playerMoney);
                }
            }
        }
    }
}