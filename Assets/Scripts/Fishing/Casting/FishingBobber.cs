using System;
using System.Collections;
using UnityEngine;

// Điều khiển phao câu: Quỹ đạo bay vòng cung parabol, dập dềnh trên nước, vẽ dây câu
public class FishingBobber : MonoBehaviour
{
    [Header("Visual & Line Renderer")]
    [Tooltip("SpriteRenderer hiển thị hình ảnh phao câu.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("LineRenderer vẽ dây câu nối từ đầu cần tới phao.")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Flight & Arc Settings")]
    [Tooltip("Độ cao đỉnh của quỹ đạo ném parabol.")]
    [SerializeField] private float arcHeight = 1.5f;

    [Tooltip("Thời gian phao bay từ tay tới mặt nước (giây).")]
    [SerializeField] private float flightDuration = 0.65f;

    [Header("Floating Bobbing Settings")]
    [Tooltip("Biên độ dập dềnh khi nổi trên nước.")]
    [SerializeField] private float bobbingAmplitude = 0.05f;

    [Tooltip("Tần số nhấp nhô theo sóng nước.")]
    [SerializeField] private float bobbingFrequency = 3f;

    [Header("Effects & Prefabs")]
    [Tooltip("Hiệu ứng bọt nước khi phao chạm nước (tùy chọn).")]
    [SerializeField] private GameObject splashVfxPrefab;

    [Tooltip("Hiệu ứng gợn sóng quanh phao (tùy chọn).")]
    [SerializeField] private GameObject rippleVfxPrefab;

    [Header("Spot Detection & Debug")]
    [Tooltip("Dữ liệu điểm câu hiện tại phao đang nằm trong.")]
    [SerializeField] private FishingSpotData currentSpotData;
    [Tooltip("Bán kính quét tìm FishingSpotZone quanh điểm phao rơi.")]
    [SerializeField] private float spotDetectionRadius = 0.5f;

    public FishingSpotData CurrentSpotData => currentSpotData;

    private Transform rodTipTransform;
    private Vector2 targetLandingPosition;
    private bool isFlying = false;
    private bool isFloating = false;
    private float floatTimer = 0f;
    private GameObject spawnedRippleInstance;

    public event Action OnLanded;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        UpdateFishingLine();

