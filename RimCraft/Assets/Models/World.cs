using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Независимый от Unity класс работающий с миром и хранящий ссылки на его составные части
 */
public class World {
    //Двумерная база данных класса
    Tile[,] tiles;
    // Ширина мира в кол-ве тайлов
    int width;
    // Высота мира в кол-ве тайлов
    int height;

    // Доступ к аргументам
    public int Width
    {
        get
        {
            return width;
        }
    }
    public int Height
    {
        get
        {
            return height;
        }
    }
    // Конец доутспа к аргументам

    public World(int width = 100, int height = 100)
    {
        this.width = width;
        this.height = height;

        //Создается массив тайлов
        tiles = new Tile[width, height];

        //Заполняется новыми тайлами
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tiles[x, y] = new Tile(this, x, y);
            }
        }

        Debug.Log("World created with " + (width * height) + " tiles.");
    }

    // Возвращает ссылку на Tile объект находящийся в мире по конкретным координатам
    public Tile GetTileAt(int x, int y)
    {
        // Проверка попадают ли введенные координаты в рамки мира
        if (x > width || x < 0 || y > height || y < 0)
        {
            Debug.LogError("Tile (" + x + ", " + y + ") is out of range");
            return null;
        }
        return tiles[x, y];
    }

    // Временная функция. Случайно задает тип тайлов в мире
    public void RandomizeTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.Range(0,2) == 0)
                {
                    tiles[x, y].Type = Tile.TileType.Empty;
                } else
                {
                    tiles[x, y].Type = Tile.TileType.Floor;
                }
            }
        }
    }
}
