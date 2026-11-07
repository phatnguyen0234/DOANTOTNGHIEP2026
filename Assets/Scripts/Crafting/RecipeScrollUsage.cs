using UnityEngine;
using UnityEngine.EventSystems;

public class RecipeScrollUsage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HotbarController hotbarController;
    [SerializeField] private Inventory playerInventory;

    private void Awake()
    {
        if (hotbarController == null) hotbarController = FindAnyObjectByType<HotbarController>();
        if (playerInventory == null) playerInventory = FindAnyObjectByType<Inventory>();
    }

    private void Update()
    {
        // On Right Click
        if (Input.GetMouseButtonDown(1))
        {
            // Ignore UI clicks
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            TryUseScroll();
        }
    }

    private void TryUseScroll()
    {
        if (hotbarController == null || playerInventory == null) return;

        InventorySlot activeSlot = hotbarController.SelectedSlot;
        if (activeSlot == null || activeSlot.IsEmpty() || activeSlot.ItemData == null) return;

        if (activeSlot.ItemData is RecipeScrollItemData scrollData)
        {
            CraftingRecipe recipe = scrollData.recipeToUnlock;

            if (recipe == null) return;

            if (PlayerRecipeManager.Instance.unlockedRecipes.Contains(recipe))
            {
                Debug.Log($"<color=yellow>You already know the recipe for {recipe.resultItem.ItemName}!</color>");
                return;
            }

            PlayerRecipeManager.Instance.UnlockRecipe(recipe);

            playerInventory.TryRemoveItem(scrollData, 1);

            Debug.Log($"<color=cyan>Successfully read scroll and learned: {recipe.resultItem.ItemName}</color>");
        }
    }
}