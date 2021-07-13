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

public class JewelHunter2D : Editor
{
    private const string templateName = "JewelHunter2D";
    private const string templateSpacedName = "Jewel Hunter 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "JH2D";
    private const int textureSize = 64;
    private const int mapWidth = 28;
    private const int mapHeight = 31;
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
        Player = 0,
        Enemy,
        Scared,
        Eaten,
        Jewel,
        Powerup,
        Wall,
        WallLeft,
        WallTopLeft,
        WallTop,
        WallTopRight,
        WallRight,
        WallBottomRight,
        WallBottom,
        WallBottomLeft,
        Ground,
        EnemyGate,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];

    private enum TilePath
    {
        Ground = 0,
        EnemyGate,
        WallTopLeft,
        WallTop,
        WallTopRight,
        WallLeft,
        Wall,
        WallRight,
        WallBottomLeft,
        WallBottom,
        WallBottomRight,
        Max
    };
    static string[] tilePaths = new string[(int)TilePath.Max];
    static Vector3Int[] tileCoordinates = {
        new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0),
        new Vector3Int(2, 0, 0), new Vector3Int(3, 0, 0), new Vector3Int(4, 0, 0),
        new Vector3Int(2, 1, 0), new Vector3Int(3, 1, 0), new Vector3Int(4, 1, 0),
        new Vector3Int(2, 2, 0), new Vector3Int(3, 2, 0), new Vector3Int(4, 2, 0)
    };

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
        // Tags: 
        ScriptUtilities.RemoveTag(scriptPrefix + "Player");
        ScriptUtilities.RemoveTag(scriptPrefix + "Enemy");
        ScriptUtilities.RemoveTag(scriptPrefix + "Collectible");
        ScriptUtilities.RemoveTag(scriptPrefix + "ExitPoint");
        // Layers: to sort collision detections
        int playerLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Player");
        int enemyLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Enemy");
        int collectibleLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Collectible");
        // Set layer collision matrix for the moving objects
        // Player 
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, collectibleLayer, false);
        // Enemy
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, false);
        Physics2D.IgnoreLayerCollision(enemyLayer, collectibleLayer, false);
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
        // Set up camera
        GameObject mainCameraObject = GameObject.Find("Main Camera");
        mainCameraObject.GetComponent<Camera>().orthographicSize = 17.0f;
        mainCameraObject.transform.position = new Vector3(mapWidth / 2, mapHeight / 2, -10.0f);
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags: enemy, player (already exist) for factions
        ScriptUtilities.CreateTag(scriptPrefix + "Player");
        ScriptUtilities.CreateTag(scriptPrefix + "Enemy");
        ScriptUtilities.CreateTag(scriptPrefix + "Collectible");
        ScriptUtilities.CreateTag(scriptPrefix + "ExitPoint");
        // Layers: to sort collision detections
        int playerLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Player");
        int enemyLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Enemy");
        int collectibleLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Collectible");
        // Set layer collision matrix for the moving objects
        // Player 
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, collectibleLayer, false);
        // Enemy
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        Physics2D.IgnoreLayerCollision(enemyLayer, collectibleLayer, true);
    }

    private static void GenerateAssets()
    {
        GenerateInputActions();
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
        // Create Move action and add bindings
        var action = map.AddAction("Move");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

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
        Color playerColor = new Color(232.0f / 255.0f, 186.0f / 255.0f, 176.0f / 255.0f);
        Color hatColor = new Color(48.0f / 255.0f, 49.0f / 255.0f, 50.0f / 255.0f);
        Color enemyColor = new Color(74.0f / 255.0f, 171.0f / 255.0f, 128.0f / 255.0f);
        Color eyeWhite = new Color(222.0f / 255.0f, 222.0f / 255.0f, 222.0f / 255.0f);
        Color eyeBlue = new Color(33.0f / 255.0f, 33.0f / 255.0f, 222.0f / 255.0f);
        Color scaredBlue = new Color(22.0f / 255.0f, 0.0f / 255.0f, 253.0f / 255.0f);
        Color jewelColor = new Color(112.0f / 255.0f, 209.0f / 255.0f, 244.0f / 255.0f);
        Color wallColor = new Color(152.0f / 255.0f, 124.0f / 255.0f, 70.0f / 255.0f);
        Color groundColor = new Color(103.0f / 255.0f, 79.0f / 255.0f, 32.0f / 255.0f);
        Color enemyGate = new Color(253.0f / 255.0f, 185.0f / 255.0f, 200.0f / 255.0f);

        // Generate textures
        // Player
        DrawPlayerTexture(playerColor, hatColor);

        // Enemy
        path = DrawEnemyTexture("texture_enemy", enemyColor, eyeWhite, eyeBlue);
        spritePaths[(int)SpritePath.Enemy] = path;
        // Scared body
        path = DrawEnemyTexture("texture_scared", scaredBlue, eyeWhite, eyeBlue);
        spritePaths[(int)SpritePath.Scared] = path;
        // Eaten body
        path = DrawEnemyTexture("texture_eaten", Color.clear, eyeWhite, eyeBlue);
        spritePaths[(int)SpritePath.Eaten] = path;
        // Collectible
        // Jewel
        path = ContentUtilities.CreateTexture2DDiamondAsset("texture_jewel", texturesPath, textureSize / 4, textureSize / 4, jewelColor);
        spritePaths[(int)SpritePath.Jewel] = path;
        // Powerup
        path = ContentUtilities.CreateTexture2DDiamondAsset("texture_powerup", texturesPath, textureSize, textureSize, jewelColor);
        spritePaths[(int)SpritePath.Powerup] = path;
        // Environment 
        // Wall
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_wall", texturesPath, textureSize, textureSize, wallColor);
        spritePaths[(int)SpritePath.Wall] = path;
        // Wall - top left corner
        const int thickness = textureSize / 2;
        path = ContentUtilities.CreateSegmentTexture2DCorner("texture_wall_topleft", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Corner.TopLeft, thickness);
        spritePaths[(int)SpritePath.WallTopLeft] = path;
        path = ContentUtilities.CreateSegmentTexture2DCorner("texture_wall_topright", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Corner.TopRight, thickness);
        spritePaths[(int)SpritePath.WallTopRight] = path;
        path = ContentUtilities.CreateSegmentTexture2DCorner("texture_wall_bottomleft", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Corner.BottomLeft, thickness);
        spritePaths[(int)SpritePath.WallBottomLeft] = path;
        path = ContentUtilities.CreateSegmentTexture2DCorner("texture_wall_bottomright", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Corner.BottomRight, thickness);
        spritePaths[(int)SpritePath.WallBottomRight] = path;

        path = ContentUtilities.CreateSegmentTexture2DHalf("texture_wall_left", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Side.Left, thickness);
        spritePaths[(int)SpritePath.WallLeft] = path;
        path = ContentUtilities.CreateSegmentTexture2DHalf("texture_wall_top", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Side.Top, thickness);
        spritePaths[(int)SpritePath.WallTop] = path;
        path = ContentUtilities.CreateSegmentTexture2DHalf("texture_wall_right", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Side.Right, thickness);
        spritePaths[(int)SpritePath.WallRight] = path;
        path = ContentUtilities.CreateSegmentTexture2DHalf("texture_wall_bottom", texturesPath, textureSize, textureSize, wallColor, ContentUtilities.Side.Bottom, thickness);
        spritePaths[(int)SpritePath.WallBottom] = path;

        // Ground
        path = ContentUtilities.CreateTexture2DRectangleAsset("texture_ground", texturesPath, textureSize, textureSize, groundColor);
        spritePaths[(int)SpritePath.Ground] = path;
        // Enemy gate
        path = ContentUtilities.CreateSegmentTexture2DHalf("texture_enemygate", texturesPath, textureSize, textureSize, enemyGate, ContentUtilities.Side.Bottom, thickness);
        spritePaths[(int)SpritePath.EnemyGate] = path;
    }

    private static void GenerateObjects()
    {
        GameObject newObject;
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;

        // Create the game manager object
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Enemy base
        GameObject enemyHouseObject = new GameObject("EnemyHouse");
        enemyHouseObject.transform.position = new Vector2(14.0f, 14.5f);
        BoxCollider2D collider = enemyHouseObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(6, 3);
        collider.isTrigger = true;

        // Create spawn points
        GameObject spawnPoints = new GameObject("SpawnPoints");
        Transform parent = spawnPoints.transform;
        CreateSpawnPoint("PlayerSpawnPoint", parent, 14.0f, 23.5f);
        CreateSpawnPoint("Enemy1SpawnPoint", parent, 14.0f, 11.5f);
        CreateSpawnPoint("Enemy2SpawnPoint", parent, 14.0f, 14.5f);
        CreateSpawnPoint("Enemy3SpawnPoint", parent, 12.0f, 14.5f);
        CreateSpawnPoint("Enemy4SpawnPoint", parent, 16.0f, 14.5f);

        // Create enemy leaving cell exit points
        GameObject exitPoints = new GameObject("ExitPoints");
        parent = exitPoints.transform;
        CreateExitPoint("ExitPointNW", parent, 9.5f, 20.5f);
        CreateExitPoint("ExitPointNE", parent, 18.5f, 20.5f);
        CreateExitPoint("ExitPointW", parent, 6.5f, 14.5f);
        CreateExitPoint("ExitPointE", parent, 21.5f, 14.5f);
        CreateExitPoint("ExitPointSW", parent, 9.5f, 5.5f);
        CreateExitPoint("ExitPointSE", parent, 18.5f, 5.5f);

        // Create a container for consumables
        newObject = new GameObject("Collectible");

        // Create objects then make them prefab
        // Assemble Player and its clone, a clone is used when it is crossing the warp tunnel
        AssemblePlayerPrefab("Player", SpritePath.Player);
        AssemblePlayerPrefab("PlayerClone", SpritePath.Player);

        // Assemble the enemies and their clones
        AssembleEnemyPrefab("Enemy", SpritePath.Enemy);
        AssembleEnemyPrefab("EnemyClone", SpritePath.Enemy);

        // Create consumables
        // Jewel
        CreateCollectiblePrefab("Jewel", SpritePath.Jewel);
        // Powerup
        CreateCollectiblePrefab("Powerup", SpritePath.Powerup);

        // Create path finding nodes container
        new GameObject("PathFinder");
        // Create node prefab
        newObject = new GameObject("PathNode");
        ContentUtilities.CreatePrefab(newObject, prefabsPath);

        // Create side bars to hide characters going through the warp tunnels
        CreateBars(spritePaths[(int)SpritePath.Wall]);
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
        GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w - margin, h, "Score: 999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(scoreTextObject, scoreTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create life text panel
        float offsetY = -h;
        GameObject lifeTextPanel = ContentUtilities.CreateUIBackgroundObject("LifeTextPanel", w, h);
        ContentUtilities.AnchorUIObject(lifeTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create life text
        GameObject lifeTextObject = ContentUtilities.CreateUITextObject("LifeText", w - margin, h, "Life: 3", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(lifeTextObject, lifeTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "You've Broken Free!", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

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
        sb.AppendLine("Use Arrow Keys or WASD to Control the Character");
        sb.AppendLine("Don't get Caught by the Enemies!");
        sb.AppendLine("Collect all Jewels to Win");
        sb.AppendLine("Collect the Power Up and the Enemies will Fear You!");
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
        WriteJH2DBlinkTextScriptToFile();
        WriteJH2DCharacterScriptToFile();
        WriteJH2DEnemyScriptToFile();
        WriteJH2DCollectibleScriptToFile();
        WriteJH2DGameManagerScriptToFile();
        WriteJH2DPathFinderScriptToFile();
        WriteJH2DPathNodeScriptToFile();
        WriteJH2DPlayerScriptToFile();
    }

    private static void GenerateTileMap()
    {
        string tileAssetPath;

        // Create tile asset
        tileAssetPath = ContentUtilities.CreateTileAsset("ground_tile", spritePaths[(int)SpritePath.Ground], tilesPath);
        tilePaths[(int)TilePath.Ground] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("enemygate_tile", spritePaths[(int)SpritePath.EnemyGate], tilesPath);
        tilePaths[(int)TilePath.EnemyGate] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile", spritePaths[(int)SpritePath.Wall], tilesPath);
        tilePaths[(int)TilePath.Wall] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_left", spritePaths[(int)SpritePath.WallLeft], tilesPath);
        tilePaths[(int)TilePath.WallLeft] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_topleft", spritePaths[(int)SpritePath.WallTopLeft], tilesPath);
        tilePaths[(int)TilePath.WallTopLeft] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_top", spritePaths[(int)SpritePath.WallTop], tilesPath);
        tilePaths[(int)TilePath.WallTop] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_topright", spritePaths[(int)SpritePath.WallTopRight], tilesPath);
        tilePaths[(int)TilePath.WallTopRight] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_right", spritePaths[(int)SpritePath.WallRight], tilesPath);
        tilePaths[(int)TilePath.WallRight] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_bottomright", spritePaths[(int)SpritePath.WallBottomRight], tilesPath);
        tilePaths[(int)TilePath.WallBottomRight] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_bottom", spritePaths[(int)SpritePath.WallBottom], tilesPath);
        tilePaths[(int)TilePath.WallBottom] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("wall_tile_bottomleft", spritePaths[(int)SpritePath.WallBottomLeft], tilesPath);
        tilePaths[(int)TilePath.WallBottomLeft] = tileAssetPath;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        // Ground layer
        GameObject tilemapObject;
        tilemapObject = ContentUtilities.CreateTilemapObject("GroundLayer");
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 0;
        // Find the automatically created Grid object
        GameObject gridObject = GameObject.Find("Grid");
        // Gate layer
        tilemapObject = ContentUtilities.CreateTilemapObject("GateLayer", gridObject);
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 1;
        // Upper layer
        tilemapObject = ContentUtilities.CreateTilemapObject("UpperLayer", gridObject);
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 2;

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        Tile tile;
        // 0 - ground, 1 - wall
        // Ground
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Ground]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.Ground], tile);
        // Enemy gate
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.EnemyGate]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.EnemyGate], tile);

        // Wall - top left
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallTopLeft]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallTopLeft], tile);
        // Wall - top
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallTop]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallTop], tile);
        // Wall - top right
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallTopRight]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallTopRight], tile);
        // Wall - left
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallLeft]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallLeft], tile);
        // Wall - body
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Wall]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.Wall], tile);
        // Wall - right
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallRight]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallRight], tile);
        // Wall - bottom left
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallBottomLeft]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallBottomLeft], tile);
        // Wall - bottom
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallBottom]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallBottom], tile);
        // Wall - bottom right
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.WallBottomRight]);
        paletteTilemap.SetTile(tileCoordinates[(int)TilePath.WallBottomRight], tile);

        // ... Add more tiles if needed
    }

    private static void AssembleEnemyPrefab(string name, SpritePath textureRef)
    {
        // Body
        string assetPath = spritePaths[(int)textureRef];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Box;
        GameObject body = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, assetPath, shape);
        body.tag = scriptPrefix + "Enemy";
        body.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Enemy");
        body.GetComponent<SpriteRenderer>().sortingOrder = 2;
        body.GetComponent<Collider2D>().isTrigger = true;
        // Configure rigid body
        Rigidbody2D rb = body.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0.0f;
        // Create prefab
        ContentUtilities.CreatePrefab(body, prefabsPath, true);
    }

    private static void AssemblePlayerPrefab(string name, SpritePath bodyTexture)
    {
        string assetPath = spritePaths[(int)bodyTexture];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Box;
        GameObject newObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, assetPath, shape);
        newObject.tag = scriptPrefix + "Player";
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Player");
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 2;
        newObject.GetComponent<Collider2D>().isTrigger = true;
        // Configure size
        BoxCollider2D collider = newObject.GetComponent<BoxCollider2D>();
        collider.size *= 0.8f;
        // Configure rigid body
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.freezeRotation = true;
        rb.gravityScale = 0.0f;
        // Make this a prefab
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void AssignCharacterStats(GameObject spawnPoint, float speed, GameObject gameObject, GameObject clonePrefab, string className)
    {
        ScriptUtilities.AssignObjectFieldToObject(spawnPoint, gameObject, className, "spawnPoint");
        ScriptUtilities.AssignObjectFieldToObject(clonePrefab, gameObject, className, "clonePrefab");
        ScriptUtilities.AssignFloatFieldToObject(speed, gameObject, className, "speed");
    }

    private static void CreateBars(string spritePath)
    {
        int sortingOrder = 3;
        // Create a container
        GameObject container = new GameObject("Bars");
        // Left bar
        GameObject newObject = ContentUtilities.CreateTexturedFigment("LeftBar", -1.5f, 15.5f, spritePath);
        newObject.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        newObject.transform.localScale = new Vector3(3.0f, 31.0f, 1.0f);
        newObject.transform.SetParent(container.transform, true);
        // Right bar
        newObject = ContentUtilities.CreateTexturedFigment("RightBar", 29.5f, 15.5f, spritePath);
        newObject.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        newObject.transform.localScale = new Vector3(3.0f, 31.0f, 1.0f);
        newObject.transform.SetParent(container.transform, true);
    }

    private static void CreateExitPoint(string name, Transform parent, float posX, float posY)
    {
        GameObject newObject = new GameObject(name);
        newObject.tag = scriptPrefix + "ExitPoint";
        newObject.transform.position = new Vector2(posX, posY);
        newObject.transform.SetParent(parent, true);
    }

    private static void CreateCollectiblePrefab(string name, SpritePath textureRef)
    {
        string assetPath = spritePaths[(int)textureRef];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Box;
        GameObject newObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, assetPath, shape);
        newObject.tag = scriptPrefix + "Collectible";
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Collectible");
        DestroyImmediate(newObject.GetComponent<Rigidbody2D>());
        newObject.GetComponent<BoxCollider2D>().isTrigger = true;
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void CreateSpawnPoint(string name, Transform parent, float posX, float posY)
    {
        GameObject newObject = new GameObject(name);
        newObject.transform.position = new Vector2(posX, posY);
        newObject.transform.SetParent(parent, true);
    }

    private static string DrawEnemyTexture(string name, Color bodyColor, Color eyeWhiteColor, Color pupilColor)
    {
        int bodyW = 2 * textureSize;
        int bodyH = 2 * textureSize;
        int eyeW = textureSize / 2;
        int eyeH = textureSize / 2;
        int pupilW = textureSize / 4;
        int pupilH = textureSize / 4;
        // Create color arrays
        Color[] body = ContentUtilities.FillBitmapShapeRectangle(bodyW, bodyH, bodyColor);
        Color[] eye = ContentUtilities.FillBitmapShapeRectangle(eyeW, eyeH, eyeWhiteColor);
        Color[] pupil = ContentUtilities.FillBitmapShapeRectangle(pupilW, pupilH, pupilColor);
        // Combine an eyeball
        eye = ContentUtilities.CopyBitmap(pupil, pupilW, pupilH, eye, eyeW, eyeH, new Vector2Int(eyeW / 4, eyeH / 4));
        // Draw left eye
        Color[] combined = ContentUtilities.CopyBitmap(eye, eyeW, eyeH, body, bodyW, bodyH, new Vector2Int(bodyW / 4, bodyH / 2));
        // Draw right eye
        combined = ContentUtilities.CopyBitmap(eye, eyeW, eyeH, combined, bodyW, bodyH, new Vector2Int(bodyW / 2, bodyH / 2));
        // Generate a texture asset from the combined array
        string path = ContentUtilities.CreateBitmapAsset(name, combined, bodyW, bodyH, texturesPath);
        // Return a path for later access
        return path;
    }

    private static void DrawPlayerTexture(Color bodyColor, Color hatColor)
    {
        int bgW = 2 * textureSize;
        int bgH = 2 * textureSize; 
        int bodyW = (int)(0.75f * bgW);
        int bodyH = (int)(0.75f * bgH);
        int hatW = (int)(0.6f * bgW);
        int hatH = (int)(0.5f * bgW);
        int rimW = bgW;
        int rimH = (int)(0.125f * bgH);
        // Create color arrays
        Color[] bg = ContentUtilities.FillBitmap(bgW, bgH, Color.clear);
        Color[] body = ContentUtilities.FillBitmapShapeCircle(bodyW, bodyH, bodyColor);
        Color[] hat = ContentUtilities.FillBitmapShapeRectangle(hatW, hatH, hatColor);
        Color[] rim = ContentUtilities.FillBitmapShapeRectangle(rimW, rimH, hatColor);
        // Combine the pieces
        Color[] combined = ContentUtilities.CopyBitmap(body, bodyW, bodyH, bg, bgW, bgH, new Vector2Int((bgW - bodyW) / 2, 0));
        combined = ContentUtilities.CopyBitmap(hat, hatW, hatH, combined, bgW, bgH, new Vector2Int((bgW - hatW) / 2, bgH - hatH));
        combined = ContentUtilities.CopyBitmap(rim, rimW, rimH, combined, bgW, bgH, new Vector2Int(0, bgH - hatH));
        // Generate a texture asset from the combined array
        string path = ContentUtilities.CreateBitmapAsset("texture_player", combined, bgW, bgH, texturesPath);
        spritePaths[(int)SpritePath.Player] = path;
    }

    private static int[] GetCollectibleMapArray()
    {
        return new int[] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1,  1, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1, -1,
            -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  0, -1, -1, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1,  0, -1, -1, -1, -1,  0, -1,
            -1,  1,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  1, -1,
            -1, -1, -1,  0, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1,  0, -1, -1, -1,
            -1, -1, -1,  0, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1,  0, -1, -1, -1,
            -1,  0,  0,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0, -1, -1,  0,  0,  0,  0,  0,  0, -1,
            -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1,
            -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1, -1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  0, -1,
            -1,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetGroundLayerMapArray()
    {
        return new int[] {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
        };
    }

    private static int[] GetGateLayerMapArray()
    {
        return new int[] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetUpperLayerMapArray()
    {
        return new int[] {
            6,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  6,  6,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  3,  6,
            7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,
            7, -1,  8,  9,  9, 10, -1,  8,  9,  9,  9, 10, -1,  5,  7, -1,  8,  9,  9,  9, 10, -1,  8,  9,  9, 10, -1,  5,
            7, -1,  5,  6,  6,  7, -1,  5,  6,  6,  6,  7, -1,  5,  7, -1,  5,  6,  6,  6,  7, -1,  5,  6,  6,  7, -1,  5,
            7, -1,  2,  3,  3,  4, -1,  2,  3,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  3,  4, -1,  2,  3,  3,  4, -1,  5,
            7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,
            7, -1,  8,  9,  9, 10, -1,  8, 10, -1,  8,  9,  9,  9,  9,  9,  9, 10, -1,  8, 10, -1,  8,  9,  9, 10, -1,  5,
            7, -1,  2,  3,  3,  4, -1,  5,  7, -1,  2,  3,  3,  6,  6,  3,  3,  4, -1,  5,  7, -1,  2,  3,  3,  4, -1,  5,
            7, -1, -1, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1, -1, -1,  5,
            6,  9,  9,  9,  9, 10, -1,  5,  6,  9,  9, 10, -1,  5,  7, -1,  8,  9,  9,  6,  7, -1,  8,  9,  9,  9,  9,  6,
            6,  6,  6,  6,  6,  7, -1,  5,  6,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  6,  7, -1,  5,  6,  6,  6,  6,  6,
            6,  6,  6,  6,  6,  7, -1,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,  7, -1,  5,  6,  6,  6,  6,  6,
            6,  6,  6,  6,  6,  7, -1,  5,  7, -1,  8,  9,  9, -1, -1,  9,  9, 10, -1,  5,  7, -1,  5,  6,  6,  6,  6,  6,
            3,  3,  3,  3,  3,  4, -1,  2,  4, -1,  5, -1, -1, -1, -1, -1, -1,  7, -1,  2,  4, -1,  2,  3,  3,  3,  3,  3,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5, -1, -1, -1, -1, -1, -1,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            9,  9,  9,  9,  9, 10, -1,  8, 10, -1,  5, -1, -1, -1, -1, -1, -1,  7, -1,  8, 10, -1,  8,  9,  9,  9,  9,  9,
            6,  6,  6,  6,  6,  7, -1,  5,  7, -1,  2,  3,  3,  3,  3,  3,  3,  4, -1,  5,  7, -1,  5,  6,  6,  6,  6,  6,
            6,  6,  6,  6,  6,  7, -1,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,  7, -1,  5,  6,  6,  6,  6,  6,
            6,  6,  6,  6,  6,  7, -1,  5,  7, -1,  8,  9,  9,  9,  9,  9,  9, 10, -1,  5,  7, -1,  5,  6,  6,  6,  6,  6,
            6,  3,  3,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  6,  6,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  3,  3,  6,
            7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,
            7, -1,  8,  9,  9, 10, -1,  8,  9,  9,  9, 10, -1,  5,  7, -1,  8,  9,  9,  9, 10, -1,  8,  9,  9, 10, -1,  5,
            7, -1,  2,  3,  6,  7, -1,  2,  3,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  3,  4, -1,  5,  6,  3,  4, -1,  5,
            7, -1, -1, -1,  5,  7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,  7, -1, -1, -1,  5,
            6,  9, 10, -1,  5,  7, -1,  8, 10, -1,  8,  9,  9,  9,  9,  9,  9, 10, -1,  8, 10, -1,  5,  7, -1,  8,  9,  6,
            6,  3,  4, -1,  2,  4, -1,  5,  7, -1,  2,  3,  3,  6,  6,  3,  3,  4, -1,  5,  7, -1,  2,  4, -1,  2,  3,  6,
            7, -1, -1, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1,  5,  7, -1, -1, -1, -1, -1, -1,  5,
            7, -1,  8,  9,  9,  9,  9,  6,  6,  9,  9, 10, -1,  5,  7, -1,  8,  9,  9,  6,  6,  9,  9,  9,  9, 10, -1,  5,
            7, -1,  2,  3,  3,  3,  3,  3,  3,  3,  3,  4, -1,  2,  4, -1,  2,  3,  3,  3,  3,  3,  3,  3,  3,  4, -1,  5,
            7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  5,
            6,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  9,  6
        };
    }

    private static void LayoutCollectible(int[] mapArray)
    {
        GameObject container = GameObject.Find("Collectible");
        Transform parentTransform = container.transform;
        GameObject jewelPrefab = ContentUtilities.LoadPrefab("Jewel", prefabsPath);
        GameObject powerupPrefab = ContentUtilities.LoadPrefab("Powerup", prefabsPath);
        GameObject[] collectiblePrefabs = new GameObject[] { jewelPrefab, powerupPrefab };

        // Go through each row
        int index = 0;
        for (int y = 0; y < mapHeight; y++)
        {
            // Go through each column
            for (int x = 0; x < mapWidth; x++)
            {
                // Resolve index and collectible type at the coordinates
                int collectibleType = mapArray[index];
                index++;
                // If not a known type
                if (collectibleType < 0 || collectibleType >= collectiblePrefabs.Length)
                {
                    // Skip
                    continue;
                }
                // Instantiate and position the collectible object
                GameObject prefab = collectiblePrefabs[collectibleType];
                GameObject newObject = LayoutObject(prefab.name, prefab, (float)x + 0.5f, (float)y + 0.5f);
                newObject.transform.SetParent(parentTransform);
            }
        }
    }

    private static void LayoutLevel()
    {
        // Build tiles array
        Tile[] tiles = new Tile[tilePaths.Length];
        for (int i = 0; i < tilePaths.Length; i++)
        {
            tiles[i] = ContentUtilities.LoadTileAtPath(tilePaths[i]);
        }

        // Get tile maps
        Tilemap groundTilemap = GameObject.Find("GroundLayer").GetComponent<Tilemap>();
        Tilemap gateTilemap = GameObject.Find("GateLayer").GetComponent<Tilemap>();
        Tilemap upperTilemap = GameObject.Find("UpperLayer").GetComponent<Tilemap>();

        // Lay out the tile map from an integer-based map array
        LayoutMap(GetGroundLayerMapArray(), groundTilemap, tiles);
        LayoutMap(GetGateLayerMapArray(), gateTilemap, tiles);
        LayoutMap(GetUpperLayerMapArray(), upperTilemap, tiles);

        // Layout the collectible 
        LayoutCollectible(GetCollectibleMapArray());
    }

    private static void LayoutMap(int[] mapArray, Tilemap tilemap, Tile[] tiles)
    {
        // Start plotting objects from bottom right corner
        const int startX = 0;
        const int startY = 0;
        Vector3Int marker = new Vector3Int(startX, startY, 0);
        const int step = 1;

        // Go through the rows
        for (int i = 0; i < mapHeight; i++)
        {
            // Go through the columns
            for (int j = 0; j < mapWidth; j++)
            {
                // Resolve current index
                int index = i * mapWidth + j;
                // Get current tile type
                int tileType = mapArray[index];
                if (tileType < 0 || tileType >= tiles.Length)
                {
                    // Increment horizontal step
                    marker.x += step;
                    continue;
                }
                // Set the tile
                tilemap.SetTile(marker, tiles[tileType]);
                // Increment horizontal step
                marker.x += step;
            }
            // Reset horizontal step
            marker.x = startX;
            // Increment vertical step
            marker.y += step; 
        }
    }

    private static GameObject LayoutObject(string name, GameObject prefab, float posX, float posY)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        newObject.name = name;
        newObject.transform.position = new Vector2(posX, posY);
        return newObject;
    }

    private static void LayoutObjects()
    {
        GameObject prefab;
        // Lay out player
        prefab = ContentUtilities.LoadPrefab("Player", prefabsPath);
        Vector2 position = GameObject.Find("PlayerSpawnPoint").transform.position;
        LayoutObject("Player", prefab, position.x, position.y);
        // Lay out enemies 
        prefab = ContentUtilities.LoadPrefab("Enemy", prefabsPath);
        position = GameObject.Find("Enemy1SpawnPoint").transform.position;
        LayoutObject("Enemy1", prefab, position.x, position.y);
        position = GameObject.Find("Enemy2SpawnPoint").transform.position;
        LayoutObject("Enemy2", prefab, position.x, position.y);
        position = GameObject.Find("Enemy3SpawnPoint").transform.position;
        LayoutObject("Enemy3", prefab, position.x, position.y);
        position = GameObject.Find("Enemy4SpawnPoint").transform.position;
        LayoutObject("Enemy4", prefab, position.x, position.y);
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Character", "Enemy", "GameManager", "PathFinder", "PathNode", "Player" }))
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

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        // Exit points for enemy AI
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Set arrays
        // Exit points
        GameObject[] exitPoints = GameObject.FindGameObjectsWithTag(scriptPrefix + "ExitPoint");
        ScriptUtilities.AssignObjectsFieldToObject(exitPoints, go, className, "enemyExitPoints");
        // Enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(scriptPrefix + "Enemy");
        ScriptUtilities.AssignObjectsFieldToObject(enemies, go, className, "enemyObjects");

        // Other game objects
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject lifeTextObject = GameObject.Find("LifeText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject enemyHouseObject = GameObject.Find("EnemyHouse");
        GameObject collectibleObject = GameObject.Find("Collectible");
        GameObject playerObject = GameObject.Find("Player");
        GameObject pathFinderObject = GameObject.Find("PathFinder");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign stuffs to Game Manager
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(lifeTextObject, "Text", go, className, "lifeText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectFieldToObject(enemyHouseObject, go, className, "enemyHouseObject");
        ScriptUtilities.AssignObjectFieldToObject(collectibleObject, go, className, "collectibleObject");
        ScriptUtilities.AssignObjectFieldToObject(playerObject, go, className, "playerObject");
        ScriptUtilities.AssignComponentFieldToObject(pathFinderObject, scriptPrefix + "PathFinder", go, scriptPrefix + "GameManager", "pathFinder");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
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
        GameObject pathFinderObject = GameObject.Find("PathFinder");
        // Access the prefabs
        GameObject gameManagerPrefab = ContentUtilities.LoadPrefab("GameManager", prefabsPath);
        GameObject playerPrefab = ContentUtilities.LoadPrefab("Player", prefabsPath);
        GameObject enemyPrefab = ContentUtilities.LoadPrefab("Enemy", prefabsPath);
        GameObject pathNodePrefab = ContentUtilities.LoadPrefab("PathNode", prefabsPath);
        GameObject jewelPrefab = ContentUtilities.LoadPrefab("Jewel", prefabsPath);
        GameObject powerupPrefab = ContentUtilities.LoadPrefab("Powerup", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PathFinder", pathFinderObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Player", playerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", enemyPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PathNode", pathNodePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Collectible", jewelPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Collectible", powerupPrefab);

        string className = scriptPrefix + "Enemy";
        Sprite eatenSprite = ContentUtilities.LoadSprite("texture_eaten", texturesPath);
        Sprite scaredSprite = ContentUtilities.LoadSprite("texture_scared", texturesPath);
        // Assign scared sprites to enemy prefabs
        ScriptUtilities.AssignSpriteFieldToObject(eatenSprite, enemyPrefab, className, "eatenSprite");
        // Assign scared sprites to enemy prefabs
        ScriptUtilities.AssignSpriteFieldToObject(scaredSprite, enemyPrefab, className, "scaredSprite");

        // Assign types to collectible prefabs
        className = scriptPrefix + "Collectible";
        ScriptUtilities.AssignIntFieldToObject(0, jewelPrefab, className, "type");
        ScriptUtilities.AssignIntFieldToObject(1, powerupPrefab, className, "type");

        // Layout game objects
        LayoutObjects();

        // Get objects
        GameObject playerObject = GameObject.Find("Player");
        GameObject enemy1Object = GameObject.Find("Enemy1");
        GameObject enemy2Object = GameObject.Find("Enemy2");
        GameObject enemy3Object = GameObject.Find("Enemy3");
        GameObject enemy4Object = GameObject.Find("Enemy4");
        GameObject playerSpawnPoint = GameObject.Find("/SpawnPoints/PlayerSpawnPoint");
        GameObject enemy1SpawnPoint = GameObject.Find("/SpawnPoints/Enemy1SpawnPoint");
        GameObject enemy2SpawnPoint = GameObject.Find("/SpawnPoints/Enemy2SpawnPoint");
        GameObject enemy3SpawnPoint = GameObject.Find("/SpawnPoints/Enemy3SpawnPoint");
        GameObject enemy4SpawnPoint = GameObject.Find("/SpawnPoints/Enemy4SpawnPoint");

        // Instantiate and set up
        SetupPathFinder();
        InstantiateAndSetupGameManager(gameManagerPrefab);

        // Get the clone prefabs
        GameObject enemyClonePrefab = ContentUtilities.LoadPrefab("EnemyClone", prefabsPath);
        GameObject playerClonePrefab = ContentUtilities.LoadPrefab("PlayerClone", prefabsPath);

        className = scriptPrefix + "Enemy";
        // Assign stats to enemy
        AssignCharacterStats(enemy1SpawnPoint, 3.0f, enemy1Object, enemyClonePrefab, className);
        AssignCharacterStats(enemy2SpawnPoint, 3.0f, enemy2Object, enemyClonePrefab, className);
        AssignCharacterStats(enemy3SpawnPoint, 3.0f, enemy3Object, enemyClonePrefab, className);
        AssignCharacterStats(enemy4SpawnPoint, 3.0f, enemy4Object, enemyClonePrefab, className);
        className = scriptPrefix + "Player";
        AssignCharacterStats(playerSpawnPoint, 4.0f, playerObject, playerClonePrefab, className);

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

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        ReverseEngineerMapArrays();
        ReverseEngineerScripts();
    }

    private static void ReverseEngineerCollectibleMapArray()
    {
        // Call this method only after the Game Template has been created

        GameObject groundLayer = GameObject.Find("GroundLayer");
        int w = groundLayer.GetComponent<Tilemap>().size.x;
        int h = groundLayer.GetComponent<Tilemap>().size.y;

        // Build parameters
        GameObject collectibleObject = GameObject.Find("Collectible");
        GameObject jewelPrefab = ContentUtilities.LoadPrefab("Jewel", prefabsPath);
        GameObject powerupPrefab = ContentUtilities.LoadPrefab("Powerup", prefabsPath);
        GameObject[] guide = new GameObject[] { jewelPrefab, powerupPrefab };
        // Convert into a map array
        int[] mapArray = ContentUtilities.ConvertContainerLayoutToMapArray(collectibleObject, guide, w, h);
        // Convert map array to string
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        // Print out to console
        Debug.Log("Collectible Map");
        Debug.Log(sb.ToString());
    }

    private static void ReverseEngineerTilemapArray(Tilemap tilemap)
    {
        // Call this method only after the Game Template has been created

        int w = mapWidth;
        int h = mapHeight;

        // Create name arrays following the order set by the enum above
        string[] tileNames = new string[] {
            "ground_tile", "enemygate_tile", "wall_tile_topleft", "wall_tile_top", "wall_tile_topright", 
            "wall_tile_left", "wall_tile", "wall_tile_right", "wall_tile_bottomleft", "wall_tile_bottom", 
            "wall_tile_bottomright" 
        };
        // Assert this array has the same element as the enum
        Assert.IsTrue(tileNames.Length == (int)TilePath.Max);

        // Build parameters
        Tile[] guide = new Tile[(int)TilePath.Max];
        for (int i = 0; i < guide.Length; i++)
        {
            guide[i] = ContentUtilities.LoadTile(tileNames[i], tilesPath);
        }
        // Convert into a map array
        int[] mapArray = ContentUtilities.ConvertTilemapToMapArray(tilemap, guide, w, h);
        // Convert map array to string
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        // Print out to console
        Debug.Log("Printing Tile Map " + tilemap.name);
        Debug.Log(sb.ToString());
    }

    private static void ReverseEngineerMapArrays()
    {
        ReverseEngineerCollectibleMapArray();
        ReverseEngineerTilemapArray(GameObject.Find("GroundLayer").GetComponent<Tilemap>());
        ReverseEngineerTilemapArray(GameObject.Find("UpperLayer").GetComponent<Tilemap>());
        ReverseEngineerTilemapArray(GameObject.Find("GateLayer").GetComponent<Tilemap>());
    }

    private static void ReverseEngineerScripts()
    {
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DCharacter", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DEnemy", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DCollectible", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DPathFinder", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DPathNode", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("JH2DPlayer", scriptsPath);
        AssetDatabase.Refresh();
    }

    private static void SetupPathFinder()
    {
        GameObject go = GameObject.Find("PathFinder");
        string className = scriptPrefix + "PathFinder";

        // Get objects
        GameObject groundLayerObject = GameObject.Find("GroundLayer");
        GameObject gateLayerObject = GameObject.Find("GateLayer");
        GameObject upperLayerObject = GameObject.Find("UpperLayer");
        GameObject pathNodePrefab = ContentUtilities.LoadPrefab("PathNode", prefabsPath);
        // Assign stuffs to Path Finder        
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PathFinder", go);
        ScriptUtilities.AssignComponentFieldToObject(groundLayerObject, "Tilemap", go, className, "groundTilemap");
        ScriptUtilities.AssignComponentFieldToObject(gateLayerObject, "Tilemap", go, className, "gateTilemap");
        ScriptUtilities.AssignComponentFieldToObject(upperLayerObject, "Tilemap", go, className, "upperTilemap");
        ScriptUtilities.AssignObjectFieldToObject(pathNodePrefab, go, className, "nodePrefab");
    }

    private static void WriteJH2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("JH2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DCharacterScriptToFile()
    {
        StringBuilder sb = new StringBuilder(11220);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public abstract class JH2DCharacter : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float speed = 3.0f;");
        sb.AppendLine("    public GameObject spawnPoint;");
        sb.AppendLine("    public GameObject clonePrefab; // Used this when rossing the warp tunnel");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float halfWidth = 1.0f;");
        sb.AppendLine("    private GameObject clone;");
        sb.AppendLine("    protected JH2DGameManager gameManager;");
        sb.AppendLine("    protected Rigidbody2D rb;");
        sb.AppendLine("    protected JH2DPathNode currentNode = null;");
        sb.AppendLine("    protected bool obstacleAhead = false;");
        sb.AppendLine("    private const float leftLimit = 0.5f;");
        sb.AppendLine("    private const float rightLimit = 27.5f;");
        sb.AppendLine("");
        sb.AppendLine("    protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = JH2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        halfWidth = CalculateHalfWidth();");
        sb.AppendLine("        AddClone();");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void AddClone()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Add a clone");
        sb.AppendLine("        clone = Instantiate(clonePrefab);");
        sb.AppendLine("        clone.name = clonePrefab.name;");
        sb.AppendLine("        clone.transform.SetParent(transform);");
        sb.AppendLine("        clone.transform.rotation = transform.rotation;");
        sb.AppendLine("        ShowClone(false, transform.position);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected abstract float CalculateHalfWidth(); // Needed to check if the character is stradling the warp tunnel edge");
        sb.AppendLine("");
        sb.AppendLine("    protected bool CheckNodeProximity(float tolerance)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Check if this character is at the node (within a tolerable threshold)");
        sb.AppendLine("        Vector2 nodePosition = currentNode.WorldPosition;");
        sb.AppendLine("        Vector2 myPosition = transform.position;");
        sb.AppendLine("        Vector2 displacement = new Vector2(nodePosition.x - myPosition.x, nodePosition.y - myPosition.y);");
        sb.AppendLine("        return displacement.sqrMagnitude < (tolerance * tolerance);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected virtual bool CheckPassability(JH2DPathNode.Passable passability)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if this node can be passed by this character's type");
        sb.AppendLine("        return passability == JH2DPathNode.Passable.Everyone;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected void EnforceObstacleConstraint()");
        sb.AppendLine("    {");
        sb.AppendLine("        SnapPosition();");
        sb.AppendLine("        bool obstacleFound = false;");
        sb.AppendLine("        Vector2 velocity = rb.velocity;");
        sb.AppendLine("        // Check for passability in the direction this character is heading");
        sb.AppendLine("        if (velocity.x > 0.0f && !CheckPassability(currentNode.rightPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            obstacleFound = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (velocity.x < 0.0f && !CheckPassability(currentNode.leftPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            obstacleFound = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (velocity.y > 0.0f && !CheckPassability(currentNode.upPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            obstacleFound = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (velocity.y < 0.0f && !CheckPassability(currentNode.downPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            obstacleFound = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Report error if somehow got into an impassable node");
        sb.AppendLine("        if (!CheckPassability(currentNode.passable))");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.LogError(\"Error! \" + gameObject.name + \" Entered non-passable node at \" + currentNode.position + \", velocity \" + rb.velocity);");
        sb.AppendLine("            obstacleFound = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If moving into an obstacle ");
        sb.AppendLine("        if (obstacleFound)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Adjust position");
        sb.AppendLine("            Vector2 displacement = currentNode.WorldPosition - (Vector2)transform.position;");
        sb.AppendLine("            float expectToTravel = speed * Time.deltaTime;");
        sb.AppendLine("            if (displacement.sqrMagnitude <= expectToTravel * expectToTravel)");
        sb.AppendLine("            {");
        sb.AppendLine("                transform.position = currentNode.WorldPosition;");
        sb.AppendLine("                rb.velocity = Vector2.zero;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        obstacleAhead = obstacleFound;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected void EnforceWarpConstraint()");
        sb.AppendLine("    {");
        sb.AppendLine("        bool isStradling = false;");
        sb.AppendLine("        int mapWidth = gameManager.pathFinder.mapWidth;");
        sb.AppendLine("        int mapHeight = gameManager.pathFinder.mapHeight;");
        sb.AppendLine("");
        sb.AppendLine("        // Clone rotation and position");
        sb.AppendLine("        clone.transform.rotation = transform.rotation;");
        sb.AppendLine("        Vector2 clonePosition = transform.position;");
        sb.AppendLine("");
        sb.AppendLine("        // Going through either warp tunnel teleports this character to the other side");
        sb.AppendLine("            // The checks assume the tunnel exits situated on the extreme left and right");
        sb.AppendLine("        // If completely through the right tunnel");
        sb.AppendLine("        if (transform.position.x - halfWidth > rightLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            transform.position = new Vector2(transform.position.x - mapWidth, transform.position.y);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if completely through the Left tunnel");
        sb.AppendLine("        else if (transform.position.x + halfWidth < leftLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            transform.position = new Vector2(transform.position.x + mapWidth, transform.position.y);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if stradling the right tunnel");
        sb.AppendLine("        else if (transform.position.x + halfWidth > rightLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("            clonePosition.x -= mapWidth;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if stradling the left tunnel");
        sb.AppendLine("        else if (transform.position.x - halfWidth < leftLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("            clonePosition.x += mapWidth;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Show / hide and position the clone");
        sb.AppendLine("        ShowClone(isStradling, clonePosition);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public abstract void Respawn();");
        sb.AppendLine("    ");
        sb.AppendLine("    private void ShowClone(bool flag, Vector3 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Show / hide and position the clone");
        sb.AppendLine("        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();");
        sb.AppendLine("        if (sr.enabled != flag)");
        sb.AppendLine("        {");
        sb.AppendLine("            sr.enabled = flag;");
        sb.AppendLine("        }");
        sb.AppendLine("        clone.transform.position = position;");
        sb.AppendLine("        // Change sprite if not the same");
        sb.AppendLine("        Sprite originalSprite = GetComponent<SpriteRenderer>().sprite;");
        sb.AppendLine("        if (sr.enabled == true && sr.sprite != originalSprite)");
        sb.AppendLine("        {");
        sb.AppendLine("            sr.sprite = originalSprite;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected void SnapPosition()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Snap the character to the position of the node it is occupying");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        Vector2 nodePosition = currentNode.WorldPosition;");
        sb.AppendLine("        Vector2 adjustedPosition = transform.position;");
        sb.AppendLine("        if (!CheckPassability(currentNode.upPassable) && adjustedPosition.y > nodePosition.y)");
        sb.AppendLine("        {");
        sb.AppendLine("            adjustedPosition.y = nodePosition.y;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (!CheckPassability(currentNode.downPassable) && adjustedPosition.y < nodePosition.y)");
        sb.AppendLine("        {");
        sb.AppendLine("            adjustedPosition.y = nodePosition.y;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (!CheckPassability(currentNode.rightPassable) && adjustedPosition.x > nodePosition.x)");
        sb.AppendLine("        {");
        sb.AppendLine("            adjustedPosition.x = nodePosition.x;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (!CheckPassability(currentNode.leftPassable) && adjustedPosition.x < nodePosition.x)");
        sb.AppendLine("        {");
        sb.AppendLine("            adjustedPosition.x = nodePosition.x;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        transform.position = adjustedPosition;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected void UpdatePathNode()");
        sb.AppendLine("    {");
        sb.AppendLine("        // As the character moves through the path nodes, this updates its current node");
        sb.AppendLine("        Vector2 position = transform.position;");
        sb.AppendLine("        JH2DPathNode newNode = gameManager.pathFinder.GetNode((int)position.x, (int)position.y);");
        sb.AppendLine("");
        sb.AppendLine("        if (newNode != currentNode)");
        sb.AppendLine("        {");
        sb.AppendLine("            currentNode = newNode;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DCharacter", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DCollectibleScriptToFile()
    {
        StringBuilder sb = new StringBuilder(736);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DCollectible : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Type type = Type.Jewel;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Type");
        sb.AppendLine("    {");
        sb.AppendLine("        Jewel = 0,");
        sb.AppendLine("        Powerup,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DCollectible", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DEnemyScriptToFile()
    {
        StringBuilder sb = new StringBuilder(30312);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DEnemy : JH2DCharacter");
        sb.AppendLine("{");
        sb.AppendLine("    public float viewRange = 8.0f;");
        sb.AppendLine("    public float initialIdleTime = 1.5f;");
        sb.AppendLine("    public float maxScaredTime = 12.5f;");
        sb.AppendLine("    public float scaredSpeed = 2.5f;");
        sb.AppendLine("    public float eyeSpeed = 10.0f;");
        sb.AppendLine("    public float minAlertedTime = 5.0f;");
        sb.AppendLine("    public float maxAlertedTime = 15.0f;");
        sb.AppendLine("    public Sprite eatenSprite;");
        sb.AppendLine("    public Sprite scaredSprite;");
        sb.AppendLine("    private Sprite normalSprite;");
        sb.AppendLine("    private SpriteRenderer spriteRenderer;");
        sb.AppendLine("    private BoxCollider2D boxCollider;");
        sb.AppendLine("    private State state;");
        sb.AppendLine("    private float aiTimer = 0.0f;");
        sb.AppendLine("    private float alertTimer = 0.0f;");
        sb.AppendLine("    private JH2DPathNode startNode;");
        sb.AppendLine("    List<JH2DPathNode> nodePath;");
        sb.AppendLine("");
        sb.AppendLine("    private enum State");
        sb.AppendLine("    {");
        sb.AppendLine("        Alerted = 0,");
        sb.AppendLine("        Idle,");
        sb.AppendLine("        LeaveRoom,");
        sb.AppendLine("        Patrol,");
        sb.AppendLine("        Chase,");
        sb.AppendLine("        Edible,");
        sb.AppendLine("        Eaten,");
        sb.AppendLine("        Wait,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    new protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.Start();");
        sb.AppendLine("        spriteRenderer = GetComponent<SpriteRenderer>();");
        sb.AppendLine("        normalSprite = spriteRenderer.sprite;");
        sb.AppendLine("        boxCollider = GetComponent<BoxCollider2D>();");
        sb.AppendLine("        EnterStateIdle();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!JH2DGameManager.gameStarted)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Update the enemy's state machine");
        sb.AppendLine("        UpdatePathNode();");
        sb.AppendLine("        switch (state)");
        sb.AppendLine("        {");
        sb.AppendLine("            case State.Alerted:");
        sb.AppendLine("                ExecuteStateAlerted();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Idle:");
        sb.AppendLine("                ExecuteStateIdle();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.LeaveRoom:");
        sb.AppendLine("                ExecuteStateLeaveRoom();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Patrol:");
        sb.AppendLine("                ExecuteStatePatrol();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Chase:");
        sb.AppendLine("                ExecuteStateChase();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Edible:");
        sb.AppendLine("                ExecuteStateEdible();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Eaten:");
        sb.AppendLine("                ExecuteStateEaten();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case State.Wait:");
        sb.AppendLine("                ExecuteStateWait();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Update facing");
        sb.AppendLine("        UpdateFacing();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!JH2DGameManager.gameStarted)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        EnforceWarpConstraint();");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        EnforceObstacleConstraint();        ");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AnimateBlinking()");
        sb.AppendLine("    {");
        sb.AppendLine("        const float interval = 0.5f;");
        sb.AppendLine("        const int numSteps = 9;");
        sb.AppendLine("");
        sb.AppendLine("        // Do not animate unless the Edible state is about to end");
        sb.AppendLine("        if (aiTimer > numSteps * interval)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Set proper sprite according to remaining time");
        sb.AppendLine("        for (int i = 0; i < numSteps; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            float min = i * interval;");
        sb.AppendLine("            float max = (i + 1) * interval;");
        sb.AppendLine("            if (aiTimer >= min && aiTimer < max)");
        sb.AppendLine("            {");
        sb.AppendLine("                Sprite sprite = i % 2 == 0 ? normalSprite : scaredSprite;");
        sb.AppendLine("                spriteRenderer.sprite = sprite;");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AvoidPlayer()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (startNode == currentNode)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        const float tolerance = 0.1f;");
        sb.AppendLine("        bool proximityCheck = CheckNodeProximity(tolerance);");
        sb.AppendLine("        // If feeling threatened");
        sb.AppendLine("        if (CheckPlayerProximity())");
        sb.AppendLine("        {");
        sb.AppendLine("            // If this enemy is on a node");
        sb.AppendLine("            if (proximityCheck)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Evaluate a direction to escape");
        sb.AppendLine("                Vector2 playerPosition = gameManager.playerObject.transform.position;");
        sb.AppendLine("                Vector2 myPosition = transform.position;");
        sb.AppendLine("                Vector2 distance = myPosition - playerPosition;");
        sb.AppendLine("                startNode = currentNode;");
        sb.AppendLine("                Vector2 direction = FindEscapePath(distance);");
        sb.AppendLine("                rb.velocity = direction * scaredSpeed;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Move about randomly");
        sb.AppendLine("            int numExits = CountAvailableExits();");
        sb.AppendLine("            if ((obstacleAhead && proximityCheck) || (numExits >= 3 && proximityCheck))");
        sb.AppendLine("            {");
        sb.AppendLine("                startNode = currentNode;");
        sb.AppendLine("                transform.position = currentNode.WorldPosition;");
        sb.AppendLine("                rb.velocity = RandomDirection() * scaredSpeed;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void BecomeEdible()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (state != State.Eaten)");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStateEdible();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected override float CalculateHalfWidth()");
        sb.AppendLine("    {");
        sb.AppendLine("        return 0.5f * GetComponent<Collider2D>().bounds.size.x;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected bool CheckPlayerProximity()");
        sb.AppendLine("    {");
        sb.AppendLine("        float threshold = viewRange;");
        sb.AppendLine("        Vector2 displacement = gameManager.playerObject.transform.position - transform.position;");
        sb.AppendLine("        return displacement.sqrMagnitude < (threshold * threshold);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected override bool CheckPassability(JH2DPathNode.Passable passability)");
        sb.AppendLine("    {");
        sb.AppendLine("        return passability == JH2DPathNode.Passable.Everyone || passability == JH2DPathNode.Passable.Enemy;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private int CountAvailableExits()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Count number of passable exits from the current occupied node");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return 0;");
        sb.AppendLine("        }");
        sb.AppendLine("        int numDirections = 0;");
        sb.AppendLine("        if (base.CheckPassability(currentNode.upPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.downPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.rightPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.leftPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        return numDirections;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateAlerted()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Alerted;");
        sb.AppendLine("        aiTimer = 0.0f;");
        sb.AppendLine("        UpdateChasePlayerPath();");
        sb.AppendLine("        // Randomly set a duration for this alerted state");
        sb.AppendLine("        alertTimer = Random.Range(minAlertedTime, maxAlertedTime);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateChase()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Chase the player");
        sb.AppendLine("        state = State.Chase;");
        sb.AppendLine("        aiTimer = 0.0f;");
        sb.AppendLine("        UpdateChasePlayerPath();");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void EnterStateEaten()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Eaten;");
        sb.AppendLine("        // Hide the body and disable collider");
        sb.AppendLine("        spriteRenderer.sprite = eatenSprite;");
        sb.AppendLine("        // Find a path back to the house");
        sb.AppendLine("        Vector2 destination = gameManager.enemyHouseObject.transform.position;");
        sb.AppendLine("        Vector2 position = transform.position;");
        sb.AppendLine("        JH2DPathFinder pathFinder = gameManager.pathFinder;");
        sb.AppendLine("        nodePath = pathFinder.FindPath((int)position.x, (int)position.y, (int)destination.x, (int)destination.y, true);");
        sb.AppendLine("        nodePath.RemoveAt(0);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateEdible()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Edible;");
        sb.AppendLine("        // Set up the edible state");
        sb.AppendLine("        aiTimer = maxScaredTime;");
        sb.AppendLine("        spriteRenderer.sprite = scaredSprite;");
        sb.AppendLine("        startNode = currentNode;");
        sb.AppendLine("        // Initially scatter in a random direction");
        sb.AppendLine("        Vector2 randomDirection = RandomDirection();");
        sb.AppendLine("        rb.velocity = randomDirection * speed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateIdle()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Idle;");
        sb.AppendLine("        // Set up normal look");
        sb.AppendLine("        aiTimer = initialIdleTime;");
        sb.AppendLine("        spriteRenderer.sprite = normalSprite;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateLeaveRoom()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.LeaveRoom;");
        sb.AppendLine("        // Pick a random exit point near the house");
        sb.AppendLine("        int randomInteger = Random.Range(0, gameManager.enemyExitPoints.Length);");
        sb.AppendLine("        Vector2 destination = gameManager.enemyExitPoints[randomInteger].transform.position;");
        sb.AppendLine("        Vector2 position = transform.position;");
        sb.AppendLine("        // Find a path to the chosen exit");
        sb.AppendLine("        JH2DPathFinder pathFinder = gameManager.pathFinder;");
        sb.AppendLine("        nodePath = pathFinder.FindPath((int)position.x, (int)position.y, (int)destination.x, (int)destination.y, true);");
        sb.AppendLine("        nodePath.RemoveAt(0);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStatePatrol()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Patrol;");
        sb.AppendLine("        // Patrol around");
        sb.AppendLine("        Vector2 randomDirection = RandomDirection();");
        sb.AppendLine("        rb.velocity = randomDirection * speed;");
        sb.AppendLine("        startNode = currentNode;");
        sb.AppendLine("        // Randomly set the time until it becomes alerted and magically sense Player's location");
        sb.AppendLine("        aiTimer = Random.Range(minAlertedTime, maxAlertedTime);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateWait()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Wait;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateAlerted()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Revert to Patrol state after a short while");
        sb.AppendLine("        if (alertTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            alertTimer -= Time.deltaTime;");
        sb.AppendLine("            if (alertTimer <= 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                EnterStatePatrol();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // In this state, it instinctively knows the player's location");
        sb.AppendLine("        UpdateChasePlayerPath();");
        sb.AppendLine("        if (FollowPath(nodePath))");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStatePatrol();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateChase()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!CheckPlayerProximity())");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStatePatrol();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateChasePlayerPath();");
        sb.AppendLine("        if (FollowPath(nodePath))");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStatePatrol();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateEaten()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (FollowPath(nodePath))");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStateIdle();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateEdible()");
        sb.AppendLine("    {");
        sb.AppendLine("        aiTimer -= Time.deltaTime;");
        sb.AppendLine("        ");
        sb.AppendLine("        if (aiTimer <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Time's up, back to normal");
        sb.AppendLine("            spriteRenderer.sprite = normalSprite;");
        sb.AppendLine("            EnterStatePatrol();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        AnimateBlinking();");
        sb.AppendLine("        AvoidPlayer();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateIdle()");
        sb.AppendLine("    {");
        sb.AppendLine("        aiTimer -= Time.deltaTime;");
        sb.AppendLine("        if (aiTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        EnterStateLeaveRoom();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateLeaveRoom()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (CheckPlayerProximity() && !IsInEnemyHouse())");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStateChase();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (FollowPath(nodePath))");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStatePatrol();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStatePatrol()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (CheckPlayerProximity())");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterStateChase();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (startNode == currentNode)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (aiTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            aiTimer -= Time.deltaTime;");
        sb.AppendLine("            if (aiTimer <= 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                EnterStateAlerted();");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Patrol around randomly");
        sb.AppendLine("        const float tolerance = 0.1f;");
        sb.AppendLine("        bool proximityCheck = CheckNodeProximity(tolerance);");
        sb.AppendLine("        int numExits = CountAvailableExits();");
        sb.AppendLine("        if ((obstacleAhead && proximityCheck) || (numExits >= 3 && proximityCheck))");
        sb.AppendLine("        {");
        sb.AppendLine("            startNode = currentNode;");
        sb.AppendLine("            transform.position = currentNode.WorldPosition;");
        sb.AppendLine("            rb.velocity = RandomDirection() * speed;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateWait()");
        sb.AppendLine("    {");
        sb.AppendLine("        // wait till further notice");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private Vector2 FindEscapePath(Vector2 distance)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector2[] directions = new Vector2[2];");
        sb.AppendLine("        int numDirections = 0;");
        sb.AppendLine("");
        sb.AppendLine("        // Find a number of potential escape directions");
        sb.AppendLine("        if (Mathf.Abs(distance.x) > Mathf.Abs(distance.y))");
        sb.AppendLine("        {");
        sb.AppendLine("            if (distance.x > 0.0f && base.CheckPassability(currentNode.rightPassable))");
        sb.AppendLine("            {");
        sb.AppendLine("                return Vector2.right;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (distance.x < 0.0f && base.CheckPassability(currentNode.leftPassable))");
        sb.AppendLine("            {");
        sb.AppendLine("                return Vector2.left;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                if (base.CheckPassability(currentNode.upPassable))");
        sb.AppendLine("                {");
        sb.AppendLine("                    directions[numDirections] = Vector2.up;");
        sb.AppendLine("                    numDirections++;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (base.CheckPassability(currentNode.downPassable))");
        sb.AppendLine("                {");
        sb.AppendLine("                    directions[numDirections] = Vector2.down;");
        sb.AppendLine("                    numDirections++;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            if (distance.y > 0.0f && base.CheckPassability(currentNode.upPassable))");
        sb.AppendLine("            {");
        sb.AppendLine("                return Vector2.up;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (distance.y < 0.0f && base.CheckPassability(currentNode.downPassable))");
        sb.AppendLine("            {");
        sb.AppendLine("                return Vector2.down;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("               if (base.CheckPassability(currentNode.rightPassable))");
        sb.AppendLine("                {");
        sb.AppendLine("                    directions[numDirections] = Vector2.right;");
        sb.AppendLine("                    numDirections++;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (base.CheckPassability(currentNode.leftPassable))");
        sb.AppendLine("                {");
        sb.AppendLine("                    directions[numDirections] = Vector2.left;");
        sb.AppendLine("                    numDirections++;");
        sb.AppendLine("                }");
        sb.AppendLine("             }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // If found");
        sb.AppendLine("        if (numDirections > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Randomly pick a direction to escape");
        sb.AppendLine("            return directions[Random.Range(0, numDirections)];");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Nowhere to run, so just continue moving in the same direction");
        sb.AppendLine("        return rb.velocity.normalized;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool FollowPath(List<JH2DPathNode> nodePath)");
        sb.AppendLine("    {   ");
        sb.AppendLine("        if (nodePath == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (nodePath.Count == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Follow the nodes path to a destination");
        sb.AppendLine("        JH2DPathNode nextDestination = nodePath[0];");
        sb.AppendLine("        Vector2 currentPos = transform.position;");
        sb.AppendLine("        Vector2 nextPos = nextDestination.WorldPosition;");
        sb.AppendLine("        Vector2 direction = nextPos - currentPos;");
        sb.AppendLine("        float distance = direction.magnitude;");
        sb.AppendLine("        if (distance <= speed * Time.deltaTime)");
        sb.AppendLine("        {");
        sb.AppendLine("            currentNode = nextDestination;");
        sb.AppendLine("            transform.position = nextDestination.WorldPosition;");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            nodePath.RemoveAt(0);");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        rb.velocity = direction.normalized * speed;");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ForceStop()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("        EnterStateWait();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsInEnemyHouse()");
        sb.AppendLine("    {");
        sb.AppendLine("        BoxCollider2D enemyHouseCollider = gameManager.enemyHouseObject.GetComponent<BoxCollider2D>();");
        sb.AppendLine("        return enemyHouseCollider.IsTouching(GetComponent<BoxCollider2D>());");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsVulnerable()");
        sb.AppendLine("    {");
        sb.AppendLine("        return state == State.Edible || state == State.Eaten;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerEnter2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If this object is a child");
        sb.AppendLine("        if (collidedObject.transform.parent != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Use the parent instead");
        sb.AppendLine("            collidedObject = collidedObject.transform.parent.gameObject;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Handle collision");
        sb.AppendLine("        if (collidedObject.CompareTag(\"JH2DPlayer\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            if (state == State.Edible)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Eaten!");
        sb.AppendLine("                EnterStateEaten();");
        sb.AppendLine("                gameManager.NotifyEnemyEaten();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private Vector2 RandomDirection()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Vector2.zero;");
        sb.AppendLine("        }");
        sb.AppendLine("        Vector2[] directions = new Vector2[4];");
        sb.AppendLine("        int numDirections = 0;");
        sb.AppendLine("        if (base.CheckPassability(currentNode.upPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            directions[numDirections] = Vector2.up;");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.downPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            directions[numDirections] = Vector2.down;");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.rightPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            directions[numDirections] = Vector2.right;");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (base.CheckPassability(currentNode.leftPassable))");
        sb.AppendLine("        {");
        sb.AppendLine("            directions[numDirections] = Vector2.left;");
        sb.AppendLine("            numDirections++;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (numDirections == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Vector2.zero;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        int randomInteger = Random.Range(0, numDirections);");
        sb.AppendLine("        return directions[randomInteger];");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public override void Respawn()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (spawnPoint)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            transform.position = spawnPoint.transform.position;");
        sb.AppendLine("            UpdatePathNode();");
        sb.AppendLine("            EnterStateIdle();");
        sb.AppendLine("        }");
        sb.AppendLine("    } ");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateChasePlayerPath()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Every 1 second, reassess the player's location");
        sb.AppendLine("        if (aiTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            aiTimer -= Time.deltaTime;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        aiTimer = 1.0f;");
        sb.AppendLine("        Vector2 playerPos = gameManager.playerObject.transform.position;");
        sb.AppendLine("        Vector2 myPos = transform.position;");
        sb.AppendLine("        JH2DPathFinder pathFinder = gameManager.pathFinder;");
        sb.AppendLine("        nodePath = pathFinder.FindPath((int)myPos.x, (int)myPos.y, (int)playerPos.x, (int)playerPos.y, true);");
        sb.AppendLine("        nodePath.RemoveAt(0);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateFacing()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get the current movement direction");
        sb.AppendLine("        Vector3 direction = rb.velocity.normalized;");
        sb.AppendLine("        // If different from the current facing");
        sb.AppendLine("        if (direction != transform.up)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Change facing");
        sb.AppendLine("            transform.up = direction;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DEnemy", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(12301);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int totalLife = 3;");
        sb.AppendLine("    public int jewelValue = 10;");
        sb.AppendLine("    public int[] enemyEatenScores = { 200, 400, 800, 1600 };");
        sb.AppendLine("    public GameObject[] enemyExitPoints = new GameObject[6];");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text lifeText;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject enemyHouseObject;");
        sb.AppendLine("    public GameObject collectibleObject;");
        sb.AppendLine("    public GameObject playerObject;");
        sb.AppendLine("    public GameObject[] enemyObjects;");
        sb.AppendLine("    public JH2DPathFinder pathFinder;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    private int numEnemiesEaten = 0;");
        sb.AppendLine("    private int totalCollectible;");
        sb.AppendLine("    private int numCollectibleEaten = 0;");
        sb.AppendLine("");
        sb.AppendLine("    public static bool gameStarted = false;");
        sb.AppendLine("    public static JH2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Ensure only one instance exist");
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
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("        UpdateLife();");
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Start the game");
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            playButton.gameObject.SetActive(false);");
        sb.AppendLine("        }");
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
        sb.AppendLine("");
        sb.AppendLine("    public void ActivatePowerup()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Activate power up");
        sb.AppendLine("        numEnemiesEaten = 0;");
        sb.AppendLine("        UpdateVictoryCondition();");
        sb.AppendLine("        foreach (GameObject enemyObject in enemyObjects)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!enemyObject.GetComponent<JH2DEnemy>().IsInEnemyHouse())");
        sb.AppendLine("            {");
        sb.AppendLine("                enemyObject.GetComponent<JH2DEnemy>().BecomeEdible();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddScore()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Add score and update UI");
        sb.AppendLine("        currentScore += jewelValue;");
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("        UpdateVictoryCondition();");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Too bad...\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameWon()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Victory!\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyEnemyEaten()");
        sb.AppendLine("    {");
        sb.AppendLine("        numEnemiesEaten++;");
        sb.AppendLine("        currentScore += enemyEatenScores[numEnemiesEaten];");
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyPlayerDestruction()");
        sb.AppendLine("    {");
        sb.AppendLine("        totalLife -= 1;");
        sb.AppendLine("        UpdateLife();");
        sb.AppendLine("        StopEveryone();");
        sb.AppendLine("        if (totalLife > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Respawn");
        sb.AppendLine("            StartCoroutine(WaitToRespawn(0.5f));");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // No more life, game over");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        StopEveryone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Respawn()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Still has lives left, repawn and continue the level");
        sb.AppendLine("        playerObject.GetComponent<JH2DPlayer>().Respawn();");
        sb.AppendLine("        foreach (GameObject enemyObject in enemyObjects)");
        sb.AppendLine("        {");
        sb.AppendLine("            enemyObject.GetComponent<JH2DEnemy>().Respawn();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set up objects");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        totalCollectible = collectibleObject.transform.childCount;");
        sb.AppendLine("        numCollectibleEaten = 0;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void StopEveryone()");
        sb.AppendLine("    {");
        sb.AppendLine("        playerObject.GetComponent<JH2DPlayer>().ForceStop();");
        sb.AppendLine("        foreach (GameObject enemyObject in enemyObjects)");
        sb.AppendLine("        {");
        sb.AppendLine("            enemyObject.GetComponent<JH2DEnemy>().ForceStop();");
        sb.AppendLine("");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"JewelHunter2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateLife()");
        sb.AppendLine("    {");
        sb.AppendLine("        lifeText.text = \"Life: \" + totalLife.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateScore()");
        sb.AppendLine("    {");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateVictoryCondition()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Game is won when all collectible has been eaten");
        sb.AppendLine("        numCollectibleEaten++;");
        sb.AppendLine("        if (numCollectibleEaten >= totalCollectible)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameWon();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToRespawn(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait a bit before respawning");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        Respawn();");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DPathFinderScriptToFile()
    {
        StringBuilder sb = new StringBuilder(13515);

        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DPathFinder : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Tilemap groundTilemap;");
        sb.AppendLine("    public Tilemap gateTilemap;");
        sb.AppendLine("    public Tilemap upperTilemap;");
        sb.AppendLine("    public GameObject nodePrefab;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public int mapWidth;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public int mapHeight;");
        sb.AppendLine("    private JH2DPathNode[] pathNodes;");
        sb.AppendLine("    private List<JH2DPathNode> openList;");
        sb.AppendLine("    private List<JH2DPathNode> closedList;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Create path nodes based on width, height, and tile layers");
        sb.AppendLine("        mapWidth = groundTilemap.size.x;");
        sb.AppendLine("        mapHeight = groundTilemap.size.y;");
        sb.AppendLine("        GenerateNodes();");
        sb.AppendLine("        GenerateConnections();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private List<JH2DPathNode>CalculatePath(JH2DPathNode endNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Path found, process and return");
        sb.AppendLine("        List<JH2DPathNode> path = new List<JH2DPathNode>();");
        sb.AppendLine("        path.Add(endNode);");
        sb.AppendLine("        JH2DPathNode currentNode = endNode;");
        sb.AppendLine("        while (currentNode.cameFromNode != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            path.Add(currentNode.cameFromNode);");
        sb.AppendLine("            currentNode = currentNode.cameFromNode;");
        sb.AppendLine("        }");
        sb.AppendLine("        path.Reverse();");
        sb.AppendLine("        return path;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private int CalculateTaxiCabDistanceCost(JH2DPathNode node1, JH2DPathNode node2)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Return an extimated distance (Taxi Cab aka Manhattan distance)");
        sb.AppendLine("        return Math.Abs(node2.position.x - node1.position.x) + Math.Abs(node2.position.y - node1.position.y);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool CheckPassable(JH2DPathNode pathNode, bool isEnemy)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (isEnemy)");
        sb.AppendLine("        {");
        sb.AppendLine("            return pathNode.passable == JH2DPathNode.Passable.Everyone || pathNode.passable == JH2DPathNode.Passable.Enemy;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            return pathNode.passable == JH2DPathNode.Passable.Everyone;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public List<JH2DPathNode>FindPath(int startX, int startY, int endX, int endY, bool isEnemy)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate a path to the destination node, using A* Algorithm");
        sb.AppendLine("        JH2DPathNode startNode = GetNode(startX, startY);");
        sb.AppendLine("        JH2DPathNode endNode = GetNode(endX, endY);");
        sb.AppendLine("        openList = new List<JH2DPathNode>() { startNode };");
        sb.AppendLine("        closedList = new List<JH2DPathNode>();");
        sb.AppendLine("");
        sb.AppendLine("        for (int i = 0; i < mapHeight; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int j = 0; j < mapWidth; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                JH2DPathNode node = GetNode(j, i);");
        sb.AppendLine("                node.gCost = int.MaxValue;");
        sb.AppendLine("                node.CalculateFCost();");
        sb.AppendLine("                node.cameFromNode = null;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        startNode.gCost = 0;");
        sb.AppendLine("        startNode.hCost = CalculateTaxiCabDistanceCost(startNode, endNode);");
        sb.AppendLine("        startNode.CalculateFCost();");
        sb.AppendLine("");
        sb.AppendLine("        while (openList.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            JH2DPathNode currentNode = GetLowestFCostNode(openList);");
        sb.AppendLine("            if (currentNode == endNode)");
        sb.AppendLine("            {");
        sb.AppendLine("                return CalculatePath(endNode);");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            openList.Remove(currentNode);");
        sb.AppendLine("            closedList.Add(currentNode);");
        sb.AppendLine("");
        sb.AppendLine("            foreach (JH2DPathNode neighborNode in currentNode.neighbors)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (neighborNode == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (closedList.Contains(neighborNode))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                int tentativeGCost = currentNode.gCost + CalculateTaxiCabDistanceCost(currentNode, neighborNode);");
        sb.AppendLine("                if (tentativeGCost < neighborNode.gCost)");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (!CheckPassable(neighborNode, isEnemy))");
        sb.AppendLine("                    {");
        sb.AppendLine("                        closedList.Add(neighborNode);");
        sb.AppendLine("                        continue;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    neighborNode.cameFromNode = currentNode;");
        sb.AppendLine("                    neighborNode.gCost = tentativeGCost;");
        sb.AppendLine("                    neighborNode.hCost = CalculateTaxiCabDistanceCost(neighborNode, endNode);");
        sb.AppendLine("                    neighborNode.CalculateFCost();");
        sb.AppendLine("                    if (!openList.Contains(neighborNode))");
        sb.AppendLine("                    {");
        sb.AppendLine("                        openList.Add(neighborNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private JH2DPathNode GetLowestFCostNode(List<JH2DPathNode> pathNodeList)");
        sb.AppendLine("    {");
        sb.AppendLine("        JH2DPathNode lowestFCostNode = pathNodeList[0];");
        sb.AppendLine("        for (int i = 1; i < pathNodeList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (pathNodeList[i].fCost < lowestFCostNode.fCost)");
        sb.AppendLine("            {");
        sb.AppendLine("                lowestFCostNode = pathNodeList[i];");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return lowestFCostNode;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void GenerateConnections()");
        sb.AppendLine("    {");
        sb.AppendLine("        // For each and every node, connect it with its neighbors");
        sb.AppendLine("        for (int i = 0; i < mapHeight; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int j = 0; j < mapWidth; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                JH2DPathNode currentNode = pathNodes[i * mapWidth + j];");
        sb.AppendLine("                if (j > 0)");
        sb.AppendLine("                {");
        sb.AppendLine("                    JH2DPathNode leftNode = pathNodes[i * mapWidth + j - 1];");
        sb.AppendLine("                    currentNode.leftPassable = leftNode.passable;");
        sb.AppendLine("                    if (leftNode.passable != JH2DPathNode.Passable.None)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        currentNode.AddNeighbor(leftNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("                if (j < mapWidth - 1)");
        sb.AppendLine("                {");
        sb.AppendLine("                    JH2DPathNode rightNode = pathNodes[i * mapWidth + j + 1];");
        sb.AppendLine("                    currentNode.rightPassable = rightNode.passable;");
        sb.AppendLine("                    if (rightNode.passable != JH2DPathNode.Passable.None)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        currentNode.AddNeighbor(rightNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("                if (i > 0)");
        sb.AppendLine("                {");
        sb.AppendLine("                    JH2DPathNode upNode = pathNodes[(i - 1) * mapWidth + j];");
        sb.AppendLine("                    currentNode.downPassable = upNode.passable;");
        sb.AppendLine("                    if (upNode.passable != JH2DPathNode.Passable.None)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        currentNode.AddNeighbor(upNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("                if (i < mapHeight - 1)");
        sb.AppendLine("                {");
        sb.AppendLine("                    JH2DPathNode downNode = pathNodes[(i + 1) * mapWidth + j];");
        sb.AppendLine("                    currentNode.upPassable = downNode.passable;");
        sb.AppendLine("                    if (downNode.passable != JH2DPathNode.Passable.None)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        currentNode.AddNeighbor(downNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void GenerateNodes()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Generate the path-finding nodes");
        sb.AppendLine("        pathNodes = new JH2DPathNode[mapWidth * mapHeight];");
        sb.AppendLine("        for (int i = 0; i < mapHeight; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            for (int j = 0; j < mapWidth; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                Vector3Int position = new Vector3Int(j, i, 0);");
        sb.AppendLine("                Tile groundTile = groundTilemap.GetTile(position) as Tile;");
        sb.AppendLine("                Tile upperTile = upperTilemap.GetTile(position) as Tile;");
        sb.AppendLine("                Tile gateTile = gateTilemap.GetTile(position) as Tile;");
        sb.AppendLine("                Tile tile;");
        sb.AppendLine("                if (upperTile)");
        sb.AppendLine("                {");
        sb.AppendLine("                    tile = upperTile;");
        sb.AppendLine("                }");
        sb.AppendLine("                else if (gateTile)");
        sb.AppendLine("                {");
        sb.AppendLine("                    tile = gateTile;");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    tile = groundTile;");
        sb.AppendLine("                }                    ");
        sb.AppendLine("                if (tile == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    Debug.LogError(\"Error! Node \" + position + \" does not contain a tile.\");");
        sb.AppendLine("                }");
        sb.AppendLine("                JH2DPathNode newNode = Instantiate(nodePrefab).GetComponent<JH2DPathNode>();");
        sb.AppendLine("                newNode.transform.SetParent(transform);");
        sb.AppendLine("                newNode.Initialize(new Vector2Int(j, i), tile, upperTile != null, gateTile != null);");
        sb.AppendLine("                pathNodes[i * mapWidth + j] = newNode;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public JH2DPathNode GetNode(int x, int y)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Clamp values to within map width and height");
        sb.AppendLine("        x = Math.Min(x, mapWidth - 1);");
        sb.AppendLine("        x = Math.Max(x, 0);");
        sb.AppendLine("        y = Math.Min(y, mapHeight - 1);");
        sb.AppendLine("        y = Math.Max(y, 0);");
        sb.AppendLine("        // Calculate index");
        sb.AppendLine("        int index = y * mapWidth + x;");
        sb.AppendLine("        // Check range");
        sb.AppendLine("        if (index >= pathNodes.Length || index < 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return node");
        sb.AppendLine("        return pathNodes[index];");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DPathFinder", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DPathNodeScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3957);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DPathNode : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public JH2DPathNode[] neighbors; ");
        sb.AppendLine("    public Vector2Int position;");
        sb.AppendLine("    public Passable passable = Passable.Everyone;");
        sb.AppendLine("    public Passable upPassable = Passable.Everyone;");
        sb.AppendLine("    public Passable downPassable = Passable.Everyone;");
        sb.AppendLine("    public Passable leftPassable = Passable.Everyone;");
        sb.AppendLine("    public Passable rightPassable = Passable.Everyone;");
        sb.AppendLine("    private Vector2 worldPosition;");
        sb.AppendLine("    private int numNeighbors;");
        sb.AppendLine("");
        sb.AppendLine("    public int gCost;");
        sb.AppendLine("    public int hCost;");
        sb.AppendLine("    public int fCost;");
        sb.AppendLine("    public JH2DPathNode cameFromNode;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Passable");
        sb.AppendLine("    {");
        sb.AppendLine("        None = 0,");
        sb.AppendLine("        Everyone,");
        sb.AppendLine("        Enemy,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(Vector2Int position, Tile tile, bool isWall, bool isGate)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Init one of the nodes to represent the level layout");
        sb.AppendLine("        numNeighbors = 0;");
        sb.AppendLine("        neighbors = new JH2DPathNode[4];");
        sb.AppendLine("        this.position = position;");
        sb.AppendLine("        worldPosition.x = (float)position.x + 0.5f;");
        sb.AppendLine("        worldPosition.y = (float)position.y + 0.5f;");
        sb.AppendLine("        // Set object name and position");
        sb.AppendLine("        gameObject.name = tile.name + \" \" + position.ToString();");
        sb.AppendLine("        transform.position = worldPosition;");
        sb.AppendLine("        // Set passability");
        sb.AppendLine("        if (tile == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            passable = Passable.Everyone;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            if (isWall)");
        sb.AppendLine("            {");
        sb.AppendLine("                passable = Passable.None;");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (isGate)");
        sb.AppendLine("            {");
        sb.AppendLine("                passable = Passable.Enemy;");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                passable = Passable.Everyone;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public Vector2 WorldPosition { get => worldPosition; set => worldPosition = value; }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddNeighbor(JH2DPathNode neighbor)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (numNeighbors >= neighbors.Length)");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.LogWarning(\"Can't add neighbor.\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        neighbors[numNeighbors] = neighbor;");
        sb.AppendLine("        numNeighbors++;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void CalculateFCost()");
        sb.AppendLine("    {");
        sb.AppendLine("        fCost = gCost + hCost;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DPathNode", scriptsPath, sb.ToString());
    }

    private static void WriteJH2DPlayerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(7947);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class JH2DPlayer : JH2DCharacter");
        sb.AppendLine("{");
        sb.AppendLine("    private State state;");
        sb.AppendLine("    private float hInput = 0.0f;");
        sb.AppendLine("    private float vInput = 0.0f;");
        sb.AppendLine("");
        sb.AppendLine("    private enum State");
        sb.AppendLine("    {");
        sb.AppendLine("        Normal = 0,");
        sb.AppendLine("        Caught,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    new protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.Start();");
        sb.AppendLine("        EnterStateNormal();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!JH2DGameManager.gameStarted)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdatePathNode();");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        switch (state)");
        sb.AppendLine("        {");
        sb.AppendLine("            case State.Normal:");
        sb.AppendLine("                ExecuteStateNormal();");
        sb.AppendLine("                break;");
        sb.AppendLine("            case State.Caught:");
        sb.AppendLine("                ExecuteStateCaught();");
        sb.AppendLine("                break;");
        sb.AppendLine("            default:");
        sb.AppendLine("                break;");
        sb.AppendLine("        } ");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdatePathNode();");
        sb.AppendLine("        if (!JH2DGameManager.gameStarted)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        EnforceWarpConstraint();");
        sb.AppendLine("        if (currentNode == null)");
        sb.AppendLine("        {            ");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        EnforceObstacleConstraint();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerEnter2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        ");
        sb.AppendLine("        // Handle collision");
        sb.AppendLine("        if (collidedObject.CompareTag(\"JH2DEnemy\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // If this object is a child (mirror clone)");
        sb.AppendLine("            if (collidedObject.transform.parent != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Use the parent instead");
        sb.AppendLine("                collidedObject = collidedObject.transform.parent.gameObject;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (!collidedObject.GetComponent<JH2DEnemy>().IsVulnerable() && state != State.Caught)");
        sb.AppendLine("            {");
        sb.AppendLine("                EnterStateCaught();");
        sb.AppendLine("                gameManager.NotifyPlayerDestruction();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if collided object is collectible");
        sb.AppendLine("        else if (collidedObject.CompareTag(\"JH2DCollectible\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            JH2DCollectible collectible = collidedObject.GetComponent<JH2DCollectible>();");
        sb.AppendLine("            // Handle jewel eaten ");
        sb.AppendLine("            if (collectible.type == JH2DCollectible.Type.Jewel)");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.AddScore();");
        sb.AppendLine("            }");
        sb.AppendLine("            // Handle powerup eaten");
        sb.AppendLine("            else if (collectible.type == JH2DCollectible.Type.Powerup)");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.ActivatePowerup();");
        sb.AppendLine("            }");
        sb.AppendLine("            Destroy(collidedObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected override float CalculateHalfWidth()");
        sb.AppendLine("    {");
        sb.AppendLine("        return 0.5f * GetComponent<Collider2D>().bounds.size.x;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector2 inputVector = input.Get<Vector2>();");
        sb.AppendLine("        hInput = inputVector.x;");
        sb.AppendLine("        vInput = inputVector.y;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateCaught()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Caught;");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateNormal()");
        sb.AppendLine("    {");
        sb.AppendLine("        state = State.Normal;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateCaught()");
        sb.AppendLine("    {");
        sb.AppendLine("");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateNormal()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (CheckPassability(currentNode.upPassable) && vInput > 0.0f && !(rb.velocity.y > 0.0f))");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.up * speed;");
        sb.AppendLine("            transform.position = currentNode.WorldPosition;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (CheckPassability(currentNode.downPassable) && vInput < 0.0f && !(rb.velocity.y < 0.0f))");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.down * speed;");
        sb.AppendLine("            transform.position = currentNode.WorldPosition;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (CheckPassability(currentNode.rightPassable) && hInput > 0.0f && !(rb.velocity.x > 0.0f))");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.right * speed;");
        sb.AppendLine("            transform.position = currentNode.WorldPosition;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (CheckPassability(currentNode.leftPassable) && hInput < 0.0f && !(rb.velocity.x < 0.0f))");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.left * speed;");
        sb.AppendLine("            transform.position = currentNode.WorldPosition;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ForceStop()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("        EnterStateCaught();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public override void Respawn()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (spawnPoint)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            transform.position = spawnPoint.transform.position;");
        sb.AppendLine("            EnterStateNormal();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("JH2DPlayer", scriptsPath, sb.ToString());
    }
}
