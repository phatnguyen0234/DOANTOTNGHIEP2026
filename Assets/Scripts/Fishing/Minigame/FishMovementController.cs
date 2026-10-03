using UnityEngine;

// Điều khiển AI bơi của cá trong Minigame với nhiều dạng hành vi (Movement Patterns)
public class FishMovementController : MonoBehaviour
{
    public float Position { get; private set; } = 0.5f; // [0, 1]
    public float Velocity { get; private set; } = 0f;

    private FishData fishData;
    private float targetPosition = 0.5f;
    private float nextMoveTimer = 0f;
    private float smoothVelocity = 0f;

    public void Initialize(FishData data)
    {
        fishData = data;
        Position = 0.3f;
        targetPosition = 0.5f;
        nextMoveTimer = 0f;
        smoothVelocity = 0f;
        Velocity = 0f;
    }

    public void UpdateMovement(float deltaTime)
    {
        if (fishData == null) return;

        nextMoveTimer -= deltaTime;
        if (nextMoveTimer <= 0f)
        {
            DecideNextTarget();
        }

        ExecuteMovement(deltaTime);
    }

    private void DecideNextTarget()
    {
        FishMovementType moveType = fishData.MovementType;

        switch (moveType)
        {
            case FishMovementType.Floater:
                // Thường ưu tiên bơi ở nửa trên [0.4 .. 0.95]
                targetPosition = Random.Range(0.4f, 0.95f);
                break;

            case FishMovementType.Sinker:
                // Thường ưu tiên bơi ở nửa dưới [0.05 .. 0.6]
                targetPosition = Random.Range(0.05f, 0.6f);
                break;

            case FishMovementType.Dart:
                // Nhảy bất ngờ giữa các vị trí cách xa nhau
                float currentPos = Position;
                if (currentPos > 0.5f)
                {
                    targetPosition = Random.Range(0.1f, 0.4f);
                }
                else
                {
                    targetPosition = Random.Range(0.6f, 0.9f);
                }
                break;

            case FishMovementType.Mixed:
                // Ngẫu nhiên bất kỳ vị trí nào
                targetPosition = Random.Range(0.05f, 0.95f);
                break;

            case FishMovementType.Smooth:
            default:
                // Bơi đều đặn xung quanh vị trí hiện tại
                float offset = Random.Range(-0.4f, 0.4f);
                targetPosition = Mathf.Clamp(Position + offset, 0.1f, 0.9f);
                break;
        }

        Vector2 interval = fishData.ChangeTargetInterval;
        float baseInterval = Random.Range(interval.x, interval.y);

        // Với loại cá Dart, khoảng đổi hướng nhanh hơn
        if (moveType == FishMovementType.Dart)
        {
            baseInterval *= 0.6f;
        }

        nextMoveTimer = baseInterval;
    }

    private void ExecuteMovement(float deltaTime)
    {
        float speed = fishData.MovementSpeed;
        float smoothTime = Mathf.Max(0.05f, (1.2f / speed));

        if (fishData.MovementType == FishMovementType.Dart)
        {
            smoothTime *= 0.4f; // Giật nhanh hơn
        }

        float previousPos = Position;
        Position = Mathf.SmoothDamp(Position, targetPosition, ref smoothVelocity, smoothTime, 5f, deltaTime);
        Position = Mathf.Clamp01(Position);

        Velocity = (Position - previousPos) / Mathf.Max(deltaTime, 0.001f);
    }
}
