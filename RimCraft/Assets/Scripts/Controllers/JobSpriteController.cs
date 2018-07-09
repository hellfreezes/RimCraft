using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobSpriteController : MonoBehaviour {

    FurnitureSpriteController fsc;
    Dictionary<Job, GameObject> jobGameObjectMap;

	// Use this for initialization
	void Start () {
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        WorldController.Instance.world.jobQueue.RegisterJobCreationCallback(OnJobCreated);
	}
	
	// Update is called once per frame
	void OnJobCreated (Job job) {
        // Отобразить спрайт
        // Sprite theSprite = fsc.GetSpriteForFurniture(job.jobObjectType);

        // Визуальная часть создания нового объекта
        // Объект создан. Пора назначить ему GameObject

        GameObject job_go = new GameObject();

        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("Попытка создать работу, которая уже есть в списке. Вероятно работа была доавлена в очередь заново.");
            return;
        }

        //Добавляем связь GameObject и экземпляра в словарь
        jobGameObjectMap.Add(job, job_go);
        job_go.name = "JOB_" + job.jobObjectType + "_" + job.tile.X + "_" + job.tile.Y;
        job_go.transform.position = new Vector3(job.tile.X, job.tile.Y, 0);
        job_go.transform.SetParent(this.transform, true);

        SpriteRenderer spriteRenderer = job_go.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        spriteRenderer.sortingLayerName = "Furniture";
        spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f);

        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
	}

    void OnJobEnded(Job job)
    {
        //Удалить спрайт
        GameObject job_go = jobGameObjectMap[job];

        job.UnregisterJobCancelCallback(OnJobEnded);
        job.UnregisterJobCompleteCallback(OnJobEnded);

        Destroy(job_go);
    }
}
