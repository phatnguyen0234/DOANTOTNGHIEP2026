using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CurrentActData 
{
    public int currentActNumber;
    //TODO: List <QuestProgressData>
    public List<QuestProgressData> currentQuest = new();
    //TODO: bool hasShownIntro;
    public bool hasShownIntro;
    //TODO: bool isAllActsCompleted
    public bool isAllActsCompleted;
}


