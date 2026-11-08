using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    // Quản lý việc Instantiate từ Prefab và Caching instance cho từng loại UI View
    public class UIFactory
    {
        private readonly Dictionary<Type, IUIView> cachedViews = new Dictionary<Type, IUIView>();
        private readonly Dictionary<UILayerType, Transform> layerContainers;
        private readonly UIRegistry registry;

        public UIFactory(Dictionary<UILayerType, Transform> layerContainers, UIRegistry registry = null)
        {
            this.layerContainers = layerContainers;
            this.registry = registry;
        }

        // Đăng ký thủ công một View đã có sẵn (ví dụ trên Scene cũ) vào Cache
        public void RegisterExistingView<T>(T viewInstance) where T : Component, IUIView
        {
            if (viewInstance == null) return;
            Type viewType = typeof(T);
            if (!cachedViews.ContainsKey(viewType))
            {
                cachedViews[viewType] = viewInstance;
            }
        }

        // Lấy hoặc tạo mới một View theo Type chỉ định
        public T GetOrCreateView<T>(GameObject prefabOverride = null) where T : Component, IUIView
        {
            Type viewType = typeof(T);

            // 1. Kiểm tra Cache
            if (cachedViews.TryGetValue(viewType, out IUIView existingView))
            {
                if (existingView != null && existingView.GameObject != null)
                {
                    return existingView as T;
                }
                cachedViews.Remove(viewType);
            }

            // 2. Tìm trong Scene hiện tại (Hỗ trợ tương thích ngược chuyển đổi mượt mà)
            T sceneView = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (sceneView != null)
            {
                cachedViews[viewType] = sceneView;
                sceneView.Initialize();
                return sceneView;
            }

            // 3. Khởi tạo từ Prefab
            GameObject prefab = prefabOverride;
            if (prefab == null && registry != null)
            {
                prefab = registry.GetPrefab<T>();
            }

            if (prefab == null)
            {
                Debug.LogWarning($"[UIFactory] Không tìm thấy Prefab hoặc Scene Instance cho View: {viewType.Name}");
                return null;
            }

            // Xác định Layer Container tương ứng
            T tempComponent = prefab.GetComponent<T>();
            UILayerType targetLayer = tempComponent != null ? tempComponent.Layer : UILayerType.Screen;

            Transform parentTransform = null;
            if (layerContainers != null && layerContainers.TryGetValue(targetLayer, out Transform container))
            {
                parentTransform = container;
            }

            GameObject spawnedObj = UnityEngine.Object.Instantiate(prefab, parentTransform);
            spawnedObj.name = prefab.name;

            T viewComponent = spawnedObj.GetComponent<T>();
            if (viewComponent == null)
            {
                viewComponent = spawnedObj.GetComponentInChildren<T>(true);
            }

            if (viewComponent != null)
            {
                cachedViews[viewType] = viewComponent;
                viewComponent.Initialize();
                spawnedObj.SetActive(false);
            }
            else
            {
                Debug.LogError($"[UIFactory] Prefab {prefab.name} không chứa component {viewType.Name}!");
            }

            return viewComponent;
        }

        public bool TryGetCachedView<T>(out T view) where T : class, IUIView
        {
            Type viewType = typeof(T);
            if (cachedViews.TryGetValue(viewType, out IUIView val) && val != null)
            {
                view = val as T;
                return view != null;
            }
            view = null;
            return false;
        }
    }
}
