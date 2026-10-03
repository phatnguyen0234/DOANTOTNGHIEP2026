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

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (targetFollow != null && gameObject.activeSelf)
        {
            transform.position = targetFollow.position + offset;
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
