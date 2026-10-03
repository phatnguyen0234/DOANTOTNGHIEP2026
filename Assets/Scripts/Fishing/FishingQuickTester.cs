using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script kiểm thử tạm thời thay thế luồng Minigame:
// Khi người chơi kích hoạt công cụ câu cá, nhân vật dừng lại 1 giây rồi tự động câu được cá và nhận thưởng vào Inventory.
public class FishingQuickTester : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Tham chiếu tới Inventory của người chơi.")]
    [SerializeField] private Inventory inventory;

    [Tooltip("Tham chiếu tới PlayerMovement để khóa/mở di chuyển.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Tham chiếu tới ItemDropSpawner khi túi đồ đầy.")]
    [SerializeField] private ItemDropSpawner itemDropSpawner;

    [Tooltip("Tham chiếu tới ResultUI (tùy chọn).")]
    [SerializeField] private FishingResultUI resultUI;

    [Header("Quick Test Settings")]
    [Tooltip("Thời gian chờ câu cá lên (giây).")]
    [SerializeField] private float fishingWaitDuration = 1.0f;

    [Tooltip("Danh sách cá có thể câu được trong chế độ test.")]
    [SerializeField] private List<FishData> testFishPool = new List<FishData>();

    [Tooltip("ItemData phần thưởng trực tiếp nếu chưa tạo FishData ScriptableObject.")]
    [SerializeField] private ItemData fallbackFishItem;

    [Tooltip("Số lượng cá nhận được mỗi lần câu.")]
    [SerializeField, Min(1)] private int rewardQuantity = 1;

    [Header("Debug Shortcut")]
    [Tooltip("Phím tắt để test câu cá nhanh bất cứ lúc nào (Mặc định: F).")]
    [SerializeField] private KeyCode testFishingKey = KeyCode.F;

    public bool IsFishing { get; private set; } = false;

    public static FishingQuickTester Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();
    }

    private void Update()
    {
        // Phím tắt test nhanh
        if (Input.GetKeyDown(testFishingKey) && !IsFishing)
        {
            StartQuickFishing();
        }
    }

    private void ResolveDependencies()
    {
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (itemDropSpawner == null) itemDropSpawner = FindAnyObjectByType<ItemDropSpawner>();
        if (resultUI == null) resultUI = FindAnyObjectByType<FishingResultUI>(FindObjectsInactive.Include);
    }

    // Bắt đầu quy trình câu cá test 1 giây
    public void StartQuickFishing(Action onFinished = null)
    {
        if (IsFishing) return;

        StopAllCoroutines();
        StartCoroutine(QuickFishingRoutine(onFinished));
    }

    private IEnumerator QuickFishingRoutine(Action onFinished)
    {
        IsFishing = true;
        Debug.Log("<color=#00D2FF>[FishingQuickTest] Bắt đầu quăng cần câu... Đang đợi 1 giây...</color>");

        // 1. Dừng người chơi trong 1 giây
        yield return new WaitForSeconds(fishingWaitDuration);

        // 2. Chọn cá từ pool hoặc dùng fallback
        FishData caughtFish = null;
        ItemData rewardItem = fallbackFishItem;
        string fishName = "Cá";
        int exp = 25;

        if (testFishPool != null && testFishPool.Count > 0)
        {
            int randIndex = UnityEngine.Random.Range(0, testFishPool.Count);
            caughtFish = testFishPool[randIndex];
            if (caughtFish != null)
            {
                fishName = caughtFish.FishName;
                rewardItem = caughtFish.RewardItemData ?? fallbackFishItem;
                exp = caughtFish.ExpReward;
            }
        }
        else if (rewardItem != null)
        {
            fishName = rewardItem.ItemName;
        }

        // 3. Thưởng cá vào Inventory
        if (rewardItem != null && inventory != null)
        {
            bool added = inventory.TryAddItem(rewardItem, rewardQuantity);
            if (added)
            {
                Debug.Log($"<color=#00FF66>[FishingQuickTest] Câu thành công! Đã thêm {rewardQuantity}x {fishName} vào Inventory. (+{exp} EXP)</color>");
            }
            else
            {
                Debug.LogWarning($"[FishingQuickTest] Túi đồ đã đầy! Đang drop {fishName} xuống đất.");
                if (itemDropSpawner != null && playerMovement != null)
                {
                    itemDropSpawner.SpawnDrop(rewardItem, rewardQuantity, playerMovement.transform.position);
                }
            }
        }
        else
        {
            Debug.Log($"<color=#00FF66>[FishingQuickTest] Câu thành công {fishName}! (Chưa gán RewardItemData)</color>");
        }

        // 4. Hiển thị thông báo ResultUI nếu có
        if (resultUI != null)
        {
            FishingResult result = new FishingResult(
                caughtFish,
                true,
                true,
                exp,
                rewardItem,
                rewardQuantity
            );
            resultUI.ShowResult(result);
        }

        // 5. Kết thúc câu cá, giải phóng người chơi
        IsFishing = false;
        onFinished?.Invoke();
    }
}
