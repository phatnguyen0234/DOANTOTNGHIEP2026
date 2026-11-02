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

    public void Interact(GameObject interactor)
    {
        if (craftingUIPanel == null) return;

        if (interactor != null)
        {
            playerTransform = interactor.transform;
        }

        if (craftingUIPanel.activeSelf)
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
        craftingUIPanel.SetActive(true);

        Inventory playerInv = Inventory.Instance;
        if (playerInv != null && !playerInv.IsBagOpen)
        {
            playerInv.OpenBag();
        }
    }

    public void CloseCraftingTable()
    {
        craftingUIPanel.SetActive(false);
        ReturnItemsToPlayer();

        Inventory playerInv = Inventory.Instance;
        if (playerInv != null && playerInv.IsBagOpen) playerInv.CloseBag();
    }

    //private void Update()
    //{
    //    if (isOpen && playerTransform != null)
    //    {
    //        float distance = Vector2.Distance(transform.position, playerTransform.position);
    //        if (distance > autoCloseDistance)
    //        {
    //            CloseCraftingTable();
    //        }
    //    }
    //}

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