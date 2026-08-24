using UnityEngine;

// ScriptableObject đại diện cho dữ liệu tĩnh cấu hình của một Item trong game.
// Chứa các thông tin bất biến như tên, ID, icon, giá bán, max stack.
// Tuyệt đối không lưu trữ dữ liệu runtime (như số lượng hiện có) tại đây.
[CreateAssetMenu(fileName = "NewItemData", menuName = "Farming/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    [Tooltip("Mã định danh duy nhất của Item (Ví dụ: wood, stone, carrot_seed).")]
    [SerializeField] private string itemID;

    [Tooltip("Tên hiển thị trong UI của Item.")]
    [SerializeField] private string itemName;

    [Tooltip("Mô tả chi tiết hoặc công dụng của Item.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    [Tooltip("Hình ảnh biểu tượng hiển thị trong Inventory / UI.")]
    [SerializeField] private Sprite icon;

    [Tooltip("Loại Item (Seed, Crop, Tool, Resource,...).")]
    [SerializeField] private ItemType itemType;

    [Header("Stack & Economy")]
    [Tooltip("Số lượng tối đa có thể xếp chồng trong một ô (Tối thiểu là 1).")]
    [SerializeField, Min(1)] private int maxStack = 99;

    [Tooltip("Giá mua tại cửa hàng.")]
    [SerializeField, Min(0)] private int buyPrice = 0;

    [Tooltip("Giá bán cho cửa hàng / Shipping Bin.")]
    [SerializeField, Min(0)] private int sellPrice = 0;

    [Tooltip("Item này có được phép bán không?")]
    [SerializeField] private bool canSell = true;

    #region Public Properties (Encapsulation)

    public string ItemID => itemID;
    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemType ItemType => itemType;
    public int MaxStack => maxStack;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
    public bool CanSell => canSell;

    #endregion

    #region Editor Validation

    private void OnValidate()
    {
        // Đảm bảo maxStack luôn tối thiểu là 1
        if (maxStack < 1)
        {
            maxStack = 1;
        }

        // Đảm bảo giá mua/bán không âm
        if (buyPrice < 0) buyPrice = 0;
        if (sellPrice < 0) sellPrice = 0;

        // Cảnh báo nếu itemID bị để trống trong Editor
        if (string.IsNullOrWhiteSpace(itemID))
        {
            itemID = name.ToLower().Replace(" ", "_");
        }
    }

    #endregion
}
