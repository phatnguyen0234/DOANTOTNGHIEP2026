using UnityEngine;

// Điều khiển chuyển động thanh đỡ màu xanh (Player Bar) di chuyển trực tiếp theo chuột (loại bỏ lực/trọng lực)
public class FishingBarPhysics : MonoBehaviour
{
    [Header("Mouse Follow Settings")]
    [Tooltip("Tốc độ bám theo chuột (giá trị càng cao bám càng nhạy).")]
    [SerializeField] private float followSpeed = 35f;

    [Tooltip("Có làm mượt chuyển động chuột không (false = bám dính tức thì theo chuột).")]
    [SerializeField] private bool smoothMovement = true;

    [Header("Bar Geometry (Normalized [0, 1])")]
    [Tooltip("Kích thước chiều cao chuẩn của thanh đỡ (theo tỉ lệ normalized).")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float baseBarHeight = 0.2f;

    public float Position { get; private set; } = 0.5f; // Vị trí tâm của thanh [0, 1]
    public float Velocity { get; private set; } = 0f;
    public float BarHeight { get; private set; } = 0.2f;

    public void Initialize(float barSizeMultiplier = 1.0f)
    {
        BarHeight = Mathf.Clamp(baseBarHeight * barSizeMultiplier, 0.08f, 0.6f);
        Position = 0.5f;
        Velocity = 0f;
    }

    // Cập nhật vị trí thanh đỡ theo vị trí chuột [0..1]
    public void UpdateMousePosition(float targetNormalizedY, float deltaTime)
    {
        float halfHeight = BarHeight / 2f;
        float minPos = halfHeight;
        float maxPos = 1f - halfHeight;

        float targetPos = Mathf.Clamp(targetNormalizedY, minPos, maxPos);

        if (smoothMovement && followSpeed > 0f)
        {
            float previousPos = Position;
            Position = Mathf.Lerp(Position, targetPos, 1f - Mathf.Exp(-followSpeed * deltaTime));
            Velocity = (Position - previousPos) / Mathf.Max(deltaTime, 0.001f);
        }
        else
        {
            Position = targetPos;
            Velocity = 0f;
        }

        Position = Mathf.Clamp(Position, minPos, maxPos);
    }

    // Tương thích ngược: UpdatePhysics sẽ gọi UpdateMousePosition nếu không truyền vị trí
    public void UpdatePhysics(bool isHolding, float deltaTime)
    {
        // Không dùng lực vật lý
    }

    // Kiểm tra xem vị trí cá (fishPos [0, 1]) có nằm trong vùng bao phủ của thanh không
    public bool IsFishInside(float fishPosition)
    {
        float halfHeight = BarHeight / 2f;
        return (fishPosition >= Position - halfHeight) && (fishPosition <= Position + halfHeight);
    }
}
