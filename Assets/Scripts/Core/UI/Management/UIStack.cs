using System.Collections.Generic;
using UnityEngine;

namespace Core.UI
{
    // Quản lý ngăn xếp điều hướng LIFO cho Screens và Popups.
    // Điều phối phím ESC hoặc Back button đóng mở giao diện có thứ tự, tránh xung đột.
    public class UIStack
    {
        private readonly List<UIScreen> activeScreens = new List<UIScreen>();
        private readonly Stack<UIPopup> activePopups = new Stack<UIPopup>();

        public int ScreenCount => activeScreens.Count;
        public int PopupCount => activePopups.Count;

        // Thêm một Screen vào danh sách đang mở
        public void PushScreen(UIScreen screen)
        {
            if (screen == null) return;

            if (!activeScreens.Contains(screen))
            {
                if (screen.HideLowerScreens)
                {
                    // Tạm dừng tương tác các screen bên dưới
                    for (int i = 0; i < activeScreens.Count; i++)
                    {
                        activeScreens[i].OnPause();
                    }
                }

                activeScreens.Add(screen);
            }
        }

        // Loại bỏ một Screen khỏi danh sách đang mở
        public void RemoveScreen(UIScreen screen)
        {
            if (screen == null) return;

            if (activeScreens.Remove(screen))
            {
                // Khôi phục lại screen trên cùng còn lại (nếu có)
                if (activeScreens.Count > 0)
                {
                    activeScreens[activeScreens.Count - 1].OnResume();
                }
            }
        }

        // Đẩy một Popup lên trên cùng
        public void PushPopup(UIPopup popup)
        {
            if (popup == null) return;
            activePopups.Push(popup);
        }

        // Lấy và xóa Popup trên cùng
        public UIPopup PopPopup()
        {
            if (activePopups.Count > 0)
            {
                return activePopups.Pop();
            }
            return null;
        }

        // Xử lý nút Back hoặc phím ESC
        // Ưu tiên 1: Đóng Popup trên cùng
        // Ưu tiên 2: Nếu có Screen đang mở -> Đóng Screen trên cùng
        // Trả về true nếu đã xử lý đóng một UI nào đó; false nếu không có UI nào đang mở
        public bool HandleBack()
        {
            // 1. Ưu tiên đóng Popup trước
            while (activePopups.Count > 0)
            {
                UIPopup topPopup = activePopups.Pop();
                if (topPopup != null && topPopup.IsOpen)
                {
                    topPopup.OnHide();
                    return true;
                }
            }

            // 2. Đóng Screen trên cùng
            if (activeScreens.Count > 0)
            {
                UIScreen topScreen = activeScreens[activeScreens.Count - 1];
                activeScreens.RemoveAt(activeScreens.Count - 1);

                if (topScreen != null && topScreen.IsOpen)
                {
                    topScreen.OnHide();

                    if (activeScreens.Count > 0)
                    {
                        activeScreens[activeScreens.Count - 1].OnResume();
                    }
                    return true;
                }
            }

            return false;
        }

        public void ClearAll()
        {
            while (activePopups.Count > 0)
            {
                UIPopup pop = activePopups.Pop();
                if (pop != null && pop.IsOpen) pop.OnHide();
            }

            for (int i = activeScreens.Count - 1; i >= 0; i--)
            {
                if (activeScreens[i] != null && activeScreens[i].IsOpen)
                {
                    activeScreens[i].OnHide();
                }
            }
            activeScreens.Clear();
        }
    }
}
