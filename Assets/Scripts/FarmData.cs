using System;
using System.Collections.Generic;

[Serializable]
public class FarmData
{
    public string farmId;
    public string playerId;

    // List để sau này dễ save/load JSON.
    public List<FarmCell> cells = new List<FarmCell>();
}