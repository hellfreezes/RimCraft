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

    // Список персонажей
    List<Character> characters;

    Dictionary<string, Furniture> furniturePrototypes;

    // Ширина мира в кол-ве тайлов
    int width;
    // Высота мира в кол-ве тайлов
    int height;

    public JobQueue jobQueue;

    Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;
    Action<Character> cbCharacterCreated;

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
        jobQueue = new JobQueue(); // Создаем новую очередь работ

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

        characters = new List<Character>();

        
    }

    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }
    }

    public Character CreateCharacter(Tile t)
    {

        //Debug.Log("CreateCharacter");
        Character c = new Character(t);
        characters.Add(c);
        if (cbCharacterCreated != null) 
            cbCharacterCreated(c);

        return c;
    }

    void CreateFurniturePrototype()
    {
        furniturePrototypes = new Dictionary<string, Furniture>();

        Furniture wallPrototype = Furniture.CreatePrototype("Wall", 0, 1, 1, true);
        furniturePrototypes.Add("Wall", wallPrototype);
    }


    //ВРЕМЕННАЯ
    public void SetupPathfindingExample()
    {
        Debug.Log("Запущена временная функция для построения карты");

        // Создаем временную карту для проведения тестов по поиску пути

        int l = width / 2 - 5;
        int h = height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = h - 5; y < h + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;

                if (x == 1 || x == (l+9) || y == h || y == (h+9))
                {
                    if (x != (l+9) && y != (h+4))
                    {
                        PlaceFurniture("Wall", tiles[x, y]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Возвращает ссылку на Tile объект находящийся в мире по конкретным координатам
    /// </summary>
    /// <param name="x">Координата сетки по X</param>
    /// <param name="y">Координата сетки по Y</param>
    /// <returns>Возвращает ссылку на экземпляр Тайла по указанным координатам</returns>
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

    public void RegisterCharacterCreated(Action<Character> callback)
    {
        cbCharacterCreated += callback;
    }

    public void UnregisterCharacterCreated(Action<Character> callback)
    {
        cbCharacterCreated -= callback;
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
        return furniturePrototypes[furnitureType].IsVaildPosition(t);
    }

    public Furniture GetFurniturePrototype(string objectType)
    {
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("Нет экземпляра фурнитуры для введенного типа");
            return null;
        }

        return furniturePrototypes[objectType];
    }
} 
