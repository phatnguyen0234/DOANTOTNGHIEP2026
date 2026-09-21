using UnityEngine;

// Interface chuẩn hóa cho tất cả các đối tượng trong thế giới có khả năng rơi vật phẩm (Cây, Nông sản, Đá, Quái vật, Thùng đồ...).
// Cho phép hệ thống rơi vật phẩm hoạt động hoàn toàn độc lập (Decoupled) mà không cần biết chi tiết đối tượng cụ thể là gì.
public interface IDropSource
{
    // Trả về bảng tỉ lệ rơi cấu hình của đối tượng
    DropTable GetDropTable();

    // Trả về vị trí phát sinh vật phẩm rơi trong thế giới
    Vector3 GetDropPosition();

    // Trả về ngữ cảnh rơi (công cụ, người tác động, cấp bậc, vận may...)
    DropContext GetDropContext();
}
