using UnityEngine;

namespace Core.UI
{
    // Lớp cơ sở cho các màn hình giao diện lớn: Inventory, Chest, Minigame
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour, IUIView
    {
        [Header("Screen Settings")]
        [Tooltip("Nếu true, khi screen này mở sẽ tự động ẩn các screen bên dưới nó.")]
        [SerializeField] private bool hideLowerScreens = false;

        private CanvasGroup canvasGroup;
        private bool isInitialized = false;

        public GameObject GameObject => gameObject;
        public virtual bool IsOpen => gameObject.activeSelf;
        public virtual UILayerType Layer => UILayerType.Screen;
        public virtual bool HideLowerScreens => hideLowerScreens;

        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                {
                    canvasGroup = GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
                return canvasGroup;
            }
        }

        public virtual void Initialize()
        {
            if (isInitialized) return;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            isInitialized = true;
        }

        public virtual void OnShow()
        {
            Initialize();
            EnsureAncestorsActive();
            gameObject.SetActive(true);
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = 1f;
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
            }
        }

        private void EnsureAncestorsActive()
        {
            if (transform.localScale == Vector3.zero) transform.localScale = Vector3.one;
            Transform p = transform.parent;
            while (p != null && p.GetComponent<Canvas>() == null)
            {
                if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                if (p.localScale == Vector3.zero) p.localScale = Vector3.one;
                p = p.parent;
            }
        }

        public virtual void OnHide()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.blocksRaycasts = false;
                CanvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        public virtual void OnPause()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.blocksRaycasts = false;
                CanvasGroup.interactable = false;
            }
        }

        public virtual void OnResume()
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.blocksRaycasts = true;
                CanvasGroup.interactable = true;
            }
        }
    }
}
