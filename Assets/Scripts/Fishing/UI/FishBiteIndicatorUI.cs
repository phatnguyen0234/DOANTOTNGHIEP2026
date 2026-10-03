using System.Collections;
using UnityEngine;

// Hiển thị hiệu ứng bóng bóng chấm than "!" khi cá cắn câu
public class FishBiteIndicatorUI : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Khoảng dịch chuyển vị trí so với điểm mục tiêu.")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0.8f, 0);

    [Tooltip("Animation curve scale nảy của dấu chấm than.")]
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(Vector3 worldPosition)
    {
        gameObject.SetActive(true);
        transform.position = worldPosition + offset;
        StopAllCoroutines();
        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        transform.localScale = Vector3.zero;
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1.2f, popCurve.Evaluate(t));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
