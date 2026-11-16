using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActData", menuName = "Act/ActData")]
public class ActData : ScriptableObject
{
    public int ActNumber;
    public string ActTitle;
    public string ActSubtitle;
    public float DisplayDuration;
    public List<QuestData> quest = new();
}
