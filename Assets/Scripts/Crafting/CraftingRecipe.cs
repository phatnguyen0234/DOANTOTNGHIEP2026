using System;
using UnityEngine;

[Serializable]
public struct CraftingIngredient
{
    public ItemData item;
    public IngredientGroup ingredientGroup;
    public int amount;

    public bool IsWildcard => ingredientGroup != null && ingredientGroup.IsValid;
    public bool HasRequirement => amount > 0 && (item != null || IsWildcard);

    /// <summary>
    /// Checks whether a placed ItemData satisfies this recipe slot.
    /// </summary>
    public bool Matches(ItemData candidate)
    {
        if (candidate == null || amount <= 0)
            return false;

        if (IsWildcard)
            return ingredientGroup.Contains(candidate);

        if (item == null)
            return false;

        if (item == candidate)
            return true;

        return !string.IsNullOrEmpty(item.ItemID) && item.ItemID == candidate.ItemID;
    }

    public ItemData GetDisplayItem(int cycleIndex)
    {
        if (IsWildcard)
            return ingredientGroup.GetCycleItem(cycleIndex);

        return item;
    }

    public Sprite GetDisplayIcon(int cycleIndex)
    {
        if (IsWildcard)
        {
            ItemData cycled = ingredientGroup.GetCycleItem(cycleIndex);
            if (cycled != null && cycled.Icon != null)
                return cycled.Icon;

            return ingredientGroup.fallbackIcon;
        }

        return item != null ? item.Icon : null;
    }

    public string GetTooltipLabel(int cycleIndex)
    {
        if (IsWildcard)
        {
            ItemData cycled = ingredientGroup.GetCycleItem(cycleIndex);
            return cycled != null
                ? $"{ingredientGroup.groupName} ({cycled.ItemName})"
                : ingredientGroup.groupName;
        }

        return item != null ? item.ItemName : string.Empty;
    }
}

public enum RecipeCategory
{
    Tool,
    Resource,
    Weapon,
    Equipment,
    Requirement,
    Furniture,
    Consumable,
    All // MUST remain at the very end of the enum to preserve serialized asset indices
}

public enum RecipeUnlockType
{
    DefaultUnlocked,    // Automatically unlocked from the start of the game
    AutoByIngredients,  // Unlocks automatically when all required ingredients are discovered
    ManualViaScroll     // Unlocks ONLY when reading a specific recipe scroll
}

[CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Farming/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Categorization")]
    public RecipeCategory category = RecipeCategory.Tool;

    [Header("Unlock Settings")]
    [Tooltip("Defines how the player unlocks this specific recipe.")]
    public RecipeUnlockType unlockType = RecipeUnlockType.AutoByIngredients;

    [Header("Recipe Shape (3x3 Grid)")]
    [Tooltip("Index 0-2: Top Row. Index 3-5: Middle Row. Index 6-8: Bottom Row. Leave empty for blank spaces.")]
    public CraftingIngredient[] requiredIngredients = new CraftingIngredient[9];

    [Header("Result")]
    public ItemData resultItem;
    public int resultAmount = 1;
}