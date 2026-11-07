using System.Collections.Generic;
using UnityEngine;

public class PlayerRecipeManager : MonoBehaviour
{
    public static PlayerRecipeManager Instance { get; private set; }

    [Header("Recipe Database")]
    [Tooltip("If true, all recipes in 'allAvailableRecipes' will be unlocked immediately on Start.")]
    public bool unlockAllOnStart = true;

    [Tooltip("Drag all CraftingRecipe assets in your project here.")]
    public List<CraftingRecipe> allAvailableRecipes = new List<CraftingRecipe>();

    [Header("Runtime Unlocked Recipes")]
    [Tooltip("List of crafting recipes the player currently has access to.")]
    public List<CraftingRecipe> unlockedRecipes = new List<CraftingRecipe>();

    public event System.Action OnRecipeUnlocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (unlockAllOnStart)
        {
            UnlockAllRecipes();
        }
    }

    // Populates unlockedRecipes with all available recipes
    public void UnlockAllRecipes()
    {
        foreach (var recipe in allAvailableRecipes)
        {
            if (recipe != null && !unlockedRecipes.Contains(recipe))
            {
                unlockedRecipes.Add(recipe);
            }
        }
        OnRecipeUnlocked?.Invoke();
    }

    public void UnlockRecipe(CraftingRecipe newRecipe)
    {
        if (newRecipe != null && !unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log($"<color=green>Unlocked new recipe: {newRecipe.resultItem.ItemName}</color>");
            OnRecipeUnlocked?.Invoke();
        }
    }
}