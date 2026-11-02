using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RecipeConflictChecker : EditorWindow
{
    private List<string> conflictReports = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("Farming Tools/Check Recipe Conflicts")]
    public static void ShowWindow()
    {
        // Creates and opens the Editor Window
        GetWindow<RecipeConflictChecker>("Recipe Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Crafting Recipe Conflict Checker", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Run Check", GUILayout.Height(30)))
        {
            AnalyzeRecipes();
        }

        GUILayout.Space(10);

        if (conflictReports.Count == 0)
        {
            GUILayout.Label("No conflicts found or check not run yet.", EditorStyles.helpBox);
        }
        else
        {
            GUILayout.Label($"Found {conflictReports.Count} potential conflict(s):", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "box");
            foreach (string report in conflictReports)
            {
                EditorGUILayout.HelpBox(report, MessageType.Warning);
            }
            GUILayout.EndScrollView();
        }
    }

    private void AnalyzeRecipes()
    {
        conflictReports.Clear();

        // 1. Find all CraftingRecipe ScriptableObjects in the project
        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe");
        List<CraftingRecipe> allRecipes = new List<CraftingRecipe>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(assetPath);
            if (recipe != null)
            {
                allRecipes.Add(recipe);
            }
        }

        // 2. Compare every recipe against each other
        for (int i = 0; i < allRecipes.Count; i++)
        {
            for (int j = i + 1; j < allRecipes.Count; j++)
            {
                if (AreRecipesConflicting(allRecipes[i], allRecipes[j]))
                {
                    string name1 = allRecipes[i].name;
                    string name2 = allRecipes[j].name;
                    conflictReports.Add($"Conflict detected between: '{name1}' and '{name2}'. They share the exact same shape and item requirements.");
                }
            }
        }

        if (conflictReports.Count == 0)
        {
            Debug.Log("<color=green>Recipe Check Complete: All recipes are unique!</color>");
        }
    }

    private bool AreRecipesConflicting(CraftingRecipe r1, CraftingRecipe r2)
    {
        // Get bounding boxes for both recipes
        GetRecipeBounds(r1, out int minX1, out int maxX1, out int minY1, out int maxY1);
        GetRecipeBounds(r2, out int minX2, out int maxX2, out int minY2, out int maxY2);

        // Check for empty recipes
        bool r1Empty = minX1 > maxX1;
        bool r2Empty = minX2 > maxX2;

        if (r1Empty && r2Empty) return true; // Both empty -> conflict
        if (r1Empty || r2Empty) return false;

        int w1 = maxX1 - minX1 + 1;
        int h1 = maxY1 - minY1 + 1;
        int w2 = maxX2 - minX2 + 1;
        int h2 = maxY2 - minY2 + 1;

        // If shapes have different dimensions, they don't conflict
        if (w1 != w2 || h1 != h2) return false;

        // Compare content relative to their bounding box starting points
        for (int y = 0; y < h1; y++)
        {
            for (int x = 0; x < w1; x++)
            {
                int idx1 = (minY1 + y) * 3 + (minX1 + x);
                int idx2 = (minY2 + y) * 3 + (minX2 + x);

                CraftingIngredient req1 = r1.requiredIngredients != null && idx1 < r1.requiredIngredients.Length ? r1.requiredIngredients[idx1] : new CraftingIngredient();
                CraftingIngredient req2 = r2.requiredIngredients != null && idx2 < r2.requiredIngredients.Length ? r2.requiredIngredients[idx2] : new CraftingIngredient();

                bool empty1 = req1.item == null || req1.amount <= 0;
                bool empty2 = req2.item == null || req2.amount <= 0;

                // Mismatch in shape structure (one has item, one doesn't at this relative pos)
                if (empty1 != empty2) return false;

                // Both have items, check if they are the exact same item
                if (!empty1 && !empty2)
                {
                    if (req1.item.ItemID != req2.item.ItemID)
                        return false;

                    // Note: We do not check amounts here because if they have the same shape and same items, 
                    // putting 5 stones will fulfill BOTH recipes, causing an engine conflict on which to craft.
                }
            }
        }

        return true;
    }

    private void GetRecipeBounds(CraftingRecipe recipe, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = 3; maxX = -1; minY = 3; maxY = -1;
        if (recipe == null || recipe.requiredIngredients == null) return;

        int len = Mathf.Min(9, recipe.requiredIngredients.Length);
        for (int i = 0; i < len; i++)
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
}