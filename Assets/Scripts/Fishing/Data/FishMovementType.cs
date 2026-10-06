using UnityEngine;

// Phân loại phong cách bơi của từng loài cá trong Minigame
public enum FishMovementType
{
    Smooth,     // Bơi điềm tĩnh, mượt mà, đổi hướng từ tốn (Dễ - Trung bình)
    Dart,       // Giật cục, hay nhảy vọt bất ngờ (Khó)
    Floater,    // Có xu hướng nổi lên nửa trên của thanh (Trung bình)
    Sinker,     // Có xu hướng chìm xuống đáy rất nhanh (Trung bình)
    Mixed       // Kết hợp các hành vi ngẫu nhiên, khó đoán (Rất khó / Cá hiếm / Boss)
}
