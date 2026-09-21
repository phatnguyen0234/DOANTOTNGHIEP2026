using System.Collections.Generic;
using UnityEngine;

// Lớp điều phối tập trung cho toàn bộ hệ thống rơi vật phẩm (Item Drop System Facade).
// Cung cấp các API thuận tiện, giúp gameplay code (Axe, Hoe, Combat, Harvest...)
// chỉ cần gọi một dòng lệnh duy nhất để kích hoạt rơi vật phẩm.
public static class DropSystem
{
    // Kích hoạt rơi vật phẩm từ một đối tượng hiện thực IDropSource
    public static void TriggerDrop(IDropSource source, DropContext customContext = default)
    {
        if (source == null) return;

        DropTable table = source.GetDropTable();
        if (table == null) return;

        DropContext context = customContext;
        if (context.source == null)
        {
            context = source.GetDropContext();
        }

        Vector3 dropPosition = source.GetDropPosition();
        TriggerDrop(table, dropPosition, context);
    }

    // Kích hoạt rơi vật phẩm trực tiếp từ một DropTable tại vị trí chỉ định
    public static void TriggerDrop(DropTable table, Vector3 position, DropContext context = default)
    {
        if (table == null) return;

        List<ItemDropResult> drops = DropGenerator.Generate(table, context);
        if (drops.Count == 0) return;

        if (ItemDropSpawner.Instance != null)
        {
            ItemDropSpawner.Instance.SpawnDrops(drops, position);
        }
        else
        {
            Debug.LogWarning("[DropSystem] Chưa tìm thấy ItemDropSpawner trong Scene để sinh vật phẩm!");
        }
    }
}
