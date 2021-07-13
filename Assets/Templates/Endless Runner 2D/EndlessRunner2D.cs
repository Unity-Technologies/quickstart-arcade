using System;
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

public class EndlessRunner2D : Editor
{
    private const string templateName = "EndlessRunner2D";
    private const string templateSpacedName = "Endless Runner 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "ER2D";
    private const int textureSize = 64;
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
    private static string materialPath;

    private enum SpritePath
    {
        Player = 0,
        Enemy,
        Coin,
        Spike,
        Crate,
        Earth,
        Grass,
        EarthLight,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];

    private enum TilePath
    {
        Grass = 0,
        Earth,
        Crate,
        Spike,
        EarthLight,
        Max
    }
    static string[] tilePaths = new string[(int)TilePath.Max];

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
        ScriptUtilities.RemoveTag(scriptPrefix + "Enemy");
        ScriptUtilities.RemoveTag(scriptPrefix + "Objective");
        ScriptUtilities.RemoveTag(scriptPrefix + "Platforms");
        ScriptUtilities.RemoveTag(scriptPrefix + "Hazards");
        ScriptUtilities.RemoveTag(scriptPrefix + "Collectibles");
        // Layers
        ScriptUtilities.RemoveLayer(scriptPrefix + "Platforms");
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
        // Generate tile map
        GenerateTileMap();
        // Layout level
        LayoutLevel();
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
        Camera mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();
        Color deepSkyBlue = new Color(0.0f / 255.0f, 186.0f / 255.0f, 255.0f / 255.0f);
        mainCamera.orthographicSize = 8.0f;
        mainCamera.backgroundColor = deepSkyBlue;
        mainCamera.transform.position = new Vector3(17.5f, 3.0f, -10.0f);
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags
        ScriptUtilities.CreateTag(scriptPrefix + "Enemy");
        ScriptUtilities.CreateTag(scriptPrefix + "Objective");
        ScriptUtilities.CreateTag(scriptPrefix + "Platforms");
        ScriptUtilities.CreateTag(scriptPrefix + "Hazards");
        ScriptUtilities.CreateTag(scriptPrefix + "Collectibles");
        // Layers
        ScriptUtilities.CreateLayer(scriptPrefix + "Platforms");
    }

    private static void GenerateAssets()
    {
        GenerateInputActions();
        GenerateTextures();
        GenerateMaterials();
        AssetDatabase.Refresh();
        PostProcessTextures();
    }

    private static void GenerateInputActions()
    {
        // Create asset instance
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        // Create map
        var map = asset.AddActionMap("Gameplay");
        // Create Jump action and add bindings
        var action = map.AddAction("Jump", interactions: "press");
        action.AddBinding(new InputBinding("<Keyboard>/space"));

        // Create UI action map
        map = asset.AddActionMap("UI");
        // Create AnyKey action and add bindings
        action = map.AddAction("AnyKey", interactions: "tap");
        action.AddBinding(new InputBinding("<Keyboard>/anyKey"));
        // Create ClickAnywhere action and add bindings
        action = map.AddAction("ClickAnywhere", interactions: "tap");
        action.AddBinding(new InputBinding("<Mouse>/Press"));

        // Save asset
        string path = "Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions";
        File.WriteAllText(path, asset.ToJson());
    }

    private static void GenerateTextures()
    {
        string path;
        Color goldenYellow = new Color(255.0f / 255.0f, 223.0f / 255.0f, 0.0f / 255.0f);
        Color krylonPewter = new Color(169.0f / 255.0f, 174.0f / 255.0f, 182.0f / 255.0f);
        Color woodBrown = new Color(205.0f / 255.0f, 163.0f / 255.0f, 96.0f / 255.0f);
        Color darkBrown = new Color(101.0f / 255.0f, 67.0f / 255.0f, 33.0f / 255.0f);
        Color lightBrown = new Color(181.0f / 255.0f, 101.0f / 255.0f, 29.0f / 255.0f);
        Color grassGreen = new Color(0.0f / 255.0f, 154.0f / 255.0f, 23.0f / 255.0f);

        // Generate textures
        // player
        path = ContentUtilities.CreateTexture2DRectangleAsset("player_texture", texturesPath, textureSize, textureSize, Color.blue);
        spritePaths[(int)SpritePath.Player] = path;
        // Enemy
        path = ContentUtilities.CreateTexture2DOctagonAsset("enemy_texture", texturesPath, textureSize, textureSize, Color.red);
        spritePaths[(int)SpritePath.Enemy] = path;
        // Coin 
        path = ContentUtilities.CreateTexture2DCircleAsset("coin_texture", texturesPath, textureSize / 2, textureSize / 2, goldenYellow);
        spritePaths[(int)SpritePath.Coin] = path;
        // Tile crate
        path = ContentUtilities.CreateTexture2DRectangleAsset("tile_crate_texture", texturesPath, textureSize, textureSize, woodBrown);
        spritePaths[(int)SpritePath.Crate] = path;
        // Tile earth
        path = ContentUtilities.CreateTexture2DRectangleAsset("tile_earth_texture", texturesPath, textureSize, textureSize, darkBrown);
        spritePaths[(int)SpritePath.Earth] = path;
        // Tile grass
        path = ContentUtilities.CreateTexture2DRectangleAsset("tile_grass_texture", texturesPath, textureSize, textureSize, grassGreen);
        spritePaths[(int)SpritePath.Grass] = path;
        // Tile spike
        path = ContentUtilities.CreateTexture2DTriangleAsset("tile_spike_texture", texturesPath, textureSize, textureSize, krylonPewter);
        spritePaths[(int)SpritePath.Spike] = path;
        // Tile earth - lighter tone
        path = ContentUtilities.CreateTexture2DRectangleAsset("tile_earth_light_texture", texturesPath, textureSize, textureSize, lightBrown);
        spritePaths[(int)SpritePath.EarthLight] = path;
    }

    private static void GenerateTileMap()
    {
        string tileAssetPath;

        // Create tile asset
        tileAssetPath = ContentUtilities.CreateTileAsset("crate_tile", spritePaths[(int)SpritePath.Crate], tilesPath);
        tilePaths[(int)TilePath.Crate] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("earth_tile", spritePaths[(int)SpritePath.Earth], tilesPath);
        tilePaths[(int)TilePath.Earth] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("earth_light_tile", spritePaths[(int)SpritePath.EarthLight], tilesPath);
        tilePaths[(int)TilePath.EarthLight] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("grass_tile", spritePaths[(int)SpritePath.Grass], tilesPath);
        tilePaths[(int)TilePath.Grass] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("spike_tile", spritePaths[(int)SpritePath.Spike], tilesPath);
        tilePaths[(int)TilePath.Spike] = tileAssetPath;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        GameObject tilemapObject = ContentUtilities.CreateTilemapObject("PlatformsLayer");
        tilemapObject.tag = scriptPrefix + "Platforms";
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Platforms");
        // Get the grid object
        GameObject gridObject = GameObject.Find("Grid");
        // Create a hazards layer
        tilemapObject = ContentUtilities.CreateTilemapObject("HazardsLayer", gridObject);
        tilemapObject.tag = scriptPrefix + "Hazards";

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        Tile tile;
        // Earth
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Earth]);
        paletteTilemap.SetTile(Vector3Int.zero, tile);
        // Grass
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Grass]);
        paletteTilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        // Crate
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Crate]);
        paletteTilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        // Spike
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Spike]);
        paletteTilemap.SetTile(new Vector3Int(3, 0, 0), tile);
        // Earth, light
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.EarthLight]);
        paletteTilemap.SetTile(new Vector3Int(0, 1, 0), tile);
        // ... Add more tiles if needed
    }

    private static void GenerateMaterials()
    {
        string path;

        // Create physics 2D material for the player
        path = ContentUtilities.CreatePhysicsMaterial2D("PlayerPhysicsMaterial2D", 0.0f, 0.0f, texturesPath);
        materialPath = path;
    }

    private static void GenerateObjects()
    {
        GameObject newObject;
        Rigidbody2D rb;
        string assetPath;
        ContentUtilities.ColliderShape shape;
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;

        // Create the game manager object
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create objects then make them prefab
        // Player
        assetPath = spritePaths[(int)SpritePath.Player];
        shape = ContentUtilities.ColliderShape.Box;
        newObject = ContentUtilities.CreateTexturedBody("Player", 0.0f, 0.0f, assetPath, shape);
        // Add input
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Configure physics
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 2.0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath("Assets/" + materialPath, typeof(PhysicsMaterial2D)) as PhysicsMaterial2D;
        newObject.GetComponent<BoxCollider2D>().sharedMaterial = material;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Enemy
        assetPath = spritePaths[(int)SpritePath.Enemy];
        shape = ContentUtilities.ColliderShape.Box;
        newObject = ContentUtilities.CreateTexturedBody("Enemy", 0.0f, 0.0f, assetPath, shape);
        newObject.tag = scriptPrefix + "Enemy";
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.mass = 10000;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Coin
        assetPath = spritePaths[(int)SpritePath.Coin];
        shape = ContentUtilities.ColliderShape.Circle;
        newObject = ContentUtilities.CreateTexturedBody("Coin", 0.0f, 0.0f, assetPath, shape);
        newObject.tag = scriptPrefix + "Collectibles";
        newObject.GetComponent<Collider2D>().isTrigger = true;
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Objective
        newObject = new GameObject("Objective");
        newObject.tag = scriptPrefix + "Objective";
        BoxCollider2D collider = newObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.0f, 6.0f);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Start Point
        newObject = new GameObject("StartPoint");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void GenerateUI()
    {
        // Create canvas game object and event system
        GameObject canvasObject = ContentUtilities.CreateUICanvas();
        Transform parent = canvasObject.transform;

        const float margin = 10.0f;
        const int fontSize = 24;
        float w = 150.0f;
        float h = 40.0f;

        // Create score text panel
        GameObject scoreTextPanel = ContentUtilities.CreateUIBackgroundObject("ScoreTextPanel", w, h);
        ContentUtilities.AnchorUIObject(scoreTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create score text
        GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w / 2, h, "0", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(scoreTextObject, scoreTextPanel.transform, ContentUtilities.Anchor.Right, new Vector2(-margin, 0.0f));
        // Create coin image
        GameObject coinImageObject = ContentUtilities.CreateUIImageObject("CoinImage", h - 6, h - 6, texturesPath + "/coin_texture.png");
        ContentUtilities.AnchorUIObject(coinImageObject, scoreTextPanel.transform, ContentUtilities.Anchor.Left, new Vector2(margin, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "Too bad...", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create time text panel
        w = 200.0f;
        h = 40.0f;
        GameObject timeTextPanel = ContentUtilities.CreateUIBackgroundObject("TimeTextPanel", w, h);
        ContentUtilities.AnchorUIObject(scoreTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create time text
        GameObject loopTextObject = ContentUtilities.CreateUITextObject("TimeText", w, h, "Time: 00 : 00", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(loopTextObject, timeTextPanel.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create play button
        w = 150.0f;
        h = 40.0f;
        GameObject buttonObject = ContentUtilities.CreateUIButtonObject("PlayButton", w, h, "Play", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(buttonObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create help panel
        w = 600.0f;
        h = 240.0f;
        GameObject helpPanelObject = ContentUtilities.CreateUIBackgroundObject("HelpPanel", w, h, 0.9f);
        ContentUtilities.AnchorUIObject(helpPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create help panel text
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Your Character will Run Automatically");
        sb.AppendLine("Press Space Bar to Jump");
        sb.AppendLine("Avoid Bumping into Walls or Other Hazards");
        sb.AppendLine("Collect Coins and Survive as Long as Possible!");
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
        WriteER2DBlinkTextScriptToFile();
        WriteER2DEnemyScriptToFile();
        WriteER2DGameManagerScriptToFile();
        WriteER2DPlayerScriptToFile();
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Enemy", "GameManager", "Player" }))
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
        GameObject playerPrefab = ContentUtilities.LoadPrefab("Player", prefabsPath);
        GameObject enemyPrefab = ContentUtilities.LoadPrefab("Enemy", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Player", playerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", enemyPrefab);

        // Player prefab to set platform mask
        string className = scriptPrefix + "Player";
        ScriptUtilities.AssignLayerMaskToObject(LayerMask.GetMask(scriptPrefix + "Platforms"), playerPrefab, className, "platformsLayer");

        // Instantiate a copy of GameManager
        InstantiateAndSetupGameManager(gameManagerPrefab);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static int[] GetHazardsMapArray()
    {
        // Get this using the Reverse Engineer Menu Item
        return new int[]
        {
            // Array size (232, 11)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1,  3, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetPlatformsMapArray()
    {
        // Get this using the Reverse Engineer Menu Item
        return new int[]
        {
              // Array size (232, 11)
              1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  4,  1,  1,  1,  1, -1, -1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1, -1,  1,  1,  1,  4,  4,  4,  1,  1,  1,  1,  1,  4, -1, -1, -1,  4,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1, -1, -1, -1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,
              4,  1,  1,  1,  1,  1,  1,  4,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1, -1, -1,  1,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  4,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  4,  4,  1, -1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  4,  1, -1, -1, -1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  4,  1,  1,  4,  1,  1,  4, -1, -1, -1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,
              1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  4,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  4,  1,  1, -1, -1,  1,  4,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1, -1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1, -1, -1, -1,  1,  1,  4,  1,  4,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  4,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  4,  4,  1,  1,  1,  1,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1,  4,  4, -1, -1, -1,  1,  1,  4,  4,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  1,  4,  1,  1,  4,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  4,  1,  1,  1,
              1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4, -1, -1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  4,  1,  1,  1,  1, -1,  4,  1,  1,  4,  1,  1,  4,  1,  4,  1,  1,  1, -1, -1, -1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1, -1, -1, -1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  4,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  4,  1,  1,  1,  1,  1,  1,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2,  2,  2,  2, -1,  2,  2,  2,  2,  2,  2, -1, -1,  2,  2,  2,  2, -1, -1, -1,  2,  2, -1, -1,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2,  2, -1,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2, -1, -1, -1,  2,  2, -1, -1, -1, -1,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,  2, -1, -1,  2,  2,  2,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject platformsLayerObject = GameObject.Find("PlatformsLayer");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject timeTextObject = GameObject.Find("TimeText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject playerObject = GameObject.Find("Player");
        GameObject startPointObject = GameObject.Find("StartPoint");
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag(scriptPrefix + "Collectibles");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects or components
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(cameraObject, "Camera", go, className, "mainCamera");
        ScriptUtilities.AssignComponentFieldToObject(platformsLayerObject, "Tilemap", go, className, "tilemap");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(timeTextObject, "Text", go, className, "timeText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectFieldToObject(playerObject, go, className, "playerObject");
        ScriptUtilities.AssignObjectFieldToObject(startPointObject, go, className, "startPointObject");
        ScriptUtilities.AssignObjectsFieldToObject(collectibles, go, className, "collectibles");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
    }

    private static void LayoutLevel()
    {
        // Get the tile maps
        GameObject platformsLayerObject = GameObject.Find("PlatformsLayer");
        GameObject hazardsLayerObject = GameObject.Find("HazardsLayer");
        Tilemap platformsLayer = platformsLayerObject.GetComponent<Tilemap>();
        Tilemap hazardsLayer = hazardsLayerObject.GetComponent<Tilemap>();
        // Create a tiles guide (order matters)
        Tile[] tilesGuide = new Tile[tilePaths.Length];
        for (int i = 0; i < tilesGuide.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTileAtPath(tilePaths[i]);
        }
        // Configure size and start points
        Vector2Int size = new Vector2Int(GetPlatformsMapArray().Length/11, 11);
        Vector2Int start = new Vector2Int(0, -5);
        // Layout according to a map array
        ContentUtilities.PlotTiles(platformsLayer, tilesGuide, GetPlatformsMapArray(), size, start);
        ContentUtilities.PlotTiles(hazardsLayer, tilesGuide, GetHazardsMapArray(), size, start);

        // Add coins
        GameObject coinPrefab = ContentUtilities.LoadPrefab("Coin", prefabsPath);
        LayoutObject(coinPrefab, 65.0f, 4.0f);
        LayoutObject(coinPrefab, 120.0f, 3.5f);
        LayoutObject(coinPrefab, 164.0f, 6.5f);
        // Add enemies
        GameObject enemyPrefab = ContentUtilities.LoadPrefab("Enemy", prefabsPath);
        LayoutObject(enemyPrefab, 142.0f, 0.5f);
        // Add player object
        GameObject playerPrefab = ContentUtilities.LoadPrefab("Player", prefabsPath);
        LayoutObject(playerPrefab, 15.0f, 0.5f);
        // Add objective
        GameObject objectivePrefab = ContentUtilities.LoadPrefab("Objective", prefabsPath);
        GameObject objectiveObject = PrefabUtility.InstantiatePrefab(objectivePrefab) as GameObject;
        objectiveObject.transform.position = new Vector2(212.5f, 3.0f);
        // Add start point
        GameObject startPointPrefab = ContentUtilities.LoadPrefab("StartPoint", prefabsPath);
        GameObject startPointObject = PrefabUtility.InstantiatePrefab(startPointPrefab) as GameObject;
        startPointObject.transform.position = new Vector2(16.5f, 0.5f);

        // Done laying out, add rigid body, tile map collider, and composite collider
        // Tile layer
        platformsLayerObject.AddComponent<Rigidbody2D>();
        platformsLayerObject.AddComponent<TilemapCollider2D>();
        platformsLayerObject.AddComponent<CompositeCollider2D>();
        platformsLayerObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        platformsLayerObject.GetComponent<TilemapCollider2D>().usedByComposite = true;
        // Hazards layer
        hazardsLayerObject.AddComponent<Rigidbody2D>();
        hazardsLayerObject.AddComponent<TilemapCollider2D>();
        hazardsLayerObject.AddComponent<CompositeCollider2D>();
        hazardsLayerObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        hazardsLayerObject.GetComponent<TilemapCollider2D>().usedByComposite = true;
    }

    private static void LayoutCrateStrip(int index, int height, Tile tile, Tilemap tileMap)
    {
        const int mapStartX = 0;
        const int mapStartY = 0;
        Vector3Int marker = new Vector3Int(mapStartX + index, mapStartY, 0);

        if (height < 0)
        {
            if (Math.Abs(height) > 0)
            {
                // Paint hanging crate
                marker.y = Math.Abs(height) - 1;
                tileMap.SetTile(marker, tile);
            }
            return;
        }
        // Paint normal crate stack
        for (int i = 0; i < height; i++)
        {
            tileMap.SetTile(marker, tile);
            marker.y += 1;
        }
    }

    private static void LayoutGroundStrip(int index, int height, Tile groundTile, Tile grassTile, Tilemap tileMap)
    {
        if (height <= 0)
        {
            return;
        }

        const int mapStartX = 0;
        const int mapStartY = -5;
        Vector3Int marker = new Vector3Int(mapStartX + index, mapStartY, 0);
        for (int i = 0; i < height; i++)
        {
            Tile tile;
            if (i == height - 1)
            {
                tile = grassTile;
            }
            else
            {
                tile = groundTile;
            }
            tileMap.SetTile(marker, tile);
            marker.y += 1;
        }
    }

    private static void LayoutObject(GameObject coinPrefab, float worldX, float worldY)
    {
        GameObject newCoin = PrefabUtility.InstantiatePrefab(coinPrefab) as GameObject;
        newCoin.transform.position = new Vector2(worldX, worldY);
    }

    private static void LayoutSpikes(int index, int height, Tile tile, Tilemap tileMap)
    {
        const int mapStartX = 0;
        const int mapStartY = 0;
        Vector3Int marker = new Vector3Int(mapStartX + index, mapStartY, 0);

        if (height <= 0)
        {
            return;
        }
        // Paint normal crate stack
        marker.y += (height - 1);
        tileMap.SetTile(marker, tile);
    }

    private static void PostProcessTextures()
    {
        Sprite[] tempSprites = new Sprite[(int)SpritePath.Max];

        for (int i = 0; i < (int)SpritePath.Max; i++)
        {
            string path = "Assets/" + spritePaths[i];
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            ti.spritePixelsPerUnit = textureSize;
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

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        ReverseEngineerMapArrays();
        ReverseEngineerScripts();
        AssetDatabase.Refresh();
    }

    private static void ReverseEngineerMapArrays()
    {
        // Get tilemaps
        Tilemap platformsTilemap = GameObject.Find("PlatformsLayer").GetComponent<Tilemap>();
        Tilemap hazardsTilemap = GameObject.Find("HazardsLayer").GetComponent<Tilemap>();
        int w = platformsTilemap.size.x;
        int h = platformsTilemap.size.y;
        int startX = 0;
        int startY = -5;
        // Build tile guide
        string[] tileNames = new string[] { "grass_tile", "earth_tile", "crate_tile", "spike_tile", "earth_light_tile" };
        Tile[] tilesGuide = new Tile[(int)TilePath.Max];
        Assert.IsTrue(tileNames.Length == (int)TilePath.Max);
        for (int i = 0; i < tilePaths.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTile(tileNames[i], tilesPath);
        }
        // Reverse platforms layer
        int[] mapArray = ContentUtilities.ConvertTilemapToMapArray(platformsTilemap, tilesGuide, w, h, startX, startY);
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        Debug.Log("Printing " + platformsTilemap.name);
        Debug.Log(sb.ToString());
        // Reverse hazards layer
        mapArray = ContentUtilities.ConvertTilemapToMapArray(hazardsTilemap, tilesGuide, w, h, startX, startY);
        sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        Debug.Log("Printing " + hazardsTilemap.name);
        Debug.Log(sb.ToString());
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("ER2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("ER2DEnemy", EndlessRunner2D.scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("ER2DGameManager", EndlessRunner2D.scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("ER2DPlayer", EndlessRunner2D.scriptsPath);
    }

    private static void WriteER2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class ER2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("ER2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteER2DEnemyScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3700);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class ER2DEnemy : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private Vector2 startPos;");
        sb.AppendLine("    private AIState currentState = AIState.Idle;");
        sb.AppendLine("    private float range = 0.75f;");
        sb.AppendLine("    private float speed = 0.5f;");
        sb.AppendLine("");
        sb.AppendLine("    private enum AIState");
        sb.AppendLine("    {");
        sb.AppendLine("        Idle = 0,");
        sb.AppendLine("        PatrolLeft,");
        sb.AppendLine("        PatrolRight,");
        sb.AppendLine("        Max");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        startPos = new Vector2(transform.position.x, transform.position.y);");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // The enemy will simply patrol left and right, within a short range");
        sb.AppendLine("        if (currentState == AIState.Idle)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (Random.Range(0, 1) == 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                currentState = AIState.PatrolLeft;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                currentState = AIState.PatrolRight;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else ");
        sb.AppendLine("        {");
        sb.AppendLine("            float d = transform.position.x - startPos.x;");
        sb.AppendLine("            if (Mathf.Abs(d) > range)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (transform.position.x > startPos.x)");
        sb.AppendLine("                {");
        sb.AppendLine("                    currentState = AIState.PatrolLeft;");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    currentState = AIState.PatrolRight;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }    ");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ER2DGameManager.gameStarted != true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Do not update if game is not active");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Handle the simple patrol movement");
        sb.AppendLine("        if (currentState == AIState.PatrolLeft)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(-speed, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (currentState == AIState.PatrolRight)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(speed, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(0.0f, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("ER2DEnemy", scriptsPath, sb.ToString());
    }

    private static void WriteER2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(11461);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class ER2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Camera mainCamera;");
        sb.AppendLine("    public Tilemap tilemap;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text timeText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public GameObject playerObject;");
        sb.AppendLine("    public GameObject startPointObject;");
        sb.AppendLine("    public GameObject[] collectibles;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    private float timeSurvived = 0.0f;");
        sb.AppendLine("    private Vector2 fixedCameraDistance = new Vector2();");
        sb.AppendLine("    private float cameraStartY;");
        sb.AppendLine("");
        sb.AppendLine("    public static bool gameStarted = false;");
        sb.AppendLine("    public static ER2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Ensure only one instance exists");
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
        sb.AppendLine("        SetupObjects();");
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Start the game");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            playButton.gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (gameStarted != true)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        timeSurvived += Time.deltaTime;");
        sb.AppendLine("        UpdateTime();");
        sb.AppendLine("        UpdateCameraPosition();");
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
        sb.AppendLine("            // Enable the play button");
        sb.AppendLine("            playButton.gameObject.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if result panel is being displayed");
        sb.AppendLine("        else if (resultPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Close it");
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            // Enable the play button");
        sb.AppendLine("            playButton.gameObject.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if the play button is enabled");
        sb.AppendLine("        else if (playButton.gameObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Click it");
        sb.AppendLine("            TaskOnPlayButtonClick();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnClickAnywhere()");
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
        sb.AppendLine("            // Enable the play button");
        sb.AppendLine("            playButton.gameObject.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if result panel is being displayed");
        sb.AppendLine("        else if (resultPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Close it");
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            // Enable the play button");
        sb.AppendLine("            playButton.gameObject.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    public void AddScore(int change)");
        sb.AppendLine("    {");
        sb.AppendLine("        currentScore += change;");
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Too bad...\";");
        sb.AppendLine("        resultText.gameObject.SetActive(true);");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public Tile GetTile(Vector2 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3Int cellPos = tilemap.WorldToCell(position);");
        sb.AppendLine("        Tile tile = tilemap.GetTile(cellPos) as Tile;");
        sb.AppendLine("        return tile;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void LevelCleared()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector2 cameraPosition = mainCamera.transform.position;");
        sb.AppendLine("        Vector2 playerPosition = playerObject.transform.position;");
        sb.AppendLine("        // Teleport the player back to the start point");
        sb.AppendLine("        Vector2 difference = cameraPosition - playerPosition;");
        sb.AppendLine("        playerObject.transform.position = new Vector2(startPointObject.transform.position.x, playerPosition.y);");
        sb.AppendLine("        Vector2 playerPos = playerObject.transform.position;");
        sb.AppendLine("        // Teleport the camera back to the start point");
        sb.AppendLine("        mainCamera.transform.position = new Vector3(playerPos.x + difference.x, cameraPosition.y, -10.0f);");
        sb.AppendLine("        // Reactivate the coins");
        sb.AppendLine("        foreach (GameObject collectible in collectibles)");
        sb.AppendLine("        {");
        sb.AppendLine("            collectible.SetActive(true);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set up objects");
        sb.AppendLine("        cameraStartY = mainCamera.transform.position.y;");
        sb.AppendLine("        fixedCameraDistance.x = mainCamera.transform.position.x - playerObject.transform.position.x;");
        sb.AppendLine("        fixedCameraDistance.y = mainCamera.transform.position.y - playerObject.transform.position.y;");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("        UpdateTime();");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(SceneManager.GetActiveScene().name);");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateCameraPosition()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Make the camera follow the player");
        sb.AppendLine("        Vector2 playerPos = playerObject.transform.position;");
        sb.AppendLine("        float newCameraX = playerPos.x + fixedCameraDistance.x;");
        sb.AppendLine("        float newCameraY = cameraStartY;");
        sb.AppendLine("        // Ease the camera to the desired position");
        sb.AppendLine("        Vector3 cameraPos = mainCamera.transform.position;");
        sb.AppendLine("        float dt = Time.deltaTime;");
        sb.AppendLine("        float k = 2.5f;");
        sb.AppendLine("        mainCamera.transform.position = new Vector3(cameraPos.x + k * dt * (newCameraX - cameraPos.x), cameraPos.y + k * dt * (newCameraY - cameraPos.y), cameraPos.z);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateScore()");
        sb.AppendLine("    {");
        sb.AppendLine("        scoreText.text = currentScore.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateTime()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Max value is 1 hour");
        sb.AppendLine("        const float secondsInOneHour = 3599.0f;");
        sb.AppendLine("        float value = Mathf.Min(timeSurvived, secondsInOneHour);");
        sb.AppendLine("        // Extract minutes and seconds");
        sb.AppendLine("        int minutes = (int)(value / 60.0f);");
        sb.AppendLine("        int seconds = (int)(value - (minutes * 60.0f));");
        sb.AppendLine("        // Print text in 00 : 00 format");
        sb.AppendLine("        timeText.text = \"Time: \" + minutes.ToString(\"D2\") + \" : \" + seconds.ToString(\"D2\");");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("ER2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteER2DPlayerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(11038);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class ER2DPlayer : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public LayerMask platformsLayer;");
        sb.AppendLine("    public float jumpForce = 650.0f;");
        sb.AppendLine("    public float runSpeed = 4.0f;");
        sb.AppendLine("    public float coyoteMoment = 0.2f;");
        sb.AppendLine("    private ER2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private BoxCollider2D boxCollider;");
        sb.AppendLine("    private float footOffset;");
        sb.AppendLine("    private float groundDistance;");
        sb.AppendLine("    private float rayLength = 0.2f;");
        sb.AppendLine("    private float coyoteTimer = 0.0f;");
        sb.AppendLine("    private bool isDead = false;");
        sb.AppendLine("    private bool isGrounded = false;");
        sb.AppendLine("    private bool jumpCommand = false;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Save variables");
        sb.AppendLine("        gameManager = ER2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        boxCollider = GetComponent<BoxCollider2D>();");
        sb.AppendLine("        footOffset = boxCollider.bounds.size.x * 0.5f;");
        sb.AppendLine("        groundDistance = boxCollider.size.y * 0.5f;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isDead)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail if the player is dead");
        sb.AppendLine("            rb.velocity = new Vector2(0.0f, rb.velocity.y);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Enforce coyote moment (where the player stays in mid-air for a short while before falling)");
        sb.AppendLine("        bool oldGrounded = isGrounded;");
        sb.AppendLine("        isGrounded = IsGrounded();");
        sb.AppendLine("        const float threshold = 0.1f;");
        sb.AppendLine("        if (!isGrounded && oldGrounded && rb.velocity.y <= threshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            coyoteTimer = coyoteMoment;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (coyoteTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            coyoteTimer -= Time.deltaTime;");
        sb.AppendLine("            FreezeInMidAir();");
        sb.AppendLine("        }");
        sb.AppendLine("        if (transform.position.y < -5.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Kill the player if he falls too low");
        sb.AppendLine("            Kill();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (ER2DGameManager.gameStarted == false)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Set forward speed");
        sb.AppendLine("        rb.velocity = new Vector2(runSpeed, rb.velocity.y);");
        sb.AppendLine("        // Handle jump");
        sb.AppendLine("        if (jumpCommand)");
        sb.AppendLine("        {");
        sb.AppendLine("            jumpCommand = false;");
        sb.AppendLine("            Jump();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isDead)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If ran into an enemy");
        sb.AppendLine("        if (collidedObject.CompareTag(\"ER2DEnemy\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Kill the player");
        sb.AppendLine("            Kill();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionStay2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isDead)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // If colliding with the platforms");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        bool collidingPlatforms = collidedObject.CompareTag(\"ER2DPlatforms\");");
        sb.AppendLine("        bool collidingHazards = collidedObject.CompareTag(\"ER2DHazards\");");
        sb.AppendLine("        if (collidingHazards || collidingPlatforms)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Get contact points");
        sb.AppendLine("            const float threshold = 0.4f;");
        sb.AppendLine("            int contactCount = collision.contactCount;");
        sb.AppendLine("            ContactPoint2D[] contacts = new ContactPoint2D[contactCount];");
        sb.AppendLine("            collision.GetContacts(contacts);");
        sb.AppendLine("            Vector3 hitPosition = Vector3.zero;");
        sb.AppendLine("            // For each contact point");
        sb.AppendLine("            for (int i = 0; i < contactCount; i++)");
        sb.AppendLine("            {");
        sb.AppendLine("                ContactPoint2D point = contacts[i];");
        sb.AppendLine("                // If standing on a hazard");
        sb.AppendLine("                if (point.normal.y >= threshold && !isGrounded && collidingHazards)");
        sb.AppendLine("                {");
        sb.AppendLine("                    hitPosition.x = contacts[i].point.x;");
        sb.AppendLine("                    hitPosition.y = contacts[i].point.y - 0.5f;");
        sb.AppendLine("                    // Kill the player");
        sb.AppendLine("                    Kill();");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("                // If ran face-first into a hazard or platform");
        sb.AppendLine("                if (point.normal.x == -1.0f)");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Kill the player");
        sb.AppendLine("                    Kill();");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    ");
        sb.AppendLine("    private void OnTriggerEnter2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isDead)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If ran into a coin");
        sb.AppendLine("        if (collidedObject.CompareTag(\"ER2DCollectibles\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Pick it up");
        sb.AppendLine("            gameManager.AddScore(1);");
        sb.AppendLine("            collision.gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if triggered the objective marker");
        sb.AppendLine("        else if (collidedObject.CompareTag(\"ER2DObjective\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Notify the game that the loop is cleared");
        sb.AppendLine("            gameManager.LevelCleared();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnJump()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ER2DGameManager.gameStarted == false)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (isDead)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        jumpCommand = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FreezeInMidAir()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (rb.velocity.y < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(rb.velocity.x, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool IsGrounded()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If already leaving ground");
        sb.AppendLine("        const float threshold = 0.5f;");
        sb.AppendLine("        if (rb.velocity.y > threshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Return not grounded");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // With two rays, check if the player is grounded on a platform");
        sb.AppendLine("        RaycastHit2D leftCheck = Raycast(new Vector2(-footOffset, -groundDistance), Vector2.down, rayLength, platformsLayer);");
        sb.AppendLine("        RaycastHit2D rightCheck = Raycast(new Vector2(footOffset, -groundDistance), Vector2.down, rayLength, platformsLayer);");
        sb.AppendLine("");
        sb.AppendLine("        // Return if either ray hit the platforms");
        sb.AppendLine("        return leftCheck || rightCheck;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Jump()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isGrounded == false && coyoteTimer <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Can jump if grounded or having a Wile E. Coyote's off the cliff moment");
        sb.AppendLine("        coyoteTimer = 0.0f;");
        sb.AppendLine("        isGrounded = false;");
        sb.AppendLine("        rb.AddForce(new Vector2(0.0f, jumpForce));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Kill()");
        sb.AppendLine("    {");
        sb.AppendLine("        isDead = true;");
        sb.AppendLine("        rb.velocity = new Vector2(0.0f, rb.velocity.y);");
        sb.AppendLine("        gameManager.GameOver();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    RaycastHit2D Raycast(Vector2 offset, Vector2 rayDirection, float length, LayerMask mask)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector2 pos = transform.position;");
        sb.AppendLine("");
        sb.AppendLine("        RaycastHit2D hit = Physics2D.Raycast(pos + offset, rayDirection, length, mask);");
        sb.AppendLine("");
        sb.AppendLine("        return hit;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("ER2DPlayer", scriptsPath, sb.ToString());
    }
}
