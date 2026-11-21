using UnityEngine;

// Đại diện cho đá/tảng đá trong game có thể đập vỡ bằng Cuốc chim (Pickaxe).
// Hiện thực IDropSource để tự động kích hoạt rơi đá/quặng thông qua DropSystem khi bị đập vỡ.
public class Rock : MonoBehaviour, IDropSource
{
    [Header("Drop Configuration")]
    [Tooltip("Bảng tỉ lệ rơi vật phẩm khi đá bị đập vỡ.")]
    [SerializeField] private DropTable dropTable;

    private bool isBroken = false;

    // Khởi tạo trạng thái khi load game (không kích hoạt rơi vật phẩm)
    public void Init(bool isDestroyed)
    {
        isBroken = isDestroyed;
        if (isDestroyed)
        {
            Destroy(gameObject);
        }
    }

    public void Hit(GameObject player = null)
    {
        if (isBroken) return;
        isBroken = true;

        DropContext context = new DropContext(
            source: gameObject,
            player: player,
            tool: null,
            toolLevel: 1,
            dropMultiplier: 1f,
            luck: 0f
        );

        DropSystem.TriggerDrop(this, context);
        Destroy(gameObject);
    }

    #region IDropSource Implementation

    public DropTable GetDropTable()
    {
        return dropTable;
    }

    public Vector3 GetDropPosition()
    {
        return transform.position;
    }

    public DropContext GetDropContext()
    {
        return new DropContext(gameObject, null, null, 1, 1f, 0f);
    }

    #endregion
}
