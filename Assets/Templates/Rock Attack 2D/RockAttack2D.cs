using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class RockAttack2D : Editor
{
    const string templateName = "RockAttack2D";
    const string templateSpacedName = "Rock Attack 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "RA2D";
    private const int textureWidth = 128;
    private const int textureHeight = 128;
    // All paths are relative to 'Assets' folder.
    // Do not include 'Assets/' in the file paths!
    public static string templatePath = "Templates/" + templateSpacedName;
    public static string prefabsPath = templatePath + "/Prefabs";
    public static string scriptsPath = templatePath + "/Scripts";
    public static string scenesPath = templatePath + "/Scenes";
    public static string settingsPath = templatePath + "/Settings";
    public static string texturesPath = templatePath + "/Textures";
    private static string[] subFolders = { prefabsPath, scriptsPath, scenesPath, settingsPath, texturesPath };

    private enum SpritePath
    {
        PlayerShip = 0,
        PlayerBullet,
        EnemyUFO,
        EnemyBullet,
        RockLarge,
        RockMedium,
        RockSmall,
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
        ScriptUtilities.RemoveTag(scriptPrefix + "PlayerShip");
        ScriptUtilities.RemoveTag(scriptPrefix + "PlayerBullet");
        ScriptUtilities.RemoveTag(scriptPrefix + "Enemy");
        int playerLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Player");
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, false);
        int rocksLayer = ScriptUtilities.RemoveLayer(scriptPrefix + "Rocks");
        Physics2D.IgnoreLayerCollision(rocksLayer, rocksLayer, false);
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
        GameObject mainCamera = GameObject.Find("Main Camera");
        mainCamera.GetComponent<Camera>().backgroundColor = Color.black;
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Create tags
        ScriptUtilities.CreateTag(scriptPrefix + "PlayerShip");
        ScriptUtilities.CreateTag(scriptPrefix + "PlayerBullet");
        ScriptUtilities.CreateTag(scriptPrefix + "Enemy");
        // Create Player Layer
        int playerLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Player");
        Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
        // Create Rock Layer 
        int rocksLayer = ScriptUtilities.CreateLayer(scriptPrefix + "Rocks");
        Physics2D.IgnoreLayerCollision(rocksLayer, rocksLayer, true);
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

        // Create player ship texture
        path = ContentUtilities.CreateTexture2DTriangleAsset("player_ship_texture", texturesPath, textureWidth/2, textureHeight/2, Color.yellow);
        spritePaths[(int)SpritePath.PlayerShip] = path;
        // Create player bullet texture
        path = ContentUtilities.CreateTexture2DCircleAsset("player_bullet_texture", texturesPath, textureWidth / 8, textureHeight / 8, Color.yellow);
        spritePaths[(int)SpritePath.PlayerBullet] = path;

        // Create enemy UFO texture
        path = ContentUtilities.CreateTexture2DCircleAsset("enemy_ufo_texture", texturesPath, textureWidth / 2, textureHeight / 2, Color.red);
        spritePaths[(int)SpritePath.EnemyUFO] = path;
        // Create enemy bullet texture
        path = ContentUtilities.CreateTexture2DCircleAsset("enemy_bullet_texture", texturesPath, textureWidth / 8, textureHeight / 8, Color.red);
        spritePaths[(int)SpritePath.EnemyBullet] = path;

        // Create big rock texture
        path = ContentUtilities.CreateTexture2DOctagonAsset("rock_large_texture", texturesPath, textureWidth/2*3, textureHeight/2*3, Color.grey);
        spritePaths[(int)SpritePath.RockLarge] = path;
        // Create medium rock texture
        path = ContentUtilities.CreateTexture2DOctagonAsset("rock_medium_texture", texturesPath, textureWidth, textureHeight, Color.grey);
        spritePaths[(int)SpritePath.RockMedium] = path;
        // Create small rock texture
        path = ContentUtilities.CreateTexture2DOctagonAsset("rock_small_texture", texturesPath, textureWidth/2, textureHeight/2, Color.grey);
        spritePaths[(int)SpritePath.RockSmall] = path;
    }

    private static void PostProcessTextures()
    {
        float PPU = textureWidth;
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

        // Create an invisible object to hold script
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create an object pool game object
        newObject = new GameObject("ObjectsPool");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create player ship
        int playerLayer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Player");
        newObject = ContentUtilities.CreateTexturedBody("PlayerShip", 0.0f, 0.0f, spritePaths[(int)SpritePath.PlayerShip], ContentUtilities.ColliderShape.Polygon);
        newObject.tag = scriptPrefix + "PlayerShip";
        newObject.layer = playerLayer;
        newObject.GetComponent<Rigidbody2D>().gravityScale = 0.0f;
        // Add input
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create player ship clone
        CreatePlayerShipClonePrefab();
        // Create player bullet
        newObject = ContentUtilities.CreateTexturedBody("PlayerBullet", 0.0f, 0.0f, spritePaths[(int)SpritePath.PlayerBullet], ContentUtilities.ColliderShape.Circle);
        newObject.tag = scriptPrefix + "PlayerBullet";
        newObject.layer = playerLayer;
        newObject.GetComponent<Rigidbody2D>().gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Create enemy UFO and clone
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Circle;
        CreateEnemyPrefab("EnemyUFO", spritePaths[(int)SpritePath.EnemyUFO], shape);
        CreateEnemyPrefab("EnemyUFOClone", spritePaths[(int)SpritePath.EnemyUFO], shape);
        // Create enemy bullet
        CreateEnemyPrefab("EnemyBullet", spritePaths[(int)SpritePath.EnemyBullet], shape);
        // Create rocks
        shape = ContentUtilities.ColliderShape.Polygon;
        CreateEnemyPrefab("RockLarge", spritePaths[(int)SpritePath.RockLarge], shape);
        CreateEnemyPrefab("RockLargeClone", spritePaths[(int)SpritePath.RockLarge], shape);
        CreateEnemyPrefab("RockMedium", spritePaths[(int)SpritePath.RockMedium], shape);
        CreateEnemyPrefab("RockMediumClone", spritePaths[(int)SpritePath.RockMedium], shape);
        CreateEnemyPrefab("RockSmall", spritePaths[(int)SpritePath.RockSmall], shape);
        CreateEnemyPrefab("RockSmallClone", spritePaths[(int)SpritePath.RockSmall], shape);
    }

    private static void GenerateUI()
    {
        // Create canvas game object and event system
        GameObject canvasObject = ContentUtilities.CreateUICanvas();
        Transform parent = canvasObject.transform;

        // Create score text panel
        const float margin = 10.0f;
        const int fontSize = 24;
        float w = 150.0f;
        float h = 40.0f;
        GameObject scoreTextPanel = ContentUtilities.CreateUIBackgroundObject("ScoreTextPanel", w, h);
        ContentUtilities.AnchorUIObject(scoreTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create score text
        w = 150.0f;
        h = 40.0f;
        GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w - margin, h, "Score: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(scoreTextObject, scoreTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

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
        sb.AppendLine("WASD or Arrow Keys to Control Your Ship");
        sb.AppendLine("Space Bar to Shoot");
        sb.AppendLine("Don't get hit by Rocks or Enemy Lasers!");
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
        WriteRA2DRockScriptToFile();
        WriteRA2DBlinkTextScriptToFile();
        WriteRA2DEnemyBulletScriptToFile();
        WriteRA2DEnemyUFOScriptToFile();
        WriteRA2DGameManagerScriptToFile();
        WriteRA2DObjectsPoolScriptToFile();
        WriteRA2DPlayerBulletScriptToFile();
        WriteRA2DPlayerShipScriptToFile();
        WriteRA2DSpaceBodyScriptToFile();
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Rock", "EnemyBullet", "EnemyUFO", "GameManager", "PlayerBullet", "PlayerShip" }))
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
        GameObject objectsPoolPrefab = ContentUtilities.LoadPrefab("ObjectsPool", prefabsPath);
        GameObject rockLargePrefab = ContentUtilities.LoadPrefab("RockLarge", prefabsPath);
        GameObject rockMediumPrefab = ContentUtilities.LoadPrefab("RockMedium", prefabsPath);
        GameObject rockSmallPrefab = ContentUtilities.LoadPrefab("RockSmall", prefabsPath);
        GameObject rockLargeClonePrefab = ContentUtilities.LoadPrefab("RockLargeClone", prefabsPath);
        GameObject rockMediumClonePrefab = ContentUtilities.LoadPrefab("RockMediumClone", prefabsPath);
        GameObject rockSmallClonePrefab = ContentUtilities.LoadPrefab("RockSmallClone", prefabsPath);
        GameObject enemyBulletPrefab = ContentUtilities.LoadPrefab("EnemyBullet", prefabsPath);
        GameObject enemyUFOPrefab = ContentUtilities.LoadPrefab("EnemyUFO", prefabsPath);
        GameObject enemyUFOClonePrefab = ContentUtilities.LoadPrefab("EnemyUFOClone", prefabsPath);
        GameObject playerBulletPrefab = ContentUtilities.LoadPrefab("PlayerBullet", prefabsPath);
        GameObject playerShipPrefab = ContentUtilities.LoadPrefab("PlayerShip", prefabsPath);
        GameObject playerShipClonePrefab = ContentUtilities.LoadPrefab("PlayerShipClone", prefabsPath);
        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "ObjectsPool", objectsPoolPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Rock", rockLargePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Rock", rockMediumPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Rock", rockSmallPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "EnemyBullet", enemyBulletPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "EnemyUFO", enemyUFOPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PlayerBullet", playerBulletPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "PlayerShip", playerShipPrefab);

        // Assign prefab references
        string className;
        className = scriptPrefix + "GameManager";
        ScriptUtilities.AssignObjectFieldToObject(playerShipPrefab, gameManagerPrefab, className, "playerShipPrefab");
        ScriptUtilities.AssignObjectFieldToObject(enemyUFOPrefab, gameManagerPrefab, className, "UFOPrefab");
        // Assign clone for player ship
        ScriptUtilities.AssignObjectFieldToObject(playerShipClonePrefab, playerShipPrefab, scriptPrefix + "PlayerShip", "clonePrefab");
        // Assign clone for UFO
        ScriptUtilities.AssignObjectFieldToObject(enemyUFOClonePrefab, enemyUFOPrefab, scriptPrefix + "EnemyUFO", "clonePrefab");
        // Assign fields for rocks
        const int mediumSpawn = 2;
        const int smallSpawn = 1;
        const int undefined = 0;
        AssignFieldsForRock(mediumSpawn, rockLargeClonePrefab, rockLargePrefab);
        AssignFieldsForRock(smallSpawn, rockMediumClonePrefab, rockMediumPrefab);
        AssignFieldsForRock(undefined, rockSmallClonePrefab, rockSmallPrefab);

        // Assign fields to objects pool
        className = scriptPrefix + "ObjectsPool";
        ScriptUtilities.AssignObjectFieldToObject(rockSmallPrefab, objectsPoolPrefab, className, "smallRockPrefab");
        ScriptUtilities.AssignObjectFieldToObject(rockMediumPrefab, objectsPoolPrefab, className, "mediumRockPrefab");
        ScriptUtilities.AssignObjectFieldToObject(rockLargePrefab, objectsPoolPrefab, className, "largeRockPrefab");
        ScriptUtilities.AssignObjectFieldToObject(enemyBulletPrefab, objectsPoolPrefab, className, "enemyBulletPrefab");
        ScriptUtilities.AssignObjectFieldToObject(playerBulletPrefab, objectsPoolPrefab, className, "playerBulletPrefab");

        // Instantiate a copy of GameManager
        PrefabUtility.InstantiatePrefab(objectsPoolPrefab);
        InstantiateAndSetupGameManager(gameManagerPrefab);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static void AssignFieldsForRock(int spawnType, GameObject clonePrefab, GameObject rockPrefab)
    {
        string className = scriptPrefix + "Rock";

        ScriptUtilities.AssignIntFieldToObject(spawnType, rockPrefab, className, "spawnType");
        if (clonePrefab != null)
        {
            ScriptUtilities.AssignObjectFieldToObject(clonePrefab, rockPrefab, className, "clonePrefab");
        }
    }

    private static void CreateEnemyPrefab(string name, string spritePath, ContentUtilities.ColliderShape shape)
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, spritePath, shape);
        newObject.tag = scriptPrefix + "Enemy";
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Rocks");
        newObject.GetComponent<Rigidbody2D>().gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void CreatePlayerShipClonePrefab()
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody("PlayerShipClone", 0.0f, 0.0f, spritePaths[(int)SpritePath.PlayerShip], ContentUtilities.ColliderShape.Polygon);
        newObject.tag = scriptPrefix + "PlayerShip";
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Player");
        newObject.GetComponent<Rigidbody2D>().gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        // Instantiate an object from its prefab
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        // Fields assignment for the instantiated object
        GameObject objectsPoolObject = GameObject.Find("ObjectsPool");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        string className = scriptPrefix + "GameManager";
        ScriptUtilities.AssignComponentFieldToObject(objectsPoolObject, scriptPrefix + "ObjectsPool", go, className, "objectsPool");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
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
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DRock", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DEnemyBullet", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DEnemyUFO", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DObjectsPool", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DPlayerBullet", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DPlayerShip", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RA2DSpaceBody", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void WriteRA2DRockScriptToFile()
    {
        StringBuilder sb = new StringBuilder(5744);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DRock : RA2DSpaceBody");
        sb.AppendLine("{");
        sb.AppendLine("    public RA2DObjectsPool.ObjectType spawnType;");
        sb.AppendLine("    public float maxThrust = 60.0f;");
        sb.AppendLine("    public int scoreValue = 10;");
        sb.AppendLine("    private float minAngularSpeed = -20.0f;");
        sb.AppendLine("    private float maxAngularSpeed = 20.0f;");
        sb.AppendLine("    private RA2DGameManager gameManager;");
        sb.AppendLine("    ");
        sb.AppendLine("    public void Initialize()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Estimate radius");
        sb.AppendLine("        radius = EstimateRadius();");
        sb.AppendLine("        AddClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnEnable()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("        gameManager.NumRocks++;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.UpdateObjectWrapping(gameObject, radius);");
        sb.AppendLine("        UpdateClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Hit()");
        sb.AppendLine("    {");
        sb.AppendLine("        SpawnChildren();");
        sb.AppendLine("        gameManager.NumRocks--;");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (collision.gameObject.CompareTag(\"RA2DPlayerBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.AddScore(scoreValue);");
        sb.AppendLine("            collision.gameObject.GetComponent<RA2DPlayerBullet>().DeactivateNow();");
        sb.AppendLine("            Hit();");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (collision.gameObject.CompareTag(\"RA2DPlayerShip\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            Destroy(collision.gameObject);");
        sb.AppendLine("            gameManager.GameLost();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float EstimateRadius()");
        sb.AppendLine("    {");
        sb.AppendLine("        PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();");
        sb.AppendLine("        float maxHalfWidth = float.MinValue;");
        sb.AppendLine("        float maxHalfHeight = float.MinValue;");
        sb.AppendLine("        // Find extreme values from the polygon's points");
        sb.AppendLine("        foreach (Vector2 point in collider.points)");
        sb.AppendLine("        {");
        sb.AppendLine("            float hw = Mathf.Abs(point.x);");
        sb.AppendLine("            float hh = Mathf.Abs(point.y);");
        sb.AppendLine("            if (maxHalfWidth < hw)");
        sb.AppendLine("            {");
        sb.AppendLine("                maxHalfWidth = hw;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (maxHalfHeight < hh)");
        sb.AppendLine("            {");
        sb.AppendLine("                maxHalfHeight = hh;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return the bigger value");
        sb.AppendLine("        return maxHalfWidth > maxHalfHeight ? maxHalfWidth : maxHalfHeight;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RandomForce()");
        sb.AppendLine("    {");
        sb.AppendLine("        float thrustX = Random.Range(-maxThrust, maxThrust);");
        sb.AppendLine("        float thrustY = Random.Range(-maxThrust, maxThrust);");
        sb.AppendLine("        GetComponent<Rigidbody2D>().AddForce(new Vector2(thrustX, thrustY));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RandomTorque()");
        sb.AppendLine("    {");
        sb.AppendLine("        GetComponent<Rigidbody2D>().AddTorque(Random.Range(minAngularSpeed, maxAngularSpeed));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnChildren()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Do not spawn children if the prefab is null");
        sb.AppendLine("        if (spawnType == RA2DObjectsPool.ObjectType.Undefined)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Spawn random number of children");
        sb.AppendLine("        int numChildren = UnityEngine.Random.Range(2, 3);");
        sb.AppendLine("        for (int i = 0; i < numChildren; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            float angle = UnityEngine.Random.Range(0.0f, 360.0f);");
        sb.AppendLine("            GameObject child = gameManager.objectsPool.RequestObject(spawnType);");
        sb.AppendLine("            child.transform.position = transform.position;");
        sb.AppendLine("            child.transform.rotation = Quaternion.identity;");
        sb.AppendLine("            RA2DRock component = child.GetComponent<RA2DRock>();");
        sb.AppendLine("            component.Initialize();");
        sb.AppendLine("            component.RandomForce();");
        sb.AppendLine("            component.RandomTorque();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DRock", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("RA2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DEnemyBulletScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3009);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DEnemyBullet : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float missileForce = 100.0f;");
        sb.AppendLine("    public float lifeSpan = 15.0f;");
        sb.AppendLine("    RA2DGameManager gameManager;");
        sb.AppendLine("    private IEnumerator coroutine;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get references");
        sb.AppendLine("        gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Hit player's ship");
        sb.AppendLine("        if (collision.gameObject.CompareTag(\"RA2DPlayerShip\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            DeactivateNow();");
        sb.AppendLine("            Destroy(collision.gameObject);");
        sb.AppendLine("            gameManager.GameLost();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Hit player's bullet");
        sb.AppendLine("        else if (collision.gameObject.CompareTag(\"RA2DPlayerBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            DeactivateNow();");
        sb.AppendLine("            collision.gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void DeactivateNow()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop coroutine if not null");
        sb.AppendLine("        if (coroutine != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            StopCoroutine(coroutine);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Deactivate");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }    ");
        sb.AppendLine("");
        sb.AppendLine("    public void Fire()");
        sb.AppendLine("    {");
        sb.AppendLine("        GetComponent<Rigidbody2D>().AddForce(transform.up * missileForce);");
        sb.AppendLine("        // Deactivate after life time");
        sb.AppendLine("        coroutine = WaitToDeactivate(lifeSpan);");
        sb.AppendLine("        StartCoroutine(coroutine);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToDeactivate(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        // Then deactivate self");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DEnemyBullet", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DEnemyUFOScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4497);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DEnemyUFO : RA2DSpaceBody");
        sb.AppendLine("{");
        sb.AppendLine("    public float initialForce = 50.0f;");
        sb.AppendLine("    public float maxShootDelay = 3.0f;");
        sb.AppendLine("    public int scoreValue = 100;");
        sb.AppendLine("    private float shootDelay = 0.0f;");
        sb.AppendLine("    private RA2DGameManager gameManager;");
        sb.AppendLine("    Rigidbody2D rb;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameObject.name = \"EnemyUFO\";");
        sb.AppendLine("        gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("        // Estimate radius");
        sb.AppendLine("        radius = 0.5f * GetComponent<CircleCollider2D>().bounds.size.x;");
        sb.AppendLine("        // Apply initial force");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        if (UnityEngine.Random.Range(0, 1) == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.AddForce(new Vector2(initialForce, 0.0f));");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.AddForce(new Vector2(-initialForce, 0.0f));");
        sb.AppendLine("        }");
        sb.AppendLine("        gameManager.NumUFO++;");
        sb.AppendLine("");
        sb.AppendLine("        // Add a clone");
        sb.AppendLine("        AddClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.UpdateObjectWrapping(gameObject, 0.0f);");
        sb.AppendLine("        UpdateClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        shootDelay -= Time.deltaTime;");
        sb.AppendLine("        if (shootDelay < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            Shoot();");
        sb.AppendLine("            shootDelay = maxShootDelay;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (collision.gameObject.CompareTag(\"RA2DPlayerBullet\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            collision.gameObject.GetComponent<RA2DPlayerBullet>().DeactivateNow();");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("            gameManager.NumUFO--;");
        sb.AppendLine("            gameManager.AddScore(scoreValue);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (collision.gameObject.CompareTag(\"RA2DPlayerShip\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            Destroy(collision.gameObject);");
        sb.AppendLine("            Destroy(gameObject);");
        sb.AppendLine("            gameManager.GameLost();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Shoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject target = gameManager.playerShipObject;");
        sb.AppendLine("        // Shoot in the direction of the player's ship");
        sb.AppendLine("        if (target != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Vector2 myPos = transform.position;");
        sb.AppendLine("            Vector2 targetPos = target.transform.position;");
        sb.AppendLine("            Vector2 direction = new Vector2(targetPos.x - myPos.x, targetPos.y - myPos.y);");
        sb.AppendLine("            // Request game object from objects pool");
        sb.AppendLine("            GameObject bullet = gameManager.objectsPool.RequestObject(RA2DObjectsPool.ObjectType.EnemyBullet);");
        sb.AppendLine("            // Set bullet's position and heading");
        sb.AppendLine("            bullet.transform.position = transform.position;");
        sb.AppendLine("            bullet.transform.up = direction.normalized;");
        sb.AppendLine("            bullet.GetComponent<RA2DEnemyBullet>().Fire();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DEnemyUFO", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(16310);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public RA2DObjectsPool objectsPool;");
        sb.AppendLine("    public GameObject playerShipPrefab;");
        sb.AppendLine("    public GameObject UFOPrefab;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public int minRocks = 16;");
        sb.AppendLine("    public int maxUFO = 1;");
        sb.AppendLine("    private int numRocks = 0;");
        sb.AppendLine("    private int numUFO = 0;");
        sb.AppendLine("    private Camera mainCamera;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float sceneWidth;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float sceneHeight;");
        sb.AppendLine("    private float sceneLeft;");
        sb.AppendLine("    private float sceneRight;");
        sb.AppendLine("    private float sceneBottom;");
        sb.AppendLine("    private float sceneTop;");
        sb.AppendLine("    private const float minSpawnDelay = 1.0f;");
        sb.AppendLine("    private const float maxSpawnDelay = 5.0f;");
        sb.AppendLine("    private float spawnDelay = 0.0f;");
        sb.AppendLine("    private const float minUFODelay = 7.5f;");
        sb.AppendLine("    private const float maxUFODelay = 15.0f;");
        sb.AppendLine("    private float UFODelay = 0.0f;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public GameObject playerShipObject;");
        sb.AppendLine("");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static RA2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    // Use fields so we know where these are referenced (helps with debugging)");
        sb.AppendLine("    public int NumUFO { get => numUFO; set => numUFO = value; }");
        sb.AppendLine("    public int NumRocks { get => numRocks; set => numRocks = value; }");
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
        sb.AppendLine("        SetupCamera();");
        sb.AppendLine("        SetupObjects();");
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Start game");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            playButton.gameObject.SetActive(false);");
        sb.AppendLine("            playerShipObject = Instantiate(playerShipPrefab);");
        sb.AppendLine("            playerShipObject.name = playerShipPrefab.name;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        float deltaTime = Time.deltaTime;");
        sb.AppendLine("        // Spawn an rock every fixed interval");
        sb.AppendLine("        spawnDelay -= deltaTime;");
        sb.AppendLine("        if (spawnDelay <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);");
        sb.AppendLine("            SpawnRock();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Spawn an UFO every fixed interval");
        sb.AppendLine("        UFODelay -= deltaTime;");
        sb.AppendLine("        if (UFODelay <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            UFODelay = Random.Range(minUFODelay, maxUFODelay);");
        sb.AppendLine("            SpawnUFO();");
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
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool CheckBottomStradling(GameObject spaceBody, float radius)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (spaceBody.transform.position.y - radius < sceneBottom) && (spaceBody.GetComponent<Rigidbody2D>().velocity.y < 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool CheckLeftStradling(GameObject spaceBody, float radius)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (spaceBody.transform.position.x - radius < sceneLeft) && (spaceBody.GetComponent<Rigidbody2D>().velocity.x < 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool CheckRightStradling(GameObject spaceBody, float radius)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (spaceBody.transform.position.x + radius > sceneRight) && (spaceBody.GetComponent<Rigidbody2D>().velocity.x > 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool CheckTopStradling(GameObject spaceBody, float radius)");
        sb.AppendLine("    {");
        sb.AppendLine("        return (spaceBody.transform.position.y + radius > sceneTop) && (spaceBody.GetComponent<Rigidbody2D>().velocity.y > 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupCamera()");
        sb.AppendLine("    {");
        sb.AppendLine("        mainCamera = Camera.main;");
        sb.AppendLine("        // Derive edges from camera's width and height");
        sb.AppendLine("        sceneWidth = mainCamera.orthographicSize * 2.0f * mainCamera.aspect;");
        sb.AppendLine("        sceneHeight = mainCamera.orthographicSize * 2.0f;");
        sb.AppendLine("        sceneRight = sceneWidth / 2.0f;");
        sb.AppendLine("        sceneLeft = sceneRight * -1.0f;");
        sb.AppendLine("        sceneTop = sceneHeight / 2.0f;");
        sb.AppendLine("        sceneBottom = sceneTop * -1.0f;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        UFODelay = Random.Range(minUFODelay, maxUFODelay);");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore;");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnRock()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Do not spawn if there are too many rocks");
        sb.AppendLine("        if (NumRocks >= minRocks)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Request the rock game object from the objects pool");
        sb.AppendLine("        GameObject rock = objectsPool.RequestObject(RA2DObjectsPool.ObjectType.LargeRock);");
        sb.AppendLine("        RA2DRock component = rock.GetComponent<RA2DRock>();");
        sb.AppendLine("        component.Initialize();");
        sb.AppendLine("        // Calculate the necessary offset distance");
        sb.AppendLine("        float offset = component.radius;");
        sb.AppendLine("        // Randomly generate a position");
        sb.AppendLine("        Vector2 newPosition = new Vector2(Random.Range(sceneLeft, sceneRight), Random.Range(sceneBottom, sceneTop));");
        sb.AppendLine("        if (Mathf.Abs(newPosition.x) > Mathf.Abs(newPosition.y))");
        sb.AppendLine("        {");
        sb.AppendLine("            newPosition.x = newPosition.x > 0.0f ? sceneRight + offset : sceneLeft - offset;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            newPosition.y = newPosition.y > 0.0f ? sceneTop + offset : sceneBottom - offset;");
        sb.AppendLine("        }");
        sb.AppendLine("        rock.transform.position = newPosition;");
        sb.AppendLine("        rock.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f));");
        sb.AppendLine("");
        sb.AppendLine("        // Give it a random direction");
        sb.AppendLine("        const float k = 1.0f;");
        sb.AppendLine("        Vector2 tgtPosition = new Vector2(Random.Range(sceneLeft+k, sceneRight-k), Random.Range(sceneBottom+k, sceneTop-k));");
        sb.AppendLine("        tgtPosition -= newPosition;");
        sb.AppendLine("        tgtPosition = tgtPosition.normalized;");
        sb.AppendLine("        // Give it a push of random magnitude");
        sb.AppendLine("        float maxThrust = component.maxThrust;");
        sb.AppendLine("        float randomForce = Random.Range(0.0f, maxThrust);");
        sb.AppendLine("        rock.GetComponent<Rigidbody2D>().AddForce(tgtPosition * randomForce);");
        sb.AppendLine("        // Start a random spin");
        sb.AppendLine("        component.RandomTorque();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnUFO()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (NumUFO >= maxUFO)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Randomly place the UFO");
        sb.AppendLine("        float randomInteger = Random.Range(0, 1);");
        sb.AppendLine("        float spawnX = randomInteger == 0 ? sceneLeft : sceneRight;");
        sb.AppendLine("        float spawnY = Random.Range(sceneBottom, sceneTop);");
        sb.AppendLine("        // Instantiate the UFO");
        sb.AppendLine("        GameObject UFO = Instantiate(UFOPrefab);");
        sb.AppendLine("        // Position the UFO");
        sb.AppendLine("        float ufoOffset = 0.5f * UFO.GetComponent<Collider2D>().bounds.size.x;");
        sb.AppendLine("        ufoOffset = randomInteger == 0 ? -ufoOffset : ufoOffset;");
        sb.AppendLine("        UFO.transform.position = new Vector2(spawnX + ufoOffset, spawnY);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"RockAttack2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddScore(int change)");
        sb.AppendLine("    {");
        sb.AppendLine("        currentScore += change;");
        sb.AppendLine("        currentScore = System.Math.Min(currentScore, 9999);");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameLost()");
        sb.AppendLine("    {");
        sb.AppendLine("        playButton.gameObject.SetActive(true);");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void UpdateObjectWrapping(GameObject spaceObject, float radius)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (spaceObject.GetComponent<SpriteRenderer>().isVisible == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        Transform soTransform = spaceObject.transform;");
        sb.AppendLine("        Rigidbody2D rb = spaceObject.GetComponent<Rigidbody2D>();");
        sb.AppendLine("        // Wrap around object that passes the right edge completely");
        sb.AppendLine("        if (soTransform.position.x - radius > sceneRight)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (rb.velocity.x > 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                soTransform.position = new Vector2(soTransform.position.x - sceneWidth, soTransform.position.y);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Wrap around object that passes the left edge completely");
        sb.AppendLine("        if (soTransform.position.x + radius < sceneLeft)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (rb.velocity.x < 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                soTransform.position = new Vector2(soTransform.position.x + sceneWidth, soTransform.position.y);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Wrap around object that passes the top edge completely");
        sb.AppendLine("        if (soTransform.position.y - radius > sceneTop)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (rb.velocity.y > 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                soTransform.position = new Vector2(soTransform.position.x, soTransform.position.y - sceneHeight);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Wrap around object that passes the bottom edge completely");
        sb.AppendLine("        if (soTransform.position.y + radius < sceneBottom)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (rb.velocity.y < 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                soTransform.position = new Vector2(soTransform.position.x, soTransform.position.y + sceneHeight);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DObjectsPoolScriptToFile()
    {
        StringBuilder sb = new StringBuilder(5537);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DObjectsPool : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject smallRockPrefab;");
        sb.AppendLine("    public GameObject mediumRockPrefab;");
        sb.AppendLine("    public GameObject largeRockPrefab;");
        sb.AppendLine("    public GameObject enemyBulletPrefab;");
        sb.AppendLine("    public GameObject playerBulletPrefab;");
        sb.AppendLine("    private List<GameObject> smallRocks = new List<GameObject>(72);");
        sb.AppendLine("    private List<GameObject> mediumRocks = new List<GameObject>(24);");
        sb.AppendLine("    private List<GameObject> largeRocks = new List<GameObject>(8);");
        sb.AppendLine("    private List<GameObject> enemyBullets = new List<GameObject>(10);");
        sb.AppendLine("    private List<GameObject> playerBullets = new List<GameObject>(20);");
        sb.AppendLine("");
        sb.AppendLine("    public enum ObjectType");
        sb.AppendLine("    {");
        sb.AppendLine("        Undefined = 0,");
        sb.AppendLine("        SmallRock,");
        sb.AppendLine("        MediumRock,");
        sb.AppendLine("        LargeRock,");
        sb.AppendLine("        EnemyBullet,");
        sb.AppendLine("        PlayerBullet,");
        sb.AppendLine("        Max");
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    private GameObject GetPrefabForType(ObjectType type)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (type)");
        sb.AppendLine("        {");
        sb.AppendLine("            case ObjectType.SmallRock:");
        sb.AppendLine("                return smallRockPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.MediumRock:");
        sb.AppendLine("                return mediumRockPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.LargeRock:");
        sb.AppendLine("                return largeRockPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.EnemyBullet:");
        sb.AppendLine("                return enemyBulletPrefab;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.PlayerBullet:");
        sb.AppendLine("                return playerBulletPrefab;");
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
        sb.AppendLine("            case ObjectType.LargeRock:");
        sb.AppendLine("                return largeRocks;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.MediumRock:");
        sb.AppendLine("                return mediumRocks;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.SmallRock:");
        sb.AppendLine("                return smallRocks;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.EnemyBullet:");
        sb.AppendLine("                return enemyBullets;");
        sb.AppendLine("");
        sb.AppendLine("            case ObjectType.PlayerBullet:");
        sb.AppendLine("                return playerBullets;");
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

        ScriptUtilities.CreateScriptFile("RA2DObjectsPool", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DPlayerBulletScriptToFile()
    {
        StringBuilder sb = new StringBuilder(2317);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DPlayerBullet : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float missileForce = 350.0f;");
        sb.AppendLine("    public float lifeSpan = 1.5f;");
        sb.AppendLine("    private RA2DGameManager gameManager;");
        sb.AppendLine("    private IEnumerator coroutine;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.UpdateObjectWrapping(gameObject, 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void DeactivateNow()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop coroutine");
        sb.AppendLine("        if (coroutine != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            StopCoroutine(coroutine);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Deactivate");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Fire()");
        sb.AppendLine("    {");
        sb.AppendLine("        GetComponent<Rigidbody2D>().AddForce(transform.up * missileForce);");
        sb.AppendLine("        // Destroy self after a while");
        sb.AppendLine("        coroutine = WaitToDeactivate(lifeSpan);");
        sb.AppendLine("        StartCoroutine(coroutine);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToDeactivate(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        // Then deactivate");
        sb.AppendLine("        gameObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DPlayerBullet", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DPlayerShipScriptToFile()
    {
        StringBuilder sb = new StringBuilder(5566);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DPlayerShip : RA2DSpaceBody");
        sb.AppendLine("{");
        sb.AppendLine("    public float thrust = 5.0f;");
        sb.AppendLine("    public float rotationSpeed = 270.0f;");
        sb.AppendLine("    public float speedMax = 2.0f;");
        sb.AppendLine("    public float dampFactor = 0.9f;");
        sb.AppendLine("    private Vector2 inputVector;");
        sb.AppendLine("    private RA2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        radius = EstimateRadius();");
        sb.AppendLine("        // Add a clone");
        sb.AppendLine("        AddClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnDestroy()");
        sb.AppendLine("    {");
        sb.AppendLine("        Destroy(clone);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        ControlShip();");
        sb.AppendLine("        ConstrainPosition();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        inputVector = input.Get<Vector2>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnShoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        Shoot();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ControlShip()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Change facing");
        sb.AppendLine("        transform.Rotate(0.0f, 0.0f, inputVector.x * -rotationSpeed * Time.deltaTime);");
        sb.AppendLine("        float inputVertical = inputVector.y;");
        sb.AppendLine("");
        sb.AppendLine("        // If forward / backward thrust input detected");
        sb.AppendLine("        if (Mathf.Abs(inputVertical) > float.Epsilon)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Apply forward thrust");
        sb.AppendLine("            rb.AddForce(transform.up * thrust * inputVertical);");
        sb.AppendLine("            // Cap speed to maximum");
        sb.AppendLine("            if (rb.velocity.sqrMagnitude > speedMax * speedMax)");
        sb.AppendLine("            {");
        sb.AppendLine("                rb.velocity = rb.velocity.normalized * speedMax;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Slowly dampen the ship's speed to zero");
        sb.AppendLine("            if (rb.velocity.sqrMagnitude > float.Epsilon)");
        sb.AppendLine("            {");
        sb.AppendLine("                rb.velocity -= (rb.velocity * dampFactor * Time.deltaTime);");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                rb.velocity *= 0.0f;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ConstrainPosition()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.UpdateObjectWrapping(gameObject, radius);");
        sb.AppendLine("        UpdateClone();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float EstimateRadius()");
        sb.AppendLine("    {");
        sb.AppendLine("        PolygonCollider2D collider = gameObject.GetComponent<PolygonCollider2D>();");
        sb.AppendLine("        float maxHalfWidth = float.MinValue;");
        sb.AppendLine("        float maxHalfHeight = float.MinValue;");
        sb.AppendLine("        // Find extreme values from the polygon's points");
        sb.AppendLine("        foreach (Vector2 point in collider.points)");
        sb.AppendLine("        {");
        sb.AppendLine("            float hw = Mathf.Abs(point.x);");
        sb.AppendLine("            float hh = Mathf.Abs(point.y);");
        sb.AppendLine("            if (maxHalfWidth < hw)");
        sb.AppendLine("            {");
        sb.AppendLine("                maxHalfWidth = hw;");
        sb.AppendLine("            }");
        sb.AppendLine("            if (maxHalfHeight < hh)");
        sb.AppendLine("            {");
        sb.AppendLine("                maxHalfHeight = hh;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Return the bigger value");
        sb.AppendLine("        return maxHalfWidth > maxHalfHeight ? maxHalfWidth : maxHalfHeight;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Shoot()");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject bullet = gameManager.objectsPool.RequestObject(RA2DObjectsPool.ObjectType.PlayerBullet);");
        sb.AppendLine("        bullet.transform.position = transform.position;");
        sb.AppendLine("        bullet.transform.rotation = transform.rotation;");
        sb.AppendLine("        bullet.GetComponent<RA2DPlayerBullet>().Fire();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DPlayerShip", scriptsPath, sb.ToString());
    }

    private static void WriteRA2DSpaceBodyScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3710);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RA2DSpaceBody : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject clonePrefab;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float radius = 1.0f;");
        sb.AppendLine("    protected GameObject clone = null;");
        sb.AppendLine("");
        sb.AppendLine("    protected void AddClone()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Bail if already has a clone");
        sb.AppendLine("        if (clone)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Add a clone");
        sb.AppendLine("        clone = Instantiate(clonePrefab);");
        sb.AppendLine("        clone.name = clonePrefab.name;");
        sb.AppendLine("        clone.transform.SetParent(transform);");
        sb.AppendLine("        clone.transform.rotation = transform.rotation;");
        sb.AppendLine("        ShowClone(false, transform.position);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ShowClone(bool flag, Vector3 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Show / hide and position the clone");
        sb.AppendLine("        SpriteRenderer sr = clone.GetComponent<SpriteRenderer>();");
        sb.AppendLine("        if (sr.enabled != flag)");
        sb.AppendLine("        {");
        sb.AppendLine("            sr.enabled = flag;");
        sb.AppendLine("        }");
        sb.AppendLine("        clone.transform.position = position;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected void UpdateClone()");
        sb.AppendLine("    {");
        sb.AppendLine("        RA2DGameManager gameManager = RA2DGameManager.sharedInstance;");
        sb.AppendLine("        bool isStradling = false;");
        sb.AppendLine("");
        sb.AppendLine("        // Clone rotation and position");
        sb.AppendLine("        clone.transform.rotation = transform.rotation;");
        sb.AppendLine("        Vector2 clonePosition = transform.position;");
        sb.AppendLine("");
        sb.AppendLine("        // Check if object is stradling edges");
        sb.AppendLine("        if (gameManager.CheckLeftStradling(gameObject, radius))");
        sb.AppendLine("        {");
        sb.AppendLine("            clonePosition.x += gameManager.sceneWidth;");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (gameManager.CheckRightStradling(gameObject, radius))");
        sb.AppendLine("        {");
        sb.AppendLine("            clonePosition.x -= gameManager.sceneWidth;");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (gameManager.CheckBottomStradling(gameObject, radius))");
        sb.AppendLine("        {");
        sb.AppendLine("            clonePosition.y += gameManager.sceneHeight;");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (gameManager.CheckTopStradling(gameObject, radius))");
        sb.AppendLine("        {");
        sb.AppendLine("            clonePosition.y -= gameManager.sceneHeight;");
        sb.AppendLine("            isStradling = true;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Show / hide and position the clone");
        sb.AppendLine("        ShowClone(isStradling, clonePosition);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RA2DSpaceBody", scriptsPath, sb.ToString());
    }
}
