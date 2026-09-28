using System;
using TMPro;
using UnityEditor.Search;
using UnityEngine;

public class FarmStore : MonoBehaviour
{
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI textMeshProGui;
    [SerializeField] private GameObject shopUI;

    string[] messages = new string[] { "xin chao, rat vui duoc gap ban", "Hom nay, ban muon mua gi nao?" };


    private bool isMessage = false;
    private int currentMessage = 0;

    private void Awake()
    {
        messagePanel.SetActive(false);
        shopUI.SetActive(false);
    }

    private void Update()
    {
        if(isMessage && Input.GetMouseButtonDown(0))
        {
            currentMessage++;
            Show();
        }
    }

    private void Show()
    {
        if (currentMessage < messages.Length) textMeshProGui.text = messages[currentMessage];
        else
        {
            messagePanel.SetActive(false);
            shopUI.SetActive(true);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.CompareTag("Player") && !isMessage)
        {
            isMessage = true;
            messagePanel.SetActive(true);
            textMeshProGui.text = messages[0];
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            messagePanel.SetActive(false);
            shopUI.SetActive(false);
        }
    }
}
