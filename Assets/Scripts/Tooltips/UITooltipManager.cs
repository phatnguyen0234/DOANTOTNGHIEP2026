using UnityEngine;
using TMPro;

public class UITooltipManager : MonoBehaviour
{
    public static UITooltipManager Instance { get; private set; }

    [Header("References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Sorting")]
    [SerializeField] private int tooltipSortingOrder = 100;

    private RectTransform rectTransform;
    private Canvas tooltipCanvas;

    private void Awake()
    {
        Instance = this;
        if (tooltipPanel != null)
        {
            rectTransform = tooltipPanel.GetComponent<RectTransform>();
            tooltipCanvas = tooltipPanel.GetComponent<Canvas>();
            if (tooltipCanvas == null)
            {
                tooltipCanvas = tooltipPanel.AddComponent<Canvas>();
            }
            HideTooltip();
        }
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            rectTransform.position = Input.mousePosition + new Vector3(15, -15, 0);
        }
    }

    public void ShowTooltip(string itemName)
    {
        if (tooltipPanel == null || string.IsNullOrEmpty(itemName)) return;

        tooltipText.text = itemName;
        tooltipPanel.SetActive(true);

        // Set overrideSorting sau khi active để Unity áp dụng đúng sortingOrder lên mọi BlitzyUI.Screen
        if (tooltipCanvas != null)
        {
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = tooltipSortingOrder;
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}