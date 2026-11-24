using System;
using System.Collections.Generic;
using UnityEngine;

public class ActManager : MonoBehaviour
{
    //TODO: bien co de check luc new game
    //  [SerializeField] private List<ActData> actList = new();
    [SerializeField] private ActData data;
    public static ActManager instance;
    public static event Action<ActData> OnActStarted;
    //TODO: [SerializeField] private Vector2 position;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //TODO: duyệt qua danh sách các hồi => láy ActData dựa vào curentactdat.currentActnumber
        //TODO: check hashown => nếu false => hiện => bắn thông báo sang ActBannerUI để chạy chuyển màn
        //TODO: duyệt qua list các quest trong hồi => questdata dựa vào curentdata.curentquest
        //TODO: duyệt xem trạng thái đang là gì => hiện UI cho phù hợp, đây là trạng thái Locked, InProgress, ReadyToAccept, Completed, nhưng cái nào là để hiện ! và người chơi chưa bấm vào nhỉ
        OnActStarted?.Invoke(data);
        //TODO: PlayerMovement.instance.transform.position = postion;
    }

    private void Action()
    {
        //TODO: Khi người chơi thực hiện các hành động tilled,plant,water,harvest => hàm này sẽ nhận thông báo
        //TODO: Nó check xem curentfarmtool nhưng seed, harvest thì ko có; tôi định gửi cả curentfarmtool; check xem có trùng với questType ko? Nếu có thì amount ++;
        //TODO: nếu amount == questdat.amount => nhận thưởng nếu có; phát thông báo (vậy cái triggerid sẽ sử dụng như thế nào)
    }
}
