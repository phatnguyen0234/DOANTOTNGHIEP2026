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
            gameObject.SetActive(true);
        }

        public virtual void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
