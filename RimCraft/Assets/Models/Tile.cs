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
    Action<Tile> cbTileTypeChanged;

    //LooseObject - объекты, которые можно переносить
    LooseObject looseObject;
    //InstalledObject - объекты, которые стационано установлены (Мебель например)
    InstalledObject installedObject;

    World world; //Ссылка на мир
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
            if (cbTileTypeChanged != null && type != oldType)
                cbTileTypeChanged(this);
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
        cbTileTypeChanged += callback;
    }
    public void UnregisterTileTypeChangeCallBack(Action<Tile> callback)
    {
        cbTileTypeChanged -= callback;
    }

    public bool PlaceObject()
    {

    }
}
