using System;
using UnityEngine;
using BlitzyUI;

// Điều khiển màn hình Câu cá (FishingScreen) trong hệ thống UIManager.
// Tích hợp cả Minigame kéo cá (FishingMinigame) và Popup kết quả (FishingResultUI).
// Quản lý toàn bộ vòng đời Screen: Nhận dữ liệu cá/cần câu khi Push, chuyển sang bảng kết quả khi câu xong, và tự động Pop khi kết thúc.
[RequireComponent(typeof(Canvas))]
public class FishingScreen : BlitzyUI.Screen
{
    [Header("UI Panels")]
    [Tooltip("Panel chứa toàn bộ Minigame kéo cá (thanh xanh, cá, tiến độ).")]
    [SerializeField] private GameObject minigamePanel;

    [Tooltip("Panel Popup hiển thị kết quả (thành công/thất bại).")]
    [SerializeField] private GameObject resultPanel;

    [Header("Sub-Systems")]
    [Tooltip("Component FishingMinigame điều khiển logic vật lý & luật chơi.")]
    [SerializeField] private FishingMinigame minigame;

    [Tooltip("Component FishingResultUI hiển thị popup phần thưởng.")]
    [SerializeField] private FishingResultUI resultUI;

    public static FishingScreen Instance { get; private set; }

    // Sự kiện toàn cục thông báo kết quả câu cá cho các hệ thống khác (FishingController, Quest, Audio, v.v.)
    public static event Action<FishingResult> OnAnyFishingFinished;
    public event Action<FishingResult> OnFishingFinished;

    public bool IsMinigameActive => minigame != null && minigame.IsActive;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        ResolveComponents();
    }

    private void OnDestroy()
    {
        UnbindEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region BlitzyUI.Screen Lifecycle

    public override void OnSetup()
    {
        Instance = this;
        keepCached = true;

        ResolveComponents();
        BindEvents();
    }

    public override void OnPush(Data data)
    {
        FishData fish = null;
        FishingRodData rod = null;

        if (data != null)
        {
            data.TryGet("fish", out fish);
            data.TryGet("rod", out rod);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(true);
        }

        if (minigame != null)
        {
            minigame.StartMinigame(fish, rod);
        }
        else
        {
            Debug.LogError("[FishingScreen] Không tìm thấy FishingMinigame component để khởi động minigame!", this);
        }

        PushFinished();
    }

    public override void OnPop()
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        PopFinished();
    }

    public override void OnFocus()
    {
    }

    public override void OnFocusLost()
    {
    }

    #endregion

    #region Event Binding & Result Flow

    private void ResolveComponents()
    {
        if (minigame == null) minigame = GetComponentInChildren<FishingMinigame>(true);
        if (resultUI == null) resultUI = GetComponentInChildren<FishingResultUI>(true);

        if (minigamePanel == null && minigame != null)
        {
            minigamePanel = minigame.gameObject;
        }

        if (resultPanel == null && resultUI != null)
        {
            resultPanel = resultUI.gameObject;
        }
    }

    private void BindEvents()
    {
        ResolveComponents();

        if (minigame != null)
        {
            minigame.OnMinigameFinished -= HandleMinigameFinished;
            minigame.OnMinigameFinished += HandleMinigameFinished;
        }

        if (resultUI != null)
        {
            resultUI.OnClosed -= HandleResultClosed;
            resultUI.OnClosed += HandleResultClosed;
        }
    }

    private void UnbindEvents()
    {
        if (minigame != null)
        {
            minigame.OnMinigameFinished -= HandleMinigameFinished;
        }

        if (resultUI != null)
        {
            resultUI.OnClosed -= HandleResultClosed;
        }
    }

    private void HandleMinigameFinished(FishingResult result)
    {
        OnFishingFinished?.Invoke(result);
        OnAnyFishingFinished?.Invoke(result);

        if (minigamePanel != null)
        {
            minigamePanel.SetActive(false);
        }

        if (resultUI != null)
        {
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }
            resultUI.ShowResult(result);
        }
        else
        {
            CloseScreen();
        }
    }

    private void HandleResultClosed()
    {
        CloseScreen();
    }

    public void CloseScreen()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.Fishing, include: true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #endregion
}

