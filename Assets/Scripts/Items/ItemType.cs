using UnityEngine;

// Phân loại danh mục của Item trong game Farming.
public enum ItemType
{
    Seed,
    Crop,
    Resource,
    AnimalProduct,
    Food,
    Tool,
    Material,
    Quest
}

// Định nghĩa các loại công cụ và hành vi tương tác của vật phẩm trong game Farming.
public enum ToolType
{
    None,
    Hoe,         // Cuốc đất
    Shovel,      // Xẻng: đào cây, hoàn trả ô đất
    Axe,         // rìu: chặt cây
    Pickaxe,     // đập đá
    WateringCan, // Bình tưới nước
    SeedBag,     // Hạt giống / Gieo trồng
    Harvest      // Thu hoạch
}

