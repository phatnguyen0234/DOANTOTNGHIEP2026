using UnityEngine;
using UnityEngine.EventSystems;
using BlitzyUI;

public class RecipeBookItemController : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("The exact ItemID of the Recipe Book ItemData")]
    [SerializeField] private string recipeBookItemID = "databook";

    [Tooltip("Direct reference to the RecipeBook ItemData (optional)")]
    [SerializeField] private ItemData recipeBookItemData;

    [Header("Hotbar Reference")]
    [Tooltip("Reference to HotbarController to check the currently selected slot")]
    [SerializeField] private HotbarController hotbarController;

    private void Awake()
    {
        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
        }
    }

    private void Update()
    {
        // Check for Right Mouse Button click
        if (Input.GetMouseButtonDown(1))
        {
            bool isBookOpen = false;
            if (UIManager.Instance != null && UIManager.Instance.GetScreen(ScreenIds.DataBook) != null)
            {
                isBookOpen = true;
            }
            else if (RecipeBookUI.Instance != null && RecipeBookUI.Instance.IsOpen)
            {
                isBookOpen = true;
            }

            // Ignore if clicking on UI elements unless the book is already open
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (!isBookOpen)
                    return;
            }

            if (IsHoldingRecipeBook())
            {
                if (UIManager.Instance != null)
                {
                    if (isBookOpen)
                    {
                        UIManager.Instance.QueuePopTo(ScreenIds.DataBook, include: true);
                        Debug.Log("<color=yellow>[RecipeBookItemController] Dong DataBook qua UIManager</color>");
                    }
                    else
                    {
                        UIManager.Instance.QueuePush(ScreenIds.DataBook);
                        Debug.Log("<color=green>[RecipeBookItemController] Mo DataBook qua UIManager</color>");
                    }
                }
                else if (RecipeBookUI.Instance != null)
                {
                    RecipeBookUI.Instance.ToggleBook();
                    Debug.Log("<color=green>[RecipeBookItemController] Bat/tat DataBook fallback</color>");
                }
            }
        }
    }

    private bool IsHoldingRecipeBook()
    {
        if (hotbarController == null)
        {
            hotbarController = FindAnyObjectByType<HotbarController>();
            if (hotbarController == null) return false;
        }

        InventorySlot activeSlot = hotbarController.SelectedSlot;
        if (activeSlot == null || activeSlot.IsEmpty() || activeSlot.ItemData == null)
            return false;

        if (recipeBookItemData != null && activeSlot.ItemData == recipeBookItemData)
            return true;

        return activeSlot.ItemData.ItemID == recipeBookItemID;
    }
}
