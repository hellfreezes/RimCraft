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
                    // Место в которое движется персонаж. Если мы не двигаемся то curr=dest

    private Tile _destTile;
    public Tile destTile
    {
        get { return _destTile; }
        set {
            if (_destTile != value)
            {
                _destTile = value;
                pathAStar = null; // Если произошла смена точки назначения, то путь нуждается в перестройке
            }
        }
    }
    Path_AStar pathAStar; // система поиска пути для персонажа
    
    float movementProcentage;
    Job myJob; // Задание над которым работает персонаж
    public Inventory inventory; //Предмето, который несет персонаж (это не то, что надето и не то, во что экипирован персонаж)

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

    void GetNewJob()
    {
        myJob = currTile.world.jobQueue.Dequeue();

        if (myJob == null)
            return;

        destTile = myJob.tile;
        //Подписываем метод OnJobEnded на указанные ниже события происходящие в Job
        myJob.RegisterJobCompleteCallback(OnJobEnded);
        myJob.RegisterJobCancelCallback(OnJobEnded);

        // Тутже проверяем возможно ли добраться до тайла в котором находится эта работа
        // Возможно персонаж не пойдет в место работы сразу (возможно нужно еще захватить материал)
        // Но проверить возможно ли дойти до места работы всё же необходимо.


        pathAStar = new Path_AStar(currTile.world, currTile, destTile);
        if (pathAStar.Lenght() == 0) // Попытались построить путь, но пройти в конечную точку невозможно
        {
            Debug.LogError("До пункта назначения (до работы) нет пути");
            //Надо бы отменить работу тут
            AbandonJob(); // Надо бы вернуть работу обратно в очередь
            destTile = currTile;
        }
    }

    void Update_DoJob(float deltaTime)
    {
        // Есть ли у нас задание
        if (myJob == null)
        { //Персонаж свободен. У него нет работы
            // Значит взять работу из очереди работ
            GetNewJob();

            if (myJob == null) // Если эта новая работа НЕсуществует то,
            {
                // Для персонажа нет работы. Выходим из метода
                destTile = currTile;
                return;
            }
        }

        //В этой точке у нас есть работа и до нее есть путь

        /*
         * 1. Хватает ли для этой работы материала у персонажа?
         */
        if (myJob.HasAllMaterial() == false)
        {
            // У нас недостаточно какого то материала
            /* 2. Есть ли у персонажа необходимое для работы
             */
            if (inventory != null)
            {
                if (myJob.DesiresInventoryType(inventory) > 0)
                {
                    // Если да. То несем это в место работы и кладем в тайл с работой
                    if (currTile == myJob.tile)
                    {
                        // Мы в месте работы, поэтому складываем переносимый материал в место работы
                        currTile.world.inventoryManager.PlaceInventory(myJob, inventory);

                        // Вызываем все коллбэки. Потому, как в процессе выполнения с объектом задания могут происходить изменения. Например визуальные
                        myJob.DoWork(0); 

                        // Переносит ли персонаж чтонибудь?
                        if (inventory.stackSize == 0)
                        { // Перонаж положил всё в тайл с работой. У него больше ничего нет. Обнуляем инвентарь
                            inventory = null;
                        }
                        else
                        {
                            // Персонаж принес больше чем нужно было
                            // Остатки остались у персонажа
                            Debug.LogError("Персонаж всё еще что-то держит, так быть не должно. Пока обнуляем его инвентарь. Но это означает, что проиходит утечка предметов. Предмемты удалены безвозвратно. ИСПРАВИТЬ!!!");
                            inventory = null;
                        }
                    }
                    else
                    {
                        // Всё еще идем в место работы
                        destTile = myJob.tile;
                        return;
                    }
                }
                else
                {
                    // Персонаж держит какой то предмет, но для работы он не нужен
                    // Надо бросить этот предмет.
                    // TODO: найти ближайший свободный тайл и бросить предмет туда
                    if (currTile.world.inventoryManager.PlaceInventory(currTile, inventory) == false)
                    {
                        Debug.LogError("Персонаж попробовать положить предмет в неправильный тайл");
                        //FIXME: следующая строка приведет к потере предмета
                        inventory = null;
                    }
                }
            }
            else
            {
                // Персонаж ничего не держит.

                // Находимся ли мы в тайле который содержит необходимые материалы для работы
                if (currTile.inventory != null && (myJob.canPickupFromStockpile || currTile.furniture == null || currTile.furniture.IsStockpile() == false) && myJob.DesiresInventoryType(currTile.inventory) > 0)
                { // Если персонаж в тайле, который содержит нужный материал, то поднять его
                    // Поднять предметы

                    currTile.world.inventoryManager.PlaceInventory(this, currTile.inventory, myJob.DesiresInventoryType(currTile.inventory));

                }
                else
                {

                    // Работе не хватает материалов. Но у персонажа их нет.

                    Inventory desired = myJob.GetFirstDesiredInventory();

                    // FIXME: временное решение. Примитивно
                    Inventory supplier = currTile.world.inventoryManager.GetClosestInventoryOfType(desired.objectType, currTile, desired.maxStackSize - desired.stackSize, myJob.canPickupFromStockpile);

                    if (supplier == null)
                    { // На сцене нет нужного предмета для данной работы
                        Debug.Log(desired.objectType + " не найдено ни в одном тайле");
                        AbandonJob(); // Отказываемся от этой работы
                        return;
                    }

                    // Идем тайл в котором лежит нужный материал
                    // destTile = <тайл с нужным материалом>
                    destTile = supplier.tile;
                    return;
                }
            }
            return; // Пока необходимый материал не будет лежать в месте работы, дальше по методу двигаться нельзя
        }

        // Если персонаж достиг этой точки то,
        // Для работы есть весь необходимый материал
        // Убедимся, что тайл места назначения - это тайл с работой
        destTile = myJob.tile;

        // Проверяем есть ли у нас цель куда идти
        // Мы наместе?
        if (currTile == myJob.tile) // Мы достигли цели
        // Мы рядом с местом работы (в смежном тайле)
        {
            // Мы рядом с работой. Вызывается функция которая отсчитывает таймер работы 
            // и в конце вызывает завершающий метод
            myJob.DoWork(deltaTime);
        }

        // Update_DoMovement
    }

    void Update_DoMovement(float deltaTime)
    {
        //if (destTile == null)
        //    return;

        if (currTile == destTile)
        {// Мы никуда не движимся
            pathAStar = null;
            return; // Мы там где должны быть
        }

        /// currTile - тайл в котором персонаж находится сейчас, возможно что-то делает в нем или покидает его в данный момент
        /// nextTile - тайл в который персонаж заходит в данный момент
        /// destTile - тайл финальной точки назначения. В него персонаж не двигается напрямую. Используется для нахождения пути.
        ///            А достигается этот тайл посредствам движения через череду nextTile

        if (nextTile == null || nextTile == currTile) // Если персонаж никуда не идет
        {
            //Получить следующий тайл из системы поиска пути
            if (pathAStar == null || pathAStar.Lenght() == 0)
            {
                // Путь еще не построен, значит построить
                pathAStar = new Path_AStar(currTile.world, currTile, destTile);
                if (pathAStar.Lenght() == 0) // Попытались построить путь, но пройти в конечную точку невозможно
                {
                    Debug.LogError("До пункта назначения нет пути");
                    //Надо бы отменить работу тут
                    AbandonJob(); // Надо бы вернуть работу обратно в очередь
                    return;
                }

                // Убираем первый тайл из пути, т.к. первый тайл - это тайл в котором мы сейчас стоим
                nextTile = pathAStar.Dequeue();
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

        // Проверяем условия входа в тайл
        if (nextTile.IsEnterable() == Enterablylity.Never) // Сюда никогда нельзя входить
        {
            // Похоже что путь устарел. Надо его обновить
            // FIXME:
            // В идеале такого не должно происходить. Персонаж должен следить за оптимальностью своего пути
            // Возможно стоить корректрировать путь, содержащий тайлы, которые были изменены. 
            // И делать надо это в момент их изменения, чтобы персонаж не терял время и не зашел в тупик.
            // Возможно пригодятся эвенты
            Debug.LogError("Персонаж пытается пройти через непроходимую вершину!");
            nextTile = null; // В следующий тайл нельзя идти
            pathAStar = null; // Сбрасываем найденный маршрут. Возможно карта поменялась пока персонаж шел к месту назначения
            return;
        } else if (nextTile.IsEnterable() == Enterablylity.Soon) // Войти можно но позже
        {
            /// Мы не можем зайти в этот тайл сейчас, но можем позже.
            /// Вероятно это дверь. Поэтому нужно подождать
            /// Т.е. нет необходимости сбрасывать путь

            // Ждем....
            return;
        }

        // Вычисляем какое в этом фрейме прошли расстояние
        float distThisFrame = speed / nextTile.movementCost * deltaTime; //movementCost не должно быть 0!!!!

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
        /// TODO:
        /// Если мы не можем выполнить эту работу, но нужно пометить у персонажа в списке
        /// что эта работа пока невыполнима. И подписать персонажа на эвент - работа изменена
        /// чтобы исключить ее из этого списка и проверить снова.

        //Debug.Log("Персонаж отказался от работы: " + myJob);
        nextTile = destTile = currTile;
        pathAStar = null;
        //Debug.Log("Возврат работы обратно в очередь: " + myJob);
        currTile.world.jobQueue.Enqueue(myJob);
        myJob.CancelJob();
        //myJob = null;
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
        // Отписываем от событий данный метод для j.
        j.UnregisterJobCancelCallback(OnJobEnded);
        j.UnregisterJobCompleteCallback(OnJobEnded);

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
        return null;
        //throw new NotImplementedException();
    }

    public void ReadXml(XmlReader reader)
    {
        //throw new NotImplementedException();
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
    }
}
