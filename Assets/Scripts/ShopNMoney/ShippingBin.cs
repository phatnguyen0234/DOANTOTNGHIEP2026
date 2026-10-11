using System.Collections.Generic;
using UnityEngine;

public class ShippingBin : MonoBehaviour, ITradeReceiver
{
    private Dictionary<ItemData, int> pendingShipments = new Dictionary<ItemData, int>();

    public bool CanAcceptItem(ItemData item)
    {
        return item != null && item.CanSell;
    }

    public void ProcessTransaction(ItemData item, int amount, Inventory playerInventory, PlayerMoney playerMoney)
    {
        if (!CanAcceptItem(item))
        {
            Debug.LogWarning("This item cannot be sold!");
            return;
        }

        if (playerInventory.TryRemoveItem(item, amount))
        {
            if (pendingShipments.ContainsKey(item))
                pendingShipments[item] += amount;
            else
                pendingShipments.Add(item, amount);

            Debug.Log($"Tossed {amount}x {item.ItemName} into the Shipping Bin.");
        }
    }

    private void Update()
    {
        // Press P to simulate end of day
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