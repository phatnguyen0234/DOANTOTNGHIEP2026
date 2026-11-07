using System.Collections.Generic;
using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName;

    [Header("NPC's Inventory")]
    [SerializeField] private List<ShopItem> myShopItems = new();

    private bool isPlayerInRange;

    private void Update()
    {
        if (!isPlayerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            ShopManager shopManager = ShopManager.instance;

            if (shopManager == null)
                return;

            if (shopManager.IsShopOpen &&
                shopManager.CurrentShopNpc == this)
            {
                shopManager.CloseShop();
            }
            else
            {
                shopManager.OpenShop(this, myShopItems);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = true;

        Debug.Log($"Press F to buy from {npcName}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        isPlayerInRange = false;

        ShopManager shopManager = ShopManager.instance;

        if (shopManager == null)
            return;

        if (shopManager.CurrentShopNpc == this)
        {
            shopManager.CloseShop();
        }
    }
}