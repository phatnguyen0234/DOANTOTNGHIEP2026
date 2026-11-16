using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CurrentActData 
{
    public int currentActNumber;
    public List<QuestProgressData> currentQuest = new();
    public bool hasShownIntro;
    public bool isAllActsCompleted;
}


