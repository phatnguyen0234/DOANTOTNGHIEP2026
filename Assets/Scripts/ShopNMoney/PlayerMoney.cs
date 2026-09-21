using System;
using UnityEngine;

public class PlayerMoney : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private int currentMoney = 500; 

    public event Action<int> OnMoneyChanged;

    public int CurrentMoney => currentMoney;

    private void Start()
    {
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0 || currentMoney < amount)
            return false;

        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }
}