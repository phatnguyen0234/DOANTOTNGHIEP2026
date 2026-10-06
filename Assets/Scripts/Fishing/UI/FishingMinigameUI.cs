using UnityEngine;
using UnityEngine.UI;

// Điều khiển giao diện hiển thị đồ họa của Minigame câu cá (Fish Icon, Player Bar, Progress Bar)
public class FishingMinigameUI : MonoBehaviour
{
    [Header("UI Rect References")]
    [Tooltip("Khung chứa đường bơi của cá (chiều cao chuẩn).")]
    [SerializeField] private RectTransform trackArea;

    [Tooltip("RectTransform của thanh đỡ màu xanh (Player Bar).")]
    [SerializeField] private RectTransform playerBarRect;

    [Tooltip("Image hiển thị thanh đỡ màu xanh (đổi màu khi cá vào/ra).")]
    [SerializeField] private Image playerBarImage;

    [Tooltip("RectTransform của Icon con cá.")]
    [SerializeField] private RectTransform fishIconRect;

    [Tooltip("Image của Icon con cá.")]
    [SerializeField] private Image fishIconImage;

    [Tooltip("Slider hoặc Image hiển thị thanh tiến độ câu (Fill Amount).")]
    [SerializeField] private Image progressFillImage;

    [Header("Visual Feedback Colors")]
    [SerializeField] private Color barCatchColor = new Color(0.2f, 0.85f, 0.3f, 0.85f);
    [SerializeField] private Color barMissColor = new Color(0.9f, 0.3f, 0.2f, 0.65f);

    private float trackHeight = 200f;

    private void Awake()
    {
        if (trackArea != null)
        {
            trackHeight = trackArea.rect.height;
        }
    }

    public void Setup(FishData fishData, float barNormalizedHeight)
    {
        if (trackArea != null)
        {
            trackHeight = trackArea.rect.height;
        }

        // Cập nhật kích thước chiều cao thanh xanh dựa theo barNormalizedHeight
        if (playerBarRect != null)
        {
            float targetHeight = trackHeight * barNormalizedHeight;
            playerBarRect.sizeDelta = new Vector2(playerBarRect.sizeDelta.x, targetHeight);
        }

        // Cập nhật Sprite icon của loài cá
        if (fishIconImage != null && fishData != null && fishData.Icon != null)
        {
            fishIconImage.sprite = fishData.Icon;
        }
    }

    public void UpdateDisplay(float barPos, float barHeight, float fishPos, float progressVal, bool isFishInside)
    {
        if (trackArea == null) return;

        trackHeight = trackArea.rect.height;

        // 1. Cập nhật vị trí thanh xanh (anchor ở dưới cùng hoặc center)
        if (playerBarRect != null)
        {
            float barY = (barPos - 0.5f) * trackHeight;
            playerBarRect.anchoredPosition = new Vector2(playerBarRect.anchoredPosition.x, barY);

            if (playerBarImage != null)
            {
                playerBarImage.color = isFishInside ? barCatchColor : barMissColor;
            }
        }

        // 2. Cập nhật vị trí icon con cá
        if (fishIconRect != null)
        {
            float fishY = (fishPos - 0.5f) * trackHeight;
            fishIconRect.anchoredPosition = new Vector2(fishIconRect.anchoredPosition.x, fishY);
        }

        // 3. Cập nhật thanh tiến độ bắt cá
        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = progressVal;
        }
    }

    // Chuyển đổi vị trí con trỏ chuột sang tỉ lệ normalized Y [0, 1] trên khung track
    public bool TryGetMouseNormalizedY(out float normalizedY)
    {
        normalizedY = 0.5f;
        if (trackArea == null) return false;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Camera eventCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(trackArea, Input.mousePosition, eventCam, out Vector2 localPoint))
        {
            float height = trackArea.rect.height;
            if (height > 0.001f)
            {
                normalizedY = Mathf.Clamp01((localPoint.y / height) + 0.5f);
                return true;
            }
        }
        return false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
