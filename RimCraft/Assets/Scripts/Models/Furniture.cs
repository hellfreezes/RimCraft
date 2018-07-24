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
    protected Action<Furniture, float> updateActions; // Какие-то действия которые умеет фурнитура

    public Func<Furniture, Enterablylity> isEnterable; // Условия прохода через фурнитуру (в форме методов которые возвращают Enterability)

    // Перечень заданий связанных с данной конкретной фурнитурой
    List<Job> jobs;

    // Ссылка на базовый тайл под объектом. Хотя объект может занимать больше чем 1 тайл
    public Tile tile { get; protected set; }

    // Используется для определения спрайта, который нужно отобразить
    public string objectType { get; protected set; }
    // Цена перемещения через этот объект.
    // 0 тут означает, что через этот объект нельзя пройти
    public float movementCost { get; protected set; }

    public bool roomEnclouser { get; protected set; }

    // Размеры объекта в тайлах
    int width = 1;
    int height = 1;
    public bool linksToNeighbour { get; protected set; }

    public Action<Furniture> cbOnChanged;

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
    }

    // Конструктор который копирует прототип - не использовать напрямую, если не используются субклассы
    // Вместо него нужно использовать Clone
    protected Furniture(Furniture other)
    {
        this.objectType = other.objectType;
        this.movementCost = other.movementCost;
        this.roomEnclouser = other.roomEnclouser;
        this.width = other.width;
        this.height = other.height;
        this.linksToNeighbour = other.linksToNeighbour;

       // Перенимаем кастомные параметры и методы
        this.furnParameters = new Dictionary<string, float>(other.furnParameters);
        this.jobs = new List<Job>();

        if (other.updateActions != null)
            this.updateActions = (Action<Furniture, float>)other.updateActions.Clone();
        if (other.isEnterable != null)
            this.isEnterable = (Func<Furniture, Enterablylity>)other.isEnterable.Clone();
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
        this.width = width;
        this.height = height;
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
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.cbOnChanged != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
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
        jobs.Add(j);
        tile.world.jobQueue.Enqueue(j);
    }

    public void RemoveJob(Job j)
    {
        jobs.Remove(j);
        j.CancelJob();
        tile.world.jobQueue.Remove(j);
    }

    public void ClearJobs()
    {
        foreach (Job j in jobs)
        {
            RemoveJob(j);
        }
    }

    public void RegisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged += callback;
    }

    public void UnregisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged -= callback;
    }

    public bool IsVaildPosition(Tile tile)
    {
        return funcPositionValidation(tile);
    }

    /// <summary>
    /// Метод должен быть заменен скриптом LUA, подгружаемым для каждого вида фурнитуры
    /// отдельно из текстового файла.
    /// </summary>\
    protected bool DEFAULT__IsVaildPosition(Tile tile)
    {
        // Проверяет можно ли ипользовать тайл для установки фурнитуры
        if (tile.Type != TileType.Floor)
        {
            return false;
        }

        // Также проверяет занятость
        if (tile.furniture != null)
        {
            return false;
        }

        return true;
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

    public void ReadXml(XmlReader reader)
    {
        //Все остальное читается в World
        //movementCost = float.Parse(reader.GetAttribute("MovementCost"));

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
}
