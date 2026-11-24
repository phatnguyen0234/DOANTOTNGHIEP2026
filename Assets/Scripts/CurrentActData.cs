using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CurrentActData 
{
    public int currentActNumber;
    //TODO: có cần biến để check xem đã hiện thông báo ! chưa ko nhỉ?
    public List<QuestProgressData> currentQuest = new(); // chỗ này hiện tại đang là quest đơn, có cần list các quest như này ko nhỉ?
    public bool hasShownIntro;
    public bool isAllActsCompleted;
}


