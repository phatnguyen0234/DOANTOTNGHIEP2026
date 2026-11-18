using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class GameDataExporterWindow : EditorWindow
{
    private const string PREFS_WEBHOOK_URL = "FarmingRPG_GoogleSheetWebhookURL";

    private string webhookUrl = "";
    private string lastGeneratedJson = "";
    private string statusMessage = "";
    private MessageType statusType = MessageType.Info;
    private Vector2 scrollPos;
    private bool isUploading = false;

    [MenuItem("Farming Tools/Export Game Data to Google Sheet")]
    public static void ShowWindow()
    {
        GetWindow<GameDataExporterWindow>("Data Exporter");
    }

    private void OnEnable()
    {
        webhookUrl = EditorPrefs.GetString(PREFS_WEBHOOK_URL, "");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Game Economy & Crafting Data Exporter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scans all ItemData, RecipeScrollItemData, and CraftingRecipe assets in the project, " +
            "serializes them into JSON, and syncs them directly to your Google Sheet.",
            MessageType.Info
        );

        GUILayout.Space(8);

        // Google Apps Script Webhook URL Field
        EditorGUILayout.LabelField("Google Apps Script Web App URL:", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        webhookUrl = EditorGUILayout.TextField(webhookUrl);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(PREFS_WEBHOOK_URL, webhookUrl.Trim());
        }

        GUILayout.Space(12);

        // Action Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("1. Generate JSON Preview & Save Local", GUILayout.Height(35)))
        {
            lastGeneratedJson = BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount);
            SaveLocalJsonBackup(lastGeneratedJson);
            statusMessage = $"Generated JSON successfully: {itemCount} Items, {scrollCount} Scrolls, {recipeCount} Recipes.";
            statusType = MessageType.Info;
        }

        GUI.enabled = !isUploading && !string.IsNullOrWhiteSpace(webhookUrl);
        if (GUILayout.Button(isUploading ? "Syncing..." : "2. Export & Sync to Google Sheet", GUILayout.Height(35)))
        {
            lastGeneratedJson = BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount);
            SaveLocalJsonBackup(lastGeneratedJson);
            SendJsonToGoogleSheet(lastGeneratedJson, itemCount, scrollCount, recipeCount);
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        // JSON Preview Area
        if (!string.IsNullOrEmpty(lastGeneratedJson))
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Generated JSON Payload:", EditorStyles.boldLabel);
            if (GUILayout.Button("Copy JSON", GUILayout.Width(100)))
            {
                EditorGUIUtility.systemCopyBuffer = lastGeneratedJson;
                Debug.Log("[DataExporter] JSON copied to clipboard.");
            }
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box", GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(lastGeneratedJson, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private string BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount)
    {
        ExportPayload payload = new ExportPayload
        {
            exportedAtUtc = DateTime.UtcNow.ToString("o")
        };

        // 1. Scan all ItemData assets (including derived classes like RecipeScrollItemData)
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in itemGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (asset == null) continue;

            if (asset is RecipeScrollItemData scrollAsset)
            {
                payload.scrolls.Add(new ScrollExportDto
                {
                    scrollItemID = scrollAsset.ItemID,
                    scrollName = scrollAsset.ItemName,
                    buyPrice = scrollAsset.BuyPrice,
                    sellPrice = scrollAsset.SellPrice,
                    unlocksRecipeName = scrollAsset.recipeToUnlock != null ? scrollAsset.recipeToUnlock.name : "NONE",
                    unlocksResultItemID = (scrollAsset.recipeToUnlock != null && scrollAsset.recipeToUnlock.resultItem != null)
                        ? scrollAsset.recipeToUnlock.resultItem.ItemID
                        : "NONE"
                });
            }
            else
            {
                payload.items.Add(new ItemExportDto
                {
                    itemID = asset.ItemID,
                    itemName = asset.ItemName,
                    itemType = asset.ItemType.ToString(),
                    toolType = asset.ToolType.ToString(),
                    cropId = asset.CropId,
                    fertilizerType = asset.FertilizerType.ToString(),
                    maxStack = asset.MaxStack,
                    buyPrice = asset.BuyPrice,
                    sellPrice = asset.SellPrice,
                    canSell = asset.CanSell,
                    description = asset.Description
                });
            }
        }

        // 2. Scan all CraftingRecipe assets
        string[] recipeGuids = AssetDatabase.FindAssets("t:CraftingRecipe");
        foreach (string guid in recipeGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
            if (recipe == null) continue;

            RecipeExportDto recipeDto = new RecipeExportDto
            {
                recipeAssetName = recipe.name,
                category = recipe.category.ToString(),
                unlockType = recipe.unlockType.ToString(),
                resultItemID = recipe.resultItem != null ? recipe.resultItem.ItemID : "NONE",
                resultItemName = recipe.resultItem != null ? recipe.resultItem.ItemName : "NONE",
                resultAmount = recipe.resultAmount
            };

            // Aggregate ingredients by ItemID so the formula is easy to analyze economically
            Dictionary<string, IngredientExportDto> aggregatedIngredients = new Dictionary<string, IngredientExportDto>();
            if (recipe.requiredIngredients != null)
            {
                foreach (var ing in recipe.requiredIngredients)
                {
                    if (ing.item == null || ing.amount <= 0) continue;

                    if (aggregatedIngredients.TryGetValue(ing.item.ItemID, out var existing))
                    {
                        existing.totalAmount += ing.amount;
                    }
                    else
                    {
                        aggregatedIngredients[ing.item.ItemID] = new IngredientExportDto
                        {
                            itemID = ing.item.ItemID,
                            itemName = ing.item.ItemName,
                            totalAmount = ing.amount
                        };
                    }
                }
            }

            recipeDto.ingredients = aggregatedIngredients.Values.ToList();
            recipeDto.ingredientsSummary = string.Join(" + ", recipeDto.ingredients.Select(i => $"{i.totalAmount}x {i.itemName} ({i.itemID})"));

            payload.recipes.Add(recipeDto);
        }

        // Sort cleanly for readability
        payload.items = payload.items.OrderBy(i => i.itemType).ThenBy(i => i.itemID).ToList();
        payload.scrolls = payload.scrolls.OrderBy(s => s.scrollItemID).ToList();
        payload.recipes = payload.recipes.OrderBy(r => r.category).ThenBy(r => r.resultItemID).ToList();

        itemCount = payload.items.Count;
        scrollCount = payload.scrolls.Count;
        recipeCount = payload.recipes.Count;

        return JsonUtility.ToJson(payload, true);
    }

    private void SaveLocalJsonBackup(string json)
    {
        string folderPath = Path.Combine(Application.dataPath, "../GameDataExports");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, "GameEconomyData.json");
        File.WriteAllText(filePath, json, Encoding.UTF8);
        Debug.Log($"[DataExporter] Saved local backup to: {Path.GetFullPath(filePath)}");
    }

    private void SendJsonToGoogleSheet(string json, int itemCount, int scrollCount, int recipeCount)
    {
        isUploading = true;
        statusMessage = "Uploading data to Google Sheet...";
        statusType = MessageType.Info;

        UnityWebRequest request = new UnityWebRequest(webhookUrl.Trim(), "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var operation = request.SendWebRequest();
        operation.completed += _ =>
        {
            isUploading = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                statusMessage = $"Synced to Google Sheet! ({itemCount} Items, {scrollCount} Scrolls, {recipeCount} Recipes).";
                statusType = MessageType.Info;
                Debug.Log($"[DataExporter] Google Sheet Response: {request.downloadHandler.text}");
            }
            else
            {
                statusMessage = $"Upload failed: {request.error}\nResponse: {request.downloadHandler.text}";
                statusType = MessageType.Error;
                Debug.LogError($"[DataExporter] Sync Error: {request.error}");
            }

            request.Dispose();
            Repaint();
        };
    }

    #region Serializable DTOs for JsonUtility

    [Serializable]
    private class ExportPayload
    {
        public string exportedAtUtc;
        public List<ItemExportDto> items = new List<ItemExportDto>();
        public List<ScrollExportDto> scrolls = new List<ScrollExportDto>();
        public List<RecipeExportDto> recipes = new List<RecipeExportDto>();
    }

    [Serializable]
    private class ItemExportDto
    {
        public string itemID;
        public string itemName;
        public string itemType;
        public string toolType;
        public string cropId;
        public string fertilizerType;
        public int maxStack;
        public int buyPrice;
        public int sellPrice;
        public bool canSell;
        public string description;
    }

    [Serializable]
    private class ScrollExportDto
    {
        public string scrollItemID;
        public string scrollName;
        public int buyPrice;
        public int sellPrice;
        public string unlocksRecipeName;
        public string unlocksResultItemID;
    }

    [Serializable]
    private class RecipeExportDto
    {
        public string recipeAssetName;
        public string category;
        public string unlockType;
        public string resultItemID;
        public string resultItemName;
        public int resultAmount;
        public string ingredientsSummary;
        public List<IngredientExportDto> ingredients = new List<IngredientExportDto>();
    }

    [Serializable]
    private class IngredientExportDto
    {
        public string itemID;
        public string itemName;
        public int totalAmount;
    }

    #endregion
}