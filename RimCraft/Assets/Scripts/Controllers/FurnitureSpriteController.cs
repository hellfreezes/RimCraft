﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Класс юнити, управляющий нашим миром
 */
public class FurnitureSpriteController : MonoBehaviour {

    private Dictionary<Furniture, GameObject> furnitureGameObjectMap; // Связка между установленными объектами и их игровыми объектами

    World world
    {
        get { return WorldController.Instance.world; }
    }

    void Start() {
        furnitureGameObjectMap = new Dictionary<Furniture, GameObject>();

        world.RegisterFurnitureCreated(OnFurnitureCreated);

        foreach(Furniture furn in world.furnitures)
        {
            OnFurnitureCreated(furn);
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
                                                               //Применяем поправку позиционирования в случае мультитайловости
                                                               //Незабыть, что furn.Width и furn.Height - это integer
                                                               //Поэтому делим на float не на integer
        furn_go.transform.position = new Vector3(furn.tile.X + ((furn.Width - 1) / 2f), furn.tile.Y + ((furn.Height - 1) / 2f), 0);
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
                northTile.furniture.objectType.Contains("Wall") && southTile.furniture.objectType.Contains("Wall"))
            {
                furn_go.transform.rotation = Quaternion.Euler(0, 0, 90);
                //furn_go.transform.Translate(1f, 0, 0, Space.World); // Не ну это капец! Так нельзя! :)
            }
        }

        SpriteRenderer spriteRenderer = furn_go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSpriteForFurniture(furn);
        spriteRenderer.sortingLayerName = "Furniture";
        spriteRenderer.color = furn.tint;

        // Подписывает метод OnTileTypeChanged тайл на событие изменения tile_data. 
        // Если событие изменения происходит в tile_data, то вызывается метод OnTileTypeChanged
        furn.RegisterOnChangeCallback(OnFurnitureChanged);
        furn.RegisterOnRemoveCallback(OnFurnitureRemoved);
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
        furn_go.GetComponent<SpriteRenderer>().color = furn.tint;
    }

    // Подписчик только что был уведомлен о том, что фурнитура была удалена из игры
    // Визуальная часть должна быть обновлена
    void OnFurnitureRemoved(Furniture furn)
    {
        if (furnitureGameObjectMap.ContainsKey(furn) == false)
        {
            Debug.LogError("Попытка удалить со сцены фурнитуру, которой нет в словаре");
            return;
        }

        GameObject furn_go = furnitureGameObjectMap[furn];
        Destroy(furn_go);
        furnitureGameObjectMap.Remove(furn);
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
                if (furn.GetParameter("openness") < 0.1f)
                {
                    //дверь закрыта
                    spriteName = "Door";
                }
                else if (furn.GetParameter("openness") < 0.5f)
                {
                    spriteName = "Door_openness_1";
                }
                else if (furn.GetParameter("openness") < 0.9f)
                {
                    spriteName = "Door_openness_2";
                }
                else
                {
                    spriteName = "Door_openness_3";
                }
            }
            // end of hardcode

            return SpriteManager.current.GetSprite("Furniture", spriteName);
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

        //if (furnitureSprites.ContainsKey(spriteName) == false)
        //{
        //    Debug.LogError("В базе нет спрайта с именем: " + spriteName);
        //    return null;
        //}

        

        return SpriteManager.current.GetSprite("Furniture", spriteName);
    }

    public Sprite GetSpriteForFurniture(string objectType)
    {
        Sprite s = SpriteManager.current.GetSprite("Furniture", objectType);
        if (s == null)
        {
            s = SpriteManager.current.GetSprite("Furniture", objectType + "_");
        }

        return s;
        
    }
}
