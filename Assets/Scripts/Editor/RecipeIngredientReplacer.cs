using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RecipeIngredientReplacer : EditorWindow
{
    private ItemData oldIngredient;
    private ItemData newIngredient;

    // A helper class to manage the selection state of each found recipe
    private class RecipeSelection
    {
        public CraftingRecipe recipe;
        public bool isSelected;
    }

    private List<RecipeSelection> foundRecipes = new List<RecipeSelection>();
    private Vector2 scrollPosition;
    private string resultMessage = "";

    [MenuItem("Farming Tools/Bulk Replace Ingredients")]
    public static void ShowWindow()
    {
        GetWindow<RecipeIngredientReplacer>("Replace Ingredient");
    }

    private void OnGUI()
    {
        GUILayout.Label("Selective Replace Ingredients", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("1. Select the Old Ingredient to find all recipes using it.\n2. Select the New Ingredient.\n3. Check the recipes you want to modify, then click Replace.", MessageType.Info);

        GUILayout.Space(10);

        // Detect changes in the 'oldIngredient' field to automatically search for recipes
        EditorGUI.BeginChangeCheck();
        oldIngredient = (ItemData)EditorGUILayout.ObjectField("Old Ingredient", oldIngredient, typeof(ItemData), false);
        if (EditorGUI.EndChangeCheck())
        {
            FindRecipesWithOldIngredient();
            resultMessage = ""; // Clear old messages
        }

        newIngredient = (ItemData)EditorGUILayout.ObjectField("New Ingredient", newIngredient, typeof(ItemData), false);

        GUILayout.Space(10);

        if (foundRecipes.Count > 0)
        {
            GUILayout.Label($"Found {foundRecipes.Count} recipe(s) containing '{oldIngredient.name}':", EditorStyles.boldLabel);

            // Buttons to quickly select or deselect all recipes in the list
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(100))) ToggleAll(true);
            if (GUILayout.Button("Deselect All", GUILayout.Width(100))) ToggleAll(false);
            EditorGUILayout.EndHorizontal();

            // Display the scrollable list of recipes with checkboxes
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, "box", GUILayout.Height(250));
            foreach (var item in foundRecipes)
            {
                if (item.recipe != null)
                {
                    item.isSelected = EditorGUILayout.ToggleLeft(item.recipe.name, item.isSelected);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Enable the replace button ONLY if a new ingredient is set and at least one recipe is checked
            GUI.enabled = newIngredient != null && HasAnySelected();
            if (GUILayout.Button("Replace in Selected Recipes", GUILayout.Height(35)))
            {
                ReplaceInSelected();
            }
            GUI.enabled = true; // Reset GUI state
        }
        else if (oldIngredient != null)
        {
            EditorGUILayout.HelpBox("No recipes found containing this ingredient.", MessageType.Warning);
        }

        // Show success or error messages at the bottom
        if (!string.IsNullOrEmpty(resultMessage))
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(resultMessage, EditorStyles.boldLabel);
        }
    }

    private void FindRecipesWithOldIngredient()
    {
        foundRecipes.Clear();
        if (oldIngredient == null) return;

        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);

            if (recipe != null && recipe.requiredIngredients != null)
            {
                // Check if this recipe contains the old ingredient
                bool containsOld = false;
                foreach (var ingredient in recipe.requiredIngredients)
                {
                    if (ingredient.item == oldIngredient)
                    {
                        containsOld = true;
                        break;
                    }
                }

                // If found, add to the list and select it by default
                if (containsOld)
                {
                    foundRecipes.Add(new RecipeSelection { recipe = recipe, isSelected = true });
                }
            }
        }
    }

    private void ToggleAll(bool state)
    {
        foreach (var item in foundRecipes)
        {
            item.isSelected = state;
        }
    }

    private bool HasAnySelected()
    {
        foreach (var item in foundRecipes)
        {
            if (item.isSelected) return true;
        }
        return false;
    }

    private void ReplaceInSelected()
    {
        if (oldIngredient == newIngredient)
        {
            resultMessage = "<color=red>Old and New ingredients are the same!</color>";
            return;
        }

        int replacedCount = 0;

        foreach (var item in foundRecipes)
        {
            if (item.isSelected && item.recipe != null)
            {
                bool isModified = false;
                for (int i = 0; i < item.recipe.requiredIngredients.Length; i++)
                {
                    if (item.recipe.requiredIngredients[i].item == oldIngredient)
                    {
                        item.recipe.requiredIngredients[i].item = newIngredient;
                        isModified = true;
                    }
                }

                if (isModified)
                {
                    EditorUtility.SetDirty(item.recipe);
                    replacedCount++;
                }
            }
        }

        if (replacedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            resultMessage = $"<color=green> Successfully updated {replacedCount} recipe(s)!</color>";

            // Refresh the list to remove the recipes that no longer have the old ingredient
            FindRecipesWithOldIngredient();
        }
    }
}