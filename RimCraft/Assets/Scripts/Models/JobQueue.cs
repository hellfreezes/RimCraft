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

    public void RegisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated += callback;
    }

    public void UnregisterJobCreationCallback(Action<Job> callback)
    {
        cbJobCreated -= callback;
    }
}
