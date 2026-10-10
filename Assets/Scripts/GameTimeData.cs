using System;
using UnityEngine;

[Serializable]
public class GameTimeData 
{
    public float currentHour;
    public int currentDay;
    public Season currentSeason;

    public GameTimeData(float currentHour, int currentDay, Season currentSeason)
    {
        this.currentHour = currentHour;
        this.currentDay = currentDay;
        this.currentSeason = currentSeason;
    }
}


