using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;

public class RocketDocker2D : Editor
{
    private const string templateName = "RocketDocker2D";
    private const string templateSpacedName = "Rocket Docker 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "RD2D";
    private const int textureSize = 32;
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

    enum SpritePath
    {
        Docker = 0,
        FlameSmall,
        FlameMedium,
        FlameLarge,
        Terrain,
        Zone2X,
        Zone3X,
        Zone4X,
        Zone5X,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];
    enum TilePath
    {
        Terrain = 0,
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
        ScriptUtilities.RemoveTag(scriptPrefix + "RoughTerrain");
        ScriptUtilities.RemoveTag(scriptPrefix + "DockingZone");
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
        // Lay out level
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
        GameObject mainCameraObject = GameObject.Find("Main Camera");
        Camera camera = mainCameraObject.GetComponent<Camera>();
        camera.backgroundColor = Color.black;
        camera.orthographicSize = 50.0f;
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags
        ScriptUtilities.CreateTag(scriptPrefix + "RoughTerrain");
        ScriptUtilities.CreateTag(scriptPrefix + "DockingZone");
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
        Color dockerWhite = new Color(228.0f * d, 228.0f * d, 228.0f * d);
        Color thrusterFlame = new Color(255.0f * d, 69.0f * d, 0.0f * d);
        Color terrainColor = new Color(92.0f * d, 28.0f * d, 28.0f * d);
        Color zone2X = new Color(248.0f * d, 222.0f * d, 126.0f * d);
        Color zone3X = new Color(118.0f * d, 186.0f * d, 27.0f * d);
        Color zone4X = new Color(100.0f * d, 158.0f * d, 255.0f * d);
        Color zone5X = new Color(172.0f * d, 79.0f * d, 198.0f * d);

        // Create docker texture
        path = ContentUtilities.CreateTexture2DTriangleAsset("docker_texture", texturesPath, 2 * textureSize, 3 * textureSize, dockerWhite);
        spritePaths[(int)SpritePath.Docker] = path;
        // Create thruster flame textures
        path = ContentUtilities.CreateTexture2DTriangleAsset("flame_texture_small", texturesPath, textureSize, textureSize, thrusterFlame);
        spritePaths[(int)SpritePath.FlameSmall] = path;
        path = ContentUtilities.CreateTexture2DTriangleAsset("flame_texture_medium", texturesPath, textureSize, 2 * textureSize, thrusterFlame);
        spritePaths[(int)SpritePath.FlameMedium] = path;
        path = ContentUtilities.CreateTexture2DTriangleAsset("flame_texture_large", texturesPath, textureSize, 3 * textureSize, thrusterFlame);
        spritePaths[(int)SpritePath.FlameLarge] = path;
        // Terrain
        path = ContentUtilities.CreateTexture2DRectangleAsset("terrain_texture", texturesPath, textureSize, textureSize, terrainColor);
        spritePaths[(int)SpritePath.Terrain] = path;
        // Docking zone
        path = ContentUtilities.CreateTexture2DRectangleAsset("zone2x_texture", texturesPath, textureSize, textureSize, zone2X);
        spritePaths[(int)SpritePath.Zone2X] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("zone3x_texture", texturesPath, textureSize, textureSize, zone3X);
        spritePaths[(int)SpritePath.Zone3X] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("zone4x_texture", texturesPath, textureSize, textureSize, zone4X);
        spritePaths[(int)SpritePath.Zone4X] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("zone5x_texture", texturesPath, textureSize, textureSize, zone5X);
        spritePaths[(int)SpritePath.Zone5X] = path;
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

        // Create a Spawn Point
        newObject = new GameObject("SpawnPoint");
        newObject.transform.position = new Vector2(0.0f, 40.0f);

        // Assemble Docker Unit
        AssembleDockerUnit();

        // Create Docking Zones
        CreateDockingZone("DockingZone2X", spritePaths[(int)SpritePath.Zone2X], 6.0f);
        CreateDockingZone("DockingZone3X", spritePaths[(int)SpritePath.Zone3X], 5.0f);
        CreateDockingZone("DockingZone4X", spritePaths[(int)SpritePath.Zone4X], 4.0f);
        CreateDockingZone("DockingZone5X", spritePaths[(int)SpritePath.Zone5X], 3.0f);
    }

