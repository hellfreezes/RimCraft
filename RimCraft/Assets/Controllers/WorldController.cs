using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class WorldController : MonoBehaviour {

    [SerializeField]
    private Sprite floorSprite;

    private static WorldController instance;

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

        //Create a world with empty Tiles
        World = new World();

        //Создать GO для каждого тайла, чтобы отображать их в игре
        for (int x = 0; x < World.Width; x++)
        {
            for (int y = 0; y < World.Height; y++)
            {
                Tile tile_data = World.GetTileAt(x, y);

                GameObject tile_go = new GameObject();
                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                tile_go.AddComponent<SpriteRenderer>();
                tile_data.RegisterTileTypeChangeCallBack((tile) => { OnTileTypeChanged(tile, tile_go); });
            }
        }

        World.RandomizeTiles();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnTileTypeChanged(Tile tile_data, GameObject tile_go)
    {
        if (tile_data.Type == Tile.TileType.Floor)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = floorSprite;

        } else if (tile_data.Type == Tile.TileType.Empty)
        {
            tile_go.GetComponent<SpriteRenderer>().sprite = null;
        } else
        {
            Debug.LogError("Unrecognized tile type");
        }
    }
}
