﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour {
    [SerializeField]
    GameObject circleCursorPrefab;              // Объект курсор

    bool buildModeIsObjects = false;            // Устанавливаем ли мы объекты или правим тайлы
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    Vector3 lastFramePosition;                  // Позиция мыши взятая из предыдущего кадра
    Vector3 currFramePosition;                  // Позиция мыши в данный момент

    Vector3 dragStartPosition;                  // Позиция мыши с которой начато перетаскивание
    List<GameObject> dragPreviewGameObjects;    // Лист хранящий в себе маркеры выделенных тайлов

    // Use this for initialization
    void Start () {
        dragPreviewGameObjects = new List<GameObject>();
	}
	
	// Update is called once per frame
	void Update () {
        //Текущее положение мыши относительно мира
        currFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        currFramePosition.z = 0;

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();

        //Записываем текущее положение мыши для следующего кадра (в след кадре - это будет уже предыдущее положение мыши)
        lastFramePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
	}

    // Обновить положение курсора
    /*private void UpdateCursor()
    {
        Tile tileUnderMouse = GetTileAtWorldCoord(currFramePosition);
        if (tileUnderMouse != null)
        {
            circleCursor.SetActive(true);
            Vector3 cursorPosition = new Vector3(tileUnderMouse.X, tileUnderMouse.Y, 0);
            circleCursor.transform.position = cursorPosition;
        }
        else
        {
            circleCursor.SetActive(false);
        }
    }*/

    // Обработка нажатия левой кнопки мыши
    private void UpdateDragging()
    {
        // Если мышь над элементом интерфейса, то отменяем выполнение
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Это обработка drag&drop. Обработка прямоугольной области содержащей тайлы
        // Начало перетаскивания - фиксируем координаты тут
        if (Input.GetMouseButtonDown(0)) // нажата в предыдущем фрейме
        {
            //Запоминаем позицию с которой начали перетаскивание
            dragStartPosition = currFramePosition;

        }

        // Координаты начала перетаскивания и конца перетаскивания (из них строится прямоугольная область)
        int start_x = Mathf.FloorToInt(dragStartPosition.x);
        int end_x = Mathf.FloorToInt(currFramePosition.x);
        int start_y = Mathf.FloorToInt(dragStartPosition.y);
        int end_y = Mathf.FloorToInt(currFramePosition.y);

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


        // Подчищаем ненужные копии маркеров каждый фрейм
        while(dragPreviewGameObjects.Count > 0)
        {
            // TODO: не самое лучше решение на мой взгляд
            // Находит ВСЕ маркеры и деспавнит их КАЖДЫЙ фрейм
            GameObject go = dragPreviewGameObjects[0];
            dragPreviewGameObjects.RemoveAt(0);
            SimplePool.Despawn(go);
        }

        if (Input.GetMouseButton(0)) // всё еще нажата - это процесс перетаскивания с зажатой лв. клавишей мыши
        {
            // Отобразить сетку подсказок, какие тайлы попали в область выделения
            // Перебираем все тайлы попавшие в прямоугольную область описанную перетаскиванием
            // Отображаем подсказку
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    // Отображаем подсказку на каждом тайле - это маркер в данный момент в виде кружка
                    // Визуализация маркера происходит с помощью отображения копии circleCursorPrefab
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);
                    if (t != null)
                    {
                        GameObject go = SimplePool.Spawn(circleCursorPrefab, new Vector3(x, y, 0), Quaternion.identity);
                        go.transform.SetParent(this.transform, true);
                        dragPreviewGameObjects.Add(go);
                    }
                }
            }
        }

        // Конец перетаскивания - фиксируем тут координаты
        if (Input.GetMouseButtonUp(0)) // отпущена
        {
            // Перебираем все тайлы попавшие в прямоугольную область описанную перетаскиванием
            for (int x = start_x; x <= end_x; x++)
            {
                for (int y = start_y; y <= end_y; y++)
                {
                    // Меняем тип тайла на ПОЛ
                    Tile t = WorldController.Instance.World.GetTileAt(x, y);

                    if (t != null)
                    {
                        if (buildModeIsObjects == true)
                        {
                            // Режим установки объектов.
                            // Устанавливаем объект и назначаем тайл для него

                            // FIXME: пока что мы только устанавливаем стены и не имеем возможности установить что либо еще

                            WorldController.Instance.World.PlaceFurniture(buildModeObjectType, t);
                        }
                        else
                        {
                            // Режим изменения тайлов

                            t.Type = buildModeTile;
                        }
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
        Debug.Log("Wall");
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return WorldController.Instance.World.GetTileAt(x, y);
    }
}
