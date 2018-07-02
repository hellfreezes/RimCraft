using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс контролирующий персонажей
/// </summary>
public class Character {

    public float X {
        get
        {
            return Mathf.Lerp(currTile.X, destTile.X, movementProcentage);
        }
    }
    public float Y
    {
        get
        {
            return Mathf.Lerp(currTile.Y, destTile.Y, movementProcentage);
        }
    }

    Tile currTile;  // место в котором персонаж находимся
    Tile destTile;  // Место в которое движется персонаж. Если мы не двигаемся то curr=dest
    float movementProcentage;


    float speed = 2f;

    // Конструктор
    public Character(Tile tile)
    {
        currTile = destTile = tile;
    }

    public void Update(float deltaTime)
    {
        // Проверяем есть ли у нас цель куда идти
        if (currTile == destTile)
            return;

        // Вычисляем расстояние до цели
        float distToTravel = Mathf.Sqrt(Mathf.Pow(currTile.X - destTile.X, 2) + Mathf.Pow(currTile.Y - destTile.Y, 2));

        // Вычисляем какое в этом фрейме прошли расстояние
        float distThisFrame = speed * deltaTime;

        // Переводим пройденное расстояние в проценты
        float procThisFrame = distToTravel / distToTravel;

        // Получаем итого сколько прошли в процентах
        movementProcentage += procThisFrame;

        // Если всего прошли 100 или более %, то значит мы достигли места назначения
        if (movementProcentage >= 1)
        {
            // Достигли тайла назначения
            // Сбрасываем счетчики
            currTile = destTile;
            movementProcentage = 0;
        }
    }

    // Установить тайл назначения движения
    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile, true) == false )
        {
            Debug.Log("Тайл не является соседним.");
        }

        destTile = tile;
    }
}
