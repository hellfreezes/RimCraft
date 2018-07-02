using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpriteController : MonoBehaviour {

    private Dictionary<Character, GameObject> characterGameObjectMap; // Связка между персонажами и их игровыми объектами

    private Dictionary<string, Sprite> charaterSprites;

    World world
    {
        get { return WorldController.Instance.world; }
    }

    // Use this for initialization
    void Start () {
        //LoadSprites();

	}
}
