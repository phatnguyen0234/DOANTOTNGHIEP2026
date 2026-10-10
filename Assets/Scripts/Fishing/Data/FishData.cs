using UnityEngine;

// ScriptableObject định nghĩa cấu hình và thuộc tính của một loài cá
[CreateAssetMenu(fileName = "NewFishData", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("Tên của loài cá.")]
    [SerializeField] private string fishName = "Cá Chép";

    [Tooltip("Mô tả hoặc lore của loài cá.")]
    [TextArea(2, 4)]
    [SerializeField] private string description = "Một loài cá nước ngọt phổ biến.";

    [Tooltip("Sprite icon của cá khi hiển thị trong Minigame và UI popup.")]
    [SerializeField] private Sprite icon;

    [Tooltip("ItemData tương ứng trong Inventory khi câu thành công loài cá này.")]
    [SerializeField] private ItemData rewardItemData;

    [Header("Minigame & AI Difficulty")]
    [Tooltip("Độ khó tổng thể từ 0 (cực dễ) đến 100 (cực khó/huyền thoại).")]
    [Range(0f, 100f)]
    [SerializeField] private float difficulty = 30f;

    [Tooltip("Kiểu di chuyển của cá trong Minigame.")]
    [SerializeField] private FishMovementType movementType = FishMovementType.Smooth;

    [Tooltip("Tốc độ bơi cơ bản của cá (đơn vị: normalized bar/giây).")]
    [Range(0.2f, 3f)]
    [SerializeField] private float movementSpeed = 0.8f;

    [Tooltip("Gia tốc đổi hướng của cá.")]
    [Range(0.5f, 5f)]
    [SerializeField] private float acceleration = 1.5f;

    [Tooltip("Khoảng thời gian ngẫu nhiên (giây) giữa các lần đổi mục tiêu di chuyển.")]
    [SerializeField] private Vector2 changeTargetInterval = new Vector2(1f, 2.5f);

    [Header("Spawning & Economy")]
    [Tooltip("Trọng số tỉ lệ xuất hiện (Weight càng cao càng dễ cắn câu).")]
    [Range(1, 100)]
    [SerializeField] private int spawnWeight = 50;

    [Tooltip("Điểm kinh nghiệm nhận được khi câu thành công.")]
    [SerializeField] private int expReward = 15;

    #region Public Properties (Encapsulation)

    public string FishName => fishName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemData RewardItemData => rewardItemData;
    public float Difficulty => difficulty;
    public FishMovementType MovementType => movementType;
    public float MovementSpeed => movementSpeed;
    public float Acceleration => acceleration;
    public Vector2 ChangeTargetInterval => changeTargetInterval;
    public int SpawnWeight => spawnWeight;
    public int ExpReward => expReward;

    #endregion
}
