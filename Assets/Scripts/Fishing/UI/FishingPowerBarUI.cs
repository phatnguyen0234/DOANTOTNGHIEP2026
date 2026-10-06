using UnityEngine;
using UnityEngine.UI;

// Hiển thị thanh tích lực khi người chơi giữ nút để quăng cần
public class FishingPowerBarUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Thanh trượt hoặc hình ảnh thể hiện độ dài lực tích.")]
    [SerializeField] private Image fillImage;

    [Tooltip("Gradient màu đổi từ xanh sang đỏ / vàng khi tích lực.")]
    [SerializeField] private Gradient powerGradient;

    [Tooltip("Khoảng dịch chuyển vị trí so với Player (World Space UI).")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    private Transform targetFollow;
    private Canvas parentCanvas;
    private Camera mainCam;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        mainCam = Camera.main;

        if (powerGradient == null || powerGradient.colorKeys == null || powerGradient.colorKeys.Length == 0)
        {
            powerGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.85f, 0.3f), 0.0f); // Xanh lá
            colorKeys[1] = new GradientColorKey(new Color(1.0f, 0.85f, 0.1f), 0.5f); // Vàng
            colorKeys[2] = new GradientColorKey(new Color(1.0f, 0.25f, 0.2f), 1.0f); // Đỏ cam
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            powerGradient.SetKeys(colorKeys, alphaKeys);
        }

        Hide();
    }

    private void Update()
    {
        if (targetFollow != null && gameObject.activeSelf)
        {
            Vector3 worldPos = targetFollow.position + offset;
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    transform.position = mainCam.WorldToScreenPoint(worldPos);
                }
            }
            else
            {
                transform.position = worldPos;
            }
        }
    }

    public void Show(Transform followTransform)
    {
        targetFollow = followTransform;
        gameObject.SetActive(true);
        SetPower(0f);
    }

    public void SetPower(float powerNormalized)
    {
        powerNormalized = Mathf.Clamp01(powerNormalized);

        if (fillImage != null)
        {
            fillImage.fillAmount = powerNormalized;
            if (powerGradient != null)
            {
                fillImage.color = powerGradient.Evaluate(powerNormalized);
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        targetFollow = null;
    }
}
