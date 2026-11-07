using System.Collections.Generic;
using UnityEngine;

public class PlayerRecipeManager : MonoBehaviour
{
    public static PlayerRecipeManager Instance { get; private set; }

    [Header("Debug / Testing Tools")]
    [Tooltip("Enable this to automatically unlock all recipes when playing. Only works in Unity Editor!")]
    public bool debugUnlockAll = false;

    [Header("Discovery System")]
    [Tooltip("List of ItemIDs the player has discovered at least once.")]
    public List<string> discoveredItemIDs = new List<string>();

    [Header("Runtime Unlocked Recipes")]
    [Tooltip("The actual list of recipes the player has unlocked.")]
    public List<CraftingRecipe> unlockedRecipes = new List<CraftingRecipe>();

    public event System.Action OnRecipeUnlocked;

    public event System.Action<CraftingRecipe> OnShowUnlockNotification;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Invoke(nameof(InitializeDefaultRecipes), 0.1f);

#if UNITY_EDITOR
        if (debugUnlockAll)
        {
            Invoke(nameof(UnlockAllFromMasterDatabase), 0.15f);
        }
#endif
    }

    private void InitializeDefaultRecipes()
    {
        if (CraftingSystem.Instance == null) return;

        foreach (var recipe in CraftingSystem.Instance.allRecipes)
        {
            if (recipe.unlockType == RecipeUnlockType.DefaultUnlocked && !unlockedRecipes.Contains(recipe))
            {
                unlockedRecipes.Add(recipe);
            }
        }
        OnRecipeUnlocked?.Invoke();
    }

    public void DiscoverItem(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.ItemID)) return;

        if (!discoveredItemIDs.Contains(item.ItemID))
        {
            discoveredItemIDs.Add(item.ItemID);
            CheckForAutoUnlocks();
        }
    }

    private void CheckForAutoUnlocks()
    {
        if (CraftingSystem.Instance == null) return;

        foreach (var recipe in CraftingSystem.Instance.allRecipes)
        {
            if (recipe.unlockType == RecipeUnlockType.AutoByIngredients && !unlockedRecipes.Contains(recipe))
            {
                bool hasDiscoveredAll = true;

                foreach (var ingredient in recipe.requiredIngredients)
                {
                    if (ingredient.item != null && ingredient.amount > 0)
                    {
                        if (!discoveredItemIDs.Contains(ingredient.item.ItemID))
                        {
                            hasDiscoveredAll = false;
                            break;
                        }
                    }
                }
                if (hasDiscoveredAll)
                {
                    UnlockRecipe(recipe);
                }
            }
        }
    }

    public void UnlockRecipe(CraftingRecipe newRecipe)
    {
        if (newRecipe != null && !unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log($"<color=green>Unlocked new recipe: {newRecipe.resultItem.ItemName}</color>");

            OnRecipeUnlocked?.Invoke();

            OnShowUnlockNotification?.Invoke(newRecipe);
        }
    }

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
            OnRecipeUnlocked?.Invoke();
        }
    }
}