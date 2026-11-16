using TMPro;
using UnityEngine;
using System.Collections;
using UnityEditor.Toolbars;

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

    void OnEnable()
    {
        //TODO: ActManger.instance.hoi += PassHoi(scriptable);
    }

    void Disable()
    {
        //TODO: ActManger.instance.hoi -= PassHoi(scriptable);
    }

    public void PassHoi()
    {
        //StartCoroutine(Chuyenman());
    }

    //private IEnumerator Chuyenman()
    //{
    //    //TODO: khoa di chuyen nhan vat, toi nghi nen co 1 bien khoa PlayerMovement.instance.moveDirection = Vector2.zero;
    //    //TODO: Fade(1f,0f)
    //    //TODO: Gan ActTile and ActSubtitle
    //    //TODO: Fade(0f,1f)
    //    //TODO: Delay Coroutine(2.5s)
    //    //TODO: Fade(1f,0f) 
    //    //TODO: tra quyen dieu khien player
    //}

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
