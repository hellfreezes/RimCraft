using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager {
    public List<Inventory> inventories;

    public InventoryManager()
    {
        inventories = new List<Inventory>();
    }

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        if (tile.PlaceInvenory(inv) == false)
        {
            // По какой то причине тайл не принимает предмет, дальше продолжать нет смысла
            return false;
        }

        // В этом месте inv может быть пустым стаком если он объединен с другим стаком
        if (inv.stackSize == 0)
        {
            inventories.Remove(inv);
        }

        // 
    }
}
