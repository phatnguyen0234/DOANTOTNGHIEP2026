using TMPro;
using UnityEngine;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private float blackScreenDuration = 1.5f;
    [SerializeField] private float fadeDuration = 1f;
    public static ScreenFader Instance;
    private bool isPassoutRunning = false;

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerPassOutSleep()
    {
        if (isPassoutRunning) return;
        StartCoroutine(PassoutRoutine());
    }

    private IEnumerator PassoutRoutine()
    {
        isPassoutRunning = true;
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.isSleeping = true;
        }
        if (screenText != null)
        {
            screenText.text = "Bạn đã bị gục đi vì kiệt sức...!";
        }
        if (screenFade != null)
        {
            screenFade.blocksRaycasts = true;
        }
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        if (PlayerStamina.Instance != null)
        {
            PlayerStamina.Instance.ResetStamina();
        }
        TimeManager.GoToSleep();
        if (screenText != null)
        {
            screenText.text = "Chào buổi sáng!\nMột ngày mới đã bắt đầu.";
        }
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        yield return Fade(1f, 0f);
        if (screenFade != null)
        {
            screenFade.blocksRaycasts = false;
        }
        if (PlayerMovement.instance != null)
        {
            PlayerMovement.instance.isSleeping = false;
        }
        isPassoutRunning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (screenFade == null) yield break;
        for (float time = 0f; time < fadeDuration; time += Time.unscaledDeltaTime)
        {
            screenFade.alpha = Mathf.Lerp(from, to, time / fadeDuration);
            yield return null;
        }
        screenFade.alpha = to;
    }
}

