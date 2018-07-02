﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Это объекты, которые можно установить. Такие вещи например как: двери, стены, мебель и тп
public class Furniture {

    // Ссылка на базовый тайл под объектом. Хотя объект может занимать больше чем 1 тайл
    public Tile tile { get; protected set; }

    // Используется для определения спрайта, который нужно отобразить
    public string objectType { get; protected set; }
    // Цена перемещения через этот объект.
    // 0 тут означает, что через этот объект нельзя пройти
    float movementCost = 1f;

    // Размеры объекта в тайлах
    int width = 1;
    int height = 1;
    public bool linksToNeighbour { get; protected set; }

    Action<Furniture> cbOnChanged;

    // Накопитель функций для проверки возможности установки фурнитуры
    Func<Tile, bool> funcPositionValidation;

    //TODO: пока не умеем вращать объекты перед установкой. А также не умеем ставить объекты на несколько тайлов

    protected Furniture()
    {

    }

    // Конструктор для создания прототипа
    static public Furniture CreatePrototype(string objectType, float movementCost = 1f, int width = 1, int height = 1, bool linksToNeighbour = false)
    {
        Furniture obj = new Furniture();
        obj.objectType = objectType;
        obj.movementCost = movementCost;
        obj.width = width;
        obj.height = height;
        obj.linksToNeighbour = linksToNeighbour;

        obj.funcPositionValidation = obj.__IsVaildPosition;

        return obj;
    }

    static public Furniture PlaceInstance (Furniture proto, Tile tile)
    {
        if (proto.funcPositionValidation(tile) == false)
        {
            Debug.LogError("Неподходящее место для установки");
            return null;
        }

        Furniture obj = new Furniture();

        obj.objectType = proto.objectType;
        obj.movementCost = proto.movementCost;
        obj.width = proto.width;
        obj.height = proto.height;
        obj.linksToNeighbour = proto.linksToNeighbour;

        obj.tile = tile;

        if (tile.PlaceFurniture(obj) == false)
        {
            // По какой то причине не удалось устновить объект
            // Возможно в тайле уже есть объект

            return null;
        }

        if (obj.linksToNeighbour)
        {
            int x = tile.X;
            int y = tile.Y;

            Tile t;
            t = tile.world.GetTileAt(x, y - 1);
            if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x - 1, y);
            if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x, y + 1);
            if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
            t = tile.world.GetTileAt(x + 1, y);
            if (t != null && t.furniture != null && t.furniture.objectType == obj.objectType)
            {
                t.furniture.cbOnChanged(t.furniture);
            }
        }

        return obj;
    }

    public void RegisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged += callback;
    }

    public void UnregisterOnChangeCallback(Action<Furniture> callback)
    {
        cbOnChanged -= callback;
    }

    public bool IsVaildPosition(Tile tile)
    {
        return funcPositionValidation(tile);
    }

    public bool __IsVaildPosition(Tile tile)
    {
        // Проверяет можно ли ипользовать тайл для установки фурнитуры
        if (tile.Type != TileType.Floor)
        {
            return false;
        }

        // Также проверяет занятость
        if (tile.furniture != null)
        {
            return false;
        }

        return true;
    }

    public bool __IsVaildPositionForDoor(Tile tile)
    {
        if (__IsVaildPosition(tile) == false)
            return false;
        // Проверка на наличие пары стен N/S или W/E

        return true;
    }
}