using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewIngredientGroup", menuName = "Farming/Ingredient Group")]
public class IngredientGroup : ScriptableObject
{
    [Header("Group Identity")]
    [Tooltip("Display name of this ingredient group (e.g., Any Fish, Any Egg, Any Milk).")]
    public string groupName = "Any Fish";

    [Tooltip("Optional fallback icon if the group list is empty.")]
    public Sprite fallbackIcon;

    [Header("Allowed Items")]
    [Tooltip("All ItemData assets accepted as valid substitutes for this group.")]
    public List<ItemData> items = new();

    public bool IsValid => items != null && items.Count > 0;

    /// <summary>
    /// Checks whether the provided ItemData belongs to this ingredient group.
    /// </summary>
    public bool Contains(ItemData candidate)
    {
        if (candidate == null || items == null)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == candidate)
                return true;

            if (items[i] != null &&
                !string.IsNullOrEmpty(items[i].ItemID) &&
                items[i].ItemID == candidate.ItemID)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the item to display at a given cycle step for DataBook UI rotation.
    /// </summary>
    public ItemData GetCycleItem(int cycleIndex)
    {
        if (items == null || items.Count == 0)
            return null;

        int validIndex = Mathf.Abs(cycleIndex) % items.Count;
        return items[validIndex];
    }
}