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
        CleanupInventory(inv);

        // Создаем новый стак в тайле, если до этого он был пуст
        if (tileWasEmpty)
        {
            if (inventories.ContainsKey(tile.inventory.objectType) == false)
            {
                inventories[tile.inventory.objectType] = new List<Inventory>();
            } 
            inventories[tile.inventory.objectType].Add(inv);
        }


        return true;
    }

    /// <summary>
    /// Добавлять предмет в работу
    /// </summary>
    /// <param name="job"></param>
    /// <param name="inv"></param>
    /// <returns></returns>
    public bool PlaceInventory(Job job, Inventory inv)
    {
        if (job.inventoryRequirements.ContainsKey(inv.objectType) == false)
        { // Этот предмет для работы не нужен
            Debug.LogError("Попытка добавить предмет, который для этой работы ненужен");
            return false;
        }

        // Добавляем предмет
        job.inventoryRequirements[inv.objectType].stackSize += inv.stackSize;

        // Контролируем остатки прдметов в стаке
        if (job.inventoryRequirements[inv.objectType].maxStackSize < job.inventoryRequirements[inv.objectType].stackSize)
        { // Попытка положить предметов больше чем нужно
            // Берем сколько нужно, а остальное оставляем персонажу
            inv.stackSize = job.inventoryRequirements[inv.objectType].stackSize - job.inventoryRequirements[inv.objectType].maxStackSize;
            job.inventoryRequirements[inv.objectType].stackSize = job.inventoryRequirements[inv.objectType].maxStackSize;
        } else { 
            // Персонаж принес ровно столько, сколько нужно
            inv.stackSize = 0;
        }



        // В этом месте inv может быть пустым стаком если он объединен с другим стаком
        CleanupInventory(inv);

        return true;
    }


    public bool PlaceInventory(Character character, Inventory sourceInventory, int amount = -1)
    {
        if (amount < 0)
            amount = sourceInventory.stackSize;

        if (character.inventory == null)
        {
            character.inventory = sourceInventory.Clone();
            character.inventory.stackSize = 0;
            inventories[character.inventory.objectType].Add(character.inventory);
        } else if (character.inventory.objectType != sourceInventory.objectType)
        {
            Debug.LogError("Персонаж попытался поднять предметы которые не соответсвуют нужным для работы");
            return false;
        }

        // Добавляем предмет
        character.inventory.stackSize += amount;

        // Контролируем остатки прдметов в стаке
        if (character.inventory.maxStackSize < character.inventory.stackSize)
        { // Попытка положить предметов больше чем нужно
            // Берем сколько нужно, а остальное оставляем персонажу
            sourceInventory.stackSize = character.inventory.stackSize - character.inventory.maxStackSize;
            character.inventory.stackSize = character.inventory.maxStackSize;
        }
        else
        {
            // Персонаж принес ровно столько, сколько нужно
            sourceInventory.stackSize -= amount;
        }



        // В этом месте inv может быть пустым стаком если он объединен с другим стаком
        CleanupInventory(sourceInventory);

        return true;
    }

    void CleanupInventory(Inventory inv)
    {
        if (inv.stackSize == 0)
        {
            if (inventories.ContainsKey(inv.objectType))
            {
                inventories[inv.objectType].Remove(inv);
            }

            if (inv.tile != null)
            {
                inv.tile.inventory = null;
                inv.tile = null;
            }

            if (inv.character != null)
            {
                inv.character.inventory = null;
                inv.character = null;
            }
        } 
    }

    /// <summary>
    /// Находит стак необходимых предметов поблизости к Tile t
    /// </summary>
    /// <param name="objectType">Тип предметов</param>
    /// <param name="t">Тайл от которого искать ближайшие</param>
    /// <param name="desiredAmount">Необходимое количество</param>
    /// <returns>Если поблизости нет необходимого количества, то возвращает самый большой стак из найденных</returns>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount)
    {
        //FIXME: 
        // 1) Метод на самом деле выдает не самый ближайший предмет
        // 2) Пока что нет способа выдать ближайший предмет. Нужно уточнять наш inventories

        if (inventories.ContainsKey(objectType) == false)
        { // Предмет в мире не найден
            Debug.LogError("В мире нет предмета " + objectType);
            return null;
        }


        // Перебирает все стаки на сцене и выдает первый попавшийся соответствующий критериям
        foreach (Inventory inv in inventories[objectType])
        {
            if (inv.tile != null)
            {
                return inv;
            }
        }

        return null;
    }
}
