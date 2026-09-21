using UnityEngine;

// Struct truyền tải ngữ cảnh runtime khi phát sinh sự kiện rơi vật phẩm.
// Cho phép tính toán các hiệu ứng biến thiên như cấp công cụ, buff may mắn (Luck), hệ số nhân sản lượng (Multiplier).
public struct DropContext
{
    public GameObject source;
    public GameObject player;
    public ItemData tool;
    public int toolLevel;
    public float dropMultiplier;
    public float luck;

    public DropContext(GameObject source = null, GameObject player = null, ItemData tool = null, int toolLevel = 1, float dropMultiplier = 1f, float luck = 0f)
    {
        this.source = source;
        this.player = player;
        this.tool = tool;
        this.toolLevel = Mathf.Max(1, toolLevel);
        this.dropMultiplier = Mathf.Max(0.1f, dropMultiplier);
        this.luck = luck;
    }

    public static DropContext Default => new DropContext(null, null, null, 1, 1f, 0f);
}
