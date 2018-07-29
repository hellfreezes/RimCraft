using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobQueue {
    //Очередь работ
    Queue<Job> jobQueue;

    Action<Job> cbJobCreated;

    public int Count
    {
        get { return jobQueue.Count; }
    }

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job j)
    {
        //Debug.Log("Adding job to queue. Existing queue size: " + jobQueue.Count);
        if (j.jobTime < 0)
        {
            // Работа с продолжительностью отрицательной - это работа которая выполняется мнгновенно после ее создания
            // и не попадает в очередь работ

            j.DoWork(0);
            return;
        }

        jobQueue.Enqueue(j);
        
        if (cbJobCreated != null)
        {
            cbJobCreated(j);
        }
    }

    public Job Dequeue()
    {
        if (jobQueue.Count == 0)
            return null;

        return jobQueue.Dequeue();
    }

    public void Remove(Job j)
    {
        // TODO: Кажется неправильное удаление. Придумать другое решение
        List<Job> jobs = new List<Job>(jobQueue);

        if (jobs.Contains(j) == false)
        {
            // Debug.LogError("Попытка удалить из очереди работу, которой там нет");
            // Скорее всего эта работа не в очереди потому, что какой то из персонажей над ней работает
            return;
        }

        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }

    public void RegisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated += callback;
    }

    public void UnregisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated -= callback;
    }
}
