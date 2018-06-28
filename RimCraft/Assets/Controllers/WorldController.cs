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
    void Start() {
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

                tile_go.AddComponent<SpriteRenderer>();
                // tile_data.RegisterTileTypeChangeCallBack((tile) => { OnTileTypeChanged(tile, tile_go); }); // Старый вариант без использования tileGameObjectMap

                // Подписывает метод OnTileTypeChanged тайл на событие изменения tile_data. 
                // Если событие изменения происходит в tile_data, то вызывается метод OnTileTypeChanged
                tile_data.RegisterTileTypeChangeCallBack(OnTileTypeChanged);
            }
        }

        World.RandomizeTiles();
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

            tile_data.UnregisterTileTypeChangeCallBack(OnTileTypeChanged);

            Destroy(tile_go);
        }
    }

    // Метод, который подписывается на изменение тайла
    void OnTileTypeChanged(Tile tile_data)
    {
        if (tileGameObjectMap.ContainsKey(tile_data) == false)
        {
            Debug.LogError("Связь между " + tile_data.Type + " и каким либо объектом на сцене отсутсвует.");
            return;
        }

        GameObject tile_go = tileGameObjectMap[tile_data];

        if (tile_go == null)
        {
            Debug.LogError(tile_data.Type + " пуст.");
            return;
        }

        if (tile_data.Type == TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;

        } else if (tile_data.Type == TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = null;
        } else
        {
            Debug.LogError("Unrecognized tile type");
        }
    }

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
