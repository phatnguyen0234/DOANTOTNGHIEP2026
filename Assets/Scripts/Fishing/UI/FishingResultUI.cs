using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hiển thị Popup thông báo kết quả sau khi kết thúc lượt câu cá
public class FishingResultUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Panel chính chứa toàn bộ kết quả.")]
    [SerializeField] private GameObject resultPanel;

    [Tooltip("Text tiêu đề (Thành công / Thất bại).")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Text tên loài cá đã câu được.")]
    [SerializeField] private TextMeshProUGUI fishNameText;

    [Tooltip("Image hiển thị ảnh của loài cá.")]
    [SerializeField] private Image fishIconImage;

    [Tooltip("GameObject nhãn 'PERFECT!'")]
    [SerializeField] private GameObject perfectBadge;

    [Tooltip("Text hiển thị điểm kinh nghiệm nhận được.")]
    [SerializeField] private TextMeshProUGUI expText;

    [Tooltip("Thời gian hiển thị tự động biến mất (giây).")]
    [SerializeField] private float autoCloseDuration = 2.5f;

    public event Action OnClosed;

    private void Awake()
    {
        Hide();
    }

    public void ShowResult(FishingResult result)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        gameObject.SetActive(true);

        if (result.IsSuccess && result.CaughtFish != null)
        {
            if (titleText != null) titleText.text = "CÂU ĐƯỢC CÁ!";
            if (fishNameText != null) fishNameText.text = result.CaughtFish.FishName;
            if (fishIconImage != null)
            {
                fishIconImage.gameObject.SetActive(true);
                fishIconImage.sprite = result.CaughtFish.Icon;
            }
            if (perfectBadge != null) perfectBadge.SetActive(result.IsPerfect);
            if (expText != null) expText.text = $"+{result.EarnedExp} EXP";
        }
        else
        {
            if (titleText != null) titleText.text = "CÁ ĐÃ THOÁT!";
            if (fishNameText != null) fishNameText.text = "Tiếc quá, cá đã chạy mất...";
            if (fishIconImage != null) fishIconImage.gameObject.SetActive(false);
            if (perfectBadge != null) perfectBadge.SetActive(false);
            if (expText != null) expText.text = "";
        }

        StopAllCoroutines();
        StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDuration);
        Hide();
    }

    public void Hide()
    {
        StopAllCoroutines();
        if (resultPanel != null) resultPanel.SetActive(false);
        gameObject.SetActive(false);
        OnClosed?.Invoke();
    }
}
