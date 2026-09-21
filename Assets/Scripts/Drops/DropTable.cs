using System.Collections.Generic;
using UnityEngine;

// ScriptableObject định nghĩa bảng rơi vật phẩm (DropTable) cho các đối tượng trong game.
// Cây trồng, cây cối, đá, quái vật, thùng rương đều có thể gán bảng DropTable riêng biệt.
[CreateAssetMenu(fileName = "NewDropTable", menuName = "Farming/Drop Table")]
public class DropTable : ScriptableObject
{
    [Tooltip("Danh sách các mục vật phẩm có thể rơi từ nguồn này.")]
    [SerializeField] private List<DropEntry> entries = new List<DropEntry>();

    public IReadOnlyList<DropEntry> Entries => entries;

    // Sinh danh sách vật phẩm thực tế theo ngữ cảnh chỉ định
    public List<ItemDropResult> GenerateDrops(DropContext context = default)
    {
        return DropGenerator.Generate(this, context);
    }
}
