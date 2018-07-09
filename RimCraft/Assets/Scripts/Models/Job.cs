using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Этот класс содержит информацию о работе для персонажей
/// </summary>
public class Job {
    //Пока делаем чтобы установка фурнитуры совершалась по средствам этого класса

    public Tile tile { get; protected set; }

    float jobTime = 1f; // время необходимое для выполнение работы

    //FIXME: временное решение связывающее экземпляр работы конкретно с фурнитурой. 
    //А хотелось бы чтобы работа могла быть связана с разными объектами
    // Использовать Generic
    public string jobObjectType { get; protected set; }

    // События которые расскажут всем подписчикам о том что происходит
    Action<Job> cbJobComplete; // Событие вызываемое по звершению работы
    Action<Job> cbJobCancel;  // Событие вызываемое если работа отменена

    public Job(Tile tile, string jobObjectType, Action<Job> cbJobComplete, float jobTime = 0.1f)
    {
        this.tile = tile;
        this.jobObjectType = jobObjectType;
        this.cbJobComplete += cbJobComplete;
        this.jobTime = jobTime;
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;
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

    public void CancelJob()
    {
        if (cbJobCancel != null)
            cbJobCancel(this);
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
}
