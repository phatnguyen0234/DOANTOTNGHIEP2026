using UnityEngine;
using TMPro;

public class UITooltipManager : MonoBehaviour
{
    public static UITooltipManager Instance { get; private set; }

    [Header("References")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private RectTransform rectTransform;

    private void Awake()
    {
        Instance = this;
        if (tooltipPanel != null)
        {
            rectTransform = tooltipPanel.GetComponent<RectTransform>();
            HideTooltip();
        }
    }

    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            rectTransform.position = Input.mousePosition + new Vector3(15, -15, 0);
        }
    }

    public void ShowTooltip(string itemName)
    {
        if (tooltipPanel == null || string.IsNullOrEmpty(itemName)) return;

        tooltipText.text = itemName;
        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}