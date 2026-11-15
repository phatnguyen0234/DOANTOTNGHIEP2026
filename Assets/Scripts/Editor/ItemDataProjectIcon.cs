using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public class ItemDataProjectIcon
{
    // Unique key to save settings per project
    private static string PREFS_KEY => Application.productName + "_ItemDataIconFolders";

    // List to hold all configured folder paths
    public static List<string> TargetFolders = new List<string>();

    // Static constructor called automatically by Unity on load or recompile
    static ItemDataProjectIcon()
    {
        LoadSettings();
        EditorApplication.projectWindowItemOnGUI += DrawItemDataIcon;
    }

    // Loads the saved folder paths from EditorPrefs
    public static void LoadSettings()
    {
        string savedPaths = EditorPrefs.GetString(PREFS_KEY, "Assets/Data/InventoryItem");
        TargetFolders = savedPaths.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    // Saves the current folder paths to EditorPrefs and refreshes the window
    public static void SaveSettings()
    {
        string joinedPaths = string.Join(";", TargetFolders);
        EditorPrefs.SetString(PREFS_KEY, joinedPaths);
        EditorApplication.RepaintProjectWindow();
    }

    // Main drawing method triggered for each item in the Project Window
    private static void DrawItemDataIcon(string guid, Rect selectionRect)
    {
        // 1. Convert GUID to asset path
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(assetPath)) return;

        // 2. Check if the asset is inside one of our target folders
        if (!IsInTargetFolder(assetPath)) return;

        // 3. Load the asset to check if it's an ItemData ScriptableObject
        ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (itemData == null || itemData.Icon == null) return;

        // 4. Extract the sprite and texture
        Sprite iconSprite = itemData.Icon;
        Texture2D iconTexture = iconSprite.texture;
        if (iconTexture == null) return;

        // 5. Calculate drawing bounds based on current view mode (Grid or List)
        Rect iconRect = GetIconRect(selectionRect);

        // 6. Draw a dark grey background to cover the default Unity ScriptableObject icon
        EditorGUI.DrawRect(iconRect, new Color(0.2f, 0.2f, 0.2f, 1f));

        // 7. Calculate the exact cropped area (UV coordinates) of the sprite on the sprite sheet
        Rect texRect = iconSprite.textureRect;
        Rect uvRect = new Rect(
            texRect.x / iconTexture.width,
            texRect.y / iconTexture.height,
            texRect.width / iconTexture.width,
            texRect.height / iconTexture.height
        );

        // 8. Draw only the specific cropped sprite portion with alpha blending
        GUI.DrawTextureWithTexCoords(iconRect, iconTexture, uvRect, true);
    }

    // Validates if the given path starts with any of the configured target folders
    public static bool IsInTargetFolder(string assetPath)
    {
        if (TargetFolders == null || TargetFolders.Count == 0) return true;

        foreach (string folderPath in TargetFolders)
        {
            if (!string.IsNullOrWhiteSpace(folderPath) && assetPath.StartsWith(folderPath))
            {
                return true;
            }
        }
        return false;
    }

    // Calculates the correct rectangle area to draw the icon safely
    private static Rect GetIconRect(Rect selectionRect)
    {
        // Height > 20 indicates Grid view (Large icons)
        if (selectionRect.height > 20f)
        {
            float iconSize = Mathf.Min(selectionRect.width, selectionRect.height) - 16f;
            return new Rect(
                selectionRect.x + (selectionRect.width - iconSize) / 2f,
                selectionRect.y + 2f,
                iconSize,
                iconSize
            );
        }
        else // List view (Small icons)
        {
            return new Rect(selectionRect.x, selectionRect.y, 16f, 16f);
        }
    }
}

// ---------------------------------------------------------
// Standalone Editor Window for Folder Settings & Duplicate Icon Audit
// ---------------------------------------------------------
public class ItemDataIconSettingsWindow : EditorWindow
{
    private class DuplicateIconGroup
    {
        public Sprite sharedSprite;
        public List<ItemData> items = new List<ItemData>();
    }

    private List<DuplicateIconGroup> duplicateGroups = new List<DuplicateIconGroup>();
    private int totalScannedItems = 0;
    private int missingIconCount = 0;
    private bool hasScanned = false;
    private Vector2 scrollPosition;

    [MenuItem("Farming Tools/Item Icon Settings")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataIconSettingsWindow>("Icon Settings");
    }

