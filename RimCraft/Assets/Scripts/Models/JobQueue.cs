using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobQueue {
    //Очередь работ
    Queue<Job> jobQueue;

    Action<Job> cbJobCreated;

    public JobQueue()
    {
        jobQueue = new Queue<Job>();
    }

    public void Enqueue(Job j)
    {
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
