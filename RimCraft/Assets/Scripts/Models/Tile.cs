﻿using System;
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

    public float movementCost
    {
        get
        {
            if (type == TileType.Empty)
                return 0; // непроходим

            if (furniture == null)
                return 1;

            return 1 * furniture.movementCost;
        }
    }

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
        // БОЛЕЕ ИЗЯЩНОЕ РЕШЕНИЕ:
        // Приминительно к соседям по горизонтали и по вертикали:
        // Первая линия
        // Координаты соседних тайлов отличаются всегда только на 1 по
        // горизонтали или по вертикали.
        // Проверяем так ли это (левая и правая сторона выражения может быть
        // либо 1 либо 0, но не может быть одновременно 0 + 0 и 1 + 1. Таким
        // образом если тайлы соседние то выражение будет равно 1.
        // Вторая линия 
        // проверяет оси отдельно и таким образом проверяет диагональных соседей
        return
            Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||
            ((diagOkay == true) && (Mathf.Abs(this.X - tile.X) == 1) && (Mathf.Abs(this.Y - tile.Y) == 1));

        // СТАРОЕ РЕШЕНИЕ (НО НЕ МЕНЕЕ ХОРОШЕЕ) - оно более читабельное:
        // Проверяем находится ли текущй экземпляр на одной X с проверяемым
        // И если так, то смотрим находится ли проверяемый всего на 1 позицию по Y
        // в верх или вниз. Если все истина, то тайлы соседние
        /*
        if (this.X == tile.X && (Mathf.Abs(this.Y - tile.Y) == 1))
        {
            return true;
        }
        // Тоже самое, только если тайлы находятся на одной оси Y, а проверяем
        // позицию +-1 по X.
        if (this.Y == tile.Y && (Mathf.Abs(this.X - tile.X) == 1))
        {
            return true;
        }
        // Если проверяем и диагональных соседей, то
        if (diagOkay == true)
        {
            // Проверяем диагональных соседей справа от текущего экземляра
            if (this.X == tile.X + 1 && (this.Y == tile.Y + 1 || this.Y == tile.Y - 1))
                return true;
            // и слева от экземпляра
            if (this.X == tile.X - 1 && (this.Y == tile.Y + 1 || this.Y == tile.Y - 1))
                return true;
        }
        return false;
        */
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="isDiagOkay">Можно ли совершать диагональные перемещения</param>
    /// <returns></returns>
    public Tile[] GetNeighbours(bool isDiagOkay = false)
    {
        Tile[] ns;

        if (isDiagOkay == false)
        {
            ns = new Tile[4]; // Порядок такйлов: N E S W
        } else
        {
            ns = new Tile[8]; // Порядок тайлов N E S W NE SE SW NW
        }

        Tile n;

        n = world.GetTileAt(X, Y + 1);
        ns[0] = n;
        n = world.GetTileAt(X + 1, Y);
        ns[1] = n;
        n = world.GetTileAt(X, Y - 1);
        ns[2] = n;
        n = world.GetTileAt(X - 1, Y);
        ns[3] = n;

        if (isDiagOkay == true)
        {
            n = world.GetTileAt(X + 1, Y + 1);
            ns[4] = n;
            n = world.GetTileAt(X + 1, Y - 1);
            ns[5] = n;
            n = world.GetTileAt(X - 1, Y - 1);
            ns[6] = n;
            n = world.GetTileAt(X - 1, Y + 1);
            ns[7] = n;
        }

        return ns;
    }
}
