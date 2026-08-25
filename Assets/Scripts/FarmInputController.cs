using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

public class FarmInputController : MonoBehaviour
{
    [SerializeField] private FarmManager farmManager;
    [SerializeField] private GridCursor gridCursor;

    public FarmTool SelectedTool { get; private set; } = FarmTool.None;

    private void Update()
    {
        HandleToolUse();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SelectTool(FarmTool.None);
        }
    }


    public void SelectTool(FarmTool tool)
    {
        SelectedTool = tool;

        // Chỉ khi có tool thì mới bật highlight và xoay theo chuột.
        gridCursor.SetInteractionEnabled(tool != FarmTool.None);

        Debug.Log("Selected tool: " + SelectedTool);
    }

    private void HandleToolUse()
    {
        if (SelectedTool == FarmTool.None)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (!gridCursor.HasValidTarget)
            return;

        bool success = false;
        string message;

        switch (SelectedTool)
        {
            case FarmTool.Hoe:
                success = farmManager.Hoe(gridCursor.CurrentCell, out message);
                break;

            case FarmTool.Seed:
                success = farmManager.Plant(gridCursor.CurrentCell, out message);
                break;

            case FarmTool.Water:
                success = farmManager.Water(gridCursor.CurrentCell, out message);
                break;

            case FarmTool.Harvest:
                success = farmManager.Harvest(gridCursor.CurrentCell, out message);
                break;

            default:
                return;
        }

        Debug.Log(success ? "SUCCESS: " + message : "REJECTED: " + message);
    }

}