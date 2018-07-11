using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions {

    // Обновлялка двери, вызывается каждый тик
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        // Открывается ли дверь?
        if (furn.furnParameters["is_opening"] >= 1)
        {

            // Выполняем открытие
            furn.furnParameters["openess"] += deltaTime;
            if (furn.furnParameters["openess"] >= 1)
            {
                // Дверь открыта, нужно закрыть
                furn.furnParameters["is_opening"] = 0;
            }
        } else // Иначе нужно закрыть дверь
        {
            furn.furnParameters["openess"] -= deltaTime;
        }
        furn.furnParameters["openess"] = Mathf.Clamp01(furn.furnParameters["openess"]);
    }

    // Возвращает можно ли зайти в эту дверь или нет
    public static Enterablylity Door_IsEnterable(Furniture furn)
    {
        // Можно зайти или нет зависит от того, открыта ли дверь полностью
        furn.furnParameters["is_opening"] = 1;

        if (furn.furnParameters["openess"] >= 1)
        {
            return Enterablylity.Yes; // Зайти можно
        }
        return Enterablylity.Soon; // Подождите дверь открывается
    }
}
