using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public ItemData currentItem;

    [HideInInspector]
    public string customTooltipText;

    private bool isPointerOver;

    public void SetTooltipData(ItemData item, string overrideText = null)
    {
        currentItem = item;
        customTooltipText = overrideText;

        if (isPointerOver)
        {
            RefreshTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        RefreshTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        if (UITooltipManager.Instance != null)
        {
            UITooltipManager.Instance.HideTooltip();
        }
    }

    private void RefreshTooltip()
    {
        if (UITooltipManager.Instance == null)
            return;

        string label = !string.IsNullOrEmpty(customTooltipText)
            ? customTooltipText
            : (currentItem != null ? currentItem.ItemName : string.Empty);

        if (!string.IsNullOrEmpty(label))
        {
            UITooltipManager.Instance.ShowTooltip(label);
        }
        else
        {
            UITooltipManager.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        isPointerOver = false;
        if (UITooltipManager.Instance != null)
        {
            UITooltipManager.Instance.HideTooltip();
        }
    }
}