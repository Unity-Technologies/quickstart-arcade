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

public class BattleTank2D : Editor
{
    private const string templateName = "BattleTank2D";
    private const string templateSpacedName = "Battle Tank 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "BT2D";
    private const int textureSize = 64;
    private const int mapWidth = 26;
    private const int mapHeight = 26;
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
        Tank=0,
        Bullet,
        Boundary,
        Ground,
        Brick,
        Forest,
        Steel,
        Water,
        Headquarters,
        PowerupBomb,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];

    private enum TilePath
    {
        Ground = 0,
        Brick,
        Steel,
        Forest,
        Water,
        Max
    };
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
        // Tags: enemy, player (already exist) for factions
        ScriptUtilities.RemoveTag(scriptPrefix + "Player");
        ScriptUtilities.RemoveTag(scriptPrefix + "PlayerBullet");
        ScriptUtilities.RemoveTag(scriptPrefix + "Enemy");
        ScriptUtilities.RemoveTag(scriptPrefix + "EnemyBullet");
        ScriptUtilities.RemoveTag(scriptPrefix + "EnemySpawnPoint");
        ScriptUtilities.RemoveTag(scriptPrefix + "Powerup");
        ScriptUtilities.RemoveTag(scriptPrefix + "PowerupSpawnPoint");
        // Sorting Layers: Ground, Forest
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Ground");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Water");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Objects");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Forest");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Powerup");
        // Layers: to sort collision detections
        int playerTankLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "PlayerTank");
        int enemyTankLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "EnemyTank");
        int playerBulletLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "PlayerBullet");
        int enemyBulletLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "EnemyBullet");
        int powerupLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Powerup");
        int groundLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Ground");
        int wallLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Wall");
        int forestLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Forest");
        int waterLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Water");
        // Set layer collision matrix for the moving objects
        // Player 
        Physics2D.IgnoreLayerCollision(playerTankLayer, playerTankLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, enemyTankLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, playerBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, powerupLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, forestLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, waterLayer, false);
        // Enemy tank
        Physics2D.IgnoreLayerCollision(enemyTankLayer, enemyTankLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, playerBulletLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, powerupLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, forestLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, waterLayer, false);
        // Player bullet 
        Physics2D.IgnoreLayerCollision(playerBulletLayer, playerBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, powerupLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, forestLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, waterLayer, false);
        // Enemy bullet
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, powerupLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, groundLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, forestLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, waterLayer, false);
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
        mainCameraObject.GetComponent<Camera>().orthographicSize = 15.0f;
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags: enemy, player (already exist) for factions
        ScriptUtilities.CreateTag(scriptPrefix + "Player");
        ScriptUtilities.CreateTag(scriptPrefix + "PlayerBullet");
        ScriptUtilities.CreateTag(scriptPrefix + "Enemy");
        ScriptUtilities.CreateTag(scriptPrefix + "EnemyBullet");
        ScriptUtilities.CreateTag(scriptPrefix + "EnemySpawnPoint");
        ScriptUtilities.CreateTag(scriptPrefix + "Powerup");
        ScriptUtilities.CreateTag(scriptPrefix + "PowerupSpawnPoint");
        // Sorting Layers: Ground, Forest
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Ground");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Water");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Objects");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Forest");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Powerup");
        // Layers: to sort collision detections
        int playerTankLayer = ScriptUtilities.CreateLayer(scriptPrefix + "PlayerTank");
        int enemyTankLayer = ScriptUtilities.CreateLayer(scriptPrefix + "EnemyTank");
        int playerBulletLayer = ScriptUtilities.CreateLayer(scriptPrefix + "PlayerBullet");
        int enemyBulletLayer = ScriptUtilities.CreateLayer(scriptPrefix + "EnemyBullet");
        int powerupLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Powerup");
        int groundLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Ground");
        int wallLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Wall");
        int forestLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Forest");
        int waterLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Water");
        // Set layer collision matrix for the moving objects
        // Player 
        Physics2D.IgnoreLayerCollision(playerTankLayer, playerTankLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, enemyTankLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, playerBulletLayer, true);
        Physics2D.IgnoreLayerCollision(playerTankLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, powerupLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(playerTankLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(playerTankLayer, forestLayer, true);
        Physics2D.IgnoreLayerCollision(playerTankLayer, waterLayer, false);
        // Enemy tank
        Physics2D.IgnoreLayerCollision(enemyTankLayer, enemyTankLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, playerBulletLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, enemyBulletLayer, true);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, powerupLayer, true);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, forestLayer, true);
        Physics2D.IgnoreLayerCollision(enemyTankLayer, waterLayer, false);
        // Player bullet 
        Physics2D.IgnoreLayerCollision(playerBulletLayer, playerBulletLayer, true);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, enemyBulletLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, powerupLayer, true);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, forestLayer, true);
        Physics2D.IgnoreLayerCollision(playerBulletLayer, waterLayer, true);
        // Enemy bullet
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, enemyBulletLayer, true);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, powerupLayer, true);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, groundLayer, true);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, wallLayer, false);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, forestLayer, true);
        Physics2D.IgnoreLayerCollision(enemyBulletLayer, waterLayer, true);
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
        // Create Shoot action and add bindings
        var action = map.AddAction("Shoot", interactions: "tap");
        action.AddBinding(new InputBinding("<Keyboard>/space"));
        action.AddBinding(new InputBinding("<Mouse>/leftButton"));
        // Create Move action and add bindings
        action = map.AddAction("Move");
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
        const float k = 1.0f / 255.0f;
        Color chassisColor = new Color(168.0f * k, 168.0f * k, 168.0f * k);
        Color turretColor = new Color(250.0f * k, 250.0f * k, 250.0f * k);
        Color gunColor = new Color(228.0f * k, 228.0f * k, 228.0f * k);
        Color goldColor = new Color(234.0f * k, 199.0f * k, 28.0f * k);
        Color forestGreen = new Color(50.0f * k, 205.0f * k, 50.0f * k); 
        Color waterBlue = new Color(30.0f * k, 144.0f * k, 255.0f * k); 
        Color brickColor = new Color(183.0f * k, 177.0f * k, 174.0f * k); 
        Color steelGray = new Color(255.0f * k, 255.0f * k, 255.0f * k); 
        Color powerUpColor = new Color(255.0f * k, 223.0f * k, 0.0f * k);
        Color groundColor = new Color(71.0f * k, 72.0f * k, 76.0f * k);
        Color boundaryColor = new Color(0.0f * k, 0.0f * k, 128.0f * k);

        // Generate textures
        // Gray scale 
        path = DrawGrayScaleTankTexture(chassisColor, turretColor, gunColor);
        spritePaths[(int)SpritePath.Tank] = path;
        // Powerup - Bomb
        path = DrawPowerupBombTexture(powerUpColor);
        spritePaths[(int)SpritePath.PowerupBomb] = path;
        
        // Bullet
        path = ContentUtilities.CreateTexture2DTriangleAsset("bullet_texture", texturesPath, textureSize / 4, textureSize / 4, Color.white);
        spritePaths[(int)SpritePath.Bullet] = path;

        // Environment
        path = ContentUtilities.CreateTexture2DRectangleAsset("boundary_texture", texturesPath, textureSize / 2, textureSize / 2, boundaryColor);
        spritePaths[(int)SpritePath.Boundary] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("ground_texture", texturesPath, textureSize / 2, textureSize / 2, groundColor);
        spritePaths[(int)SpritePath.Ground] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("brick_texture", texturesPath, textureSize / 2, textureSize / 2, brickColor);
        spritePaths[(int)SpritePath.Brick] = path;
        path = ContentUtilities.CreateTexture2DOctagonAsset("forest_texture", texturesPath, textureSize / 2, textureSize / 2, forestGreen);
        spritePaths[(int)SpritePath.Forest] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("steel_texture", texturesPath, textureSize / 2, textureSize / 2, steelGray);
        spritePaths[(int)SpritePath.Steel] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("water_texture", texturesPath, textureSize / 2, textureSize / 2, waterBlue);
        spritePaths[(int)SpritePath.Water] = path;

        // Headquarters
        path = ContentUtilities.CreateTexture2DTriangleAsset("headquarters_texture", texturesPath, textureSize, textureSize, goldColor);
        spritePaths[(int)SpritePath.Headquarters] = path;
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

        // Create the player spawn point
        newObject = new GameObject("PlayerSpawnPoint");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create enemy spawn points
        newObject = new GameObject("EnemySpawnPoint");
        // Add a trigger collider
        BoxCollider2D collider = newObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2.0f, 2.0f);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create 4 power up spawn points and put into a container
        GameObject container = new GameObject("PowerupsSpawns");
        newObject = new GameObject("PowerupSpawnPoint");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        GameObject prefab = ContentUtilities.LoadPrefab("PowerupSpawnPoint", prefabsPath);
        const int numPowerupSpawns = 4;
        float d = 8.5f;
        Vector2[] positions = { new Vector2(-d, d), new Vector2(d, d), new Vector2(d, -d), new Vector2(-d, -d) };
        for (int i = 0; i < numPowerupSpawns; i++)
        {
            string index = string.Format("{00}", i + 1);
            newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            newObject.name = prefab.name + index;
            newObject.tag = scriptPrefix + "PowerupSpawnPoint";
            newObject.transform.position = positions[i];
            newObject.transform.SetParent(container.transform);
        }

        // Create objects then make them prefab
        const float k = 1.0f / 255.0f;
        string tag;
        string layer;
        // Player tank
        tag = scriptPrefix + "Player";
        layer = scriptPrefix + "PlayerTank";
        Color playerColor = new Color(234.0f * k, 199.0f * k, 28.0f * k);
        AssembleTankPrefab("PlayerTank", tag, layer, SpritePath.Tank, playerColor, true);
        tag = scriptPrefix + "Enemy";
        layer = scriptPrefix + "EnemyTank";
        // Enemy tank minion
        Color minionColor = new Color(255.0f * k, 255.0f * k, 255.0f * k);
        AssembleTankPrefab("MinionEnemyTank", tag, layer, SpritePath.Tank, minionColor);
        // Enemy tank veteran
        Color veteranColor = new Color(102.0f * k, 153.0f * k, 204.0f * k);
        AssembleTankPrefab("VeteranEnemyTank", tag, layer, SpritePath.Tank, veteranColor);
        // Enemy tank elite
        Color eliteColor = new Color(194.0f * k, 24.0f * k, 7.0f * k);
        AssembleTankPrefab("EliteEnemyTank", tag, layer, SpritePath.Tank, eliteColor);
        // Enemy tank boss
        Color bossColor = new Color(225.0f * k, 0.0f * k, 225.0f * k);
        AssembleTankPrefab("BossEnemyTank", tag, layer, SpritePath.Tank, bossColor);

        // Create bullet
        CreateBulletPrefab("PlayerBullet", scriptPrefix + "PlayerBullet", scriptPrefix + "PlayerBullet", SpritePath.Bullet);
        CreateBulletPrefab("EnemyBullet", scriptPrefix + "EnemyBullet", scriptPrefix + "EnemyBullet", SpritePath.Bullet);

        // Create boundary
        CreateBoundary(SpritePath.Boundary);

        // Headquarters
        CreateHeadquarters(SpritePath.Headquarters);

        // Power up
        AssemblePowerupPrefab("PowerupBomb", SpritePath.PowerupBomb);
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
        GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w - margin, h, "Score: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
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
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "VICTORY!", TextAnchor.MiddleCenter, fontSize, Color.white);
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
        sb.AppendLine("Use Arrow Keys or WASD to Control Your Tank");
        sb.AppendLine("Press Space Bar or Left Click to Shoot");
        sb.AppendLine("Protect Your Base and Use the Bomb Power Up Wisely!");
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
        WriteBT2DBlinkTextScriptToFile();
        WriteBT2DBulletScriptToFile();
        WriteBT2DDestructibleScriptToFile();
        WriteBT2DEnemyScriptToFile();
        WriteBT2DEnemySpawnPointScriptToFile();
        WriteBT2DGameManagerScriptToFile();
        WriteBT2DHeadquartersScriptToFile();
        WriteBT2DPlayerScriptToFile();
        WriteBT2DPlayerSpawnPointScriptToFile();
        WriteBT2DPowerupSpawnPointScriptToFile();
        WriteBT2DTankScriptToFile();
    }

    private static void GenerateTileMap()
    {
        string tileAssetPath;

        // Create tile asset
        tileAssetPath = ContentUtilities.CreateTileAsset("ground_tile", spritePaths[(int)SpritePath.Ground], tilesPath);
        tilePaths[(int)TilePath.Ground] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("brick_tile", spritePaths[(int)SpritePath.Brick], tilesPath);
        tilePaths[(int)TilePath.Brick] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("steel_tile", spritePaths[(int)SpritePath.Steel], tilesPath);
        tilePaths[(int)TilePath.Steel] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("forest_tile", spritePaths[(int)SpritePath.Forest], tilesPath);
        tilePaths[(int)TilePath.Forest] = tileAssetPath;
        tileAssetPath = ContentUtilities.CreateTileAsset("water_tile", spritePaths[(int)SpritePath.Water], tilesPath);
        tilePaths[(int)TilePath.Water] = tileAssetPath;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        string groundSortingLayer = scriptPrefix + "Ground";
        string waterSortingLayer = scriptPrefix + "Water";
        string objectsSortingLayer = scriptPrefix + "Objects";
        string forestSortingLayer = scriptPrefix + "Forest";
        // Ground layer
        GameObject tilemapObject;
        tilemapObject = ContentUtilities.CreateTilemapObject("GroundLayer");
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Ground");
        tilemapObject.GetComponent<TilemapRenderer>().sortingLayerName = groundSortingLayer;
        // Find the automatically created Grid object
        GameObject gridObject = GameObject.Find("Grid");
        // Water layer
        tilemapObject = ContentUtilities.CreateTilemapObject("WaterLayer", gridObject);
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Water");
        tilemapObject.GetComponent<TilemapRenderer>().sortingLayerName = waterSortingLayer;
        tilemapObject.AddComponent<Rigidbody2D>();
        tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapObject.AddComponent<CompositeCollider2D>();
        tilemapObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        tilemapObject.GetComponent<TilemapCollider2D>().usedByComposite = true;
        // Brick layer
        tilemapObject = ContentUtilities.CreateTilemapObject("BrickLayer", gridObject);
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Wall");
        tilemapObject.GetComponent<TilemapRenderer>().sortingLayerName = objectsSortingLayer;
        tilemapObject.AddComponent<Rigidbody2D>();
        tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        // Steel layer
        tilemapObject = ContentUtilities.CreateTilemapObject("SteelLayer", gridObject);
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Wall");
        tilemapObject.GetComponent<TilemapRenderer>().sortingLayerName = objectsSortingLayer;
        tilemapObject.AddComponent<Rigidbody2D>();
        tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        // Forest layer
        tilemapObject = ContentUtilities.CreateTilemapObject("ForestLayer", gridObject);
        tilemapObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Forest");
        tilemapObject.GetComponent<TilemapRenderer>().sortingLayerName = forestSortingLayer;

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        Tile tile;
        // 0 - Ground, 1 - brick, 2 - steel, 3 - forest, 4 - water
        // Ground
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Ground]);
        paletteTilemap.SetTile(new Vector3Int(0, 0, 0), tile);
        // Brick
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Brick]);
        paletteTilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        // Steel
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Steel]);
        paletteTilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        // Forest
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Forest]);
        paletteTilemap.SetTile(new Vector3Int(3, 0, 0), tile);
        // Water
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Water]);
        paletteTilemap.SetTile(new Vector3Int(4, 0, 0), tile);
        // ... Add more tiles if needed
    }

    private static void AddPlayerInputComponent(GameObject playerObject)
    {
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        playerObject.AddComponent<PlayerInput>().actions = asset;
        playerObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
    }

    private static void AssemblePowerupPrefab(string name, SpritePath powerupRef)
    {
        string path;
        Rigidbody2D rb;
        string sortingLayer = scriptPrefix + "Powerup";

        // Get texture path
        path = spritePaths[(int)SpritePath.PowerupBomb];
        // Create body
        GameObject frameObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, path);
        frameObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Powerup");
        frameObject.tag = scriptPrefix + "Powerup";
        // Configure rigid body and collider
        rb = frameObject.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        rb.gravityScale = 0.0f;
        frameObject.GetComponent<BoxCollider2D>().isTrigger = true;
        frameObject.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
        // Create prefab
        ContentUtilities.CreatePrefab(frameObject, prefabsPath, true);
    }

    private static void AssembleTankPrefab(string name, string tag, string layer, SpritePath textureEnum, Color color, bool isPlayer=false)
    {
        string path;
        Rigidbody2D rb;
        BoxCollider2D collider;
        string sortingLayer = scriptPrefix + "Objects";
        
        // Get the path
        path = spritePaths[(int)textureEnum];
        // Create the object
        GameObject tankObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, path);
        tankObject.tag = tag;
        tankObject.layer = ScriptUtilities.IndexOfLayer(layer);
        // Set color
        tankObject.GetComponent<SpriteRenderer>().color = color;
        // Configure rigid body and collider
        rb = tankObject.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0.0f;
        collider = tankObject.GetComponent<BoxCollider2D>();
        collider.size *= 0.97f;
        tankObject.GetComponent<SpriteRenderer>().sortingLayerName = sortingLayer;
        // Add input
        if (isPlayer == true)
        {
            AddPlayerInputComponent(tankObject);
        }

        // Add gun reference point
        GameObject gunMuzzle = new GameObject("GunMuzzle");
        gunMuzzle.transform.SetParent(tankObject.transform);
        gunMuzzle.transform.localPosition = new Vector2(0.0f, 1.0f);

        // Create prefab and destroy the object
        ContentUtilities.CreatePrefab(tankObject, prefabsPath, true);
    }

    private static void AssignTankStats(GameObject tankPrefab, GameObject bulletPrefab, int hitPoint, float speed, float bulletSpeed, int value)
    {
        string className = scriptPrefix + "Tank";
        ScriptUtilities.AssignObjectFieldToObject(bulletPrefab, tankPrefab, className, "bulletPrefab");
        ScriptUtilities.AssignIntFieldToObject(hitPoint, tankPrefab, className, "hitPoint");
        ScriptUtilities.AssignFloatFieldToObject(speed, tankPrefab, className, "speed");
        ScriptUtilities.AssignFloatFieldToObject(bulletSpeed, tankPrefab, className, "bulletSpeed");
        ScriptUtilities.AssignIntFieldToObject(value, tankPrefab, className, "value");
    }

    private static void CreateBoundary(SpritePath textureRef)
    {
        string assetPath = spritePaths[(int)textureRef];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Box;

        // Create boundary object to contain the gray boundary objects
        GameObject boundaryObject = new GameObject("Boundary");

        Rigidbody2D rb;
        Transform transform;
        // Left boundary
        GameObject leftBound = ContentUtilities.CreateTexturedBody("LeftBound", -14.0f, 0.0f, assetPath, shape);
        transform = leftBound.GetComponent<Transform>();
        transform.localScale = new Vector2(2.0f, 26.0f);
        transform.SetParent(boundaryObject.transform);
        rb = leftBound.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        // Right boundary
        GameObject rightBound = ContentUtilities.CreateTexturedBody("RightBound", 14.0f, 0.0f, assetPath, shape);
        transform = rightBound.GetComponent<Transform>();
        transform.localScale = new Vector2(2.0f, 26.0f);
        transform.SetParent(boundaryObject.transform);
        rb = rightBound.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        // Top bound
        GameObject topBound = ContentUtilities.CreateTexturedBody("TopBound", 0.0f, 14.0f, assetPath, shape);
        transform = topBound.GetComponent<Transform>();
        transform.localScale = new Vector2(30.0f, 2.0f);
        transform.SetParent(boundaryObject.transform);
        rb = topBound.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        // Bottom bound
        GameObject bottomBound = ContentUtilities.CreateTexturedBody("BottomBound", 0.0f, -14.0f, assetPath, shape);
        transform = bottomBound.GetComponent<Transform>();
        transform.localScale = new Vector2(30.0f, 2.0f);
        transform.SetParent(boundaryObject.transform);
        rb = bottomBound.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    private static void CreateBulletPrefab(string name, string layerName, string tagName, SpritePath textureRef)
    {
        string assetPath = spritePaths[(int)textureRef];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Polygon;
        GameObject newObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, assetPath, shape);
        newObject.layer = ScriptUtilities.IndexOfLayer(layerName);
        newObject.tag = tagName;
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        newObject.GetComponent<SpriteRenderer>().sortingLayerName = scriptPrefix + "Objects";
        newObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        rb.freezeRotation = true;
        rb.gravityScale = 0.0f;
        rb.mass = 0.001f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void CreateHeadquarters(SpritePath textureRef)
    {
        string assetPath = spritePaths[(int)textureRef];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Box;
        GameObject newObject = ContentUtilities.CreateTexturedBody("Headquarters", 0.0f, 0.0f, assetPath, shape);
        newObject.tag = scriptPrefix + "Player";
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0.0f;
        newObject.GetComponent<SpriteRenderer>().sortingLayerName = scriptPrefix + "Ground";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static string DrawGrayScaleTankTexture(Color chassisColor, Color turretColor, Color gunColor)
    {
        // Draw chassis shape
        int chassisW = textureSize;
        int chassisH = textureSize;
        Color[] chassisMap = ContentUtilities.FillBitmapShapeRectangle(chassisW, chassisH, chassisColor);

        // Draw turret shape
        int turretW = chassisW / 2;
        int turretH = chassisH / 2;
        Color[] turretMap = ContentUtilities.FillBitmapShapeRectangle(turretW, turretH, turretColor);

        // Draw gun color
        int gunW = turretW / 2;
        int gunH = turretH;
        Color[] gunMap = ContentUtilities.FillBitmapShapeRectangle(gunW, gunH, gunColor);

        // Combined the shapes
        chassisMap = ContentUtilities.CopyBitmap(gunMap, gunW, gunH, chassisMap, chassisW, chassisH, new Vector2Int(chassisW / 2 - turretW / 2 + gunW / 2, chassisH - gunH - gunH / 4));
        chassisMap = ContentUtilities.CopyBitmap(turretMap, turretW, turretH, chassisMap, chassisW, chassisH, new Vector2Int(chassisW / 4, chassisH / 4));

        // Create texture asset
        return ContentUtilities.CreateBitmapAsset("tank_texture", chassisMap, chassisW, chassisH, texturesPath);
    }

    private static string DrawPowerupBombTexture(Color color)
    {
        // Draw frame shape
        int frameW = textureSize;
        int frameH = textureSize;
        Color[] frameMap = ContentUtilities.FillBitmapShapeRectangleFrame(frameW, frameH, color);

        // Draw content shape
        int contentW = frameW / 2;
        int contentH = frameH / 2;
        Color[] contentMap = ContentUtilities.FillBitmapShapeOctagon(contentW, contentH, color);

        // Combine the shapes
        Color[] combined = ContentUtilities.CopyBitmap(contentMap, contentW, contentH, frameMap, frameW, frameH, new Vector2Int(frameW / 4, frameH / 4));

        // Create bitmap asset
        return ContentUtilities.CreateBitmapAsset("powerup_bomb_texture", combined, frameW, frameH, texturesPath);
    }

    private static int[] GetBrickLayerMapArray()
    {
        return new int[]
        {
            // Array size (26, 26)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1,  1,  1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1,  1,  1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1,  1,  1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1,  1,  1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1,  1,  1, -1, -1, -1, -1,
              1,  1, -1, -1,  1,  1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1,  1,  1, -1, -1,  1,  1,
             -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1,
             -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
              1,  1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1,  1,  1,
              1,  1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1,  1,  1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,  1,  1, -1, -1,
             -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetForestLayerMapArray()
    {
        return new int[]
        {
            // Array size (26, 26)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  3,  3, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetGroundLayerMapArray()
    {
        return new int[]
        {
            // Array size(26, 26)
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
              0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0
        };
    }

    private static int[] GetSteelLayerMapArray()
    {
        return new int[]
        {
            // Array size (26, 26)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
              2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetWaterLayerMapArray()
    {
        return new int[]
        {
            // Array size (26, 26)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  4,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  4,  4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject lifeTextObject = GameObject.Find("LifeText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject[] powerupSpawnPoints = GameObject.FindGameObjectsWithTag(scriptPrefix + "PowerupSpawnPoint");
        GameObject playerSpawnPointObject = GameObject.Find("PlayerSpawnPoint");
        GameObject[] enemySpawnPoints = GameObject.FindGameObjectsWithTag("BT2DEnemySpawnPoint");
        GameObject headquartersObject = GameObject.Find("Headquarters");
        GameObject groundLayerObject = GameObject.Find("GroundLayer");
        GameObject waterLayerObject = GameObject.Find("WaterLayer");
        GameObject brickLayerObject = GameObject.Find("BrickLayer");
        GameObject steelLayerObject = GameObject.Find("SteelLayer");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(lifeTextObject, "Text", go, className, "lifeText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectsFieldToObject(powerupSpawnPoints, go, className, "powerupSpawnPoints");
        ScriptUtilities.AssignObjectFieldToObject(playerSpawnPointObject, go, className, "playerSpawnPoint");
        ScriptUtilities.AssignObjectsFieldToObject(enemySpawnPoints, go, className, "enemySpawnPoints");
        ScriptUtilities.AssignObjectFieldToObject(headquartersObject, go, className, "headquartersObject");
        ScriptUtilities.AssignComponentFieldToObject(groundLayerObject, "Tilemap", go, className, "groundLayer");
        ScriptUtilities.AssignComponentFieldToObject(waterLayerObject, "Tilemap", go, className, "waterLayer");
        ScriptUtilities.AssignComponentFieldToObject(brickLayerObject, "Tilemap", go, className, "brickLayer");
        ScriptUtilities.AssignComponentFieldToObject(steelLayerObject, "Tilemap", go, className, "steelLayer");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
    }

    private static void LayoutLevel()
    {
        // Get the tile maps
        Tilemap groundLayer = GameObject.Find("GroundLayer").GetComponent<Tilemap>();
        Tilemap waterLayer = GameObject.Find("WaterLayer").GetComponent<Tilemap>();
        Tilemap brickLayer = GameObject.Find("BrickLayer").GetComponent<Tilemap>();
        Tilemap steelLayer = GameObject.Find("SteelLayer").GetComponent<Tilemap>();
        Tilemap forestLayer = GameObject.Find("ForestLayer").GetComponent<Tilemap>();
        // Create a tiles guide (order matters)
        Tile[] tilesGuide = new Tile[tilePaths.Length];
        for (int i = 0; i < tilesGuide.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTileAtPath(tilePaths[i]);
        }
        // Configure size and start points
        Vector2Int size = new Vector2Int(26, 26);
        Vector2Int start = new Vector2Int(-13, -13);
        // Layout according to a map array
        ContentUtilities.PlotTiles(groundLayer, tilesGuide, GetGroundLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(waterLayer, tilesGuide, GetWaterLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(brickLayer, tilesGuide, GetBrickLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(steelLayer, tilesGuide, GetSteelLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(forestLayer, tilesGuide, GetForestLayerMapArray(), size, start);

        // Layout headquarters
        GameObject prefab = ContentUtilities.LoadPrefab("Headquarters", prefabsPath);
        LayoutObject(prefab, 0.0f, -12.0f);
        // Spawn points
        prefab = ContentUtilities.LoadPrefab("PlayerSpawnPoint", prefabsPath);
        GameObject newObject;
        // Player spawn point
        newObject = LayoutObject(prefab, 0.0f, -8.0f);
        newObject.name = "PlayerSpawnPoint";
        // Enemy spawn points
        prefab = ContentUtilities.LoadPrefab("EnemySpawnPoint", prefabsPath);
        newObject = LayoutObject(prefab, -12.0f, 12.0f);
        newObject.name = "EnemySpawnPoint1";
        newObject.tag = scriptPrefix + "EnemySpawnPoint";
        newObject = LayoutObject(prefab, 0.0f, 12.0f);
        newObject.name = "EnemySpawnPoint2";
        newObject.tag = scriptPrefix + "EnemySpawnPoint";
        newObject = LayoutObject(prefab, 12.0f, 12.0f);
        newObject.name = "EnemySpawnPoint3";
        newObject.tag = scriptPrefix + "EnemySpawnPoint";
    }

    private static GameObject LayoutObject(GameObject prefab, float posX, float posY)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        newObject.transform.position = new Vector2(posX, posY);
        return newObject;
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Bullet", "Destructible", "Enemy", "EnemySpawnPoint", "GameManager", "Headquarters", "Player",
            "PlayerSpawnPoint", "PowerupSpawnPoint", "Tank" }))
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
        GameObject brickLayerObject = GameObject.Find("BrickLayer");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Access the prefabs
        GameObject gameManagerPrefab = ContentUtilities.LoadPrefab("GameManager", prefabsPath);
        GameObject powerupSpawnPointPrefab = ContentUtilities.LoadPrefab("PowerupSpawnPoint", prefabsPath);
        GameObject playerSpawnPointPrefab = ContentUtilities.LoadPrefab("PlayerSpawnPoint", prefabsPath);
        GameObject enemySpawnPointPrefab = ContentUtilities.LoadPrefab("EnemySpawnPoint", prefabsPath);
        GameObject headquartersPrefab = ContentUtilities.LoadPrefab("Headquarters", prefabsPath);
        GameObject powerupBombPrefab = ContentUtilities.LoadPrefab("PowerupBomb", prefabsPath);
        GameObject playerBulletPrefab = ContentUtilities.LoadPrefab("PlayerBullet", prefabsPath);
        GameObject enemyBulletPrefab = ContentUtilities.LoadPrefab("EnemyBullet", prefabsPath);
        GameObject minionEnemyTankPrefab = ContentUtilities.LoadPrefab("MinionEnemyTank", prefabsPath);
        GameObject veteranEnemyTankPrefab = ContentUtilities.LoadPrefab("VeteranEnemyTank", prefabsPath);
        GameObject eliteEnemyTankPrefab = ContentUtilities.LoadPrefab("EliteEnemyTank", prefabsPath);
        GameObject bossEnemyTankPrefab = ContentUtilities.LoadPrefab("BossEnemyTank", prefabsPath);
        GameObject playerTankPrefab = ContentUtilities.LoadPrefab("PlayerTank", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PowerupSpawnPoint", powerupSpawnPointPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PlayerSpawnPoint", playerSpawnPointPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "EnemySpawnPoint", enemySpawnPointPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Headquarters", headquartersPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Bullet", playerBulletPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Bullet", enemyBulletPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", minionEnemyTankPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", veteranEnemyTankPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", eliteEnemyTankPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", bossEnemyTankPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Player", playerTankPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Destructible", brickLayerObject);

        // Assign to public fields for monobehavior classes, if needed 
        string className;
        className = scriptPrefix + "PlayerSpawnPoint";
        ScriptUtilities.AssignObjectFieldToObject(playerTankPrefab, playerSpawnPointPrefab, className, "tankPrefab");
        className = scriptPrefix + "PowerupSpawnPoint";
        ScriptUtilities.AssignObjectFieldToObject(powerupBombPrefab, powerupSpawnPointPrefab, className, "powerupPrefab");
        className = scriptPrefix + "Tank";
        AssignTankStats(minionEnemyTankPrefab, enemyBulletPrefab, 1, 2.0f, 4.0f, 100);
        AssignTankStats(veteranEnemyTankPrefab, enemyBulletPrefab, 1, 5.0f, 6.0f, 200);
        AssignTankStats(eliteEnemyTankPrefab, enemyBulletPrefab, 1, 3.5f, 9.0f, 300);
        AssignTankStats(bossEnemyTankPrefab, enemyBulletPrefab, 4, 4.0f, 6.0f, 400);
        AssignTankStats(playerTankPrefab, playerBulletPrefab, 1, 5.0f, 10.0f, 0);
        // Assign tank prefabs array to game manager
        GameObject[] spawns =
        {
            minionEnemyTankPrefab,
            minionEnemyTankPrefab,
            minionEnemyTankPrefab,
            minionEnemyTankPrefab,
            minionEnemyTankPrefab,
            veteranEnemyTankPrefab,
            minionEnemyTankPrefab,
            veteranEnemyTankPrefab,
            eliteEnemyTankPrefab,
            minionEnemyTankPrefab,
            veteranEnemyTankPrefab,
            minionEnemyTankPrefab,
            eliteEnemyTankPrefab,
            veteranEnemyTankPrefab,
            minionEnemyTankPrefab,
            eliteEnemyTankPrefab,
            veteranEnemyTankPrefab,
            bossEnemyTankPrefab,
            minionEnemyTankPrefab,
            eliteEnemyTankPrefab
        };
        className = scriptPrefix + "GameManager";
        ScriptUtilities.AssignObjectsFieldToObject(spawns, gameManagerPrefab, className, "enemyPrefabs");
        // Instantiate objects
        InstantiateAndSetupGameManager(gameManagerPrefab);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static void PostProcessTextures()
    {
        float PPU = textureSize / 2;
        Sprite[] tempSprites = new Sprite[(int)SpritePath.Max];

        for (int i = 0; i < (int)SpritePath.Max; i++)
        {
            string path = "Assets/" + spritePaths[i];
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            ti.spritePixelsPerUnit = PPU;
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
        // Note: the scripts and tilemaps must exist before this function is called
        ReverseEngineerTilemaps();
        ReverseEngineerScripts();
    }

    private static void ReverseEngineerTilemaps()
    {
        // Call this method only after the Game Template has been created

        int startX = -13;
        int startY = -13;
        int w = 26;
        int h = 26;

        // Create name arrays following the order set by the enum above
        string[] tileNames = new string[] {
            "ground_tile", "brick_tile", "steel_tile", "forest_tile", "water_tile"
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
        Tilemap[] layers =
        {
            GameObject.Find("GroundLayer").GetComponent<Tilemap>(),
            GameObject.Find("WaterLayer").GetComponent<Tilemap>(),
            GameObject.Find("BrickLayer").GetComponent<Tilemap>(),
            GameObject.Find("SteelLayer").GetComponent<Tilemap>(),
            GameObject.Find("ForestLayer").GetComponent<Tilemap>()
        };
        // Convert and print
        foreach (Tilemap layer in layers)
        {
            // Convert into a map array
            int[] mapArray = ContentUtilities.ConvertTilemapToMapArray(layer, guide, w, h, startX, startY);
            // Convert map array to string
            StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
            // Print out to console
            Debug.Log("Printing Tile Map " + layer.name);
            Debug.Log(sb.ToString());
        }
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DBullet", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DDestructible", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DEnemy", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DEnemySpawnPoint", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DHeadquarters", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DPlayer", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DPlayerSpawnPoint", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DPowerupSpawnPoint", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("BT2DTank", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void WriteBT2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("BT2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DBulletScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1755);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DBullet : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject owner;");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(GameObject tankObject, Vector2 position, Vector2 direction, float speed)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Initialize the tank bullet / shell");
        sb.AppendLine("        owner = tankObject;");
        sb.AppendLine("        // Place it at the gun and give it a muzzle velocity");
        sb.AppendLine("        transform.position = position;");
        sb.AppendLine("        transform.up = direction;");
        sb.AppendLine("        GetComponent<Rigidbody2D>().velocity = (direction * speed);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        // If owner is destroyed");
        sb.AppendLine("        if (owner == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Destroy this bullet");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Deactivate this bullet");
        sb.AppendLine("            gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DBullet", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DDestructibleScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1686);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DDestructible : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private Tilemap tilemap;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        tilemap = GetComponent<Tilemap>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Tiles in the destructible layer can be destroyed by bullets");
        sb.AppendLine("");
        sb.AppendLine("        if (collision.gameObject.name.Contains(\"Bullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Destroy each hit tiles");
        sb.AppendLine("            Vector3 hitPosition = Vector3.zero;");
        sb.AppendLine("            foreach(ContactPoint2D hit in collision.contacts)");
        sb.AppendLine("            {");
        sb.AppendLine("                hitPosition.x = hit.point.x + 0.5f * hit.normal.x;");
        sb.AppendLine("                hitPosition.y = hit.point.y + 0.5f * hit.normal.y;");
        sb.AppendLine("                tilemap.SetTile(tilemap.WorldToCell(hitPosition), null);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DDestructible", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DEnemyScriptToFile()
    {
        StringBuilder sb = new StringBuilder(15843);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DEnemy : BT2DTank");
        sb.AppendLine("{");
        sb.AppendLine("    private BT2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private GameObject playerObject = null;");
        sb.AppendLine("    private GameObject headquartersObject = null;");
        sb.AppendLine("    private AIState state;");
        sb.AppendLine("    private float actionTimer = 0.0f;");
        sb.AppendLine("    const float actionDelay = 5.0f;");
        sb.AppendLine("    const float initialDelay = 1.0f;");
        sb.AppendLine("    const int sightRange = 10;");
        sb.AppendLine("    Vector2 playerTaxiCabMetric = new Vector2();");
        sb.AppendLine("    Vector2 headquartersTaxiCabMetric = new Vector2();");
        sb.AppendLine("    Vector2 aiDestination;");
        sb.AppendLine("");
        sb.AppendLine("    private enum AIState");
        sb.AppendLine("    {");
        sb.AppendLine("        Idle = 0,");
        sb.AppendLine("        Move,");
        sb.AppendLine("        Ready,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    new protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Initialize this enemy tank");
        sb.AppendLine("        base.Start();");
        sb.AppendLine("        gameManager = BT2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        EnterStateIdle();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If game is not active");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            // Stop");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Update AI decision every once in a while");
        sb.AppendLine("        actionTimer -= Time.deltaTime;");
        sb.AppendLine("        if (actionTimer <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            UpdateAIDecision();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Update the enemy's current state");
        sb.AppendLine("        switch (state)");
        sb.AppendLine("        {");
        sb.AppendLine("            case AIState.Idle:");
        sb.AppendLine("                ExecuteStateIdle();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case AIState.Move:");
        sb.AppendLine("                ExecuteStateMove();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case AIState.Ready:");
        sb.AppendLine("                ExecuteStateReady();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("");
        sb.AppendLine("        if (collidedObject.CompareTag(\"BT2DPlayerBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Hit by the player's bullet");
        sb.AppendLine("            Hit();");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (collidedObject.CompareTag(\"BT2DEnemy\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bumped into a friendly tank, will try to avoid");
        sb.AppendLine("            AvoidFriendly(collidedObject.transform.position);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerEnter2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        if (collidedObject.CompareTag(\"BT2DEnemySpawnPoint\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            collidedObject.GetComponent<BT2DEnemySpawnPoint>().IncrementOccupancy();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerExit2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        if (collidedObject.CompareTag(\"BT2DEnemySpawnPoint\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            collidedObject.GetComponent<BT2DEnemySpawnPoint>().DecrementOccupancy();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AvoidFriendly(Vector2 collidedObjectPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Randomly pick and move in another direction");
        sb.AppendLine("        Vector2 avoidDirection = RandomAvoidanceDirection(collidedObjectPosition);");
        sb.AppendLine("        const float distance = 2.0f;");
        sb.AppendLine("        float duration = distance / speed;");
        sb.AppendLine("        EnterStateMove(avoidDirection, avoidDirection * 2.0f, duration);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateIdle()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Idle for some time");
        sb.AppendLine("        state = AIState.Idle;");
        sb.AppendLine("        actionTimer = 1.0f;");
        sb.AppendLine("        RoundPosition();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateMove(Vector2 up, Vector2 destination, float duration)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Move to a destination");
        sb.AppendLine("        state = AIState.Move;");
        sb.AppendLine("        transform.up = up;");
        sb.AppendLine("        aiDestination = destination;");
        sb.AppendLine("        actionTimer = duration;");
        sb.AppendLine("        RoundPosition();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterStateReady(Vector2 up)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get into the ready state");
        sb.AppendLine("        state = AIState.Ready;");
        sb.AppendLine("        transform.up = up;");
        sb.AppendLine("        actionTimer = actionDelay;");
        sb.AppendLine("        RoundPosition();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateIdle()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb.velocity = new Vector2(0.0f, 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateMove()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Move and fire");
        sb.AppendLine("        base.Shoot();");
        sb.AppendLine("        Vector2 d = aiDestination - new Vector2(transform.position.x, transform.position.y);");
        sb.AppendLine("        if (d.magnitude <= speed * Time.deltaTime)");
        sb.AppendLine("        {");
        sb.AppendLine("            transform.position = aiDestination;");
        sb.AppendLine("            actionTimer = 0.0f;");
        sb.AppendLine("            rb.velocity = new Vector2(0.0f, 0.0f);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        rb.velocity = transform.up * speed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteStateReady()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Shoot");
        sb.AppendLine("        base.Shoot();");
        sb.AppendLine("        rb.velocity = new Vector2(0.0f, 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Hit()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Reduce hit point");
        sb.AppendLine("        base.hitPoint--;");
        sb.AppendLine("        // If hit point reaches zero");
        sb.AppendLine("        if (base.hitPoint <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Destroy this tank");
        sb.AppendLine("            gameManager.AddEnemyDestroyed(1, value);");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private Vector2 RandomAvoidanceDirection(Vector2 collidedObjectPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Randomly pick a direction, except the one given as argument");
        sb.AppendLine("        List<Vector2> directions = new List<Vector2>();");
        sb.AppendLine("        Vector2 displacement = collidedObjectPosition - (Vector2)transform.position;");
        sb.AppendLine("");
        sb.AppendLine("        if (Mathf.Abs(displacement.x) > Mathf.Abs(displacement.y))");
        sb.AppendLine("        {");
        sb.AppendLine("            directions.Add(Vector2.up);");
        sb.AppendLine("            directions.Add(Vector2.down);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            directions.Add(Vector2.left);");
        sb.AppendLine("            directions.Add(Vector2.right);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        int randomInteger = Random.Range(0, directions.Count);");
        sb.AppendLine("        return directions[randomInteger];");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private Vector2 RandomDirection()");
        sb.AppendLine("    {");
        sb.AppendLine("        int randomInteger = Random.Range(0, 4);");
        sb.AppendLine("        if (randomInteger == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new Vector2(0.0f, 1.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (randomInteger == 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new Vector2(0.0f, -1.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (randomInteger == 2)");
        sb.AppendLine("        {");
        sb.AppendLine("            return new Vector2(1.0f, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            return new Vector2(-1.0f, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void RoundPosition()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Round up the tank's position to the closest integer coordinates");
        sb.AppendLine("        transform.position = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateAIDecision()");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdateAIParameters();");
        sb.AppendLine("");
        sb.AppendLine("        GameObject target = null;");
        sb.AppendLine("        Vector2 distanceMetric = Vector2.zero;");
        sb.AppendLine("        const int tolerance = 8 * 8;");
        sb.AppendLine("        if (headquartersObject != null && headquartersTaxiCabMetric.sqrMagnitude <= tolerance)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Attack headquarters if it is close");
        sb.AppendLine("            target = headquartersObject;");
        sb.AppendLine("            distanceMetric = headquartersTaxiCabMetric;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (playerObject != null && playerTaxiCabMetric.sqrMagnitude <= tolerance)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Attack the player if he or she is close");
        sb.AppendLine("            target = playerObject;");
        sb.AppendLine("            distanceMetric = playerTaxiCabMetric;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // If a target is found");
        sb.AppendLine("        if (target != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // If it is on the same line");
        sb.AppendLine("            if (distanceMetric.x == 0 || distanceMetric.y == 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Face that direction and prepare to fire");
        sb.AppendLine("                Vector2 up = new Vector2((float)distanceMetric.x, (float)distanceMetric.y);");
        sb.AppendLine("                EnterStateReady(up.normalized);");
        sb.AppendLine("            }");
        sb.AppendLine("            // Else");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                // Try to close the gap on the target");
        sb.AppendLine("                Vector2 destination = new Vector2(transform.position.x, transform.position.y);");
        sb.AppendLine("                Vector2 direction = Vector2.zero;");
        sb.AppendLine("                float duration = 0.0f;");
        sb.AppendLine("                if (Mathf.Abs(distanceMetric.x) < Mathf.Abs(distanceMetric.y))");
        sb.AppendLine("                {");
        sb.AppendLine("                    destination.x += (float)distanceMetric.x;");
        sb.AppendLine("                    duration = Mathf.Abs(distanceMetric.x) / speed;");
        sb.AppendLine("                    direction.x = distanceMetric.x > 0.0f ? 1.0f : -1.0f;");
        sb.AppendLine("                }");
        sb.AppendLine("                else");
        sb.AppendLine("                {");
        sb.AppendLine("                    destination.y += (float)distanceMetric.y;");
        sb.AppendLine("                    duration = Mathf.Abs(distanceMetric.y) / speed;");
        sb.AppendLine("                    direction.y = distanceMetric.y > 0.0f ? 1.0f : -1.0f;");
        sb.AppendLine("                }");
        sb.AppendLine("                EnterStateMove(direction, destination.normalized, duration);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Move randomly about");
        sb.AppendLine("            Vector2 up = RandomDirection();");
        sb.AppendLine("            const float minDistance = 1.0f;");
        sb.AppendLine("            const float maxDistance = 6.0f;");
        sb.AppendLine("            Vector2 destination = up * Random.Range(minDistance, maxDistance);");
        sb.AppendLine("            float duration = Random.Range(1.0f, actionDelay);");
        sb.AppendLine("            EnterStateMove(destination, up, duration);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateAIParameters()");
        sb.AppendLine("    {");
        sb.AppendLine("        headquartersObject = gameManager.headquartersObject;");
        sb.AppendLine("        if (headquartersObject != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            headquartersTaxiCabMetric.x = (int)headquartersObject.transform.position.x - (int)transform.position.x;");
        sb.AppendLine("            headquartersTaxiCabMetric.y = (int)headquartersObject.transform.position.y - (int)transform.position.y;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        playerObject = gameManager.playerTankObject;");
        sb.AppendLine("        if (playerObject != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            playerTaxiCabMetric.x = (int)playerObject.transform.position.x - (int)transform.position.x;");
        sb.AppendLine("            playerTaxiCabMetric.y = (int)playerObject.transform.position.y - (int)transform.position.y;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DEnemy", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DEnemySpawnPointScriptToFile()
    {
        StringBuilder sb = new StringBuilder(2620);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DEnemySpawnPoint : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int occupancy = 0;");
        sb.AppendLine("");
        sb.AppendLine("    public void DecrementOccupancy()");
        sb.AppendLine("    {");
        sb.AppendLine("        occupancy--;");
        sb.AppendLine("        if (occupancy < 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            occupancy = 0;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void IncrementOccupancy()");
        sb.AppendLine("    {");
        sb.AppendLine("        occupancy++;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    Vector2 RandomDirection()");
        sb.AppendLine("    {");
        sb.AppendLine("        int randomInteger = Random.Range(0, 4);");
        sb.AppendLine("        if (randomInteger == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return transform.up;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (randomInteger == 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            return transform.right;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (randomInteger == 1)");
        sb.AppendLine("        {");
        sb.AppendLine("            return -transform.up;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            return -transform.right;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SpawnTank(GameObject prefab, int number)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (occupancy > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Do not spawn if an existing tank is too close");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Spawn the tank facing a random direction");
        sb.AppendLine("        GameObject newObject = Instantiate(prefab);");
        sb.AppendLine("        newObject.name = prefab.name + number.ToString(\"00\");");
        sb.AppendLine("        newObject.transform.position = transform.position;");
        sb.AppendLine("        newObject.transform.up = RandomDirection();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DEnemySpawnPoint", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(16622);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int[] powerupThresholds = { 3, 7, 12, 16 };");
        sb.AppendLine("    public int maxPowerups = 3;");
        sb.AppendLine("    public GameObject[] enemyPrefabs;");
        sb.AppendLine("    public int maxEnemyInPlay = 5;");
        sb.AppendLine("    public int powerupValue = 500;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text lifeText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public GameObject[] powerupSpawnPoints;");
        sb.AppendLine("    public GameObject playerSpawnPoint;");
        sb.AppendLine("    public GameObject[] enemySpawnPoints;");
        sb.AppendLine("    public GameObject headquartersObject;");
        sb.AppendLine("    public Tilemap groundLayer;");
        sb.AppendLine("    public Tilemap brickLayer;");
        sb.AppendLine("    public Tilemap steelLayer;");
        sb.AppendLine("    public Tilemap waterLayer;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public GameObject playerTankObject;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float leftEdge;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float rightEdge;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float topEdge;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float bottomEdge;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    private int numEnemyDestroyed = 0;");
        sb.AppendLine("    private int numEnemySpawned = 0;");
        sb.AppendLine("    private float enemySpawnInterval = 4.5f;");
        sb.AppendLine("    private float enemySpawnTimer = 3.0f;");
        sb.AppendLine("");
        sb.AppendLine("    public static bool gameStarted = false;");
        sb.AppendLine("    public static BT2DGameManager sharedInstance = null;");
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
        sb.AppendLine("        SetupObjects();");
        sb.AppendLine("        UpdateScore();");
        sb.AppendLine("        UpdateLife();");
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Start the game");
        sb.AppendLine("            playerTankObject = playerSpawnPoint.GetComponent<BT2DPlayerSpawnPoint>().SpawnTank();");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            playButton.gameObject.SetActive(false);");
        sb.AppendLine("            enemySpawnTimer = enemySpawnInterval;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (gameStarted == false)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateEnemySpawns();");
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
        sb.AppendLine("        public void ActivatePowerup(GameObject powerup)");
        sb.AppendLine("    {");
        sb.AppendLine("        // If type is bomb");
        sb.AppendLine("        if (powerup.name.Contains(\"PowerupBomb\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Destroy all enemy tanks");
        sb.AppendLine("            GameObject[] enemies = GameObject.FindGameObjectsWithTag(\"BT2DEnemy\");");
        sb.AppendLine("            foreach (GameObject enemyTank in enemies)");
        sb.AppendLine("            {");
        sb.AppendLine("                Destroy(enemyTank);");
        sb.AppendLine("                AddEnemyDestroyed(1, 0);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Add to score");
        sb.AppendLine("        AddScore(powerupValue);");
        sb.AppendLine("        // Clean up");
        sb.AppendLine("        Destroy(powerup);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddEnemyDestroyed(int number, int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Add to destroyed count");
        sb.AppendLine("        int oldNumber = numEnemyDestroyed;");
        sb.AppendLine("        numEnemyDestroyed += number;");
        sb.AppendLine("        // Check if we need to spawn a new power up");
        sb.AppendLine("        for (int i = 0; i < maxPowerups; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (oldNumber < powerupThresholds[i] && numEnemyDestroyed >= powerupThresholds[i])");
        sb.AppendLine("            {");
        sb.AppendLine("                SpawnPowerup();");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Add score");
        sb.AppendLine("        AddScore(value);");
        sb.AppendLine("        // Check victory condition");
        sb.AppendLine("        if (numEnemyDestroyed >= enemyPrefabs.Length)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameWon();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
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
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameWon()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Victory!\";");
        sb.AppendLine("        resultText.gameObject.SetActive(true);");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return gameStarted == true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyPlayerDestruction()");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdateLife();");
        sb.AppendLine("        BT2DPlayerSpawnPoint spawnPoint = playerSpawnPoint.GetComponent<BT2DPlayerSpawnPoint>();");
        sb.AppendLine("        // If there is life left");
        sb.AppendLine("        if (spawnPoint.totalLife > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Respawn player");
        sb.AppendLine("            playerTankObject = spawnPoint.SpawnTank();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Game over");
        sb.AppendLine("            GameOver();");
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
        sb.AppendLine("        resultText.gameObject.SetActive(false);");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        // Set up the edges");
        sb.AppendLine("        float hW = groundLayer.size.x * 0.5f - 1.0f;");
        sb.AppendLine("        float hH = groundLayer.size.y * 0.5f - 1.0f;");
        sb.AppendLine("        leftEdge = -hW;");
        sb.AppendLine("        rightEdge = hW;");
        sb.AppendLine("        bottomEdge = -hH;");
        sb.AppendLine("        topEdge = hH;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnPowerup()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Randomly pick a spot to spawn power up");
        sb.AppendLine("            // Each spawn point can be used only once");
        sb.AppendLine("        int numSpawnPoints = powerupSpawnPoints.Length;");
        sb.AppendLine("        int randomInteger = Random.Range(0, numSpawnPoints);");
        sb.AppendLine("        int index = randomInteger;");
        sb.AppendLine("        for (int i = 0; i < numSpawnPoints; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            BT2DPowerupSpawnPoint currentPoint = powerupSpawnPoints[index].GetComponent<BT2DPowerupSpawnPoint>();");
        sb.AppendLine("            if (!currentPoint.IsExhausted())");
        sb.AppendLine("            {");
        sb.AppendLine("                currentPoint.Spawn();");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("            index++;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"BattleTank2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void UpdateEnemySpawns()");
        sb.AppendLine("    {");
        sb.AppendLine("        int totalPrefabs = enemyPrefabs.Length;");
        sb.AppendLine("        if (numEnemySpawned >= totalPrefabs)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Do not spawn if spawn count exceeded total tanks in this level");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (numEnemySpawned - numEnemyDestroyed >= maxEnemyInPlay)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Do not spawn if there are too many enemies in play");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (enemySpawnTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            enemySpawnTimer -= Time.deltaTime;");
        sb.AppendLine("            if (enemySpawnTimer > 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Do not spawn if it is too soon after the last spawning");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Reset timer");
        sb.AppendLine("        enemySpawnTimer = enemySpawnInterval;");
        sb.AppendLine("        // Spawn the next tank in line");
        sb.AppendLine("        GameObject prefab = enemyPrefabs[numEnemySpawned];");
        sb.AppendLine("        int randomSpawnIndex = Random.Range(0, enemySpawnPoints.Length);");
        sb.AppendLine("        for (int i = 0; i < enemySpawnPoints.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (enemySpawnPoints[randomSpawnIndex].GetComponent<BT2DEnemySpawnPoint>().occupancy <= 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("            randomSpawnIndex = randomSpawnIndex >= enemySpawnPoints.Length - 1 ? 0 : randomSpawnIndex + 1;");
        sb.AppendLine("        }");
        sb.AppendLine("        numEnemySpawned++;");
        sb.AppendLine("        enemySpawnPoints[randomSpawnIndex].GetComponent<BT2DEnemySpawnPoint>().SpawnTank(prefab, numEnemySpawned);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateLife()");
        sb.AppendLine("    {");
        sb.AppendLine("        int lifeLeft = playerSpawnPoint.GetComponent<BT2DPlayerSpawnPoint>().totalLife;");
        sb.AppendLine("        lifeText.text = \"Life: \" + lifeLeft.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateScore()");
        sb.AppendLine("    {");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DHeadquartersScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1071);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DHeadquarters : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        // If hit by bullet from either side");
        sb.AppendLine("        if (collision.gameObject.CompareTag(\"BT2DEnemyBullet\") ||");
        sb.AppendLine("            collision.gameObject.CompareTag(\"BT2DPlayerBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // It is game over");
        sb.AppendLine("            BT2DGameManager.sharedInstance.GameOver();");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DHeadquarters", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DPlayerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(13611);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DPlayer : BT2DTank");
        sb.AppendLine("{");
        sb.AppendLine("    private BT2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private float nextX;");
        sb.AppendLine("    private float nextY;");
        sb.AppendLine("    private float hInput = 0.0f;");
        sb.AppendLine("    private float vInput = 0.0f;");
        sb.AppendLine("    // For Debugging");
        sb.AppendLine("    public bool vAllowedP;");
        sb.AppendLine("    public bool hAllowedP;");
        sb.AppendLine("");
        sb.AppendLine("    new protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        base.Start();");
        sb.AppendLine("        gameManager = BT2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (BT2DGameManager.gameStarted == false)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateControls();");
        sb.AppendLine("        // Always ease the tank to the closest round number coordinates");
        sb.AppendLine("        if (rb.velocity.SqrMagnitude() == 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            Vector2 position = transform.position;");
        sb.AppendLine("            if (position.x != nextX)");
        sb.AppendLine("            {");
        sb.AppendLine("                float x = EaseToValue(position.x, nextX, speed);");
        sb.AppendLine("                transform.position = new Vector2(x, transform.position.y);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (position.y != nextY)");
        sb.AppendLine("            {");
        sb.AppendLine("                float y = EaseToValue(position.y, nextY, speed);");
        sb.AppendLine("                transform.position = new Vector2(transform.position.x, y);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Read the input vector");
        sb.AppendLine("        Vector2 inputVector = input.Get<Vector2>();");
        sb.AppendLine("        hInput = inputVector.x;");
        sb.AppendLine("        vInput = inputVector.y;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnShoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Fire the tank gun");
        sb.AppendLine("        base.Shoot();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateControls()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Reset velocity to zero every update");
        sb.AppendLine("        Vector2 oldVelocity = rb.velocity;");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        Vector2 position = transform.position;");
        sb.AppendLine("        const float threshold = 0.2f;");
        sb.AppendLine("        if (Mathf.Abs(hInput) > 0.0f && vInput == 0.0f && IsOnGrid(position.y))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Horizontal movement");
        sb.AppendLine("            float multiplier = hInput > 0.0f ? 1.0f : -1.0f;");
        sb.AppendLine("            transform.up = multiplier * Vector2.right;");
        sb.AppendLine("            if (Mathf.Abs(hInput) > threshold && CheckDirectionClear(transform.up))");
        sb.AppendLine("            {");
        sb.AppendLine("                rb.velocity = new Vector2(multiplier * base.speed, 0.0f);");
        sb.AppendLine("                nextX = NextGridValue(position.x, rb.velocity.x);");
        sb.AppendLine("                nextY = Mathf.Round(position.y);");
        sb.AppendLine("                nextX = ConstrainX(nextX);");
        sb.AppendLine("                nextY = ConstrainY(nextY);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (Mathf.Abs(vInput) > 0.0f && hInput == 0.0f && IsOnGrid(position.x))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Vertical movement");
        sb.AppendLine("            float multiplier = vInput > 0.0f ? 1.0f : -1.0f;");
        sb.AppendLine("            transform.up = multiplier * Vector2.up;");
        sb.AppendLine("            if (Mathf.Abs(vInput) > threshold && CheckDirectionClear(transform.up))");
        sb.AppendLine("            {");
        sb.AppendLine("                rb.velocity = new Vector2(0.0f, multiplier * base.speed);");
        sb.AppendLine("                nextY = NextGridValue(position.y, rb.velocity.y);");
        sb.AppendLine("                nextX = Mathf.Round(position.x);");
        sb.AppendLine("                nextX = ConstrainX(nextX);");
        sb.AppendLine("                nextY = ConstrainY(nextY);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If hit by enemy tank or bullet");
        sb.AppendLine("        if (collidedObject.CompareTag(\"BT2DEnemy\") ||");
        sb.AppendLine("            collidedObject.CompareTag(\"BT2DEnemyBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Take damage");
        sb.AppendLine("            Hit();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnTriggerEnter2D(Collider2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If a power up has been triggered");
        sb.AppendLine("        if (collidedObject.CompareTag(\"BT2DPowerup\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Activate power up");
        sb.AppendLine("            gameManager.ActivatePowerup(collidedObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool CheckDirectionClear(Vector2 direction)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if the direction is clear of obstacles");
        sb.AppendLine("        const float probeDistance = 1.01f;");
        sb.AppendLine("        Vector2 point1 = Vector2.zero;");
        sb.AppendLine("        Vector2 point2 = Vector2.zero;");
        sb.AppendLine("        Vector2 myPos = transform.position;");
        sb.AppendLine("        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Probe in horizontal direction");
        sb.AppendLine("            point1.x = myPos.x + direction.x * probeDistance;");
        sb.AppendLine("            point1.y = myPos.y + 0.5f;");
        sb.AppendLine("            point2.x = point1.x;");
        sb.AppendLine("            point2.y = myPos.y - 0.5f;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Probe in vertical direction");
        sb.AppendLine("            point1.y = myPos.y + direction.y * probeDistance;");
        sb.AppendLine("            point1.x = myPos.x + 0.5f;");
        sb.AppendLine("            point2.y = point1.y;");
        sb.AppendLine("            point2.x = myPos.x - 0.5f;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (CheckTile(point1))");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (CheckTile(point2))");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool CheckTile(Vector2 point)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if the projected point is obstructed by a tile");
        sb.AppendLine("        if (gameManager.brickLayer.GetTile(gameManager.brickLayer.WorldToCell(point)) != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (gameManager.waterLayer.GetTile(gameManager.waterLayer.WorldToCell(point)) != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (gameManager.steelLayer.GetTile(gameManager.steelLayer.WorldToCell(point)) != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float ConstrainX(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value > gameManager.rightEdge)");
        sb.AppendLine("        {");
        sb.AppendLine("            return gameManager.rightEdge;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (value < gameManager.leftEdge)");
        sb.AppendLine("        {");
        sb.AppendLine("            return gameManager.leftEdge;");
        sb.AppendLine("        }");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float ConstrainY(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (value > gameManager.topEdge)");
        sb.AppendLine("        {");
        sb.AppendLine("            return gameManager.topEdge;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (value < gameManager.bottomEdge)");
        sb.AppendLine("        {");
        sb.AppendLine("            return gameManager.bottomEdge;");
        sb.AppendLine("        }");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float EaseToValue(float currentValue, float nextValue, float speed)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Ease the tank toward the next point");
        sb.AppendLine("        float d = nextValue - currentValue;");
        sb.AppendLine("        float rv;");
        sb.AppendLine("        if (d > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            rv = currentValue + Mathf.Abs(speed) * Time.deltaTime;");
        sb.AppendLine("            if (rv > nextValue)");
        sb.AppendLine("            {");
        sb.AppendLine("                rv = nextValue;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            rv = currentValue - Mathf.Abs(speed) * Time.deltaTime;");
        sb.AppendLine("            if (rv < nextValue)");
        sb.AppendLine("            {");
        sb.AppendLine("                rv = nextValue;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return rv;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void Hit()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Take damage");
        sb.AppendLine("        base.hitPoint--;");
        sb.AppendLine("        // If ran out of hit point");
        sb.AppendLine("        if (base.hitPoint <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Notify the player's tank destruction");
        sb.AppendLine("            gameManager.NotifyPlayerDestruction();");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(Vector2 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        transform.position = position;");
        sb.AppendLine("        nextX = position.x;");
        sb.AppendLine("        nextY = position.y;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool IsOnGrid(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if this tank's coordinates is near round numbers");
        sb.AppendLine("        const float threshold = 0.01f;");
        sb.AppendLine("        float d = value - Mathf.Round(value);");
        sb.AppendLine("");
        sb.AppendLine("        return d * d < threshold * threshold;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float NextGridValue(float currentValue, float velocity)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Project the next point on the grid");
        sb.AppendLine("        if (velocity > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return Mathf.Ceil(currentValue); ");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            return Mathf.Floor(currentValue);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DPlayer", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DPlayerSpawnPointScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1354);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DPlayerSpawnPoint : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject tankPrefab;");
        sb.AppendLine("    public int totalLife = 3;");
        sb.AppendLine("");
        sb.AppendLine("    public GameObject SpawnTank()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If there is life left");
        sb.AppendLine("        if (totalLife > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Spawn the tank");
        sb.AppendLine("            totalLife--;");
        sb.AppendLine("            GameObject newTank = Instantiate(tankPrefab);");
        sb.AppendLine("            newTank.name = tankPrefab.name;");
        sb.AppendLine("            newTank.transform.up = transform.up;");
        sb.AppendLine("            newTank.GetComponent<BT2DPlayer>().Initialize(transform.position);");
        sb.AppendLine("            return newTank;");
        sb.AppendLine("        }");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DPlayerSpawnPoint", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DPowerupSpawnPointScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1671);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DPowerupSpawnPoint : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject powerupPrefab;");
        sb.AppendLine("    public float range = 1.0f;");
        sb.AppendLine("    public int maxSpawned = 1;");
        sb.AppendLine("    private int numSpawned = 0;");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsExhausted()");
        sb.AppendLine("    {");
        sb.AppendLine("        return maxSpawned <= numSpawned;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Spawn()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (IsExhausted())");
        sb.AppendLine("        {");
        sb.AppendLine("            // Do not spawn if this point has been used before");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Spawn the power up");
        sb.AppendLine("        GameObject newObject = Instantiate(powerupPrefab);");
        sb.AppendLine("        float xOffset = Random.Range(-range, range);");
        sb.AppendLine("        float yOffset = Random.Range(-range, range);");
        sb.AppendLine("        Vector2 position = transform.position;");
        sb.AppendLine("        newObject.transform.position = new Vector2(position.x + xOffset, position.y + yOffset);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DPowerupSpawnPoint", scriptsPath, sb.ToString());
    }

    private static void WriteBT2DTankScriptToFile()
    {
        StringBuilder sb = new StringBuilder(2621);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class BT2DTank : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject bulletPrefab;");
        sb.AppendLine("    public int hitPoint = 1;");
        sb.AppendLine("    public float speed = 1.0f;");
        sb.AppendLine("    public float bulletSpeed = 1.0f;");
        sb.AppendLine("    public int value = 100;");
        sb.AppendLine("    private Transform gunTransform;");
        sb.AppendLine("    private BT2DBullet bullet;");
        sb.AppendLine("        ");
        sb.AppendLine("    protected void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Save the gun's transform");
        sb.AppendLine("        gunTransform = transform.GetChild(0);");
        sb.AppendLine("        // Instantiate and initialize bullet");
        sb.AppendLine("        bullet = Instantiate(bulletPrefab).GetComponent<BT2DBullet>();");
        sb.AppendLine("        bullet.name = bulletPrefab.name;");
        sb.AppendLine("        bullet.gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnDestroy()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (bullet && !bullet.isActiveAndEnabled)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Destroy bullet game object");
        sb.AppendLine("            Destroy(bullet.gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool IsReadyToFire()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Only a fixed number of bullet(s) can be active at one time");
        sb.AppendLine("        return !bullet.isActiveAndEnabled;");
        sb.AppendLine("    }");
        sb.AppendLine("     ");
        sb.AppendLine("    protected void Shoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!IsReadyToFire())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Fire the tank gun");
        sb.AppendLine("        Vector2 muzzlePosition = gunTransform.position;");
        sb.AppendLine("        // Create the bullet object");
        sb.AppendLine("        bullet.gameObject.SetActive(true);");
        sb.AppendLine("        bullet.Initialize(gameObject, muzzlePosition, gunTransform.up, bulletSpeed);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("BT2DTank", scriptsPath, sb.ToString());
    }
}
