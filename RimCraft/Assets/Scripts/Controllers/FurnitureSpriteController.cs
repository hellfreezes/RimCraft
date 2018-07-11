using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class FurnitureSpriteController : MonoBehaviour {

    private Dictionary<Furniture, GameObject> furnitureGameObjectMap; // Связка между установленными объектами и их игровыми объектами

    private Dictionary<string, Sprite> furnitureSprites;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    void Start() {
        LoadSprites();

        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        world.RegisterFurnitureCreated(OnFurnitureCreated);

        foreach(Furniture furn in world.furnitures)
        {
            OnFurnitureCreated(furn);
        }
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

     // Подписчик на событие в World, которое происходит когда фурнитура создается
    void OnFurnitureCreated(Furniture furn)
    {
        //Debug.Log("OnInstalledObjectCreated");
        //FIXME: не учитывает возможность находится на нескольких тайлах

        // Визуальная часть создания нового объекта
        // Объект создан. Пора назначить ему GameObject

        GameObject furn_go = new GameObject();

        //Добавляем связь GameObject и экземпляра в словарь
        furnitureGameObjectMap.Add(furn, furn_go);
        furn_go.name = furn.objectType + "_" + furn.tile.X + "_" + furn.tile.Y;
        furn_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
        furn_go.transform.SetParent(this.transform, true);

        //FIXME: Hardcoding - поворот двери точно должен быть не тут
        if (furn.objectType == "Door")
        {
            // По умолчанию дверь между EW
            // Проверить если дверь между стен SN, то повернуть на 90гр
            Tile northTile = world.GetTileAt(furn.tile.X, furn.tile.Y + 1);
            Tile southTile = world.GetTileAt(furn.tile.X, furn.tile.Y - 1);

            if (northTile != null && southTile != null &&
                northTile.furniture != null && southTile.furniture != null &&
                northTile.furniture.objectType == "Wall" && southTile.furniture.objectType == "Wall")
            {
                furn_go.transform.rotation = Quaternion.Euler(0, 0, 90);
                furn_go.transform.Translate(1f, 0, 0, Space.World); // Не ну это капец! Так нельзя! :)
            }
        }

        SpriteRenderer spriteRenderer = furn_go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForFurniture(furn);
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
    public Sprite GetSpriteForFurniture(Furniture furn)
    {
        string spriteName = furn.objectType;

        if (furn.linksToNeighbour == false) // Если объект не является составным
        {
            //FIXME: ниже хардкод который нужно написать нормально
            // Проверяем если это дверь, на сколько она открыта и меняем спрайт соотвественно
            if (furn.objectType == "Door")
            {
                if (furn.furnParameters["openness"] < 0.1f)
                {
                    //дверь закрыта
                    spriteName = "Door";
                }
                else if (furn.furnParameters["openness"] < 0.5f)
                {
                    spriteName = "Door_openness_1";
                }
                else if (furn.furnParameters["openness"] < 0.9f)
                {
                    spriteName = "Door_openness_2";
                }
                else
                {
                    spriteName = "Door_openness_3";
                }
            }
            // end of hardcode

            return furnitureSprites[spriteName];
        }

        // Если объект составной то продолжаем работать
        spriteName = furn.objectType + "_";
        int x = furn.tile.X;
        int y = furn.tile.Y;

        Tile t;
        t = world.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "N";
        }
        t = world.GetTileAt(x - 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "E";
        }
        t = world.GetTileAt(x, y + 1);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
        {
            spriteName += "S";
        }
        t = world.GetTileAt(x + 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == furn.objectType)
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

    public Sprite GetSpriteForFurniture(string objectType)
    {
        if (furnitureSprites.ContainsKey(objectType))
        {
            return furnitureSprites[objectType];
        } else if (furnitureSprites.ContainsKey(objectType + "_"))
        {
            return furnitureSprites[objectType + "_"];
        }
        Debug.LogError("В базе нет спрайта с именем: " + objectType);
        return null;
        
    }
}
