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

        GameObject obj_go = new GameObject();

        //Добавляем связь GameObject и экземпляра в словарь
        furnitureGameObjectMap.Add(furn, obj_go);
        obj_go.name = furn.objectType + "_" + furn.tile.X + "_" + furn.tile.Y;
        obj_go.transform.position = new Vector3(furn.tile.X, furn.tile.Y, 0);
        obj_go.transform.SetParent(this.transform, true);

        SpriteRenderer spriteRenderer = obj_go.AddComponent<SpriteRenderer>();
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
    public Sprite GetSpriteForFurniture(Furniture obj)
    {
        if (obj.linksToNeighbour == false) // Если объект не является составным
            return furnitureSprites[obj.objectType];

        // Если объект составной то продолжаем работать
        string spriteName = obj.objectType + "_";
        int x = obj.tile.X;
        int y = obj.tile.Y;

        Tile t;
        t = world.GetTileAt(x, y - 1);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "N";
        }
        t = world.GetTileAt(x - 1, y);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "E";
        }
        t = world.GetTileAt(x, y + 1);
        if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
        {
            spriteName += "S";
        }
        t = world.GetTileAt(x + 1, y);
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
