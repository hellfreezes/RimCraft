using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Это предметы, которые могут лежать на полу: ресурсы, стопки каких-либо предметов
// также вероятно неустановленные объекты
public class Inventory {

    public string objectType = "Steel Plate";

    public int maxStackSize = 50;
    private int _stackSize = 1;
    public int stackSize {
        get { return _stackSize; }
        set
        {
            if (_stackSize != value)
            {
                _stackSize = value;

                if (cbInventoryChanged != null)
                {
                    cbInventoryChanged(this);
                }
            }
        }
    }

    Action<Inventory> cbInventoryChanged;

    // Инвентарь содержится либо в тайле либо у персонажа в рюкзаке
    public Tile tile;
    public Character character;

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

    public void RegisterChangeCallback(Action<Inventory> callback)
    {
        cbInventoryChanged += callback;
    }
    public void UnregisterChangeCallback(Action<Inventory> callback)
    {
        cbInventoryChanged -= callback;
    }
}