        if (isFloating)
        {
            UpdateFloatingBobbing();
        }
    }

    // Bắt đầu ném phao từ vị trí cần câu tới điểm đích
    public void Launch(Transform rodTip, Vector2 startPos, Vector2 targetPos, Action onLandedCallback = null)
    {
        rodTipTransform = rodTip;
        targetLandingPosition = targetPos;
        OnLanded = onLandedCallback;

        gameObject.SetActive(true);
        transform.position = startPos;
        isFlying = true;
        isFloating = false;
        floatTimer = 0f;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
        }

        StopAllCoroutines();
        StartCoroutine(FlightRoutine(startPos, targetPos));
    }

    private IEnumerator FlightRoutine(Vector2 startPos, Vector2 targetPos)
    {
        float elapsed = 0f;

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);

            // Nội suy vị trí tuyến tính
            Vector2 linearPos = Vector2.Lerp(startPos, targetPos, t);

            // Thêm độ cao parabol: 4 * h * t * (1 - t)
            float arcOffset = 4f * arcHeight * t * (1f - t);
            transform.position = new Vector3(linearPos.x, linearPos.y + arcOffset, 0f);

            yield return null;
        }

        transform.position = new Vector3(targetPos.x, targetPos.y, 0f);
        isFlying = false;
        isFloating = true;

        SpawnSplashEffect();
        SpawnRippleEffect();

        // Kiểm tra và debug FishingSpot tại vị trí phao đáp xuống
        DetectSpotAtPosition(targetPos);

        OnLanded?.Invoke();
    }

    private void UpdateFloatingBobbing()
    {
        floatTimer += Time.deltaTime * bobbingFrequency;
        float yOffset = Mathf.Sin(floatTimer) * bobbingAmplitude;
        transform.position = new Vector3(targetLandingPosition.x, targetLandingPosition.y + yOffset, 0f);
    }

    private void UpdateFishingLine()
    {
        if (lineRenderer == null || !lineRenderer.enabled) return;

        Vector3 startLinePos = rodTipTransform != null ? rodTipTransform.position : transform.position;
        lineRenderer.SetPosition(0, startLinePos);
        lineRenderer.SetPosition(1, transform.position);
    }

    // Hiệu ứng phao giật chìm khi cá cắn câu
    public void PlayBiteNibbleAnimation()
    {
        StopAllCoroutines();
        StartCoroutine(BiteNibbleRoutine());
    }

    private IEnumerator BiteNibbleRoutine()
    {
        Vector3 basePos = targetLandingPosition;
        for (int i = 0; i < 3; i++)
        {
            transform.position = basePos + new Vector3(0, -0.15f, 0);
            yield return new WaitForSeconds(0.08f);
            transform.position = basePos + new Vector3(0, 0.05f, 0);
            yield return new WaitForSeconds(0.08f);
        }
        transform.position = basePos;
        isFloating = true;
    }

    private void SpawnSplashEffect()
    {
        if (splashVfxPrefab != null)
        {
            GameObject splash = Instantiate(splashVfxPrefab, targetLandingPosition, Quaternion.identity);
            Destroy(splash, 2f);
        }
    }

    private void SpawnRippleEffect()
    {
        if (rippleVfxPrefab != null && spawnedRippleInstance == null)
        {
            spawnedRippleInstance = Instantiate(rippleVfxPrefab, targetLandingPosition, Quaternion.identity, transform);
        }
    }

    // Phát hiện và debug FishingSpot tại vị trí phao rơi
    public FishingSpotData DetectSpotAtPosition(Vector2 pos)
    {
        currentSpotData = null;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, spotDetectionRadius);
        foreach (var col in colliders)
        {
            FishingSpotZone zone = col.GetComponent<FishingSpotZone>() ?? col.GetComponentInParent<FishingSpotZone>();
            if (zone != null && zone.SpotData != null)
            {
                currentSpotData = zone.SpotData;
                break;
            }
        }

        if (currentSpotData != null)
        {
            Debug.Log($"<color=#00FF7F>[FishingBobber] Phao đã bắt được FishingSpot: <b>{currentSpotData.SpotName}</b> (Asset: {currentSpotData.name}) tại tọa độ {pos}</color>", this);
        }
        else
        {
            Debug.Log($"<color=#FFA500>[FishingBobber] Phao rơi tại tọa độ {pos} - Không bắt được FishingSpotZone nào (null / Vùng nước tự do)</color>", this);
        }

        return currentSpotData;
    }

    // Cập nhật spot từ nguồn bên ngoài (ví dụ CastingController / WaterDetector)
    public void SetSpotData(FishingSpotData spotData)
    {
        currentSpotData = spotData;
        if (currentSpotData != null)
        {
            Debug.Log($"<color=#00FF7F>[FishingBobber] Đồng bộ FishingSpot: <b>{currentSpotData.SpotName}</b> (Asset: {currentSpotData.name})</color>", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        FishingSpotZone zone = other.GetComponent<FishingSpotZone>() ?? other.GetComponentInParent<FishingSpotZone>();
        if (zone != null && zone.SpotData != null)
        {
            currentSpotData = zone.SpotData;
            Debug.Log($"<color=#00FF7F>[FishingBobber] OnTriggerEnter2D chạm FishingSpot: <b>{currentSpotData.SpotName}</b> (Asset: {currentSpotData.name})</color>", this);
        }
    }

    // Thu hồi phao và ẩn đi
    public void Retrieve()
    {
        StopAllCoroutines();
        isFlying = false;
        isFloating = false;
        currentSpotData = null;

        if (spawnedRippleInstance != null)
        {
            Destroy(spawnedRippleInstance);
            spawnedRippleInstance = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        gameObject.SetActive(false);
    }
}
