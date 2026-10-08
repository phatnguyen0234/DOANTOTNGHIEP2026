using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FishSpawnEntry
{
    [Tooltip("Dữ liệu loài cá.")]
    public FishData fishData;

    [Tooltip("Trọng số xuất hiện tại điểm câu này (càng cao càng dễ gặp).")]
    [Range(1, 100)]
    public int weight = 10;
}

// ScriptableObject định nghĩa danh sách cá có thể câu được tại một địa điểm câu cụ thể
[CreateAssetMenu(fileName = "NewFishingSpotData", menuName = "Fishing/Spot Data")]
public class FishingSpotData : ScriptableObject
{
    [Tooltip("Tên của khu vực câu cá (ví dụ: Ao Nông Trại, Sông Rừng, Bờ Biển).")]
    [SerializeField] private string spotName = "Ao Làng";

    [Tooltip("Danh sách các loài cá có thể xuất hiện tại đây.")]
    [SerializeField] private List<FishSpawnEntry> availableFishes = new List<FishSpawnEntry>();

    [Tooltip("Cá mặc định fallback nếu không chọn được cá nào.")]
    [SerializeField] private FishData fallbackFish;

    public string SpotName => spotName;
    public IReadOnlyList<FishSpawnEntry> AvailableFishes => availableFishes;

    // Bốc ngẫu nhiên 1 loài cá dựa trên tỉ lệ trọng số (Weight-based random)
    public FishData PickRandomFish()
    {
        if (availableFishes == null || availableFishes.Count == 0)
        {
            if (fallbackFish != null)
            {
                Debug.Log($"<color=#FFA500>[FishingSpotData] Spot '{spotName}' không có danh sách cá, chọn cá fallback: <b>{fallbackFish.FishName}</b></color>");
            }
            return fallbackFish;
        }

        int totalWeight = 0;
        for (int i = 0; i < availableFishes.Count; i++)
        {
            if (availableFishes[i].fishData != null)
            {
                totalWeight += Mathf.Max(1, availableFishes[i].weight);
            }
        }

        if (totalWeight <= 0)
        {
            if (fallbackFish != null)
            {
                Debug.Log($"<color=#FFA500>[FishingSpotData] Spot '{spotName}' tổng trọng số <= 0, chọn cá fallback: <b>{fallbackFish.FishName}</b></color>");
            }
            return fallbackFish;
        }

        int randomVal = UnityEngine.Random.Range(0, totalWeight);
        int currentSum = 0;

        for (int i = 0; i < availableFishes.Count; i++)
        {
            FishSpawnEntry entry = availableFishes[i];
            if (entry.fishData == null) continue;

            currentSum += Mathf.Max(1, entry.weight);
            if (randomVal < currentSum)
            {
                Debug.Log($"<color=#00FF7F>[FishingSpotData] Spot '<b>{spotName}</b>' bốc trúng cá: <b>{entry.fishData.FishName}</b> (Tỉ lệ: {entry.weight}/{totalWeight})</color>");
                return entry.fishData;
            }
        }

        FishData fallbackResult = fallbackFish ?? availableFishes[0].fishData;
        if (fallbackResult != null)
        {
            Debug.Log($"<color=#FFA500>[FishingSpotData] Spot '<b>{spotName}</b>' chọn cá fallback cuối: <b>{fallbackResult.FishName}</b></color>");
        }
        return fallbackResult;
    }

    public override string ToString()
    {
        return $"{spotName} ({name})";
    }
}
