using System;
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    public enum state
    {
        normal,
        exhausted,
        passout
    }

    [SerializeField] private float max = 270f;
    private float currentStamina;
    private state statePlayer;
    public static PlayerStamina Instance;
    public static event Action<float, float> onStaminaChanged;
    public static event Action<state> onStateStaminaChanged;

    private void Awake()
    {
        Instance = this;
        currentStamina = max;
        onStaminaChanged?.Invoke(currentStamina, max);
    }

    private void Start()
    {
        TimeManager.Instance.onNewDay += NewDay;
    }

    private void OnDisable()
    {
        TimeManager.Instance.onNewDay -= NewDay;
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina -= amount;
        onStaminaChanged?.Invoke(currentStamina, max);
        if(currentStamina <= -15f)
        {
            statePlayer = state.passout;
            TimeManager.GoToSleep();
        }
        else if(currentStamina <= 0f)
        {
            statePlayer = state.exhausted;
            onStateStaminaChanged?.Invoke(statePlayer);
        }
    }

    public void NewDay()
    {
        if (statePlayer == state.passout) currentStamina = max * 0.5f;
        else currentStamina = max;
        statePlayer = state.normal;
        onStateStaminaChanged?.Invoke(statePlayer);
        onStaminaChanged?.Invoke(currentStamina, max);
    }
}
