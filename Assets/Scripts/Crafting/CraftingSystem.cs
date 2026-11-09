using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    [Header("Core References")]
    public Inventory craftingGridInventory;
    public Inventory playerInventory;
    public CraftingOutputUI outputUI;

    [Header("Master Recipe Database")]
    public List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

    private CraftingRecipe currentValidRecipe = null;
    private int maxCraftableAmount = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

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

    public void BindCraftingUI(Inventory gridInv, CraftingOutputUI outUI)
    {
        if (craftingGridInventory != null)
        {
            craftingGridInventory.OnInventoryChanged -= EvaluateGrid;
        }

        craftingGridInventory = gridInv;
        outputUI = outUI;

        if (playerInventory == null)
        {
            playerInventory = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        }

        if (craftingGridInventory != null)
        {
            craftingGridInventory.OnInventoryChanged += EvaluateGrid;
            EvaluateGrid();
        }
    }

    public void UnbindCraftingUI(Inventory gridInv)
    {
        if (craftingGridInventory == gridInv)
        {
            if (craftingGridInventory != null)
            {
                craftingGridInventory.OnInventoryChanged -= EvaluateGrid;
            }
            craftingGridInventory = null;
            outputUI = null;
        }
    }

    private void EvaluateGrid()
    {
        currentValidRecipe = null;
        maxCraftableAmount = 0;

        foreach (var recipe in allRecipes)
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
        possibleCrafts = 0;

        GetRecipeBounds(recipe, out int rMinX, out int rMaxX, out int rMinY, out int rMaxY);
        GetGridBounds(out int gMinX, out int gMaxX, out int gMinY, out int gMaxY);

        if (gMinX > gMaxX || rMinX > rMaxX) return false;

        int rWidth = rMaxX - rMinX + 1;
        int rHeight = rMaxY - rMinY + 1;
        int gWidth = gMaxX - gMinX + 1;
        int gHeight = gMaxY - gMinY + 1;

        if (rWidth != gWidth || rHeight != gHeight) return false;

        possibleCrafts = int.MaxValue;
        bool hasAnyIngredient = false;

        for (int y = 0; y < rHeight; y++)
        {
            for (int x = 0; x < rWidth; x++)
            {
                int rIndex = (rMinY + y) * 3 + (rMinX + x);
                int gIndex = (gMinY + y) * 3 + (gMinX + x);

                if (rIndex >= recipe.requiredIngredients.Length) return false;

                CraftingIngredient required = recipe.requiredIngredients[rIndex];
                InventorySlot gridSlot = craftingGridInventory.GetSlot(gIndex);

                if (required.item == null || required.amount <= 0)
                {
                    if (!gridSlot.IsEmpty()) return false;
                }
                else
                {
                    if (gridSlot.IsEmpty() || gridSlot.ItemData.ItemID != required.item.ItemID)
                        return false;

                    if (gridSlot.Amount < required.amount)
                        return false;

                    int craftsForThisSlot = gridSlot.Amount / required.amount;
                    if (craftsForThisSlot < possibleCrafts)
                    {
                        possibleCrafts = craftsForThisSlot;
                    }

                    hasAnyIngredient = true;
                }
            }
        }

        return hasAnyIngredient && possibleCrafts > 0;
    }

    private void GetRecipeBounds(CraftingRecipe recipe, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 3; maxX = -1; minY = 3; maxY = -1;

        if (recipe == null || recipe.requiredIngredients == null) return;

        int length = Mathf.Min(9, recipe.requiredIngredients.Length);

        for (int i = 0; i < length; i++)
        {
            if (recipe.requiredIngredients[i].item != null && recipe.requiredIngredients[i].amount > 0)
            {
                int x = i % 3;
                int y = i / 3;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
    }

    private void GetGridBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 3; maxX = -1; minY = 3; maxY = -1;
        for (int i = 0; i < 9; i++)
        {
            InventorySlot slot = craftingGridInventory.GetSlot(i);
            if (!slot.IsEmpty())
            {
                int x = i % 3;
                int y = i / 3;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
    }

    public void ExecuteCrafting(bool craftAll)
    {
        if (currentValidRecipe == null) return;

        if (playerInventory == null)
        {
            playerInventory = Inventory.Instance ?? FindAnyObjectByType<Inventory>(FindObjectsInactive.Include);
        }
        if (playerInventory == null) return;

        int craftsToPerform = craftAll ? maxCraftableAmount : 1;
        int totalYield = currentValidRecipe.resultAmount * craftsToPerform;

        if (!playerInventory.CanAddItem(currentValidRecipe.resultItem, totalYield))
        {
            Debug.LogWarning("Player inventory is full! Cannot craft.");
            return;
        }

        // 1. Give result to player
        playerInventory.TryAddItem(currentValidRecipe.resultItem, totalYield);

        // 2. Consume ingredients based on their mapped positions
        GetRecipeBounds(currentValidRecipe, out int rMinX, out int rMaxX, out int rMinY, out int rMaxY);
        GetGridBounds(out int gMinX, out int gMaxX, out int gMinY, out int gMaxY);

        int rWidth = rMaxX - rMinX + 1;
        int rHeight = rMaxY - rMinY + 1;

        for (int y = 0; y < rHeight; y++)
        {
            for (int x = 0; x < rWidth; x++)
            {
                int rIndex = (rMinY + y) * 3 + (rMinX + x);
                int gIndex = (gMinY + y) * 3 + (gMinX + x);

                CraftingIngredient required = currentValidRecipe.requiredIngredients[rIndex];
                if (required.item != null && required.amount > 0)
                {
                    int amountToRemove = required.amount * craftsToPerform;
                    craftingGridInventory.RemoveItemFromSlot(gIndex, amountToRemove);
                }
            }
        }
    }
}