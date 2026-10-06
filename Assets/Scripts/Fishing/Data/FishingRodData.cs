using UnityEngine;

// ScriptableObject định nghĩa thông số của một loại cần câu
[CreateAssetMenu(fileName = "NewFishingRodData", menuName = "Fishing/Rod Data")]
public class FishingRodData : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("Tên của loại cần câu.")]
    [SerializeField] private string rodName = "Cần Câu Tre";

    [Tooltip("Mã định danh của ItemData tương ứng.")]
    [SerializeField] private ItemData itemData;

    [Header("Casting Settings")]
    [Tooltip("Khoảng cách ném phao tối thiểu (đơn vị: mét trong Unity).")]
    [SerializeField] private float minCastDistance = 1.5f;

    [Tooltip("Khoảng cách ném phao tối đa khi tích đầy lực.")]
    [SerializeField] private float maxCastDistance = 6.0f;

    [Tooltip("Tốc độ tích lực (charge speed).")]
    [SerializeField] private float chargeSpeed = 1.0f;

    [Header("Bite & Reaction Settings")]
    [Tooltip("Thời gian chờ tối thiểu để cá cắn câu (giây).")]
    [SerializeField] private float minBiteWaitTime = 2.0f;

    [Tooltip("Thời gian chờ tối đa để cá cắn câu (giây).")]
    [SerializeField] private float maxBiteWaitTime = 6.0f;

    [Tooltip("Thời gian cho phép người chơi phản xạ giật cần khi có dấu ! (giây).")]
    [SerializeField] private float hookReactionWindow = 1.2f;

    [Header("Minigame Modifiers")]
    [Tooltip("Hệ số kích thước thanh đỡ cá (1.0 là kích thước chuẩn).")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float barSizeMultiplier = 1.0f;

    [Tooltip("Hệ số tốc độ tích điểm tiến độ (Progress Gain Multiplier).")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float progressGainMultiplier = 1.0f;

    [Tooltip("Hệ số làm chậm tốc độ tụt tiến độ khi cá ra khỏi thanh.")]
    [Range(0.5f, 2.0f)]
    [SerializeField] private float progressLossReduction = 1.0f;

    #region Public Properties

    public string RodName => rodName;
    public ItemData ItemData => itemData;
    public float MinCastDistance => minCastDistance;
    public float MaxCastDistance => maxCastDistance;
    public float ChargeSpeed => chargeSpeed;
    public float MinBiteWaitTime => minBiteWaitTime;
    public float MaxBiteWaitTime => maxBiteWaitTime;
    public float HookReactionWindow => hookReactionWindow;
    public float BarSizeMultiplier => barSizeMultiplier;
    public float ProgressGainMultiplier => progressGainMultiplier;
    public float ProgressLossReduction => progressLossReduction;

    #endregion
}
