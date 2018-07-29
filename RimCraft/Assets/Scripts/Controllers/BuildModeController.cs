using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum BuildMode
{
    FLOOR,
    FURNITURE,
    DECONSTRUCT
}

public class BuildModeController : MonoBehaviour {
    public BuildMode buildMode = BuildMode.FLOOR;            // Устанавливаем ли мы объекты или правим тайлы
    TileType buildModeTile = TileType.Floor;


    public string buildModeObjectType;

    //GameObject furniturePreview;
    //FurnitureSpriteController fsc;

    MouseController mouseController;

    // Use this for initialization
    void Start () {
        //fsc = FindObjectOfType<FurnitureSpriteController>();
        mouseController = FindObjectOfType<MouseController>();

        //// Заготовка под предпросмотор устанавливаемого объекта. Под мышку
        //furniturePreview = new GameObject();
        //furniturePreview.transform.SetParent(this.transform);
        //furniturePreview.AddComponent<SpriteRenderer>().sortingLayerName = "Jobs";
        //furniturePreview.SetActive(false);
    }

    //private void Update()
    //{
    //    if (buildModeIsObjects == true && buildModeObjectType != null & buildModeObjectType != "")
    //    {
    //        // Отобразить прозрачный образец устанавливаемого объекта
    //        // подкрашенный цветом в зависимости от того возможна ли его установка здесь или нет
    //        ShowFurnitureSpriteAtCoordinates(buildModeObjectType, mouseController.GetMouseOverTile());
    //    } else
    //    {   // Если мы не в режиме постройки объектов, то не показываем предпросмотр
    //        furniturePreview.SetActive(false);
    //    }
    //}

    ///// <summary>
    ///// Показывает объект перед его установкой (Предпросмотр). Подкрашивает его в зависимости от возможности
    ///// установки
    ///// </summary>
    ///// <param name="furnitureType">Тип объекта</param>
    ///// <param name="t">В каком тайле показать</param>
    //void ShowFurnitureSpriteAtCoordinates(string furnitureType, Tile t)
    //{
    //    furniturePreview.SetActive(true);

    //    SpriteRenderer spriteRenderer = furniturePreview.GetComponent<SpriteRenderer>();

    //    //Тут создается спрайт для предпросмотра того, что будет построено.
    //    spriteRenderer.sprite = fsc.GetSpriteForFurniture(furnitureType);

    //    if (WorldController.Instance.world.IsFurniturePlacmentVaild(furnitureType, t))
    //    {
    //        spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f); // Установка разрешена
    //    } else
    //    {
    //        spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.25f); // Установка запрещена
    //    }

    //    Furniture proto = WorldController.Instance.world.furniturePrototypes[furnitureType];

    //                                                            // Позиционирование объекта с учетом поправки на мультитайловость
    //    furniturePreview.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    //}

    /// <summary>
    /// Проверяет на возможность устанавливать объект перетаскиванием (сразу много объектов)
    /// Привязка идет к размеру объекта. Если он 1х1 тайл, то перетаскивать его можно.
    /// </summary>
    /// <returns></returns>
    public bool IsObjectDraggable()
    {
        if (buildMode == BuildMode.FLOOR || buildMode == BuildMode.DECONSTRUCT)
        {
            // пол можно перетаскивать
            return true;
        }
        Furniture proto = WorldController.Instance.world.furniturePrototypes[buildModeObjectType];

        return proto.Width == 1 && proto.Height == 1;
    }

    public void SetMode_BuildFloor()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Floor;

        mouseController.StartBuildMode();
    }

    public void SetMode_Bulldozer()
    {
        buildMode = BuildMode.FLOOR;
        buildModeTile = TileType.Empty;

        mouseController.StartBuildMode();
    }

    public void SetMode_BuildFurniture(string objectType)
    {
        buildMode = BuildMode.FURNITURE;
        buildModeObjectType = objectType;

        mouseController.StartBuildMode();
    }

    public void SetMode_Deconstruct()
    {
        buildMode = BuildMode.DECONSTRUCT;
        mouseController.StartBuildMode();
    }

    // Метод для кнопки
    // Генерирует тестовый мир
    public void BuildPathfindingTestsWorld()
    {
        WorldController.Instance.world.SetupPathfindingExample();

        //Path_TileGraph tileGraph = new Path_TileGraph(WorldController.Instance.world);
    }

    public void DoBuild(Tile t)
    {
        if (buildMode == BuildMode.FURNITURE) // Установка фурнитуры
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

                Job j;

                if (WorldController.Instance.world.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    // Тут необходимо клонировать прототип
                    j = WorldController.Instance.world.furnitureJobPrototypes[furnitureType].Clone();
                    // Прототип не имеет тайла указывающего где работа, надо назначить!
                    j.tile = t;
                }
                else
                {
                    Debug.Log("Не задан прототип работы для создания сооружения: "+ furnitureType+". Применен дефолтный прототип.");
                    j = new Job(t, furnitureType, FurnitureActions.JobComlete_FurnitureBuilding,
                    0.1f,
                    null);
                }

                j.furniturePrototype = WorldController.Instance.world.furniturePrototypes[furnitureType];

                // FIXME: Это ручной способ. Надо чтобы всё происходило автоматически
                t.pendingFurnitureJob = j;

                j.RegisterJobStoppedCallback((theJob) => { theJob.tile.pendingFurnitureJob = null; });

                //Временное решение: мнгновенное выполнение работы
                WorldController.Instance.world.jobQueue.Enqueue(j);

                //Debug.Log("Размер очереди на выполнение работ: " + WorldController.Instance.world.jobQueue.Count);
            }
        }
        else if (buildMode == BuildMode.FLOOR) // Установка пола
        {
            // Режим изменения тайлов

            t.Type = buildModeTile;
        }
        else if (buildMode == BuildMode.DECONSTRUCT) // Демонтаж фурнитуры
        {
            if (t.furniture != null)
            {
                t.furniture.Decostruct(); // Вызываем встроенный метод в фурнитуру
            }
        } else
        {
            Debug.LogError("Неустановленный режим");
        }
    }
}