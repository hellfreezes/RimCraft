using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Xml;

/// <summary>
/// Данный класс загружает файлы изображений с диска и создает из них отсортированную коллекцию
/// </summary>
public class SpriteManager : MonoBehaviour {

    Dictionary<string, Sprite> sprites;

    public static SpriteManager current;

    // Use this for initialization
    private void OnEnable()
    {
        current = this;

        LoadSprites();
    }

    void LoadSprites()
    {
        sprites = new Dictionary<string, Sprite>();

        string filePath = Path.Combine(Application.streamingAssetsPath, "Images");

        LoadSpritesFromDirectory(filePath);
    }

    void LoadSpritesFromDirectory(string filePath)
    {
        Debug.Log("Загружаются спрайты из директории: " + filePath);
        // Скинруем на предмет субдиректорий и если так то выполняем этот же метод для них
        string[] subDirectories = Directory.GetDirectories(filePath);
        foreach(string subDirectory in subDirectories)
        {
            LoadSpritesFromDirectory(subDirectory);
        }

        // Тут загружаем файлы
        string[] filesInDirectory = Directory.GetFiles(filePath);
        foreach (string file in filesInDirectory)
        {
            LoadImage(file);
        }
    }

    void LoadImage(string filePath)
    {
        byte[] imageBytes = File.ReadAllBytes(filePath);

        Texture2D imageTexture = new Texture2D(1, 1); // размер не важен

        //FIXME:
        // Временный костыль из за того что LoadImage всегда возвращает true
        if (filePath.Contains(".xml") || filePath.Contains(".meta"))
        {
            return;
        }

        if (imageTexture.LoadImage(imageBytes))
        {
            // Изображение загружено
            // Пробуем найти xml файл с описанием как вытаскивать оттуда спрайты
            string baseSpriteName = Path.GetFileNameWithoutExtension(filePath);
            string basePath = Path.GetDirectoryName(filePath);

            string xmlPath = Path.Combine(basePath, baseSpriteName + ".xml");
            try
            {
                string xmlText = File.ReadAllText(xmlPath);
                XmlTextReader reader = new XmlTextReader(new StringReader(xmlText));

                if (reader.ReadToDescendant("Sprites") && reader.ReadToDescendant("Sprite"))
                {
                    do
                    {
                        ReadSpriteFromXml(reader, imageTexture);
                    } while (reader.ReadToNextSibling("Sprite"));
                } else
                {
                    Debug.LogError("Sprite не найден в xml файле");
                }
            } catch
            {
                // Невозможно прочесть файл. Инструкции по вычленению спрайтов отсутствуют
                LoadSprite(baseSpriteName, imageTexture, new Rect(0, 0, imageTexture.width, imageTexture.height), 32);
            }
        }

    }

    void ReadSpriteFromXml(XmlReader reader, Texture2D imageTexture)
    {
        string spriteName = reader.GetAttribute("name");
        //TODO: написать обработчик ошибок парсинга
        int x = int.Parse(reader.GetAttribute("x"));
        int y = int.Parse(reader.GetAttribute("y"));
        int w = int.Parse(reader.GetAttribute("w"));
        int h = int.Parse(reader.GetAttribute("h"));
        int pixelsPerUnit = int.Parse(reader.GetAttribute("pixelsPerUnit"));

        LoadSprite(spriteName, imageTexture, new Rect(x, y, w, h), pixelsPerUnit);
    }

    void LoadSprite(string spriteName, Texture2D imageTexture, Rect spriteCoordinates, int pixelsPerUnity)
    {
        Vector2 pivotPoint = new Vector2(0.5f, 0.5f); // середина

        Sprite s = Sprite.Create(imageTexture, spriteCoordinates, pivotPoint, pixelsPerUnity);
        sprites[spriteName] = s;
    }

    public Sprite GetSprite(string spriteName)
    {
        if (sprites.ContainsKey(spriteName) == false)
        {
            //Debug.LogError("Спрайт с имененм " + spriteName + " отсутсвует в коллекции");
            return null;
        }

        return sprites[spriteName];
    }
}
