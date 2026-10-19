using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Giường ngủ kiểu Stardew Valley: bước lên giường -> hỏi có đi ngủ không.
// Y: khoá di chuyển, màn hình tối dần, tua tới sáng hôm sau (cây lớn, đất khô, lưu game - xem TimeManager.GoToSleep) rồi sáng dần lại.
// N hoặc bước ra khỏi giường: huỷ. Hộp thoại và màn đen được tạo bằng code nên chỉ cần gắn script vào object giường.
[RequireComponent(typeof(BoxCollider2D))]
public class Bed : MonoBehaviour
{
    [SerializeField] private KeyCode yesKey = KeyCode.Y;
    [SerializeField] private KeyCode noKey = KeyCode.N;

    [Tooltip("Chỗ người chơi đứng khi thức dậy. Để trống = giữ nguyên chỗ cũ.")]
    [SerializeField] private Transform wakeUpPoint;

    [Tooltip("Thời gian màn hình tối dần / sáng dần (giây).")]
    [SerializeField] private float fadeDuration = 1f;

    [Tooltip("Thời gian giữ màn hình đen trước và sau khi sang ngày (giây).")]
    [SerializeField] private float blackScreenDuration = 1.5f;

    private enum State { Idle, Asking, Sleeping }
    private State state = State.Idle;

    private GameObject dialog;
    private CanvasGroup screenFade;
    private TMP_Text screenText;

    // Lúc gắn script: collider (tự thêm, vừa khít hình giường) chuyển thành vùng trigger.
    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (state != State.Asking) return;

        if (Input.GetKeyDown(yesKey))
        {
            StartCoroutine(SleepRoutine());
        }
        else if (Input.GetKeyDown(noKey))
        {
            SetAsking(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (state == State.Idle && collision.CompareTag("Player")) SetAsking(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (state == State.Asking && collision.CompareTag("Player")) SetAsking(false);
    }

    private void SetAsking(bool asking)
    {
        state = asking ? State.Asking : State.Idle;
        dialog.SetActive(asking);
    }

    private IEnumerator SleepRoutine()
    {
        state = State.Sleeping;
        dialog.SetActive(false);

        PlayerMovement player = PlayerMovement.instance;
        if (player != null) player.isSleeping = true;

        screenText.text = "Zzz...";
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(blackScreenDuration);

        TimeManager.GoToSleep();
        if (player != null && wakeUpPoint != null) player.transform.position = wakeUpPoint.position;

        screenText.text = "Chào buổi sáng!\nMột ngày mới đã bắt đầu.";
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        yield return Fade(1f, 0f);

        if (player != null) player.isSleeping = false;
        state = State.Idle;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float time = 0f; time < fadeDuration; time += Time.unscaledDeltaTime)
        {
            screenFade.alpha = Mathf.Lerp(from, to, time / fadeDuration);
            yield return null;
        }
        screenFade.alpha = to;
    }

    // Canvas phủ màn hình: lớp đen (tối dần khi ngủ) + hộp thoại hỏi ở cạnh dưới.
    private void BuildUI()
    {
        Canvas canvas = new GameObject("Bed UI", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Image black = CreateImage("Screen Fade", canvas.transform, Color.black);
        Stretch(black.rectTransform);
        screenFade = black.gameObject.AddComponent<CanvasGroup>();
        screenFade.alpha = 0f;
        screenFade.blocksRaycasts = false;
        screenText = CreateText(black.transform, 56f);

        Image box = CreateImage("Sleep Dialog", canvas.transform, new Color(0f, 0f, 0f, 0.75f));
        box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        box.rectTransform.sizeDelta = new Vector2(820f, 170f);
        box.rectTransform.anchoredPosition = new Vector2(0f, 150f);
        CreateText(box.transform, 40f).text =
            $"Đi ngủ đến sáng hôm sau?\n<color=#FFD54F>[{yesKey}]</color> Có        <color=#FFD54F>[{noKey}]</color> Không";
        dialog = box.gameObject;
        dialog.SetActive(false);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, float fontSize)
    {
        TextMeshProUGUI text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        Stretch(text.rectTransform);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