    private static void GenerateTileMap()
    {
        string terrainTileAssetPath;

        // Create tile asset
        terrainTileAssetPath = ContentUtilities.CreateTileAsset("terrain_tile", spritePaths[(int)SpritePath.Terrain], tilesPath);
        tilePaths[(int)TilePath.Terrain] = terrainTileAssetPath;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        // Ground layer
        GameObject tilemapObject;
        tilemapObject = ContentUtilities.CreateTilemapObject("RoughTerrain");
        tilemapObject.tag = scriptPrefix + "RoughTerrain";
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder = 0;
        Rigidbody2D rb = tilemapObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        TilemapCollider2D collider2D = tilemapObject.AddComponent<TilemapCollider2D>();
        tilemapObject.AddComponent<CompositeCollider2D>();
        collider2D.usedByComposite = true;

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        Tile tile;
        // 0 - terrain
        // Ground
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Terrain]);
        paletteTilemap.SetTile(Vector3Int.zero, tile);

        // ... Add more tiles if needed
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

        // Create fuel text panel
        float offsetY = -h; 
        GameObject fuelTextPanel = ContentUtilities.CreateUIBackgroundObject("FuelTextPanel", w, h);
        ContentUtilities.AnchorUIObject(fuelTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create fuel text
        GameObject fuelTextObject = ContentUtilities.CreateUITextObject("FuelText", w - margin, h, "Fuel: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(fuelTextObject, fuelTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        w = 200.0f;
        // Create altitude text panel
        GameObject altitudeTextPanel = ContentUtilities.CreateUIBackgroundObject("AltitudeTextPanel", w, h);
        ContentUtilities.AnchorUIObject(altitudeTextPanel, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin));
        // Create altitude text
        GameObject altitudeTextObject = ContentUtilities.CreateUITextObject("AltitudeText", w - margin, h, "Altitude: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(altitudeTextObject, altitudeTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create horizontal speed text panel
        offsetY = -h;
        GameObject horizontalSpeedTextPanel = ContentUtilities.CreateUIBackgroundObject("HorizontalSpeedTextPanel", w, h);
        ContentUtilities.AnchorUIObject(horizontalSpeedTextPanel, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin + offsetY));
        // Create horizontal speed text
        GameObject horizontalSpeedTextObject = ContentUtilities.CreateUITextObject("HorizontalSpeedText", w - margin, h, "H. Speed: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(horizontalSpeedTextObject, horizontalSpeedTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create vertical speed text panel
        offsetY = -h * 2;
        GameObject verticalSpeedTextPanel = ContentUtilities.CreateUIBackgroundObject("VerticalSpeedTextPanel", w, h);
        ContentUtilities.AnchorUIObject(verticalSpeedTextPanel, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin + offsetY));
        // Create vertical speed text
        GameObject verticalSpeedTextObject = ContentUtilities.CreateUITextObject("VerticalSpeedText", w - margin, h, "V. Speed: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(verticalSpeedTextObject, verticalSpeedTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        string text = "Great Docking\nFuel +1000\nScore +750";
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, text, TextAnchor.MiddleCenter, fontSize, Color.white);
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
        sb.AppendLine("Use Up Arrow or the W Keys to Activate the Main Thruster");
        sb.AppendLine("Use the Left / Right Arrows or the A / D Keys for the Side Thrusters");
        sb.AppendLine("Aim for the Colored Docking Pads ");
        sb.AppendLine("Control the Docking Speed!");
        GameObject helpPanelTextObject = ContentUtilities.CreateUITextObject("Text", w - margin * 2, h, sb.ToString(), TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(helpPanelTextObject, helpPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create press any key text
        w = 200;
        h = 40;
        GameObject pressAnyKeyTextObject = ContentUtilities.CreateUITextObject("PressAnyKeyText", w, h, "Press Any Key", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(pressAnyKeyTextObject, parent, ContentUtilities.Anchor.Bottom, new Vector2(0.0f, margin));
    }

    private static void GenerateScripts()
    {
        WriteRD2DBlinkTextScriptToFile();
        WriteRD2DGameManagerScriptToFile();
        WriteRD2DDockerUnitScriptToFile();
        WriteRD2DDockingZoneScriptToFile();
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "GameManager", "DockerUnit", "DockingZone" }))
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
        GameObject dockerUnitPrefab = ContentUtilities.LoadPrefab("DockerUnit", prefabsPath);
        GameObject dockingZone2XPrefab = ContentUtilities.LoadPrefab("DockingZone2X", prefabsPath);
        GameObject dockingZone3XPrefab = ContentUtilities.LoadPrefab("DockingZone3X", prefabsPath);
        GameObject dockingZone4XPrefab = ContentUtilities.LoadPrefab("DockingZone4X", prefabsPath);
        GameObject dockingZone5XPrefab = ContentUtilities.LoadPrefab("DockingZone5X", prefabsPath);

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DockerUnit", dockerUnitPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DockingZone", dockingZone2XPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DockingZone", dockingZone3XPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DockingZone", dockingZone4XPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DockingZone", dockingZone5XPrefab);

        // Assign parameters
        AssignDockingZoneParameters(dockingZone2XPrefab, 2.0f, 125.0f);
        AssignDockingZoneParameters(dockingZone3XPrefab, 3.0f, 250.0f);
        AssignDockingZoneParameters(dockingZone4XPrefab, 4.0f, 500.0f);
        AssignDockingZoneParameters(dockingZone5XPrefab, 5.0f, 1000.0f);

        // Instantiate objects
        InstantiateAndSetupDockerUnit(dockerUnitPrefab);
        InstantiateAndSetupGameManager(gameManagerPrefab);
        
        // Clean up
        EditorPrefs.DeleteKey(prefKey);
        // Save
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        // Notify builder
        ScriptUtilities.NotifyBuildComplete(templateName);
    }

    private static void AssembleDockerUnit()
    {
        // Create the docker unit body
        string path = spritePaths[(int)SpritePath.Docker];
        ContentUtilities.ColliderShape shape = ContentUtilities.ColliderShape.Polygon;
        GameObject dockerUnit = ContentUtilities.CreateTexturedBody("DockerUnit", 0.0f, 0.0f, path, shape);
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        dockerUnit.AddComponent<PlayerInput>().actions = asset;
        dockerUnit.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Configure physics
        Rigidbody2D rb = dockerUnit.GetComponent<Rigidbody2D>();
        rb.gravityScale = 1.0f / 6.0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Transform parent = dockerUnit.transform;
        // Create small thruster flame
        path = spritePaths[(int)SpritePath.FlameSmall];
        GameObject thrusterFlameSmall = ContentUtilities.CreateTexturedFigment("ThrusterFlameSmall", 0.0f, -2.0f, path);
        thrusterFlameSmall.transform.SetParent(parent);
        thrusterFlameSmall.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
        thrusterFlameSmall.SetActive(false);
        // Create medium thruster flame
        path = spritePaths[(int)SpritePath.FlameMedium];
        GameObject thrusterFlameMedium = ContentUtilities.CreateTexturedFigment("ThrusterFlameMedium", 0.0f, -2.5f, path);
        thrusterFlameMedium.transform.SetParent(parent);
        thrusterFlameMedium.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
        thrusterFlameMedium.SetActive(false);
        // Create big thruster flame
        path = spritePaths[(int)SpritePath.FlameLarge];
        GameObject thrusterFlameBig = ContentUtilities.CreateTexturedFigment("ThrusterFlameBig", 0.0f, -3.0f, path);
        thrusterFlameBig.transform.SetParent(parent);
        thrusterFlameBig.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f);
        thrusterFlameBig.SetActive(false);

        dockerUnit.transform.position = new Vector2(0.0f, 40.0f);
        ContentUtilities.CreatePrefab(dockerUnit, prefabsPath);
    }

    private static void AssignDockingZoneParameters(GameObject prefab, float scoreMultiplier, float fuel)
    {
        string className = scriptPrefix + "DockingZone";
        ScriptUtilities.AssignFloatFieldToObject(scoreMultiplier, prefab, className, "scoreMultiplier");
        ScriptUtilities.AssignFloatFieldToObject(fuel, prefab, className, "fuel");
    }

    private static void CreateDockingZone(string name, string SpritePath, float scale)
    {
        GameObject newObject = ContentUtilities.CreateTexturedBody(name, 0.0f, 0.0f, SpritePath);
        newObject.tag = scriptPrefix + "DockingZone";
        newObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        newObject.transform.localScale = new Vector3(scale, 1.0f, 1.0f);
        ContentUtilities.CreatePrefab(newObject, prefabsPath);
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject dockerUnitObject = GameObject.Find("DockerUnit");
        GameObject spawnPointObject = GameObject.Find("SpawnPoint");
        GameObject playButtonObject = GameObject.Find("PlayButton");
        GameObject scoreTextObject = GameObject.Find("ScoreText");
        GameObject fuelTextObject = GameObject.Find("FuelText");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject altitudeTextObject = GameObject.Find("AltitudeText");
        GameObject horizontalSpeedTextObject = GameObject.Find("HorizontalSpeedText");
        GameObject verticalSpeedTextObject = GameObject.Find("VerticalSpeedText");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(dockerUnitObject, scriptPrefix + "DockerUnit", go, className, "dockerUnit");
        ScriptUtilities.AssignObjectFieldToObject(spawnPointObject, go, className, "spawnPoint");
        ScriptUtilities.AssignComponentFieldToObject(playButtonObject, "Button", go, className, "playButton");
        ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
        ScriptUtilities.AssignComponentFieldToObject(fuelTextObject, "Text", go, className, "fuelText");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(altitudeTextObject, "Text", go, className, "altitudeText");
        ScriptUtilities.AssignComponentFieldToObject(horizontalSpeedTextObject, "Text", go, className, "horizontalSpeedText");
        ScriptUtilities.AssignComponentFieldToObject(verticalSpeedTextObject, "Text", go, className, "verticalSpeedText");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
    }

    private static void InstantiateAndSetupDockerUnit(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "DockerUnit";

        // Get objects
        GameObject flameSmall = go.transform.GetChild(0).gameObject;
        GameObject flameMedium = go.transform.GetChild(1).gameObject;
        GameObject flameLarge = go.transform.GetChild(2).gameObject;
        // Assign objects
        ScriptUtilities.AssignObjectFieldToObject(flameSmall, go, className, "flameSmall");
        ScriptUtilities.AssignObjectFieldToObject(flameMedium, go, className, "flameMedium");
        ScriptUtilities.AssignObjectFieldToObject(flameLarge, go, className, "flameLarge");
    }

    private static void LayoutLevel()
    {
        int[] heightMap =
        {
         -24,  -23,  -23,  -23,  -22,  -22,  -21,  -20,  -19,  -17,  -16,  -15,  -14,  -13,  -12,  -10,   -8,   -6,   -3,   -2,
          -1,    0,    0,    1,    1,    2,    2,    2,    2,    2,    2,    2,    1,    1,    1,    0,    0,   -1,   -2,   -2,
          -5,   -7,  -13,  -15,  -18,  -18,  -18,  -18,  -18,  -18,  -18,  -20,  -24,  -26,  -28,  -29,  -30,  -31,  -32,  -33,
         -33,  -34,  -34,  -34,  -34,  -33,  -33,  -33,  -32,  -31,  -30,  -29,  -28,  -24,  -17,  -14,  -12,   -9,   -7,   -5,
          -2,   -1,    1,    2,    3,    5,    6,    6,    6,    6,    6,    6,    6,    6,    6,    6,    6,    5,    5,    4,
           3,    0,    0,   -2,   -3,   -5,   -9,  -10,  -10,  -11,  -12,  -13,  -15,  -15,  -16,  -17,  -21,  -23,  -26,  -27,
         -29,  -29,  -29,  -28,  -27,  -26,  -24,  -22,  -21,  -20,  -20,  -20,  -20,  -20,  -20,  -20,  -21,  -21,  -21,  -21,
         -21,  -21,  -21,  -21,  -21,  -20,  -18,  -16,  -15,  -15,  -14,  -12,  -11,  -10,   -9,   -8,   -7,   -6,   -6,   -5,
          -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -5,   -4,   -3,   -2,   -2,   -2,
          -3,   -3,   -2,   -1,    0,    1,    2,    4,    6,    7,    8,   10,   10,   11,   12,   14,   14,   15,   15,   15,
          16,   16,   17,   18
        };

        // Lay out the tile map
        LayoutTilemap(heightMap);

        // Lay out the docking zones
        GameObject zone2XPrefab = ContentUtilities.LoadPrefab("DockingZone2X", prefabsPath);
        GameObject zone3XPrefab = ContentUtilities.LoadPrefab("DockingZone3X", prefabsPath);
        GameObject zone4XPrefab = ContentUtilities.LoadPrefab("DockingZone4X", prefabsPath);
        GameObject zone5XPrefab = ContentUtilities.LoadPrefab("DockingZone5X", prefabsPath);
        LayoutZone(zone2XPrefab, 64.0f, -3.5f);
        LayoutZone(zone2XPrefab, -12.0f, 7.5f);
        LayoutZone(zone3XPrefab, -55.5f, -16.5f);
        LayoutZone(zone3XPrefab, 38.5f, -19.5f);
        LayoutZone(zone4XPrefab, -39.0f, -32.5f);
        LayoutZone(zone5XPrefab, 19.5f, -27.5f);
    }

    private static void LayoutTilemap(int[] mapArray)
    {
        // Draw tiles as directed by a height map array
        int startX = -102;
        int startY = -50;
        int endX = 102;
        int endY = 50;
        Tilemap tilemap = GameObject.Find("RoughTerrain").GetComponent<Tilemap>();
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Terrain]);
        Vector3Int marker = Vector3Int.zero;
        for (int x = startX, index = 0; x < endX; x++, index++)
        {
            marker.x = x;
            for (int y = startY; y <= mapArray[index] && y < endY; y++)
            {
                marker.y = y;
                tilemap.SetTile(marker, tile);
            }
        }
    }

    private static void LayoutZone(GameObject zonePrefab, float posX, float posY)
    {
        GameObject zoneObject = PrefabUtility.InstantiatePrefab(zonePrefab) as GameObject;
        zoneObject.transform.position = new Vector2(posX, posY);
    }

    //[MenuItem("Templates/" + templateSpacedName + "/Reverse Engineer")]
    private static void ReverseEngineer()
    {
        // Note: the scripts and tilemaps must exist before this function is called
        ReverseEngineerScripts();
        ReverseEngineerTilemaps();
    }

    private static void ReverseEngineerScripts()
    {
        Debug.Log("Stringified scripts!");
        // Make sure the scripts exist or these calls will trigger an error
        ScriptUtilities.ConvertScriptToStringBuilder("RD2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RD2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RD2DDockerUnit", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("RD2DDockingZone", scriptsPath);
        AssetDatabase.Refresh();
    }

    private static void ReverseEngineerTilemaps()
    {
        // Can't use the ContentUtilities method because the map is too large
        // Use the following algorithm instead:
        // For each column from left to right
        // Check for tile height
        // Store result in a height map array
        // Print out in row of 20 so it is tidier

        // Scan camera area to create height map
        int startX = -102;
        int startY = -50;
        int endX = 102;
        int endY = 50;
        Tilemap tilemap = GameObject.Find("RoughTerrain").GetComponent<Tilemap>();
        int[] heightMap = new int[endX - startX];
        Vector3Int marker = Vector3Int.zero;
        int index = 0;
        for (int x = startX; x < endX; x++, index++)
        {
            marker.x = x;
            for (int y = startY; y < endY; y++)
            {
                marker.y = y;
                Tile tile = tilemap.GetTile(marker) as Tile;
                if (tile != null)
                {
                    heightMap[index] = y;
                }
            }
        }

        // Print out the height map
        StringBuilder sb = new StringBuilder();
        sb.Append("Height Map:");
        for (int i = 0; i < heightMap.Length; i++)
        {
            if (i % 20 == 0)
            {
                sb.Append("\n");
            }
            sb.Append(string.Format("{0,4:D}", heightMap[i]));
            sb.Append(", ");
        }
        Debug.Log(sb.ToString());
    }

    private static void WriteRD2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class RD2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("RD2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteRD2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(11020);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class RD2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float displayMultiplier = 100.0f;");
        sb.AppendLine("    public RD2DDockerUnit dockerUnit;");
        sb.AppendLine("    public GameObject spawnPoint;");
        sb.AppendLine("    public Text scoreText;");
        sb.AppendLine("    public Text fuelText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Text altitudeText;");
        sb.AppendLine("    public Text horizontalSpeedText;");
        sb.AppendLine("    public Text verticalSpeedText;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Button playButton;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    private Rigidbody2D dockerUnitRb;");
        sb.AppendLine("    private int currentScore = 0;");
        sb.AppendLine("");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static RD2DGameManager sharedInstance = null;");
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
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Start the game");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            StartCoroutine(WaitToActivateDockerUnit(0.5f));");
        sb.AppendLine("            ResetGame();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateKinematicStatsText();");
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
        sb.AppendLine("");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Game Over!\";");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return gameStarted;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyCrash(string comment)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (dockerUnit.FuelRemained > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            resultPanelObject.SetActive(true);");
        sb.AppendLine("            resultText.text = comment;");
        sb.AppendLine("            ResetGame();");
        sb.AppendLine("            StartCoroutine(WaitToActivateDockerUnit(3.0f));");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyFuelChanged()");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdateFuelText();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifySuccessfulDocking(string comment, int score, int fuel)");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = comment + \"\\nFuel +\" + fuel + \"\\nScore +\" + score;");
        sb.AppendLine("        AddScore(score);");
        sb.AppendLine("        ResetGame();       ");
        sb.AppendLine("        StartCoroutine(WaitToActivateDockerUnit(3.0f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        dockerUnit.gameObject.SetActive(true);");
        sb.AppendLine("        dockerUnit.Respawn(spawnPoint.transform.position);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        dockerUnitRb = dockerUnit.GetComponent<Rigidbody2D>();");
        sb.AppendLine("        playButton.onClick.AddListener(TaskOnPlayButtonClick);");
        sb.AppendLine("        playButton.gameObject.SetActive(false);");
        sb.AppendLine("        UpdateScoreText();");
        sb.AppendLine("        UpdateFuelText();");
        sb.AppendLine("        UpdateKinematicStatsText();");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnPlayButtonClick()");
        sb.AppendLine("    {");
        sb.AppendLine("        SceneManager.LoadScene(\"RocketDocker2D\");");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateFuelText()");
        sb.AppendLine("    {");
        sb.AppendLine("        int fuelLevel = (int)dockerUnit.FuelRemained;");
        sb.AppendLine("        fuelText.text = \"Fuel: \" + fuelLevel;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateKinematicStatsText()");
        sb.AppendLine("    {");
        sb.AppendLine("        const float offsetY = 50.0f;");
        sb.AppendLine("        float altitude = (offsetY + dockerUnit.transform.position.y) * displayMultiplier;");
        sb.AppendLine("        altitudeText.text = \"Altitude: \" + (int)altitude;");
        sb.AppendLine("");
        sb.AppendLine("        float hSpeed = dockerUnitRb.velocity.x * displayMultiplier;");
        sb.AppendLine("        horizontalSpeedText.text = \"H. Speed: \" + (int)hSpeed;");
        sb.AppendLine("");
        sb.AppendLine("        float vSpeed = dockerUnitRb.velocity.y * displayMultiplier;");
        sb.AppendLine("        verticalSpeedText.text = \"V. Speed: \" + (int)vSpeed;");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void UpdateScoreText()");
        sb.AppendLine("    {");
        sb.AppendLine("        scoreText.text = \"Score: \" + currentScore;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToActivateDockerUnit(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        dockerUnit.Activate();");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RD2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteRD2DDockerUnitScriptToFile()
    {
        StringBuilder sb = new StringBuilder(13031);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class RD2DDockerUnit : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject flameSmall;");
        sb.AppendLine("    public GameObject flameMedium;");
        sb.AppendLine("    public GameObject flameLarge;");
        sb.AppendLine("    public float minThrust = 1.0f;");
        sb.AppendLine("    public float maxThrust = 10.0f;");
        sb.AppendLine("    public float thrustIncrement = 4.0f;");
        sb.AppendLine("    public float startFuel = 10000;");
        sb.AppendLine("    public float horizontalBound = 95.0f;");
        sb.AppendLine("    public float verticalBound = 45.0f;");
        sb.AppendLine("    public float maxSpeed = 10.0f;");
        sb.AppendLine("    private RD2DGameManager gameManager;");
        sb.AppendLine("    private Rigidbody2D rb;");
        sb.AppendLine("    private float thrustValue;");
        sb.AppendLine("    private float fuelRemained;");
        sb.AppendLine("    private float fuelConsumptionFactor = 0.1f;");
        sb.AppendLine("    private bool thrustOn = false;");
        sb.AppendLine("    private Quaternion unTilted = Quaternion.identity;");
        sb.AppendLine("    private Quaternion leftTilted = Quaternion.Euler(new Vector3(0.0f, 0.0f, 45.0f));");
        sb.AppendLine("    private Quaternion rightTilted = Quaternion.Euler(new Vector3(0.0f, 0.0f, -45.0f));");
        sb.AppendLine("");
        sb.AppendLine("    public float FuelRemained { get => fuelRemained; set => fuelRemained = value; }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = RD2DGameManager.sharedInstance;");
        sb.AppendLine("        rb = GetComponent<Rigidbody2D>();");
        sb.AppendLine("        rb.isKinematic = true;");
        sb.AppendLine("        fuelRemained = startFuel;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (thrustOn == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            ApplyThrust();");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateFlames();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void FixedUpdate()");
        sb.AppendLine("    {");
        sb.AppendLine("        EnforceConstraints();");
        sb.AppendLine("        if (thrustValue <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        rb.AddForce(transform.up * thrustValue);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void OnCollisionEnter2D(Collision2D collision)");
        sb.AppendLine("    {");
        sb.AppendLine("        GameObject collidedObject = collision.gameObject;");
        sb.AppendLine("        if (collidedObject.CompareTag(\"RD2DRoughTerrain\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.NotifyCrash(\"You docked on rough terrain!\");");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (collidedObject.CompareTag(\"RD2DDockingZone\"))");
        sb.AppendLine("        {");
        sb.AppendLine("            collidedObject.GetComponent<RD2DDockingZone>().HandleDocking(gameObject, collision.relativeVelocity);");
        sb.AppendLine("        }");
        sb.AppendLine("        Restart();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (rb.isKinematic)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateControls(input.Get<Vector2>());");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Activate()");
        sb.AppendLine("    {");
        sb.AppendLine("        rb.isKinematic = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddFuel(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        fuelRemained += value;");
        sb.AppendLine("        if (fuelRemained > startFuel)");
        sb.AppendLine("        {");
        sb.AppendLine("            fuelRemained = startFuel;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ApplyThrust()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If fuel runs out");
        sb.AppendLine("        if (fuelRemained <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            // No more thrust");
        sb.AppendLine("            thrustValue = 0.0f;");
        sb.AppendLine("            transform.rotation = unTilted;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If thrust value is less than minimum");
        sb.AppendLine("        if (thrustValue < minThrust)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Set it to minimum");
        sb.AppendLine("            thrustValue = minThrust;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Apply thrust incrementally");
        sb.AppendLine("        thrustValue += thrustIncrement * Time.deltaTime;");
        sb.AppendLine("        thrustValue = Mathf.Min(thrustValue, maxThrust);");
        sb.AppendLine("        // Consumes fuel");
        sb.AppendLine("        fuelRemained -= (thrustValue * fuelConsumptionFactor);");
        sb.AppendLine("        if (fuelRemained < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            fuelRemained = 0.0f;");
        sb.AppendLine("        }");
        sb.AppendLine("        gameManager.NotifyFuelChanged();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void EnforceConstraints()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Constrain speed");
        sb.AppendLine("        if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)");
        sb.AppendLine("        {");
        sb.AppendLine("            rb.velocity = rb.velocity.normalized * maxSpeed;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Constrain position");
        sb.AppendLine("        Vector2 myPos = transform.position;");
        sb.AppendLine("        if (myPos.x < -horizontalBound)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.x = -horizontalBound;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (myPos.x > horizontalBound)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.x = horizontalBound;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (myPos.y < -verticalBound)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.y = -verticalBound;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (myPos.y > verticalBound)");
        sb.AppendLine("        {");
        sb.AppendLine("            myPos.y = verticalBound;");
        sb.AppendLine("        }");
        sb.AppendLine("        transform.position = myPos;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Restart()");
        sb.AppendLine("    {");
        sb.AppendLine("        transform.rotation = unTilted;");
        sb.AppendLine("        thrustValue = 0.0f;");
        sb.AppendLine("        rb.isKinematic = true;");
        sb.AppendLine("        rb.velocity = Vector2.zero;");
        sb.AppendLine("        rb.angularVelocity = 0.0f;");
        sb.AppendLine("        thrustOn = false;");
        sb.AppendLine("        UpdateFlames();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Respawn(Vector2 position)");
        sb.AppendLine("    {");
        sb.AppendLine("        transform.position = position;");
        sb.AppendLine("        GetComponent<Rigidbody2D>().isKinematic = true;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateControls(Vector2 input)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (fuelRemained <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            thrustOn = false;");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (input.y != 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            thrustOn = true;");
        sb.AppendLine("            transform.rotation = unTilted;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (input.x > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            thrustOn = true;");
        sb.AppendLine("            transform.rotation = rightTilted;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (input.x < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            thrustOn = true;");
        sb.AppendLine("            transform.rotation = leftTilted;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            thrustOn = false;");
        sb.AppendLine("            thrustValue = 0.0f;");
        sb.AppendLine("            transform.rotation = unTilted;");
        sb.AppendLine("        }        ");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateFlames()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Pick which sprite(s) is active depending on the thruster value");
        sb.AppendLine("        if (fuelRemained <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            flameLarge.SetActive(false);");
        sb.AppendLine("            flameMedium.SetActive(false);");
        sb.AppendLine("            flameSmall.SetActive(false);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        if (thrustValue > 0.7f * maxThrust)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (flameSmall.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameSmall.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameMedium.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameMedium.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (!flameLarge.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameLarge.SetActive(true);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (thrustValue > 0.3f * maxThrust)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (flameSmall.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameSmall.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (!flameMedium.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameMedium.SetActive(true);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameLarge.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameLarge.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (thrustValue > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!flameSmall.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameSmall.SetActive(true);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameMedium.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameMedium.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameLarge.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameLarge.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            if (flameSmall.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameSmall.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameMedium.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameMedium.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (flameLarge.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                flameLarge.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RD2DDockerUnit", scriptsPath, sb.ToString());
    }

    private static void WriteRD2DDockingZoneScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4771);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class RD2DDockingZone : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float scoreMultiplier = 2.0f;");
        sb.AppendLine("    public float fuel = 125.0f;");
        sb.AppendLine("    public float speedThreshold = 3.0f;");
        sb.AppendLine("    private RD2DGameManager gameManager;");
        sb.AppendLine("    private float maxScore = 100.0f;");
        sb.AppendLine("    private float halfWidth;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = RD2DGameManager.sharedInstance;");
        sb.AppendLine("        halfWidth = GetComponent<BoxCollider2D>().size.x * 0.5f * transform.localScale.x;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float CalculateBaseScore(float speed, float offset)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate base score for the docking ");
        sb.AppendLine("        float halfScore = maxScore * 0.5f;");
        sb.AppendLine("        float stabilityScore = halfScore * Mathf.Abs((speedThreshold - speed) / speedThreshold);");
        sb.AppendLine("        float accuracyScore = halfScore * Mathf.Abs((halfWidth - Mathf.Abs(offset)) / halfWidth);");
        sb.AppendLine("        return stabilityScore + accuracyScore;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private string DetermineComment(float score)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Determine a proper feedback depending on the score the player gets");
        sb.AppendLine("        if (score > maxScore * 0.9f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return \"Perfect Docking on x\" + (int)scoreMultiplier + \" Zone\";");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (score > maxScore * 0.7f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return \"Great Docking on x\" + (int)scoreMultiplier + \" Zone\";");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (score > maxScore * 0.5f)");
        sb.AppendLine("        {");
        sb.AppendLine("            return \"Acceptable Docking on x\" + (int)scoreMultiplier + \" Zone\";");
        sb.AppendLine("        }");
        sb.AppendLine("        else ");
        sb.AppendLine("        {");
        sb.AppendLine("            return \"Poor Docking on x\" + (int)scoreMultiplier + \" Zone\";");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void HandleDocking(GameObject dockerUnit, Vector2 relativeVelocity)");
        sb.AppendLine("    {");
        sb.AppendLine("        float speed = relativeVelocity.magnitude;");
        sb.AppendLine("        // If the docker unit's speed is too much");
        sb.AppendLine("        if (speed >= speedThreshold)");
        sb.AppendLine("        {");
        sb.AppendLine("            // It is a crash!");
        sb.AppendLine("            dockerUnit.SetActive(false);");
        sb.AppendLine("            gameManager.NotifyCrash(\"You docked too hard!\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        Vector3 rotation = dockerUnit.transform.rotation.eulerAngles;");
        sb.AppendLine("        const float rotationTolerance = 5.0f;");
        sb.AppendLine("        // If the unit docked with an angle");
        sb.AppendLine("        if (Mathf.Abs(rotation.z) > rotationTolerance)");
        sb.AppendLine("        {");
        sb.AppendLine("            // It is a crash!");
        sb.AppendLine("            dockerUnit.SetActive(false);");
        sb.AppendLine("            gameManager.NotifyCrash(\"Your angle is off!\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Safely docked");
        sb.AppendLine("        Vector2 dockingPosition = dockerUnit.transform.position;");
        sb.AppendLine("        float offset = Mathf.Abs(dockingPosition.x - transform.position.x);");
        sb.AppendLine("        // Calculate score and determine a proper feedback");
        sb.AppendLine("        float score = CalculateBaseScore(speed, offset);");
        sb.AppendLine("        string comment = DetermineComment(score);");
        sb.AppendLine("        dockerUnit.GetComponent<RD2DDockerUnit>().AddFuel(fuel);");
        sb.AppendLine("        gameManager.NotifySuccessfulDocking(comment, (int)(score * scoreMultiplier), (int)fuel);");
        sb.AppendLine("        gameManager.NotifyFuelChanged();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("RD2DDockingZone", scriptsPath, sb.ToString());
    }
}
