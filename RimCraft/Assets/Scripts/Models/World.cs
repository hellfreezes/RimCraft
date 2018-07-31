using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;
using MoonSharp.Interpreter;



/*
 * Независимый от Unity класс работающий с миром и хранящий ссылки на его составные части
 */
 [MoonSharpUserData]
public class World : IXmlSerializable {
    public static World current { get; protected set; }

    //Двумерная база данных класса
    Tile[,] tiles;
    // Список персонажей
    public List<Character> characters;
    public List<Furniture> furnitures;
    public List<Room>      rooms;
    public InventoryManager inventoryManager;

    // Карта мира для поиска пути
    public Path_TileGraph tileGraph;

    public Dictionary<string, Furniture> furniturePrototypes { get; protected set; }
    public Dictionary<string, Job> furnitureJobPrototypes;

    // Ширина мира в кол-ве тайлов
    int width;
    // Высота мира в кол-ве тайлов
    int height;

    public JobQueue jobQueue;

    Action<Furniture> cbFurnitureCreated;
    Action<Tile> cbTileChanged;
    Action<Character> cbCharacterCreated;
    Action<Inventory> cbInventoryCreated;

    FurnitureActions luaLibrary;

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


    // Этот конструктор применяется только для Сериализации
    public World()
    {
        // Пустой конструктор
    }

    public World(int width, int height)
    {
        //Создаем новый мир
        SetupWorld(width, height);

        //Создаем одного персонажа (Debug)
        CreateCharacter(GetTileAt(Width / 2, Height / 2));
    }

    /// <summary>
    /// Возвращает комнату ваакум (дефолтная комната. Существует всегда, не может быть удалена)
    /// </summary>
    /// <returns></returns>
    public Room GetOutsideRoom()
    {
        return rooms[0];
    }

    public int GetRoomId(Room r)
    {
        if (r == null)
            return -1;

        return rooms.IndexOf(r);
    }

    public Room GetRoomFromId(int id)
    {
        if (id < 0 || id > rooms.Count - 1)
            return null;
        return rooms[id];
    }

    /// <summary>
    /// Удаляет комнату, но только не дефолтную
    /// </summary>
    /// <param name="r"></param>
    public void DeleteRoom(Room r)
    {
        if (r == GetOutsideRoom())
        {
            Debug.LogError("Попытка удалить дефолтную комнату");
            return;
        }
        r.ReturnTilesToOutsideRoom();
        rooms.Remove(r);
    }

    public void AddRoom(Room r)
    {
        rooms.Add(r);
    }

