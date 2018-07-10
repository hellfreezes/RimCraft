using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Класс контролирующий персонажей
/// </summary>
public class Character : IXmlSerializable {

    public float X {
        get
        {
            return Mathf.Lerp(currTile.X, nextTile.X, movementProcentage);
        }
    }
    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, nextTile.Y, movementProcentage);
        }
    }

    public Tile currTile { get; protected set; }  // место в котором персонаж находимся
    Tile nextTile;  // Следующий тайл в череде тайлов до тайла назначения
    Tile destTile;  // Место в которое движется персонаж. Если мы не двигаемся то curr=dest
    Path_AStar pathAStar; // система поиска пути для персонажа
    
    float movementProcentage;
    Job myJob; // Задание над которым работает персонаж

    float speed = 4f;

    Action<Character> cbOnCharacterChanged;

    //Пустой для сериализации
    public Character()
    {

    }


    // Конструктор
    public Character(Tile tile)
    {
        currTile = destTile = nextTile = tile;
    }

    void Update_DoJob(float deltaTime)
    {
        // Есть ли у нас задание
        if (myJob == null)
        { //Персонаж свободен. У него нет работы
            // Значит взять работу из очереди работ
            myJob = currTile.world.jobQueue.Dequeue();
            if (myJob != null) // Если эта новая работа существует то,
            {
                // TODO: Проверить а можем ли мы добраться до места работы

                //Получить работу
                destTile = myJob.tile;
                //Подписываем метод OnJobEnded на указанные ниже события происходящие в Job

                //myJob.RegisterJobCompleteCallback(OnJobEnded);
                myJob.RegisterJobCompleteCallback(OnJobEnded);
                myJob.RegisterJobCancelCallback(OnJobEnded);
            }
        }

        // Проверяем есть ли у нас цель куда идти
        // Мы наместе?
        if (currTile == destTile) // Мы достигли цели
        //if (pathAStar != null && pathAStar.Lenght() == 1) // Мы рядом с местом работы (в смежном тайле)
        {   
            if (myJob != null) //И у нас есть работа
            {
                myJob.DoWork(deltaTime);
            }
        }
    }

    void Update_DoMovement(float deltaTime)
    {
        if (currTile == destTile)
        {// Мы никуда не движимся
            pathAStar = null;
            return; // Мы там где должны быть
        }

        if (nextTile == null || nextTile == currTile)
        {
            //Получить следующий тайл из системы поиска пути
            if (pathAStar == null || pathAStar.Lenght() == 0)
            {
                // Путь еще не построен, значит построить
                pathAStar = new Path_AStar(currTile.world, currTile, destTile);
                if (pathAStar.Lenght() == 0)
                {
                    Debug.LogError("До пункта назначения нет пути");
                    //Надо бы отменить работу тут
                    AbandonJob(); // Надо бы вернуть работу обратно в очередь
                    pathAStar = null;
                    return;
                }
            }

            // Получить следующий тайл - точку назначения из системы пути
            nextTile = pathAStar.Dequeue();
            if (nextTile == currTile)
            {
                Debug.LogError("попытка получить следующий тайл неудалась: nextTile == currTile");
            }
        }


        //if (pathAStar.Lenght() == 1)
        //{
        //    return;
        //}
        // В этом месте мы уже должны были получить корректный путь к точке назначения


        // Вычисляем расстояние до цели
        // Вчисляем длинну гипотинузы (квадратный корень из суммы квадратов катетов).
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - nextTile.X, 2) + Mathf.Pow(currTile.Y - nextTile.Y, 2));

        // Вычисляем какое в этом фрейме прошли расстояние
        float distThisFrame = speed * deltaTime;

        // Переводим пройденное расстояние в проценты
        float procThisFrame = distThisFrame / distToTravel;

        // Получаем итого сколько прошли в процентах
        movementProcentage += procThisFrame;

        // Если всего прошли 100 или более %, то значит мы достигли места назначения
        if (movementProcentage >= 1)
        {
            // Достигли тайла назначения (nextTile)

            // Сбрасываем счетчики
            currTile = nextTile;
            movementProcentage = 0;
        }
    }

    public void Update(float deltaTime)
    {
        // Debug.Log("Character update");
        Update_DoJob(deltaTime);

        Update_DoMovement(deltaTime);



        if (cbOnCharacterChanged != null)
            cbOnCharacterChanged(this);
    }

    // Оменяет задание на работу
    public void AbandonJob()
    {
        nextTile = destTile = currTile;
        pathAStar = null;
        myJob.CancelJob();
        currTile.world.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    // Установить тайл назначения движения
    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile, true) == false )
        {
            Debug.Log("Тайл не является соседним.");
        }

        destTile = tile;
    }

    // Метод вызываемый когда произошло событие отмены или завершения определенной работы
    void OnJobEnded (Job j)
    {
        if (j != myJob)
        {
            Debug.LogError("Персонажу сообщили о завершении чужой работы. Видимо мы забыли где то отписаться от события.");
            return;
        }

        myJob = null; // Убираем связь. Работы закончена
    }

    public void RegisterOnCharacterChangedCallback(Action<Character> callback)
    {
        cbOnCharacterChanged += callback;
    }

    public void UnregisterOnCharacterChangedCallback(Action<Character> callback)
    {
        cbOnCharacterChanged -= callback;
    }

    /* ********************************************************
     * 
     *             Методы для Сохранения/Загрузки
     * 
     * ********************************************************/

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
    }
}
