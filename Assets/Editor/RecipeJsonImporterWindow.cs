using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class RecipeJsonImporterWindow : EditorWindow
{
    private const string PREFS_OUTPUT_FOLDER = "FarmingRPG_RecipeImportOutputFolder";
    private const string PREFS_LAST_CATEGORY = "FarmingRPG_RecipeImportCategory";

    private string rawJsonInput = "";
    private string loadedJsonFilePath = "";

    private RecipeCategory selectedCategory = RecipeCategory.Consumable;
    private bool overrideJsonCategory = true;

    private DefaultAsset outputFolderAsset;
    private string outputFolderPath = "Assets/Data/Recipes/Food";

    private bool overwriteExistingAssets = true;
    private bool autoRegisterToCraftingSystem = true;

    private Vector2 jsonScrollPos;
    private Vector2 previewScrollPos;

    private List<ParsedRecipeEntry> previewEntries = new List<ParsedRecipeEntry>();
    private string statusMessage = "";
    private MessageType statusType = MessageType.Info;

    private static readonly RecipeCategory[] ValidCategories =
    {
        RecipeCategory.Tool,
        RecipeCategory.Resource,
        RecipeCategory.Weapon,
        RecipeCategory.Equipment,
        RecipeCategory.Requirement,
        RecipeCategory.Furniture,
        RecipeCategory.Consumable
    };

    [MenuItem("Farming Tools/Import Recipes from JSON")]
    public static void ShowWindow()
    {
        GetWindow<RecipeJsonImporterWindow>("Recipe JSON Importer");
    }

    private void OnEnable()
    {
        outputFolderPath = EditorPrefs.GetString(PREFS_OUTPUT_FOLDER, "Assets/Data/Recipes/Food");
        int savedCat = EditorPrefs.GetInt(PREFS_LAST_CATEGORY, (int)RecipeCategory.Consumable);
        if (Enum.IsDefined(typeof(RecipeCategory), savedCat) && (RecipeCategory)savedCat != RecipeCategory.All)
        {
            selectedCategory = (RecipeCategory)savedCat;
        }

        if (AssetDatabase.IsValidFolder(outputFolderPath))
        {
            outputFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputFolderPath);
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Crafting Recipe JSON Importer (Google Sheet)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Imports CraftingRecipe ScriptableObjects from a Google Sheet JSON export.\n" +
            "Supports Slot_0..Slot_8 in 'item_id:amount' format as well as wildcard groups via 'group:Any_Fish:1'.",
            MessageType.Info
        );

        GUILayout.Space(6);

        // 1. Target Category & Output Folder Settings
        EditorGUILayout.LabelField("1. Target Category & Destination Folder", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Category Dropdown (excluding RecipeCategory.All)
        int currentCatIdx = Array.IndexOf(ValidCategories, selectedCategory);
        if (currentCatIdx < 0) currentCatIdx = ValidCategories.Length - 1;

        string[] categoryLabels = new string[ValidCategories.Length];
        for (int i = 0; i < ValidCategories.Length; i++)
            categoryLabels[i] = ValidCategories[i].ToString();

        EditorGUI.BeginChangeCheck();
        int newCatIdx = EditorGUILayout.Popup("Target Recipe Category", currentCatIdx, categoryLabels);
        if (EditorGUI.EndChangeCheck())
        {
            selectedCategory = ValidCategories[newCatIdx];
            EditorPrefs.SetInt(PREFS_LAST_CATEGORY, (int)selectedCategory);
        }

        overrideJsonCategory = EditorGUILayout.ToggleLeft(
            new GUIContent(
                "Force selected Category on all imported recipes (ignore JSON 'Category' column)",
                "If unchecked, uses the JSON 'Category' column when valid and falls back to Target Recipe Category."
            ),
            overrideJsonCategory
        );

        EditorGUILayout.Space(4);

        // Output Folder Picker (Drag Folder OR Browse Button)
        EditorGUI.BeginChangeCheck();
        DefaultAsset newFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("Output Folder (Project)", "Drag a target folder from the Project window."),
            outputFolderAsset,
            typeof(DefaultAsset),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            outputFolderAsset = newFolderAsset;
            if (outputFolderAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(outputFolderAsset);
                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    outputFolderPath = assetPath;
                    EditorPrefs.SetString(PREFS_OUTPUT_FOLDER, outputFolderPath);
                }
            }
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        outputFolderPath = EditorGUILayout.TextField("Folder Path", outputFolderPath);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREFS_OUTPUT_FOLDER, outputFolderPath);
            outputFolderAsset = AssetDatabase.IsValidFolder(outputFolderPath)
                ? AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputFolderPath)
                : null;
        }

        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string initialDir = Directory.Exists(outputFolderPath) ? outputFolderPath : Application.dataPath;
            string chosenAbs = EditorUtility.OpenFolderPanel("Select Output Folder Inside Assets/", initialDir, "");
            if (!string.IsNullOrEmpty(chosenAbs))
            {
                string dataPathNorm = Application.dataPath.Replace('\\', '/');
                string chosenNorm = chosenAbs.Replace('\\', '/');
                if (chosenNorm.StartsWith(dataPathNorm, StringComparison.OrdinalIgnoreCase))
                {
                    outputFolderPath = "Assets" + chosenNorm.Substring(dataPathNorm.Length);
                    EditorPrefs.SetString(PREFS_OUTPUT_FOLDER, outputFolderPath);
                    outputFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(outputFolderPath);
                }
                else
                {
                    statusMessage = "Please select a folder located inside this Unity project's Assets/ directory.";
                    statusType = MessageType.Error;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        overwriteExistingAssets = EditorGUILayout.ToggleLeft("Overwrite existing CraftingRecipe assets with the same name", overwriteExistingAssets);
        autoRegisterToCraftingSystem = EditorGUILayout.ToggleLeft("Auto-register imported recipes into CraftingSystem.allRecipes (active scene)", autoRegisterToCraftingSystem);

        EditorGUILayout.EndVertical();

        GUILayout.Space(6);

        // 2. JSON Input Source
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("2. Google Sheet JSON Input", EditorStyles.boldLabel);
        if (GUILayout.Button("Load .json File...", GUILayout.Width(130)))
        {
            string file = EditorUtility.OpenFilePanel("Select Google Sheet Recipes JSON", "", "json");
            if (!string.IsNullOrEmpty(file) && File.Exists(file))
            {
                loadedJsonFilePath = file;
                rawJsonInput = File.ReadAllText(file, Encoding.UTF8);
                ParseAndValidateJson();
            }
        }
        if (GUILayout.Button("Clear", GUILayout.Width(60)))
        {
            rawJsonInput = "";
            loadedJsonFilePath = "";
            previewEntries.Clear();
            statusMessage = "";
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(loadedJsonFilePath))
        {
            EditorGUILayout.LabelField($"Loaded file: {loadedJsonFilePath}", EditorStyles.miniLabel);
        }

        jsonScrollPos = EditorGUILayout.BeginScrollView(jsonScrollPos, "box", GUILayout.Height(130));
        rawJsonInput = EditorGUILayout.TextArea(rawJsonInput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6);

        // 3. Action Buttons
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrWhiteSpace(rawJsonInput);
        if (GUILayout.Button("1. Parse & Preview JSON", GUILayout.Height(32)))
        {
            ParseAndValidateJson();
        }

        GUI.enabled = previewEntries.Count > 0 && !string.IsNullOrWhiteSpace(outputFolderPath);
        if (GUILayout.Button($"2. Import {previewEntries.Count} Recipes to '{selectedCategory}'", GUILayout.Height(32)))
        {
            ExecuteImport();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        // 4. Preview List
        if (previewEntries.Count > 0)
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField($"Preview ({previewEntries.Count} parsed recipes):", EditorStyles.boldLabel);

            previewScrollPos = EditorGUILayout.BeginScrollView(previewScrollPos, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < previewEntries.Count; i++)
            {
                ParsedRecipeEntry entry = previewEntries[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                string badge = entry.alreadyExists ? "[UPDATE]" : "[NEW]";
                EditorGUILayout.LabelField($"{i + 1}. {badge} {entry.assetFileName}.asset", EditorStyles.boldLabel, GUILayout.Width(230));
                EditorGUILayout.LabelField($"Cat: {entry.resolvedCategory} | Unlock: {entry.unlockType} | Out: {entry.resultItemID} x{entry.resultAmount}", GUILayout.Width(310));

                if (entry.warnings.Count > 0)
                {
                    GUIStyle warnStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.9f, 0.25f, 0.2f) } };
                    EditorGUILayout.LabelField("ERROR: " + string.Join("; ", entry.warnings), warnStyle);
                }
                else
                {
                    GUIStyle okStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.2f, 0.7f, 0.3f) } };
                    EditorGUILayout.LabelField($"OK ({entry.filledSlotCount} slots)", okStyle);
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void ParseAndValidateJson()
    {
        previewEntries.Clear();

        if (string.IsNullOrWhiteSpace(rawJsonInput))
        {
            statusMessage = "JSON input is empty.";
            statusType = MessageType.Warning;
            return;
        }

        try
        {
            object root = SimpleJson.Parse(rawJsonInput);
            List<Dictionary<string, object>> rawList = ExtractRecipeRecords(root);

            if (rawList == null || rawList.Count == 0)
            {
                statusMessage = "No recipe objects found in JSON. Expected a root array [...] or an object containing 'recipes': [...].";
                statusType = MessageType.Error;
                return;
            }

            // Build lookup caches for all ItemData & IngredientGroup assets in project
            Dictionary<string, ItemData> itemById = BuildItemLookup();
            Dictionary<string, IngredientGroup> groupByName = BuildGroupLookup();

            string normalizedOutDir = NormalizeAssetFolderPath(outputFolderPath);
            int errorRows = 0;

            foreach (var row in rawList)
            {
                ParsedRecipeEntry entry = new ParsedRecipeEntry();

                string resultId = GetString(row, "Result_ItemID", "resultItemID", "result_item_id", "resultItemId").Trim();
                string rawAssetName = GetString(row, "Recipe_Asset", "recipeAssetName", "recipe_asset", "name").Trim();
                string resultName = GetString(row, "Result_Name", "resultItemName", "result_name").Trim();

                entry.resultItemID = resultId;
                entry.resultAmount = Mathf.Max(1, GetInt(row, 1, "Result_Amount", "resultAmount", "result_amount"));

                string baseName = !string.IsNullOrEmpty(rawAssetName)
                    ? rawAssetName
                    : (!string.IsNullOrEmpty(resultName) ? resultName : resultId);
                entry.assetFileName = SanitizeFileName(baseName);

                if (string.IsNullOrEmpty(entry.assetFileName))
                {
                    entry.warnings.Add("Missing Recipe_Asset and Result_ItemID");
                }

                // Resolve Category
                entry.resolvedCategory = selectedCategory;
                if (!overrideJsonCategory)
                {
                    string jsonCat = GetString(row, "Category", "category").Trim();
                    if (!string.IsNullOrEmpty(jsonCat) &&
                        Enum.TryParse(jsonCat, true, out RecipeCategory parsedCat) &&
                        parsedCat != RecipeCategory.All)
                    {
                        entry.resolvedCategory = parsedCat;
                    }
                }

                // Resolve UnlockType
                entry.unlockType = RecipeUnlockType.AutoByIngredients;
                string jsonUnlock = GetString(row, "UnlockType", "unlockType", "unlock_type").Trim();
                if (!string.IsNullOrEmpty(jsonUnlock) &&
                    Enum.TryParse(jsonUnlock, true, out RecipeUnlockType parsedUnlock))
                {
                    entry.unlockType = parsedUnlock;
                }

                // Resolve Result ItemData
                if (string.IsNullOrEmpty(resultId))
                {
                    entry.warnings.Add("Missing Result_ItemID");
                }
                else if (itemById.TryGetValue(resultId, out ItemData foundResult))
                {
                    entry.resolvedResultItem = foundResult;
                }
                else
                {
                    entry.warnings.Add($"Result ItemID '{resultId}' not found in project");
                }

                // Resolve 3x3 Slots (Slot_0 .. Slot_8)
                entry.slots = new CraftingIngredient[9];
                for (int s = 0; s < 9; s++)
                {
                    string slotRaw = GetString(row, $"Slot_{s}", $"slot_{s}", $"slot{s}").Trim();
                    if (string.IsNullOrEmpty(slotRaw))
                        continue;

                    ParseSlotToken(slotRaw, itemById, groupByName, out CraftingIngredient parsedIng, out string slotErr);
                    if (!string.IsNullOrEmpty(slotErr))
                    {
                        entry.warnings.Add($"Slot_{s}: {slotErr}");
                    }
                    else if (parsedIng.HasRequirement)
                    {
                        entry.slots[s] = parsedIng;
                        entry.filledSlotCount++;
                    }
                }

                if (entry.filledSlotCount == 0)
                {
                    entry.warnings.Add("All 9 slots are empty");
                }

                string targetAssetPath = $"{normalizedOutDir}/{entry.assetFileName}.asset";
                entry.alreadyExists = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(targetAssetPath) != null;

                if (entry.warnings.Count > 0)
                    errorRows++;

                previewEntries.Add(entry);
            }

            if (errorRows > 0)
            {
                statusMessage = $"Parsed {previewEntries.Count} recipes, but {errorRows} have missing ItemIDs/Groups (highlighted in red below). Fix them or import valid ones.";
                statusType = MessageType.Warning;
            }
            else
            {
                statusMessage = $"Validated {previewEntries.Count} recipes cleanly with 0 errors! Ready to import into '{normalizedOutDir}' as '{selectedCategory}'.";
                statusType = MessageType.Info;
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"JSON Parse Error: {ex.Message}";
            statusType = MessageType.Error;
        }
    }

    private void ExecuteImport()
    {
        if (previewEntries.Count == 0)
            return;

        string normalizedOutDir = NormalizeAssetFolderPath(outputFolderPath);
        if (!normalizedOutDir.StartsWith("Assets", StringComparison.Ordinal))
        {
            statusMessage = "Output folder must be inside 'Assets/'.";
            statusType = MessageType.Error;
            return;
        }

        EnsureFolderExists(normalizedOutDir);

        int createdCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;
        List<CraftingRecipe> importedAssets = new List<CraftingRecipe>();

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var entry in previewEntries)
            {
                if (entry.resolvedResultItem == null || entry.filledSlotCount == 0 || string.IsNullOrEmpty(entry.assetFileName))
                {
                    skippedCount++;
                    continue;
                }

                string assetPath = $"{normalizedOutDir}/{entry.assetFileName}.asset";
                CraftingRecipe existing = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(assetPath);

                if (existing != null)
                {
                    if (!overwriteExistingAssets)
                    {
                        skippedCount++;
                        continue;
                    }

                    Undo.RecordObject(existing, "Update Imported CraftingRecipe");
                    ApplyEntryToRecipe(existing, entry);
                    EditorUtility.SetDirty(existing);
                    importedAssets.Add(existing);
                    updatedCount++;
                }
                else
                {
                    CraftingRecipe newRecipe = CreateInstance<CraftingRecipe>();
                    ApplyEntryToRecipe(newRecipe, entry);
                    AssetDatabase.CreateAsset(newRecipe, assetPath);
                    importedAssets.Add(newRecipe);
                    createdCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        int registeredCount = 0;
        if (autoRegisterToCraftingSystem && importedAssets.Count > 0)
        {
            CraftingSystem craftingSys = CraftingSystem.Instance ?? FindAnyObjectByType<CraftingSystem>(FindObjectsInactive.Include);
            if (craftingSys != null)
            {
                Undo.RecordObject(craftingSys, "Register Imported Recipes");
                if (craftingSys.allRecipes == null)
                    craftingSys.allRecipes = new List<CraftingRecipe>();

                foreach (var r in importedAssets)
                {
                    if (r != null && !craftingSys.allRecipes.Contains(r))
                    {
                        craftingSys.allRecipes.Add(r);
                        registeredCount++;
                    }
                }
                EditorUtility.SetDirty(craftingSys);
            }
        }

        outputFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(normalizedOutDir);
        statusMessage = $"Import complete! Created: {createdCount}, Updated: {updatedCount}, Skipped: {skippedCount}" +
                        (autoRegisterToCraftingSystem ? $" | Registered to CraftingSystem: +{registeredCount}." : ".");
        statusType = MessageType.Info;

        Debug.Log($"[RecipeJsonImporter] {statusMessage}");
        ParseAndValidateJson();
    }

    private static void ApplyEntryToRecipe(CraftingRecipe target, ParsedRecipeEntry entry)
    {
        target.category = entry.resolvedCategory;
        target.unlockType = entry.unlockType;
        target.resultItem = entry.resolvedResultItem;
        target.resultAmount = entry.resultAmount;
        target.requiredIngredients = new CraftingIngredient[9];
        for (int i = 0; i < 9; i++)
        {
            target.requiredIngredients[i] = entry.slots[i];
        }
    }

    private static void ParseSlotToken(
        string token,
        Dictionary<string, ItemData> itemById,
        Dictionary<string, IngredientGroup> groupByName,
        out CraftingIngredient ingredient,
        out string error)
    {
        ingredient = default;
        error = null;

        // Expected formats:
        //   "salmon:1"
        //   "salmon" (defaults amount = 1)
        //   "group:Any_Fish:1" or "Group:Any_Fish:1"
        //   "Any_Fish:1" (auto-resolves to IngredientGroup if not an ItemID)
        string[] parts = token.Split(':');
        int amount = 1;
        bool isExplicitGroup = false;
        string identifier;

        if (parts.Length >= 3 && parts[0].Equals("group", StringComparison.OrdinalIgnoreCase))
        {
            isExplicitGroup = true;
            identifier = parts[1].Trim();
            if (!int.TryParse(parts[2].Trim(), out amount) || amount <= 0)
                amount = 1;
        }
        else if (parts.Length == 2)
        {
            if (parts[0].Equals("group", StringComparison.OrdinalIgnoreCase))
            {
                isExplicitGroup = true;
                identifier = parts[1].Trim();
                amount = 1;
            }
            else
            {
                identifier = parts[0].Trim();
                if (!int.TryParse(parts[1].Trim(), out amount) || amount <= 0)
                    amount = 1;
            }
        }
        else
        {
            identifier = parts[0].Trim();
        }

        if (string.IsNullOrEmpty(identifier))
            return;

        if (isExplicitGroup)
        {
            if (groupByName.TryGetValue(identifier, out IngredientGroup grp))
            {
                ingredient.ingredientGroup = grp;
                ingredient.amount = amount;
            }
            else
            {
                error = $"IngredientGroup '{identifier}' not found";
            }
            return;
        }

        if (itemById.TryGetValue(identifier, out ItemData foundItem))
        {
            ingredient.item = foundItem;
            ingredient.amount = amount;
            return;
        }

        if (groupByName.TryGetValue(identifier, out IngredientGroup fallbackGroup))
        {
            ingredient.ingredientGroup = fallbackGroup;
            ingredient.amount = amount;
            return;
        }

        error = $"Unknown ItemID or Group '{identifier}'";
    }

    private static Dictionary<string, ItemData> BuildItemLookup()
    {
        Dictionary<string, ItemData> map = new Dictionary<string, ItemData>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && !string.IsNullOrEmpty(item.ItemID) && !map.ContainsKey(item.ItemID))
            {
                map[item.ItemID] = item;
            }
        }
        return map;
    }

    private static Dictionary<string, IngredientGroup> BuildGroupLookup()
    {
        Dictionary<string, IngredientGroup> map = new Dictionary<string, IngredientGroup>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:IngredientGroup");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IngredientGroup grp = AssetDatabase.LoadAssetAtPath<IngredientGroup>(path);
            if (grp == null) continue;

            if (!map.ContainsKey(grp.name))
                map[grp.name] = grp;

            if (!string.IsNullOrEmpty(grp.groupName) && !map.ContainsKey(grp.groupName))
                map[grp.groupName] = grp;
        }
        return map;
    }

    private static List<Dictionary<string, object>> ExtractRecipeRecords(object root)
    {
        List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

        if (root is List<object> rootList)
        {
            foreach (var item in rootList)
            {
                if (item is Dictionary<string, object> dict)
                    result.Add(dict);
            }
            return result;
        }

        if (root is Dictionary<string, object> rootObj)
        {
            string[] candidateKeys = { "recipes", "Food_Recipes", "data", "items", "rows" };
            foreach (string key in candidateKeys)
            {
                foreach (var kv in rootObj)
                {
                    if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && kv.Value is List<object> list)
                    {
                        foreach (var item in list)
                        {
                            if (item is Dictionary<string, object> dict)
                                result.Add(dict);
                        }
                        return result;
                    }
                }
            }

            // Fallback: pick first List<object> property found
            foreach (var kv in rootObj)
            {
                if (kv.Value is List<object> list)
                {
                    foreach (var item in list)
                    {
                        if (item is Dictionary<string, object> dict)
                            result.Add(dict);
                    }
                    return result;
                }
            }
        }

        return result;
    }

    private static string GetString(Dictionary<string, object> dict, params string[] keys)
    {
        foreach (string k in keys)
        {
            foreach (var kv in dict)
            {
                if (kv.Key.Equals(k, StringComparison.OrdinalIgnoreCase) && kv.Value != null)
                {
                    return Convert.ToString(kv.Value, CultureInfo.InvariantCulture) ?? "";
                }
            }
        }
        return "";
    }

    private static int GetInt(Dictionary<string, object> dict, int defaultValue, params string[] keys)
    {
        string raw = GetString(dict, keys);
        if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out int val))
            return val;
        return defaultValue;
    }

    private static string NormalizeAssetFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "Assets";

        return path.Trim().Replace('\\', '/').TrimEnd('/');
    }

    private static void EnsureFolderExists(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath))
            return;

        string[] segments = assetFolderPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }
            current = next;
        }
    }

    private static string SanitizeFileName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "";

        char[] invalid = Path.GetInvalidFileNameChars();
        StringBuilder sb = new StringBuilder(rawName.Length);
        foreach (char c in rawName.Trim())
        {
            if (Array.IndexOf(invalid, c) < 0)
                sb.Append(c);
        }
        return sb.ToString().Trim();
    }

    private class ParsedRecipeEntry
    {
        public string assetFileName;
        public RecipeCategory resolvedCategory;
        public RecipeUnlockType unlockType;
        public string resultItemID;
        public ItemData resolvedResultItem;
        public int resultAmount;
        public CraftingIngredient[] slots;
        public int filledSlotCount;
        public bool alreadyExists;
        public List<string> warnings = new List<string>();
    }

    #region Lightweight Self-Contained JSON Parser (Supports Root Arrays & Objects)

    private static class SimpleJson
    {
        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            int index = 0;
            return ParseValue(json, ref index);
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length)
                return null;

            char c = json[index];
            if (c == '{') return ParseObject(json, ref index);
            if (c == '[') return ParseArray(json, ref index);
            if (c == '"') return ParseString(json, ref index);
            if (c == 't' || c == 'f') return ParseBool(json, ref index);
            if (c == 'n') return ParseNull(json, ref index);
            return ParseNumber(json, ref index);
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            index++; // skip '{'

            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == '}')
                {
                    index++;
                    break;
                }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ':')
                    index++;

                object val = ParseValue(json, ref index);
                dict[key] = val;

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == '}')
                {
                    index++;
                    break;
                }
            }

            return dict;
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            List<object> list = new List<object>();
            index++; // skip '['

            while (true)
            {
                SkipWhitespace(json, ref index);
                if (index >= json.Length) break;
                if (json[index] == ']')
                {
                    index++;
                    break;
                }

                list.Add(ParseValue(json, ref index));

                SkipWhitespace(json, ref index);
                if (index < json.Length && json[index] == ',')
                {
                    index++;
                    continue;
                }
                if (index < json.Length && json[index] == ']')
                {
                    index++;
                    break;
                }
            }

            return list;
        }

        private static string ParseString(string json, ref int index)
        {
            StringBuilder sb = new StringBuilder();
            index++; // skip opening '"'

            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                    break;

                if (c == '\\' && index < json.Length)
                {
                    char esc = json[index++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (index + 4 <= json.Length)
                            {
                                string hex = json.Substring(index, 4);
                                if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                                    sb.Append((char)code);
                                index += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && "0123456789+-.eE".IndexOf(json[index]) >= 0)
            {
                index++;
            }

            string numStr = json.Substring(start, index - start);
            if (long.TryParse(numStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                return l;
            if (double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                return d;
            return 0;
        }

        private static bool ParseBool(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
                return true;
            }
            if (json.Substring(index).StartsWith("false", StringComparison.OrdinalIgnoreCase))
            {
                index += 5;
                return false;
            }
            return false;
        }

        private static object ParseNull(string json, ref int index)
        {
            if (json.Substring(index).StartsWith("null", StringComparison.OrdinalIgnoreCase))
            {
                index += 4;
            }
            return null;
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }

    #endregion
}