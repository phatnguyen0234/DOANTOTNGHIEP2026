using System.Collections.Generic;
using UnityEngine;

// Kết quả của một mục vật phẩm được tính toán rơi ra.
public struct ItemDropResult
{
    public ItemData item;
    public int amount;

    public ItemDropResult(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

// Lớp tĩnh chịu trách nhiệm tính toán xác suất và số lượng rơi vật phẩm từ DropTable.
public static class DropGenerator
{
    public static List<ItemDropResult> Generate(DropTable table, DropContext context = default)
    {
        List<ItemDropResult> results = new List<ItemDropResult>();

        if (table == null || table.Entries == null)
        {
            return results;
        }

        for (int i = 0; i < table.Entries.Count; i++)
        {
            DropEntry entry = table.Entries[i];
            if (entry == null || entry.Item == null)
            {
                continue;
            }

            // 1. Tính toán tỉ lệ rơi dựa trên dropChance và chỉ số Luck nếu được áp dụng
            float effectiveChance = entry.DropChance;
            if (entry.AffectedByLuck && context.luck > 0f)
            {
                // Mỗi điểm Luck cộng thêm 5% cơ hội thành công
                effectiveChance += context.luck * 0.05f;
            }
            effectiveChance = Mathf.Clamp01(effectiveChance);

            // Kiểm tra xúc xắc ngẫu nhiên (RNG)
            if (Random.value > effectiveChance)
            {
                continue;
            }

            // 2. Tính số lượng rơi cơ bản trong khoảng [minAmount, maxAmount]
            int amount = Random.Range(entry.MinAmount, entry.MaxAmount + 1);

            // 3. Áp dụng bổ trợ cấp bậc công cụ (Tool Level)
            if (entry.AffectedByToolLevel && context.toolLevel > 1)
            {
                // Cấp 2 +1, Cấp 3 +2...
                amount += (context.toolLevel - 1);
            }

            // 4. Áp dụng hệ số nhân sản lượng (Drop Multiplier)
            if (context.dropMultiplier > 0f && !Mathf.Approximately(context.dropMultiplier, 1f))
            {
                amount = Mathf.RoundToInt(amount * context.dropMultiplier);
            }

            if (amount > 0)
            {
                results.Add(new ItemDropResult(entry.Item, amount));
            }
        }

        return results;
    }
}
