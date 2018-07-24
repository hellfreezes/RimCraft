using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpriteController : MonoBehaviour {
    [SerializeField]
    private GameObject inventoryUIPrefab;

    private Dictionary<Inventory, GameObject> inventoryGameObjectMap;
    private Dictionary<string, Sprite> inventorySprites;

    private World world
    {
        get { return WorldController.Instance.world; }
    }

    void Start()
    {
        LoadSprites();

        inventoryGameObjectMap = new Dictionary<Inventory, GameObject>();

        world.RegisterInventoryCreated(OnInventoryCreated);

        //Проверить может есть существующе объекты. Если что вызвать коллбэк
        foreach (string objectType in world.inventoryManager.inventories.Keys)
        {
            foreach (Inventory inv in world.inventoryManager.inventories[objectType])
            {
                OnInventoryCreated(inv);
            }
        }
    }

    void LoadSprites()
    {
        inventorySprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Images/Inventory/");
        foreach (Sprite s in sprites)
        {
            inventorySprites.Add(s.name, s);
        }
    }

    // Подписчик на событие в World, которое происходит объект создается
    void OnInventoryCreated(Inventory inv)
    {
        //FIXME: не учитывает возможность находится на нескольких тайлах

        // Визуальная часть создания нового объекта
        // Объект создан. Пора назначить ему GameObject

        GameObject inv_go = new GameObject();

        //Добавляем связь GameObject и экземпляра в словарь
        inventoryGameObjectMap.Add(inv, inv_go);
        inv_go.name = inv.objectType;
        inv_go.transform.position = new Vector3(inv.tile.X, inv.tile.Y, 0);
        inv_go.transform.SetParent(this.transform, true);

        SpriteRenderer spriteRenderer = inv_go.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = inventorySprites[inv.objectType];
        spriteRenderer.sortingLayerName = "Inventory";

        if (inv.stackSize > 1)
        {
            // Стак. Надо отобразить количество предметов в стаке
            GameObject ui_go = GameObject.Instantiate(inventoryUIPrefab);
            ui_go.transform.SetParent(inv_go.transform);
            ui_go.transform.localPosition = Vector3.zero;
            ui_go.GetComponentInChildren<Text>().text = inv.stackSize.ToString();
            
        }

        inv.RegisterChangeCallback(OnInventoryChanged);
    }

    // Подписчик на событие в Furniture, которое вызывается когда что либо в фурнитуре меняется
    void OnInventoryChanged(Inventory inv)
    {
        

        // Меняем графику если это необходимо
        if (inventoryGameObjectMap.ContainsKey(inv) == false)
        {
            Debug.LogError("Попытка изменить игровой объект которого нет в словаре");
            return;
        }

        GameObject inv_go = inventoryGameObjectMap[inv];

        // Если в стаке остался хотя бы один предмет, то
        if (inv.stackSize > 0)
        {
            Text text = inv_go.GetComponentInChildren<Text>();
            // FIXME: если кол во предметов в стаке равно 1, то text.text должно быть равно ""
            if (text != null)
            {
                text.text = inv.stackSize.ToString();
            }
        } else
        {
            // Если в стаке не осталось предметов, то удаляем его визуальную составляющую
            Destroy(inv_go);
            inventoryGameObjectMap.Remove(inv);
            inv.UnregisterChangeCallback(OnInventoryChanged);
        }
    }
}
