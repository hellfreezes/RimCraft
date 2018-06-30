using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Независимый от Unity класс работающий с миром и хранящий ссылки на его составные части
 */
public class World {
    //Двумерная база данных класса
    Tile[,] tiles;

    Dictionary<string, Furniture> furniturePrototypes;

    // Ширина мира в кол-ве тайлов
    int width;
    // Высота мира в кол-ве тайлов
    int height;

    Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;

    //TODO: Возможно очередь надо вынести в отдельный специальный класс контролирующий очередь
    //Пока он PUBLIC !!!
    //Очередь работ
    public Queue<Job> jobQueue;

    // Доступ к аргументам
    public int Width
    {
        get
        {
            return width;
        }
    }
    public int Height
    {
        get
        {
            return height;
        }
    }
    // Конец доутспа к аргументам

    public World(int width = 100, int height = 100)
    {
        jobQueue = new Queue<Job>(); // Создаем новую очередь работ

        this.width = width;
        this.height = height;

        //Создается массив тайлов
        tiles = new Tile[width, height];

        //Заполняется новыми тайлами
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
                tiles[x, y].RegisterTileTypeChangeCallBack(OnTileChanged);
            }
        }

        Debug.Log("World created with " + (width * height) + " tiles.");

        CreateFurniturePrototype();
    }

    void CreateFurniturePrototype()
    {
        furniturePrototypes = new Dictionary<string, Furniture>();

        Furniture wallPrototype = Furniture.CreatePrototype("Wall", 0, 1, 1, true);
        furniturePrototypes.Add("Wall", wallPrototype);
    }

    // Возвращает ссылку на Tile объект находящийся в мире по конкретным координатам
    public Tile GetTileAt(int x, int y)
    {
        // Проверка попадают ли введенные координаты в рамки мира
        if (x > width || x < 0 || y > height || y < 0)
        {
            Debug.LogError("Tile (" + x + ", " + y + ") is out of range");
            return null;
        }
        return tiles[x, y];
    }

    // Временная функция. Случайно задает тип тайлов в мире
    public void RandomizeTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (UnityEngine.Random.Range(0,2) == 0)
                {
                    tiles[x, y].Type = TileType.Empty;
                } else
                {
                    tiles[x, y].Type = TileType.Floor;
                }
            }
        }
    }

    public void PlaceFurniture(string objectType, Tile t)
    {
        // Пока метод принимает 1 тайл для расположения - надо исправить это позже
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("furniture не содержит прототипа для: " + objectType);
            return;
        }

        Furniture obj = Furniture.PlaceInstance(furniturePrototypes[objectType], t);
        if (obj == null)
        {
            //Разместить объект не получилось т.к. в тайле уже есть объект
            return;
        }


        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(obj);
        }
    }

    public void RegisterFurnitureCreated(Action<Furniture> callback)
    {
        cbFurnitureCreated += callback;
    }

    public void UnregisterFurnitureCreated(Action<Furniture> callback)
    {
        cbFurnitureCreated -= callback;
    }

    public void RegisterTileChanged(Action<Tile> callback)
    {
        cbTileChanged += callback;
    }

    public void UnregisterTileChanged(Action<Tile> callback)
    {
        cbTileChanged -= callback;
    }

    void OnTileChanged(Tile t)
    {
        if (cbTileChanged == null)
        {
            return;
        }

        cbTileChanged(t);
    }

    public bool IsFurniturePlacmentVaild(string furnitureType, Tile t)
    {
        return furniturePrototypes[furnitureType].funcPositionValidation(t);
    }
}
