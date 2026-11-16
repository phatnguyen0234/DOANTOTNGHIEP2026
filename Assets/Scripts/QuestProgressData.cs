using System;

[Serializable]
public class QuestProgressData
{
    public enum QuestStatus
    {
        Locked,
        InProgress,
        ReadyToAccept,
        Completed
    }
    public string questId;
    public int amount;
    public QuestStatus status;
}