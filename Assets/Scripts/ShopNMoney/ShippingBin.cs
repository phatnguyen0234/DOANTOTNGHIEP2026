using System.Collections.Generic;
using UnityEngine;

public class ShippingBin : MonoBehaviour, IInteractable
{
    private Dictionary<ItemData, int> pendingShipments = new Dictionary<ItemData, int>();

    public string InteractionPrompt => "Ship Item";

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public void Interact(GameObject interactor)
    {
        // 1. Safely gather references using the Singleton for the main inventory
        Inventory playerInv = Inventory.Instance;
        HotbarController hotbar = FindFirstObjectByType<HotbarController>();

        if (playerInv == null || hotbar == null)
        {
            Debug.LogError("Missing core systems (Inventory or Hotbar).");
            return;
        }

        InventorySlot activeSlot = hotbar.SelectedSlot;

        // 2. Check if player is holding something
        if (activeSlot != null && !activeSlot.IsEmpty())
        {
            ItemData heldItem = activeSlot.ItemData;

            if (heldItem.CanSell)
            {
                // Try to remove 1 item from the player's main inventory
                if (playerInv.TryRemoveItem(heldItem, 1))
                {
                    if (pendingShipments.ContainsKey(heldItem))
                        pendingShipments[heldItem] += 1;
                    else
                        pendingShipments.Add(heldItem, 1);

                    Debug.Log($"Tossed 1x {heldItem.ItemName} into the Shipping Bin.");
                }
            }
            else
            {
                Debug.LogWarning("This item cannot be shipped!");
            }
        }
        else
        {
            Debug.Log("Your hands are empty. (Future feature: Open Shipping Summary UI here)");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayerMoney money = FindFirstObjectByType<PlayerMoney>();
            if (money != null) ProcessEndOfDayShipping(money);
        }
    }

    public void ProcessEndOfDayShipping(PlayerMoney playerMoney)
    {
        if (pendingShipments.Count == 0) return;

        int dailyTotal = 0;
        Debug.Log("--- END OF DAY SHIPPING SUMMARY ---");

        foreach (var shipment in pendingShipments)
        {
            ItemData item = shipment.Key;
            int amount = shipment.Value;
            int revenue = item.SellPrice * amount;

            dailyTotal += revenue;
            Debug.Log($"- Sold {amount}x {item.ItemName}: +{revenue}G");
        }

        playerMoney.AddMoney(dailyTotal);
        Debug.Log($"TOTAL DAILY INCOME: {dailyTotal}G");
        pendingShipments.Clear();
    }
}