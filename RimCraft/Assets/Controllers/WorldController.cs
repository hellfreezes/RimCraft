using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class WorldController : MonoBehaviour {

    [SerializeField]
    private Sprite floorSprite;

    World world;

	// Use this for initialization
	void Start () {
        //Create a world with empty Tiles
        world = new World();

        //Создать GO для каждого тайла, чтобы отображать их в игре
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile_data = world.GetTileAt(x, y);

                GameObject tile_go = new GameObject();
                tile_go.name = "Tile_" + x + "_" + y;
                tile_go.transform.position = new Vector3(tile_data.X, tile_data.Y, 0);
                tile_go.transform.SetParent(this.transform, true);

                tile_go.AddComponent<SpriteRenderer>();
                tile_data.RegisterTileTypeChangeCallBack((tile) => { OnTileTypeChanged(tile, tile_go); });
            }
        }

        world.RandomizeTiles();
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
