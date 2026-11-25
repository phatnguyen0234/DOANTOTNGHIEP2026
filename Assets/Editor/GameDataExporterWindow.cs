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
            "Scans all ItemData, RecipeScrollItemData, IngredientGroup, and CraftingRecipe assets in the project, " +
            "serializes them into JSON (including 3x3 slots & wildcard groups), and syncs them to your Google Sheet.",
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
            lastGeneratedJson = BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount, out int groupCount);
            SaveLocalJsonBackup(lastGeneratedJson);
            statusMessage = $"Generated JSON successfully: {itemCount} Items, {scrollCount} Scrolls, {groupCount} Groups, {recipeCount} Recipes.";
            statusType = MessageType.Info;
        }

        GUI.enabled = !isUploading && !string.IsNullOrWhiteSpace(webhookUrl);
        if (GUILayout.Button(isUploading ? "Syncing..." : "2. Export & Sync to Google Sheet", GUILayout.Height(35)))
        {
            lastGeneratedJson = BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount, out int groupCount);
            SaveLocalJsonBackup(lastGeneratedJson);
            SendJsonToGoogleSheet(lastGeneratedJson, itemCount, scrollCount, recipeCount, groupCount);
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

    private string BuildGameDataJson(out int itemCount, out int scrollCount, out int recipeCount, out int groupCount)
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

        // 2. Scan all IngredientGroup assets
        string[] groupGuids = AssetDatabase.FindAssets("t:IngredientGroup");
        foreach (string guid in groupGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            IngredientGroup grp = AssetDatabase.LoadAssetAtPath<IngredientGroup>(path);
            if (grp == null) continue;

            List<string> memberIds = new List<string>();
            if (grp.items != null)
            {
                foreach (var member in grp.items)
                {
                    if (member != null && !string.IsNullOrEmpty(member.ItemID))
                    {
                        memberIds.Add(member.ItemID);
                    }
                }
            }

            payload.ingredientGroups.Add(new IngredientGroupExportDto
            {
                groupAssetName = grp.name,
                groupDisplayName = grp.groupName,
                memberCount = memberIds.Count,
                memberItemIDs = memberIds
            });
        }

        // 3. Scan all CraftingRecipe assets
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

            string[] serializedSlots = new string[9];
            Dictionary<string, IngredientExportDto> aggregatedIngredients = new Dictionary<string, IngredientExportDto>();

            if (recipe.requiredIngredients != null)
            {
                int slotCount = Mathf.Min(9, recipe.requiredIngredients.Length);
                for (int i = 0; i < slotCount; i++)
                {
                    var ing = recipe.requiredIngredients[i];
                    if (ing.amount <= 0)
                    {
                        serializedSlots[i] = "";
                        continue;
                    }

                    if (ing.IsWildcard)
                    {
                        string groupKey = $"group:{ing.ingredientGroup.name}";
                        serializedSlots[i] = $"{groupKey}:{ing.amount}";

                        if (aggregatedIngredients.TryGetValue(groupKey, out var existingGroup))
                        {
                            existingGroup.totalAmount += ing.amount;
                        }
                        else
                        {
                            aggregatedIngredients[groupKey] = new IngredientExportDto
                            {
                                itemID = groupKey,
                                itemName = $"[Group: {ing.ingredientGroup.groupName}]",
                                totalAmount = ing.amount,
                                isWildcardGroup = true
                            };
                        }
                    }
                    else if (ing.item != null)
                    {
                        serializedSlots[i] = $"{ing.item.ItemID}:{ing.amount}";

                        if (aggregatedIngredients.TryGetValue(ing.item.ItemID, out var existingItem))
                        {
                            existingItem.totalAmount += ing.amount;
                        }
                        else
                        {
                            aggregatedIngredients[ing.item.ItemID] = new IngredientExportDto
                            {
                                itemID = ing.item.ItemID,
                                itemName = ing.item.ItemName,
                                totalAmount = ing.amount,
                                isWildcardGroup = false
                            };
                        }
                    }
                    else
                    {
                        serializedSlots[i] = "";
                    }
                }
            }

            recipeDto.slot0 = serializedSlots[0] ?? "";
            recipeDto.slot1 = serializedSlots[1] ?? "";
            recipeDto.slot2 = serializedSlots[2] ?? "";
            recipeDto.slot3 = serializedSlots[3] ?? "";
            recipeDto.slot4 = serializedSlots[4] ?? "";
            recipeDto.slot5 = serializedSlots[5] ?? "";
            recipeDto.slot6 = serializedSlots[6] ?? "";
            recipeDto.slot7 = serializedSlots[7] ?? "";
            recipeDto.slot8 = serializedSlots[8] ?? "";

            recipeDto.ingredients = aggregatedIngredients.Values.ToList();
            recipeDto.ingredientsSummary = string.Join(" + ", recipeDto.ingredients.Select(i => $"{i.totalAmount}x {i.itemName} ({i.itemID})"));

            payload.recipes.Add(recipeDto);
        }

        // Sort cleanly for readability
        payload.items = payload.items.OrderBy(i => i.itemType).ThenBy(i => i.itemID).ToList();
        payload.scrolls = payload.scrolls.OrderBy(s => s.scrollItemID).ToList();
        payload.ingredientGroups = payload.ingredientGroups.OrderBy(g => g.groupAssetName).ToList();
        payload.recipes = payload.recipes.OrderBy(r => r.category).ThenBy(r => r.resultItemID).ToList();

        itemCount = payload.items.Count;
        scrollCount = payload.scrolls.Count;
        groupCount = payload.ingredientGroups.Count;
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

    private void SendJsonToGoogleSheet(string json, int itemCount, int scrollCount, int recipeCount, int groupCount)
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
                statusMessage = $"Synced to Google Sheet! ({itemCount} Items, {scrollCount} Scrolls, {groupCount} Groups, {recipeCount} Recipes).";
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
        public List<IngredientGroupExportDto> ingredientGroups = new List<IngredientGroupExportDto>();
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
    private class IngredientGroupExportDto
    {
        public string groupAssetName;
        public string groupDisplayName;
        public int memberCount;
        public List<string> memberItemIDs = new List<string>();
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
        public string slot0;
        public string slot1;
        public string slot2;
        public string slot3;
        public string slot4;
        public string slot5;
        public string slot6;
        public string slot7;
        public string slot8;
        public string ingredientsSummary;
        public List<IngredientExportDto> ingredients = new List<IngredientExportDto>();
    }

    [Serializable]
    private class IngredientExportDto
    {
        public string itemID;
        public string itemName;
        public int totalAmount;
        public bool isWildcardGroup;
    }

    #endregion
}