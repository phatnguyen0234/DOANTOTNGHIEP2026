// Định nghĩa toàn bộ các trạng thái trong vòng đời một lượt câu cá
public enum FishingState
{
    Idle,           // Đang đứng yên, chưa câu hoặc đã cất cần
    Charging,       // Đang giữ nút để tích lực quăng cần (Cast Power)
    Casting,        // Đã thả nút, phao đang bay theo quỹ đạo hình vòng cung tới vị trí đích
    WaitingForBite, // Phao đã tiếp nước ổn định, đang chờ cá cắn câu
    FishBite,       // Cá cắn câu! Xuất hiện hiệu ứng '!' và đếm ngược thời gian phản xạ (Reaction Window)
    Hooked,         // Người chơi giật cần thành công, chuẩn bị chuyển cảnh sang Minigame
    Reeling,        // Đang chơi Minigame kéo cá (Thanh xanh + Cá + Progress)
    Success,        // Câu thành công! Thưởng cá và EXP
    Failed          // Câu thất bại (ném hụt trên cạn, giật trễ, hoặc cá tuột trong minigame)
}
