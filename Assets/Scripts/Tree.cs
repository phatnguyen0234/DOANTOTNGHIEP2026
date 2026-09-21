using System.Collections;
using UnityEngine;

// Đại diện cho một cây trong game có thể bị chặt bằng Rìu.
// Hiện thực IDropSource để tự động kích hoạt rơi gỗ và hạt giống thông qua DropSystem khi bị đốn hạ.
public class Tree : MonoBehaviour, IDropSource
{
    [Header("Drop Configuration")]
    [Tooltip("Bảng tỉ lệ rơi vật phẩm khi cây bị chặt đổ.")]
    [SerializeField] private DropTable dropTable;

    [Header("Chopping Settings")]
    [Tooltip("Số nhát chặt cần thiết để đốn hạ cây.")]
    [SerializeField, Min(1)] private int maxHits = 3;

    [Tooltip("Hiệu ứng hạt lá rụng khi bị chặt (tùy chọn).")]
    [SerializeField] private ParticleSystem leafParticles;

    private int currentHits = 0;
    private bool isFelled = false;
    private Coroutine shakeCoroutine;

    public int RemainingHits => Mathf.Max(0, maxHits - currentHits);
    public bool IsFelled => isFelled;

    // Xử lý khi cây nhận một nhát chém từ Rìu
    public void Hit(GameObject player = null)
    {
        if (isFelled) return;

        currentHits++;

        if (leafParticles != null)
        {
            leafParticles.Play();
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(Shake());

        // Nếu đã đủ số nhát chặt -> Đốn hạ cây và kích hoạt rơi vật phẩm
        if (currentHits >= maxHits)
        {
            FellTree(player);
        }
    }

    private void FellTree(GameObject player)
    {
        isFelled = true;

        // Xây dựng ngữ cảnh rơi
        DropContext context = new DropContext(
            source: gameObject,
            player: player,
            tool: null,
            toolLevel: 1,
            dropMultiplier: 1f,
            luck: 0f
        );

        // Kích hoạt hệ thống rơi vật phẩm
        DropSystem.TriggerDrop(this, context);

        // Hủy GameObject cây sau một khoảnh khắc nhỏ để hoàn tất frame
        Destroy(gameObject, 0.1f);
    }

    public IEnumerator Shake()
    {
        Vector3 pos = transform.position;
        transform.position += new Vector3(0.15f, 0f, 0f);
        yield return new WaitForSeconds(0.05f);
        transform.position -= new Vector3(0.3f, 0f, 0f);
        yield return new WaitForSeconds(0.05f);
        transform.position = pos;
    }

    #region IDropSource Implementation

    public DropTable GetDropTable()
    {
        if (dropTable == null)
        {
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("DropTable_Tree t:DropTable");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                dropTable = UnityEditor.AssetDatabase.LoadAssetAtPath<DropTable>(path);
            }
#endif
        }
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

#if UNITY_EDITOR
    private void Reset()
    {
        if (dropTable == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("DropTable_Tree t:DropTable");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                dropTable = UnityEditor.AssetDatabase.LoadAssetAtPath<DropTable>(path);
            }
        }
    }
#endif
}
