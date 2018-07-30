using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

// Это объекты, которые можно установить. Такие вещи например как: двери, стены, мебель и тп
public class Furniture : IXmlSerializable {

    /// <summary>
    /// Словарь содержащий кастомные параметры (float) упорядоченных по ключевой строке (string)
    /// Для получение кастомного параметра необходимо знать ключ (string).
    /// Создано чтобы в дальнейшем грузить кастомные параметры из LUA текстовых файлов
    /// </summary>
    protected Dictionary<string, float> furnParameters; // Кастомные параметры фурнитуры

    /// <summary>
    /// Перечень этих методов исполняется для фурнитуры каждый апдейт
    /// В данном случае float - это deltaTime.
    /// В вызываемом методе могут быть использованы параметры из словаря furnParameters
    /// </summary>
    //protected Action<Furniture, float> updateActions; // Какие-то действия которые умеет фурнитура
    protected List<string> updateActions; // строковые наименования функций полученных из LUA кода

    public Func<Furniture, Enterablylity> isEnterable; // Условия прохода через фурнитуру (в форме методов которые возвращают Enterability)

    // Перечень заданий связанных с данной конкретной фурнитурой
    List<Job> jobs;

    // Локальные координаты в сооружении точки (где стоять) выполнения работы персонажем
    // Точка может находится и вне тайла сооружения, но с привязкой к локальным координатам
    public Vector2 jobSpotOffset = Vector2.zero;
    // Координаты минисклада вещей которые (если) производит сооружение
    public Vector2 jobSpawnSpotOffset = Vector2.zero;

    // Ссылка на базовый тайл под объектом. Хотя объект может занимать больше чем 1 тайл
    public Tile tile { get; protected set; }

    // Используется для определения спрайта, который нужно отобразить
    public string objectType { get; protected set; }

    private string _name = null;
    public string Name
    {
        get
        {
            if (_name == null || _name.Length == 0)
            {
                return objectType;
            }

            return _name;
        }
        set
        {
            _name = value;
        }
    }

    // Цена перемещения через этот объект.
    // 0 тут означает, что через этот объект нельзя пройти
    public float movementCost { get; protected set; }

    public bool roomEnclouser { get; protected set; }

    // Размеры объекта в тайлах
    public int Width { get; protected set; }
    public int Height { get; protected set; }

    public Color tint = Color.white; // Цвет подкрашивающий нашу фурнитуру

    public bool linksToNeighbour { get; protected set; }

    public Action<Furniture> cbOnChanged;
    public Action<Furniture> cbOnRemoved;

    // Накопитель функций для проверки возможности установки фурнитуры
    Func<Tile, bool> funcPositionValidation;

    //TODO: пока не умеем вращать объекты перед установкой. А также не умеем ставить объекты на несколько тайлов

    // Функция обновления вызывается каждый тик
    public void Update(float deltaTime)
    {
        //Обновляем все загруженные в эту фурнитуру действия (точнее наверное обновляем все ее кастомные параметры furnParametrs)
        if (updateActions != null)
        {
            updateActions(this, deltaTime);
        }
    }

    // Пустой контруктор нужен только для сериализация.
    public Furniture()
    {
        furnParameters = new Dictionary<string, float>();
        jobs = new List<Job>();
        this.funcPositionValidation = this.DEFAULT__IsVaildPosition;
    }

    // Конструктор который копирует прототип - не использовать напрямую, если не используются субклассы
    // Вместо него нужно использовать Clone
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.Name = other.Name;
        this.movementCost = other.movementCost;
        this.roomEnclouser = other.roomEnclouser;
        this.Width = other.Width;
        this.Height = other.Height;
        this.tint = other.tint;
        this.linksToNeighbour = other.linksToNeighbour;
        this.jobSpotOffset = other.jobSpotOffset;
        this.jobSpawnSpotOffset = other.jobSpawnSpotOffset;

        // Перенимаем кастомные параметры и методы
        this.furnParameters = new Dictionary<string, float>(other.furnParameters);
        this.jobs = new List<Job>();

