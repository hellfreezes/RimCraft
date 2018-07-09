using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * Класс юнити, управляющий нашим миром
 */
public class WorldController : MonoBehaviour {
    public static WorldController instance;

    public World world { get; protected set; }

    public static WorldController Instance
    {
        get
        {
            return instance;
        }
    }

    // Use this for initialization
    void OnEnable() {
        //Создаем прямой доступ к единственному экземпляру
        if (instance != null)
            Debug.LogError("На сцене больше одного экземпляра WorldController");
        instance = this;

        CreateEmptyWorld();
    }

    // Update is called once per frame
    void Update() {
        //Тут можно управлять скоростью игры. вплоть до остановки
        world.Update(Time.deltaTime);
    }

    public Tile GetTileAtWorldCoord(Vector3 coord)
    {
        int x = Mathf.FloorToInt(coord.x);
        int y = Mathf.FloorToInt(coord.y);

        return world.GetTileAt(x, y);
    }

    void CreateEmptyWorld()
    {
        //Create a world with empty Tiles
        world = new World();

        // Центруем камеру
        Camera.main.transform.position = new Vector3(world.Width / 2, world.Height / 2, Camera.main.transform.position.z);
    }

    public void NewWorld()
    {
        Debug.Log("Попытка создать новый мир");

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SaveWorld()
    {
        Debug.Log("Попытка сохранить мир");
    }

    public void LoadWorld()
    {
        Debug.Log("Попытка загрузить мир");

        // Перезагружаем сцену сначала. Чтобы уничтожить все ранее созданные данные
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
