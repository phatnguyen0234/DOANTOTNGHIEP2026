using System;
using UnityEngine;

// Tính toán và theo dõi tiến độ bắt cá (Progress [0, 1]) cùng tiêu chí Perfect Catch
public class FishingProgress : MonoBehaviour
{
    [Header("Progress Settings")]
    [Tooltip("Tiến độ ban đầu khi bắt đầu minigame (mặc định 30%).")]
    [SerializeField] private float startProgress = 0.3f;

    [Tooltip("Tốc độ tăng tiến độ cơ bản mỗi giây khi cá nằm trong thanh xanh.")]
    [SerializeField] private float baseGainSpeed = 0.25f;

    [Tooltip("Tốc độ tụt tiến độ cơ bản mỗi giây khi cá ra ngoài thanh.")]
    [SerializeField] private float baseLossSpeed = 0.18f;

    public float Value { get; private set; } = 0.3f;
    public bool IsPerfect { get; private set; } = true;
    public bool IsFinished { get; private set; } = false;

    // Sự kiện cập nhật: (giá trị tiến độ, cá có đang trong thanh không)
    public event Action<float, bool> OnProgressChanged;

    // Sự kiện kết thúc minigame: (thành công?, hoàn hảo perfect?)
    public event Action<bool, bool> OnCompleted;

    private float currentGainMultiplier = 1.0f;
    private float currentLossReduction = 1.0f;

    public void Initialize(float gainMultiplier = 1.0f, float lossReduction = 1.0f)
    {
        Value = startProgress;
        IsPerfect = true;
        IsFinished = false;
        currentGainMultiplier = Mathf.Max(0.1f, gainMultiplier);
        currentLossReduction = Mathf.Max(0.1f, lossReduction);

        OnProgressChanged?.Invoke(Value, true);
    }

    public void UpdateProgress(bool isFishInside, float deltaTime)
    {
        if (IsFinished) return;

        if (isFishInside)
        {
            Value += baseGainSpeed * currentGainMultiplier * deltaTime;
        }
        else
        {
            Value -= (baseLossSpeed / currentLossReduction) * deltaTime;
            IsPerfect = false; // Một khi cá đã trượt khỏi thanh thì mất Perfect
        }

        Value = Mathf.Clamp01(Value);
        OnProgressChanged?.Invoke(Value, isFishInside);

        // Kiểm tra điều kiện Thắng / Thua
        if (Value >= 1f)
        {
            IsFinished = true;
            OnCompleted?.Invoke(true, IsPerfect);
        }
        else if (Value <= 0f)
        {
            IsFinished = true;
            OnCompleted?.Invoke(false, false);
        }
    }
}
