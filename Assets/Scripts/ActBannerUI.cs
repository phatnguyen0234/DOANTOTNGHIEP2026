using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ActBannerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private CanvasGroup introBlackScreen;
    [SerializeField] private TMP_Text textTile;
    [SerializeField] private TMP_Text subTitle;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float duration = 2.5f;
    //1 biến button
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void OnEnable()
    {
        ActManager.OnActStarted += PassAct;
        //TODO: đki hàm khi quét hoàn thành
    }

    void OnDisable()
    {
        ActManager.OnActStarted -= PassAct;
        //TODO: hủy hàm quét hhoanf thành
    }

    public void NotifyQuest()
    {
        //TODO: Button.SetActive(true)
        //TODO: Sau khi click vào thì Buttton.SetActive(false)
    }
    public void PassAct(ActData data)
    {
        StartCoroutine(Chuyenman(data));
    }

    private IEnumerator Chuyenman(ActData data)
    {
        PlayerMovement.instance.isCutSceneLoaded = true;
        yield return FadeGroup(introBlackScreen, 1f, 0f, duration);
       // yield return new WaitForSecondsRealtime(data.DisplayDuration);
        textTile.text = data.ActTitle;
        subTitle.text = data.ActSubtitle;
        yield return FadeGroup(screenFade, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(data.DisplayDuration);
        yield return FadeGroup(screenFade, 1f, 0f, fadeDuration);
        PlayerMovement.instance.isCutSceneLoaded = false;
    }

    private IEnumerator FadeGroup(CanvasGroup targetGroup, float from, float to, float duration)
    {
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            targetGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        targetGroup.alpha = to;
    }
}
