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
    private static bool IsInTargetFolder(string assetPath)
    {
        if (TargetFolders == null || TargetFolders.Count == 0) return true;

        foreach (string folderPath in TargetFolders)
        {
            if (assetPath.StartsWith(folderPath))
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
// This class creates a standalone Editor Window for the settings
// ---------------------------------------------------------
public class ItemDataIconSettingsWindow : EditorWindow
{
    // Adds this window to the "Farming Tools" menu alongside the Recipe Checker
    [MenuItem("Farming Tools/Item Icon Settings")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataIconSettingsWindow>("Icon Settings");
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

            // Delete button for this row
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                ItemDataProjectIcon.TargetFolders.RemoveAt(i);
                ItemDataProjectIcon.SaveSettings();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // Button to add a new folder path row
        if (GUILayout.Button("Add New Folder Path", GUILayout.Height(25)))
        {
            ItemDataProjectIcon.TargetFolders.Add("Assets/...");
            ItemDataProjectIcon.SaveSettings();
        }

        // Auto-save when the user types or modifies a field
        if (GUI.changed)
        {
            ItemDataProjectIcon.SaveSettings();
        }
    }
}