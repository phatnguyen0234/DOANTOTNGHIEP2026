using System;

[Serializable]
public class ShopItem
{
    public ItemData itemData;
    public int stock = 10;

    public ShopItem() { }

    public ShopItem(ShopItem template)
    {
        this.itemData = template.itemData;
        this.stock = template.stock;
    }
}

/// <summary>
/// Lightweight, network-serializable representation of a shop slot.
/// Ready to be wrapped in NetworkList or RPCs when migrating to Multiplayer.
/// </summary>
[Serializable]
public struct NetworkShopSlotData
{
    public int poolIndex; // Index referencing the NPC's possibleShopItems list
    public int remainingStock;

    public NetworkShopSlotData(int poolIndex, int remainingStock)
    {
        this.poolIndex = poolIndex;
        this.remainingStock = remainingStock;
    }
}