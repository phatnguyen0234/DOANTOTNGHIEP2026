//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.Tilemaps;

//public class  GridCursor : MonoBehaviour
//{
//    [SerializeField] private Tilemap groundTileMap;
//    [SerializeField] private Tilemap highlightTileMap;
//    [SerializeField] private TileBase hightlightTile;
//    [SerializeField] private Transform player;
//    //[SerializeField] private FarmInputController farmInputController;


//    private void Update()
//    {
//        highlightTileMap.ClearAllTiles();

//        //if(farmInputController.CurrentTool == FarmTool.None)
//        //{
//        //    return;
//        //}
//        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
//        worldPos.z = 0f;

//        Vector3Int gridPos = groundTileMap.WorldToCell(worldPos);

//        Vector3Int playerGridPos = groundTileMap.WorldToCell(player.position);

//        if(Mathf.Abs(gridPos.x - playerGridPos.x) <= 1 && Mathf.Abs(gridPos.y - playerGridPos.y) <= 1 && gridPos != playerGridPos)
//        {
//            highlightTileMap.SetTile(gridPos, hightlightTile);
//        }
//    }
//}