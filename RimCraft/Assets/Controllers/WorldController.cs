using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class WorldController : MonoBehaviour {

    [SerializeField]
    private Sprite floorSprite;
    [SerializeField]
    private Sprite emptySprite;

    private static WorldController instance;

    private Dictionary<Tile, GameObject> tileGameObjectMap; // Связь между тайлом и объектом в мире

    private Dictionary<Furniture, GameObject> furnitureGameObjectMap; // Связка между установленными объектами и их игровыми объектами

    private Dictionary<string, Sprite> furnitureSprites;

    public World World { get; protected set; }

    public static WorldController Instance
    {
        get
        {
            return instance;
        }
    }

    // Use this for initialization
    void OnEnable() {
        //Создаем прямой доступ к единственному экземпляру
        if (instance != null)
            Debug.LogError("На сцене больше одного экземпляра WorldController");
        instance = this;

        LoadSprites();

        tileGameObjectMap = new Dictionary<Tile, GameObject>(); // Создаем новый словарь связей
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        //Create a world with empty Tiles
        World = new World();

        World.RegisterFurnitureCreated(OnFurnitureCreated);

        //Создать GO для каждого тайла, чтобы отображать их в игре
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                Tile tile_data = World.GetTileAt(x, y);

                GameObject tile_go = new GameObject();

                tileGameObjectMap.Add(tile_data, tile_go); //Создаем связь между Tile и GameObject который будет его отображать
                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                //Рендер для объекта
                //Также добавляем пустой спрайт
                SpriteRenderer spriteRenderer = tile_go.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = emptySprite;

                // tile_data.RegisterTileTypeChangeCallBack((tile) => { OnTileTypeChanged(tile, tile_go); }); // Старый вариант без использования tileGameObjectMap

                // Подписывает метод OnTileTypeChanged тайл на событие изменения tile_data. 
                // Если событие изменения происходит в tile_data, то вызывается метод OnTileTypeChanged
            }
        }

        World.RegisterTileChanged(OnTileChanged);

        // Центруем камеру
        Camera.main.transform.position = new Vector3(World.Width / 2, World.Height / 2, Camera.main.transform.position.z);

        //World.RandomizeTiles();
    }

    // Update is called once per frame
    void Update() {

    }

    void LoadSprites()
    {
        furnitureSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Furniture/");
        foreach (Sprite s in sprites)
        {
            furnitureSprites.Add(s.name, s);
        }
    }

    // Перебирает абсолютно все связи Tile-GameObject в словаре
    // И удаляет связь из словаря, подписку на событие OnTileTypeChange и удаляет GameObject
    void DestroyAllTileGameObjects()
    {
        while (tileGameObjectMap.Count > 0)
        {
            Tile tile_data = tileGameObjectMap.Keys.First(); // Доступ Keys.First обеспечивает библиотека Linq
            GameObject tile_go = tileGameObjectMap[tile_data];

            tileGameObjectMap.Remove(tile_data);

            tile_data.UnregisterTileTypeChangeCallBack(OnTileChanged);

            Destroy(tile_go);
        }
    }

    // Метод, который подписывается на событие в World, вызывается когда тайл меняется
    void OnTileChanged(Tile tile_data)
    {
        // Этот метод меняет спрайт тайла

        // Сначала метод находит связанный с тайлом GameObject
        // Используя при этом словарь связей tileGameObjectMap
        if (tileGameObjectMap.ContainsKey(tile_data) == false)
        {
            Debug.LogError("Связь между " + tile_data.Type + " и каким либо объектом на сцене отсутсвует.");
            return;
        }

        // tile_go - это GameObject тайла
        GameObject tile_go = tileGameObjectMap[tile_data];

        // Если по какой либо причине он равен null, то возвращаем ошибку
        if (tile_go == null)
        {
            Debug.LogError(tile_data.Type + " пуст.");
            return;
        }

        // FIXME: жуткий хардкодинг использующий ifelse
        // Меняем спрайт тайла в зависимости от его типа
        if (tile_data.Type == TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;

        } else if (tile_data.Type == TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = null;
        } else
        {
            // Возвращаем ошибку если тип у тайла не зарегистрирован в TileType (разве это возможно?)
            Debug.LogError("Unrecognized tile type");
        }
    }

    // Подписчик на событие в World, которое происходит когда фурнитура создается
    void OnFurnitureCreated(Furniture furn)
    {
        //Debug.Log("OnInstalledObjectCreated");
        //FIXME: не учитывает возможность находится на нескольких тайлах

        // Визуальная часть создания нового объекта
        // Объект создан. Пора назначить ему GameObject

        GameObject obj_go = new GameObject();

        //Добавляем связь GameObject и экземпляра в словарь
        furnitureGameObjectMap.Add(furn, obj_go);
        obj_go.name = furn.objectType + "_" + furn.tile.X + "_" + furn.tile.Y;
        obj_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
        obj_go.transform.SetParent(this.transform, true);

        SpriteRenderer spriteRenderer = obj_go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForFurniture(furn); //FIXME: тут надо бы назначить необходимый спрайт
        spriteRenderer.sortingLayerName = "Furniture";
        
        // Подписывает метод OnTileTypeChanged тайл на событие изменения tile_data. 
        // Если событие изменения происходит в tile_data, то вызывается метод OnTileTypeChanged
        furn.RegisterOnChangeCallback(OnFurnitureChanged);
    }

    // Подписчик на событие в Furniture, которое вызывается когда что либо в фурнитуре меняется
    void OnFurnitureChanged(Furniture furn)
    {
        // Меняем графику если это необходимо
        if (furnitureGameObjectMap.ContainsKey(furn) == false)
        {
            Debug.LogError("Попытка изменить игровой объект фурнитуры которой нет в словаре");
            return;
        }

        GameObject furn_go = furnitureGameObjectMap[furn];
        furn_go.GetComponent<SpriteRenderer>().sprite = GetSpriteForFurniture(furn);
    }

    // Если графика спрайта имеет зависимость от соседних спрайтов, то вычисляем
    // Например это может быть стена
    // Если зависимости нет, то выбираем спрайт из ресурсов по имени объекта
    Sprite GetSpriteForFurniture(Furniture obj)
    {
        if (obj.linksToNeighbour == false) // Если объект не является составным
            return furnitureSprites[obj.objectType];

        // Если объект составной то продолжаем работать
        string spriteName = obj.objectType + "_";
        int x = obj.tile.X;
        int y = obj.tile.Y;

        Tile t;
        t = World.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "N";
        }
        t = World.GetTileAt(x - 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "E";
        }
        t = World.GetTileAt(x, y + 1);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "S";
        }
        t = World.GetTileAt(x + 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "W";
        }

        if (furnitureSprites.ContainsKey(spriteName) == false)
        {
            Debug.LogError("В базе нет спрайта с именем: " + spriteName);
            return null;
        }

        return furnitureSprites[spriteName];
    }
}
