using System.Collections.Generic;
using UnityEngine;

// Thành phần lựa chọn loài cá xuất hiện trong lượt câu dựa theo địa điểm và độ hiếm
public class FishSpawner : MonoBehaviour
{
    [Header("Default Fish Pool")]
    [Tooltip("Danh sách các loài cá mặc định của thế giới nếu không ở trong Spot đặc biệt.")]
    [SerializeField] private List<FishData> defaultFishPool = new List<FishData>();

    [Tooltip("Cá mặc định nếu danh sách rỗng.")]
    [SerializeField] private FishData fallbackFish;

    // Chọn ra 1 loài cá cho lượt câu hiện tại
    public FishData SpawnFish(FishingSpotData spotData, FishingRodData rodData)
    {
        // 1. Ưu tiên lấy từ dữ liệu điểm câu (FishingSpotData) nếu có
        if (spotData != null)
        {
            FishData picked = spotData.PickRandomFish();
            if (picked != null) return picked;
        }

        // 2. Chọn từ bảng cá mặc định
        if (defaultFishPool != null && defaultFishPool.Count > 0)
        {
            int totalWeight = 0;
            for (int i = 0; i < defaultFishPool.Count; i++)
            {
                if (defaultFishPool[i] != null)
                {
                    totalWeight += Mathf.Max(1, defaultFishPool[i].SpawnWeight);
                }
            }

            if (totalWeight > 0)
            {
                int randomVal = Random.Range(0, totalWeight);
                int currentSum = 0;

                for (int i = 0; i < defaultFishPool.Count; i++)
                {
                    FishData fish = defaultFishPool[i];
                    if (fish == null) continue;

                    currentSum += Mathf.Max(1, fish.SpawnWeight);
                    if (randomVal < currentSum)
                    {
                        return fish;
                    }
                }
            }

            return defaultFishPool[0];
        }

        return fallbackFish;
    }
}
