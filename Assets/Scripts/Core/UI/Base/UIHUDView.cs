using UnityEngine;

namespace Core.UI
{
    // Lớp cơ sở cho các thành phần HUD thường trực: Hotbar, thanh lực câu cá, thanh máu, thể lực
    public abstract class UIHUDView : MonoBehaviour, IUIView
    {
        private bool isInitialized = false;

        public GameObject GameObject => gameObject;
        public virtual bool IsOpen => gameObject.activeSelf;
        public virtual UILayerType Layer => UILayerType.HUD;

        public virtual void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
        }

        public virtual void OnShow()
        {
            Initialize();
            EnsureAncestorsActive();
            gameObject.SetActive(true);
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
            gameObject.SetActive(false);
        }
    }
}
