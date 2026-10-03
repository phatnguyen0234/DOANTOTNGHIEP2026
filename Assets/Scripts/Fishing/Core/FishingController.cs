using System;
using System.Collections;
using UnityEngine;

// Nhạc trưởng điều phối toàn bộ hệ thống câu cá (Fishing System Coordinator)
public class FishingController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới PlayerMovement.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Tham chiếu tới CastingController.")]
    [SerializeField] private CastingController castingController;

    [Tooltip("Tham chiếu tới FishSpawner.")]
    [SerializeField] private FishSpawner fishSpawner;

    [Tooltip("Tham chiếu tới FishingMinigame.")]
    [SerializeField] private FishingMinigame minigame;

    [Tooltip("Tham chiếu tới Inventory của người chơi.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới ItemDropSpawner (tùy chọn).")]
    [SerializeField] private ItemDropSpawner itemDropSpawner;

    [Tooltip("Tham chiếu tới WaterDetector.")]
    [SerializeField] private WaterDetector waterDetector;

    [Header("UI Dependencies")]
    [SerializeField] private FishingPowerBarUI powerBarUI;
    [SerializeField] private FishBiteIndicatorUI biteIndicatorUI;
    [SerializeField] private FishingResultUI resultUI;

    [Header("Default Rod Config")]
    [SerializeField] private FishingRodData defaultRodData;

    public FishingStateMachine StateMachine { get; private set; }
    public FishingState CurrentState => StateMachine != null ? StateMachine.CurrentState : FishingState.Idle;
    public bool IsFishing => CurrentState != FishingState.Idle;

    // Sự kiện toàn cục cho các hệ thống khác lắng nghe
    public event Action<FishingState> OnFishingStateChanged;
    public event Action<FishingResult> OnFishingFinished;

    private FishingRodData activeRodData;
    private FishingSpotData currentSpotData;
    private FishData targetFish;
    private float biteTimer;
    private float reactionTimer;

    public static FishingController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        StateMachine = new FishingStateMachine();
        StateMachine.OnStateChanged += HandleStateChanged;

        ResolveDependencies();
    }

    private void Start()
    {
        BindEvents();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    private void ResolveDependencies()
    {
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (castingController == null) castingController = GetComponentInChildren<CastingController>();
        if (waterDetector == null) waterDetector = GetComponentInChildren<WaterDetector>();
        if (waterDetector == null) waterDetector = FindAnyObjectByType<WaterDetector>();
        if (fishSpawner == null) fishSpawner = GetComponentInChildren<FishSpawner>();
        if (minigame == null) minigame = GetComponentInChildren<FishingMinigame>();
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
        if (itemDropSpawner == null) itemDropSpawner = FindAnyObjectByType<ItemDropSpawner>();

        if (powerBarUI == null) powerBarUI = FindAnyObjectByType<FishingPowerBarUI>(FindObjectsInactive.Include);
        if (biteIndicatorUI == null) biteIndicatorUI = FindAnyObjectByType<FishBiteIndicatorUI>(FindObjectsInactive.Include);
        if (resultUI == null) resultUI = FindAnyObjectByType<FishingResultUI>(FindObjectsInactive.Include);
    }

    private void BindEvents()
    {
        if (castingController != null)
        {
            castingController.OnPowerChanged += HandlePowerChanged;
            castingController.OnCastCompleted += HandleCastCompleted;
        }

        if (minigame != null)
        {
            minigame.OnMinigameFinished += HandleMinigameFinished;
        }

        if (resultUI != null)
        {
            resultUI.OnClosed += HandleResultUIClosed;
        }
    }

    private void UnbindEvents()
    {
        if (castingController != null)
        {
            castingController.OnPowerChanged -= HandlePowerChanged;
            castingController.OnCastCompleted -= HandleCastCompleted;
        }

        if (minigame != null)
        {
            minigame.OnMinigameFinished -= HandleMinigameFinished;
        }

        if (resultUI != null)
        {
            resultUI.OnClosed -= HandleResultUIClosed;
        }
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case FishingState.WaitingForBite:
                UpdateWaitingForBite();
                break;

            case FishingState.FishBite:
                UpdateFishBiteReaction();
                break;
        }
    }

    #region Input Actions & Water Click Flow

    // Kiểm tra xem vị trí có phải là mặt nước hợp lệ để câu không
    public bool IsWaterAtPosition(Vector2 worldPosition, out FishingSpotData spotData)
    {
        spotData = null;
        if (waterDetector != null)
        {
            return waterDetector.IsInWater(worldPosition, out spotData);
        }
        return true; // Fallback nếu chưa cấu hình detector
    }

    // [Click lần 1]: Người chơi chọn cần câu và click vào mặt nước -> Bắt đầu hiện thanh lực trên đầu
    public bool TryStartFishingAtWater(Vector2 targetWaterPos, FishingRodData rodData = null)
    {
        if (CurrentState != FishingState.Idle) return false;

        if (!IsWaterAtPosition(targetWaterPos, out currentSpotData))
        {
            return false;
        }

        activeRodData = rodData != null ? rodData : defaultRodData;

        // Xoay hướng nhân vật về điểm click nước
        if (playerMovement != null)
        {
            Vector2 playerPos = playerMovement.transform.position;
            Vector2 dir = (targetWaterPos - playerPos).normalized;
            playerMovement.SetFacingDirection(dir);
        }

        StateMachine.ChangeState(FishingState.Charging);

        if (castingController != null)
        {
            castingController.StartCharging(activeRodData, targetWaterPos);
        }

        if (powerBarUI != null && playerMovement != null)
        {
            powerBarUI.Show(playerMovement.transform);
        }

        return true;
    }

    // Bắt đầu nạp lực câu cá chuẩn
    public bool StartChargingCast(FishingRodData rodData = null)
    {
        if (CurrentState != FishingState.Idle) return false;

        activeRodData = rodData != null ? rodData : defaultRodData;

        StateMachine.ChangeState(FishingState.Charging);

        if (castingController != null)
        {
            castingController.StartCharging(activeRodData);
        }

        if (powerBarUI != null && playerMovement != null)
        {
            powerBarUI.Show(playerMovement.transform);
        }

        return true;
    }

    // [Click lần 2]: Người chơi click lần nữa để chốt lực -> Quăng cần và ẩn thanh lực đi
    public void ReleaseCast()
    {
        if (CurrentState != FishingState.Charging) return;

        if (powerBarUI != null)
        {
            powerBarUI.Hide();
        }

        StateMachine.ChangeState(FishingState.Casting);

        Vector2 playerPos = playerMovement != null ? (Vector2)playerMovement.transform.position : (Vector2)transform.position;
        Vector2 facingDir = playerMovement != null ? playerMovement.FacingDirection : Vector2.down;

        if (castingController != null)
        {
            castingController.ReleaseCast(playerPos, facingDir);
        }
    }

    // Người chơi bấm nút tương tác trong lúc đang thả phao / cá cắn câu
    public void OnInteractAction()
    {
        if (CurrentState == FishingState.FishBite)
        {
            HookFish();
        }
        else if (CurrentState == FishingState.WaitingForBite)
        {
            CancelFishing("Thu cần sớm.");
        }
    }

    #endregion

    #region State Transition Handlers

    private void HandleStateChanged(FishingState prevState, FishingState newState)
    {
        OnFishingStateChanged?.Invoke(newState);

        switch (newState)
        {
            case FishingState.Idle:
                if (castingController != null) castingController.RetrieveBobber();
                if (powerBarUI != null) powerBarUI.Hide();
                if (biteIndicatorUI != null) biteIndicatorUI.Hide();
                break;

            case FishingState.Failed:
                StartCoroutine(DelayedResetToIdle(1.5f));
                break;

            case FishingState.Success:
                StartCoroutine(DelayedResetToIdle(2.0f));
                break;
        }
    }

    private void HandlePowerChanged(float power)
    {
        if (powerBarUI != null && CurrentState == FishingState.Charging)
        {
            powerBarUI.SetPower(power);
        }
    }

    private void HandleCastCompleted(bool inWater, FishingSpotData spotData)
    {
        if (!inWater)
        {
            Debug.Log("[FishingController] Phao rơi ngoài vùng nước!");
            StateMachine.ChangeState(FishingState.Failed);
            if (castingController != null) castingController.RetrieveBobber();
            return;
        }

        currentSpotData = spotData;

        // Chọn loài cá cho lượt câu này
        if (fishSpawner != null)
        {
            targetFish = fishSpawner.SpawnFish(currentSpotData, activeRodData);
        }

        // Chuyển sang trạng thái câu cá và chờ đúng 1 giây để lên cá!
        StateMachine.ChangeState(FishingState.Reeling);
        StartCoroutine(CatchFishAfterDelayRoutine(1.0f));
    }

    // Luồng câu cá: Đợi 1 giây rồi giật lên cá
    private IEnumerator CatchFishAfterDelayRoutine(float delay)
    {
        Debug.Log("<color=#00D2FF>[FishingController] Đang quăng cần vào mặt nước... Đợi 1s để kéo cá lên!</color>");
        yield return new WaitForSeconds(delay);

        if (targetFish == null && fishSpawner != null)
        {
            targetFish = fishSpawner.SpawnFish(currentSpotData, activeRodData);
        }

        FishingResult result = new FishingResult(
            targetFish,
            true,
            true,
            targetFish != null ? targetFish.ExpReward : 25,
            targetFish != null ? targetFish.RewardItemData : null,
            1
        );

        HandleMinigameFinished(result);
    }

    private void UpdateWaitingForBite()
    {
        biteTimer -= Time.deltaTime;
        if (biteTimer <= 0f)
        {
            TriggerFishBite();
        }
    }

    private void TriggerFishBite()
    {
        StateMachine.ChangeState(FishingState.FishBite);

        reactionTimer = activeRodData != null ? activeRodData.HookReactionWindow : 1.2f;

        FishingBobber bobber = castingController != null ? castingController.GetBobber() : null;
        if (bobber != null)
        {
            bobber.PlayBiteNibbleAnimation();
            if (biteIndicatorUI != null)
            {
                biteIndicatorUI.Show(bobber.transform.position);
            }
        }
        else if (playerMovement != null && biteIndicatorUI != null)
        {
            biteIndicatorUI.Show(playerMovement.transform.position);
        }
    }

    private void UpdateFishBiteReaction()
    {
        reactionTimer -= Time.deltaTime;
        if (reactionTimer <= 0f)
        {
            if (biteIndicatorUI != null) biteIndicatorUI.Hide();
            StateMachine.ChangeState(FishingState.Failed);

            if (resultUI != null)
            {
                resultUI.ShowResult(FishingResult.CreateFailed());
            }
        }
    }

    private void HookFish()
    {
        if (biteIndicatorUI != null) biteIndicatorUI.Hide();
        StateMachine.ChangeState(FishingState.Hooked);
        StartCoroutine(CatchFishAfterDelayRoutine(1.0f));
    }

    // Phương thức kiểm thử nhanh
    public void TriggerQuickFishingTest()
    {
        if (CurrentState != FishingState.Idle) return;
        StateMachine.ChangeState(FishingState.Casting);
        StartCoroutine(CatchFishAfterDelayRoutine(1.0f));
    }

    private void HandleMinigameFinished(FishingResult result)
    {
        if (result.IsSuccess)
        {
            StateMachine.ChangeState(FishingState.Success);
            AwardRewards(result);
        }
        else
        {
            StateMachine.ChangeState(FishingState.Failed);
        }

        if (resultUI != null)
        {
            resultUI.ShowResult(result);
        }

        OnFishingFinished?.Invoke(result);
    }

    private void AwardRewards(FishingResult result)
    {
        if (result == null || !result.IsSuccess) return;

        // 1. Thêm vật phẩm vào Inventory
        if (result.RewardItem != null && inventory != null)
        {
            bool added = inventory.TryAddItem(result.RewardItem, result.Quantity);
            if (!added && itemDropSpawner != null && playerMovement != null)
            {
                // Nếu túi đầy, drop cá ra đất cạnh player
                itemDropSpawner.SpawnDrop(result.RewardItem, result.Quantity, playerMovement.transform.position);
            }
        }

        Debug.Log($"[FishingController] Câu thành công {result.CaughtFish?.FishName}! Nhận {result.EarnedExp} EXP. Perfect: {result.IsPerfect}");
    }

    private void HandleResultUIClosed()
    {
        if (CurrentState == FishingState.Success || CurrentState == FishingState.Failed)
        {
            StateMachine.ResetToIdle();
        }
    }

    private IEnumerator DelayedResetToIdle(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (CurrentState == FishingState.Success || CurrentState == FishingState.Failed)
        {
            StateMachine.ResetToIdle();
        }
    }

    public void CancelFishing(string reason = "")
    {
        if (castingController != null) castingController.CancelCharging();
        if (minigame != null) minigame.StopMinigame();
        if (powerBarUI != null) powerBarUI.Hide();
        if (biteIndicatorUI != null) biteIndicatorUI.Hide();

        StateMachine.ResetToIdle();
        Debug.Log($"[FishingController] Hủy câu cá: {reason}");
    }

    #endregion
}
