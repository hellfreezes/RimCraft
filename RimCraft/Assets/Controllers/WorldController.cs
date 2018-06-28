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

    public World World { get; protected set; }

    public static WorldController Instance
    {
        get
        {
            return instance;
        }
    }

    // Use this for initialization
    void Start () {
        //Создаем прямой доступ к единственному экземпляру
        if (instance != null)
            Debug.LogError("На сцене больше одного экземпляра WorldController");
        instance = this;

        tileGameObjectMap = new Dictionary<Tile, GameObject>(); // Создаем новый словарь связей

        //Create a world with empty Tiles
        World = new World();

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
                tile_data.RegisterTileTypeChangeCallBack( OnTileTypeChanged );
            }
        }

        World.RandomizeTiles();
	}
	
	// Update is called once per frame
	void Update () {
		
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
}
