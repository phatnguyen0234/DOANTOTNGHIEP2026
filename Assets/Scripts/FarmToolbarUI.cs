using UnityEngine;
using UnityEngine.UI;

public class FarmToolbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FarmInputController farmInputController;

    [Header("Slot background images")]
    [SerializeField] private Image hoeSlot;
    [SerializeField] private Image seedSlot;
    [SerializeField] private Image waterSlot;
    [SerializeField] private Image harvestSlot;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.75f, 0.25f);

    private void Update()
    {
        RefreshSelection();
    }

    public void SelectHoe()
    {
        farmInputController.SelectTool(FarmTool.Hoe);
    }

    public void SelectSeed()
    {
        farmInputController.SelectTool(FarmTool.Seed);
    }

    public void SelectWater()
    {
        farmInputController.SelectTool(FarmTool.Water);
    }

    public void SelectHarvest()
    {
        farmInputController.SelectTool(FarmTool.Harvest);
    }

    private void RefreshSelection()
    {
        FarmTool selectedTool = farmInputController.SelectedTool;

        SetSlotColor(hoeSlot, selectedTool == FarmTool.Hoe);
        SetSlotColor(seedSlot, selectedTool == FarmTool.Seed);
        SetSlotColor(waterSlot, selectedTool == FarmTool.Water);
        SetSlotColor(harvestSlot, selectedTool == FarmTool.Harvest);
    }

    private void SetSlotColor(Image slot, bool isSelected)
    {
        slot.color = isSelected ? selectedColor : normalColor;
    }
}