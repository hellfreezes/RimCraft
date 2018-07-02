using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Базовый тип тайла. Первый слой - поверхность
public enum TileType { Empty, Floor };

/* 
 * Tile - класс независимы от движка Unity. Хранит в себе информацию о клетке из которых состоит мир.
 * Будет содержать информацию о поверхности, ссылться на строения или вещи, хранить качество окружающей
 * среды
 */
public class Tile {
    TileType type = TileType.Empty;
    // Делегат хранящий в себе методы принимающие аргумент Tile
    Action<Tile> cbTileChanged;

    //LooseObject - объекты, которые можно переносить
    Inventory inventory;
    //InstalledObject - объекты, которые стационано установлены (Мебель например)
    public Furniture furniture { get; protected set; }

    public Job pendingFurnitureJob;
    
    public World world { get; protected set; } //Ссылка на мир
    int x; //Место по X в мире
    int y; //Место по Y в мире

    //Ниже хранятся методы публичного доступа к приватным аргументам класса
    //Публичный доступ к типу тайла
    public TileType Type
    {
        get
        {
            return type;
        }

        set
        {
            //Проверяет при изменении тайла, а изминился ли он вообще
            TileType oldType = type;
            type = value;
            //Запускаем делегат на изменение и даем понять всем подписчикам, что изменен тип тайла
            if (cbTileChanged != null && type != oldType)
                cbTileChanged(this);
        }
    }
    public int X
    {
        get
        {
            return x;
        }
    }
    public int Y
    {
        get
        {
            return y;
        }
    }

    //Конструктор класса
    public Tile(World world, int x, int y)
    {
        this.world = world;
        this.x = x;
        this.y = y;
    }

    //Публичные методы регистрирующие подписчиком на cbTileTypeChanged делегат
    public void RegisterTileTypeChangeCallBack(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }
    public void UnregisterTileTypeChangeCallBack(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    public bool PlaceFurniture(Furniture objInstance)
    {
        if (objInstance == null)
        {
            // Убираем объект
            furniture = null;
            return true;
        }

        if (furniture == objInstance)
        {
            Debug.LogError("Попытка поставить объект в тайл, который уже имеет объект");
            return false;
        }

        furniture = objInstance;
        return true;
    }

    // вернет истину если передаваемый тайл является соседним текущему
    public bool IsNeighbour(Tile tile, bool diagOkay = false)
    {
        if (this.X == tile.X && (this.Y == tile.Y - 1 || this.Y == tile.Y + 1))
        {
            return true;
        }
        if (this.Y == tile.Y && (this.X == tile.X - 1 || this.X == tile.X + 1))
        {
            return true;
        }

        if (diagOkay == true)
        {

            if (this.X == tile.X + 1 && (this.Y == tile.Y + 1 || this.Y == tile.Y - 1))
                return true;
            if (this.X == tile.X - 1 && (this.Y == tile.Y + 1 || this.Y == tile.Y - 1))
                return true;
        }
        return false;
    }
}
