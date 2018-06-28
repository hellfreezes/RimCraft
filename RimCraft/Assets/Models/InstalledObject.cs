using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Это объекты, которые можно установить. Такие вещи например как: двери, стены, мебель и тп
public class InstalledObject {

    // Ссылка на базовый тайл под объектом. Хотя объект может занимать больше чем 1 тайл
    Tile tile;

    // Используется для определения спрайта, который нужно отобразить
    string objectType;
    // Цена перемещения через этот объект.
    // 0 тут означает, что через этот объект нельзя пройти
    float movementCost = 1f;

    // Размеры объекта в тайлах
    int width = 1;
    int height = 1;

    protected InstalledObject()
    {

    }

    // Конструктор для создания прототипа
    static public InstalledObject CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1)
    {
        InstalledObject obj = new InstalledObject();
        obj.objectType = objectType;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;

        return obj;
    }

    static public InstalledObject PlaceInstance (InstalledObject proto, Tile tile)
    {
        InstalledObject obj = new InstalledObject();

        obj.objectType = proto.objectType;
        obj.movementCost = proto.movementCost;
        obj.width = proto.width;
        obj.height = proto.height;

        obj.tile = tile;

        //tile.installedObject = this;
        return obj;
    }
}
