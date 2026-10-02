using System.Collections;
using UnityEngine;

// Làm mờ tán cây khi Player đi vào phía sau cây (giống Stardew Valley).
// Cần một Collider2D dạng Trigger phủ phần tán cây trên cùng GameObject.
// Dùng Coroutine thay vì Update để không tốn chi phí cho hàng trăm cây do GridManager sinh ra.
public class TreeFade : MonoBehaviour
{
    [Tooltip("SpriteRenderer của tán cây (để trống sẽ tự lấy trên GameObject này).")]
    [SerializeField] private SpriteRenderer treeRenderer;

    [Tooltip("Độ trong suốt khi Player ở dưới tán cây (0 = tàng hình, 1 = rõ hoàn toàn).")]
    [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.45f;

    [Tooltip("Thời gian chuyển mờ / rõ (giây).")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (treeRenderer == null)
        {
            treeRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        FadeTo(fadedAlpha);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        FadeTo(1f);
    }

    private void FadeTo(float targetAlpha)
    {
        if (treeRenderer == null) return;
        if (!gameObject.activeInHierarchy) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(Fade(targetAlpha));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        Color color = treeRenderer.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            treeRenderer.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        treeRenderer.color = color;
        fadeCoroutine = null;
    }
}
