using UnityEngine;

public class CraftingTable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private GameObject craftingUIPanel;
    [SerializeField] private Inventory craftingGridInventory;

    [Header("Settings")]
    [SerializeField] private float autoCloseDistance = 3.5f;

    private Transform playerTransform;
    //private bool isOpen = false;

    public string InteractionPrompt => "Open Crafting Table";

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public bool IsOpen
    {
        get
        {
            if (BlitzyUI.UIManager.Instance != null && BlitzyUI.UIManager.Instance.GetScreen(BlitzyUI.ScreenIds.Crafting) != null)
            {
                var screen = BlitzyUI.UIManager.Instance.GetScreen(BlitzyUI.ScreenIds.Crafting);
                return screen != null && screen.gameObject.activeSelf;
            }
            return craftingUIPanel != null && craftingUIPanel.activeSelf;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor != null)
        {
            playerTransform = interactor.transform;
        }

        if (IsOpen)
        {
            CloseCraftingTable();
        }
        else
        {
            OpenCraftingTable();
        }
    }

    public void OpenCraftingTable()
    {
        if (BlitzyUI.UIManager.Instance != null)
        {
            BlitzyUI.UIManager.Instance.QueuePush(BlitzyUI.ScreenIds.Crafting);
            // Đảm bảo túi đồ của người chơi được đẩy vào hàng đợi mở đồng thời bên dưới
            Inventory playerInv = Inventory.Instance;
            if (playerInv != null && !playerInv.IsBagOpen)
            {
                playerInv.OpenBag();
            }
            return;
        }

        if (craftingUIPanel != null)
        {
            craftingUIPanel.SetActive(true);
        }

        Inventory inv = Inventory.Instance;
        if (inv != null && !inv.IsBagOpen)
        {
            inv.OpenBag();
        }
    }

    public void CloseCraftingTable()
    {
        if (BlitzyUI.UIManager.Instance != null && BlitzyUI.UIManager.Instance.GetScreen(BlitzyUI.ScreenIds.Crafting) != null)
        {
            BlitzyUI.UIManager.Instance.QueuePopTo(BlitzyUI.ScreenIds.Crafting, include: true);
            Inventory pInv = Inventory.Instance;
            if (pInv != null && pInv.IsBagOpen)
            {
                pInv.CloseBag();
            }
            return;
        }

        if (craftingUIPanel != null)
        {
            craftingUIPanel.SetActive(false);
        }

        ReturnItemsToPlayer();

        Inventory inv = Inventory.Instance;
        if (inv != null && inv.IsBagOpen)
        {
            inv.CloseBag();
        }
    }

    private void Update()
    {
        if (IsOpen && playerTransform != null)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            if (distance > autoCloseDistance)
            {
                CloseCraftingTable();
            }
        }
    }

    private void ReturnItemsToPlayer()
    {
        if (craftingGridInventory == null) return;
        Inventory playerInv = Inventory.Instance;

        if (playerInv == null) return;

        for (int i = 0; i < craftingGridInventory.Capacity; i++)
        {
            InventorySlot slot = craftingGridInventory.GetSlot(i);
            if (!slot.IsEmpty())
            {
                int leftover = playerInv.AddItem(slot.ItemData, slot.Amount);
                if (leftover == 0)
                {
                    craftingGridInventory.ClearSlot(i);
                }
                else
                {
                    Debug.LogWarning("Player inventory is full! Dropping item: " + slot.ItemData.ItemName);
                    craftingGridInventory.RemoveItemFromSlot(i, slot.Amount - leftover);
                }
            }
        }
    }
}