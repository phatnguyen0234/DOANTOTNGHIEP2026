using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CraftingSystem))]
public class CraftingSystemEditor : Editor
{
    private const string PREFS_FOLDER_PATHS = "FarmingRPG_CraftingSystem_RecipeFolders";
    private const string PREFS_INCLUDE_SUBFOLDERS = "FarmingRPG_CraftingSystem_IncludeSubfolders";
    private const string PREFS_AUTO_SYNC_ON_DROP = "FarmingRPG_CraftingSystem_AutoSyncOnDrop";

    private readonly List<DefaultAsset> recipeFolders = new List<DefaultAsset>();
    private bool includeSubfolders = true;
    private bool autoSyncOnDrop = true;
    private string lastStatusMessage = "";

    private void OnEnable()
    {
        includeSubfolders = EditorPrefs.GetBool(PREFS_INCLUDE_SUBFOLDERS, true);
        autoSyncOnDrop = EditorPrefs.GetBool(PREFS_AUTO_SYNC_ON_DROP, true);
        LoadSavedFolders();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        CraftingSystem craftingSystem = (CraftingSystem)target;

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Master Recipe Database Tools", EditorStyles.boldLabel);

        // 1. One-click full project scan
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Option 1: Scan Entire Project (1-Click)", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.65f, 0.95f, 0.7f, 1f);
        if (GUILayout.Button("Sync All Recipes In Project", GUILayout.Height(30)))
        {
            SyncAllRecipesInProject(craftingSystem);
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clean Nulls & Sort", GUILayout.Width(130), GUILayout.Height(30)))
        {
            CleanAndSortExisting(craftingSystem);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        // 2. Drag & Drop Folders section
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Option 2: Sync From Specific Folders", EditorStyles.miniBoldLabel);

        // Drag & Drop Zone
        Rect dropArea = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
        GUI.Box(
            dropArea,
            "Drag & Drop Recipe Folders Here\n(Supports dropping multiple folders at once)",
            EditorStyles.helpBox
        );
        HandleFolderDragAndDrop(dropArea, craftingSystem);

        EditorGUILayout.Space(4);

        // Folder list management
        bool foldersChanged = false;
        for (int i = 0; i < recipeFolders.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            DefaultAsset updated = (DefaultAsset)EditorGUILayout.ObjectField(
                $"Folder {i + 1}",
                recipeFolders[i],
                typeof(DefaultAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                if (updated == null || AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(updated)))
                {
                    recipeFolders[i] = updated;
                    foldersChanged = true;
                }
                else
                {
                    Debug.LogWarning("[CraftingSystemEditor] Please assign a folder, not a file.");
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                recipeFolders.RemoveAt(i);
                foldersChanged = true;
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Folder Slot", GUILayout.Width(130)))
        {
            recipeFolders.Add(null);
            foldersChanged = true;
        }
        if (recipeFolders.Count > 0 && GUILayout.Button("Clear Folders", GUILayout.Width(100)))
        {
            recipeFolders.Clear();
            foldersChanged = true;
        }
        EditorGUILayout.EndHorizontal();

        if (foldersChanged)
        {
            SaveFolders();
        }

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();
        includeSubfolders = EditorGUILayout.ToggleLeft("Include subfolders when scanning", includeSubfolders);
        autoSyncOnDrop = EditorGUILayout.ToggleLeft("Auto-sync immediately when dropping folders", autoSyncOnDrop);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool(PREFS_INCLUDE_SUBFOLDERS, includeSubfolders);
            EditorPrefs.SetBool(PREFS_AUTO_SYNC_ON_DROP, autoSyncOnDrop);
        }

        EditorGUILayout.Space(4);

        bool hasAnyValidFolder = GetValidFolderPaths().Count > 0;
        GUI.enabled = hasAnyValidFolder;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Replace List From Folders", GUILayout.Height(28)))
        {
            SyncFromFolders(craftingSystem, replaceExisting: true);
        }
        if (GUILayout.Button("Append From Folders", GUILayout.Height(28)))
        {
            SyncFromFolders(craftingSystem, replaceExisting: false);
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        if (!string.IsNullOrEmpty(lastStatusMessage))
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(lastStatusMessage, MessageType.Info);
        }
    }

    private void HandleFolderDragAndDrop(Rect dropArea, CraftingSystem craftingSystem)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
            return;

        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            bool hasFolder = DragAndDrop.objectReferences.Any(obj =>
                obj is DefaultAsset && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj))
            );

            if (!hasFolder)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                int added = 0;

                foreach (UnityEngine.Object draggedObj in DragAndDrop.objectReferences)
                {
                    if (draggedObj is DefaultAsset folderAsset)
                    {
                        string path = AssetDatabase.GetAssetPath(folderAsset);
                        if (AssetDatabase.IsValidFolder(path) && !recipeFolders.Contains(folderAsset))
                        {
                            recipeFolders.Add(folderAsset);
                            added++;
                        }
                    }
                }

                // Remove trailing empty slots if user dragged onto the box
                recipeFolders.RemoveAll(f => f == null);
                SaveFolders();

                if (autoSyncOnDrop)
                {
                    SyncFromFolders(craftingSystem, replaceExisting: true);
                }
                else if (added > 0)
                {
                    lastStatusMessage = $"Added {added} folder(s). Click 'Replace List From Folders' to load recipes.";
                }
            }

            evt.Use();
        }
    }

    private void SyncAllRecipesInProject(CraftingSystem craftingSystem)
    {
        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe");
        List<CraftingRecipe> loaded = LoadUniqueSortedRecipes(guids, null);

        ApplyRecipesToSystem(craftingSystem, loaded, "Sync All Recipes In Project");
        lastStatusMessage = $"Synced {loaded.Count} CraftingRecipe asset(s) from the entire project.";
    }

    private void SyncFromFolders(CraftingSystem craftingSystem, bool replaceExisting)
    {
        List<string> validFolders = GetValidFolderPaths();
        if (validFolders.Count == 0)
        {
            lastStatusMessage = "No valid folders selected.";
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:CraftingRecipe", validFolders.ToArray());
        HashSet<string> allowedRoots = includeSubfolders
            ? null
            : new HashSet<string>(validFolders, StringComparer.OrdinalIgnoreCase);

        List<CraftingRecipe> loaded = LoadUniqueSortedRecipes(guids, allowedRoots);

        if (!replaceExisting && craftingSystem.allRecipes != null)
        {
            HashSet<CraftingRecipe> merged = new HashSet<CraftingRecipe>(
                craftingSystem.allRecipes.Where(r => r != null)
            );
            foreach (var r in loaded)
                merged.Add(r);

            loaded = SortRecipes(merged);
        }

        ApplyRecipesToSystem(craftingSystem, loaded, "Sync Recipes From Folders");
        lastStatusMessage = $"Synced {loaded.Count} CraftingRecipe asset(s) from {validFolders.Count} folder(s).";
    }

    private void CleanAndSortExisting(CraftingSystem craftingSystem)
    {
        if (craftingSystem.allRecipes == null)
            craftingSystem.allRecipes = new List<CraftingRecipe>();

        HashSet<CraftingRecipe> unique = new HashSet<CraftingRecipe>(
            craftingSystem.allRecipes.Where(r => r != null)
        );
        List<CraftingRecipe> cleaned = SortRecipes(unique);

        ApplyRecipesToSystem(craftingSystem, cleaned, "Clean And Sort Recipes");
        lastStatusMessage = $"Cleaned & sorted list: {cleaned.Count} unique recipe(s) remaining.";
    }

    private static List<CraftingRecipe> LoadUniqueSortedRecipes(string[] guids, HashSet<string> exactFoldersOnly)
    {
        HashSet<CraftingRecipe> unique = new HashSet<CraftingRecipe>();

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (exactFoldersOnly != null)
            {
                string parentDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(parentDir) || !exactFoldersOnly.Contains(parentDir))
                    continue;
            }

            CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(assetPath);
            if (recipe != null)
            {
                unique.Add(recipe);
            }
        }

        return SortRecipes(unique);
    }

    private static List<CraftingRecipe> SortRecipes(IEnumerable<CraftingRecipe> recipes)
    {
        return recipes
            .Where(r => r != null)
            .OrderBy(r => r.category)
            .ThenBy(r => r.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ApplyRecipesToSystem(CraftingSystem craftingSystem, List<CraftingRecipe> recipes, string undoLabel)
    {
        Undo.RecordObject(craftingSystem, undoLabel);
        craftingSystem.allRecipes = recipes;
        EditorUtility.SetDirty(craftingSystem);
        Debug.Log($"[CraftingSystemEditor] {undoLabel}: {recipes.Count} recipes assigned to '{craftingSystem.name}'.");
    }

    private List<string> GetValidFolderPaths()
    {
        List<string> paths = new List<string>();
        foreach (var folder in recipeFolders)
        {
            if (folder == null) continue;
            string p = AssetDatabase.GetAssetPath(folder);
            if (AssetDatabase.IsValidFolder(p) && !paths.Contains(p))
            {
                paths.Add(p);
            }
        }
        return paths;
    }

    private void SaveFolders()
    {
        List<string> paths = GetValidFolderPaths();
        EditorPrefs.SetString(PREFS_FOLDER_PATHS, string.Join("|", paths));
    }

    private void LoadSavedFolders()
    {
        recipeFolders.Clear();
        string raw = EditorPrefs.GetString(PREFS_FOLDER_PATHS, "");
        if (string.IsNullOrEmpty(raw))
            return;

        string[] paths = raw.Split('|');
        foreach (string p in paths)
        {
            if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
            {
                DefaultAsset folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(p);
                if (folderAsset != null && !recipeFolders.Contains(folderAsset))
                {
                    recipeFolders.Add(folderAsset);
                }
            }
        }
    }
}