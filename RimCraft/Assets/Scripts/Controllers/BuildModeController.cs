using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour {
    bool buildModeIsObjects = false;            // Устанавливаем ли мы объекты или правим тайлы
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    // Use this for initialization
    void Start () {

	}
	
    public void SetMode_BuildFloor()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Floor;
    }

    public void SetMode_Bulldozer()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

    // Метод для кнопки
    // Генерирует тестовый мир
    public void BuildPathfindingTestsWorld()
    {
        WorldController.Instance.world.SetupPathfindingExample();

        Path_TileGraph tileGraph = new Path_TileGraph(WorldController.Instance.world);
    }

    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects == true)
        {
            // Режим установки объектов.
            // Устанавливаем объект и назначаем тайл для него

            // FIXME: следующая строка создает фурнитуру немедленно
            // WorldController.Instance.World.PlaceFurniture(buildModeObjectType, t);

            // FIXME: Следующиую проверку нужно делать не здесь!
            // Проверяем а можно ли вообще в этот тайл установить задание на работу
            string furnitureType = buildModeObjectType;
            if (WorldController.Instance.world.IsFurniturePlacmentVaild(furnitureType, t) == true
                && t.pendingFurnitureJob == null)
            {
                //Тайл пригоден к установки задания на работу
                // Добавляем работу по установки фурнитуры в очередь

                Job j = new Job(t, furnitureType, (theJob) =>
                {
                    WorldController.Instance.world.PlaceFurniture(furnitureType, theJob.tile);
                    t.pendingFurnitureJob = null;
                });

                // FIXME: Это ручной способ. Надо чтобы всё происходило автоматически
                t.pendingFurnitureJob = j;

                j.RegisterJobCancelCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });

                //Временное решение: мнгновенное выполнение работы
                WorldController.Instance.world.jobQueue.Enqueue(j);

                //Debug.Log("Размер очереди на выполнение работ: " + WorldController.Instance.world.jobQueue.Count);
            }
        }
        else
        {
            // Режим изменения тайлов

            t.Type = buildModeTile;
        }
    }
}