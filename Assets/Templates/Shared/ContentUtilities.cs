using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ContentUtilities : Editor
{
    public static string sourceFolder = Application.dataPath + "/" + "Templates";

    public enum ColliderShape
    {
        Box = 0,
        Circle,
        Polygon,
        Capsule,
        None,
        Max
    }

    public enum Corner
    {
        TopLeft = 0,
        TopRight,
        BottomRight,
        BottomLeft,
        Max
    }

    public enum Side
    {
        Top = 0,
        Right,
        Bottom,
        Left,
        Max
    }

    public enum Anchor
    {
        Center = 0,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Max
    }

    public static void AnchorUIObject(GameObject uiObject, Transform parent, Anchor anchor, Vector2 offset)
    {
        // Get RectTransform
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();

        // Set parent
        rectTransform.SetParent(parent);
        // Set anchor
        switch (anchor)
        {
            case Anchor.Top:
                rectTransform.anchorMin = new Vector2(0.5f, 1.0f);
                rectTransform.anchorMax = new Vector2(0.5f, 1.0f);
                rectTransform.pivot = new Vector2(0.5f, 1.0f);
                break;

            case Anchor.Bottom:
                rectTransform.anchorMin = new Vector2(0.5f, 0.0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.0f);
                rectTransform.pivot = new Vector2(0.5f, 0.0f);
                break;

            case Anchor.Left:
                rectTransform.anchorMin = new Vector2(0.0f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.0f, 0.5f);
                rectTransform.pivot = new Vector2(0.0f, 0.5f);
                break;

            case Anchor.Right:
                rectTransform.anchorMin = new Vector2(1.0f, 0.5f);
                rectTransform.anchorMax = new Vector2(1.0f, 0.5f);
                rectTransform.pivot = new Vector2(1.0f, 0.5f);
                break;

            case Anchor.TopLeft:
                rectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                rectTransform.anchorMax = new Vector2(0.0f, 1.0f);
                rectTransform.pivot = new Vector2(0.0f, 1.0f);
                break;

            case Anchor.TopRight:
                rectTransform.anchorMin = new Vector2(1.0f, 1.0f);
                rectTransform.anchorMax = new Vector2(1.0f, 1.0f);
                rectTransform.pivot = new Vector2(1.0f, 1.0f);
                break;

            case Anchor.BottomLeft:
                rectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                rectTransform.anchorMax = new Vector2(0.0f, 0.0f);
                rectTransform.pivot = new Vector2(0.0f, 0.0f);
                break;

            case Anchor.BottomRight:
                rectTransform.anchorMin = new Vector2(1.0f, 0.0f);
                rectTransform.anchorMax = new Vector2(1.0f, 0.0f);
                rectTransform.pivot = new Vector2(1.0f, 0.0f);
                break;

            default:
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        // Set anchored position
        rectTransform.anchoredPosition = new Vector2(offset.x, offset.y);
    }

    public static int[] ConvertContainerLayoutToMapArray(GameObject container, GameObject[] childrenGuide, int mapWidth, int mapHeight, int startX = 0, int startY = 0, float scaleX = 1.0f, float scaleY = 1.0f, int defaultValue = -1)
    {
        // This method go through the content of a container (with children) and convert the children position and-
        // type into an integer map array.
        // This map array can be used to recreate their map position from inside a Game Template generator.

        int[] mapArray = new int[mapWidth * mapHeight];

        // Initialize every element to default value
        for (int i = 0; i < mapArray.Length; i++)
        {
            mapArray[i] = defaultValue;
        }

        // Go through the children of container
        for (int i = 0; i < container.transform.childCount; i++)
        {
            // Get child
            GameObject child = container.transform.GetChild(i).gameObject;
            // Resolve array index from the child's scaled position
            int x = (int)(child.transform.localPosition.x / scaleX) - startX;
            int y = (int)(child.transform.localPosition.y / scaleY) - startY;
            int index = y * mapWidth + x;
            // Resolve the type from the given guide
            int type = defaultValue;
            for (int j = 0; j < childrenGuide.Length; j++)
            {
                GameObject current = childrenGuide[j];
                if (current.name == child.name)
                {
                    type = j;
                    break;
                }
            }

            // Resolve the child type from the guide
            mapArray[index] = type;
        }
        return mapArray;
    }

    public static StringBuilder ConvertMapArrayToString(int[] mapArray, int w, int h)
    {
        // Print out to console
        StringBuilder sb = new StringBuilder();
        sb.Append("Array size (" + w + ", " + h + ")\n");
        for (int y = 0; y < h; y++)
        {
            // Make a line of values
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                sb.Append(string.Format("{0,3:D}", mapArray[index]));
                if (index < mapArray.Length)
                {
                    sb.Append(",");
                }
            }
            sb.Append("\n");
        }
        return sb;
    }

    public static int[] ConvertTilemapToMapArray(Tilemap tilemap, Tile[] tilesGuide, int w, int h, int startX = 0, int startY = 0, int defaultValue = -1)
    {
        // This method go through the content of a tilemap and convert it into a coded integer mapArray
            // This mapArray can then be used to re-create the tilemap from inside a Game Template generator.
        Vector3Int marker = Vector3Int.zero;
        int[] mapArray = new int[w * h];

        // Initialize every element to default value
        for (int i = 0; i < mapArray.Length; i++)
        {
            mapArray[i] = defaultValue;
        }

        // Go through the rows
        for (int y = 0; y < h; y++)
        {
            // Go through the columns
            for (int x = 0; x < w; x++)
            {
                // Get the tile in the Tilemap
                marker.x = startX + x;
                marker.y = startY + y;
                Tile tile = tilemap.GetTile(marker) as Tile;
                if (tile == null)
                {
                    continue;
                }
                // Resolve the type from the given guide
                int type = defaultValue;
                for (int j = 0; j < tilesGuide.Length; j++)
                {
                    Tile current = tilesGuide[j];
                    if (current.name == tile.name)
                    {
                        type = j;
                        break;
                    }
                }
                // If code not found, set to -1 to mark it as something invalid
                type = type >= tilesGuide.Length ? -1 : type;
                // Set code to the corresponding index in mapArray
                mapArray[y * w + x] = type;
            }
        }
        // Return
        return mapArray;
    }

    public static Color[] CopyBitmap(Color[] src, int srcWidth, int srcHeight, Color[] dst, int dstWidth, int dstHeight, Vector2Int bottomLeft)
    {
        Assert.IsNotNull(src);
        Assert.IsNotNull(dst);
        Assert.IsTrue(src.Length <= dst.Length);
        Assert.IsFalse(srcWidth < 0 || srcHeight < 0 || dstWidth < 0 || dstHeight < 0);
        Assert.IsFalse(srcWidth * srcHeight > src.Length || dstWidth * dstHeight > dst.Length);

        int startX = bottomLeft.x;
        int startY = bottomLeft.y;
        int srcIndex = 0;

        for (int i = startY; i < startY + srcHeight; i++)
        {
            for (int j = startX; j < startX + srcWidth; j++)
            {
                Color pixel = src[srcIndex];
                srcIndex++;
                if (pixel == Color.clear)
                {
                    continue;
                }
                int dstX = j;
                int dstY = i;
                int dstIndex = dstX + dstY * dstWidth;
                dst[dstIndex] = pixel;
            }
        }

        return dst;
    }

    public static Scene CreateAndSaveScene(string name, string assetsRelativePath)
    {
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        newScene.name = name;
        EditorSceneManager.SaveScene(newScene, "Assets/" + assetsRelativePath + "/" + name + ".unity");

        return newScene;
    }

    public static string CreateBitmapAsset(string name, Color[] bitmap, int width, int height, string path)
    {
        // Create 2D texture
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(bitmap);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static void CreateFolders(string templatePath, string[] subPaths)
    {
        string dataPath = Application.dataPath;

        // Create the template home folder (if it doesn't exist)
        Directory.CreateDirectory(dataPath + "/" + templatePath);
        // Create the subfolders
        foreach (string element in subPaths)
        {
            string path = dataPath + "/" + element;
            Directory.CreateDirectory(path);
        }
    }
    
    public static GameObject CreateTexturedBody(string name, float posX, float posY, string texturePath, ColliderShape shape = ColliderShape.Box)
    {
        // Create character game object
        GameObject character = new GameObject(name);
        // Add sprite renderer
        SpriteRenderer renderer = character.AddComponent<SpriteRenderer>() as SpriteRenderer;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + texturePath);
        renderer.sprite = sprite;
        // Add rigid body
        character.AddComponent<Rigidbody2D>();
        // Add collider
        switch (shape)
        {
            case ColliderShape.Box:
                character.AddComponent<BoxCollider2D>();
                break;
            case ColliderShape.Circle:
                character.AddComponent<CircleCollider2D>();
                break;
            case ColliderShape.Polygon:
                character.AddComponent<PolygonCollider2D>();
                break;
            case ColliderShape.Capsule:
                character.AddComponent<CapsuleCollider2D>();
                break;
            default:
                // No collider
                break;
        }


        // Position the character
        character.transform.position = new Vector2(posX, posY);

        return character;
    }

    public static GameObject CreateTexturedFigment(string name, float posX, float posY, string texturePath)
    {
        // Create game object
        GameObject newObject = new GameObject(name);
        newObject.transform.position = new Vector2(posX, posY);
        // Add sprite renderer
        SpriteRenderer renderer = newObject.AddComponent<SpriteRenderer>() as SpriteRenderer;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + texturePath);
        renderer.sprite = sprite;

        return newObject;
    }

    public static GameObject CreateHexagonalTilePaletteObject(string name, string outputFolderPath)
    {
        GameObject paletteGo = GridPaletteUtility.CreateNewPalette("Assets/" + outputFolderPath, name,
            GridLayout.CellLayout.Hexagon,
            GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
        return paletteGo;
    }

    public static GameObject CreateHexagonalTilemapObject(string name, GameObject gridObject = null)
    {
        if (gridObject == null)
        {
            gridObject = ObjectFactory.CreateGameObject("Grid", typeof(Grid));
            Grid grid = gridObject.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        }
        GameObject tilemapObject = ObjectFactory.CreateGameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.parent = gridObject.transform;
        tilemapObject.GetComponent<Tilemap>().tileAnchor = Vector3.zero;
        tilemapObject.GetComponent<TilemapRenderer>().mode = TilemapRenderer.Mode.Individual; 
        
        return tilemapObject;
    }

    public static string CreatePhysicsMaterial2D(string name, float bounciness, float friction, string assetsRelativePath)
    {
        PhysicsMaterial2D material = new PhysicsMaterial2D(name);
        string pathName = assetsRelativePath + "/" + name + ".physicsMaterial2D";
        material.bounciness = bounciness;
        material.friction = friction;

        AssetDatabase.CreateAsset(material, "Assets/" + pathName);
        
        // Return an asset relative path that can be used to load this asset
        return pathName;
    }

    public static void CreatePrefab(GameObject gameObject, string assetsRelativePath, bool destroyObject=true)
    {
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, "Assets/" + assetsRelativePath + "/" + gameObject.name + ".prefab", InteractionMode.UserAction);
        if (destroyObject == true)
        {
            DestroyImmediate(gameObject);
        }
    }

    public static string CreateSegmentTexture2DCorner(string name, string path, int width, int height, Color color, Corner cornerEnum, int thickness)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }

        // Draw shape (note: the pixel organization is bottom up)
        int startColumn = 0;
        int startRow = 0;
        int endColumn = 0;
        int endRow = 0;
        switch (cornerEnum)
        {
            case Corner.TopLeft:
                startColumn = width - thickness;
                startRow = 0;
                endColumn = width;
                endRow = thickness;
                break;
            case Corner.TopRight:
                startColumn = 0;
                startRow = 0;
                endColumn = thickness;
                endRow = thickness;
                break;
            case Corner.BottomLeft:
                startColumn = width - thickness;
                startRow = height - thickness;
                endColumn = width;
                endRow = height;
                break;
            default:
                startColumn = 0;
                startRow = height - thickness;
                endColumn = thickness;
                endRow = height;
                break;
        }

        for (int i = startRow; i < endRow; i++)
        {
            for (int j = startColumn; j < endColumn; j++)
            {
                colors[i * width + j] = color;
            }
        }
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateSegmentTexture2DHalf(string name, string path, int width, int height, Color color, Side sideEnum, int thickness)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }

        // Draw shape (note: the pixel organization is bottom up)
        int startColumn = 0;
        int startRow = 0;
        int endColumn = 0;
        int endRow = 0;
        switch (sideEnum)
        {
            case Side.Top:
                startColumn = 0;
                startRow = 0;
                endColumn = width;
                endRow = thickness;
                break;
            case Side.Right:
                startColumn = 0;
                startRow = 0;
                endColumn = thickness;
                endRow = height;
                break;
            case Side.Bottom:
                startColumn = 0;
                startRow = height - thickness;
                endColumn = width;
                endRow = height;
                break;
            default:
                startColumn = width - thickness;
                startRow = 0;
                endColumn = width;
                endRow = height;
                break;
        }

        for (int i = startRow; i < endRow; i++)
        {
            for (int j = startColumn; j < endColumn; j++)
            {
                colors[i * width + j] = color;
            }
        }
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DCircleAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set texture
        Color[] colors = FillBitmapShapeCircle(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DDiamondAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set texture
        Color[] colors = FillBitmapShapeDiamond(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DFrameAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set texture
        Color[] colors = FillBitmapShapeRectangleFrame(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DHexagonAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set to texture
        Color[] colors = FillBitmapShapeHexagon(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DOctagonAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set to texture
        Color[] colors = FillBitmapShapeOctagon(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DRectangleAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set to texture
        Color[] colors = FillBitmapShapeRectangle(width, height, color);
        texture.SetPixels(colors);

        // Save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTexture2DTriangleAsset(string name, string path, int width, int height, Color color)
    {
        // Create a texture
        Texture2D texture = new Texture2D(width, height);

        // Create a bitmap array and set to texture
        Color[] colors = FillBitmapShapeTriangle(width, height, color);
        texture.SetPixels(colors);

        // Encode and save it to path
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);

        // Return an asset relative path that can be used to load this asset
        return path + "/" + name + ".png";
    }

    public static string CreateTileAsset(string name, string spriteAssetPath, string outputFolderPath)
    {
        Sprite tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/" + spriteAssetPath);
        Tile tile = TileUtility.DefaultTile(tileSprite) as Tile;
        tile.name = name;
        AssetDatabase.CreateAsset(tile, "Assets/" + outputFolderPath + "/" + name + ".asset");

        // Return an asset relative path that can be used to load this asset
        return outputFolderPath + "/" + name + ".asset";
    }

    public static GameObject CreateTilePaletteObject(string name, string outputFolderPath)
    {
        GameObject paletteGo = GridPaletteUtility.CreateNewPalette("Assets/" + outputFolderPath, name,
            GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
        return paletteGo;
    }
    
    public static GameObject CreateTilemapObject(string name, GameObject gridObject = null)
    {
        if (gridObject == null)
        {
            gridObject = ObjectFactory.CreateGameObject("Grid", typeof(Grid));
        }        
        GameObject tilemap = ObjectFactory.CreateGameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        tilemap.transform.parent = gridObject.transform;
        return tilemap;
    }

    public static GameObject CreateUIBackgroundObject(string name, float width, float height, float alpha = 0.5f)
    {
        Color gray = Color.gray;
        // Create image object
        GameObject imageObject = new GameObject(name);
        Image imageComponent = imageObject.AddComponent<Image>();
        imageComponent.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        imageComponent.type = Image.Type.Sliced;
        imageComponent.color = new Color(gray.r, gray.g, gray.b, alpha);
        // Set RectTransform size
        RectTransform rectTransform = imageComponent.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        return imageObject;
    }

    public static GameObject CreateUIButtonObject(string name, float width, float height, string text, int fontSize, Color fontColor)
    {
        // Create button object
        GameObject buttonObject = new GameObject(name);
        buttonObject.AddComponent<RectTransform>();
        Button buttonComponent = buttonObject.AddComponent<Button>();
        buttonComponent.transition = Selectable.Transition.None;
        // Set Rect Transform dimensions
        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
        // Add image component
        Image imageComponent = buttonObject.AddComponent<Image>();
        imageComponent.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        imageComponent.type = Image.Type.Sliced;
        imageComponent.color = Color.gray;
        // Create text object child
        GameObject textObjectChild = new GameObject("Text");
        textObjectChild.AddComponent<RectTransform>();
        textObjectChild.transform.SetParent(buttonObject.transform);
        textObjectChild.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        // Add text component to child
        Text textComponent = textObjectChild.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = fontColor;
        textComponent.alignment = TextAnchor.MiddleCenter;

        return buttonObject;
    }

    public static GameObject CreateUICanvas()
    {
        // Create canvas object
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvasComponent = canvasObject.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        // Add event system
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();

        return canvasObject;
    }

    public static GameObject CreateUIImageObject(string name, float width, float height, string spritePath)
    {
        // Create image object
        GameObject imageObject = new GameObject(name);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = ContentUtilities.LoadSpriteAtPath(spritePath);
        // Set RectTransform size
        RectTransform rectTransform = image.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        return imageObject;
    }

    public static GameObject CreateUITextObject(string name, float width, float height, string text, TextAnchor textAnchor, int fontSize, Color fontColor)
    {
        // Create a text UI object
        GameObject newTextObject = new GameObject(name);
        Text textComponent = newTextObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = fontColor;
        textComponent.alignment = textAnchor;
        RectTransform rectTransform = newTextObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);

        return newTextObject;
    }

    public static Color[] FillBitmap(int width, int height, Color color)
    {
        Color[] bitmap = new Color[width * height];
        for (int i = 0; i < bitmap.Length; i++)
        {
            bitmap[i] = color;
        }

        return bitmap;
    }

    public static Color[] FillBitmapShapeCircle(int width, int height, Color color)
    {
        // color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }

        // Formula, x = sqrt(r*r - y*y)
        // Draw shape (note: the pixel organization is bottom up)
        bool isEven = width % 2 == 0;
        float r = isEven ? (float)width * 0.5f - 0.5f : (float)width * 0.5f;
        float rSquare = r * r;
        float ofs = isEven ? 0.5f : 0.0f;
        float cx = isEven ? width * 0.5f - 0.5f : width * 0.5f;
        for (int i = 0; i < height; i++)
        {
            float circleY = (float)i - r + ofs; // adjust y, formula assumes center at (0, 0)
            float fx = Mathf.Sqrt(rSquare - (circleY * circleY));
            int jStart = Mathf.RoundToInt(cx - fx);
            jStart = Math.Max(jStart, 0);
            int jEnd = Mathf.RoundToInt(cx + fx);
            jEnd = Math.Min(jEnd, width);
            for (int j = jStart; j < jEnd; j++)
            {
                colors[i * width + j] = color;
            }
        }

        return colors;
    }

    public static Color[] FillBitmapShapeDiamond(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }
        // Draw shape (note: the pixel organization is bottom up)
        float midY = ((float)height - 1.0f) / 2.0f;
        Vector2 midCenterLeft = new Vector2((float)width / 2.0f - 1.0f, midY);
        Vector2 midCenterRight = new Vector2((float)width / 2.0f, midY);
        Vector2 bottomLeft = new Vector2(0.0f, 0.0f);
        Vector2 bottomRight = new Vector2((float)width - 1.0f, 0.0f);
        Vector2 directionRight = midCenterRight - bottomRight;
        Vector2 directionLeft = midCenterLeft - bottomLeft;

        for (int i = 0; i < height; i++)
        {
            float t = ((float)i / (height - 1.0f));
            Vector2 rightPoint;
            Vector2 leftPoint;
            if (t <= 0.5f)
            {
                float firstT = t / 0.5f;
                rightPoint = midCenterRight + directionLeft * firstT;
                leftPoint = midCenterLeft + directionRight * firstT;
            }
            else
            {
                float secondT = (t - 0.5f) / 0.5f;
                rightPoint = bottomRight + directionRight * secondT;
                leftPoint = bottomLeft + directionLeft * secondT;
            }
            int jStart = Mathf.RoundToInt(leftPoint.x);
            int jEnd = Mathf.RoundToInt(rightPoint.x);
            jEnd = jEnd >= width ? width - 1 : jEnd;
            for (int j = jStart; j <= jEnd; j++)
            {
                colors[i * width + j] = color;
            }
        }

        return colors;
    }

    public static Color[] FillBitmapShapeHexagon(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }
        // Draw shape (note: the pixel organization is bottom up)
        float edge = width;
        float half = edge / 2.0f;
        float quarter = edge / 4.0f;
        Vector2 leftPoint = new Vector2(half - 1.0f, 0.0f);
        Vector2 rightPoint = new Vector2(half + 1.0f, 0.0f);
        Vector2 leftLine1 = new Vector2(0.0f, quarter) - leftPoint;
        Vector2 rightLine1 = new Vector2(edge - 1.0f, quarter) - rightPoint;
        Vector2 leftLine2 = leftPoint - new Vector2(0.0f, 3.0f * quarter - 1.0f);
        Vector2 rightLine2 = rightPoint - new Vector2(edge - 1.0f, 3.0f * quarter - 1.0f);
        for (int i = 0; i < edge; i++)
        {
            float t = (float)i / edge;
            int jStart;
            int jEnd;
            if (i < (int)quarter)
            {
                float localT = t / 0.25f;
                jStart = -1 + Mathf.RoundToInt(leftPoint.x + leftLine1.x * localT);
                jEnd = 1 + Mathf.RoundToInt(rightPoint.x + rightLine1.x * localT);
            }
            else if (i < 3 * (int)quarter)
            {
                jStart = 0;
                jEnd = (int)edge;
            }
            else
            {
                float localT = (t - 0.75f) / 0.25f;
                jStart = 1 + Mathf.RoundToInt(0.0f + leftLine2.x * localT);
                jEnd = -1 + Mathf.RoundToInt(edge + rightLine2.x * localT);
            }
            for (int j = jStart; j < jEnd; j++)
            {
                colors[i * width + j] = color;
            }
        }

        return colors;
    }

    public static Color[] FillBitmapShapeOctagon(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }
        // Draw shape (note: the pixel organization is bottom up)
        float topY = (float)height - 1.0f;
        const float oneThird = 1.0f / 3.0f;
        float oneThirdX = (float)width * oneThird;
        float twoThirdX = oneThirdX * 2.0f;
        float oneThirdY = (float)height * oneThird;
        for (int i = 0; i <= (int)topY; i++)
        {
            float t = ((float)i / (height - 1.0f));
            int jStart = 0;
            int jEnd = width;
            if (t < oneThird)
            {
                float localT = t / oneThird;
                jStart = Mathf.RoundToInt(oneThirdX - oneThirdX * localT) - 1;
                jEnd = Mathf.RoundToInt(twoThirdX + oneThirdX * localT) + 1;
            }
            else if (t >= 2.0f * oneThird)
            {
                float localT = (t - 2.0f * oneThird) / oneThird;
                jStart = Mathf.RoundToInt(oneThirdX * localT) - 1;
                jEnd = Mathf.RoundToInt((float)width - oneThirdX * localT) + 1;
            }

            jStart = Math.Max(jStart, 0);
            jEnd = Math.Min(jEnd, width);
            for (int j = jStart; j < jEnd; j++)
            {
                colors[i * width + j] = color;
            }
        }

        return colors;
    }

    public static Color[] FillBitmapShapeRectangle(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = color;
        }

        return colors;
    }

    public static Color[] FillBitmapShapeRectangleFrame(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }
        // Draw shape (note: the pixel organization is bottom up)
        int thickness = height / 8;
        thickness = Math.Max(thickness, 1);
        for (int i = 0; i < height; i++)
        {
            // Bottom or top area
            if (i < thickness || i > height - thickness - 1)
            {
                // Paint the whole stretch
                for (int j = 0; j < width; j++)
                {
                    colors[i * width + j] = color;
                }
            }
            else
            {
                // Paint left frame thickness
                for (int j = 0; j < thickness; j++)
                {
                    colors[i * width + j] = color;
                }
                // Paint right frame thickness
                for (int j = width - thickness; j < width; j++)
                {
                    colors[i * width + j] = color;
                }
            }
        }

        return colors;
    }

    public static Color[] FillBitmapShapeTriangle(int width, int height, Color color)
    {
        // Color the texture
        Color[] colors = new Color[width * height];
        // Flood it with transparency
        Color transparency = Color.clear;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = transparency;
        }
        // Draw shape (note: the pixel organization is bottom up)
        float topY = (float)height - 1.0f;
        Vector2 topCenterLeft = new Vector2((float)width / 2.0f - 1.0f, topY);
        Vector2 topCenterRight = new Vector2((float)width / 2.0f, topY);
        Vector2 bottomLeft = new Vector2(0.0f, 0.0f);
        Vector2 bottomRight = new Vector2((float)width - 1.0f, 0.0f);
        Vector2 directionRight = topCenterRight - bottomRight;
        Vector2 directionLeft = topCenterLeft - bottomLeft;

        for (int i = 0; i < height; i++)
        {
            float t = ((float)i / (height - 1.0f));
            Vector2 rightLine = bottomRight + directionRight * t;
            Vector2 leftLine = bottomLeft + directionLeft * t;
            int jStart = Mathf.RoundToInt(leftLine.x);
            int jEnd = Mathf.RoundToInt(rightLine.x);
            jEnd = jEnd >= width ? width - 1 : jEnd;
            for (int j = jStart; j <= jEnd; j++)
            {
                colors[i * width + j] = color;
            }
        }

        return colors;
    }
    
    public static GameObject LoadPrefab(string objectName, string assetsRelativePath)
    {
        string pathToPrefab = "Assets/" + assetsRelativePath + "/" + objectName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath(pathToPrefab, typeof(GameObject)) as GameObject;
        if (prefab == null)
        {
            Debug.LogWarning("Can't find prefab at " + pathToPrefab);
        }
        return prefab;
    }

    public static Sprite LoadSprite(string spriteName, string assetsRelativePath)
    {
        string path = "Assets/" + assetsRelativePath + "/" + spriteName + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath(path, typeof(Sprite)) as Sprite;
        if (sprite == null)
        {
            Debug.LogWarning("Can't find sprite named " + spriteName);
        }
        return sprite;
    }

    public static Sprite LoadSpriteAtPath(string assetsRelativePath)
    {
        string path = "Assets/" + assetsRelativePath;
        Sprite sprite = AssetDatabase.LoadAssetAtPath(path, typeof(Sprite)) as Sprite;
        if (sprite == null)
        {
            Debug.LogWarning("Can't find sprite at path " + path);
        }
        return sprite;
    }

    public static Tile LoadTile(string tileName, string assetsRelativePath)
    {
        string path = "Assets/" + assetsRelativePath + "/" + tileName + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath(path, typeof(Tile)) as Tile;
        if (tile == null)
        {
            Debug.LogWarning("Can't find sprite named " + tileName);
        }
        return tile;
    }

    public static Tile LoadTileAtPath(string assetsRelativePath)
    {
        string path = "Assets/" + assetsRelativePath;
        Tile tile = AssetDatabase.LoadAssetAtPath(path, typeof(Tile)) as Tile;
        if (tile == null)
        {
            Debug.LogWarning("Can't find tile at path " + assetsRelativePath);
        }
        return tile;
    }

    public static void PaintTileBlock(Tilemap tileMap, Tile tile, Vector3Int startPos, int numColumns, int numRows)
    {
        Vector3Int marker = new Vector3Int(startPos.x, startPos.y, 0);
        // Copy a rectangular pattern defined by numRows and numColumns
        // Marker starts at top-left corner
        for (int i = 0; i < numRows; i++)
        {
            for (int j = 0; j < numColumns; j++)
            {
                tileMap.SetTile(marker, tile);
                marker.x += 1;
            }
            marker.y -= 1;
            marker.x = startPos.x;
        }
    }

    public static void PlotTiles(Tilemap tilemap, Tile[] tilesGuide, int[] mapArray, Vector2Int size, Vector2Int start)
    {
        int numTiles = tilesGuide.Length;
        Vector3Int marker = Vector3Int.zero;
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                // Solve map index
                int mapIndex = y * size.x + x;
                // Solve tile index at this coordinates
                int tileIndex = mapArray[mapIndex];
                if (tileIndex < 0 || tileIndex >= numTiles)
                {
                    // Do not plot if tile does not exist
                    continue;
                }
                // Plot the tile at the corresponding coordinates
                marker.x = start.x + x;
                marker.y = start.y + y;
                tilemap.SetTile(marker, tilesGuide[tileIndex]);
            }
        }
    }
}