    void SetupWorld(int width, int height)
    {
        current = this; // Мир у нас один. И этот мир и есть текущий мир. 
                        // А current - это статический доступ к этому единственному экземпляру

        jobQueue = new JobQueue(); // Создаем новую очередь работ

        this.width = width;
        this.height = height;

        //Создается массив тайлов
        tiles = new Tile[width, height];

        rooms = new List<Room>();
        rooms.Add(new Room()); // улица - внешнее пространство

        //Заполняется новыми тайлами
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(x, y);
                tiles[x, y].RegisterTileTypeChangeCallBack(OnTileChanged);

                tiles[x, y].room = GetOutsideRoom(); // комната по умолчанию - внешнее пространство
            }
        }

        Debug.Log("World created with " + (width * height) + " tiles.");

        CreateFurniturePrototype();

        furnitures  = new List<Furniture>();
        characters  = new List<Character>();
        inventoryManager = new InventoryManager();
    }

    public void Update(float deltaTime)
    {
        foreach (Character c in characters)
        {
            c.Update(deltaTime);
        }

        foreach (Furniture furn in furnitures)
        {
            furn.Update(deltaTime);
        }

        // FIXME: ИСПОЛЬЗУЕТСЯ ДЛЯ ДЕБАГА - УДАЛИТЬ
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    Debug.Log("Количество заданий в очереди: " + jobQueue.Count);
        //}
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

    public void SetFurnitureJobPrototype(Job j, Furniture f)
    {
        furnitureJobPrototypes[f.objectType] = j;
    }

    void LoadFurnitureLua()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "LUA");
        filePath = Path.Combine(filePath, "Furniture.lua");

        string myLuaCode = File.ReadAllText(filePath);

        if (myLuaCode == null)
        {
            Debug.LogError("Ошибка загрузки LUA кода!");
            return;
        }

        luaLibrary = new FurnitureActions(myLuaCode);
    }

    void CreateFurniturePrototype()
    {
        LoadFurnitureLua();

        furniturePrototypes = new Dictionary<string, Furniture>();
        furnitureJobPrototypes = new Dictionary<string, Job>();

        // Загружаем данные о сооружениях из XML файла
        // Возможно следующий код лучше вынести в отдельный класс хэлпер а не заставлять класс контролирующий
        // мир работать с файловой системой.

        string filePath = Path.Combine(Application.streamingAssetsPath, "Data");
        filePath = Path.Combine(filePath, "Furniture.xml");

        string furnitureXmlText = File.ReadAllText(filePath);

        XmlTextReader reader = new XmlTextReader(new StringReader(furnitureXmlText));

        if (reader.ReadToDescendant("Furnitures"))
        {
            if (reader.ReadToDescendant("Furniture"))
            {
                do
                {
                    Furniture furn = new Furniture();
                    furn.ReadXmlPrototyper(reader);

                    furniturePrototypes[furn.objectType] = furn;
                } while (reader.ReadToNextSibling("Furniture"));
            }
            else
            {
                Debug.LogError("Ошибка чтения XML файла. Не найден элемент 'Furniture'");
            }
        } else
        {
            Debug.LogError("Ошибка чтения XML файла. Не найден элемент 'Furnitures'");
        }

        // Позднее загрузажть из LUA
        //furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction); // кастомный метод
        //furniturePrototypes["Door"].isEnterable = FurnitureActions.Door_IsEnterable; // кастомные условия доступности
    }

    //void CreateFurniturePrototype()
    //{
    //    /* Необходимо заменить этот метод на метод, который будет брать данные о прототипах из XML файлов */

    //    furniturePrototypes = new Dictionary<string, Furniture>();
    //    furnitureJobPrototypes = new Dictionary<string, Job>();

    //    // Прототип стандартных стен ----------------------------------
    //    Furniture wallPrototype = new Furniture("furn_SteelWall", 0, 1, 1, true, true);
    //    furniturePrototypes.Add("furn_SteelWall", wallPrototype);
    //    furniturePrototypes["furn_SteelWall"].Name = "Basic Wall";
    //    furnitureJobPrototypes.Add("furn_SteelWall", new Job(null, "furn_SteelWall", FurnitureActions.JobComlete_FurnitureBuilding, 0.1f, new Inventory[] { new Inventory("Steel Plate", 5, 0) }));

    //    // Прототип генератора кислорода ---------------------------------
    //    Furniture oxygenGeneratorPrototype = new Furniture("Oxygen Generator", 10f, 2, 2, false, false);
    //    furniturePrototypes.Add("Oxygen Generator", oxygenGeneratorPrototype);
    //    furniturePrototypes["Oxygen Generator"].RegisterUpdateAction(FurnitureActions.OxygenGenerator_UpdateAction);

    //    // Прототип стандратной зоны хранения--------------------------
    //    Furniture stockpilePrototype = new Furniture("Stockpile", 1, 1, 1, true, false);
    //    furniturePrototypes.Add("Stockpile", stockpilePrototype);
    //    furniturePrototypes["Stockpile"].RegisterUpdateAction(FurnitureActions.Stockpile_UpdateAction);
    //    furniturePrototypes["Stockpile"].tint = new Color32(180, 30, 30, 255);
    //    furnitureJobPrototypes.Add("Stockpile", new Job(null, "Stockpile", FurnitureActions.JobComlete_FurnitureBuilding, -1f, null));

    //    // Прототип стадартной двери ------------------------------
    //    Furniture doorPrototype = new Furniture("Door", 1, 1, 1, false, true);
    //    furniturePrototypes.Add("Door", doorPrototype);
    //    furniturePrototypes["Door"].SetParameter("openness", 0f); // кастомный параметр
    //    furniturePrototypes["Door"].SetParameter("is_opening", 0f); // кастомный параметр
    //    furniturePrototypes["Door"].RegisterUpdateAction(FurnitureActions.Door_UpdateAction); // кастомный метод
    //    furniturePrototypes["Door"].isEnterable = FurnitureActions.Door_IsEnterable; // кастомные условия доступности

    //    // Прототип шахты -----------------------------------------
    //    Furniture miningDroneStationPrototype = new Furniture("Mining Drone Station", 1f, 3, 3, false, false); // Вообще-то 3х2, сделать потом
    //    furniturePrototypes.Add("Mining Drone Station", miningDroneStationPrototype);
    //    furniturePrototypes["Mining Drone Station"].jobSpotOffset = new Vector2(1, 0);
    //    furniturePrototypes["Mining Drone Station"].RegisterUpdateAction(FurnitureActions.MiningDroneStation_UpdateAction);
    //}


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
                        PlaceFurniture("furn_SteelWall", tiles[x, y]);
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

    public Furniture PlaceFurniture(string objectType, Tile t, bool doRoomFloodFill = true)
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

        furn.RegisterOnRemoveCallback(OnFurnitureRemoved);
        // Добавляем созданную фурнитуру в лист фурнитур
        furnitures.Add(furn);

        // Вероятно надо вычислить образование комнат
        if (doRoomFloodFill && furn.roomEnclosure)
        {
            Room.DoRoomFloodFill(furn.tile);
        }

        if (cbFurnitureCreated != null)
        {
            cbFurnitureCreated(furn);

            if (furn.movementCost != 1)
            {
                // Если фурнитура является проходимой, то она никак не повлияет на карту пути
                // Поэтому можно не обновлять карту 
                // FIXME: не уверен на счет условия. Надо перепроверить на практике!
                InvalidateTileGraph(); // Перестраивает карту поиска пути
            }
        }

        return furn;
    }

    /// <summary>
    /// Метод подписчик на демонтаж фурнитуры.
    /// Вызывается из Furniture
    /// </summary>
    /// <param name="furn"></param>
    public void OnFurnitureRemoved(Furniture furn)
    {
        furnitures.Remove(furn);
    }

    public void RegisterCharacterCreated(Action<Character> callback)
    {
        cbCharacterCreated += callback;
    }

    public void UnregisterCharacterCreated(Action<Character> callback)
    {
        cbCharacterCreated -= callback;
    }

    public void RegisterInventoryCreated(Action<Inventory> callback)
    {
        cbInventoryCreated += callback;
    }

    public void UnregisterInventoryCreated(Action<Inventory> callback)
    {
        cbInventoryCreated -= callback;
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

    //Это фикс т.к. cbInventoryCreated по хорошему должно находится в InventoryManager
    public void OnInventoryCreated(Inventory inv)
    {
        if (cbInventoryCreated != null)
            cbInventoryCreated(inv);
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
    // Реализация интерфейса IXmlSerializable

    public XmlSchema GetSchema()
    {
        //throw new NotImplementedException();
        return null;
    }

    public void ReadXml(XmlReader reader)
    {
        float startTime = Time.time;
        //Debug.Log("World проходит десериализацию...");
        //throw new NotImplementedException();
        width = int.Parse(reader.GetAttribute("Width"));
        height = int.Parse(reader.GetAttribute("Height"));
        SetupWorld(width, height);

        while (reader.Read()) // Начинаем построчное чтение XML
        {
            switch(reader.Name)
            {
                case "Rooms":
                    ReadXml_Rooms(reader);
                    break;
                case "Tiles": // Доходя до секции Тайлы, вызываем:
                    ReadXml_Tiles(reader);
                    break;
                case "Furnitures": // Доходя до секции Фурнитуры, вызываем:
                    ReadXml_Furnitures(reader);
                    break;
                case "Characters": // Доходя до секции Фурнитуры, вызываем:
                    ReadXml_Characters(reader);
                    break;
            }
        }
        //DEBUG - УДАЛИТЬ
        // Создаем предмет инвентаря чисто для тестов
        Inventory inv = new Inventory("Steel Plate", 50, 50);
        Tile t = GetTileAt(Width / 2, Height / 2 - 1);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(inv);
        }

        inv = new Inventory("Steel Plate", 50, 50);
        t = GetTileAt(Width / 2 - 1, Height / 2 - 3);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(inv);
        }

        inv = new Inventory("Steel Plate", 50, 50);
        t = GetTileAt(Width / 2 + 2, Height / 2 - 3);
        inventoryManager.PlaceInventory(t, inv);
        if (cbInventoryCreated != null)
        {
            cbInventoryCreated(inv);
        }

        //Debug.Log("Мир загружен за " + (Time.time - startTime).ToString() + " секунд.");
    }

    //Пробегается по всем тайлам далее в XML
    //Метод является инкапсуляцией части кода из основного ReadXml и работает последовательно
    void ReadXml_Tiles(XmlReader reader) {
        if (reader.ReadToDescendant("Tile"))
        {
            // У нас есть хотя бы один тайл
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                tiles[x, y].ReadXml(reader); // Читаем далее XML встроенным в Tile методом
            } while (reader.ReadToNextSibling("Tile")); // Если есть еще тайлы, то повторяем
        }
    }

    void ReadXml_Furnitures(XmlReader reader)
    {
        if (reader.ReadToDescendant("Furniture"))
        {
            do
            {
                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Furniture furn = PlaceFurniture(reader.GetAttribute("ObjectType"), tiles[x, y], false);
                furn.ReadXml(reader);
            } while (reader.ReadToNextSibling("Furniture"));

            // Теперь будем получать информацию о комнатах из загрузочного файла, на не генерировать налету
            //foreach (Furniture furn in furnitures)
            //{
            //    Room.DoRoomFloodFill(furn.tile, true);
            //}
        }
    }

    void ReadXml_Rooms(XmlReader reader)
    {
        if (reader.ReadToDescendant("Room"))
        {
            do
            {
                Room r = new Room();
                rooms.Add(r);
                r.ReadXml(reader);

            } while (reader.ReadToNextSibling("Room"));
        }
    }

    void ReadXml_Characters(XmlReader reader)
    {
        if (reader.ReadToDescendant("Character"))
        {
            do
            {

                int x = int.Parse(reader.GetAttribute("X"));
                int y = int.Parse(reader.GetAttribute("Y"));

                Character c = CreateCharacter(tiles[x, y]);
                c.ReadXml(reader);
            } while (reader.ReadToNextSibling("Character"));
        }
    }

    public void WriteXml(XmlWriter writer)
    {
        //Debug.Log("World проходит сериализацию...");
        //throw new NotImplementedException();
        // Перечислям все данные которые должны быть сохранены
        writer.WriteAttributeString("Width", Width.ToString());
        writer.WriteAttributeString("Height", Height.ToString());

        //Сохраняем комнаты
        writer.WriteStartElement("Rooms");
        foreach (Room r in rooms)
        {
            if (r == GetOutsideRoom())
                continue; // Не сохраняем пустоту

            writer.WriteStartElement("Room");
            r.WriteXml(writer);
            writer.WriteEndElement();
        }
        writer.WriteEndElement();

        //Сохраняем тайлы
        writer.WriteStartElement("Tiles");
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (tiles[x, y].Type != TileType.Empty) // Не сохраняем пустые тайлы
                {
                    writer.WriteStartElement("Tile");
                    tiles[x, y].WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
        }
        writer.WriteEndElement();
        //Сохраняем сооружения
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
