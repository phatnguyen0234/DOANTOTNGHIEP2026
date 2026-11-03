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

    public float Max => max;
    public float CurrentStamina => currentStamina;
    public state StatePlayer => statePlayer;

    private void Awake()
    {
        Instance = this;
        currentStamina = max;
        statePlayer = state.normal;
    }

    public void LoadStamina(float current, float max)
    {
        this.max = max;
        currentStamina = current;
        onStaminaChanged?.Invoke(currentStamina, max);
    }

    public void ConsumeStamina(float amount)
    {
        //  Debug.Log(amount);
        if (statePlayer == state.passout || (PlayerMovement.instance != null && PlayerMovement.instance.isSleeping))
        {
            return;
        }
        currentStamina -= amount;
        onStaminaChanged?.Invoke(currentStamina, max);
        if(currentStamina <= -15f)
        {
            statePlayer = state.passout;
            UIManager.Instance.TriggerPassOutSleep();
        }
        else if(currentStamina <= 0f)
        {
            statePlayer = state.exhausted;
            onStateStaminaChanged?.Invoke(statePlayer);
        }
    }

    public void ResetStamina()
    {
        if (statePlayer == state.passout) currentStamina = max * 0.5f;
        else currentStamina = max;
        Debug.Log(statePlayer);
        Debug.Log(currentStamina);
        onStaminaChanged?.Invoke(currentStamina, max);
        statePlayer = state.normal;
        onStateStaminaChanged?.Invoke(statePlayer);
      //  onStaminaChanged?.Invoke(currentStamina, max);
    }

    //public void TriggerPassoutState()
    //{
    //    statePlayer = state.passout;
    //    onStateStaminaChanged?.Invoke(statePlayer);
    //    UIManager.Instance.TriggerPassOutSleep();
    //}
}
