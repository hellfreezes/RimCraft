using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class TileSpriteController : MonoBehaviour {

    [SerializeField]
    private Sprite floorSprite;
    [SerializeField]
    private Sprite emptySprite;


    private Dictionary<Tile, GameObject> tileGameObjectMap; // Связь между тайлом и объектом в мире

    World world
    {
        get { return WorldController.Instance.world; }
    }

    void Start() {
        tileGameObjectMap = new Dictionary<Tile, GameObject>(); // Создаем новый словарь связей

        //Создать GO для каждого тайла, чтобы отображать их в игре
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile_data = world.GetTileAt(x, y);

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
        world.RegisterTileChanged(OnTileChanged);
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
}
