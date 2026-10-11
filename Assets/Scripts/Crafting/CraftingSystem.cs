using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The 9-slot inventory used for the crafting grid.")]
    public Inventory craftingGridInventory;
    public Inventory playerInventory;
    public CraftingOutputUI outputUI;

    [Header("Recipes Database")]
    public List<CraftingRecipe> availableRecipes;

    private CraftingRecipe currentValidRecipe = null;
    private int maxCraftableAmount = 0;

    private void OnEnable()
    {
        if (craftingGridInventory != null)
            craftingGridInventory.OnInventoryChanged += EvaluateGrid;
    }

    private void OnDisable()
    {
        if (craftingGridInventory != null)
            craftingGridInventory.OnInventoryChanged -= EvaluateGrid;
    }

    private void EvaluateGrid()
    {
        currentValidRecipe = null;
        maxCraftableAmount = 0;

        foreach (var recipe in availableRecipes)
        {
            if (IsShapeMatching(recipe, out int possibleCrafts))
            {
                currentValidRecipe = recipe;
                maxCraftableAmount = possibleCrafts;
                break;
            }
        }

        if (currentValidRecipe != null)
        {
            // Show the result and how many the player will get if they Shift-Click
            bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            int displayAmt = isShiftHeld ? (currentValidRecipe.resultAmount * maxCraftableAmount) : currentValidRecipe.resultAmount;

            outputUI.DisplayResult(currentValidRecipe.resultItem, displayAmt);
        }
        else
        {
            outputUI.ClearResult();
        }
    }

    private bool IsShapeMatching(CraftingRecipe recipe, out int possibleCrafts)
    {
        possibleCrafts = int.MaxValue;
        bool hasAnyIngredient = false;

        for (int i = 0; i < 9; i++)
        {
            InventorySlot gridSlot = craftingGridInventory.GetSlot(i);
            ItemData requiredItem = recipe.requiredItems[i];

            if (requiredItem == null)
            {
                if (!gridSlot.IsEmpty())
                {
                    possibleCrafts = 0;
                    return false; // Grid contains an item where the recipe expects empty space
                }
            }
            else
            {
                if (gridSlot.IsEmpty() || gridSlot.ItemData.ItemID != requiredItem.ItemID)
                {
                    possibleCrafts = 0;
                    return false; // Missing item or wrong item type
                }

                if (gridSlot.Amount < possibleCrafts)
                {
                    possibleCrafts = gridSlot.Amount; // Bottleneck for bulk crafting
                }

                hasAnyIngredient = true;
            }
        }

        return hasAnyIngredient && possibleCrafts > 0;
    }

    public void ExecuteCrafting(bool craftAll)
    {
        if (currentValidRecipe == null) return;

        int craftsToPerform = craftAll ? maxCraftableAmount : 1;
        int totalYield = currentValidRecipe.resultAmount * craftsToPerform;

        if (!playerInventory.CanAddItem(currentValidRecipe.resultItem, totalYield))
        {
            Debug.LogWarning("Player inventory is full! Cannot craft.");
            return;
        }

        // Add result to player inventory
        playerInventory.TryAddItem(currentValidRecipe.resultItem, totalYield);

        // Consume ingredients from the grid
        for (int i = 0; i < 9; i++)
        {
            if (currentValidRecipe.requiredItems[i] != null)
            {
                craftingGridInventory.RemoveItemFromSlot(i, craftsToPerform);
            }
        }

        // EvaluateGrid is automatically called by RemoveItemFromSlot due to OnInventoryChanged event
    }
}