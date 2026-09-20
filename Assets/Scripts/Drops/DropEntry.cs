using System;
using UnityEngine;

// Đại diện cho một mục cấu hình trong bảng rơi vật phẩm (DropTable).
// Định nghĩa loại vật phẩm, số lượng tối thiểu/tối đa và tỉ lệ xuất hiện.
[Serializable]
public class DropEntry
{
    [Tooltip("Vật phẩm ItemData sẽ rơi ra.")]
    [SerializeField] private ItemData item;

    [Tooltip("Số lượng tối thiểu rơi ra khi trúng tỉ lệ.")]
    [SerializeField, Min(1)] private int minAmount = 1;

    [Tooltip("Số lượng tối đa rơi ra khi trúng tỉ lệ.")]
    [SerializeField, Min(1)] private int maxAmount = 1;

    [Tooltip("Tỉ lệ phần trăm rơi ra (từ 0.0 = 0% đến 1.0 = 100%).")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

    [Tooltip("Tỉ lệ này có được cộng thêm bởi chỉ số may mắn (Luck) của người chơi không?")]
    [SerializeField] private bool affectedByLuck = false;

    [Tooltip("Số lượng rơi ra có được tăng thêm khi cấp độ công cụ cao hơn không?")]
    [SerializeField] private bool affectedByToolLevel = false;

    #region Properties

    public ItemData Item => item;
    public int MinAmount => minAmount;
    public int MaxAmount => maxAmount;
    public float DropChance => dropChance;
    public bool AffectedByLuck => affectedByLuck;
    public bool AffectedByToolLevel => affectedByToolLevel;

    #endregion

    public DropEntry() { }

    public DropEntry(ItemData item, int minAmount, int maxAmount, float dropChance)
    {
        this.item = item;
        this.minAmount = Mathf.Max(1, minAmount);
        this.maxAmount = Mathf.Max(this.minAmount, maxAmount);
        this.dropChance = Mathf.Clamp01(dropChance);
    }
}
