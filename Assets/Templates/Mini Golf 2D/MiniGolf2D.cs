using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public class MiniGolf2D : Editor
{
    private const string templateName = "MiniGolf2D";
    private const string templateSpacedName = "Mini Golf 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "MG2D";
    private const int textureSize = 64;
    private const int mapWidth = 36;
    private const int mapHeight = 24;
    private static string materialPath;
    // All paths are relative to 'Assets' folder.
    // Do not include 'Assets/' in the file paths!
    public static string templatePath = "Templates/" + templateSpacedName;
    public static string prefabsPath = templatePath + "/Prefabs";
    public static string scriptsPath = templatePath + "/Scripts";
    public static string scenesPath = templatePath + "/Scenes";
    public static string settingsPath = templatePath + "/Settings";
    public static string texturesPath = templatePath + "/Textures";
    public static string tilesPath = templatePath + "/Tiles";
    private static string[] subFolders = { prefabsPath, scriptsPath, scenesPath, settingsPath, texturesPath, tilesPath };


    private enum SpritePath
    {
        Lawn = 0,
        Mat,
        Dark,
        Ball,
        Wood,
        Metal,
        Hole,
        AimMarker,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];
    
    private enum TilePath
    {
        Lawn = 0,
        Mat,
        Dark,
        Wood,
        Max
    }
    static string[] tilePaths = new string[(int)TilePath.Max];
    static Tile[] tiles = new Tile[(int)TilePath.Max];

    [MenuItem("Templates/" + templateSpacedName + "/Delete")]
    private static void DeleteGameTemplateAndRefresh()
    {
        DeleteGameTemplate();
        AssetDatabase.Refresh();
    }

    public static void DeleteGameTemplate()
    {
        // Delete pref key
        EditorPrefs.DeleteKey(prefKey);
        // Delete folders, except Scenes
        foreach (string folderName in subFolders)
        {
            if (folderName != scenesPath)
            {
                FileUtil.DeleteFileOrDirectory(Application.dataPath + "/" + folderName);
                FileUtil.DeleteFileOrDirectory(Application.dataPath + "/" + folderName + ".meta");
            }
        }
        // Remove tags and layers
        // Tags
        ScriptUtilities.RemoveTag(scriptPrefix + "Hole");
        // Nothing to remove
        // Save scene
        Scene openScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(openScene);
        // Change current scene to TempScene
        EditorSceneManager.OpenScene("Assets/Templates/Shared/TempScene.unity");
        // Delete Scene
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/" + scenesPath);
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/" + scenesPath + ".meta");
    }

    [MenuItem("Templates/" + templateSpacedName + "/Generate")]
    public static void GenerateGameTemplate()
    {
        ScriptUtilities.InstallPackage("com.unity.inputsystem");
        // Generate folders
        GenerateFolders();
        // Generate scene and camera
        GenerateScene();
        // Generate assets
        GenerateAssets();
        // Generate objects
        GenerateObjects();
        // Generate Tilemap
        GenerateTileMap();
        // Layout
        LayoutGolfCourse();
        // Generate UI
        GenerateUI();
        // Generate scripts
        GenerateScripts();
        // Enable on script reload processing
        EnableOnScriptsReloadedProcessing();
    }

    private static void GenerateFolders()
    {
        ContentUtilities.CreateFolders(templatePath, subFolders);
    }

    private static void GenerateScene()
    {
        ContentUtilities.CreateAndSaveScene(templateName, scenesPath);
        // Set up camera
        GameObject mainCameraObject = GameObject.Find("Main Camera");
        mainCameraObject.GetComponent<Camera>().orthographicSize = mapHeight / 2;
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags
        ScriptUtilities.CreateTag(scriptPrefix + "Hole");
        // Sorting Layers
    }

    private static void GenerateAssets()
    {
        GenerateInputActions();
        GenerateMaterials();
        GenerateTextures();
        AssetDatabase.Refresh();
        PostProcessTextures();
    }

    private static void GenerateInputActions()
    {
        // Create asset instance
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        // Create map
        var map = asset.AddActionMap("Gameplay");
        // Create Mouse Down action and add bindings
        var action = map.AddAction("MouseDown", interactions: "Press", type: InputActionType.Button);
        action.AddBinding(new InputBinding("<Mouse>/leftButton"));

        // Create UI action map
        map = asset.AddActionMap("UI");
        // Create AnyKey action and add bindings
        action = map.AddAction("AnyKey", interactions: "tap");
        action.AddBinding(new InputBinding("<Keyboard>/anyKey"));
        action.AddBinding(new InputBinding("<Mouse>/Press"));

        // Save asset
        string path = "Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions";
        File.WriteAllText(path, asset.ToJson());
    }

    private static void GenerateTextures()
    {
        string path;
        const float k = 1.0f / 255.0f;
        Color lawnGreen = new Color(93.0f * k, 106.0f * k, 49.0f * k);
        Color matColor = new Color(13.0f * k, 152.0f * k, 186.0f * k);
        Color darkGreen = lawnGreen / 2.0f;
        Color ballWhite = new Color(237.0f * k, 241.0f * k, 230.0f * k);
        Color woodBrown = new Color(231.0f * k, 200.0f * k, 93.0f * k);
        Color metalGray = new Color(123.0f * k, 144.0f * k, 149.0f * k);
        Color holeBlack = new Color(48.0f * k, 25.0f * k, 52.0f * k);

        // Generate textures
        int w = textureSize;
        int h = textureSize;
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_lawn", texturesPath, w, h, lawnGreen);
        spritePaths[(int)SpritePath.Lawn] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_mat", texturesPath, w, h, matColor);
        spritePaths[(int)SpritePath.Mat] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_dark", texturesPath, w, h, darkGreen);
        spritePaths[(int)SpritePath.Dark] = path;
        path = ContentUtilities.CreateTexture2DCircleAsset("texture_ball", texturesPath, w, h, ballWhite);
        spritePaths[(int)SpritePath.Ball] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_wood", texturesPath, w, h, woodBrown);
        spritePaths[(int)SpritePath.Wood] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_metal", texturesPath, w / 2, h * 4, metalGray);
        spritePaths[(int)SpritePath.Metal] = path;
        path = ContentUtilities.CreateTexture2DTriangleAsset("texture_aim_marker", texturesPath, w, h, Color.white);
        spritePaths[(int)SpritePath.AimMarker] = path;
        int r = (int)(w * 1.5f);
        path = ContentUtilities.CreateTexture2DCircleAsset("texture_hole", texturesPath, r, r, holeBlack);
        spritePaths[(int)SpritePath.Hole] = path;
    }

    private static void GenerateMaterials()
    {
        // Create physics 2D material for ball
        materialPath = ContentUtilities.CreatePhysicsMaterial2D("BallPhysicsMaterial2D", 0.5f, 0.0f, texturesPath);
    }
    
    private static void GenerateObjects()
    {
        GameObject newObject;

        // Game Manager
        newObject = new GameObject("GameManager");
        ContentUtilities.CreatePrefab(newObject, prefabsPath);

        // Level Markers
        newObject = new GameObject("Level1");
        newObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        newObject = new GameObject("Level2");
        newObject.transform.position = new Vector3(0.0f, mapHeight, 0.0f);
        newObject = new GameObject("Level3");
        newObject.transform.position = new Vector3(0.0f, mapHeight * 2, 0.0f);

        // Golf ball
        newObject = CreateGolfBall();
        ContentUtilities.CreatePrefab(newObject, prefabsPath);

        // Hole
        newObject = CreateHole();
        ContentUtilities.CreatePrefab(newObject, prefabsPath);

        // Rotating block
        newObject = CreateRotatingBlock();
        ContentUtilities.CreatePrefab(newObject, prefabsPath);

        // Aim marker asset
        newObject = CreateAimMarker();
        ContentUtilities.CreatePrefab(newObject, prefabsPath);
    }

    private static void GenerateUI()
    {
        // Create canvas game object and event system
        GameObject canvasObject = ContentUtilities.CreateUICanvas();
        Transform parent = canvasObject.transform;

        // Add input actions
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        canvasObject.AddComponent<PlayerInput>().actions = asset;
        canvasObject.GetComponent<PlayerInput>().defaultActionMap = "UI";

        const float margin = 10.0f;
        const int fontSize = 24;
        float w = 150.0f;
        float h = 40.0f;

        // Create level text panel
        GameObject levelTextPanel = ContentUtilities.CreateUIBackgroundObject("LevelTextPanel", w, h);
        ContentUtilities.AnchorUIObject(levelTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create level text
        GameObject levelTextObject = ContentUtilities.CreateUITextObject("LevelText", w - margin, h, "Level: 99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(levelTextObject, levelTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create par text panel
        w = 200;
        float offsetY = -h;
        GameObject parTextPanel = ContentUtilities.CreateUIBackgroundObject("ParTextPanel", w, h);
        ContentUtilities.AnchorUIObject(parTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create par text
        GameObject parTextObject = ContentUtilities.CreateUITextObject("ParText", w - margin, h, "Par: 99 / 99 / 99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(parTextObject, parTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create strokes text panel
        w = 240;
        offsetY = -h * 2;
        GameObject strokesTextPanel = ContentUtilities.CreateUIBackgroundObject("StrokesTextPanel", w, h);
        ContentUtilities.AnchorUIObject(strokesTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create score text
        GameObject strokesTextObject = ContentUtilities.CreateUITextObject("StrokesText", w - margin, h, "Strokes: 99 / 99 / 99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(strokesTextObject, strokesTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "Hole in One!", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create reset button
        w = 150.0f;
        h = 40.0f;
        GameObject resetButtonObject = ContentUtilities.CreateUIButtonObject("ResetButton", w, h, "Reset", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resetButtonObject, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin));

        // Create next button
        offsetY = -h;
        GameObject nextButtonObject = ContentUtilities.CreateUIButtonObject("NextButton", w, h, "Next", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(nextButtonObject, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin + offsetY));

        // Create context help text
        w = 360.0f;
        h = 40.0f;
        GameObject contextHelpObject = ContentUtilities.CreateUIBackgroundObject("ContextHelp", w, h);
        contextHelpObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        ContentUtilities.AnchorUIObject(contextHelpObject, parent, ContentUtilities.Anchor.Bottom, new Vector2(0.0f, margin));
        // Attach an UI text object
        GameObject textObject = ContentUtilities.CreateUITextObject("Text", w - 20.0f, h, "Context help text.", TextAnchor.MiddleCenter, 15, Color.white);
        ContentUtilities.AnchorUIObject(textObject, contextHelpObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create help panel
        w = 600.0f;
        h = 240.0f;
        GameObject helpPanelObject = ContentUtilities.CreateUIBackgroundObject("HelpPanel", w, h, 0.9f);
        ContentUtilities.AnchorUIObject(helpPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create help panel text
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Click on the Blue Patch to Put the Ball on Tee");
        sb.AppendLine("Drag Away From the Ball To Aim It");
        sb.AppendLine("Click Again to Strike the Ball");
        sb.AppendLine("Complete the Course with As Few Strokes As Possible");
        GameObject helpPanelTextObject = ContentUtilities.CreateUITextObject("Text", w, h, sb.ToString(), TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(helpPanelTextObject, helpPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create press any key text
        w = 200;
        h = 40;
        GameObject pressAnyKeyTextObject = ContentUtilities.CreateUITextObject("PressAnyKeyText", w, h, "Press Any Key", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(pressAnyKeyTextObject, parent, ContentUtilities.Anchor.Bottom, new Vector2(0.0f, margin));
    }

    private static void GenerateScripts()
    {
        WriteMG2DBallScriptToFile();
        WriteMG2DBlinkTextScriptToFile();
        WriteMG2DContextHelpScriptToFile();
        WriteMG2DGameManagerScriptToFile();
        WriteMG2DGridScriptToFile();
        WriteMG2DHudScriptToFile();
        WriteMG2DLevelScriptToFile();
        WriteMG2DRotatingBlockScriptToFile();
    }

    private static void GenerateTileMap()
    {
        // Generate tile map, pallette, and tile assets here...
        string tileAssetPath;

        // Create tile asset
        tileAssetPath = ContentUtilities.CreateTileAsset("tile_lawn", spritePaths[(int)SpritePath.Lawn], tilesPath);
        tilePaths[(int)TilePath.Lawn] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("tile_mat", spritePaths[(int)SpritePath.Mat], tilesPath);
        tilePaths[(int)TilePath.Mat] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("tile_dark", spritePaths[(int)SpritePath.Dark], tilesPath);
        tilePaths[(int)TilePath.Dark] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("tile_wood", spritePaths[(int)SpritePath.Wood], tilesPath);
        tilePaths[(int)TilePath.Wood] = tileAssetPath;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        // Ground layer
        GameObject tilemapObject;
        tilemapObject = ContentUtilities.CreateTilemapObject("GroundLayer");
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 0;
        // Find the automatically created Grid object
        GameObject gridObject = GameObject.Find("Grid");
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        gridObject.AddComponent<PlayerInput>().actions = asset;
        gridObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Upper layer
        tilemapObject = ContentUtilities.CreateTilemapObject("ObstaclesLayer", gridObject);
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 1;
        tilemapObject.AddComponent<Rigidbody2D>();
        tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapObject.AddComponent<CompositeCollider2D>();
        tilemapObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        tilemapObject.GetComponent<TilemapCollider2D>().usedByComposite = true;

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        int numColumns = ((int)TilePath.Max + 1) / 2;
        Vector3Int marker = Vector3Int.zero;
        int index = 0;
        foreach (string path in tilePaths)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + path);
            marker.x = index % numColumns;
            marker.y = index / numColumns;
            paletteTilemap.SetTile(marker, tile);
            tiles[index] = tile;
            index++;
        }
        // ... Add more tiles if needed
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Ball", "GameManager", "Grid", "Hud", "Level", "RotatingBlock" }))
        {
            // Go directly to build
            PostScriptsCompiledBuild();
        }
        else
        {
            // Enable call back on scripts recompiled
            EditorPrefs.SetBool(prefKey, true);
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (!EditorPrefs.GetBool(prefKey, false))
        {
            return;
        }
        EditorApplication.update += PostProcessOnce;
    }

    private static void PostProcessOnce()
    {
        EditorApplication.update -= PostProcessOnce;
        PostScriptsCompiledBuild();
    }

    private static void PostScriptsCompiledBuild()
    {
        // Get game objects
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Access the prefabs
        GameObject gameManagerPrefab = ContentUtilities.LoadPrefab("GameManager", prefabsPath);
        GameObject ballPrefab = ContentUtilities.LoadPrefab("Ball", prefabsPath);
        GameObject holePrefab = ContentUtilities.LoadPrefab("Hole", prefabsPath);
        GameObject rotatingBlockPrefab = ContentUtilities.LoadPrefab("RotatingBlock", prefabsPath);
        GameObject aimMarkerPrefab = ContentUtilities.LoadPrefab("AimMarker", prefabsPath);

        // Other objects
        GameObject canvasObject = GameObject.Find("Canvas");
        GameObject gridObject = GameObject.Find("Grid");
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject level1Object = GameObject.Find("Level1");
        GameObject level2Object = GameObject.Find("Level2");
        GameObject level3Object = GameObject.Find("Level3");
        GameObject contextHelpObject = GameObject.Find("ContextHelp");

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Ball", ballPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "RotatingBlock", rotatingBlockPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Hud", canvasObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Grid", gridObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Level", level1Object);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Level", level2Object);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Level", level3Object);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "ContextHelp", contextHelpObject);

        // Create and set up initial objects
        InstantiateAndSetupGameManager(gameManagerPrefab);
        GameObject ballObject = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
        AssignBallParameters(aimMarkerPrefab, ballObject);
        AssignGridParameters(ballObject, gridObject);
        AssignHudParameters(canvasObject);
        AssignLevelParameters(1, 3, level1Object);
        AssignLevelParameters(1, 4, level2Object);
        AssignLevelParameters(1, 5, level3Object);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
		// Notify builder
		ScriptUtilities.NotifyBuildComplete(templateName);
	}

    private static void PostProcessTextures()
    {
        float PPU = textureSize;
        Sprite[] tempSprites = new Sprite[(int)SpritePath.Max];

        for (int i = 0; i < (int)SpritePath.Max; i++)
        {
            string path = "Assets/" + spritePaths[i];
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            ti.spritePixelsPerUnit = PPU;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = FilterMode.Point;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            // Save to temp array
            tempSprites[i] = ContentUtilities.LoadSpriteAtPath(spritePaths[i]);
        }

        // Add sprites to the sprite atlas
        SpriteAtlas sa = new SpriteAtlas();
        SpriteAtlasExtensions.Add(sa, tempSprites);
        // Change texture settings
        SpriteAtlasTextureSettings textureSettings = SpriteAtlasExtensions.GetTextureSettings(sa);
        textureSettings.filterMode = FilterMode.Point;
        SpriteAtlasExtensions.SetTextureSettings(sa, textureSettings);
        // Save sprite atlas asset
        AssetDatabase.CreateAsset(sa, "Assets/" + texturesPath + "/" + scriptPrefix + "SpriteAtlas.spriteatlas");
    }

    private static void AssignBallParameters(GameObject aimMarkerPrefab, GameObject ballObject)
    {
        string className = scriptPrefix + "Ball";
        GameObject aimMarkerObject = PrefabUtility.InstantiatePrefab(aimMarkerPrefab) as GameObject;
        // Assign aim marker as child and parameter
        aimMarkerObject.transform.SetParent(ballObject.transform);
        ScriptUtilities.AssignObjectFieldToObject(aimMarkerObject, ballObject, className, "aimMarker");
    }
    
    private static void AssignGridParameters(GameObject ballObject, GameObject gridObject)
    {
        Transform transform = gridObject.transform;
        string className = scriptPrefix + "Grid";
        // Assign ball
        ScriptUtilities.AssignComponentFieldToObject(ballObject, scriptPrefix + "Ball", gridObject, className, "ball");
        // Assign elements
        ScriptUtilities.AssignComponentFieldToObject(transform.GetChild(0).gameObject, "Tilemap", gridObject, className, "lawnTilemap");
    }

    private static void AssignHudParameters(GameObject canvasObject)
    {
        Transform transform = canvasObject.transform;
        string className = scriptPrefix + "Hud";

        // Get objects
        GameObject levelTextObject = GameObject.Find("LevelText");
        GameObject parTextObject = GameObject.Find("ParText");
        GameObject strokesTextObject = GameObject.Find("StrokesText");
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject resetButtonObject = GameObject.Find("ResetButton");
        GameObject nextButtonObject = GameObject.Find("NextButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign UI elements
        ScriptUtilities.AssignComponentFieldToObject(levelTextObject, "Text", canvasObject, className, "levelText");
        ScriptUtilities.AssignComponentFieldToObject(parTextObject, "Text", canvasObject, className, "parText");
        ScriptUtilities.AssignComponentFieldToObject(strokesTextObject, "Text", canvasObject, className, "strokesText");
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, canvasObject, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", canvasObject, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(resetButtonObject, "Button", canvasObject, className, "resetButton");
        ScriptUtilities.AssignComponentFieldToObject(nextButtonObject, "Button", canvasObject, className, "nextButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, canvasObject, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", canvasObject, className, "pressAnyKeyText");
    }

    private static void AssignLevelParameters(int level, int par, GameObject levelObject)
    {
        string className = scriptPrefix + "Level";
        ScriptUtilities.AssignIntFieldToObject(level, levelObject, className, "level");
        ScriptUtilities.AssignIntFieldToObject(par, levelObject, className, "par");
    }

    private static GameObject CreateAimMarker()
    {
        GameObject newObject = new GameObject("AimMarker");
        GameObject spriteObject = ContentUtilities.CreateTexturedFigment("AimMarkerSprite", 0.0f, 0.0f, spritePaths[(int)SpritePath.AimMarker]);
        spriteObject.transform.SetParent(newObject.transform);
        spriteObject.transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
        spriteObject.GetComponent<SpriteRenderer>().sortingOrder = 3;

        return newObject;
    }

    private static GameObject CreateGolfBall()
    {
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Circle;
        GameObject newObject = ContentUtilities.CreateTexturedBody("Ball", 0.0f, 0.0f, spritePaths[(int)SpritePath.Ball], shape);
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0.0f;
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 3;
        
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath("Assets/" + materialPath, typeof(PhysicsMaterial2D)) as PhysicsMaterial2D;
        newObject.GetComponent<CircleCollider2D>().sharedMaterial = material;

        return newObject;
    }

    private static GameObject CreateHole()
    {
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Circle;
        GameObject newObject = ContentUtilities.CreateTexturedBody("Hole", 0.0f, 0.0f, spritePaths[(int)SpritePath.Hole], shape);
        newObject.tag = scriptPrefix + "Hole";
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        rb.bodyType = RigidbodyType2D.Static;
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 2;
        newObject.GetComponent<CircleCollider2D>().isTrigger = true;
        return newObject;
    }

    private static GameObject CreateRotatingBlock()
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody("RotatingBlock", 0.0f, 0.0f, spritePaths[(int)SpritePath.Metal]);
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        // Constrain the x and y movement
        rb.constraints = RigidbodyConstraints2D.FreezePosition;
        // Configure other rigid body settings
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0.0f;
        rb.angularDrag = 0.0f;
        rb.mass = 10000.0f;
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 3;

        return newObject;
    }

    private static int[] GetGroundLayerMapArray()
    {
        return new int[] {
            // Array size (32, 72)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  1,  1,  1,  1,  1,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  1,  1,  1,  1,  1,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  1,  1,  1,  1,  1,  0,  0,  0, -1, -1, -1,  0,  0,  2,  2,  2,  2,  2,  2,  2,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  1,  1,  1,  1,  1,  0,  0,  0, -1, -1, -1,  0,  0,  2,  2,  2,  2,  2,  2,  2,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  2,  2,  2,  2,  2,  2,  2,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  2,  2,  2,  2,  2,  2,  2,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  2,  2,  2,  2,  2,  2,  2,  2,  2,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetObstaclesLayerMapArray()
    {
        return new int[] {
            // Array size (32, 72)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1,
             -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3, -1, -1, -1,
             -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get Objects
        GameObject level1Object = GameObject.Find("Level1");
        GameObject level2Object = GameObject.Find("Level2");
        GameObject level3Object = GameObject.Find("Level3");
        GameObject[] levels =
        {
            level1Object, level2Object, level3Object
        };
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject gridObject = GameObject.Find("Grid");
        GameObject canvasObject = GameObject.Find("Canvas");
        GameObject contextHelpObject = GameObject.Find("ContextHelp");
        // Assign objects or components
        ScriptUtilities.AssignObjectsFieldToObject(levels, go, className, "levels");
        ScriptUtilities.AssignComponentFieldToObject(cameraObject, "Camera", go, className, "mainCamera");
        ScriptUtilities.AssignComponentFieldToObject(gridObject, scriptPrefix + "Grid", go, className, "grid");
        ScriptUtilities.AssignComponentFieldToObject(canvasObject, scriptPrefix + "Hud", go, className, "hud");
        ScriptUtilities.AssignComponentFieldToObject(contextHelpObject, scriptPrefix + "ContextHelp", go, className, "contextHelp");
    }

    private static void LayoutGolfCourse()
    {
        // Lay out the levels' tiles
        LayoutLevels();
        // Lay out objects
        GameObject holePrefab = ContentUtilities.LoadPrefab("Hole", prefabsPath);
        GameObject rotatingBlockPrefab = ContentUtilities.LoadPrefab("RotatingBlock", prefabsPath);
        // Level 1
        GameObject levelObject = GameObject.Find("Level1");
        LayoutObject(holePrefab, levelObject.transform, 4.5f, 7.0f);
        // Level 2
        levelObject = GameObject.Find("Level2");
        LayoutObject(holePrefab, levelObject.transform, -12.5f, 4.0f);
        // Level 3
        levelObject = GameObject.Find("Level3");
        LayoutObject(holePrefab, levelObject.transform, 6.5f, -5.0f);
        LayoutObject(rotatingBlockPrefab, levelObject.transform, -0.5f, 4.5f);
    }

    private static void LayoutLevels()
    {
        // Get the tile maps
        Tilemap groundLayer = GameObject.Find("GroundLayer").GetComponent<Tilemap>();
        Tilemap obstaclesLayer = GameObject.Find("ObstaclesLayer").GetComponent<Tilemap>();
        // Create a tiles guide (order matters)
        Tile[] tilesGuide = new Tile[tilePaths.Length];
        for (int i = 0; i < tilesGuide.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTileAtPath(tilePaths[i]);
        }
        // Configure size and start points
        Vector2Int size = new Vector2Int(32, 72);
        Vector2Int start = new Vector2Int(-16, -12);
        // Layout according to a map array
        ContentUtilities.PlotTiles(groundLayer, tilesGuide, GetGroundLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(obstaclesLayer, tilesGuide, GetObstaclesLayerMapArray(), size, start);
    }
    
    private static void LayoutObject(GameObject prefab, Transform parent, float x, float y)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        newObject.transform.SetParent(parent);
        newObject.transform.localPosition = new Vector2(x, y);
    }

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        // Note: the scripts and tilemaps must exist before this function is called
        ReverseEngineerTilemaps();
        ReverseEngineerScripts();
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DBall", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DContextHelp", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DGrid", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DHud", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DLevel", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("MG2DRotatingBlock", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void ReverseEngineerTilemaps()
    {
        // Call this method only after the Game Template has been created

        int startX = -16;
        int startY = -12;
        int w = 32;
        int h = 72;

        // Create name arrays following the order set by the enum above
        string[] tileNames = new string[] {
            "tile_lawn", "tile_mat", "tile_dark", "tile_wood"
        };
        // Assert this array has the same element as the enum
        Assert.IsTrue(tileNames.Length == (int)TilePath.Max);

        // Build parameters
        Tile[] guide = new Tile[(int)TilePath.Max];
        for (int i = 0; i < guide.Length; i++)
        {
            guide[i] = ContentUtilities.LoadTile(tileNames[i], tilesPath);
        }

        // Get tile maps
        Tilemap groundLayer = GameObject.Find("GroundLayer").GetComponent<Tilemap>();
        Tilemap obstaclesLayer = GameObject.Find("ObstaclesLayer").GetComponent<Tilemap>();

        // Ground Layer
        // Convert into a map array
        int[] mapArray = ContentUtilities.ConvertTilemapToMapArray(groundLayer, guide, w, h, startX, startY);
        // Convert map array to string
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        // Print out to console
        Debug.Log("Printing Tile Map " + groundLayer.name);
        Debug.Log(sb.ToString());

        // Obstacles Layer
        // Convert into a map array
        mapArray = ContentUtilities.ConvertTilemapToMapArray(obstaclesLayer, guide, w, h, startX, startY);
        // Convert map array to string
        sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        // Print out to console
        Debug.Log("Printing Tile Map " + obstaclesLayer.name);
        Debug.Log(sb.ToString());
    }

    private static void WriteMG2DBallScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4691);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DBall : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject aimMarker;");
        sb.AppendLine("    public float maxPutt = 40.0f;");
        sb.AppendLine("    [System.NonSerialized]");
        sb.AppendLine("    public bool isHoled = false;");
        sb.AppendLine("    private float puttLength = 4.0f;");
        sb.AppendLine("    private Vector3 aim;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private float speed;");
        sb.AppendLine("    private float speedThreshold = 0.5f;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerStay2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isHoled == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If the collided object is a hole");
        sb.AppendLine("        if (collidedObject.CompareTag(\"MG2DHole\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Check if the ball is contained, within a speed and distance threshold");
        sb.AppendLine("            Vector3 d = transform.position - collidedObject.transform.position;");
        sb.AppendLine("            float ballRadius = GetComponent<CircleCollider2D>().radius;");
        sb.AppendLine("            float holeRadius = collision.gameObject.GetComponent<CircleCollider2D>().radius;");
        sb.AppendLine("            float threshold = (ballRadius - holeRadius) * 0.85f;");
        sb.AppendLine("            if (d.sqrMagnitude < threshold * threshold)");
        sb.AppendLine("            {");
        sb.AppendLine("                Stop();");
        sb.AppendLine("                isHoled = true;");
        sb.AppendLine("                gameObject.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ApplyDamping(float dampFactor)");
        sb.AppendLine("    {");
        sb.AppendLine("        rb.velocity -= rb.velocity * dampFactor * Time.deltaTime;");
        sb.AppendLine("        speed = rb.velocity.magnitude;");
        sb.AppendLine("        if (speed <= speedThreshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            speed = 0.0f;");
        sb.AppendLine("            Stop();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void EnableAimMarker(bool flag)");
        sb.AppendLine("    {");
        sb.AppendLine("        aimMarker.SetActive(flag);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public float GetSpeed()");
        sb.AppendLine("    {");
        sb.AppendLine("        return speed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SetAimingPosition(Vector3 aimPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        aim = transform.position - aimPosition;");
        sb.AppendLine("        ");
        sb.AppendLine("        // Set marker scale");
        sb.AppendLine("        float magnitude = aim.magnitude;");
        sb.AppendLine("        magnitude = Mathf.Min(puttLength, magnitude);");
        sb.AppendLine("        magnitude = Mathf.Max(1.0f, magnitude);");
        sb.AppendLine("        aimMarker.transform.localScale = new Vector3(1.0f, magnitude, 1.0f);");
        sb.AppendLine("        // Set marker rotation");
        sb.AppendLine("        float radian = Mathf.Atan2(aim.y, aim.x);");
        sb.AppendLine("        aimMarker.transform.rotation = Quaternion.Euler(0.0f, 0.0f, -90.0f + 180.0f * radian / Mathf.PI);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Stop()");
        sb.AppendLine("    {        ");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Strike()");
        sb.AppendLine("    {");
        sb.AppendLine("        float magnitude = aim.magnitude;");
        sb.AppendLine("        float puttMagnitude = (magnitude / puttLength) * maxPutt;");
        sb.AppendLine("        rb.velocity = aim / magnitude * puttMagnitude;        ");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DBall", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DBlinkText : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private Text text;");
        sb.AppendLine("");
        sb.AppendLine("    private void OnEnable()");
        sb.AppendLine("    {");
        sb.AppendLine("        text = GetComponent<Text>();");
        sb.AppendLine("        StartCoroutine(WaitToBlink(0.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToBlink(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        if (text.isActiveAndEnabled)");
        sb.AppendLine("        {");
        sb.AppendLine("            text.enabled = false;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            text.enabled = true;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        StartCoroutine(WaitToBlink(wait));");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DContextHelpScriptToFile()
    {
        StringBuilder sb = new StringBuilder(8690);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DContextHelp : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float textDuration = 10.0f;");
        sb.AppendLine("    public float fadeDuration = 0.75f;");
        sb.AppendLine("    private Text guideText;");
        sb.AppendLine("    private Image backGround;");
        sb.AppendLine("    private Color backGroundColor;");
        sb.AppendLine("    private Color guideTextColor;");
        sb.AppendLine("    private Color transparence = new Color(0.0f, 0.0f, 0.0f, 0.0f);");
        sb.AppendLine("    private bool[] shownFlags;");
        sb.AppendLine("    private IEnumerator oldFadeForDuration;");
        sb.AppendLine("    private IEnumerator oldShowForDuration;");
        sb.AppendLine("    private IEnumerator oldWaitToShow;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Guide");
        sb.AppendLine("    {");
        sb.AppendLine("        BallOnTee = 0,");
        sb.AppendLine("        HowToAim,");
        sb.AppendLine("        AimForHole,");
        sb.AppendLine("        NextButton,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get guide text");
        sb.AppendLine("        guideText = GetComponentInChildren<Text>();");
        sb.AppendLine("        guideTextColor = guideText.color;");
        sb.AppendLine("        // Get background image");
        sb.AppendLine("        backGround = GetComponent<Image>();");
        sb.AppendLine("        backGroundColor = backGround.color;");
        sb.AppendLine("        // Hide the element");
        sb.AppendLine("        guideText.color = transparence;");
        sb.AppendLine("        backGround.color = transparence;");
        sb.AppendLine("        // Restart");
        sb.AppendLine("        Restart();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator FadeForDuration(float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Compute the fade rate");
        sb.AppendLine("        Color fadeRate = new Color(0.0f, 0.0f, 0.0f, 1.0f / duration);");
        sb.AppendLine("");
        sb.AppendLine("        // Animate fading");
        sb.AppendLine("        while(backGround.color.a > 0.0f || guideText.color.a > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Fade guideText");
        sb.AppendLine("            guideText.color -= fadeRate * Time.deltaTime;");
        sb.AppendLine("            if (guideText.color.a <= 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                guideText.color = transparence;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Fade backGround");
        sb.AppendLine("            backGround.color -= fadeRate * Time.deltaTime;");
        sb.AppendLine("            if (backGround.color.a <= 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                backGround.color = transparence;");
        sb.AppendLine("            }");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Restart()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Init shown flags");
        sb.AppendLine("        shownFlags = new bool[(int)Guide.Max];");
        sb.AppendLine("        for (int i = 0; i < shownFlags.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            shownFlags[i] = false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Show(Guide guide, float wait = 0.0f)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check that this guide has not been shown");
        sb.AppendLine("        if (shownFlags[(int)guide] == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Stop old coroutines so they don't interfere with the new ones");
        sb.AppendLine("        StopAllLocalCoroutines();");
        sb.AppendLine("        // Use coroutine to show, after a period of wait");
        sb.AppendLine("        oldWaitToShow = WaitToShow(wait, guide);");
        sb.AppendLine("        StartCoroutine(oldWaitToShow);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ShowGuide(Guide guide)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check that this guide has not been shown");
        sb.AppendLine("        if (shownFlags[(int)guide] == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Mark that this guide has been shown");
        sb.AppendLine("        if ((int)guide < (int)Guide.Max)");
        sb.AppendLine("        {");
        sb.AppendLine("            shownFlags[(int)guide] = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Set color");
        sb.AppendLine("        guideText.color = guideTextColor;");
        sb.AppendLine("        backGround.color = backGroundColor;");
        sb.AppendLine("        // Set text");
        sb.AppendLine("        switch (guide)");
        sb.AppendLine("        {");
        sb.AppendLine("            case Guide.BallOnTee:");
        sb.AppendLine("                SetText(\"Click on the blue patch to put the golf ball on tee.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.HowToAim:");
        sb.AppendLine("                SetText(\"Drag the mouse away from the ball to aim. Then click to strike the ball.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.AimForHole:");
        sb.AppendLine("                SetText(\"Aim for the hole.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.NextButton:");
        sb.AppendLine("                SetText(\"Great! Now click the Next Button for next level or Reset to start again.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                SetText(\"Unknown guide\");");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Show for a duration");
        sb.AppendLine("        oldShowForDuration = ShowForDuration(textDuration);");
        sb.AppendLine("        StartCoroutine(oldShowForDuration);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetText(string text)");
        sb.AppendLine("    {");
        sb.AppendLine("        guideText.text = text;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator ShowForDuration(float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait for duration");
        sb.AppendLine("        yield return new WaitForSeconds(duration);");
        sb.AppendLine("        // Then start fading");
        sb.AppendLine("        oldFadeForDuration = FadeForDuration(fadeDuration);");
        sb.AppendLine("        StartCoroutine(oldFadeForDuration);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void StopAllLocalCoroutines()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop all local coroutines");
        sb.AppendLine("        if (oldFadeForDuration != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            StopCoroutine(oldFadeForDuration);");
        sb.AppendLine("            oldFadeForDuration = null;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (oldShowForDuration != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            StopCoroutine(oldShowForDuration);");
        sb.AppendLine("            oldShowForDuration = null;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (oldWaitToShow != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            StopCoroutine(oldWaitToShow);");
        sb.AppendLine("            oldWaitToShow = null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToShow(float wait, Guide guide)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait for a while");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        // Then start showing");
        sb.AppendLine("        ShowGuide(guide);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DContextHelp", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(9097);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Camera mainCamera;");
        sb.AppendLine("    public GameObject[] levels = new GameObject[3];");
        sb.AppendLine("    public MG2DGrid grid;");
        sb.AppendLine("    public MG2DHud hud;");
        sb.AppendLine("    public MG2DContextHelp contextHelp;");
        sb.AppendLine("    private bool busy = false;");
        sb.AppendLine("    private int currentLevel = 1;");
        sb.AppendLine("");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static MG2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (sharedInstance != null && sharedInstance != this)");
        sb.AppendLine("        {");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            sharedInstance = this;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        hud.ShowResult(false);");
        sb.AppendLine("        StartLevel(0);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop game");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("");
        sb.AppendLine("        // Total up strokes and par");
        sb.AppendLine("        int totalPar = 0;");
        sb.AppendLine("        int totalStrokes = 0;");
        sb.AppendLine("        foreach (GameObject level in levels)");
        sb.AppendLine("        {");
        sb.AppendLine("            totalPar += level.GetComponent<MG2DLevel>().par;");
        sb.AppendLine("            totalStrokes += level.GetComponent<MG2DLevel>().strokes;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        int d = totalStrokes - totalPar;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return gameStarted && !busy;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Reset scores");
        sb.AppendLine("        foreach (GameObject level in levels)");
        sb.AppendLine("        {");
        sb.AppendLine("            level.GetComponent<MG2DLevel>().strokes = 0;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Reset HUD");
        sb.AppendLine("        hud.UpdateLevel(0);");
        sb.AppendLine("        hud.UpdatePar(levels);");
        sb.AppendLine("        hud.UpdateStrokes(levels);");
        sb.AppendLine("        hud.ShowResult(false);");
        sb.AppendLine("");
        sb.AppendLine("        // Reset grid");
        sb.AppendLine("        grid.Restart();");
        sb.AppendLine("");
        sb.AppendLine("        // Start new game at level 1");
        sb.AppendLine("        StartLevel(0);");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void StartGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        contextHelp.Show(MG2DContextHelp.Guide.BallOnTee, 0.5f);");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void StartLevel(int index)");
        sb.AppendLine("    {");
        sb.AppendLine("        hud.nextButton.gameObject.SetActive(false);");
        sb.AppendLine("        hud.ShowResult(false);");
        sb.AppendLine("        ");
        sb.AppendLine("        // Set up level");
        sb.AppendLine("        currentLevel = index;");
        sb.AppendLine("        const float duration = 0.75f;");
        sb.AppendLine("        MG2DLevel level = levels[index].GetComponent<MG2DLevel>();");
        sb.AppendLine("        level.Restart();");
        sb.AppendLine("");
        sb.AppendLine("        // Use coroutines to animate movement and update HUD");
        sb.AppendLine("        StartCoroutine(MoveCameraToPosition(level.transform.position, duration));");
        sb.AppendLine("        StartCoroutine(WaitToUpdateHUD(duration));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator MoveCameraToPosition(Vector3 destination, float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Animate movement");
        sb.AppendLine("        float elapsed = 0.0f;");
        sb.AppendLine("        Vector3 origin = mainCamera.transform.position;");
        sb.AppendLine("        Vector3 d = destination - origin;");
        sb.AppendLine("        d.z = 0.0f;");
        sb.AppendLine("        while (true)");
        sb.AppendLine("        {");
        sb.AppendLine("            elapsed += Time.deltaTime;");
        sb.AppendLine("            float t = elapsed / duration;");
        sb.AppendLine("            mainCamera.transform.position = origin + d * t;");
        sb.AppendLine("            if (elapsed >= duration)");
        sb.AppendLine("            {");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        mainCamera.transform.position = new Vector3(destination.x, destination.y, -10.0f);");
        sb.AppendLine("        grid.StartLevel();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyBallOnTee()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Tell the player how to aim");
        sb.AppendLine("        contextHelp.Show(MG2DContextHelp.Guide.HowToAim);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyHoled()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Tell the player to click on the next button");
        sb.AppendLine("        contextHelp.Show(MG2DContextHelp.Guide.NextButton);");
        sb.AppendLine("        // Show next button");
        sb.AppendLine("        if (currentLevel < levels.Length - 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            hud.nextButton.gameObject.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Update UI");
        sb.AppendLine("        MG2DLevel level = levels[currentLevel].GetComponent<MG2DLevel>();");
        sb.AppendLine("        int d = level.strokes - level.par;");
        sb.AppendLine("        if (d > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            hud.ShowResult(true, d + \" over Par...\");");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (d < 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            hud.ShowResult(true, -d + \" under Par!\");");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            hud.ShowResult(true, \"Right on Par!\");");
        sb.AppendLine("        }        ");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyNext()");
        sb.AppendLine("    {");
        sb.AppendLine("        int nextLevel = currentLevel + 1;");
        sb.AppendLine("        if (nextLevel >= levels.Length)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            StartLevel(nextLevel);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyRestart()");
        sb.AppendLine("    {");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyStroke()");
        sb.AppendLine("    {");
        sb.AppendLine("        levels[currentLevel].GetComponent<MG2DLevel>().strokes++;");
        sb.AppendLine("        hud.UpdateStrokes(levels);");
        sb.AppendLine("        // Tell the player to aim for the hole");
        sb.AppendLine("        contextHelp.Show(MG2DContextHelp.Guide.AimForHole);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToUpdateHUD(float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set busy flag");
        sb.AppendLine("        busy = true;");
        sb.AppendLine("        ");
        sb.AppendLine("        // Wait");
        sb.AppendLine("        yield return new WaitForSeconds(duration);");
        sb.AppendLine("");
        sb.AppendLine("        // Update HUD");
        sb.AppendLine("        hud.UpdateLevel(currentLevel);");
        sb.AppendLine("        hud.UpdatePar(levels);");
        sb.AppendLine("        hud.UpdateStrokes(levels);");
        sb.AppendLine("");
        sb.AppendLine("        // Unset busy flag");
        sb.AppendLine("        busy = false;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DGridScriptToFile()
    {
        StringBuilder sb = new StringBuilder(9719);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Assertions;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DGrid : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public MG2DBall ball;");
        sb.AppendLine("    public Tilemap lawnTilemap;");
        sb.AppendLine("    public float roughDampFactor = 4.0f;");
        sb.AppendLine("    public float smoothDampFactor = 2.0f;");
        sb.AppendLine("    [System.NonSerialized]");
        sb.AppendLine("    public InputState inputState;");
        sb.AppendLine("    private bool clicked = false;");
        sb.AppendLine("    private MG2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    public enum InputState");
        sb.AppendLine("    {");
        sb.AppendLine("        Idle = 0,");
        sb.AppendLine("        Aiming,");
        sb.AppendLine("        Struck,");
        sb.AppendLine("        Holed,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public enum TileType");
        sb.AppendLine("    {");
        sb.AppendLine("        Mat = 0,");
        sb.AppendLine("        Lawn,");
        sb.AppendLine("        Dark,");
        sb.AppendLine("        Others,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        Assert.IsNotNull(lawnTilemap);");
        sb.AppendLine("        gameManager = MG2DGameManager.sharedInstance;");
        sb.AppendLine("        Restart();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        switch (inputState)");
        sb.AppendLine("        {");
        sb.AppendLine("            case InputState.Idle:");
        sb.AppendLine("                UpdateIdleInput();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Aiming:");
        sb.AppendLine("                UpdateAimingInput();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Struck:");
        sb.AppendLine("                UpdateStruckInput();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Holed:");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("        clicked = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMouseDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        clicked = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float DampFactorAtPosition(Vector3 worldPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        TileType type = TileTypeAtPosition(worldPosition);");
        sb.AppendLine("        if (type == TileType.Dark)");
        sb.AppendLine("        {");
        sb.AppendLine("            return roughDampFactor;");
        sb.AppendLine("        }");
        sb.AppendLine("        return smoothDampFactor;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Restart()");
        sb.AppendLine("    {");
        sb.AppendLine("        SetInputState(InputState.Idle);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private Vector3 ScreenToWorldPosition(Vector3 screenPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);");
        sb.AppendLine("        worldPosition.z = 0.0f;");
        sb.AppendLine("        return worldPosition;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetInputState(InputState state)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (state)");
        sb.AppendLine("        {");
        sb.AppendLine("            case InputState.Idle:");
        sb.AppendLine("                inputState = InputState.Idle;");
        sb.AppendLine("                ball.isHoled = false;");
        sb.AppendLine("                ball.EnableAimMarker(false);");
        sb.AppendLine("                ball.gameObject.SetActive(false);");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Aiming:");
        sb.AppendLine("                inputState = InputState.Aiming;");
        sb.AppendLine("                ball.gameObject.SetActive(true);");
        sb.AppendLine("                ball.EnableAimMarker(true);");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Struck:");
        sb.AppendLine("                inputState = InputState.Struck;");
        sb.AppendLine("                ball.EnableAimMarker(false);");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case InputState.Holed:");
        sb.AppendLine("                inputState = InputState.Holed;");
        sb.AppendLine("                ball.EnableAimMarker(false);");
        sb.AppendLine("                ball.gameObject.SetActive(false);");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void StartLevel()");
        sb.AppendLine("    {");
        sb.AppendLine("        SetInputState(InputState.Idle);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private TileType TileTypeAtPosition(Vector3 worldPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        Tile tile = lawnTilemap.GetTile(lawnTilemap.WorldToCell(worldPosition)) as Tile;");
        sb.AppendLine("        if (tile == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return TileType.Others;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (tile.name == \"tile_dark\")");
        sb.AppendLine("        {");
        sb.AppendLine("            return TileType.Dark;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (tile.name == \"tile_lawn\")");
        sb.AppendLine("        {");
        sb.AppendLine("            return TileType.Lawn;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (tile.name == \"tile_mat\")");
        sb.AppendLine("        {");
        sb.AppendLine("            return TileType.Mat;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            return TileType.Others;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateAimingInput()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3 worldPosition = ScreenToWorldPosition(Mouse.current.position.ReadValue());");
        sb.AppendLine("        ball.SetAimingPosition(worldPosition);");
        sb.AppendLine("        if (clicked)");
        sb.AppendLine("        {");
        sb.AppendLine("            ball.Strike();");
        sb.AppendLine("            SetInputState(InputState.Struck);");
        sb.AppendLine("            gameManager.NotifyStroke();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateIdleInput()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check mouse click");
        sb.AppendLine("        if (!clicked)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        Vector3 worldPosition = ScreenToWorldPosition(Mouse.current.position.ReadValue());");
        sb.AppendLine("        TileType type = TileTypeAtPosition(worldPosition);");
        sb.AppendLine("        if (type != TileType.Mat)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Put down the ball");
        sb.AppendLine("        SetInputState(InputState.Aiming);");
        sb.AppendLine("        ball.transform.position = worldPosition;");
        sb.AppendLine("        gameManager.NotifyBallOnTee();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateStruckInput()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set ball's damping factor");
        sb.AppendLine("        float dampFactor = DampFactorAtPosition(ball.transform.position);");
        sb.AppendLine("        ball.ApplyDamping(dampFactor);");
        sb.AppendLine("");
        sb.AppendLine("        // Check ball speed");
        sb.AppendLine("        if (ball.GetSpeed() > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Check if ball is holed");
        sb.AppendLine("        if (ball.isHoled)");
        sb.AppendLine("        {");
        sb.AppendLine("            SetInputState(InputState.Holed);");
        sb.AppendLine("            gameManager.NotifyHoled();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Ready ball for aiming");
        sb.AppendLine("        SetInputState(InputState.Aiming);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DGrid", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DHudScriptToFile()
    {
        StringBuilder sb = new StringBuilder(5156);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DHud : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Text levelText;");
        sb.AppendLine("    public Text parText;");
        sb.AppendLine("    public Text strokesText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Button nextButton;");
        sb.AppendLine("    public Button resetButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    private MG2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        nextButton.onClick.AddListener(TaskOnNextButtonClicked);");
        sb.AppendLine("        resetButton.onClick.AddListener(TaskOnResetButtonClicked);");
        sb.AppendLine("        resetButton.interactable = false;");
        sb.AppendLine("        gameManager = MG2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnAnyKey()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If the press any key text is not active");
        sb.AppendLine("        if (!pressAnyKeyText.gameObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If help panel is being displayed");
        sb.AppendLine("        if (helpPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Close it ");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            // Enable the reset button");
        sb.AppendLine("            resetButton.interactable = true;");
        sb.AppendLine("            // Hide the press any key text");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            // Start the game");
        sb.AppendLine("            gameManager.StartGame();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ShowResult(bool flag, string text = \"\")");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(flag);");
        sb.AppendLine("        if (resultPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            resultText.text = text;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnNextButtonClicked()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.NotifyNext();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnResetButtonClicked()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.NotifyRestart();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void UpdateLevel(int level)");
        sb.AppendLine("    {");
        sb.AppendLine("        int displayLevel = level + 1;");
        sb.AppendLine("        levelText.text = \"Level: \" + displayLevel;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void UpdatePar(GameObject[] levels)");
        sb.AppendLine("    {");
        sb.AppendLine("        string text = \"Par: \";");
        sb.AppendLine("        for (int i = 0; i < levels.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (i < levels.Length - 1)");
        sb.AppendLine("            {");
        sb.AppendLine("                text += levels[i].GetComponent<MG2DLevel>().par + \" / \";");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                text += levels[i].GetComponent<MG2DLevel>().par;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        parText.text = text;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void UpdateStrokes(GameObject[] levels)");
        sb.AppendLine("    {");
        sb.AppendLine("        string text = \"Strokes: \";");
        sb.AppendLine("        for (int i = 0; i < levels.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (i < levels.Length - 1)");
        sb.AppendLine("            {");
        sb.AppendLine("                text += levels[i].GetComponent<MG2DLevel>().strokes + \" / \";");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                text += levels[i].GetComponent<MG2DLevel>().strokes;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        strokesText.text = text;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DHud", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DLevelScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1157);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DLevel : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int level = 0;");
        sb.AppendLine("    public int par = 3;");
        sb.AppendLine("    public int strokes = 0;");
        sb.AppendLine("");
        sb.AppendLine("    public void Restart()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Reset each child");
        sb.AppendLine("        for (int i = 0; i < transform.childCount; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameObject child = transform.GetChild(i).gameObject;");
        sb.AppendLine("            // Trigger the child's on enable callback");
        sb.AppendLine("            child.SetActive(false);");
        sb.AppendLine("            child.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DLevel", scriptsPath, sb.ToString());
    }

    private static void WriteMG2DRotatingBlockScriptToFile()
    {
        StringBuilder sb = new StringBuilder(776);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class MG2DRotatingBlock : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float initialTorque = 250000.0f;");
        sb.AppendLine("");
        sb.AppendLine("    private void OnEnable()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Reset position");
        sb.AppendLine("        GetComponent<Rigidbody2D>().AddTorque(initialTorque);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("MG2DRotatingBlock", scriptsPath, sb.ToString());
    }
}
