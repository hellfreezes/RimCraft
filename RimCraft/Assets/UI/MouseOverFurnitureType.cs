using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class MouseOverFurnitureType : MonoBehaviour {
    // Проверяет какой тайл под мышкой и передает наименование в Text
    // выбанный в этот компонент
    private Text text;
    private MouseController mouseController;

    private void Start()
    {
        text = GetComponent<Text>();

        if (text == null)
        {
            Debug.LogError("No Text.UI component at this GO");
            this.enabled = false; // отключаем этот скрипт
        }

        mouseController = GameObject.FindObjectOfType<MouseController>();
        if (mouseController == null)
        {
            Debug.LogError("MouseController не найден");
            return;
        }
    }

    void Update () {
        Tile t = mouseController.GetMouseOverTile();

        string s = "null";

        if (t.furniture != null)
        {
            s = t.furniture.Name;
        }
        text.text = "Furniture: " + s;
	}
}
