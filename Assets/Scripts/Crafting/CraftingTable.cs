using UnityEngine;

public class CraftingTable : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject craftingUIPanel;
    public Inventory playerInventory;

    // Called by your Interaction Controller (Right Click raycast)
    public void OpenCraftingTable()
    {
        craftingUIPanel.SetActive(true);

        if (playerInventory != null && !playerInventory.IsBagOpen)
        {
            playerInventory.OpenBag(); // Force open player bag to drag items
        }
    }

    public void CloseCraftingTable()
    {
        craftingUIPanel.SetActive(false);
    }
}