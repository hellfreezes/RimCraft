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

    float jobTime = 1.0f; // время необходимое для выполнение работы

    // События которые расскажут всем подписчикам о том что происходит
    Action<Job> cbJobComplete; // Событие вызываемое по звершению работы
    Action<Job> cbJobCancel;  // Событие вызываемое если работа отменена

    public Job(Tile tile, Action<Job> cbJobComplete, float jobTime = 1f)
    {
        this.tile = tile;
        this.cbJobComplete += cbJobComplete;
    }

    public void DoWork(float workTime)
    {
        jobTime -= workTime;
        if (jobTime <= 0)
        {
            if (cbJobComplete != null)
                cbJobComplete(this);
        }
    }

    public void CancelJob()
    {
        if (cbJobCancel != null)
            cbJobCancel(this);
    }

    public void RegisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete += cbJobComplete;
    }

    public void UnregisterJobCompleteCallback(Action<Job> cb)
    {
        cbJobComplete -= cbJobComplete;
    }

    public void RegisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel += cbJobCancel;
    }

    public void UnregisterJobCancelCallback(Action<Job> cb)
    {
        cbJobCancel -= cbJobCancel;
    }
}
