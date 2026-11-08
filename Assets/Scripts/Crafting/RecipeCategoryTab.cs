using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeCategoryTab : MonoBehaviour
{
    public RecipeCategory tabCategory;
    public Button tabButton;

    [Header("Badge UI")]
    public GameObject badgeContainer;
    public TextMeshProUGUI badgeText;

    private void Awake()
    {
        if (tabButton == null)
            tabButton = GetComponent<Button>();
    }

    private void Start()
    {
        if (tabButton != null)
        {
            tabButton.onClick.AddListener(OnTabClicked);
        }

        UpdateBadge();
    }

    private void OnEnable()
    {
        if (PlayerRecipeManager.Instance != null)
            PlayerRecipeManager.Instance.OnUnseenRecipesChanged += UpdateBadge;

        UpdateBadge();
    }

    private void OnDisable()
    {
        if (PlayerRecipeManager.Instance != null)
            PlayerRecipeManager.Instance.OnUnseenRecipesChanged -= UpdateBadge;
    }

    public void UpdateBadge()
    {
        if (PlayerRecipeManager.Instance == null) return;

        int unseenCount = PlayerRecipeManager.Instance.GetUnseenCount(tabCategory);

        if (unseenCount > 0)
        {
            badgeContainer.SetActive(true);
            badgeText.text = unseenCount > 9 ? "9+" : unseenCount.ToString();
        }
        else
        {
            badgeContainer.SetActive(false);
        }
    }

    private void OnTabClicked()
    {
        if (PlayerRecipeManager.Instance != null)
        {
            PlayerRecipeManager.Instance.MarkCategoryAsSeen(tabCategory);
        }

        if (RecipeBookUI.Instance != null)
        {
            RecipeBookUI.Instance.ChangeCategory(tabCategory);
        }
    }
}