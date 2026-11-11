using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BlitzyUI;

[RequireComponent(typeof(Canvas))]
public class RecipeBookUI : BlitzyUI.Screen
{
    public static RecipeBookUI Instance { get; private set; }

    [Header("Slots Configuration (2 Pages = 6 Slots)")]
    [Tooltip("Drag the 6 RecipeEntry_UI instances from PageGrid_Container here")]
    public RecipeEntryUI[] entrySlots = new RecipeEntryUI[6];

    [Header("Search & Pagination UI")]
    public TMP_InputField searchInput;
    public TextMeshProUGUI pageText;
    public Button nextButton;
    public Button prevButton;
    public Button closeButton;

    [Header("Book Visibility Settings")]
    [Tooltip("Key to close the book when it is open")]
    public KeyCode closeKey = KeyCode.Escape;
    public bool startOpen = false;

    private List<CraftingRecipe> filteredRecipes = new List<CraftingRecipe>();
    private int currentSpreadIndex = 0;
    private const int RECIPES_PER_SPREAD = 6;
    private bool isEventsRegistered = false;

    public bool IsOpen => gameObject.activeSelf;

    #region Unity Lifecycle

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

        keepCached = true;
        RegisterEvents();

        if (PlayerRecipeManager.Instance != null)
        {
            PlayerRecipeManager.Instance.OnRecipeUnlocked += RefreshBook;
        }
    }

    private void Start()
    {
        if (UIManager.Instance == null)
        {
            if (!startOpen)
            {
                CloseBook();
            }
            else
            {
                OpenBook();
            }
        }
    }

    private void Update()
    {
        // Close the book when pressing Escape (only if user is not typing in the search bar)
        if (IsOpen && Input.GetKeyDown(closeKey))
        {
            if (searchInput != null && searchInput.isFocused)
            {
                searchInput.DeactivateInputField();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
            }
            else
            {
                CloseBook();
            }
        }
    }

    private void OnEnable()
    {
        RefreshBook();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (PlayerRecipeManager.Instance != null)
        {
            PlayerRecipeManager.Instance.OnRecipeUnlocked -= RefreshBook;
        }

        UnregisterEvents();
    }

    #endregion

    #region BlitzyUI.Screen Lifecycle

    public override void OnSetup()
    {
        Instance = this;
        keepCached = true;
        RegisterEvents();

        if (PlayerRecipeManager.Instance != null)
        {
            PlayerRecipeManager.Instance.OnRecipeUnlocked -= RefreshBook;
            PlayerRecipeManager.Instance.OnRecipeUnlocked += RefreshBook;
        }
    }

    public override void OnPush(Data data)
    {
        gameObject.SetActive(true);
        RefreshBook();

        // Bắt buộc gọi PushFinished để UIManager chuyển trạng thái Ready
        PushFinished();
    }

    public override void OnPop()
    {
        if (searchInput != null && searchInput.isFocused)
        {
            searchInput.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        gameObject.SetActive(false);

        // Bắt buộc gọi PopFinished để UIManager hoàn tất thao tác
        PopFinished();
    }

    public override void OnFocus()
    {
        RefreshBook();
    }

    public override void OnFocusLost()
    {
        if (searchInput != null && searchInput.isFocused)
        {
            searchInput.DeactivateInputField();
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }

    #endregion

    #region Event Handlers & Navigation

    private void RegisterEvents()
    {
        if (isEventsRegistered) return;

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBook);

        isEventsRegistered = true;
    }

    private void UnregisterEvents()
    {
        if (!isEventsRegistered) return;

        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextPage);

        if (prevButton != null)
            prevButton.onClick.RemoveListener(PrevPage);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseBook);

        isEventsRegistered = false;
    }

    // Toggles the visibility of the DataBook UI
    public void ToggleBook()
    {
        if (IsOpen) CloseBook();
        else OpenBook();
    }

    public void OpenBook()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePush(ScreenIds.DataBook);
        }
        else
        {
            gameObject.SetActive(true);
            RefreshBook();
        }
    }

    public void CloseBook()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.QueuePopTo(ScreenIds.DataBook, include: true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void RefreshBook()
    {
        if (PlayerRecipeManager.Instance == null) return;

        string keyword = searchInput != null ? searchInput.text.ToLower().Trim() : "";
        filteredRecipes.Clear();

        foreach (var recipe in PlayerRecipeManager.Instance.unlockedRecipes)
        {
            if (recipe == null || recipe.resultItem == null) continue;

            if (string.IsNullOrEmpty(keyword) || recipe.resultItem.ItemName.ToLower().Contains(keyword))
            {
                filteredRecipes.Add(recipe);
            }
        }

        currentSpreadIndex = Mathf.Clamp(currentSpreadIndex, 0, GetMaxSpreadIndex());
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int startIndex = currentSpreadIndex * RECIPES_PER_SPREAD;

        for (int i = 0; i < entrySlots.Length; i++)
        {
            if (entrySlots[i] == null) continue;

            int recipeIndex = startIndex + i;
            if (recipeIndex < filteredRecipes.Count)
            {
                entrySlots[i].gameObject.SetActive(true);
                entrySlots[i].SetupDisplay(filteredRecipes[recipeIndex]);
            }
            else
            {
                entrySlots[i].gameObject.SetActive(false);
            }
        }

        int leftPageNum = (currentSpreadIndex * 2) + 1;
        int rightPageNum = leftPageNum + 1;
        int totalPages = (GetMaxSpreadIndex() + 1) * 2;

        if (pageText != null)
        {
            pageText.text = $"Pages {leftPageNum}-{rightPageNum} / {totalPages}";
        }

        if (prevButton != null) prevButton.interactable = currentSpreadIndex > 0;
        if (nextButton != null) nextButton.interactable = currentSpreadIndex < GetMaxSpreadIndex();
    }

    private void OnSearchChanged(string text)
    {
        currentSpreadIndex = 0;
        RefreshBook();
    }

    public void NextPage()
    {
        if (currentSpreadIndex < GetMaxSpreadIndex())
        {
            currentSpreadIndex++;
            UpdateDisplay();
        }
    }

    public void PrevPage()
    {
        if (currentSpreadIndex > 0)
        {
            currentSpreadIndex--;
            UpdateDisplay();
        }
    }

    private int GetMaxSpreadIndex()
    {
        if (filteredRecipes.Count == 0) return 0;
        return (filteredRecipes.Count - 1) / RECIPES_PER_SPREAD;
    }

    #endregion
}
