using UnityEngine;

namespace Core.UI
{
    // Lớp cơ sở cho các bảng thông báo, popup xác nhận, kết quả câu cá
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIPopup : MonoBehaviour, IUIView
    {
        [Header("Popup Settings")]
        [Tooltip("Tự động chặn tương tác chuột xuống các giao diện nằm dưới.")]
        [SerializeField] private bool blockBehindRaycasts = true;

        private CanvasGroup canvasGroup;
        private bool isInitialized = false;

        public GameObject GameObject => gameObject;
        public virtual bool IsOpen => gameObject.activeSelf;
        public virtual UILayerType Layer => UILayerType.Popup;
        public virtual bool BlockBehindRaycasts => blockBehindRaycasts;

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
    }
}
