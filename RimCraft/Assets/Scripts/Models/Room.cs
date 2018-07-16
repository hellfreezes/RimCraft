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

    public Room()
    {
        tiles = new List<Tile>();
    }

    // Включить тайл в эту комнату
    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t)) // Если комната уже содержит этот тайл. То дальнейший код не нужен
            return;

        t.room = this;
        tiles.Add(t);
    }

    public void UnAssignAllTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].room = tiles[i].world.GetOutsideRoom(); // Присваиваем тайлы дефолтной комнате
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

        if (sourceFurniture.tile.room != world.GetOutsideRoom())
        {
            world.DeleteRoom(sourceFurniture.tile.room);
        }
    }
}
