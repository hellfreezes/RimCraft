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

    void Start()
    {
        LoadSprites();

        characterGameObjectMap = new Dictionary<Character, GameObject>();

        world.RegisterCharacterCreated(OnCharacterCreated);

        //Проверить может есть существующе персонажи. Если что вызвать коллбэк
        foreach (Character c in world.characters)
        {
            OnCharacterCreated(c);
        }
    }

    void LoadSprites()
    {
        charaterSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Characters/");
        foreach (Sprite s in sprites)
        {
            charaterSprites.Add(s.name, s);
        }
    }

    // Подписчик на событие в World, которое происходит когда фурнитура создается
    void OnCharacterCreated(Character character)
    {
        //Debug.Log("OnInstalledObjectCreated");
        //FIXME: не учитывает возможность находится на нескольких тайлах

        // Визуальная часть создания нового объекта
        // Объект создан. Пора назначить ему GameObject

        GameObject obj_go = new GameObject();

        //Добавляем связь GameObject и экземпляра в словарь
        characterGameObjectMap.Add(character, obj_go);
        obj_go.name = "Character";
        obj_go.transform.position = new Vector3(character.X, character.Y, 0);
        //obj_go.transform.SetParent(this.transform, true);

        SpriteRenderer spriteRenderer = obj_go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = charaterSprites["p1_front"];
        spriteRenderer.sortingLayerName = "Character";

        // Подписывает метод OnTileTypeChanged тайл на событие изменения tile_data. 
        // Если событие изменения происходит в tile_data, то вызывается метод OnTileTypeChanged
        character.RegisterOnCharacterChangedCallback(OnCharacterChanged);
    }

    // Подписчик на событие в Furniture, которое вызывается когда что либо в фурнитуре меняется
    void OnCharacterChanged(Character character)
    {
        // Меняем графику если это необходимо
        if (characterGameObjectMap.ContainsKey(character) == false)
        {
            Debug.LogError("Попытка изменить игровой объект персонажа которого нет в словаре");
            return;
        }

        GameObject char_go = characterGameObjectMap[character];


        char_go.transform.position = new Vector3(character.X, character.Y, 0f);
    }
}
