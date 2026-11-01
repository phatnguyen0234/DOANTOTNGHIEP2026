using System;
using UnityEngine;

[Serializable]
public class PlayerData 
{
    public Vector3 position;
    public Vector2 lastDirection;
    public float currentStamina;
    public float maxStamina;
    public PlayerData(Vector3 position, Vector2 lastDirection, float currentStamina, float maxStamina)
    {
        this.position = position;
        this.lastDirection = lastDirection;
        this.currentStamina = currentStamina;
        this.maxStamina = maxStamina;
    }
}
