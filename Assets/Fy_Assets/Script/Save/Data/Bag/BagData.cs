using Inventory.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class BagData
{
    public List<InventoryItem> bagContent;
    public BagData()
    {
        this.bagContent = new List<InventoryItem>();
    }
}
