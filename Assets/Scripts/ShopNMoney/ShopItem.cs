using System;
using System.Collections.Generic;
using UnityEngine;

public enum StockCycleType
{
    Permanent,
    Seasonal,
    Daily
}

public enum ShopUnlockConditionType
{
    None,                     // 1. Standard sale & day/season rotation (no extra condition)
    MinDaysPlayed,            // 2. Unlocked after playing at least N in-game days
    RequiredMaterialObtained, // 3. Unlocked after obtaining a specific material item
    HolidayOnly               // 4. Unlocked only during an in-game holiday
}

[Serializable]
public class ShopUnlockCondition
{
    public ShopUnlockConditionType conditionType = ShopUnlockConditionType.None;

    [Min(0)]
    public int requiredDaysPlayed = 0;

    public ItemData requiredMaterial;

    public string requiredHolidayEventId;
}

[Serializable]
public class ShopItem
{
    public ItemData itemData;

    [Tooltip("Set to -1 for unlimited stock (recommended for permanent or core seasonal items).")]
    public int stock = 10;

    [Tooltip("Leave empty if this item is available in all seasons.")]
    public List<Season> validSeasons = new();

    public ShopUnlockCondition unlockCondition = new();

    [HideInInspector]
    public StockCycleType cycleType = StockCycleType.Daily;

    public bool IsUnlimited => stock < 0;
    public bool IsSoldOut => !IsUnlimited && stock <= 0;

    public ShopItem() { }

    public ShopItem(ShopItem template, StockCycleType assignedCycleType)
    {
        itemData = template.itemData;
        stock = template.stock;
        validSeasons = template.validSeasons != null
            ? new List<Season>(template.validSeasons)
            : new List<Season>();
        unlockCondition = template.unlockCondition != null
            ? new ShopUnlockCondition
            {
                conditionType = template.unlockCondition.conditionType,
                requiredDaysPlayed = template.unlockCondition.requiredDaysPlayed,
                requiredMaterial = template.unlockCondition.requiredMaterial,
                requiredHolidayEventId = template.unlockCondition.requiredHolidayEventId
            }
            : new ShopUnlockCondition();
        cycleType = assignedCycleType;
    }

    public bool IsValidForSeason(Season season)
    {
        return validSeasons == null || validSeasons.Count == 0 || validSeasons.Contains(season);
    }
}

/// <summary>
/// Lightweight, network-serializable representation of a shop slot.
/// Ready to be wrapped in NetworkList or RPCs when migrating to Multiplayer.
/// </summary>
[Serializable]
public struct NetworkShopSlotData
{
    public StockCycleType cycleType;
    public int poolIndex;
    public int remainingStock;

    public NetworkShopSlotData(StockCycleType cycleType, int poolIndex, int remainingStock)
    {
        this.cycleType = cycleType;
        this.poolIndex = poolIndex;
        this.remainingStock = remainingStock;
    }
}