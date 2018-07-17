﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class MouseOverTileTypeText : MonoBehaviour {
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
        text.text = "Tile Type: " + t.Type.ToString();
	}
}
