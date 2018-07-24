using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Этот класс содержит информацию о работе для персонажей
/// </summary>
public class Job {
    //Пока делаем чтобы установка фурнитуры совершалась по средствам этого класса

    public Tile tile;

    public float jobTime { get; protected set; } // время необходимое для выполнение работы

    //FIXME: временное решение связывающее экземпляр работы конкретно с фурнитурой. 
    //А хотелось бы чтобы работа могла быть связана с разными объектами
    // Использовать Generic
    public string jobObjectType { get; protected set; }

    public bool acceptsAnyInventoryItem = false;

    // События которые расскажут всем подписчикам о том что происходит
    Action<Job> cbJobComplete; // Событие вызываемое по звершению работы
    Action<Job> cbJobCancel;  // Событие вызываемое если работа отменена
    Action<Job> cbJobWorked; // Работа выполняется (в процессе / прогресс)

    public Dictionary<string, Inventory> inventoryRequirements; // Необходимые для работы материалы


    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime, Inventory[] jobInventoryRequirements)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete += cbJobComplete;
        this.jobTime = jobTime;

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
        this.cbJobComplete = other.cbJobComplete;
        this.jobTime = other.jobTime;

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
        jobTime -= workTime;

        if (cbJobWorked != null)
            cbJobWorked(this);

        if (jobTime <= 0)
        {
            //Debug.Log("Работа выполнена");
            if (cbJobComplete != null)
            {
                //Выполняем методы которые подписались
                cbJobComplete(this);
            }
        }
    }

    /// <summary>
    /// Отменяем работу
    /// </summary>
    public void CancelJob()
    {
        if (cbJobCancel != null)
            cbJobCancel(this);

        WorldController.Instance.world.jobQueue.Remove(this);
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

    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cb;
    }

    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cb;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cb;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cb;
    }

    public void RegisterWorkedCallback(Action<Job> cb)
    {
        cbJobWorked += cb;
    }

    public void UnregisterWorkedCallback(Action<Job> cb)
    {
        cbJobWorked -= cb;
    }
}