    private void OnEnable()
    {
        ScanForDuplicateIcons();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Target Folders for Custom Icons", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Enter the folder paths (e.g., Assets/Data/InventoryItem). Custom icons will only replace ScriptableObject logos inside these specific directories.", MessageType.Info);

        EditorGUILayout.Space();

        // Draw input fields for every folder in the list
        for (int i = 0; i < ItemDataProjectIcon.TargetFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            ItemDataProjectIcon.TargetFolders[i] = EditorGUILayout.TextField(ItemDataProjectIcon.TargetFolders[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                ItemDataProjectIcon.TargetFolders.RemoveAt(i);
                ItemDataProjectIcon.SaveSettings();
                ScanForDuplicateIcons();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Add New Folder Path", GUILayout.Height(25)))
        {
            ItemDataProjectIcon.TargetFolders.Add("Assets/...");
            ItemDataProjectIcon.SaveSettings();
        }

        if (GUI.changed)
        {
            ItemDataProjectIcon.SaveSettings();
        }

        // ---------------------------------------------------------
        // Duplicate Icon Audit Section
        // ---------------------------------------------------------
        GUILayout.Space(15);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);

        EditorGUILayout.LabelField("Duplicate Icon Audit", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Target Folders & Check Duplicate Icons", GUILayout.Height(30)))
        {
            ScanForDuplicateIcons();
        }

        if (!hasScanned) return;

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            $"Scanned ItemData Assets: {totalScannedItems}\n" +
            $"Items Without Icon: {missingIconCount}\n" +
            $"Shared Icon Groups Found: {duplicateGroups.Count}",
            MessageType.Info
        );

        if (duplicateGroups.Count == 0)
        {
            EditorGUILayout.HelpBox("All scanned ItemData assets use unique icons!", MessageType.None);
            return;
        }

        GUILayout.Space(5);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, "box");

        foreach (var group in duplicateGroups)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();

            // Draw cropped sprite preview
            Rect previewRect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36), GUILayout.Height(36));
            DrawSpritePreview(previewRect, group.sharedSprite);

            GUILayout.Space(8);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Sprite: {group.sharedSprite.name} ({group.items.Count} items)", EditorStyles.boldLabel);

            foreach (ItemData item in group.items)
            {
                if (item == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {item.name}  (ID: {item.ItemID})", GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();
    }

    private void ScanForDuplicateIcons()
    {
        duplicateGroups.Clear();
        totalScannedItems = 0;
        missingIconCount = 0;
        hasScanned = true;

        // Filter only valid existing folders so AssetDatabase.FindAssets does not throw errors
        string[] validFolders = ItemDataProjectIcon.TargetFolders
            .Where(path => !string.IsNullOrWhiteSpace(path) && AssetDatabase.IsValidFolder(path))
            .ToArray();

        string[] guids = validFolders.Length > 0
            ? AssetDatabase.FindAssets("t:ItemData", validFolders)
            : AssetDatabase.FindAssets("t:ItemData");

        Dictionary<Sprite, List<ItemData>> spriteUsageMap = new Dictionary<Sprite, List<ItemData>>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (itemData == null) continue;

            totalScannedItems++;

            if (itemData.Icon == null)
            {
                missingIconCount++;
                continue;
            }

            if (!spriteUsageMap.TryGetValue(itemData.Icon, out List<ItemData> itemList))
            {
                itemList = new List<ItemData>();
                spriteUsageMap[itemData.Icon] = itemList;
            }

            itemList.Add(itemData);
        }

        // Keep only sprites used by 2 or more ItemData assets
        foreach (var kvp in spriteUsageMap)
        {
            if (kvp.Value.Count > 1)
            {
                duplicateGroups.Add(new DuplicateIconGroup
                {
                    sharedSprite = kvp.Key,
                    items = kvp.Value.OrderBy(i => i.name).ToList()
                });
            }
        }

        duplicateGroups = duplicateGroups.OrderByDescending(g => g.items.Count).ToList();
        EditorApplication.RepaintProjectWindow();
    }

    private void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1f));

        Texture2D tex = sprite.texture;
        Rect texRect = sprite.textureRect;
        Rect uvRect = new Rect(
            texRect.x / tex.width,
            texRect.y / tex.height,
            texRect.width / tex.width,
            texRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(rect, tex, uvRect, true);
    }
}