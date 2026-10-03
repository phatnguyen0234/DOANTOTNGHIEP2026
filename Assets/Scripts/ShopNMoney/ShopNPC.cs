using System.Collections.Generic;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName;

    [Header("NPC's Inventory")]
    [SerializeField] private List<ShopItem> myShopItems = new List<ShopItem>();

    private bool isPlayerInRange = false;

    private void Update()
    {
        if (isPlayerInRange)
        {
            if(Input.GetKeyDown(KeyCode.F))
            {
                ShopManager.instance.OpenShop(myShopItems);
            }
            if(Input.GetKeyUp(KeyCode.F))
            {
                ShopManager.instance.CloseShop();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log($"F to buy from {npcName}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (ShopManager.instance != null)
            {
                ShopManager.instance.CloseShop();
            }
        }
    }
}