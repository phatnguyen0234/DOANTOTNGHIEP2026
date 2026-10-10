using System;
using UnityEngine;

[Serializable]
public class PlayerData 
{
    public Vector3 position;

    public PlayerData(Vector3 position)
    {
        this.position = position;
    }
}
