using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string subTitle;
    public int amountRequired;
    public QuestType questType;
    public string targetId;
    public TriggerType triggerType;
    public string triggerId;
    public int rewardGold;
    public ItemData rewardItem;
    public int rewardItemAmount;
}
