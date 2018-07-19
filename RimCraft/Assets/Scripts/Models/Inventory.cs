using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Это предметы, которые могут лежать на полу: ресурсы, стопки каких-либо предметов
// также вероятно неустановленные объекты
public class Inventory {

    public string objectType = "Steel Plate";

    public int maxStackSize = 50;
    public int stackSize = 1;

    // Инвентарь содержится либо в тайле либо у персонажа в рюкзаке
    public Tile tile;
    public Character cha;

    public Inventory()
    {
        
    }

    public Inventory(string objectType, int maxStackSize, int stackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    protected Inventory (Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }
}
