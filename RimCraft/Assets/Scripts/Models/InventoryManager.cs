using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager {

    /// <summary>
    /// Словарь содержащий List. Для того, чтобы иметь доступ ко всем типам инвентаря и к каждому инвенторю 
    /// через элемент листа, который находится по ключевому string. А ключ - это objectType
    /// </summary>
    public Dictionary<string, List<Inventory>> inventories;

    public InventoryManager()
    {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    public bool PlaceInventory(Tile tile, Inventory inv)
    {
        bool tileWasEmpty = tile.inventory == null; // Проверяем пуст ли тайл до назначений ниже

        if (tile.PlaceInvenory(inv) == false)
        {
            // По какой то причине тайл не принимает предмет, дальше продолжать нет смысла
            return false;
        }

        // В этом месте inv может быть пустым стаком если он объединен с другим стаком
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(tile.inventory.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }
        }

        // Создаем новый стак в тайле, если до этого он был пуст
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.inventory.objectType) == false)
            {
                inventories[tile.inventory.objectType] = new List<Inventory>();
            } 
            inventories[tile.inventory.objectType].Add(tile.inventory);
        }


        return true;
    }
}
