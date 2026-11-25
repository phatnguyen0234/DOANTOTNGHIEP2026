using UnityEngine;

// Lặp một dãy sprite (dùng cho người bán gia súc đứng chờ ở trang trại).
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameLoop : MonoBehaviour
{
    [SerializeField] private Sprite[] frames = new Sprite[0];
    [SerializeField, Min(0.1f)] private float framesPerSecond = 5f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        spriteRenderer.sprite = frames[Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length];
    }
}
