using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    public List<ItemData> itemDatabase = new List<ItemData>();
    public Dictionary<string, ItemData> itemDictionary = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeItemDictionary();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeItemDictionary()
    {
        foreach (ItemData item in itemDatabase)
        {
            if (!itemDictionary.ContainsKey(item.ItemID))
            {
                itemDictionary.Add(item.ItemID, item);
            }
        }
    }

    public ItemData GetItemById(string itemId)
    {
        if (itemDictionary.TryGetValue(itemId, out ItemData item))
        {
            return item;
        }
        return null;
    }

    [ContextMenu("Get ItemData")]
    public void GetItemData()
    {
        itemDatabase.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (itemData != null)
            {
                itemDatabase.Add(itemData);
            }
        }
    }
}
