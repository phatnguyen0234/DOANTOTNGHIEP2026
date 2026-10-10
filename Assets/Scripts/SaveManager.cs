using NUnit.Framework;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public string savepath;
    public SaveData saveData;
    public List<ItemData> itemDatabase = new List<ItemData>();
    void Awake()
    {
        Instance = this;
        savepath = Path.Combine(Application.persistentDataPath, "save.json");
    }

    private void Start()
    {
        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    public void SaveGame()
    {
        GameObject player = GameObject.FindWithTag("Player");
        saveData.playerData = new PlayerData(player.transform.position);

        if(Inventory.Instance != null)
        {
            List<InventorySlotData> slotDataList = new List<InventorySlotData>();
            for(int i = 0; i < Inventory.Instance.Slots.Count; i++)
            {
                InventorySlot slot = Inventory.Instance.GetSlot(i);
                if(!slot.IsEmpty())
                {
                    slotDataList.Add(new InventorySlotData(i, slot.ItemData.ItemID, slot.Amount));
                }
            }
            saveData.inventoryData = new InventoryData(Inventory.Instance.Capacity, slotDataList);
        }
        
        if(TimeManager.Instance != null)
        {
            saveData.gameTimeData = new GameTimeData(TimeManager.Instance.currentHour, TimeManager.Instance.currentDay, TimeManager.Instance.currentSeason);
        }

        if(SoilManager.Instance != null)
        {
            saveData.farmData = SoilManager.Instance.currentFarmData;
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savepath, json);
    }

    public void LoadGame()
    {
        if(!File.Exists(savepath))
        {
            return;
        }

        string json = File.ReadAllText(savepath);
        saveData = JsonUtility.FromJson<SaveData>(json);

        GameObject player = GameObject.FindWithTag("Player");
        player.transform.position = saveData.playerData.position;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.ClearAll();
            foreach(InventorySlotData slotData in saveData.inventoryData.slots)
            {
                ItemData itemData = ItemDatabase.Instance.GetItemById(slotData.itemId);
                if (itemData != null)
                {
                    Inventory.Instance.AddItemToSlot(slotData.slotIndex, itemData, slotData.amount);
                }
            }
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.currentHour = saveData.gameTimeData.currentHour;
            TimeManager.Instance.currentDay = saveData.gameTimeData.currentDay;
            TimeManager.Instance.currentSeason = saveData.gameTimeData.currentSeason;
        }

        if(SoilManager.Instance != null)
        {
            SoilManager.Instance.currentFarmData = saveData.farmData;
            SoilManager.Instance.ReBuildFarm();
            SoilManager.Instance.ReBuildTree();
            SoilManager.Instance.ReBuildRock();
        }
    }

    [ContextMenu("Open json")]
    public void OpenJson()
    {
        if (File.Exists(savepath))
        {
            Application.OpenURL(savepath);
        }
    }
}
