using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

/// <summary>
/// Этот класс содержит информацию о работе для персонажей
/// </summary>
[MoonSharpUserData]
public class Job {
    //Пока делаем чтобы установка фурнитуры совершалась по средствам этого класса

    public Tile tile;

    protected float jobTimeRequried;
    protected bool jobRepeats = false;

    public float jobTime { get; protected set; } // время необходимое для выполнение работы

    //FIXME: временное решение связывающее экземпляр работы конкретно с фурнитурой. 
    //А хотелось бы чтобы работа могла быть связана с разными объектами
    // Использовать Generic
    public string jobObjectType { get; protected set; }

    public bool acceptsAnyInventoryItem = false;
    public bool canPickupFromStockpile = true;

    public Furniture furniturePrototype;
    public Furniture furniture; // Сооружение, которое владеет данной работой

    // События которые расскажут всем подписчикам о том что происходит
    Action<Job> cbJobCompleted; // Событие вызываемое по звершению работы, работа прошла полный цикл
    Action<Job> cbJobStopped;  // Событие вызываемое если работа отменена или работа не повторяется
    Action<Job> cbJobWorked; // Работа выполняется (в процессе / прогресс)

    List<string> cbJobWorkedLua;
    List<string> cbJobCompletedLua;

    public Dictionary<string, Inventory> inventoryRequirements; // Необходимые для работы материалы


    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime, Inventory[] jobInventoryRequirements, bool jobRepeats = false)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobCompleted += cbJobComplete;
        this.jobTimeRequried = this.jobTime = jobTime;
        this.jobRepeats = jobRepeats;

        cbJobWorkedLua = new List<string>();
        cbJobCompletedLua = new List<string>();

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (jobInventoryRequirements != null)
        {
            foreach (Inventory inv in jobInventoryRequirements)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone(); // Клон потому, что нам нужна не ссылка на экземпляр, а новый экземпляр
            }
        }

    }

    protected Job(Job other)
    {
        this.tile = other.tile;
        this.jobObjectType = other.jobObjectType;
        this.cbJobCompleted = other.cbJobCompleted;
        this.jobTime = other.jobTime;

        cbJobWorkedLua = new List<string>(other.cbJobWorkedLua);
        cbJobCompletedLua = new List<string>(other.cbJobWorkedLua);

        this.inventoryRequirements = new Dictionary<string, Inventory>();
        if (other.inventoryRequirements != null)
        {
            foreach (Inventory inv in other.inventoryRequirements.Values)
            {
                this.inventoryRequirements[inv.objectType] = inv.Clone(); // Клон потому, что нам нужна не ссылка на экземпляр, а новый экземпляр
            }
        }
    }

    public virtual Job Clone()
    {
        return new Job(this);
    }

    /// <summary>
    /// Циклический метод, отвечает за процесс выполнения работы
    /// </summary>
    /// <param name="workTime">скорость выполнения работы</param>
    public void DoWork(float workTime)
    {
        // Предварительно проверить хватает ли нам всего для выполнения этой работы
        if (HasAllMaterial() == false)
        {
            //Debug.LogError("Попытка выполнять работы для которой не хватает материалов");

            // Работа не может быть завершена, т.к. не хватает материлов,
            // но он всё еще вызывает коллбэки чтобы показывать прогресс ее выполнения
            if (cbJobWorked != null)
                cbJobWorked(this);

            if (cbJobWorkedLua != null)
            {
                foreach (string luaFunction in cbJobWorkedLua)
                {
                    FurnitureActions.Instance.CallFunction(luaFunction, this);
                }
            }

            return;
        }

        jobTime -= workTime;

        //Debug.Log("Job work: " + jobTime);

        if (cbJobWorked != null)
            cbJobWorked(this);

        if (cbJobWorkedLua != null)
        {
            foreach (string luaFunction in cbJobWorkedLua)
            {
                FurnitureActions.Instance.CallFunction(luaFunction, this);
            }
        }

        if (jobTime <= 0)
        {
            //Debug.Log("Работа выполнена");
            if (cbJobCompleted != null)
            {
                // Происходит то, что должно произойти при завершении работы
                // Выполняем методы которые подписались
                cbJobCompleted(this);
            }

            foreach (string luaFunc in cbJobCompletedLua)
            {
                FurnitureActions.Instance.CallFunction(luaFunc, this);
            }

            if (jobRepeats == false)
            {
                if (cbJobStopped != null)
                    // Дать подписчикам знать что работы остановлены
                    cbJobStopped(this);
            } else
            {
                // Работа повторяемая, соответсвенно должна быть сброшена и начата заново
                jobTime += jobTimeRequried;
            }
        }

        //Debug.Log("Job work: " + jobTime);
    }

    /// <summary>
    /// Отменяем работу
    /// </summary>
    public void CancelJob()
    {
        if (cbJobStopped != null)
            cbJobStopped(this);

        World.current.jobQueue.Remove(this);
    }

    public void Abandon()
    {
        if (cbJobStopped != null)
            cbJobStopped(this);
    }

    /// <summary>
    /// Проверяет находится ли в тайле с этой работой весь необходимый нам материал
    /// </summary>
    /// <returns></returns>
    public bool HasAllMaterial()
    {
        foreach (Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Проверяем нужен ли нам конкретный материал для работы
    /// </summary>
    /// <param name="inv">Проверяемый материал</param>
    /// <returns>Сколько конкретно материала нужно. 0 если не нужен</returns>
    public int DesiresInventoryType(Inventory inv)
    {
        if (acceptsAnyInventoryItem == true)
        {
            return inv.maxStackSize;
        }

        if (inventoryRequirements.ContainsKey(inv.objectType) == false)
        {
            //Этот материал нам ненужен вообще
            return 0;
        }

        if (inventoryRequirements[inv.objectType].stackSize >= inventoryRequirements[inv.objectType].maxStackSize)
        {
            //Этого материала хватает для работы поэтому он больше не нужен
            return 0;
        }

        // Этот материал нам нужен.
        return inventoryRequirements[inv.objectType].maxStackSize - inventoryRequirements[inv.objectType].stackSize;
    }

    public Inventory GetFirstDesiredInventory()
    {
        foreach(Inventory inv in inventoryRequirements.Values)
        {
            if (inv.maxStackSize > inv.stackSize)
            {
                return inv;
            }
        }
        return null;
    }

    public void RegisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted += cb;
    }

    public void UnregisterJobCompletedCallback(Action<Job> cb)
    {
        cbJobCompleted -= cb;
    }

    public void RegisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped += cb;
    }

    public void UnregisterJobStoppedCallback(Action<Job> cb)
    {
        cbJobStopped -= cb;
    }

    public void RegisterWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }

    public void RegisterWorkedLuaCallback(string cb)
    {
        cbJobWorkedLua.Add(cb);
    }

    public void UnregisterWorkedLuaCallback(string cb)
    {
        cbJobWorkedLua.Remove(cb);
    }

    public void RegisterCompletedLuaCallback(string cb)
    {
        cbJobCompletedLua.Add(cb);
    }

    public void UnregisterCompletedLuaCallback(string cb)
    {
        cbJobCompletedLua.Remove(cb);
    }
}
