using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;



/*
 * Независимый от Unity класс работающий с миром и хранящий ссылки на его составные части
 */
public class World : IXmlSerializable {
    //Двумерная база данных класса
    Tile[,] tiles;
    // Список персонажей
    public List<Character> characters;
    public List<Furniture> furnitures;

    // Карта мира для поиска пути
    public Path_TileGraph tileGraph;

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

    public World(int width, int height)
    {
        //Создаем новый мир
        SetupWorld(width, height);

        //Создаем одного персонажа (Debug)
        CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    void SetupWorld(int width, int height)
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

        furnitures = new List<Furniture>();
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

        int l = Width / 2 - 5;
        int b = Height / 2 - 5;

        for (int x = l - 5; x < l + 15; x++)
        {
            for (int y = b - 5; y < b + 15; y++)
            {
                tiles[x, y].Type = TileType.Floor;


                if (x == l || x == (l + 9) || y == b || y == (b + 9))
                {
                    if (x != (l + 9) && y != (b + 4))
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
        if (x >= width || x < 0 || y >= height || y < 0)
        {
            //Debug.LogError("Tile (" + x + ", " + y + ") is out of range");
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

    public Furniture PlaceFurniture(string objectType, Tile t)
    {
        // Пока метод принимает 1 тайл для расположения - надо исправить это позже
        if (furniturePrototypes.ContainsKey(objectType) == false)
        {
            Debug.LogError("furniture не содержит прототипа для: " + objectType);
            return null;
        }

        Furniture furn = Furniture.PlaceInstance(furniturePrototypes[objectType], t);
        if (furn == null)
        {
            //Разместить объект не получилось т.к. в тайле уже есть объект
            return null;
        }

        // Добавляем созданную фурнитуру в лист фурнитур
        furnitures.Add(furn);

        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(furn);
            InvalidateTileGraph();
        }

        return furn;
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

    /// <summary>
    /// Вызывается всякий раз как происходит изменение файла
    /// Это метод подписчик на событие
    /// </summary>
    /// <param name="t">Обновленный тайл</param>
    void OnTileChanged(Tile t)
    {
        if (cbTileChanged == null)
        {
            return;
        }

        cbTileChanged(t);

        InvalidateTileGraph();
    }

    // Вызывается всякий раз кода происходит изменение мира
    // в следствии чего карта пути становится неправильной
    public void InvalidateTileGraph()
    {
        tileGraph = null;
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

    /* ********************************************************
     * 
     *             Методы для Сохранения/Загрузки
     * 
     * ********************************************************/

    public World()
    {
        
    }

    // Реализация интерфейса IXmlSerializable

    public XmlSchema GetSchema()
    {
        //throw new NotImplementedException();
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        Debug.Log("World проходит десериализацию...");
        //throw new NotImplementedException();
        width = int.Parse(reader.GetAttribute("Width"));
        height = int.Parse(reader.GetAttribute("Height"));
        SetupWorld(width, height);

        while (reader.Read()) // Начинаем построчное чтение XML
        {
            switch(reader.Name)
            {
                case "Tiles": // Доходя до секции Тайлы, вызываем:
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures": // Доходя до секции Фурнитуры, вызываем:
                    ReadXml_Furnitures(reader);
                    break;
                case "Character": // Доходя до секции Фурнитуры, вызываем:
                    ReadXml_Characters(reader);
                    break;
            }
        }
    }

    //Пробегается по всем тайлам далее в XML
    //Метод является инкапсуляцией части кода из основного ReadXml и работает последовательно
    void ReadXml_Tiles(XmlReader reader) {
        while (reader.Read())
        {
            if (reader.Name != "Tile") // Если мы уже не получаем тайлы, значит они кончились
                return;

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            tiles[x, y].ReadXml(reader); // Читаем далее XML встроенным в Tile методом
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.Name != "Furniture") // Если мы уже не получаем фурнитуру, значит они кончились
                return;

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Furniture furn = PlaceFurniture(reader.GetAttribute("ObjectType"), tiles[x, y]);
            furn.ReadXml(reader);
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.Name != "Character") // Если мы уже не получаем персонажей, значит они кончились
                return;

            int x = int.Parse(reader.GetAttribute("X"));
            int y = int.Parse(reader.GetAttribute("Y"));

            Character c = CreateCharacter(tiles[x, y]);
            c.ReadXml(reader);
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        Debug.Log("World проходит сериализацию...");
        //throw new NotImplementedException();
        // Перечислям все данные которые должны быть сохранены
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());
        //Сохраняем тайлы
        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                writer.WriteStartElement("Tile");
                tiles[x, y].WriteXml(writer);
                writer.WriteEndElement();
            }
        }
        writer.WriteEndElement();
        //Сохраняем фурнитуру
        writer.WriteStartElement("Furnitures");
        foreach (Furniture furn in furnitures)
        {
            writer.WriteStartElement("Furniture");
            furn.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        //Сохраняем персонажей
        writer.WriteStartElement("Characters");
        foreach (Character c in characters)
        {
            writer.WriteStartElement("Character");
            c.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }
}
