using System.Collections.Generic;
using UnityEngine;

public class PlayerRecipeManager : MonoBehaviour
{
    public static PlayerRecipeManager Instance { get; private set; }

    [Header("Debug / Testing Tools")]
    [Tooltip("Enable this to automatically unlock all recipes when playing. Only works in Unity Editor!")]
    public bool debugUnlockAll = false;

    [Header("Runtime Unlocked Recipes")]
    [Tooltip("The actual list of recipes the player has unlocked.")]
    public List<CraftingRecipe> unlockedRecipes = new List<CraftingRecipe>();

    public event System.Action OnRecipeUnlocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Preprocessor directive: This code only runs in the Unity Editor
#if UNITY_EDITOR
        if (debugUnlockAll)
        {
            // Add a small 0.1s delay to ensure CraftingSystem is fully initialized first
            Invoke(nameof(UnlockAllFromMasterDatabase), 0.1f);
        }
#endif
    }

    // Automatically fetches and unlocks all recipes from the CraftingSystem
    public void UnlockAllFromMasterDatabase()
    {
        if (CraftingSystem.Instance != null && CraftingSystem.Instance.allRecipes != null)
        {
            unlockedRecipes.Clear();
            foreach (var recipe in CraftingSystem.Instance.allRecipes)
            {
                if (recipe != null)
                {
                    unlockedRecipes.Add(recipe);
                }
            }

            Debug.Log($"<color=orange>[Debug Mode] Auto-unlocked all {unlockedRecipes.Count} recipes for testing!</color>");

            // Notify the UI to update the recipe book
            OnRecipeUnlocked?.Invoke();
        }
        else
        {
            Debug.LogWarning("[PlayerRecipeManager] CraftingSystem not found or allRecipes is empty. Make sure CraftingSystem is in the scene!");
        }
    }

    // Called when the player reads a scroll or completes a quest
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