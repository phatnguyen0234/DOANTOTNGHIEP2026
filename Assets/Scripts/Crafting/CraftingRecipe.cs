using System;
using UnityEngine;

[Serializable]
public struct CraftingIngredient
{
    public ItemData item;
    public int amount;
}

public enum RecipeCategory
{
    Tool,
    Resource,
    Requirement,
    Furniture,
    Consumable
}

public enum RecipeUnlockType
{
    DefaultUnlocked,    // Automatically unlocked from the start of the game
    AutoByIngredients,  // Unlocks automatically when all unique ingredients are discovered
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
    [Tooltip("Index 0-2: Top Row. Index 3-5: Middle Row. Index 6-8: Bottom Row. Leave item empty for blank spaces.")]
    public CraftingIngredient[] requiredIngredients = new CraftingIngredient[9];

    [Header("Result")]
    public ItemData resultItem;
    public int resultAmount = 1;
}