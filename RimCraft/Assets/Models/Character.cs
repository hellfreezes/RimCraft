using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    Tile currTile;
    Tile destTile; // Если мы не двигаемся то curr=dest
    float movementProcentage;

    public Character(Tile tile)
    {
        currTile = destTile = tile;
    }

    public void SetDestination(Tile tile)
    {
        if (currTile.IsNeighbour(tile) == false )
        {

        }
    }
}
