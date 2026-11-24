using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeDisplaySlot
{
    public GameObject slotRoot;
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public ItemTooltipTrigger tooltipTrigger;
}

public class RecipeEntryUI : MonoBehaviour
{
    [Header("Result UI")]
    public TextMeshProUGUI resultNameText;
    public Image resultIcon;
    public TextMeshProUGUI resultAmountText;

    [Header("Grid Configuration")]
    [Tooltip("3x3 ingredient grid container.")]
    public Transform gridContainer;

    [Tooltip("Slot prefab used to render each cell in the 3x3 grid.")]
    public GameObject displaySlotPrefab;

    [Header("Wildcard Group Cycling")]
    [Tooltip("Seconds between icon transitions when displaying an IngredientGroup slot.")]
    [SerializeField] private float cycleInterval = 1.0f;

    private RecipeDisplaySlot[] gridSlots = new RecipeDisplaySlot[9];
    private CraftingRecipe currentRecipe;
    private bool isInitialized = false;
    private bool hasWildcardSlots = false;
    private float cycleTimer = 0f;
    private int cycleIndex = 0;

    private void Update()
    {
        if (!hasWildcardSlots || currentRecipe == null || cycleInterval <= 0f)
            return;

        cycleTimer += Time.unscaledDeltaTime;
        if (cycleTimer >= cycleInterval)
        {
            cycleTimer -= cycleInterval;
            cycleIndex++;
            RefreshWildcardSlots();
        }
    }

    private void OnDisable()
    {
        cycleTimer = 0f;
        cycleIndex = 0;
    }

    public void SetupDisplay(CraftingRecipe recipe)
    {
        currentRecipe = recipe;
        cycleTimer = 0f;
        cycleIndex = 0;
        hasWildcardSlots = false;

        if (!isInitialized)
        {
            InitializeSlots();
        }

        if (resultNameText != null)
        {
            resultNameText.text = recipe.resultItem.ItemName;
        }
        resultIcon.sprite = recipe.resultItem.Icon;
        if (resultAmountText != null)
        {
            resultAmountText.text = recipe.resultAmount > 1 ? recipe.resultAmount.ToString() : "";
        }

        ItemTooltipTrigger resultTrigger = resultIcon.GetComponent<ItemTooltipTrigger>();
        if (resultTrigger == null)
        {
            resultTrigger = resultIcon.gameObject.AddComponent<ItemTooltipTrigger>();
        }
        resultTrigger.SetTooltipData(recipe.resultItem);
        resultIcon.raycastTarget = true;

        for (int i = 0; i < 9; i++)
        {
            CraftingIngredient ingredient = recipe.requiredIngredients[i];
            RecipeDisplaySlot slot = gridSlots[i];

            slot.slotRoot.SetActive(true);

            if (ingredient.HasRequirement)
            {
                if (ingredient.IsWildcard)
                {
                    hasWildcardSlots = true;
                }

                ApplyIngredientVisual(slot, ingredient, cycleIndex);
            }
            else
            {
                if (slot.iconImage != null) slot.iconImage.gameObject.SetActive(false);
                if (slot.amountText != null) slot.amountText.gameObject.SetActive(false);

                if (slot.tooltipTrigger != null)
                {
                    slot.tooltipTrigger.SetTooltipData(null);
                }
            }
        }
    }

    private void RefreshWildcardSlots()
    {
        if (currentRecipe == null || currentRecipe.requiredIngredients == null)
            return;

        for (int i = 0; i < 9; i++)
        {
            CraftingIngredient ingredient = currentRecipe.requiredIngredients[i];
            if (ingredient.IsWildcard && gridSlots[i] != null)
            {
                ApplyIngredientVisual(gridSlots[i], ingredient, cycleIndex);
            }
        }
    }

    private static void ApplyIngredientVisual(RecipeDisplaySlot slot, CraftingIngredient ingredient, int currentCycle)
    {
        if (slot.iconImage != null)
        {
            slot.iconImage.gameObject.SetActive(true);
            slot.iconImage.sprite = ingredient.GetDisplayIcon(currentCycle);
            slot.iconImage.color = Color.white;
        }

        if (slot.amountText != null)
        {
            slot.amountText.gameObject.SetActive(true);
            slot.amountText.text = ingredient.amount > 1 ? ingredient.amount.ToString() : "";
        }

        if (slot.tooltipTrigger != null)
        {
            if (ingredient.IsWildcard)
            {
                slot.tooltipTrigger.SetTooltipData(
                    ingredient.GetDisplayItem(currentCycle),
                    ingredient.GetTooltipLabel(currentCycle)
                );
            }
            else
            {
                slot.tooltipTrigger.SetTooltipData(ingredient.item);
            }
        }
    }

    private void InitializeSlots()
    {
        if (gridContainer != null)
        {
            for (int i = gridContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = gridContainer.GetChild(i);
                if (child != null)
                {
                    child.gameObject.SetActive(false);

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        DestroyImmediate(child.gameObject);
                        continue;
                    }
#endif
                    Destroy(child.gameObject);
                }
            }
        }

        for (int i = 0; i < 9; i++)
        {
            GameObject slotObj = Instantiate(displaySlotPrefab, gridContainer);

            InventorySlotUI interactiveScript = slotObj.GetComponent<InventorySlotUI>();
            if (interactiveScript != null) Destroy(interactiveScript);

            RecipeDisplaySlot newSlot = new RecipeDisplaySlot();
            newSlot.slotRoot = slotObj;

            Transform iconTrans = slotObj.transform.Find("Icon");
            if (iconTrans != null)
            {
                newSlot.iconImage = iconTrans.GetComponent<Image>();
                newSlot.tooltipTrigger = iconTrans.gameObject.AddComponent<ItemTooltipTrigger>();

                if (newSlot.iconImage != null)
                {
                    newSlot.iconImage.raycastTarget = true;
                }
            }

            Transform amountTrans = slotObj.transform.Find("Amount Text")
                                 ?? slotObj.transform.Find("AmountText")
                                 ?? slotObj.transform.Find("Count");

            if (amountTrans != null) newSlot.amountText = amountTrans.GetComponent<TextMeshProUGUI>();

            gridSlots[i] = newSlot;
        }
        isInitialized = true;
    }
}