using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;
using MoonSharp.Interpreter;

/// <summary>
/// Комната с набором характеристик
/// </summary>
[MoonSharpUserData]
public class Room : IXmlSerializable
{

    Dictionary<string, float> atmosphericGasses;

    List<Tile> tiles;

    public int ID
    {
        get { return World.current.GetRoomId(this); }
    }

    public bool IsOutSideRoom()
    {
        //if (tiles.Count == 0)
       //     return false;
        return this == World.current.GetOutsideRoom();
    }

    public Room ()
    {
        tiles = new List<Tile>();
        atmosphericGasses = new Dictionary<string, float>();
    }

    /// <summary>
    /// Говорит что тайл теперь принадлежит этой комнате
    /// </summary>
    /// <param name="t"></param>
    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t))
        {
            return;
        }

        if (t.room != null)
        {
            t.room.tiles.Remove(t);
        }

        t.room = this;
        tiles.Add(t);
    }

    /// <summary>
    /// Стирает все связи тайлов с этой комнатой
    /// </summary>
    public void ReturnTilesToOutsideRoom()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = World.current.GetOutsideRoom(); // Теперь это улица
        }

        tiles = new List<Tile>();
    }

    public void ChangeGas(string name, float amount)
    {
        if (IsOutSideRoom())
        {
            return;
        }

        if (atmosphericGasses.ContainsKey(name))
        {
            atmosphericGasses[name] += amount;
        } else
        {
            atmosphericGasses[name] = amount;
        }

        if (atmosphericGasses[name] < 0)
        {
            atmosphericGasses[name] = 0;
        }
    }

    public float GetGasAmount(string name)
    {
        if (atmosphericGasses.ContainsKey(name))
        {
            return atmosphericGasses[name];
        }
        return 0;
    }

    public float GetGasPercentage(string name)
    {
        if (atmosphericGasses.ContainsKey(name) == false)
        {
            return 0;
        }

        float t = 0;
        foreach (string n in atmosphericGasses.Keys)
        {
            t += atmosphericGasses[n];

        }

        return atmosphericGasses[name] / t;
    }

    public string[] GetGasNames()
    {
        return atmosphericGasses.Keys.ToArray();
    }

    /// <summary>
    /// Проверяет тайлы вокруг на наличие объекты способных образовать комнаты
    /// </summary>
    /// <param name="sourceFurniture">Объект, который возможно образовал новую комнату или разделил существующую на две комнаты</param>
    public static void DoRoomFloodFill(Tile sourceTile, bool onlyIfOutside = false)
    {
        World world = World.current;

        Room oldRoom = sourceTile.room;

        if (oldRoom != null)
        {
            // Исходный тайл закреплен за какой-то комнатой. Поэтому этот случай - это установка нового объекта
            // который в перспективе может разделить комнату на максимум 4 новых комнаты.

            // Пытаемся обозначить новую комнату для каждой из NESW направлений
            foreach (Tile t in sourceTile.GetNeighbours())
            {
                if (t.room != null && (onlyIfOutside == false || t.room.IsOutSideRoom()))
                {
                    ActualFloodFill(t, oldRoom);
                }
            }

            //Т.к. тайлы теперь будут указывать на другую комнату, то просто создаем новый лист (тем самым стираем указатели на прежние тайлы)
            //oldRoom.tiles = new List<Tile>();

            sourceTile.room = null;

            oldRoom.tiles.Remove(sourceTile);

            if (oldRoom.IsOutSideRoom() == false)// != world.GetOutsideRoom())
            {
                // Тут oldRoom  не должна иметь какие либо привязанные тайлы
                // и следующая операция просто должна удалить комнату из списка в классе World

                if (oldRoom.tiles.Count > 0)
                {
                    Debug.LogError("oldRoom всё еще содержит некоторые тайлы. Что-то не так. Где-то ошибка.");
                }

                world.DeleteRoom(oldRoom);


            }
        }
        else
        {
            // oldRoom = null
            // Что означает что в исходной тайле скорее всего стояла стена, которую демонтировали
            // что вероятно могло привести к слиянию нескольких комнат
            // Единственный вариант который можно предпринять сейчас - это попробовать образовать
            // новую комнату начиная с тайла исходника
            //Debug.Log("Megre");

            ActualFloodFill(sourceTile, null);

        }

    }

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        // Комментарий с YouTube
        // TODO: "This is just me thinking out loud. But couldn't you just check the neighbouring tiles 
        // from the tile of the wall you deconstructed, and in which room they are. And then merge the 
        // found rooms into the lowest room index? E.g. you destroy a wall that touches room 3 and the outside 
        // (room 0), then "add" room 3 to room 0 (plus the tile of the recently deconstructed wall). 
        // And maybe then recalculate the gas? This would eleminate doing floodfills, right?﻿"

        if (tile == null)
        {
            // Вышли за пределы карты, отмена
            return;
        }

        if (tile.room != oldRoom)
        {
            // Тайл, который мы проверяем уже обрабатывается, ему уже назначена какая-то комната
            return;
        }

        if (tile.furniture != null && tile.furniture.roomEnclouser)
        {
            // Проверяемый тайл - это объект способный ограничивать комнаты (стена, дверь и тп)
            // Он не может быть частью комнаты

            return;
        }

        if (tile.Type == TileType.Empty)
        {
            return;
        }

        // Если добались до сюда, значит нам нужно создать новую комнату
        Room newRoom = new Room();

        Queue<Tile> tilesToCheck = new Queue<Tile>(); // Создаем очередь из тайлов
        // Тайл проверен и может быть добавлен в новую комнату
        tilesToCheck.Enqueue(tile); // Добавляем в очередь текущий проверенный тайл (для последующего его назначения комнате)

        bool isConnectedToSpace = false;

        int tilesProcessed = 0;


        // Пока в очереди тайлов есть эти тайлы, выполняем
        while (tilesToCheck.Count > 0)
        {
            // Получаем из очереди очередной тайл
            Tile t = tilesToCheck.Dequeue();

            tilesProcessed++;

            if (t.room != newRoom)
            {
                // Назначаем тайл вновь созданной комнате
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbours();

                foreach (Tile t2 in ns)
                {
                    if (t2 == null || t2.Type == TileType.Empty)
                    {
                        // Соседний тайл оказался частью открытого пространства.
                        // Нужно вернуть всё назад (назначив всем тайлам newRoom ссылку на улицу)
                        // И удалить newRoom

                        isConnectedToSpace = true;

                        //if (oldRoom != null)
                        //{
                        //    newRoom.ReturnTilesToOutsideRoom();
                        //    return;
                        //}
                    }
                    else
                    {

                        // Сосед существует и не является пустым (частью открытого пространства).
                        // Не включает тайлы, которые мы уже проверяли и назначили им новую комнату,
                        // тайлы, которые содержат объекты способные образоывать комнаты
                        if (t2.room != newRoom && (t2.furniture == null || t2.furniture.roomEnclouser == false))
                        {
                            tilesToCheck.Enqueue(t2);
                        }
                    }
                }
            }
        }

        // Debug.Log("Обработано " + tilesProcessed.ToString() + " тайлов");

        if (isConnectedToSpace)
        {   // Все найденные тайлы включенные в новую комнату, по факту являются открытым пространством т.к. они
            // соеденены с ним.
            newRoom.ReturnTilesToOutsideRoom();
            return;
        }

        //Копируем состояние комнаты из предыдущей комнаты
        if (oldRoom != null)
        {
            //Debug.Log("копируем газ");
            // В данном случае комната делится на две или более
            // Поэтому просто копируем атмосферу
            newRoom.CopyGas(oldRoom);
        } else
        {
            // В данном случае комнаты объединяются в одну
            // Поэтому нужно собрать весь газ во всех комната и установить давление на основе
            // давления в других комната и размере получившийся комнаты
            // TODO ^^^

        }

        World.current.AddRoom(newRoom);

    }

    void CopyGas(Room other)
    {
        foreach (string n in other.atmosphericGasses.Keys)
        {
            this.atmosphericGasses[n] = other.atmosphericGasses[n];
            //Debug.Log(n + ":" + atmosphericGasses[n]);
        }
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

        if (reader.ReadToDescendant("Param"))
        {
            do
            {
                string k = reader.GetAttribute("name");
                float v = float.Parse(reader.GetAttribute("value"));

                atmosphericGasses[k] = v;


            } while (reader.ReadToNextSibling("Param"));
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        foreach (string k in atmosphericGasses.Keys)
        {
            writer.WriteStartElement("Param");
            writer.WriteAttributeString("name", k);
            writer.WriteAttributeString("value", atmosphericGasses[k].ToString());
            writer.WriteEndElement();
        }
    }
}
