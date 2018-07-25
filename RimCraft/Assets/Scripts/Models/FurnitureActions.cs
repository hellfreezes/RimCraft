using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Код этого класса нужно перевести полностью на LUA код. Чтобы потом закгружать его на лету в работающую программу
// и главное - дать возможность писать аддоны к игре

public static class FurnitureActions {

    // Обновлялка двери, вызывается каждый тик
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        float openSpeed = 4;

        // Открывается ли дверь?
        if (furn.GetParameter("is_opening") >= 1)
        {

            // Выполняем открытие
            furn.ChangeParameter("openness", deltaTime * openSpeed);
            if (furn.GetParameter("openness") >= 1)
            {
                // Дверь открыта, нужно закрыть
                furn.SetParameter("is_opening",  0);
            }
        } else // Иначе нужно закрыть дверь
        {
            furn.ChangeParameter("openness", -deltaTime * openSpeed);
        }
        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        furn.cbOnChanged(furn);
    }

    // Возвращает можно ли зайти в эту дверь или нет
    public static Enterablylity Door_IsEnterable(Furniture furn)
    {
        // Можно зайти или нет зависит от того, открыта ли дверь полностью
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return Enterablylity.Yes; // Зайти можно
        }
        return Enterablylity.Soon; // Подождите дверь открывается
    }


    public static void JobComlete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFurnitureJob = null;
    }

    public static Inventory[] Stockpile_GetItemFromFilter()
    {
        // TODO: получать эти данные из UI системы. Это фильтр вещей для зоны хранения

        return new Inventory[1] { new Inventory("Steel Plate", 50, 0) };
    }

    public static void Stockpile_UpdateAction(Furniture furn, float deltaTime)
    {
        // Убедимся что создали одну из следующих работ:
        // 1) если зона пуста, то любой предмет должен быть перенесен в эту зону
        // 2) если что то в зоне есть, то если стаки не полностью заполнены, то донести такие же предметы в эти стаки
        
        // TODO: Данная функция не должна вызываться каждый фрейм. А лишь в след случаях:
        // 1) создан новый предмет
        // 2) предмет доставлен
        // 3) предмет поднят
        // 4) пользователь изменил фильтр хранящихся в зоне вещей через интерфейс

        if (furn.tile.inventory != null && furn.tile.inventory.stackSize >= furn.tile.inventory.maxStackSize)
        {
            //Стак заполнен
            furn.ClearJobs();
            return;
        }

        if (furn.tile.inventory != null && furn.tile.inventory.stackSize == 0)
        {
            Debug.LogError("В зоне хранение есть стак число предметов которого равно нулю! Где то ошибка. Такой стак должен был быть уничтожен.");
            furn.ClearJobs();
            return;
        }

        // Возможно что работа уже добавлена в очередь?
        if (furn.JobCount() > 0)
        {

            return;
        }

        // В этой точке мы знаем что в инвентаре зоны ничего не лежит, но и работа на переноску еще не добавлена в очередь

        // TODO: нужно сделать чтобы зона хранения могла быть не только 1 тайлом, но могла бы быть и множеством тайлов. Например зоной 10х10.

        Inventory[] itemsDesired;

        // в зоне ничего не лежит
        if (furn.tile.inventory == null)
        {
            // Зона пуста. Попросить принести сюда что-нибудь
            itemsDesired = Stockpile_GetItemFromFilter();
        } else
        {
            // У нас есть какой то неполный стак

            Inventory desInv = furn.tile.inventory.Clone();
            desInv.maxStackSize -= desInv.stackSize;
            desInv.stackSize = 0;

            itemsDesired = new Inventory[] { desInv };
        }

        Job j = new Job(
                furn.tile,
                null,
                null,
                0,
                itemsDesired);
        // TODO: пока что полный запрет на переноску вещей из одного хранилища в другое. ИСПРАВИТЬ. Ввести приоритеты
        j.canPickupFromStockpile = false;
        j.RegisterWorkedCallback(Stockpile_JobWorked);

        furn.AddJob(j);
    }

    static void Stockpile_JobWorked(Job j)
    {
        j.tile.furniture.ClearJobs();
        // TODO: следующий код необходимо изменить как только я пойму каким образом говорить о всех типах предметов которые можно принести на эту зону
        //       одной строкой. Пока это цикл

        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                j.tile.world.inventoryManager.PlaceInventory(j.tile, inv);
                return;
            }
        }
    }
}
