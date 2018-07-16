using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Комната с набором характеристик
/// </summary>
public class Room {

    public float atmosO2 = 0;
    public float atmosN = 0;
    public float atmosCO2 = 0;

    List<Tile> tiles;

    public Room ()
    {
        tiles = new List<Tile>();
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
    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom(); // Теперь это улица
        }

        tiles = new List<Tile>();
    }


    /// <summary>
    /// Проверяет тайлы вокруг на наличие объекты способных образовать комнаты
    /// </summary>
    /// <param name="sourceFurniture">Объект, который возможно образовал новую комнату или разделил существующую на две комнаты</param>
    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        World world = sourceFurniture.tile.world;

        Room oldRoom = sourceFurniture.tile.room;

        // Пытаемся обозначить новую комнату для каждой из NESW направлений
        foreach (Tile t in sourceFurniture.tile.GetNeighbours())
        {
            ActualFloodFill(t, oldRoom);
        }

        //Т.к. тайлы теперь будут указывать на другую комнату, то просто создаем новый лист (тем самым стираем указатели на прежние тайлы)
        //oldRoom.tiles = new List<Tile>();

        sourceFurniture.tile.room = null;
        oldRoom.tiles.Remove(sourceFurniture.tile);

        if(oldRoom != world.GetOutsideRoom())
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

    protected static void ActualFloodFill(Tile tile, Room oldRoom)
    {
        if (tile == null)
        {
            // Вышли за пределы карты, отмена
            return;
        }

        if (tile.room != oldRoom)
        {
            // Тайл уже принадлежит какой-то комнате, значит комната не закрыта
            // Нет смысла продолжать. Комната не образована
            return;
        }

        if (tile.furniture != null && tile.furniture.roomEnclouser)
        {
            // Проверяемый тайл - это объект способный ограничивать комнаты (стена, дверь и тп)
            // Он не может быть частью комнаты

            return;
        }

        // Если добались до сюда, значит нам нужно создать новую комнату
        Room newRoom = new Room();

        Queue<Tile> tilesToCheck = new Queue<Tile>(); // Создаем очередь из тайлов
        // Тайл проверен и может быть добавлен в новую комнату
        tilesToCheck.Enqueue(tile); // Добавляем в очередь текущий проверенный тайл (для последующего его назначения комнате)


        // Пока в очереди тайлов есть эти тайлы, выполняем
        while(tilesToCheck.Count > 0)
        {
            // Получаем из очереди очередной тайл
            Tile t = tilesToCheck.Dequeue();

            if(t.room == oldRoom)
            {
                // Назначаем тайл вновь созданной комнате
                newRoom.AssignTile(t);

                Tile[] ns = t.GetNeighbours();

                foreach (Tile t2 in ns)
                {
                    if (t2 != null && t2.room != oldRoom && t2.furniture == null || t2.furniture.roomEnclouser == false)
                    {
                        tilesToCheck.Enqueue(t2);
                    }
                }
            }
        }

        tile.world.AddRoom(newRoom);

    }
}
