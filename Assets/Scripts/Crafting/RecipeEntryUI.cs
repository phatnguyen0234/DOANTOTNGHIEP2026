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
    [Tooltip("Ma tran 3x3")]
    public Transform gridContainer;

    [Tooltip("Slot Prefab")]
    public GameObject displaySlotPrefab;

    private RecipeDisplaySlot[] gridSlots = new RecipeDisplaySlot[9];
    private bool isInitialized = false;

    public void SetupDisplay(CraftingRecipe recipe)
    {
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
        resultTrigger.currentItem = recipe.resultItem;
        resultIcon.raycastTarget = true;

        for (int i = 0; i < 9; i++)
        {
            CraftingIngredient ingredient = recipe.requiredIngredients[i];
            RecipeDisplaySlot slot = gridSlots[i];

            slot.slotRoot.SetActive(true);

            if (ingredient.item != null && ingredient.amount > 0)
            {
                slot.iconImage.gameObject.SetActive(true);
                slot.iconImage.sprite = ingredient.item.Icon;
                slot.iconImage.color = Color.white;

                if (slot.amountText != null)
                {
                    slot.amountText.gameObject.SetActive(true);
                    slot.amountText.text = ingredient.amount > 1 ? ingredient.amount.ToString() : "";
                }

                if (slot.tooltipTrigger != null)
                {
                    slot.tooltipTrigger.currentItem = ingredient.item;
                }
            }
            else
            {
                if (slot.iconImage != null) slot.iconImage.gameObject.SetActive(false);
                if (slot.amountText != null) slot.amountText.gameObject.SetActive(false);

                if (slot.tooltipTrigger != null)
                {
                    slot.tooltipTrigger.currentItem = null;
                }
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