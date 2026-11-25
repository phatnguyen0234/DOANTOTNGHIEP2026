using System;
using UnityEngine;

public enum AnimalHome
{
    Coop,       // chuồng gà: gà, vịt, thỏ đã thuần hoá
    Barn,       // chuồng bò: bò, dê, cừu
    WildOnly    // chỉ sống trong rừng (hươu, cáo)
}

// Hàng trong sheet hoạt ảnh đã chuẩn hoá (4 khung mỗi hàng, sprite gốc quay mặt sang trái).
public enum AnimalAnim
{
    WalkSide = 0,
    WalkDown = 1,
    WalkUp = 2,
    Eat = 3,
    Sleep = 4
}

// Cấu hình một loài con vật: hình ảnh, tốc độ, sản phẩm, cách mua và cách thuần hoá.
// Tạo / cập nhật tự động bằng Tools > Map > Build Ranch.
[CreateAssetMenu(fileName = "AnimalSpecies", menuName = "Farm/Animal Species")]
public class AnimalSpecies : ScriptableObject
{
    public const int FrameCount = 20;

    [Serializable]
    public class Variant
    {
        public string name;
        public Sprite[] frames = new Sprite[FrameCount];   // con trưởng thành: 5 hàng x 4 khung
        public Sprite[] babyFrames;                         // con non (để trống nếu loài không có)
        public ItemData product;                            // ghi đè sản phẩm theo màu lông (vd lông thỏ)
        public ItemData largeProduct;
    }

    public string id;
    public string displayName;
    public AnimalHome home;
    public Variant[] variants = new Variant[0];

    [Header("Di chuyển")]
    [Min(0.1f)] public float moveSpeed = 1f;
    [Min(0.05f)] public float colliderRadius = 0.25f;
    [Min(1f)] public float framesPerSecond = 6f;

    [Header("Thức ăn")]
    [Tooltip("Những thứ con vật ăn được, xếp theo thứ tự ưu tiên khi ấn J cho ăn nhanh.")]
    public ItemData[] foods = new ItemData[0];

    [Header("Sản xuất")]
    public ItemData product;
    public ItemData largeProduct;
    [Tooltip("Cho sản phẩm sau mỗi bao nhiêu lần được ăn (gà, bò: 1; dê, vịt: 2; cừu, thỏ: 3).")]
    [Min(1)] public int productEveryDays = 1;
    [Tooltip("Số ngày được cho ăn để con non trưởng thành.")]
    [Min(0)] public int daysToAdult = 3;
    [Range(0, 1000)] public int largeProductFriendship = 600;

    [Header("Mua / thuần hoá")]
    [Tooltip("Vật phẩm mua ở cửa hàng trang trại; cầm nó chuột phải vào chuồng để thả con non.")]
    public ItemData purchaseItem;
    [Tooltip("Thức ăn dùng để thuần hoá con hoang dã. Để trống = không thuần hoá được.")]
    public ItemData tameFood;
    [Min(1)] public int tameFeedsNeeded = 3;
    [Min(0f)] public float fleeDistance = 3f;

    public bool IsTamable => tameFood != null && home != AnimalHome.WildOnly;

    public Variant GetVariant(int index)
    {
        if (variants == null || variants.Length == 0) return null;
        return variants[Mathf.Clamp(index, 0, variants.Length - 1)];
    }

    public bool IsAdult(int ageDays) => ageDays >= daysToAdult;

    // So theo ItemID giống Inventory (dự án có vài asset trùng ID).
    public bool Eats(ItemData item) => item != null && foods != null && Array.Exists(foods, f => f != null && f.ItemID == item.ItemID);

    public ItemData FavoriteFood => foods != null && foods.Length > 0 ? foods[0] : null;

    public Sprite[] FramesFor(int variant, bool adult)
    {
        Variant v = GetVariant(variant);
        if (v == null) return null;
        bool hasBaby = v.babyFrames != null && v.babyFrames.Length == FrameCount && v.babyFrames[0] != null;
        return !adult && hasBaby ? v.babyFrames : v.frames;
    }

    public ItemData ProductFor(int variant, bool large)
    {
        Variant v = GetVariant(variant);
        ItemData normal = v != null && v.product != null ? v.product : product;
        ItemData big = v != null && v.largeProduct != null ? v.largeProduct : largeProduct;
        return large && big != null ? big : normal;
    }
}
