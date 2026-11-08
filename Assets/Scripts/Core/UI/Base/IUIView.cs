using UnityEngine;

namespace Core.UI
{
    // Hợp đồng cơ bản cho toàn bộ các thành phần hiển thị giao diện trong hệ thống UI Management
    public interface IUIView
    {
        GameObject GameObject { get; }
        bool IsOpen { get; }
        UILayerType Layer { get; }

        void Initialize();
        void OnShow();
        void OnHide();
    }
}
