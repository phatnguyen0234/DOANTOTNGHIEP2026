using UnityEngine;
using UnityEngine.EventSystems;

public class ItemTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public ItemData currentItem;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && UITooltipManager.Instance != null)
        {
            UITooltipManager.Instance.ShowTooltip(currentItem.ItemName);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (UITooltipManager.Instance != null)
        {
            UITooltipManager.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        if (UITooltipManager.Instance != null) UITooltipManager.Instance.HideTooltip();
    }
}