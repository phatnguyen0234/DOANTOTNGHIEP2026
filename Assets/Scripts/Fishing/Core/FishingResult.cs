using System;
using UnityEngine;

// Cấu trúc dữ liệu chứa kết quả chi tiết sau một lượt câu cá hoàn chỉnh
[Serializable]
public class FishingResult
{
    [Tooltip("Loài cá đã câu được (nếu thành công).")]
    public FishData CaughtFish;

    [Tooltip("Lượt câu có thành công hay không.")]
    public bool IsSuccess;

    [Tooltip("Có đạt danh hiệu Perfect Catch (cá chưa từng rời khỏi thanh xanh) không.")]
    public bool IsPerfect;

    [Tooltip("Điểm kinh nghiệm nhận được.")]
    public int EarnedExp;

    [Tooltip("ItemData được trao thưởng vào Inventory.")]
    public ItemData RewardItem;

    [Tooltip("Số lượng item nhận được.")]
    public int Quantity = 1;

    public FishingResult(FishData fish, bool isSuccess, bool isPerfect, int baseExp, ItemData rewardItem, int quantity = 1)
    {
        CaughtFish = fish;
        IsSuccess = isSuccess;
        IsPerfect = isPerfect;
        EarnedExp = isPerfect ? Mathf.RoundToInt(baseExp * 1.5f) : baseExp;
        RewardItem = rewardItem;
        Quantity = quantity;
    }

    public static FishingResult CreateFailed()
    {
        return new FishingResult(null, false, false, 0, null, 0);
    }
}
