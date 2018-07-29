using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    [SerializeField]
    GameObject circleCursorPrefab;              // Объект курсор
    [SerializeField]
    GameObject buildModeControllerGO;

    FurnitureSpriteController fsc;
    BuildModeController buildModeController;
    Vector3 lastFramePosition;                  // Позиция мыши взятая из предыдущего кадра
    Vector3 currFramePosition;                  // Позиция мыши в данный момент

    Vector3 dragStartPosition;                  // Позиция мыши с которой начато перетаскивание
    List<GameObject> dragPreviewGameObjects;    // Лист хранящий в себе маркеры выделенных тайлов
    bool isDragging = false;

    enum MouseMode
    {
        SELECT,
        BUILD
    };

    MouseMode currentMode = MouseMode.SELECT;

    // Use this for initialization
    void Start () {
        fsc = FindObjectOfType<FurnitureSpriteController>();
        dragPreviewGameObjects = new List<GameObject>();
        buildModeController = buildModeControllerGO.GetComponent<BuildModeController>();
	}

    /// <summary>
    /// Получаем координаты мыши в мире
    /// </summary>
    public Vector3 GetMousePosition()
    {
        return currFramePosition;
    }

    public Tile GetMouseOverTile()
    {
        return WorldController.Instance.GetTileAtWorldCoord(currFramePosition);
            //Mathf.FloorToInt(currFramePosition.x), 
            //Mathf.FloorToInt(currFramePosition.y));
    }


	// Update is called once per frame
	void Update () {
        //Текущее положение мыши относительно мира
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;

        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (currentMode == MouseMode.BUILD)
            {
                currentMode = MouseMode.SELECT;
            } else if (currentMode == MouseMode.SELECT)
            {
                Debug.Log("Тут можно показать игровое меню");
            }
        }

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();

        //Записываем текущее положение мыши для следующего кадра (в след кадре - это будет уже предыдущее положение мыши)
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    // Обработка нажатия левой кнопки мыши
    private void UpdateDragging()
    {
        // Если мышь над элементом интерфейса, то отменяем выполнение
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Подчищаем ненужные копии маркеров каждый фрейм
        while (dragPreviewGameObjects.Count > 0)
        {
            // TODO: не самое лучше решение на мой взгляд
            // Находит ВСЕ маркеры и деспавнит их КАЖДЫЙ фрейм
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (currentMode != MouseMode.BUILD)
            return;

        // Это обработка drag&drop. Обработка прямоугольной области содержащей тайлы

        // НАЧАЛО перетаскивания - фиксируем координаты тут
        if (Input.GetMouseButtonDown(0)) // нажата в предыдущем фрейме
        {
            //Запоминаем позицию с которой начали перетаскивание
            dragStartPosition = currFramePosition;

            isDragging = true;
        } else if (isDragging == false)
        {
            dragStartPosition = currFramePosition;
        }

        if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.Escape))
        {
            // ПРАВАЯ кл. мыши отпущена, перетаскивание закончено.
            isDragging = false;
        }

        // Непозволяем пользоваться перетаскиванием в строительно режиме
        // если объект нельзя перетаскивать (растягивать). Если объект больше чем 1х1 тайл
        if (buildModeController.IsObjectDraggable() == false)
        {
            dragStartPosition = currFramePosition;
        }

        // Координаты начала перетаскивания и конца перетаскивания (из них строится прямоугольная область)
        int start_x = Mathf.FloorToInt(dragStartPosition.x + 0.5f);
        int end_x = Mathf.FloorToInt(currFramePosition.x + 0.5f);
        int start_y = Mathf.FloorToInt(dragStartPosition.y + 0.5f);
        int end_y = Mathf.FloorToInt(currFramePosition.y + 0.5f);

        // Если конечный x меньше начального, то меняем их значения местами 
        if (end_x < start_x)
        {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        // Если конечный y меньше начального, то меняем их значения местами 
        if (end_y < start_y)
        {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }

        //if (isDragging) // это процесс перетаскивания с зажатой лв. клавишей мыши
        //{
            // Отобразить сетку подсказок, какие тайлы попали в область выделения
            // Перебираем все тайлы попавшие в прямоугольную область описанную перетаскиванием
            // Отображаем подсказку
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    // Отображаем подсказку на каждом тайле - это маркер в данный момент в виде кружка
                    // Визуализация маркера происходит с помощью отображения копии circleCursorPrefab
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);
                    if (t != null)
                    {
                        if (buildModeController.buildMode == BuildMode.FURNITURE) // Если устанавливаем объект (фурнитуру)
                        {
                            ShowFurnitureSpriteAtCoordinates(buildModeController.buildModeObjectType, t);
                        } else // устанавливаем всё остальное
                        {
                            // показываем перетаскивание
                            GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                            go.transform.SetParent(this.transform, true);
                            dragPreviewGameObjects.Add(go);
                        }
                    }
                }
            }
        //}

        // КОНЕЦ перетаскивания - фиксируем тут координаты
        if (isDragging && Input.GetMouseButtonUp(0)) // отпущена
        {
            isDragging = false;
            // Перебираем все тайлы попавшие в прямоугольную область описанную перетаскиванием
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    // Меняем тип тайла на ПОЛ
                    Tile t = WorldController.Instance.world.GetTileAt(x, y);

                    if (t != null)
                    {
                        //Попытка дать комманду
                        buildModeController.DoBuild(t);
                    }
                }
            }
        }
    }

    // Перетаскивание камеры мышкой
    private void UpdateCameraMovement()
    {
        if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) //Нажата правая или средняя клавиша мыши
        {
            //Разница между предыдущим и текущим положением мыши
            Vector3 diff = lastFramePosition - currFramePosition;

            Camera.main.transform.Translate(diff);
        }

        Camera.main.orthographicSize -= Camera.main.orthographicSize * Input.GetAxis("Mouse ScrollWheel");
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 3f, 25f);
    }

    /// <summary>
    /// Показывает объект перед его установкой (Предпросмотр). Подкрашивает его в зависимости от возможности
    /// установки
    /// </summary>
    /// <param name="furnitureType">Тип объекта</param>
    /// <param name="t">В каком тайле показать</param>
    void ShowFurnitureSpriteAtCoordinates(string furnitureType, Tile t)
    {
        GameObject go = new GameObject();
        go.transform.SetParent(this.transform, true);
        dragPreviewGameObjects.Add(go);

        SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();

        //Тут создается спрайт для предпросмотра того, что будет построено.
        spriteRenderer.sprite = fsc.GetSpriteForFurniture(furnitureType);
        spriteRenderer.sortingLayerName = "Jobs";

        if (WorldController.Instance.world.IsFurniturePlacmentVaild(furnitureType, t))
        {
            spriteRenderer.color = new Color(0.5f, 1f, 0.5f, 0.25f); // Установка разрешена
        }
        else
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0.5f, 0.25f); // Установка запрещена
        }

        Furniture proto = WorldController.Instance.world.furniturePrototypes[furnitureType];

        // Позиционирование объекта с учетом поправки на мультитайловость
        go.transform.position = new Vector3(t.X + ((proto.Width - 1) / 2f), t.Y + ((proto.Height - 1) / 2f), 0);
    }

    public void StartBuildMode()
    {
        currentMode = MouseMode.BUILD;
    }
}
