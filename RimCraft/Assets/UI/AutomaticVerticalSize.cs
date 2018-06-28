using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Класс исполняется в редакторе юнити
[ExecuteInEditMode]
public class AutomaticVerticalSize : MonoBehaviour {
    [SerializeField]
    float childHeight = 35f; // высота кнопок


	// Use this for initialization
	void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // Изменяем высоту объекта-контейнера, к которому применен данный класс
    // исходя из количества кнопок-детей в нем
    public void AdjustSize()
    {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        size.y = this.transform.childCount * childHeight;

        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
