using System;
using UnityEngine;

[Serializable]
public class InventorySlotData 
{
    public int slotIndex;
    public string itemId;
    public int amount;

    public InventorySlotData(int slotIndex, string itemId, int amount)
    {
        this.slotIndex = slotIndex;
        this.itemId = itemId;
        this.amount = amount;
    }
}
