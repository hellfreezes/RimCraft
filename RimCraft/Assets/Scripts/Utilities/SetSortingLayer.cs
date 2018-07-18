using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SetSortingLayer : MonoBehaviour {

    [SerializeField]
    private string sortingLayerName = "Default";

	// Use this for initialization
	void Start () {
        GetComponent<Renderer>().sortingLayerName = sortingLayerName;
	}
}
