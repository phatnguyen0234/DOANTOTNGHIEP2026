using System;
using System.Collections;
using System.Collections.Generic;
using Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hệ thống quản lý tập trung toàn bộ giao diện người dùng (UI Management System).
// Đảm bảo bảo toàn 100% tính tương thích ngược với các tính năng cũ (TriggerPassOutSleep).
// Quản lý phân tầng Layer (HUD, Screen, Popup, Overlay), điều phối phím ESC qua UIStack,
// và khởi tạo giao diện từ Prefabs hoặc Scene thông qua UIFactory.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region Preserved Legacy Screen Fade Settings (Tương thích ngược 100%)

    [Header("Legacy Screen Fade Settings (Preserved)")]
    [SerializeField] private CanvasGroup screenFade;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private float blackScreenDuration = 1.5f;
    [SerializeField] private float fadeDuration = 1f;

    private bool isPassoutRunning = false;

    #endregion

    #region Modern UI Management Architecture

    [Header("UI System Settings")]
    [Tooltip("ScriptableObject chứa danh sách Prefab đăng ký cho hệ thống UI.")]
    [SerializeField] private UIRegistry uiRegistry;

    [Tooltip("Canvas gốc chứa toàn bộ UI. Tự động tìm hoặc sinh mới nếu để trống.")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Layer Containers (Tự động tạo nếu để trống)")]
    [SerializeField] private Transform hudLayer;
    [SerializeField] private Transform screenLayer;
    [SerializeField] private Transform popupLayer;
    [SerializeField] private Transform overlayLayer;

    private readonly UIStack uiStack = new UIStack();
    private UIFactory uiFactory;
    private readonly Dictionary<UILayerType, Transform> layerContainers = new Dictionary<UILayerType, Transform>();

    public UIStack Stack => uiStack;
    public UIFactory Factory => uiFactory;
    public Canvas RootCanvas => rootCanvas;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUISystem();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // Điều phối phím ESC tập trung toàn bộ game tại UIManager
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBack();
        }
    }

    #endregion

    #region Initialization & Layer Setup

    private void InitializeUISystem()
    {
        EnsureRootCanvas();
        EnsureEventSystem();
        EnsureLayers();

        uiFactory = new UIFactory(layerContainers, uiRegistry);
    }

    private void EnsureRootCanvas()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInChildren<Canvas>();
        }

        if (rootCanvas == null)
        {
            GameObject canvasObj = new GameObject("UIRoot_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            esObj.transform.SetParent(transform, false);
        }
    }

    private void EnsureLayers()
    {
        Transform canvasTrans = rootCanvas != null ? rootCanvas.transform : transform;

        hudLayer = EnsureLayerTransform(hudLayer, "HUD_Layer", UILayerType.HUD, canvasTrans);
        screenLayer = EnsureLayerTransform(screenLayer, "Screen_Layer", UILayerType.Screen, canvasTrans);
        popupLayer = EnsureLayerTransform(popupLayer, "Popup_Layer", UILayerType.Popup, canvasTrans);
        overlayLayer = EnsureLayerTransform(overlayLayer, "Overlay_Layer", UILayerType.Overlay, canvasTrans);

        layerContainers[UILayerType.HUD] = hudLayer;
        layerContainers[UILayerType.Screen] = screenLayer;
        layerContainers[UILayerType.Popup] = popupLayer;
        layerContainers[UILayerType.Overlay] = overlayLayer;
    }

    private Transform EnsureLayerTransform(Transform existing, string layerName, UILayerType layerType, Transform parent)
    {
        if (existing != null) return existing;

        Transform found = parent.Find(layerName);
        if (found != null) return found;

        GameObject layerObj = new GameObject(layerName, typeof(RectTransform));
        layerObj.transform.SetParent(parent, false);

        RectTransform rect = layerObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return layerObj.transform;
    }

    public Transform GetLayerContainer(UILayerType layerType)
    {
        if (layerContainers.TryGetValue(layerType, out Transform container))
        {
            return container;
        }
        return rootCanvas != null ? rootCanvas.transform : transform;
    }

    #endregion

    #region Public UI API (Screens, Popups, HUDs)

    // Hiển thị một Screen (Túi đồ, Rương đồ, Minigame)
    public T ShowScreen<T>(GameObject prefabOverride = null) where T : UIScreen
    {
        if (uiFactory == null) InitializeUISystem();

        T screen = uiFactory.GetOrCreateView<T>(prefabOverride);
        if (screen != null)
        {
            screen.OnShow();
            uiStack.PushScreen(screen);
        }
        return screen;
    }

    // Đóng một Screen
    public void HideScreen<T>() where T : UIScreen
    {
        if (uiFactory != null && uiFactory.TryGetCachedView(out T screen))
        {
            if (screen.IsOpen)
            {
                screen.OnHide();
                uiStack.RemoveScreen(screen);
            }
        }
    }

    public T GetScreen<T>() where T : UIScreen
    {
        if (uiFactory == null) InitializeUISystem();
        return uiFactory.GetOrCreateView<T>();
    }

    // Hiển thị một Popup (Bảng kết quả câu cá, Hộp thoại xác nhận)
    public T ShowPopup<T>(GameObject prefabOverride = null) where T : UIPopup
    {
        if (uiFactory == null) InitializeUISystem();

        T popup = uiFactory.GetOrCreateView<T>(prefabOverride);
        if (popup != null)
        {
            popup.OnShow();
            uiStack.PushPopup(popup);
        }
        return popup;
    }

    public T GetPopup<T>() where T : UIPopup
    {
        if (uiFactory == null) InitializeUISystem();
        return uiFactory.GetOrCreateView<T>();
    }

    // Đóng một Popup
    public void HidePopup<T>() where T : UIPopup
    {
        if (uiFactory != null && uiFactory.TryGetCachedView(out T popup))
        {
            if (popup.IsOpen)
            {
                popup.OnHide();
            }
        }
    }

    // Hiển thị thành phần HUD (Hotbar, Thanh lực quăng cần)
    public T ShowHUD<T>(GameObject prefabOverride = null) where T : UIHUDView
    {
        if (uiFactory == null) InitializeUISystem();

        T hud = uiFactory.GetOrCreateView<T>(prefabOverride);
        if (hud != null)
        {
            hud.OnShow();
        }
        return hud;
    }

    // Ẩn thành phần HUD
    public void HideHUD<T>() where T : UIHUDView
    {
        if (uiFactory != null && uiFactory.TryGetCachedView(out T hud))
        {
            if (hud.IsOpen)
            {
                hud.OnHide();
            }
        }
    }

    public T GetHUD<T>() where T : UIHUDView
    {
        if (uiFactory == null) InitializeUISystem();
        return uiFactory.GetOrCreateView<T>();
    }

    // Xử lý nút Back / ESC toàn cục
    public bool HandleBack()
    {
        // 1. Kiểm tra xem có Popup hoặc Screen nào trong Stack đang mở để đóng
        if (uiStack.HandleBack())
        {
            return true;
        }

        // 2. Nếu không có UI nào đang mở, phím ESC có thể mở túi đồ của Player
        if (Inventory.Instance != null && !Inventory.Instance.IsBagOpen)
        {
            Inventory.Instance.OpenBag();
            return true;
        }

        return false;
    }

    #endregion

    #region Preserved Legacy Methods (Giữ nguyên 100% logic để không làm lỗi TimeManager, PlayerMovement)

    public void TriggerPassOutSleep()
    {
        if (isPassoutRunning) return;
        StartCoroutine(PassoutRoutine());
    }

    private IEnumerator PassoutRoutine()
    {
        isPassoutRunning = true;
        if (PlayerMovement.instance != null) PlayerMovement.instance.isSleeping = true;
        if (screenText != null) screenText.text = "Bạn đã bị gục đi vì kiệt sức...!";
        if (screenFade != null) screenFade.blocksRaycasts = true;

        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(blackScreenDuration);

        if (PlayerStamina.Instance != null) PlayerStamina.Instance.ResetStamina();
        TimeManager.GoToSleep();

        if (screenText != null) screenText.text = "Chào buổi sáng!\nMột ngày mới đã bắt đầu.";
        yield return new WaitForSecondsRealtime(blackScreenDuration);

        yield return Fade(1f, 0f);
        if (screenFade != null) screenFade.blocksRaycasts = false;
        if (PlayerMovement.instance != null) PlayerMovement.instance.isSleeping = false;
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

    #endregion
}
