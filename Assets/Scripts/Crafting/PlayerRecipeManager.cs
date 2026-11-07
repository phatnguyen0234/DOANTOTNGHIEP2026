using System.Collections.Generic;
using UnityEngine;

public class PlayerRecipeManager : MonoBehaviour
{
    public static PlayerRecipeManager Instance { get; private set; }

    [Header("Debug / Testing Tools")]
    public bool debugUnlockAll = false;

    [Header("Runtime Unlocked Recipes")]
    public List<CraftingRecipe> unlockedRecipes = new List<CraftingRecipe>();

    public event System.Action OnRecipeUnlocked;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (debugUnlockAll)
        {
            UnlockAllFromMasterDatabase();
        }
#endif
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

            Debug.Log($"<color=orange>[Debug Mode] Unlock all {unlockedRecipes.Count} recipes </color>");
            OnRecipeUnlocked?.Invoke();
        }
        else
        {
            Debug.LogWarning("[PlayerRecipeManager] Cant find Crafting System");
        }
    }

    public void UnlockRecipe(CraftingRecipe newRecipe)
    {
        if (newRecipe != null && !unlockedRecipes.Contains(newRecipe))
        {
            unlockedRecipes.Add(newRecipe);
            Debug.Log($"<color=green> Unlock new recipe: {newRecipe.resultItem.ItemName}</color>");

            OnRecipeUnlocked?.Invoke();
        }
    }
}