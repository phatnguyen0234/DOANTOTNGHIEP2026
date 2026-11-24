using System;
using System.Collections.Generic;
using UnityEngine;

// Dữ liệu chăn nuôi được lưu trong save.json (SaveData.ranchData), cùng cách với FarmData của phần trồng trọt.
[Serializable]
public class RanchData
{
    public int nextAnimalId = 1;
    public bool starterAnimalsGiven;
    public List<AnimalRecord> animals = new List<AnimalRecord>();
}

[Serializable]
public class AnimalRecord
{
    public int id;
    public string speciesId;
    public int variant;
    public string houseId;            // chuồng đang ở ("Coop", "Barn")
    public int friendship;            // 0..1000, 200 = 1 tim
    public int ageDays;               // chỉ tăng vào những ngày được cho ăn
    public bool fedToday;             // hôm nay đã được cho ăn chưa (sang ngày mới lại đói)
    public int daysHungry;            // số ngày liên tiếp bị bỏ đói (không chết, chỉ để theo dõi)
    public int fedDays;               // số lần được ăn kể từ lần cho sản phẩm trước
    public bool productReady;
    public bool productLarge;
    public bool pettedToday;
    public Vector2 position;
}
