using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureBuildMenu : MonoBehaviour {

    [SerializeField]
    GameObject buildFurnitureButton;

	// Use this for initialization
	void Start () {
        BuildModeController bmc = FindObjectOfType<BuildModeController>();
        AutomaticVerticalSize avs = GetComponent<AutomaticVerticalSize>();

	    // Добавить в интерфейс кнопку для постройки каждого типа сооружения
        
        // Для каждого прототипа фурнитуры в мире создать копию кнопки из префаба
        foreach(string s in World.current.furniturePrototypes.Keys)
        {
            GameObject go = Instantiate(buildFurnitureButton);
            go.transform.SetParent(this.transform);

            string objectId = s;
            string objectName = World.current.furniturePrototypes[s].Name;

            go.name = "Button - Build " + objectId;
            Text t = go.GetComponentInChildren<Text>();
            t.text = "Build " + objectName;

            Button b = go.GetComponent<Button>();

            b.onClick.AddListener(delegate { bmc.SetMode_BuildFurniture(objectId); });
        }

        avs.AdjustSize();
	}
}
