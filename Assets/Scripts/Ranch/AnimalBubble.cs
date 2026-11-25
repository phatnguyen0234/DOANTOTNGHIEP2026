using UnityEngine;

// Bong bóng trên đầu con vật: trái tim thoáng qua khi được vuốt ve / cho ăn; biểu tượng sản phẩm khi đã sẵn sàng thu;
// biểu tượng thức ăn trên nền đỏ khi con vật đang đói.
public class AnimalBubble : MonoBehaviour
{
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private SpriteRenderer icon;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.06f;

    private Sprite persistentIcon;
    private Color persistentTint = Color.white;
    private float flashUntil;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        Refresh();
    }

    // Đặt cao bong bóng theo chiều cao sprite con vật.
    public void SetHeight(float height)
    {
        baseLocalPosition = new Vector3(0f, height + 0.1f, 0f);
        transform.localPosition = baseLocalPosition;
    }

    public void SetPersistent(Sprite sprite)
    {
        SetPersistent(sprite, Color.white);
    }

    // `tint` tô màu nền bong bóng (đỏ = đói).
    public void SetPersistent(Sprite sprite, Color tint)
    {
        persistentIcon = sprite;
        persistentTint = tint;
        Refresh();
    }

    public void FlashHeart(float seconds = 1.2f)
    {
        flashUntil = Time.time + seconds;
        Refresh();
    }

    private void Update()
    {
        if (flashUntil > 0f && Time.time >= flashUntil)
        {
            flashUntil = 0f;
            Refresh();
        }
        if (background != null && background.enabled)
        {
            transform.localPosition = baseLocalPosition + new Vector3(0f, Mathf.Sin(Time.time * 3f) * bobAmplitude, 0f);
        }
    }

    private void Refresh()
    {
        if (background == null || icon == null) return;

        bool flashing = flashUntil > 0f;
        Sprite shown = flashing ? RanchFeedback.Heart : persistentIcon;
        bool visible = shown != null;

        background.enabled = visible && !flashing;
        icon.enabled = visible;
        icon.sprite = shown;
        background.sprite = RanchFeedback.Bubble;
        background.color = persistentTint;

        // Icon vật phẩm 16 px thu nhỏ cho vừa bong bóng; trái tim giữ nguyên cỡ.
        float scale = flashing ? 1f : 0.55f;
        icon.transform.localScale = new Vector3(scale, scale, 1f);
        icon.transform.localPosition = flashing ? Vector3.zero : new Vector3(0f, 0.38f, 0f);
    }
}
