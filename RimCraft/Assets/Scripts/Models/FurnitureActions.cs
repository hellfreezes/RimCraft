using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

// Код этого класса нужно перевести полностью на LUA код. Чтобы потом закгружать его на лету в работающую программу
// и главное - дать возможность писать аддоны к игре

public class FurnitureActions {

    static FurnitureActions instance;
    Script myLuaScript;

    public static FurnitureActions Instance
    {
        get
        {
            return instance;
        }
    }

    public FurnitureActions(string rawLuaCode)
    {
        instance = this;

        // Tell the LUA interpreter system to load all the classes
        // that we have marked as [MoonSharpUserData]
        UserData.RegisterAssembly();

        myLuaScript = new Script();

        // If we want to be able to instantiate a new object of a class
        //   i.e. by doing    SomeClass.__new()
        // We need to make the base type visible.
        myLuaScript.Globals["Inventory"] = typeof(Inventory);
        myLuaScript.Globals["Job"] = typeof(Job);

        // Also to access statics/globals
        myLuaScript.Globals["World"] = typeof(World);

        //ActivateRemoteDebugger(myLuaScript);
        myLuaScript.DoString(rawLuaCode);
    }

    public void CallFunctionsWithFurniture(string[] functionNames, Furniture furn, float deltaTime)
    {
        foreach (string functionName in functionNames)
        {
            object func = myLuaScript.Globals[functionName];

            if (func == null)
            {
                Debug.LogError(functionName + " не является LUA функцией");
                return;
            }

            DynValue result = myLuaScript.Call(func, furn, deltaTime);
            if (result.Type == DataType.String)
                Debug.Log(result.String);
        }
    }

    public DynValue CallFunction(string functionName, params object[] args)
    {
        object func = myLuaScript.Globals[functionName];
        if (func == null)
        {
            Debug.LogError(functionName + " не является LUA функцией");
            return null;
        }

        return myLuaScript.Call(func, args);
    }


    public static void JobComplete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFurnitureJob = null;
    }
}


/*
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
            furn.CancelJobs();
            return;
        }

        if (furn.tile.inventory != null && furn.tile.inventory.stackSize == 0)
        {
            Debug.LogError("В зоне хранение есть стак число предметов которого равно нулю! Где то ошибка. Такой стак должен был быть уничтожен.");
            furn.CancelJobs();
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
        j.furniture = furn;

        furn.AddJob(j);
    }

    static void Stockpile_JobWorked(Job j)
    {
        j.CancelJob(); //RemoveJob(j); // <----- тут может быть косяк в RemoveJob
        // TODO: следующий код необходимо изменить как только я пойму каким образом говорить о всех типах предметов которые можно принести на эту зону
        //       одной строкой. Пока это цикл

        foreach (Inventory inv in j.inventoryRequirements.Values)
        {
            if (inv.stackSize > 0)
            {
                World.current.inventoryManager.PlaceInventory(j.tile, inv);
                return;
            }
        }
    }

    // Что делает генератор кислорода
    public static void OxygenGenerator_UpdateAction(Furniture furn, float deltaTime)
    {
        if (furn.tile.room.GetGasAmount("O2") < 0.20f)
        {// Ограничение давления кислорода в комнате

            //TODO: изменить скорость заполнения учитывая размеры комнаты

            furn.tile.room.ChangeGas("O2", 0.01f * deltaTime);

            //TODO: потреблять электричество в процессе работы!
        } else
        {
            // Нужно ли потреблять электричество в простое?
        }
    }

    public static void MiningDroneStation_UpdateAction(Furniture furn, float deltaTime)
    {
        Tile spawnSpot = furn.GetSpawnSpotTile();

        if (furn.JobCount() > 0)
        {
            // У сооружения уже есть задание

            // Проверить не заполнен ли склад производимого продукта у данного сооружения

            if (spawnSpot.inventory != null && spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize)
            {
                // Остановить работу т.к. мы больше не можем складировать сюда
                furn.CancelJobs();
            }

            return;
        }

        // Если мы добрались до этой точки, текущей работы нет
        if (spawnSpot.inventory != null && spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize)
        {
            // Минисклад полон. Не создавать новую работу!
            return;
        }

        // Создаем новую работу

        Tile jobSpot = furn.GetJobSpotTile();
    

        if (jobSpot.inventory != null && jobSpot.inventory.stackSize >= jobSpot.inventory.maxStackSize)
        {
            // Место сброса генерируемого инвентаря переполнено
            return;
        }

        Job j = new Job(
            jobSpot,
            null,
            MiningDroneStation_JobComplete,
            1f,
            null,
            true
            );

        furn.AddJob(j);
    }

    public static void MiningDroneStation_JobComplete(Job j)
    {
        World.current.inventoryManager.PlaceInventory(j.furniture.GetSpawnSpotTile(), new Inventory("Steel Plate", 50, 20));
    }
}
*/