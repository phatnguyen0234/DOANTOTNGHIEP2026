namespace Core.UI
{
    // Định nghĩa các lớp phân tầng hiển thị trong hệ thống UI (Canvas Sorting Layers)
    public enum UILayerType
    {
        HUD = 10,       // Hiển thị thanh trạng thái, hotbar, minimap (thường trực)
        Screen = 20,    // Các màn hình chính: Inventory, Chest, Minigame
        Popup = 30,     // Bảng thông báo, kết quả câu cá, hộp thoại xác nhận (nổi lên trên)
        Overlay = 40    // Lớp cao nhất: Drag & Drop Ghost, Fader, Loading
    }
}
