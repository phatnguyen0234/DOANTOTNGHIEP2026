using System;
using UnityEngine;

// Nhạc trưởng quản lý phiên Minigame câu cá
public class FishingMinigame : MonoBehaviour
{
    [Header("Sub-systems")]
    [Tooltip("Thành phần vật lý của thanh đỡ.")]
    [SerializeField] private FishingBarPhysics barPhysics;

    [Tooltip("Thành phần AI di chuyển của cá.")]
    [SerializeField] private FishMovementController fishMovement;

    [Tooltip("Thành phần tính toán thanh tiến độ.")]
    [SerializeField] private FishingProgress progress;

    [Tooltip("UI hiển thị Minigame.")]
    [SerializeField] private FishingMinigameUI minigameUI;

    public bool IsActive { get; private set; } = false;
    public FishData CurrentFish { get; private set; }

    // Sự kiện kết thúc Minigame: (kết quả FishingResult)
    public event Action<FishingResult> OnMinigameFinished;

    private FishingRodData currentRod;

    private void Awake()
    {
        if (barPhysics == null) barPhysics = GetComponentInChildren<FishingBarPhysics>();
        if (fishMovement == null) fishMovement = GetComponentInChildren<FishMovementController>();
        if (progress == null) progress = GetComponentInChildren<FishingProgress>();
        if (minigameUI == null) minigameUI = GetComponentInChildren<FishingMinigameUI>();

        if (progress != null)
        {
            progress.OnCompleted += HandleProgressCompleted;
        }

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (progress != null)
        {
            progress.OnCompleted -= HandleProgressCompleted;
        }
    }

    // Bắt đầu một ván Minigame với loài cá và cần câu chỉ định
    public void StartMinigame(FishData fish, FishingRodData rod)
    {
        CurrentFish = fish;
        currentRod = rod;
        IsActive = true;

        gameObject.SetActive(true);

        float barMultiplier = rod != null ? rod.BarSizeMultiplier : 1.0f;
        float gainMult = rod != null ? rod.ProgressGainMultiplier : 1.0f;
        float lossRed = rod != null ? rod.ProgressLossReduction : 1.0f;

        if (barPhysics != null) barPhysics.Initialize(barMultiplier);
        if (fishMovement != null) fishMovement.Initialize(fish);
        if (progress != null) progress.Initialize(gainMult, lossRed);

        if (minigameUI != null)
        {
            minigameUI.Show();
            minigameUI.Setup(fish, barPhysics.BarHeight);
        }
    }

    private void Update()
    {
        if (!IsActive) return;

        // Nhận input từ chuột trái hoặc phím Space/E/C
        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C);

        float dt = Time.deltaTime;

        // 1. Cập nhật vật lý thanh đỡ
        if (barPhysics != null)
        {
            barPhysics.UpdatePhysics(isHolding, dt);
        }

        // 2. Cập nhật AI của cá
        if (fishMovement != null)
        {
            fishMovement.UpdateMovement(dt);
        }

        // 3. Kiểm tra va chạm thanh & cá
        float fishPos = fishMovement != null ? fishMovement.Position : 0.5f;
        bool isFishInside = barPhysics != null && barPhysics.IsFishInside(fishPos);

        // 4. Cập nhật tiến độ
        if (progress != null)
        {
            progress.UpdateProgress(isFishInside, dt);
        }

        // 5. Cập nhật hiển thị UI
        if (minigameUI != null)
        {
            float barPos = barPhysics != null ? barPhysics.Position : 0.1f;
            float barHeight = barPhysics != null ? barPhysics.BarHeight : 0.2f;
            float progVal = progress != null ? progress.Value : 0f;

            minigameUI.UpdateDisplay(barPos, barHeight, fishPos, progVal, isFishInside);
        }
    }

    private void HandleProgressCompleted(bool isSuccess, bool isPerfect)
    {
        if (!IsActive) return;
        IsActive = false;

        if (minigameUI != null)
        {
            minigameUI.Hide();
        }

        gameObject.SetActive(false);

        FishingResult result;
        if (isSuccess && CurrentFish != null)
        {
            result = new FishingResult(
                CurrentFish,
                true,
                isPerfect,
                CurrentFish.ExpReward,
                CurrentFish.RewardItemData,
                1
            );
        }
        else
        {
            result = FishingResult.CreateFailed();
        }

        OnMinigameFinished?.Invoke(result);
    }

    public void StopMinigame()
    {
        IsActive = false;
        if (minigameUI != null) minigameUI.Hide();
        gameObject.SetActive(false);
    }
}
