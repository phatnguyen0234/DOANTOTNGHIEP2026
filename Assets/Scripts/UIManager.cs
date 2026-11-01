using TMPro;
using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private float blackScreenDuration = 1.5f;
    [SerializeField] private float fadeDuration = 1f;
    public static UIManager Instance;
    private bool isPassoutRunning = false;

    private void Awake()
    {
        Instance = this;
    //    DontDestroyOnLoad(gameObject);
    }

    public void TriggerPassOutSleep()
    {
        if (isPassoutRunning) return;
        StartCoroutine(PassoutRoutine());
    }

    private IEnumerator PassoutRoutine()
    {
        isPassoutRunning = true;
        PlayerMovement.instance.isSleeping = true;
        screenText.text = "Bạn đã bị gục đi vì kiệt sức...!";
        screenFade.blocksRaycasts = true;
        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        PlayerStamina.Instance.ResetStamina();
        TimeManager.GoToSleep();
     //   PlayerMovement.instance.transform.position = wakeUpPoint.position;
        screenText.text = "Chào buổi sáng!\nMột ngày mới đã bắt đầu.";
        yield return new WaitForSecondsRealtime(blackScreenDuration);
        yield return Fade(1f, 0f);
        screenFade.blocksRaycasts = false;
        PlayerMovement.instance.isSleeping = false;
        isPassoutRunning = false;
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

}
