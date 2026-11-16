using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/QuestData")]
public class QuestData : ScriptableObject
{
    //TODO: id
    public string questId;
    //TODO: subtile
    public string subTitle;
    //TDOO: so luong yeu cau
    public int amountRequired;
    //TODO: QuestType
    public QuestType questType;
    //TODO: targetId
    public string targetId;
    //TODO: TriggerType
    public TriggerType triggerType;
    //TODO: triggerID
    public string triggerId;
    //TODO: thưởng chia ra làm item và vàng

}
