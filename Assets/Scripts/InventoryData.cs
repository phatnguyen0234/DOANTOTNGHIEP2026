using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class InventoryData 
{
    public int capacity;
    public List<InventorySlotData> slots = new List<InventorySlotData>();
}
