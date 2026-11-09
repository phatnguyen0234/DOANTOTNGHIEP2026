using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace BlitzyUI
{
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        public const string Version = "1.1.0";

        private abstract class QueuedScreen
        {
            public Screen.Id id;
        }

        private class QueuedScreenPush : QueuedScreen
        {
            public Screen.Data data;
            public string prefabName;
            public PushedDelegate callback;

            public override string ToString()
            {
                return string.Format("[Push] {0}", id);
            }
        }

        private class QueuedScreenPop : QueuedScreen
        {
            public PoppedDelegate callback;

            public override string ToString()
            {
                return string.Format("[Pop] {0}", id);
            }
        }

        public delegate void PushedDelegate(Screen screen);
        public delegate void PoppedDelegate(Screen.Id id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            var _ = Instance;
        }

        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("UIManager_Blitzy");
                        _instance = go.AddComponent<UIManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Paths & Roots")]
        [Tooltip("Resources path where Screen prefabs are loaded from.")]
        public string resourcePrefabDirectory = "Prefabs/UI/Screens";

        [Tooltip("Root canvas holding all active screens. Auto-created if empty.")]
        public Canvas rootCanvas;

        public Camera uiCamera;

        [Header("Settings")]
        [Tooltip("Fix for input order not obeying render order (Unity Canvas sorting).")]
        public bool inputOrderFixEnabled = true;

        [Tooltip("Automatically handle Escape key to pop the topmost screen.")]
        public bool handleEscapeKey = true;

        [Tooltip("Watchdog timeout in seconds to prevent deadlock if PushFinished/PopFinished is forgotten.")]
        public float watchdogTimeoutDuration = 5.0f;

        // Events for other systems (e.g. HUD, Player Input) to observe UI state
        public static event System.Action<Screen> OnScreenPushedEvent;
        public static event System.Action<Screen.Id> OnScreenPoppedEvent;
        public static event System.Action<bool> OnAnyScreenActiveChanged;

        private CanvasScaler _rootCanvasScalar;
        private Dictionary<string, Screen> _cache;
        private Queue<QueuedScreen> _queue;
        private List<Screen> _stack;
        private State _state;

        private PushedDelegate _activePushCallback;
        private PoppedDelegate _activePopCallback;
        private Coroutine _watchdogCoroutine;

        public Vector2 ReferenceResolution => _rootCanvasScalar != null ? _rootCanvasScalar.referenceResolution : new Vector2(1920, 1080);
        public bool HasActiveScreens => _stack != null && _stack.Count > 0;
        public int ActiveScreenCount => _stack != null ? _stack.Count : 0;

        private enum State
        {
            Ready,
            Push,
            Pop
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureRootCanvas();

            if (_cache == null) _cache = new Dictionary<string, Screen>();
            if (_queue == null) _queue = new Queue<QueuedScreen>();
            if (_stack == null) _stack = new List<Screen>();
            _state = State.Ready;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            // Process queue if ready
            if (CanExecuteNextQueueItem())
            {
                ExecuteNextQueueItem();
            }

            // Centralized Escape handling
            if (handleEscapeKey && Input.GetKeyDown(KeyCode.Escape))
            {
                if (HasActiveScreens)
                {
                    if (GetScreen(ScreenIds.Chest) != null)
                    {
                        QueuePopTo(ScreenIds.Chest, include: true);
                    }
                    else if (GetScreen(ScreenIds.Crafting) != null)
                    {
                        QueuePopTo(ScreenIds.Crafting, include: true);
                    }
                    else if (GetScreen(ScreenIds.Shop) != null)
                    {
                        QueuePopTo(ScreenIds.Shop, include: true);
                    }
                    else
                    {
                        QueuePop();
                    }
                }
            }

            // Phím E bật/tắt nhanh túi đồ (Inventory)
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (HasActiveScreens)
                {
                    if (GetScreen(ScreenIds.Chest) != null)
                    {
                        QueuePopTo(ScreenIds.Chest, include: true);
                    }
                    else if (GetScreen(ScreenIds.Crafting) != null)
                    {
                        QueuePopTo(ScreenIds.Crafting, include: true);
                    }
                    else if (GetScreen(ScreenIds.Shop) != null)
                    {
                        QueuePopTo(ScreenIds.Shop, include: true);
                    }
                    else if (GetTopScreen()?.id == ScreenIds.Inventory)
                    {
                        QueuePop();
                    }
                }
                else
                {
                    QueuePush(ScreenIds.Inventory);
                }
            }
        }

        private void EnsureRootCanvas()
        {
            if (rootCanvas != null && !rootCanvas.gameObject.activeInHierarchy)
            {
                rootCanvas.gameObject.SetActive(true);

                if (!rootCanvas.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning("[UIManager] rootCanvas được gán vào một GameObject có cha bị tắt! UIManager sẽ tự tạo RootCanvas riêng để hiển thị UI.");
                    rootCanvas = null;
                }
            }

            if (rootCanvas == null)
            {
                Transform existing = transform.Find("UIManager_RootCanvas");
                if (existing != null)
                {
                    rootCanvas = existing.GetComponent<Canvas>();
                }
            }

            if (rootCanvas == null)
            {
                // Try finding an existing canvas
                rootCanvas = GetComponentInChildren<Canvas>();
            }

            if (rootCanvas == null)
            {
                // Ưu tiên tìm kiếm Canvas chính của Scene (root canvas khác với UIManager_RootCanvas)
                var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in allCanvases)
                {
                    if (c.isRootCanvas && c.gameObject.name != "UIManager_RootCanvas" && c.gameObject.activeInHierarchy)
                    {
                        if (c.gameObject.layer == 5 || c.transform.childCount > 1)
                        {
                            rootCanvas = c;
                            break;
                        }
                    }
                }
            }

            if (rootCanvas == null)
            {
                // Create a dedicated UI Root Canvas
                var canvasGo = new GameObject("UIManager_RootCanvas");
                canvasGo.transform.SetParent(transform, false);
                rootCanvas = canvasGo.AddComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            _rootCanvasScalar = rootCanvas.GetComponent<CanvasScaler>();
            if (_rootCanvasScalar == null)
            {
                _rootCanvasScalar = rootCanvas.gameObject.AddComponent<CanvasScaler>();
                _rootCanvasScalar.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _rootCanvasScalar.referenceResolution = new Vector2(1920, 1080);
                _rootCanvasScalar.matchWidthOrHeight = 0.5f;
            }
        }

        /// <summary>
        /// Queue the screen to be pushed onto the screen stack.
        /// </summary>
        public void QueuePush(Screen.Id id, Screen.Data data = null, string prefabName = null, PushedDelegate callback = null)
        {
            if (id == null)
            {
                Debug.LogError("[UIManager] Cannot push null Screen.Id!");
                return;
            }

            string prefab = prefabName ?? id.defaultPrefabName;

            if (GetScreen(id) != null)
            {
                Debug.LogWarning(string.Format("[UIManager] Screen {0} already exists in the stack. Ignoring push request.", id));
                return;
            }

            var push = new QueuedScreenPush
            {
                id = id,
                data = data,
                prefabName = prefab,
                callback = callback
            };

            _queue.Enqueue(push);

            if (CanExecuteNextQueueItem())
                ExecuteNextQueueItem();
        }

        /// <summary>
        /// Queue the screen to be popped from the screen stack. This will pop all screens on top of it as well.
        /// </summary>
        public void QueuePopTo(Screen.Id id, bool include = false, PoppedDelegate callback = null)
        {
            bool found = false;

            for (int i = 0; i < _stack.Count; i++)
            {
                var screen = _stack[i];

                if (screen.id != id)
                {
                    var queuedPop = new QueuedScreenPop { id = screen.id };
                    _queue.Enqueue(queuedPop);
                }
                else
                {
                    if (include)
                    {
                        var queuedPop = new QueuedScreenPop
                        {
                            id = screen.id,
                            callback = callback
                        };
                        _queue.Enqueue(queuedPop);
                    }

                    if (callback != null && !include)
                        callback(screen.id);

                    found = true;
                    break;
                }
            }

            if (!found)
                Debug.LogWarning(string.Format("[UIManager] {0} was not in the stack.", id));

            if (CanExecuteNextQueueItem())
                ExecuteNextQueueItem();
        }

        /// <summary>
        /// Queue the topmost screen to be popped from the screen stack.
        /// </summary>
        public void QueuePop(PoppedDelegate callback = null)
        {
            Screen topScreen = GetTopScreen();
            if (topScreen == null)
                return;

            var pop = new QueuedScreenPop
            {
                id = topScreen.id,
                callback = callback
            };

            _queue.Enqueue(pop);

            if (CanExecuteNextQueueItem())
                ExecuteNextQueueItem();
        }

        public Screen GetTopScreen()
        {
            if (_stack != null && _stack.Count > 0)
                return _stack[0];

            return null;
        }

        public Screen GetScreen(Screen.Id id)
        {
            if (_stack == null) return null;
            int count = _stack.Count;
            for (int i = 0; i < count; i++)
            {
                if (_stack[i].id == id)
                    return _stack[i];
            }

            return null;
        }

        public T GetScreen<T>(Screen.Id id) where T : Screen
        {
            Screen screen = GetScreen(id);
            return screen as T;
        }

        public void SetVisibility(bool visible)
        {
            if (rootCanvas == null) return;
            var canvasGroup = rootCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = rootCanvas.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = visible ? 1.0f : 0.0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private bool CanExecuteNextQueueItem()
        {
            return _state == State.Ready && _queue.Count > 0;
        }

        private void ExecuteNextQueueItem()
        {
            EnsureRootCanvas();
            QueuedScreen queued = _queue.Dequeue();

            if (queued is QueuedScreenPush queuedPush)
            {
                Screen screenInstance;

                if (_cache.TryGetValue(queuedPush.prefabName, out screenInstance))
                {
                    _cache.Remove(queuedPush.prefabName);
                    screenInstance.transform.SetAsLastSibling();
                    screenInstance.gameObject.SetActive(true);
                    if (screenInstance.transform.parent != null && screenInstance.transform.parent != rootCanvas.transform)
                    {
                        screenInstance.transform.parent.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Kiểm tra xem màn hình này đã có sẵn trong rootCanvas (Scene) hay chưa
                    if (rootCanvas != null)
                    {
                        Screen[] existingScreens = rootCanvas.GetComponentsInChildren<Screen>(true);
                        foreach (var s in existingScreens)
                        {
                            bool isMatch = s.gameObject.name == queuedPush.prefabName 
                                || s.gameObject.name == queuedPush.id.ToString() 
                                || s.gameObject.name == queuedPush.id.defaultPrefabName
                                || (queuedPush.id == ScreenIds.Inventory && (s is InventoryUI || s.gameObject.name.Contains("Bag") || s.gameObject.name.Contains("Inventory")))
                                || (queuedPush.id == ScreenIds.Chest && (s is ChestUI || s.gameObject.name.Contains("Chest")))
                                || (queuedPush.id == ScreenIds.Crafting && (s is CraftingScreen || s.gameObject.name.Contains("Crafting")))
                                || (queuedPush.id == ScreenIds.Shop && (s is ShopScreen || s.gameObject.name.Contains("Shop")))
                                || (queuedPush.id == ScreenIds.Fishing && (s is FishingScreen || s.gameObject.name.Contains("Fishing")));

                            if (isMatch)
                            {
                                screenInstance = s;
                                screenInstance.transform.SetAsLastSibling();
                                screenInstance.gameObject.SetActive(true);
                                if (screenInstance.transform.parent != null && screenInstance.transform.parent != rootCanvas.transform)
                                {
                                    screenInstance.transform.parent.gameObject.SetActive(true);
                                    screenInstance.transform.parent.SetAsLastSibling();
                                }
                                break;
                            }
                        }
                    }

                    if (screenInstance == null)
                    {
                        string path = System.IO.Path.Combine(resourcePrefabDirectory, queuedPush.prefabName).Replace('\\', '/');
                        GameObject prefabGo = Resources.Load<GameObject>(path);

                        if (prefabGo == null)
                        {
                            Debug.LogError(string.Format("[UIManager] Failed to load Screen prefab at path: 'Resources/{0}'", path));
                            _state = State.Ready;
                            if (CanExecuteNextQueueItem()) ExecuteNextQueueItem();
                            return;
                        }

                        GameObject screenGo = Instantiate(prefabGo, rootCanvas.transform, false);
                        RectTransform rt = screenGo.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchorMin = Vector2.zero;
                            rt.anchorMax = Vector2.one;
                            rt.offsetMin = Vector2.zero;
                            rt.offsetMax = Vector2.zero;
                            rt.localScale = Vector3.one;
                            rt.localPosition = Vector3.zero;
                        }

                        screenInstance = screenGo.GetComponent<Screen>() ?? screenGo.GetComponentInChildren<Screen>();

                        if (screenInstance == null)
                        {
                            Debug.LogError(string.Format("[UIManager] Prefab at 'Resources/{0}' does not contain any component inheriting from BlitzyUI.Screen!", path));
                            Destroy(screenGo);
                            _state = State.Ready;
                            if (CanExecuteNextQueueItem()) ExecuteNextQueueItem();
                            return;
                        }
                    }

                    screenInstance.Setup(queuedPush.id, queuedPush.prefabName);
                }

                if (inputOrderFixEnabled)
                {
                    UpdateSortOrderOverrides();
                }

                var topScreen = GetTopScreen();
                if (topScreen != null)
                {
                    topScreen.OnFocusLost();
                }

                _state = State.Push;
                _stack.Insert(0, screenInstance);

                _activePushCallback = queuedPush.callback;

                screenInstance.onPushFinished += HandlePushFinished;

                // Start deadlock watchdog timer
                StartWatchdog(screenInstance, State.Push);

                screenInstance.OnPush(queuedPush.data);

                if (_queue.Count == 0)
                {
                    screenInstance.OnFocus();
                }

                OnScreenPushedEvent?.Invoke(screenInstance);
                OnAnyScreenActiveChanged?.Invoke(true);
            }
            else if (queued is QueuedScreenPop queuedPop)
            {
                Screen screenToPop = GetTopScreen();

                if (screenToPop == null || screenToPop.id != queued.id)
                {
                    Debug.LogWarning(string.Format("[UIManager] Top screen {0} does not match queued pop {1}", screenToPop != null ? screenToPop.id.ToString() : "null", queued.id));
                    _state = State.Ready;
                    if (CanExecuteNextQueueItem()) ExecuteNextQueueItem();
                    return;
                }

                screenToPop.OnFocusLost();

                _state = State.Pop;
                _stack.RemoveAt(0);

                var newTopScreen = GetTopScreen();
                if (newTopScreen != null && _queue.Count == 0)
                {
                    newTopScreen.OnFocus();
                }

                _activePopCallback = queuedPop.callback;

                screenToPop.onPopFinished += HandlePopFinished;

                // Start deadlock watchdog timer
                StartWatchdog(screenToPop, State.Pop);

                screenToPop.OnPop();

                OnScreenPoppedEvent?.Invoke(screenToPop.id);
                if (_stack.Count == 0)
                {
                    OnAnyScreenActiveChanged?.Invoke(false);
                }
            }
        }

        private void UpdateSortOrderOverrides()
        {
            if (rootCanvas == null) return;
            int managedOrder = 1;
            int childCount = rootCanvas.transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                var child = rootCanvas.transform.GetChild(i);
                var screen = child.GetComponent<Screen>() ?? child.GetComponentInChildren<Screen>();
                if (screen != null)
                {
                    // Lấy Canvas của chính màn hình đó (ở root child hoặc ở component Screen)
                    var canvas = child.GetComponent<Canvas>() ?? screen.GetComponent<Canvas>();
                    if (canvas != null && canvas != rootCanvas)
                    {
                        canvas.overrideSorting = true;
                        if (screen.overrideManagedSorting)
                        {
                            canvas.sortingOrder = screen.overrideSortValue;
                        }
                        else
                        {
                            canvas.sortingOrder = managedOrder++;
                        }
                    }
                }
            }
        }

        private void StartWatchdog(Screen screen, State state)
        {
            StopWatchdog();
            if (watchdogTimeoutDuration > 0f)
            {
                _watchdogCoroutine = StartCoroutine(WatchdogRoutine(screen, state));
            }
        }

        private void StopWatchdog()
        {
            if (_watchdogCoroutine != null)
            {
                StopCoroutine(_watchdogCoroutine);
                _watchdogCoroutine = null;
            }
        }

        private IEnumerator WatchdogRoutine(Screen targetScreen, State expectedState)
        {
            yield return new WaitForSecondsRealtime(watchdogTimeoutDuration);

            if (_state == expectedState)
            {
                Debug.LogError(string.Format("[UIManager] <color=red>WATCHDOG TIMEOUT:</color> Screen '{0}' did not complete {1} in {2}s! Forcing State to Ready to prevent UI deadlock.", targetScreen.id, expectedState, watchdogTimeoutDuration));

                if (expectedState == State.Push)
                {
                    HandlePushFinished(targetScreen);
                }
                else if (expectedState == State.Pop)
                {
                    HandlePopFinished(targetScreen);
                }
            }
        }

        private void HandlePushFinished(Screen screen)
        {
            StopWatchdog();
            if (screen != null)
            {
                screen.onPushFinished -= HandlePushFinished;
            }

            _state = State.Ready;

            if (_activePushCallback != null)
            {
                _activePushCallback(screen);
                _activePushCallback = null;
            }

            if (CanExecuteNextQueueItem())
                ExecuteNextQueueItem();
        }

        private void HandlePopFinished(Screen screen)
        {
            StopWatchdog();
            if (screen != null)
            {
                screen.onPopFinished -= HandlePopFinished;

                if (screen.keepCached)
                {
                    screen.gameObject.SetActive(false);
                    if (!_cache.ContainsKey(screen.PrefabName))
                    {
                        _cache.Add(screen.PrefabName, screen);
                    }
                }
                else
                {
                    Destroy(screen.gameObject);
                }
            }

            _state = State.Ready;

            if (_activePopCallback != null)
            {
                _activePopCallback(screen != null ? screen.id : null);
                _activePopCallback = null;
            }

            if (_stack.Count == 0 && _queue.Count == 0)
            {
                OnAnyScreenActiveChanged?.Invoke(false);
            }

            if (CanExecuteNextQueueItem())
                ExecuteNextQueueItem();
        }
    }
}

