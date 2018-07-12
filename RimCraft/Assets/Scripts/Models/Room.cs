using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Комната с набором характеристик
/// </summary>
public class Room {

    public float atmosO2 = 0;
    public float atmosN = 0;
    public float atmosCO2 = 0;

    /// <summary>
    /// Проверяет тайлы вокруг на наличие объекты способных образовать комнаты
    /// </summary>
    /// <param name="sourceFurniture">Объект, который возможно образовал новую комнату или разделил существующую на две комнаты</param>
    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {

    }
}
