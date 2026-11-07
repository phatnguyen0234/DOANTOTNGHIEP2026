using UnityEngine;

// Creates a new option in the Right-Click > Create menu
[CreateAssetMenu(fileName = "NewRecipeScroll", menuName = "Farming/Recipe Scroll")]
public class RecipeScrollItemData : ItemData
{
    [Header("Scroll Settings")]
    [Tooltip("The crafting recipe that this scroll will unlock when the player reads it.")]
    public CraftingRecipe recipeToUnlock;
}