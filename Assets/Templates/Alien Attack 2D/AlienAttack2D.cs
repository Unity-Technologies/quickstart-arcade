using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class AlienAttack2D : Editor
{
    private const string templateName = "AlienAttack2D";
    private const string templateSpacedName = "Alien Attack 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "AA2D";
    private const int textureSize = 32;
    // All paths are relative to 'Assets' folder.
    // Do not include 'Assets/' in the file paths!
    public static string templatePath = "Templates/" + templateSpacedName;
    public static string prefabsPath = templatePath + "/Prefabs";
    public static string scriptsPath = templatePath + "/Scripts";
    public static string scenesPath = templatePath + "/Scenes";
    public static string settingsPath = templatePath + "/Settings";
    public static string texturesPath = templatePath + "/Textures";
    private static string[] subFolders = { prefabsPath, scriptsPath, scenesPath, settingsPath, texturesPath };

    enum SpritePath
    {
        Player = 0,
        EnemyMinion,
        EnemyVeteran,
        EnemyElite,
        EnemyMothership,
        PlayerLaser,
        EnemyLaser,
        Brick,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];

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
        ScriptUtilities.RemoveTag(scriptPrefix + "Player");
        // Layers
        int playerLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Player");
        int enemyLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, false);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, false);
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
        // Generate UI
        GenerateUI();
        // Generate scripts
        GenerateScripts();
        // Refresh asset database
        AssetDatabase.Refresh();
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
        // Create main camera
        GameObject mainCameraObject = GameObject.Find("Main Camera");
        Camera camera = mainCameraObject.GetComponent<Camera>();
        camera.backgroundColor = new Color(25.0f / 255.0f, 25.0f / 255.0f, 112.0f / 255.0f);
        camera.orthographicSize = 18.0f;
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags
        ScriptUtilities.CreateTag(scriptPrefix + "Player");
        // Layers
        int playerLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Player");
        int enemyLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }

    private static void GenerateAssets()
    {
        GenerateInputActions();
        GenerateTextures();
        AssetDatabase.Refresh();
        GenerateMaterials();
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
        const float d = 1.0f / 255.0f;
        Color playerWhite = new Color(255.0f * d, 237.0f * d, 237.0f * d);
        Color minionColor = new Color(255.0f * d, 192.0f * d, 203.0f * d);
        Color veteranColor = new Color(228.0f * d, 114.0f * d, 191.0f * d);
        Color eliteColor = new Color(219.0f * d, 73.0f * d, 172.0f * d);
        Color mothershipRed = new Color(253.0f * d, 0.0f * d, 2.0f * d);
        Color brickGray = new Color(159.0f * d, 160.0f * d, 155.0f * d);

        // Create player texture
        path = ContentUtilities.CreateTexture2DTriangleAsset("player_texture", texturesPath, textureSize, 2*textureSize, playerWhite);
        spritePaths[(int)SpritePath.Player] = path;
        // Create blue enemy texture
        path = DrawEnemyTexture("enemy_blue_texture", minionColor);
        spritePaths[(int)SpritePath.EnemyMinion] = path;
        // Create yellow enemy texture
        path = DrawEnemyTexture("enemy_yellow_texture", veteranColor);
        spritePaths[(int)SpritePath.EnemyVeteran] = path;
        // Create orange enemy texture
        path = DrawEnemyTexture("enemy_orange_texture", eliteColor);
        spritePaths[(int)SpritePath.EnemyElite] = path;
        // Create red mothership texture
        path = ContentUtilities.CreateTexture2DOctagonAsset("enemy_red_texture", texturesPath, 2 * textureSize, 2 * textureSize, mothershipRed);
        spritePaths[(int)SpritePath.EnemyMothership] = path;
        // Create player laser
        path = ContentUtilities.CreateTexture2DRectangleAsset("player_laser_texture", texturesPath, textureSize / 4, textureSize / 2, Color.white);
        spritePaths[(int)SpritePath.PlayerLaser] = path;
        // Create enemy laser
        path = ContentUtilities.CreateTexture2DRectangleAsset("enemy_laser_texture", texturesPath, textureSize / 4, textureSize, Color.white);
        spritePaths[(int)SpritePath.EnemyLaser] = path;
        // Create brick
        path = ContentUtilities.CreateTexture2DRectangleAsset("brick_texture", texturesPath, textureSize / 2, textureSize / 2, brickGray);
        spritePaths[(int)SpritePath.Brick] = path;
    }

    private static void GenerateMaterials()
    {

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

    private static void GenerateObjects()
    {
        GameObject newObject;
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;

        // Create the Game Manager object
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create the Game Manager object
        newObject = new GameObject("AmmoDump");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create the Alien Hive object
        newObject = new GameObject("AlienHive");
        newObject.transform.position = new Vector2(0.0f, 24.0f);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create Player Respawn point
        newObject = new GameObject("PlayerRespawnPoint");
        newObject.transform.position = new Vector2(-19.0f, -13.0f);

        // Create Mothership spawn point
        newObject = new GameObject("MothershipSpawnPoint");
        newObject.transform.position = new Vector2(21.5f, 14.0f);

        // Create Brick Block
        CreateBrickBlock();

        // Create Enemies 
        CreateEnemyMothershipPrefab();
        CreateEnemyShipPrefab("EnemyBlue", spritePaths[(int)SpritePath.EnemyMinion]);
        CreateEnemyShipPrefab("EnemyYellow", spritePaths[(int)SpritePath.EnemyVeteran]);
        CreateEnemyShipPrefab("EnemyOrange", spritePaths[(int)SpritePath.EnemyElite]);
        CreateEnemyLaser();

        // Create Player
        CreatePlayerPrefab();
    }

    private static void GenerateUI()
    {
        // Create canvas game object and event system
        GameObject canvasObject = ContentUtilities.CreateUICanvas();
        Transform parent = canvasObject.transform;

        const float margin = 10.0f;
        const int fontSize = 24;
        float w = 180.0f;
        float h = 40.0f;

        // Create score text panel
        GameObject scoreTextPanel = ContentUtilities.CreateUIBackgroundObject("ScoreTextPanel", w, h);
        ContentUtilities.AnchorUIObject(scoreTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create score text
        GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w - margin, h, "Score: 999999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(scoreTextObject, scoreTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create life text panel
        float offsetY = -h;
        GameObject lifeTextPanel = ContentUtilities.CreateUIBackgroundObject("LifeTextPanel", w, h);
        ContentUtilities.AnchorUIObject(lifeTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create life text
        GameObject lifeTextObject = ContentUtilities.CreateUITextObject("LifeText", w - margin, h, "Life: 3", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(lifeTextObject, lifeTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create level text panel
        GameObject levelTextPanel = ContentUtilities.CreateUIBackgroundObject("LevelTextPanel", w, h);
        ContentUtilities.AnchorUIObject(levelTextPanel, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin));
        // Create level text
        GameObject levelTextObject = ContentUtilities.CreateUITextObject("LevelText", w - margin, h, "Level: 99 / 99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(levelTextObject, levelTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "You Won!", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create play button
        w = 160.0f;
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
        sb.AppendLine("Use Left / Right Arrows or W / D Keys to Move");
        sb.AppendLine("Press Space Bar or Left Click to Shoot");
        sb.AppendLine("Don't Get Hit By Enemy Shots!");
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
        WriteAA2DAmmoDumpScriptToFile();
        WriteAA2DBlinkTextScriptToFile();
        WriteAA2DBrickScriptToFile();
        WriteAA2DEnemyScriptToFile();
        WriteAA2DGameManagerScriptToFile();
        WriteAA2DHiveScriptToFile();
        WriteAA2DLaserScriptToFile();
        WriteAA2DMothershipScriptToFile();
        WriteAA2DPlayerScriptToFile();
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Brick", "Enemy", "GameManager", "Hive", "Laser", "Mothership", "Player" }))
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
        GameObject ammoDumpPrefab = ContentUtilities.LoadPrefab("AmmoDump", prefabsPath);
        GameObject playerPrefab = ContentUtilities.LoadPrefab("PlayerObject", prefabsPath);
        GameObject playerLaserPrefab = ContentUtilities.LoadPrefab("PlayerLaser", prefabsPath);
        GameObject alienHivePrefab = ContentUtilities.LoadPrefab("AlienHive", prefabsPath);
        GameObject mothershipPrefab = ContentUtilities.LoadPrefab("Mothership", prefabsPath);
        GameObject enemyBluePrefab = ContentUtilities.LoadPrefab("EnemyBlue", prefabsPath);
        GameObject enemyYellowPrefab = ContentUtilities.LoadPrefab("EnemyYellow", prefabsPath);
        GameObject enemyOrangePrefab = ContentUtilities.LoadPrefab("EnemyOrange", prefabsPath);
        GameObject enemyLaserPrefab = ContentUtilities.LoadPrefab("EnemyLaser", prefabsPath);
        GameObject brickPrefab = ContentUtilities.LoadPrefab("Brick", prefabsPath);
        GameObject brickBlockPrefab = ContentUtilities.LoadPrefab("BrickBlock", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "AmmoDump", ammoDumpPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Player", playerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Laser", playerLaserPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Hive", alienHivePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Mothership", mothershipPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", enemyBluePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", enemyYellowPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Enemy", enemyOrangePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Laser", enemyLaserPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Brick", brickPrefab);

        // Assign prefab references
        // Ammo dump
        string className = scriptPrefix + "AmmoDump";
        ScriptUtilities.AssignObjectFieldToObject(enemyLaserPrefab, ammoDumpPrefab, className, "enemyLaserPrefab");
        ScriptUtilities.AssignObjectFieldToObject(playerLaserPrefab, ammoDumpPrefab, className, "playerLaserPrefab");
        // Hive
        className = scriptPrefix + "Hive";
        GameObject[] enemies = { enemyOrangePrefab, enemyYellowPrefab, enemyYellowPrefab, enemyBluePrefab, enemyBluePrefab };
        ScriptUtilities.AssignObjectsFieldToObject(enemies, alienHivePrefab, className, "enemyLineUp");
        ScriptUtilities.AssignObjectFieldToObject(mothershipPrefab, alienHivePrefab, className, "mothershipPrefab");
        // Enemies
        AssignEnemyParameters(enemyBluePrefab, 10);
        AssignEnemyParameters(enemyYellowPrefab, 20);
        AssignEnemyParameters(enemyOrangePrefab, 30);
        // Lasers
        className = scriptPrefix + "Laser";
        AssignLaserParameters(playerLaserPrefab, new Vector2(0.0f, 1.0f), 20.0f);
        AssignLaserParameters(enemyLaserPrefab, new Vector2(0.0f, -1.0f), 10.0f);

        // Instantiate objects
        PrefabUtility.InstantiatePrefab(ammoDumpPrefab);
        InstantiateAndSetupHive(alienHivePrefab);
        InstantiateAndSetupPlayer(playerPrefab);
        InstantiateAndSetupGameManager(gameManagerPrefab);

        LayoutBrickBlock(brickBlockPrefab, -16.0f, -10.0f);
        LayoutBrickBlock(brickBlockPrefab, -8.0f, -10.0f);
        LayoutBrickBlock(brickBlockPrefab, -0.0f, -10.0f);
        LayoutBrickBlock(brickBlockPrefab, 8.0f, -10.0f);
        LayoutBrickBlock(brickBlockPrefab, 16.0f, -10.0f);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static void AssignEnemyParameters(GameObject enemyPrefab, int value)
    {
        string className = scriptPrefix + "Enemy";
        ScriptUtilities.AssignIntFieldToObject(value, enemyPrefab, className, "value");
    }

    private static void AssignLaserParameters(GameObject laserPrefab, Vector2 direction, float speed)
    {
        string className = scriptPrefix + "Laser";
        ScriptUtilities.AssignVector2DFieldToObject(direction, laserPrefab, className, "direction");
        ScriptUtilities.AssignFloatFieldToObject(speed, laserPrefab, className, "speed");
    }

    private static void CreateBrickBlock()
    {
        GameObject brickBlockObject = new GameObject("BrickBlock");
        Transform parent = brickBlockObject.transform;
        string brickPath = spritePaths[(int)SpritePath.Brick];
        GameObject brickObject = ContentUtilities.CreateTexturedBody("Brick", 0.0f, 0.0f, brickPath);
        brickObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        ContentUtilities.CreatePrefab(brickObject, prefabsPath);
        GameObject brickPrefab = ContentUtilities.LoadPrefab("Brick", prefabsPath);

        Sprite brickSprite = ContentUtilities.LoadSpriteAtPath(brickPath);
        float stepX = brickSprite.rect.width / (float)textureSize;
        float stepY = -brickSprite.rect.height / (float)textureSize;
        float startX = -3.5f * stepX;
        float startY = 0.5f * stepY;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                brickObject = PrefabUtility.InstantiatePrefab(brickPrefab) as GameObject;
                brickObject.transform.SetParent(parent);
                brickObject.transform.localPosition = new Vector2(startX + i * stepX, startY + j * stepY);
            }
        }

        ContentUtilities.CreatePrefab(brickBlockObject, prefabsPath);
    }

    private static void CreateEnemyLaser()
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody("EnemyLaser", 0.0f, 0.0f, spritePaths[(int)SpritePath.EnemyLaser]);
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Enemy");
        newObject.GetComponent<Rigidbody2D>().gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath);
    }

    private static void CreateEnemyMothershipPrefab()
    {
        string name = "MotherShip";
        string texturePath = spritePaths[(int)SpritePath.EnemyMothership];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Polygon;
        GameObject mothershipObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, texturePath, shape);
        mothershipObject.GetComponent<SpriteRenderer>().sortingOrder = 0;
        mothershipObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Enemy");
        Rigidbody2D rb = mothershipObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(mothershipObject, prefabsPath);
    }

    private static void CreateEnemyShipPrefab(string name, string texturePath)
    {
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Polygon;
        GameObject enemyObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, texturePath, shape);
        enemyObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        enemyObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Enemy");
        Rigidbody2D rb = enemyObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        rb.freezeRotation = true;
        ContentUtilities.CreatePrefab(enemyObject, prefabsPath);
    }

    private static void CreatePlayerPrefab()
    {
        // Create Player object
        string name = "PlayerObject";
        string texturePath = spritePaths[(int)SpritePath.Player];
        int layerIndex = ScriptUtilities.IndexOfLayer(scriptPrefix + "Player");
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Polygon;
        GameObject playerObject = ContentUtilities.CreateTexturedBody(name, 0.0f, -13.0f, texturePath, shape);
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        playerObject.AddComponent<PlayerInput>().actions = asset;
        playerObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Configure object
        playerObject.tag = scriptPrefix + "Player";
        playerObject.layer = layerIndex;
        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        rb.freezeRotation = true;
        ContentUtilities.CreatePrefab(playerObject, prefabsPath);
        // Create Laser
        GameObject laserObject = ContentUtilities.CreateTexturedBody("PlayerLaser", 0.0f, 0.0f, spritePaths[(int)SpritePath.PlayerLaser]);
        laserObject.tag = scriptPrefix + "Player";
        laserObject.layer = layerIndex;
        rb = laserObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(laserObject, prefabsPath);
    }

    private static string DrawEnemyTexture(string name, Color bodyColor)
    {
        // Set colors
        Color transparence = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        Color eyeColor = Color.black;
        Color toothColor = bodyColor;
        // Set sizes
        int bodyW = 2 * textureSize;
        int bodyH = textureSize;
        int eyeW = bodyW / 4;
        int eyeH = bodyH / 2;
        int combinedW = bodyW;
        int combinedH = bodyH * 3 / 2;
        // Create color arrays
        Color[] body = ContentUtilities.FillBitmapShapeRectangle(bodyW, bodyH, bodyColor);
        Color[] eye = ContentUtilities.FillBitmapShapeDiamond(eyeW, eyeH, eyeColor);
        Color[] combined = ContentUtilities.FillBitmapShapeRectangle(combinedW, combinedH, transparence);
        // Draw body
        combined = ContentUtilities.CopyBitmap(body, bodyW, bodyH, combined, combinedW, combinedH, new Vector2Int(0, (combinedH / 3) - 1));
        // Draw eye
        combined = ContentUtilities.CopyBitmap(eye, eyeW, eyeH, combined, bodyW, bodyH, new Vector2Int(combinedW / 2 - eyeW - eyeW / 2, combinedH * 2 / 3 - eyeH / 2));
        combined = ContentUtilities.CopyBitmap(eye, eyeW, eyeH, combined, bodyW, bodyH, new Vector2Int(combinedW / 2 - eyeW / 2, combinedH * 2 / 3 - 2));
        combined = ContentUtilities.CopyBitmap(eye, eyeW, eyeH, combined, bodyW, bodyH, new Vector2Int(combinedW / 2 + eyeW / 2, combinedH * 2 / 3 - eyeH / 2));
        // Generate a texture asset from the combined array
        string path = ContentUtilities.CreateBitmapAsset(name, combined, combinedW, combinedH, texturesPath);
        // Return a path for later access
        return path;
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject ammoDumpObject = GameObject.Find("AmmoDump");
        GameObject playerObject = GameObject.Find("PlayerObject");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject lifeTextObject = GameObject.Find("LifeText");
        GameObject levelTextObject = GameObject.Find("LevelText");
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(ammoDumpObject, scriptPrefix + "AmmoDump", go, className, "ammoDump");
        ScriptUtilities.AssignComponentFieldToObject(playerObject, scriptPrefix + "Player", go, className, "player");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(lifeTextObject, "Text", go, className, "lifeText");
        ScriptUtilities.AssignComponentFieldToObject(levelTextObject, "Text", go, className, "levelText");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
    }

    private static void InstantiateAndSetupHive(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "Hive";

        // Get objects
        GameObject mothershipSpawnPointObject = GameObject.Find("MothershipSpawnPoint");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(mothershipSpawnPointObject, go, className, "mothershipSpawnPoint");
    }

    private static void InstantiateAndSetupPlayer(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "Player";

        // Get objects
        GameObject spawnPointObject = GameObject.Find("PlayerRespawnPoint");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(spawnPointObject, go, className, "spawnPoint");
    }

    private static void LayoutBrickBlock(GameObject brickBlockPrefab, float x, float y)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(brickBlockPrefab) as GameObject;
        newObject.transform.position = new Vector2(x, y);
    }

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        // Note: the scripts and tilemaps must exist before this function is called
        ReverseEngineerScripts();
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DAmmoDump", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DBrick", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DEnemy", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DHive", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DLaser", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DMothership", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("AA2DPlayer", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void WriteAA2DAmmoDumpScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4042);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DAmmoDump : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject enemyLaserPrefab;");
        sb.AppendLine("    public GameObject playerLaserPrefab;");
        sb.AppendLine("    private List<GameObject> enemyLasers = new List<GameObject>(3);");
        sb.AppendLine("    private List<GameObject> playerLasers = new List<GameObject>(5);");
        sb.AppendLine("");
        sb.AppendLine("    public enum ObjectType");
        sb.AppendLine("    {");
        sb.AppendLine("        Undefined = 0,");
        sb.AppendLine("        EnemyLaser,");
        sb.AppendLine("        PlayerLaser,");
        sb.AppendLine("        Max");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    private GameObject GetPrefabForType(ObjectType type)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (type)");
        sb.AppendLine("        {");
        sb.AppendLine("            case ObjectType.EnemyLaser:");
        sb.AppendLine("                return enemyLaserPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.PlayerLaser:");
        sb.AppendLine("                return playerLaserPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                Debug.LogWarning(\"Unknown object pool type!\");");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private List<GameObject> GetListForType(ObjectType type)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (type)");
        sb.AppendLine("        {");
        sb.AppendLine("            case ObjectType.EnemyLaser:");
        sb.AppendLine("                return enemyLasers;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.PlayerLaser:");
        sb.AppendLine("                return playerLasers;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                Debug.LogWarning(\"Unknown object pool type!\");");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public GameObject RequestObject(ObjectType type)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject go = null;");
        sb.AppendLine("        // Set the correct list and prefab");
        sb.AppendLine("        List<GameObject> list = GetListForType(type);");
        sb.AppendLine("        GameObject prefab = GetPrefabForType(type);");
        sb.AppendLine("        // Search for inactive object in the list");
        sb.AppendLine("        for (int i = 0; i < list.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!list[i].activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                go = list[i];");
        sb.AppendLine("                go.SetActive(true);");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // If no object found");
        sb.AppendLine("        if (!go)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Instantiate a new one");
        sb.AppendLine("            go = Instantiate(prefab);");
        sb.AppendLine("            go.name = prefab.name;");
        sb.AppendLine("            go.transform.SetParent(transform);");
        sb.AppendLine("            list.Add(go);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return value");
        sb.AppendLine("        return go;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DAmmoDump", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("AA2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DBrickScriptToFile()
    {
        StringBuilder sb = new StringBuilder(627);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DBrick : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        Destroy(gameObject);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DBrick", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DEnemyScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3431);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DEnemy : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int value = 100;");
        sb.AppendLine("    private AA2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private const float speed = 12.0f;");
        sb.AppendLine("    private const float speedVariance = 0.05f;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = AA2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (collision.gameObject.CompareTag(\"AA2DPlayer\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.AddScore(value);");
        sb.AppendLine("        }");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RequestMove(Vector2 direction, float distance)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait for random seconds before moving -- to achieve an organic feel in the fleet movement");
        sb.AppendLine("        float actualSpeed = speed + Random.Range(speed - speedVariance, speed + speedVariance);");
        sb.AppendLine("        rb.velocity = actualSpeed * direction;");
        sb.AppendLine("        float moveTimer = distance / actualSpeed;");
        sb.AppendLine("        StartCoroutine(WaitToStopMove(moveTimer));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RequestShoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait for random seconds before shooting");
        sb.AppendLine("        float randomTime = Random.Range(0.0f, 0.5f);");
        sb.AppendLine("        StartCoroutine(WaitToShoot(randomTime));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Shoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Perform actual shooting");
        sb.AppendLine("        GameObject laserObject = gameManager.ammoDump.RequestObject(AA2DAmmoDump.ObjectType.EnemyLaser);");
        sb.AppendLine("        laserObject.name = gameObject.name + \"'s Laser\";");
        sb.AppendLine("        laserObject.transform.position = transform.position;");
        sb.AppendLine("        laserObject.transform.rotation = Quaternion.identity;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToStopMove(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToShoot(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        Shoot();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DEnemy", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(9642);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public AA2DAmmoDump ammoDump;");
        sb.AppendLine("    public AA2DPlayer player;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text lifeText;");
        sb.AppendLine("    public Text levelText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public int totalLives = 3;");
        sb.AppendLine("    public int maxLevels = 10;");
        sb.AppendLine("    private int currentLives;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    private int currentLevel = 1;");
        sb.AppendLine("    private bool gamePaused = false;");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static AA2DGameManager sharedInstance = null;");
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
        sb.AppendLine("        ResetGame();");
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
        sb.AppendLine("    public void AddScore(int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        currentScore += value;");
        sb.AppendLine("        UpdateScoreText();");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Game Over!\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void GameWon()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"You Won!\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return !gamePaused && gameStarted;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool NotifyFleetDestruction()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (currentLevel >= maxLevels)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameWon();");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        currentLevel++;        ");
        sb.AppendLine("        UpdateLevelText();");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyPlayerDestruction()");
        sb.AppendLine("    {");
        sb.AppendLine("        currentLives--;");
        sb.AppendLine("        UpdateLifeText();");
        sb.AppendLine("        if (currentLives <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            player.gameObject.SetActive(true);");
        sb.AppendLine("            player.Respawn();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        currentLives = totalLives;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        UpdateLevelText();");
        sb.AppendLine("        UpdateScoreText();");
        sb.AppendLine("        UpdateLifeText();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"AlienAttack2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("        player.enabled = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateLevelText()");
        sb.AppendLine("    {");
        sb.AppendLine("        levelText.text = \"Level: \" + currentLevel + \" / \" + maxLevels;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateLifeText()");
        sb.AppendLine("    {");
        sb.AppendLine("        lifeText.text = \"Lives: \" + currentLives;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateScoreText()");
        sb.AppendLine("    {");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DHiveScriptToFile()
    {
        StringBuilder sb = new StringBuilder(17862);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DHive : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject[] enemyLineUp = new GameObject[5];");
        sb.AppendLine("    public GameObject mothershipPrefab;");
        sb.AppendLine("    public GameObject mothershipSpawnPoint;");
        sb.AppendLine("    public int numColumns = 13;");
        sb.AppendLine("    public float landingZoneY = -9.0f;");
        sb.AppendLine("    public float mothershipDelay = 10.0f;");
        sb.AppendLine("    public float shootDelay = 2.5f;");
        sb.AppendLine("    private AA2DGameManager gameManager;");
        sb.AppendLine("    private List<List<GameObject>> fleetList;");
        sb.AppendLine("    private float decisionTimer = 1.0f;");
        sb.AppendLine("    private float mothershipTimer;");
        sb.AppendLine("    private int numActiveShips = 0;");
        sb.AppendLine("    private int totalShips = 0;");
        sb.AppendLine("    private MoveState moveState;");
        sb.AppendLine("    private float leftMostPosition;");
        sb.AppendLine("    private float rightMostPosition;");
        sb.AppendLine("    private float bottomMostPosition;");
        sb.AppendLine("    private float shootTimer;");
        sb.AppendLine("    public float hStep = 2.0f;");
        sb.AppendLine("    public float vStep = 2.0f;");
        sb.AppendLine("    public float stepDelay = 3.0f;");
        sb.AppendLine("    public float rightLimit = 24.0f;");
        sb.AppendLine("    public float leftLimit = -24.0f;");
        sb.AppendLine("");
        sb.AppendLine("    private enum MoveState");
        sb.AppendLine("    {");
        sb.AppendLine("        Entrance = 0,");
        sb.AppendLine("        Left,");
        sb.AppendLine("        Right,");
        sb.AppendLine("        Down,");
        sb.AppendLine("        None,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = AA2DGameManager.sharedInstance;");
        sb.AppendLine("        SpawnFleet();");
        sb.AppendLine("        decisionTimer = 1.0f;");
        sb.AppendLine("        shootTimer = shootDelay;");
        sb.AppendLine("        mothershipTimer = mothershipDelay;");
        sb.AppendLine("        moveState = MoveState.None;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // This method represents the Hive Mind AI and control the movement, spawning, and shooting behaviors");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateParameters();");
        sb.AppendLine("        if (CheckVictoryCondition())");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.GameOver();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateMothership();");
        sb.AppendLine("        UpdateFleetShooting();");
        sb.AppendLine("        decisionTimer -= Time.deltaTime;");
        sb.AppendLine("        if (decisionTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (UpdateFleetRespawning())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateFleetMovement();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float CalculateStepDelay()");
        sb.AppendLine("    {");
        sb.AppendLine("        return stepDelay * ((float)(numActiveShips) / totalShips);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool CheckVictoryCondition()");
        sb.AppendLine("    {");
        sb.AppendLine("        return bottomMostPosition <= landingZoneY;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void DestroyAll()");
        sb.AppendLine("    {");
        sb.AppendLine("        for (int i = 0; i < fleetList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = fleetList[i];");
        sb.AppendLine("            for (int j = 0; j < column.Count; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                GameObject ship = column[j];");
        sb.AppendLine("                if (ship.activeInHierarchy)");
        sb.AppendLine("                {");
        sb.AppendLine("                    ship.SetActive(false);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterMoveStateEntrance()");
        sb.AppendLine("    {");
        sb.AppendLine("        moveState = MoveState.Entrance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterMoveStateLeft()");
        sb.AppendLine("    {");
        sb.AppendLine("        moveState = MoveState.Left;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterMoveStateRight()");
        sb.AppendLine("    {");
        sb.AppendLine("        moveState = MoveState.Right;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnterMoveStateDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        moveState = MoveState.Down;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteMoveStateEntrance()");
        sb.AppendLine("    {");
        sb.AppendLine("        MoveFleet(Vector2.down, hStep * 7.0f);");
        sb.AppendLine("        decisionTimer = CalculateStepDelay();");
        sb.AppendLine("        EnterMoveStateLeft();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteMoveStateLeft()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (leftMostPosition <= leftLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterMoveStateDown();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            MoveFleet(Vector2.left, hStep);");
        sb.AppendLine("            decisionTimer = CalculateStepDelay();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteMoveStateRight()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (rightMostPosition >= rightLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterMoveStateDown();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            MoveFleet(Vector2.right, hStep);");
        sb.AppendLine("            decisionTimer = CalculateStepDelay();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ExecuteMoveStateDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        MoveFleet(Vector2.down, hStep);");
        sb.AppendLine("        decisionTimer = CalculateStepDelay();");
        sb.AppendLine("        if (rightMostPosition >= rightLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterMoveStateLeft();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            EnterMoveStateRight();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void MoveFleet(Vector2 direction, float distance)");
        sb.AppendLine("    {");
        sb.AppendLine("        for (int i = 0; i < fleetList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = fleetList[i];");
        sb.AppendLine("            for (int j = 0; j < column.Count; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                GameObject ship = column[j];");
        sb.AppendLine("                if (!ship.activeInHierarchy)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                ship.GetComponent<AA2DEnemy>().RequestMove(direction, distance);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void PositionShip(GameObject ship, int column, int row)");
        sb.AppendLine("    {");
        sb.AppendLine("        float offsetX = 3.0f;");
        sb.AppendLine("        float offsetY = 2.0f;");
        sb.AppendLine("        float startX = transform.position.x + 0.5f + (-offsetX * (float)numColumns / 2.0f);");
        sb.AppendLine("        float startY = transform.position.y + (offsetY * 4.0f / 2.0f);");
        sb.AppendLine("");
        sb.AppendLine("        ship.transform.position = new Vector2(startX + (float)column * offsetX, startY - (float)row * offsetY);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void RespawnFleet()");
        sb.AppendLine("    {");
        sb.AppendLine("        // For each ship");
        sb.AppendLine("        for (int i = 0; i < fleetList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = fleetList[i];");
        sb.AppendLine("            for (int j = 0; j < column.Count; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Set active");
        sb.AppendLine("                GameObject ship = column[j];");
        sb.AppendLine("                ship.SetActive(true);");
        sb.AppendLine("                PositionShip(ship, i, j);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnFleet()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Spawn Ship initially");
        sb.AppendLine("        fleetList = new List<List<GameObject>>();");
        sb.AppendLine("        totalShips = 0;");
        sb.AppendLine("        for (int i = 0; i < numColumns; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = new List<GameObject>();");
        sb.AppendLine("            for (int j = 0; j < enemyLineUp.Length; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                GameObject ship = Instantiate(enemyLineUp[j]);");
        sb.AppendLine("                ship.name = \"Enemy (\" + i + \", \" + j + \")\";");
        sb.AppendLine("                column.Add(ship);");
        sb.AppendLine("                PositionShip(ship, i, j);");
        sb.AppendLine("                totalShips++;");
        sb.AppendLine("            }");
        sb.AppendLine("            fleetList.Add(column);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateFleetMovement()");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (moveState)");
        sb.AppendLine("        {");
        sb.AppendLine("            case MoveState.Entrance:");
        sb.AppendLine("                ExecuteMoveStateEntrance();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case MoveState.Left:");
        sb.AppendLine("                ExecuteMoveStateLeft();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case MoveState.Right:");
        sb.AppendLine("                ExecuteMoveStateRight();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case MoveState.Down:");
        sb.AppendLine("                ExecuteMoveStateDown();");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                EnterMoveStateEntrance();");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool UpdateFleetRespawning()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (numActiveShips > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (gameManager.NotifyFleetDestruction())");
        sb.AppendLine("        {");
        sb.AppendLine("            RespawnFleet();");
        sb.AppendLine("            StartCoroutine(WaitToActivateFleet());");
        sb.AppendLine("        }");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateFleetShooting()");
        sb.AppendLine("    {");
        sb.AppendLine("        shootTimer -= Time.deltaTime;");
        sb.AppendLine("        if (shootTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        shootTimer = shootDelay;");
        sb.AppendLine("");
        sb.AppendLine("        // Find ships at the front line");
        sb.AppendLine("        GameObject[] frontShips = new GameObject[fleetList.Count];");
        sb.AppendLine("        int numShips = 0;");
        sb.AppendLine("        for (int i = 0; i < fleetList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = fleetList[i];");
        sb.AppendLine("            for (int j = column.Count - 1; j >= 0; j--)");
        sb.AppendLine("            {");
        sb.AppendLine("                GameObject ship = column[j];");
        sb.AppendLine("                if (ship.activeInHierarchy)");
        sb.AppendLine("                {");
        sb.AppendLine("                    frontShips[numShips] = ship;");
        sb.AppendLine("                    numShips++;");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Up to 3 of the frontline ships, selected randonly, can shoot,");
        sb.AppendLine("        const int maxShots = 3;");
        sb.AppendLine("        int shotsFired = 0;");
        sb.AppendLine("        for (int i = 0; i < numShips; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            int randomInteger = Random.Range(0, numShips);");
        sb.AppendLine("            GameObject ship = frontShips[randomInteger];");
        sb.AppendLine("            if (ship != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                ship.GetComponent<AA2DEnemy>().RequestShoot();");
        sb.AppendLine("                frontShips[randomInteger] = null;");
        sb.AppendLine("                shotsFired++;");
        sb.AppendLine("                if (shotsFired >= maxShots)");
        sb.AppendLine("                {");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateMothership()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (mothershipTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            mothershipTimer -= Time.deltaTime;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        mothershipTimer = mothershipDelay;");
        sb.AppendLine("        GameObject mothership = Instantiate(mothershipPrefab);");
        sb.AppendLine("        mothership.GetComponent<AA2DMothership>().Initialize(mothershipSpawnPoint.transform.position);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateParameters()");
        sb.AppendLine("    {");
        sb.AppendLine("        // This method is used to define the fleet's current state and position as a group");
        sb.AppendLine("        numActiveShips = 0;");
        sb.AppendLine("        leftMostPosition = float.MaxValue;");
        sb.AppendLine("        rightMostPosition = -float.MaxValue;");
        sb.AppendLine("        bottomMostPosition = float.MaxValue;");
        sb.AppendLine("        for (int i = 0; i < fleetList.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            List<GameObject> column = fleetList[i];");
        sb.AppendLine("            for (int j = 0; j < column.Count; j++)");
        sb.AppendLine("            {");
        sb.AppendLine("                GameObject ship = column[j];");
        sb.AppendLine("                if (ship.activeInHierarchy)");
        sb.AppendLine("                {");
        sb.AppendLine("                    numActiveShips++;");
        sb.AppendLine("                    Vector2 shipPosition = ship.transform.position;");
        sb.AppendLine("                    if (shipPosition.x > rightMostPosition)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        rightMostPosition = shipPosition.x;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    if (shipPosition.x < leftMostPosition)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        leftMostPosition = shipPosition.x;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    if (shipPosition.y < bottomMostPosition)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        bottomMostPosition = shipPosition.y;");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    IEnumerator WaitToActivateFleet()");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(2.5f);");
        sb.AppendLine("        UpdateParameters();");
        sb.AppendLine("        EnterMoveStateEntrance();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DHive", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DLaserScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1488);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DLaser : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Vector2 direction = Vector2.down;");
        sb.AppendLine("    public float speed = 15.0f;");
        sb.AppendLine("    Rigidbody2D rb;");
        sb.AppendLine("    SpriteRenderer spriteRenderer;");
        sb.AppendLine("");
        sb.AppendLine("    private void OnEnable()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        rb.velocity = direction * speed;");
        sb.AppendLine("        spriteRenderer = GetComponent<SpriteRenderer>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!spriteRenderer.isVisible)");
        sb.AppendLine("        {");
        sb.AppendLine("            gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DLaser", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DMothershipScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1778);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DMothership : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int value = 150;");
        sb.AppendLine("    public float speed = 7.0f;");
        sb.AppendLine("    private AA2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private float limitX;");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(Vector2 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Simple left to right movement");
        sb.AppendLine("        gameManager = AA2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        rb.velocity = Vector2.left * speed;");
        sb.AppendLine("        transform.position = position;");
        sb.AppendLine("        limitX = position.x;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (transform.position.x <= -limitX)");
        sb.AppendLine("        {");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.AddScore(value);");
        sb.AppendLine("        Destroy(gameObject);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DMothership", scriptsPath, sb.ToString());
    }

    private static void WriteAA2DPlayerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4709);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class AA2DPlayer : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject spawnPoint;");
        sb.AppendLine("    public float speed = 5.0f;");
        sb.AppendLine("    public float fireDelay = 0.33f;");
        sb.AppendLine("    public float leftLimit = -25.0f;");
        sb.AppendLine("    public float rightLimit = 25.0f;");
        sb.AppendLine("    private AA2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;    ");
        sb.AppendLine("    private float fireTimer;");
        sb.AppendLine("    private bool triggerPulled = false;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = AA2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (fireTimer > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            fireTimer -= Time.deltaTime;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (triggerPulled)");
        sb.AppendLine("        {");
        sb.AppendLine("            triggerPulled = false;");
        sb.AppendLine("            Shoot();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector2 myPos = transform.position;");
        sb.AppendLine("        if (myPos.x > rightLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.x = rightLimit;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (myPos.x < leftLimit)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.x = leftLimit;");
        sb.AppendLine("        }");
        sb.AppendLine("        transform.position = myPos;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("        gameManager.NotifyPlayerDestruction();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        float hInput = input.Get<Vector2>().x;");
        sb.AppendLine("        if (Mathf.Abs(hInput) > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = hInput * Vector2.right * speed;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = Vector2.zero;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnShoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        triggerPulled = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Respawn() ");
        sb.AppendLine("    {");
        sb.AppendLine("        transform.position = spawnPoint.transform.position;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Shoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        fireTimer = fireDelay;");
        sb.AppendLine("        Vector2 offset = new Vector2(0.0f, 1.5f);");
        sb.AppendLine("        GameObject laserObject = gameManager.ammoDump.RequestObject(AA2DAmmoDump.ObjectType.PlayerLaser);");
        sb.AppendLine("        laserObject.transform.position = (Vector2)transform.position + offset;");
        sb.AppendLine("        laserObject.transform.rotation = Quaternion.identity;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("AA2DPlayer", scriptsPath, sb.ToString());
    }
}
