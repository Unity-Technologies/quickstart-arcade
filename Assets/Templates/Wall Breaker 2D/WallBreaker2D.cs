using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class WallBreaker2D : Editor
{
    private const string templateName = "WallBreaker2D";
    private const string templateSpacedName = "Wall Breaker 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "WB2D";
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
    private static string materialPath;

    enum SpritePath
    {
        Ball = 0,
        Paddle,
        BrickLight,
        BrickMedium,
        BrickDark,
        BrickWood,
        Boundary,
        Marker,
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
        ScriptUtilities.RemoveTag(scriptPrefix + "Brick");
        ScriptUtilities.RemoveTag(scriptPrefix + "Paddle");
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
    static public void GenerateGameTemplate()
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
        mainCamera.GetComponent<Camera>().backgroundColor = new Color(80.0f / 255.0f, 184.0f / 255.0f, 231.0f / 255.0f);
        mainCamera.GetComponent<Camera>().orthographicSize = 15;
        mainCamera.transform.position = new Vector3(0.0f, 12.0f, -10.0f);
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        ScriptUtilities.CreateTag(scriptPrefix + "Brick");
        ScriptUtilities.CreateTag(scriptPrefix + "Paddle");
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
        const float d = 1.0f / 255.0f;
        Color ballColor = new Color(56.0f * d, 56.0f * d, 56.0f * d);
        Color markerColor = new Color(10.0f * d, 133.0f * d, 194.0f * d);
        Color boundaryColor = new Color(128.0f * d, 128.0f * d, 128.0f * d);
        Color lightColor = new Color(169.0f * d, 171.0f * d, 173.0f * d);
        Color mediumColor = new Color(149.0f * d, 155.0f * d, 161.0f * d);
        Color darkColor = new Color(119.0f * d, 125.0f * d, 132.0f * d);
        Color woodColor = new Color(164.0f * d, 116.0f * d, 73.0f * d);

        // Create ball texture
        path = ContentUtilities.CreateTexture2DCircleAsset("ball_texture", texturesPath, textureSize, textureSize, ballColor);
        spritePaths[(int)SpritePath.Ball] = path;
        // Create player paddle texture
        path = DrawPaddleTexture();
        spritePaths[(int)SpritePath.Paddle] = path;

        int brickWidth = textureSize * 2;
        int brickHeight = textureSize / 2;
        // Create light brick texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("light_brick_texture", texturesPath, brickWidth, brickHeight, lightColor);
        spritePaths[(int)SpritePath.BrickLight] = path;
        // Create medium brick texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("medium_brick_texture", texturesPath, brickWidth, brickHeight, mediumColor);
        spritePaths[(int)SpritePath.BrickMedium] = path;
        // Create dark brick texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("dark_brick_texture", texturesPath, brickWidth, brickHeight, darkColor);
        spritePaths[(int)SpritePath.BrickDark] = path;
        // Create wood brick texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("wood_brick_texture", texturesPath, brickWidth, brickHeight, woodColor);
        spritePaths[(int)SpritePath.BrickWood] = path;

        // Create wall texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("wall_white_texture", texturesPath, 2 * textureSize, 2 * textureSize, boundaryColor);
        spritePaths[(int)SpritePath.Boundary] = path;
        // Create marker texture
        path = ContentUtilities.CreateTexture2DRectangleAsset("wall_blue_texture", texturesPath, 2 * textureSize, 2 * textureSize, markerColor);
        spritePaths[(int)SpritePath.Marker] = path;
    }

    private static void GenerateMaterials()
    {
        // Create physics 2D material for ball
        materialPath = ContentUtilities.CreatePhysicsMaterial2D("BallPhysicsMaterial2D", 1.0f, 0.0f, texturesPath);
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
        Rigidbody2D rb;
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;

        // Create the game manager object
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create the boundary container object
        newObject = new GameObject("Boundary");
        newObject.transform.position = Vector2.zero;

        // Create the bricks container object
        newObject = new GameObject("Bricks");
        newObject.transform.position = new Vector2(0.0f, 12.0f);

        // Create ball spawn point
        newObject = new GameObject("BallSpawnPoint");
        newObject.transform.position = new Vector2(0.0f, 11.0f);

        // Create objects then make them prefab
        // Ball
        newObject = ContentUtilities.CreateTexturedBody("Ball", 0.0f, 0.0f, spritePaths[(int)SpritePath.Ball], ContentUtilities.ColliderShape.Circle);
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 0.01f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        newObject.transform.position = new Vector2(0.0f, 11.0f);
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath("Assets/" + materialPath, typeof(PhysicsMaterial2D)) as PhysicsMaterial2D;
        newObject.GetComponent<CircleCollider2D>().sharedMaterial = material;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Player paddle
        newObject = ContentUtilities.CreateTexturedBody("Paddle", 0.0f, 0.0f, spritePaths[(int)SpritePath.Paddle], ContentUtilities.ColliderShape.Capsule);
        newObject.tag = scriptPrefix + "Paddle";
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        newObject.GetComponent<CapsuleCollider2D>().direction = CapsuleDirection2D.Horizontal;
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        rb.mass = 100.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        newObject.transform.position = new Vector2(0.0f, 0.0f);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Brick
        CreateBrickPrefab("BrickLight", spritePaths[(int)SpritePath.BrickLight]);
        CreateBrickPrefab("BrickMedium", spritePaths[(int)SpritePath.BrickMedium]);
        CreateBrickPrefab("BrickDark", spritePaths[(int)SpritePath.BrickDark]);
        CreateBrickPrefab("BrickWood", spritePaths[(int)SpritePath.BrickWood]);
        // Wall
        newObject = ContentUtilities.CreateTexturedBody("Wall", 0.0f, 0.0f, spritePaths[(int)SpritePath.Boundary]);
        rb = newObject.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 0.0f;
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
        sb.AppendLine("Use Left / Right Arrow Keys or A / D to Control the Paddle");
        sb.AppendLine("Bounce the Ball Back to the Bricks to Break Them");
        sb.AppendLine("Don't let the Ball Fall out of the Screen!");
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
        WriteWB2DBallScriptToFile();
        WriteWB2DBlinkTextScriptToFile();
        WriteWB2DBrickScriptToFile();
        WriteWB2DGameManagerScriptToFile();
        WriteWB2DPaddleScriptToFile();
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Ball", "Brick", "GameManager", "Paddle", "Wall" }))
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
        GameObject paddlePrefab = ContentUtilities.LoadPrefab("Paddle", prefabsPath);
        GameObject ballPrefab = ContentUtilities.LoadPrefab("Ball", prefabsPath);
        GameObject brickLightPrefab = ContentUtilities.LoadPrefab("BrickLight", prefabsPath);
        GameObject brickMediumPrefab = ContentUtilities.LoadPrefab("BrickMedium", prefabsPath);
        GameObject brickDarkPrefab = ContentUtilities.LoadPrefab("BrickDark", prefabsPath);
        GameObject brickWoodPrefab = ContentUtilities.LoadPrefab("BrickWood", prefabsPath);
        GameObject wallPrefab = ContentUtilities.LoadPrefab("Wall", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Paddle", paddlePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Ball", ballPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Brick", brickLightPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Brick", brickMediumPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Brick", brickDarkPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Brick", brickWoodPrefab);

        // Configure bricks
        ConfigureBrickStats(brickLightPrefab, 10, false);
        ConfigureBrickStats(brickMediumPrefab, 10, false);
        ConfigureBrickStats(brickDarkPrefab, 30, true);
        ConfigureBrickStats(brickWoodPrefab, 20, true);

        // Layout walls
        LayoutWalls(wallPrefab);
        // Layout bricks
        LayoutBricks();

        // Instantiate objects
        PrefabUtility.InstantiatePrefab(ballPrefab);
        InstantiateAndSetupPaddle(paddlePrefab);
        InstantiateAndSetupGameManager(gameManagerPrefab);

        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static void ConfigureBrickStats(GameObject prefab, int value, bool affectBallSpeed)
    {
        string className = scriptPrefix + "Brick";

        ScriptUtilities.AssignIntFieldToObject(value, prefab, className, "value");
        ScriptUtilities.AssignBoolFieldToObject(affectBallSpeed, prefab, className, "affectBallSpeed");
    }

    private static void CreateBrickPrefab(string name, string spritePath)
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody("Brick", 0.0f, 0.0f, spritePath);
        newObject.name = name;
        newObject.tag = scriptPrefix + "Brick";
        Rigidbody2D rb = newObject.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.gravityScale = 0.0f;
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
    }

    private static string DrawPaddleTexture()
    {
        // Colors
        Color bodyColor = new Color(170.0f / 255.0f, 169.0f / 255.0f, 173.0f / 255.0f);
        Color edgeColor = new Color(237.0f / 255.0f, 41.0f / 255.0f, 57.0f / 255.0f);
        // Dimensions
        int w = 3 * textureSize;
        int h = 1 * textureSize;
        int bodyW = 2 * textureSize;
        int bodyH = 1 * textureSize;
        int edgeW = 1 * textureSize;
        int edgeH = 1 * textureSize;

        // Draw BG
        Color[] bg = ContentUtilities.FillBitmap(w, h, Color.clear);
        // Draw edge circles
        Color[] circle = ContentUtilities.FillBitmapShapeCircle(edgeW, edgeH, edgeColor);
        // Draw body
        Color[] body = ContentUtilities.FillBitmapShapeRectangle(bodyW, bodyH, bodyColor);
        // Combine the shapes
        bg = ContentUtilities.CopyBitmap(circle, edgeW, edgeH, bg, w, h, new Vector2Int(0, 0));
        bg = ContentUtilities.CopyBitmap(circle, edgeW, edgeH, bg, w, h, new Vector2Int(w - edgeW, 0));
        bg = ContentUtilities.CopyBitmap(body, bodyW, bodyH, bg, w, h, new Vector2Int(edgeW / 2, 0));
        // Make into a texture
        return ContentUtilities.CreateBitmapAsset("paddle_texture", bg, w, h, texturesPath);
    }

    private static int[] GetBricksLayout()
    {
        return new int[]
        {
            -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  3, -1,  3, -1,  3, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0,
            -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  3, -1,  3, -1,  3, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1,
            -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  3, -1,  3, -1,  3, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0,
            -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  3, -1,  3, -1,  3, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1,
            -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  3, -1,  3, -1,  3, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0,
            -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  3, -1,  3, -1,  3, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1,
            -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  3, -1,  3, -1,  3, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0,
            -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  3, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1,
            -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  2, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0, -1,  1, -1,  0,
            -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2, -1,  2,
            -1, -1,  2, -1, -1, -1,  2, -1, -1, -1,  2, -1, -1, -1,  2, -1,  2, -1, -1, -1,  2, -1, -1, -1,  2, -1, -1, -1,  2, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static void InstantiateAndSetupPaddle(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        go.transform.position = new Vector2(0.0f, 0.0f);
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        string className = scriptPrefix + "GameManager";
        // Instantiate
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        go.name = prefab.name;

        // Get or instantiate objects
        GameObject bricksObject = GameObject.Find("Bricks");
        GameObject ballSpawnPointObject = GameObject.Find("BallSpawnPoint");
        GameObject blueBarObject = GameObject.Find("BlueBar");
        GameObject paddleObject = GameObject.Find("Paddle");
        GameObject ballObject = GameObject.Find("Ball");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject lifeTextObject = GameObject.Find("LifeText");
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects or components
        ScriptUtilities.AssignObjectFieldToObject(bricksObject, go, className, "bricksObject");
        ScriptUtilities.AssignObjectFieldToObject(ballSpawnPointObject, go, className, "ballSpawnPoint");
        ScriptUtilities.AssignObjectFieldToObject(blueBarObject, go, className, "blueBarObject");
        ScriptUtilities.AssignComponentFieldToObject(paddleObject, scriptPrefix + "Paddle", go, className, "paddle");
        ScriptUtilities.AssignComponentFieldToObject(ballObject, scriptPrefix + "Ball", go, className, "ball");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(lifeTextObject, "Text", go, className, "lifeText");
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");

        // Hide paddle
        paddleObject.SetActive(false);
    }

    private static void LayoutBricks()
    {
        Transform parent = GameObject.Find("Bricks").transform;
        int[] bricksLayout = GetBricksLayout();
        int w = 30;
        int h = 15;
        float offsetX = -15.0f;
        float offsetY = 0.0f;

        // Build parameters
        GameObject containerObject = GameObject.Find("Bricks");
        if (!containerObject)
        {
            Debug.LogWarning("Container object does not exist.");
            return;
        }
        GameObject brickLightPrefab = ContentUtilities.LoadPrefab("BrickLight", prefabsPath);
        GameObject brickMediumPrefab = ContentUtilities.LoadPrefab("BrickMedium", prefabsPath);
        GameObject brickDarkPrefab = ContentUtilities.LoadPrefab("BrickDark", prefabsPath);
        GameObject brickWoodPrefab = ContentUtilities.LoadPrefab("BrickWood", prefabsPath);
        GameObject[] guide = new GameObject[] { brickLightPrefab, brickMediumPrefab, brickDarkPrefab, brickWoodPrefab };
        
        // Convert into a map array
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Solve brick type
                int index = y * w + x;
                int prefabType = bricksLayout[index];
                // Skip if invalid
                if (prefabType < 0 || prefabType >= guide.Length)
                {
                    continue;
                }
                // Create and place the object
                float posX = offsetX + x;
                float posY = (offsetY + y) * 0.5f;
                GameObject go = PrefabUtility.InstantiatePrefab(guide[prefabType]) as GameObject;
                go.transform.localPosition = new Vector2(posX, posY);
                // Put into a container
                go.transform.SetParent(parent, false);
            }
        }
    }

    private static void LayoutMarker(string name, GameObject prefab, Sprite sprite, Transform parent, float posX, float posY, float scaleX, float scaleY)
    {
        GameObject marker = Instantiate(prefab);
        marker.GetComponent<SpriteRenderer>().sprite = sprite;
        marker.GetComponent<SpriteRenderer>().sortingOrder = 1;
        marker.name = name;
        marker.transform.position = new Vector3(posX, posY, 0.0f);
        marker.transform.localScale = new Vector2(scaleX, scaleY);
        if (parent)
        {
            marker.transform.SetParent(parent, true);
        }
    }

    private static void LayoutWalls(GameObject wallObject)
    {
        const float topY = 25.0f;
        const float bottomY = 0.0f;
        const float leftX = -15.5f;
        const float rightX = 15.5f;
        const float sideY = 12.0f;

        // Get boundary object parent
        Transform parent = GameObject.Find("Boundary").transform;

        // Top bar
        GameObject topBar = Instantiate(wallObject);
        topBar.name = "TopBar";
        topBar.transform.position = new Vector2(0.0f, topY);
        topBar.transform.localScale = new Vector2(16.0f, 0.5f);
        topBar.transform.SetParent(parent);

        // Left bar
        GameObject leftBar = Instantiate(wallObject);
        leftBar.name = "LeftBar";
        leftBar.transform.position = new Vector2(leftX, sideY);
        leftBar.transform.localScale = new Vector2(0.5f, 15.0f);
        leftBar.transform.SetParent(parent);

        // Right bar
        GameObject rightBar = Instantiate(wallObject);
        rightBar.name = "RightBar";
        rightBar.transform.position = new Vector2(rightX, sideY);
        rightBar.transform.localScale = new Vector2(0.5f, 15.0f);
        rightBar.transform.SetParent(parent);

        // Markers
        Sprite sprite = ContentUtilities.LoadSprite("wall_blue_texture", texturesPath);
        LayoutMarker("LeftLineMarker", wallObject, sprite, parent, leftX, 0.0f, 0.5f, 0.5f);
        LayoutMarker("RightLineMarker", wallObject, sprite, parent, rightX, 0.0f, 0.5f, 0.5f);

        // Lay out blue bar 
        sprite = ContentUtilities.LoadSprite("wall_blue_texture", texturesPath);
        LayoutMarker("BlueBar", wallObject, sprite, parent, 0.0f, bottomY, 15.0f, 0.5f);
    }

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        // Note: the scripts, objects, and tilemaps must exist before this function is called
        ReverseEngineerBricksLayout();
        ReverseEngineerScripts();
    }

    private static void ReverseEngineerBricksLayout()
    {
        // Call this method only after the Game Template has been created

        int w = 30;
        int h = 15;

        // Build parameters
        GameObject containerObject = GameObject.Find("Bricks");
        if (!containerObject)
        {
            Debug.LogWarning("Container object does not exist.");
            return;
        }
        GameObject brickLightPrefab = ContentUtilities.LoadPrefab("BrickLight", prefabsPath);
        GameObject brickMediumPrefab = ContentUtilities.LoadPrefab("BrickMedium", prefabsPath);
        GameObject brickDarkPrefab = ContentUtilities.LoadPrefab("BrickDark", prefabsPath);
        GameObject brickWoodPrefab = ContentUtilities.LoadPrefab("BrickWood", prefabsPath);
        GameObject[] guide = new GameObject[] { brickLightPrefab, brickMediumPrefab, brickDarkPrefab, brickWoodPrefab };
        // Convert into a map array
        int[] mapArray = ContentUtilities.ConvertContainerLayoutToMapArray(containerObject, guide, w, h, startX: -15, scaleY: 0.5f);
        // Convert map array to string
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        // Print out to console
        Debug.Log("Bricks Map");
        Debug.Log(sb.ToString());
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("WB2DBall", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("WB2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("WB2DBrick", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("WB2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("WB2DPaddle", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void WriteWB2DBallScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4984);

        sb.AppendLine("using Unity.Mathematics;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class WB2DBall : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float startSpeed = 6.0f;");
        sb.AppendLine("    public float maxSpeed = 10.0f;");
        sb.AppendLine("    public float speedStep = 1.0f;");
        sb.AppendLine("    private float currentSpeed = 0.0f;");
        sb.AppendLine("    private WB2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private const float bottomThreshold = -3.0f;");
        sb.AppendLine("    private int hitCount = 0;");
        sb.AppendLine("    private const int firstHitThreshold = 4;");
        sb.AppendLine("    private const int secondHitThreshold = 12;");
        sb.AppendLine("");
        sb.AppendLine("    private void OnEnable()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Save variables");
        sb.AppendLine("        gameManager = WB2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        // Initialize ball with semi-random direction");
        sb.AppendLine("        Vector2 direction = new Vector2(0.0f, -1.0f);");
        sb.AppendLine("        float rotation = UnityEngine.Random.Range(-30.0f, 30.0f);");
        sb.AppendLine("        direction = Quaternion.Euler(0.0f, 0.0f, rotation) * direction;");
        sb.AppendLine("        currentSpeed = startSpeed;");
        sb.AppendLine("        rb.velocity = direction * currentSpeed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If the ball falls below a threshold");
        sb.AppendLine("        if (transform.position.y < bottomThreshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Tell the game manager that the ball is lost");
        sb.AppendLine("            gameManager.BallLost();");
        sb.AppendLine("            gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        // If collided with brick");
        sb.AppendLine("        if (collidedObject.CompareTag(\"WB2DBrick\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Add score");
        sb.AppendLine("            WB2DBrick brick = collidedObject.GetComponent<WB2DBrick>();");
        sb.AppendLine("            gameManager.AddScore(brick.value);");
        sb.AppendLine("            // Adjust ball speed if needed");
        sb.AppendLine("            if (brick.affectBallSpeed == true)");
        sb.AppendLine("            {");
        sb.AppendLine("                currentSpeed += speedStep;");
        sb.AppendLine("                ClampSpeed();");
        sb.AppendLine("            }");
        sb.AppendLine("            // Destroy the brick");
        sb.AppendLine("            Destroy(collidedObject);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if collided with the paddle");
        sb.AppendLine("        else if (collidedObject.CompareTag(\"WB2DPaddle\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Rebound based on where it hits the paddle");
        sb.AppendLine("            float ballX = transform.position.x;");
        sb.AppendLine("            float paddleX = collidedObject.transform.position.x;");
        sb.AppendLine("            float paddleWidth = collidedObject.GetComponent<CapsuleCollider2D>().size.x;");
        sb.AppendLine("            float directionX = (ballX - paddleX) / paddleWidth;");
        sb.AppendLine("            rb.velocity = new Vector2(directionX, 1.0f).normalized;");
        sb.AppendLine("            rb.velocity *= currentSpeed;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionExit2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Increase ball speed after it surpasses certain thresholds");
        sb.AppendLine("        hitCount++;");
        sb.AppendLine("        if (hitCount == firstHitThreshold || hitCount == secondHitThreshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            currentSpeed += speedStep;");
        sb.AppendLine("            ClampSpeed();");
        sb.AppendLine("        }");
        sb.AppendLine("        rb.velocity = rb.velocity.normalized * currentSpeed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ClampSpeed()");
        sb.AppendLine("    {");
        sb.AppendLine("        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("WB2DBall", scriptsPath, sb.ToString());
    }

    private static void WriteWB2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class WB2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("WB2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteWB2DBrickScriptToFile()
    {
        StringBuilder sb = new StringBuilder(554);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class WB2DBrick : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int value = 10;");
        sb.AppendLine("    public bool affectBallSpeed = true;");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("WB2DBrick", scriptsPath, sb.ToString());
    }

    private static void WriteWB2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(10689);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class WB2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject bricksObject;");
        sb.AppendLine("    public GameObject ballSpawnPoint;");
        sb.AppendLine("    public WB2DBall ball;");
        sb.AppendLine("    public WB2DPaddle paddle;");
        sb.AppendLine("    public GameObject blueBarObject;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text lifeText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public int lifeCount = 3;");
        sb.AppendLine("    public float ballSpawnDelay = 1.5f;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("    private int numBricks;");
        sb.AppendLine("    private int numBroken;");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("");
        sb.AppendLine("    public static WB2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Ensure that only one instance exist");
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
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            blueBarObject.SetActive(false);");
        sb.AppendLine("            ball.gameObject.SetActive(false);");
        sb.AppendLine("            StartGame();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Go into standby mode");
        sb.AppendLine("            blueBarObject.SetActive(true);");
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
        sb.AppendLine("    public void AddScore(int score)");
        sb.AppendLine("    {");
        sb.AppendLine("        currentScore += score;");
        sb.AppendLine("        UpdateScoreUI();");
        sb.AppendLine("        numBroken++;");
        sb.AppendLine("        if (numBroken >= numBricks)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameWon();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void BallLost()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Decrement life count");
        sb.AppendLine("        lifeCount--;");
        sb.AppendLine("        UpdateLifeUI();");
        sb.AppendLine("        if (lifeCount <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Game is over if no more life");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Respawn the ball after a short wait");
        sb.AppendLine("            StartCoroutine(WaitToRespawnBall(ballSpawnDelay));");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        paddle.gameObject.SetActive(false);");
        sb.AppendLine("        blueBarObject.SetActive(true);");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Too bad...\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameWon()");
        sb.AppendLine("    {");
        sb.AppendLine("        paddle.gameObject.SetActive(false);");
        sb.AppendLine("        blueBarObject.SetActive(true);");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"You've Broken Free!\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Setup UI elements");
        sb.AppendLine("        UpdateScoreUI();");
        sb.AppendLine("        UpdateLifeUI();");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SpawnBall()");
        sb.AppendLine("    {");
        sb.AppendLine("        ball.gameObject.SetActive(true);");
        sb.AppendLine("        ball.transform.position = ballSpawnPoint.transform.position;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void StartGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Count the bricks");
        sb.AppendLine("        numBricks = bricksObject.transform.childCount;");
        sb.AppendLine("        // Activate the play button");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        // Activate the paddle");
        sb.AppendLine("        paddle.gameObject.SetActive(true);");
        sb.AppendLine("        // Wait a short time before spawning ball");
        sb.AppendLine("        StartCoroutine(WaitToRespawnBall(ballSpawnDelay));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"WallBreaker2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateLifeUI()");
        sb.AppendLine("    {");
        sb.AppendLine("        lifeText.text = \"Life: \" + lifeCount;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateScoreUI()");
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
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToRespawnBall(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait...");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        // After that, spawn the ball");
        sb.AppendLine("        SpawnBall();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("WB2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteWB2DPaddleScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1423);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class WB2DPaddle : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float speed = 8.0f;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Control the paddle's movement");
        sb.AppendLine("        Vector2 inputVector = input.Get<Vector2>();");
        sb.AppendLine("        if (Mathf.Abs(inputVector.x) > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(inputVector.x * speed, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = new Vector2(0.0f, 0.0f);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("WB2DPaddle", scriptsPath, sb.ToString());
    }
}
