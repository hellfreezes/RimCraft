using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Код этого класса нужно перевести полностью на LUA код. Чтобы потом закгружать его на лету в работающую программу
// и главное - дать возможность писать аддоны к игре

public static class FurnitureActions {

    // Обновлялка двери, вызывается каждый тик
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        float openSpeed = 4;

        // Открывается ли дверь?
        if (furn.GetParameter("is_opening") >= 1)
        {

            // Выполняем открытие
            furn.ChangeParameter("openness", deltaTime * openSpeed);
            if (furn.GetParameter("openness") >= 1)
            {
                // Дверь открыта, нужно закрыть
                furn.SetParameter("is_opening",  0);
            }
        } else // Иначе нужно закрыть дверь
        {
            furn.ChangeParameter("openness", -deltaTime * openSpeed);
        }
        furn.SetParameter("openness", Mathf.Clamp01(furn.GetParameter("openness")));

        furn.cbOnChanged(furn);
    }

    // Возвращает можно ли зайти в эту дверь или нет
    public static Enterablylity Door_IsEnterable(Furniture furn)
    {
        // Можно зайти или нет зависит от того, открыта ли дверь полностью
        furn.SetParameter("is_opening", 1);

        if (furn.GetParameter("openness") >= 1)
        {
            return Enterablylity.Yes; // Зайти можно
        }
        return Enterablylity.Soon; // Подождите дверь открывается
    }


    public static void JobComlete_FurnitureBuilding(Job theJob)
    {
        WorldController.Instance.world.PlaceFurniture(theJob.jobObjectType, theJob.tile);
        theJob.tile.pendingFurnitureJob = null;
    }
}