        if (other.updateActions != null)
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
        if (other.isEnterable != null)
            this.isEnterable = (Func<Furniture, Enterablylity>)other.isEnterable.Clone();
        if (other.funcPositionValidation != null)
            this.funcPositionValidation = (Func<Tile, bool>)other.funcPositionValidation.Clone();
    }

    public virtual Furniture Clone()
    {
        return new Furniture(this);
    }

    /// <summary>
    /// Конструктор для создания прототипа из параметров. Применяется только в одном случае и только для создания прототипов.
    /// Для установки фурнитуры надо использовать Конструктор клонирования прототипов
    /// </summary>
    /// <param name="objectType">Строковое имя</param>
    /// <param name="movementCost">Стоимость прохода через</param>
    /// <param name="width">Ширина в кол-ве тайлов</param>
    /// <param name="height">Длина в кол-ве тайлов</param>
    /// <param name="linksToNeighbour">Зависит ли графика от соседних тайлов</param>
    /// <param name="roomEnclouser">Может ли закрывать периметр, образуя комнаты</param>
    public Furniture (string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false, bool roomEnclouser = false)
    {
        this.furnParameters = new Dictionary<string, float>();

        this.objectType = objectType;
        this.movementCost = movementCost;
        this.roomEnclouser = roomEnclouser;
        this.Width = width;
        this.Height = height;
        this.linksToNeighbour = linksToNeighbour;

        this.funcPositionValidation = this.DEFAULT__IsVaildPosition;
    }

    static public Furniture PlaceInstance (Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("Неподходящее место для установки");
            return null;
        }

        Furniture obj = proto.Clone();

        obj.tile = tile;

        if (tile.PlaceFurniture(obj) == false)
        {
            // По какой то причине не удалось устновить объект
            // Возможно в тайле уже есть объект

            return null;
        }

        // Уведомляем соседние тайлы об установки в этот тайл фурнитуры. (Если у нее есть зависимость от соседей)
        if (obj.linksToNeighbour)
        {
            int x = tile.X;
            int y = tile.Y;

            Tile t;
            t = World.current.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.current.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.current.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = World.current.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return obj;
    }

    public int JobCount()
    {
        return jobs.Count;
    }

    public void AddJob(Job j)
    {
        j.furniture = this;
        jobs.Add(j);
        j.RegisterJobStoppedCallback(OnJobStopped);
        World.current.jobQueue.Enqueue(j);
    }

    protected void RemoveJob(Job j)
    {
        j.UnregisterJobStoppedCallback(OnJobStopped);
        jobs.Remove(j);
        j.furniture = null;
    }

    void OnJobStopped(Job j)
    {
        RemoveJob(j);
    }

    protected void ClearJobs()
    {
        Job[] jobs_arr = jobs.ToArray();
        foreach (Job j in jobs_arr)
        {
            RemoveJob(j);
        }
    }

    public void CancelJobs()
    {
        Job[] jobs_arr = jobs.ToArray();
        foreach (Job j in jobs_arr)
        {
            j.CancelJob();
        }
    }

    // Является ли фурнитура зоной хранения
    public bool IsStockpile()
    {
        return objectType == "Stockpile";
    }

    // Проверка на допустимость расположения фурнитуры в данном тайле
    public bool IsVaildPosition(Tile tile)
    {
        return funcPositionValidation(tile);
    }

    /// <summary>
    /// Метод должен быть заменен скриптом LUA, подгружаемым для каждого вида фурнитуры
    /// отдельно из текстового файла.
    /// </summary>
    protected bool DEFAULT__IsVaildPosition(Tile tile)
    {
        // На случай если фурнитура занимает больше чем 1х1 тайл
        for (int x_off = tile.X; x_off < (tile.X + Width); x_off++)
        {
            for (int y_off = tile.Y; y_off < (tile.Y + Height); y_off++)
            {
                Tile t = World.current.GetTileAt(x_off, y_off);

                // Проверяет можно ли ипользовать тайл для установки фурнитуры
                if (t.Type != TileType.Floor)
                {
                    return false;
                }

                // Также проверяет занятость
                if (t.furniture != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Демонтаж фурнитуры
    /// </summary>
    public void Decostruct()
    {
        //Debug.Log("Попытка разобрать объект");
        tile.UnplaceFurniture(); // Очистка тайла от фурнитуры

        if (cbOnRemoved != null)
        {
            // Выполняем все методы подписчики
            cbOnRemoved(this);
        }

        if (roomEnclouser)
        {
            Room.DoRoomFloodFill(this.tile);
        }

        World.current.InvalidateTileGraph();
        // Основная инфа и ссылки на фурнитуру удалены.
        // Надо удалить подписки на события

    }

    /// <summary>
    /// Получает кастомный параметр из словаря параметров по ключевой строке
    /// </summary>
    /// <param name="key">ключевая строка</param>
    /// <param name="default_value">значение которое будет возвращено если ключевой строки не найдено в словаре</param>
    /// <returns></returns>
    public float GetParameter(string key, float default_value = 0)
    {
        if (furnParameters.ContainsKey(key) == false)
            return default_value;
        return furnParameters[key];
    }

    public Tile GetJobSpotTile()
    {
        return World.current.GetTileAt((int)(tile.X + jobSpotOffset.x), (int)(tile.Y + jobSpotOffset.y));
    }

    public Tile GetSpawnSpotTile()
    {
        return World.current.GetTileAt((int)(tile.X + jobSpawnSpotOffset.x), (int)(tile.Y + jobSpawnSpotOffset.y));
    }

    public void SetParameter(string key, float value)
    {
        furnParameters[key] = value;
    }

    public void ChangeParameter(string key, float value)
    {
        if (furnParameters.ContainsKey(key) == false)
            furnParameters[key] = value;

        furnParameters[key] += value;
    }

    public void RegisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged += callback;
    }

    public void UnregisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged -= callback;
    }

    public void RegisterOnRemoveCallback(Action<Furniture> callback)
    {
        cbOnRemoved += callback;
    }

    public void UnregisterOnRemoveCallback(Action<Furniture> callback)
    {
        cbOnRemoved -= callback;
    }

    /// <summary>
    /// Регистрирует метод для фурнитуры, который будет вызываться каждый апдейт
    /// Нужно будет изменить такой способ регистрации метода, когда будем использовать LUA
    /// </summary>
    /// <param name="a"></param>
    public void RegisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions += a;
    }

    public void UnregisterUpdateAction(Action<Furniture, float> a)
    {
        updateActions -= a;
    }

    //public bool __IsVaildPositionForDoor(Tile tile)
    //{
    //    if (__IsVaildPosition(tile) == false)
    //        return false;
    //    // Проверка на наличие пары стен N/S или W/E

    //    return true;
    //}

    /* ********************************************************
     * 
     *             Методы для Сохранения/Загрузки
     * 
     * ********************************************************/

    public XmlSchema GetSchema()
    {
        throw new NotImplementedException();
    }

    public void ReadXmlPrototyper(XmlReader readerParent)
    {
        objectType = readerParent.GetAttribute("objectType");

        XmlReader reader = readerParent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "Name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;
                case "MovementCost":
                    reader.Read();
                    movementCost = reader.ReadContentAsFloat();
                    break;
                case "Width":
                    reader.Read();
                    Width = reader.ReadContentAsInt();
                    break;
                case "Height":
                    reader.Read();
                    Height = reader.ReadContentAsInt();
                    break;
                case "LinksToNeighbours":
                    reader.Read();
                    linksToNeighbour = reader.ReadContentAsBoolean();
                    break;
                case "EnclosesRooms":
                    reader.Read();
                    roomEnclouser = reader.ReadContentAsBoolean();
                    break;
                case "OnUpdate":
                    string functionName = reader.GetAttribute("functionName");
                     break;
                case "Params":
                    ReadXmlParams(reader);
                    break;
                case "BuildingJob":
                    float jobTime = float.Parse(reader.GetAttribute("jobTime"));
                    List<Inventory> invs = new List<Inventory>();
                    XmlReader invs_reader = reader.ReadSubtree();
                    while(invs_reader.Read())
                    {
                        if (invs_reader.Name == "Inventory")
                        {
                            Inventory inv = new Inventory(
                                invs_reader.GetAttribute("objectType"),
                                int.Parse(invs_reader.GetAttribute("amount")),
                                0
                                );
                            invs.Add(inv);
                        }
                    }

                    Job j = new Job(null, objectType, FurnitureActions.JobComlete_FurnitureBuilding, jobTime, invs.ToArray());
                    World.current.SetFurnitureJobPrototype(j, this);
                    break;
            }
        }
    }

    public void ReadXml(XmlReader reader)
    {
        //Все остальное читается в World
        //movementCost = float.Parse(reader.GetAttribute("MovementCost"));

        ReadXmlParams(reader);
    }

    public void ReadXmlParams(XmlReader reader)
    {
        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));

                furnParameters[k] = v;


            } while (reader.ReadToNextSibling("Param"));
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        writer.WriteAttributeString("X", tile.X.ToString());
        writer.WriteAttributeString("Y", tile.Y.ToString());
        writer.WriteAttributeString("ObjectType", objectType);
        //writer.WriteAttributeString("MovementCost", movementCost.ToString());

        foreach (string k in furnParameters.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", furnParameters[k].ToString());
            writer.WriteEndElement();
        }
    }
}
