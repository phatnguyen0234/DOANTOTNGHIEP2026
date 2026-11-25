using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IngredientGroup))]
public class IngredientGroupEditor : Editor
{
    private DefaultAsset sourceFolder;
    private bool includeSubfolders = true;
    private bool clearExistingBeforeLoad = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Batch Folder Loader", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("Source Folder", "Drag a folder from the Project window (e.g., Assets/Data/InventoryItem/Animal/Fish)."),
            sourceFolder,
            typeof(DefaultAsset),
            false
        );

        includeSubfolders = EditorGUILayout.Toggle(
            new GUIContent("Include Subfolders", "Also scan nested folders such as Sea, River, Lake, etc."),
            includeSubfolders
        );

        clearExistingBeforeLoad = EditorGUILayout.Toggle(
            new GUIContent("Replace Existing List", "Clear current items before loading from folder."),
            clearExistingBeforeLoad
        );

        EditorGUILayout.Space(6);

        GUI.enabled = sourceFolder != null;
        if (GUILayout.Button("Load All ItemData From Folder", GUILayout.Height(28)))
        {
            LoadItemsFromFolder((IngredientGroup)target);
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void LoadItemsFromFolder(IngredientGroup group)
    {
        string folderPath = AssetDatabase.GetAssetPath(sourceFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning("[IngredientGroupEditor] Selected asset is not a valid folder.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folderPath });
        List<ItemData> loadedItems = new();
        HashSet<ItemData> seen = new();

        if (!clearExistingBeforeLoad && group.items != null)
        {
            for (int i = 0; i < group.items.Count; i++)
            {
                if (group.items[i] != null && seen.Add(group.items[i]))
                {
                    loadedItems.Add(group.items[i]);
                }
            }
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!includeSubfolders)
            {
                string parentDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (parentDir != folderPath)
                    continue;
            }

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (item != null && seen.Add(item))
            {
                loadedItems.Add(item);
            }
        }

        loadedItems.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        Undo.RecordObject(group, "Load ItemData Into IngredientGroup");
        group.items = loadedItems;
        EditorUtility.SetDirty(group);
        AssetDatabase.SaveAssets();

        Debug.Log($"[IngredientGroupEditor] Loaded {loadedItems.Count} ItemData assets into '{group.name}' from '{folderPath}'.");
    }
}