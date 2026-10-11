using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Farming/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Shape (3x3 Grid)")]
    [Tooltip("Index 0-2: Top Row. Index 3-5: Middle Row. Index 6-8: Bottom Row. Leave empty for blank spaces.")]
    public ItemData[] requiredItems = new ItemData[9];

    [Header("Result")]
    public ItemData resultItem;
    public int resultAmount = 1;
}