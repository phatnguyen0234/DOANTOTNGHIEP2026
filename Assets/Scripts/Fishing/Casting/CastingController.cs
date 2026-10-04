using System;
using UnityEngine;

// Điều khiển logic tích lực (Charging) và ném phao (Casting)
public class CastingController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới WaterDetector.")]
    [SerializeField] private WaterDetector waterDetector;

    [Tooltip("Tham chiếu tới FishingBobber prefab hoặc instance trong scene.")]
    [SerializeField] private FishingBobber bobber;

    [Tooltip("Transform điểm đầu cần câu (nơi dây câu xuất phát).")]
    [SerializeField] private Transform rodTip;

    [Header("Default Fallback Configs")]
    [SerializeField] private float defaultMinDistance = 1.5f;
    [SerializeField] private float defaultMaxDistance = 6.0f;
    [SerializeField] private float defaultChargeSpeed = 1.2f;

    public float CurrentPower { get; private set; } = 0f;
    public bool IsCharging { get; private set; } = false;

    // Sự kiện phát ra khi lực sạc thay đổi (giá trị 0 -> 1)
    public event Action<float> OnPowerChanged;
    // Sự kiện phát ra khi hoàn thành ném phao: (thành công ở nước không?, fishingSpotData tìm thấy)
    public event Action<bool, FishingSpotData> OnCastCompleted;

    private FishingRodData currentRodData;
    private bool chargeIncreasing = true;

    private void Awake()
    {
        if (waterDetector == null) waterDetector = GetComponent<WaterDetector>();
        if (bobber != null) bobber.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (IsCharging)
        {
            UpdateCharging();
        }
    }

    private Vector2? targetWaterPoint = null;

    public void StartCharging(FishingRodData rodData, Vector2? targetPoint = null)
    {
        currentRodData = rodData;
        targetWaterPoint = targetPoint;
        CurrentPower = 0f;
        IsCharging = true;
        chargeIncreasing = true;
        OnPowerChanged?.Invoke(CurrentPower);
    }

    private void UpdateCharging()
    {
        float speed = currentRodData != null ? currentRodData.ChargeSpeed : defaultChargeSpeed;

        if (chargeIncreasing)
        {
            CurrentPower += speed * Time.deltaTime;
            if (CurrentPower >= 1f)
            {
                CurrentPower = 1f;
                chargeIncreasing = false;
            }
        }
        else
        {
            CurrentPower -= speed * Time.deltaTime;
            if (CurrentPower <= 0f)
            {
                CurrentPower = 0f;
                chargeIncreasing = true;
            }
        }

        OnPowerChanged?.Invoke(CurrentPower);
    }

    // Kết thúc tích lực và quăng phao
    public void ReleaseCast(Vector2 playerPosition, Vector2 facingDirection)
    {
        if (!IsCharging) return;
        IsCharging = false;

        float minDist = currentRodData != null ? currentRodData.MinCastDistance : defaultMinDistance;
        float maxDist = currentRodData != null ? currentRodData.MaxCastDistance : defaultMaxDistance;

        Vector2 targetPosition;
        if (targetWaterPoint.HasValue)
        {
            // Ném tới đúng vị trí nước mục tiêu mà người chơi đã click
            targetPosition = targetWaterPoint.Value;
        }
        else
        {
            float castDistance = Mathf.Lerp(minDist, maxDist, CurrentPower);
            targetPosition = playerPosition + facingDirection.normalized * castDistance;
        }

        Vector2 startPos = rodTip != null ? (Vector2)rodTip.position : playerPosition;

        if (bobber != null)
        {
            bobber.Launch(rodTip, startPos, targetPosition, () =>
            {
                HandleBobberLanded(targetPosition);
            });
        }
        else
        {
            HandleBobberLanded(targetPosition);
        }
    }

    // Ném thẳng phao tới vị trí nước mục tiêu được click
    public void CastDirectlyTo(Vector2 playerPosition, Vector2 targetWaterPosition, FishingRodData rodData = null)
    {
        currentRodData = rodData;
        IsCharging = false;
        CurrentPower = 1f;

        Vector2 startPos = rodTip != null ? (Vector2)rodTip.position : playerPosition;

        if (bobber != null)
        {
            bobber.Launch(rodTip, startPos, targetWaterPosition, () =>
            {
                HandleBobberLanded(targetWaterPosition);
            });
        }
        else
        {
            HandleBobberLanded(targetWaterPosition);
        }
    }

    private void HandleBobberLanded(Vector2 targetPosition)
    {
        bool inWater = false;
        FishingSpotData spotData = null;

        if (waterDetector != null)
        {
            inWater = waterDetector.IsInWater(targetPosition, out spotData);
        }
        else
        {
            // Mặc định cho phép nếu chưa cấu hình detector
            inWater = true;
        }

        OnCastCompleted?.Invoke(inWater, spotData);
    }

    public void CancelCharging()
    {
        IsCharging = false;
        CurrentPower = 0f;
        OnPowerChanged?.Invoke(0f);
    }

    public void RetrieveBobber()
    {
        if (bobber != null)
        {
            bobber.Retrieve();
        }
    }

    public FishingBobber GetBobber() => bobber;
}
