using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions {

    // Обновлялка двери, вызывается каждый тик
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        float openSpeed = 4;

        // Открывается ли дверь?
        if (furn.furnParameters["is_opening"] >= 1)
        {

            // Выполняем открытие
            furn.furnParameters["openness"] += deltaTime * openSpeed;
            if (furn.furnParameters["openness"] >= 1)
            {
                // Дверь открыта, нужно закрыть
                furn.furnParameters["is_opening"] = 0;
            }
        } else // Иначе нужно закрыть дверь
        {
            furn.furnParameters["openness"] -= deltaTime * openSpeed;
        }
        furn.furnParameters["openness"] = Mathf.Clamp01(furn.furnParameters["openness"]);

        furn.cbOnChanged(furn);
    }

    // Возвращает можно ли зайти в эту дверь или нет
    public static Enterablylity Door_IsEnterable(Furniture furn)
    {
        // Можно зайти или нет зависит от того, открыта ли дверь полностью
        furn.furnParameters["is_opening"] = 1;

        if (furn.furnParameters["openness"] >= 1)
        {
            return Enterablylity.Yes; // Зайти можно
        }
        return Enterablylity.Soon; // Подождите дверь открывается
    }
}
