using UnityEngine;

// Xử lý chuyển động vật lý mượt mà của thanh đỡ màu xanh (Player Reel Bar)
public class FishingBarPhysics : MonoBehaviour
{
    [Header("Physics Parameters")]
    [Tooltip("Gia tốc đẩy thanh đi lên khi người chơi giữ nút.")]
    [SerializeField] private float acceleration = 6.0f;

    [Tooltip("Trọng lực kéo thanh rơi xuống khi thả nút.")]
    [SerializeField] private float gravity = 4.5f;

    [Tooltip("Tốc độ bay lên tối đa.")]
    [SerializeField] private float maxRiseSpeed = 3.0f;

    [Tooltip("Tốc độ rơi xuống tối đa.")]
    [SerializeField] private float maxFallSpeed = 3.5f;

    [Tooltip("Hệ số nảy khi thanh chạm đáy (Bounce Damping).")]
    [Range(0f, 0.6f)]
    [SerializeField] private float bounceDamping = 0.25f;

    [Header("Bar Geometry (Normalized [0, 1])")]
    [Tooltip("Kích thước chiều cao chuẩn của thanh đỡ (theo tỉ lệ normalized).")]
    [Range(0.1f, 0.5f)]
    [SerializeField] private float baseBarHeight = 0.2f;

    public float Position { get; private set; } = 0.1f; // Vị trí tâm của thanh [0, 1]
    public float Velocity { get; private set; } = 0f;
    public float BarHeight { get; private set; } = 0.2f;

    public void Initialize(float barSizeMultiplier = 1.0f)
    {
        BarHeight = Mathf.Clamp(baseBarHeight * barSizeMultiplier, 0.08f, 0.6f);
        Position = BarHeight / 2f;
        Velocity = 0f;
    }

    public void UpdatePhysics(bool isHolding, float deltaTime)
    {
        // 1. Cập nhật vận tốc dựa theo thao tác người chơi
        if (isHolding)
        {
            Velocity += acceleration * deltaTime;
        }
        else
        {
            Velocity -= gravity * deltaTime;
        }

        // 2. Giới hạn vận tốc
        Velocity = Mathf.Clamp(Velocity, -maxFallSpeed, maxRiseSpeed);

        // 3. Cập nhật vị trí
        Position += Velocity * deltaTime;

        // 4. Xử lý va chạm biên đáy và biên đỉnh (Clamping & Bouncing)
        float halfHeight = BarHeight / 2f;
        float minPos = halfHeight;
        float maxPos = 1f - halfHeight;

        if (Position <= minPos)
        {
            Position = minPos;
            if (Velocity < 0)
            {
                // Nảy ngược nhẹ từ đáy lên
                Velocity = -Velocity * bounceDamping;
            }
        }
        else if (Position >= maxPos)
        {
            Position = maxPos;
            if (Velocity > 0)
            {
                // Nảy ngược nhẹ từ đỉnh xuống
                Velocity = -Velocity * bounceDamping;
            }
        }
    }

    // Kiểm tra xem vị trí cá (fishPos [0, 1]) có nằm trong vùng bao phủ của thanh không
    public bool IsFishInside(float fishPosition)
    {
        float halfHeight = BarHeight / 2f;
        return (fishPosition >= Position - halfHeight) && (fishPosition <= Position + halfHeight);
    }
}
