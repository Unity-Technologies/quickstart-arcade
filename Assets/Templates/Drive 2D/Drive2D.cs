using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class Drive2D : Editor
{
    private const string templateName = "Drive2D";
    private const string templateSpacedName = "Drive 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "DR2D";
    private const int textureSize = 64;
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
        Background = 0,
        RoadLight,
        RoadDark,
        RumbleLight,
        RumbleDark,
        GrassLight,
        GrassDark,
        TreePine,
        TreeScrub,
        CarRed,
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
        // Craete and set up camera
        GameObject mainCameraObject = GameObject.Find("Main Camera");
        Camera camera = mainCameraObject.GetComponent<Camera>();
        const float size = 7.5f;
        camera.orthographicSize = size;
        camera.transform.position = new Vector3(0.0f, size, -10.0f);
        camera.backgroundColor = new Color(51.0f / 255.0f, 204.0f / 255.0f, 255.0f / 255.0f);
        // Set window->rendering->lighting settings
        const float k = 1.0f / 255.0f;
        RenderSettings.ambientLight = new Color(255.0f * k, 255.0f * k, 255.0f * k);
    }

    private static void GenerateAssets()
    {
        GenerateInputActions();
        GenerateTextures();
        AssetDatabase.Refresh();
        PostProcessTextures();
        AssetDatabase.Refresh();
        GenerateMaterials();
        AssetDatabase.Refresh();
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
        action.AddBinding(new InputBinding("<Mouse>/Press"));

        // Save asset
        string path = "Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions";
        File.WriteAllText(path, asset.ToJson());
    }

    private static void GenerateTextures()
    {
        string path;
        const float k = 1.0f / 255.0f;
        Color roadLight = new Color(107.0f * k, 107.0f * k, 107.0f * k);
        Color roadDark = new Color(105.0f * k, 105.0f * k, 105.0f * k);
        Color dividerLight = new Color(255.0f * k, 255.0f * k, 255.0f * k);
        Color dividerDark = new Color(0.0f * k, 0.0f * k, 0.0f * k);
        Color grassLight = new Color(16.0f * k, 200.0f * k, 16.0f * k);
        Color grassDark = new Color(0.0f * k, 154.0f * k, 0.0f * k);
        Color treeGreen = new Color(0.0f * k, 98.0f * k, 0.0f * k);

        // Generate textures
        const int w = textureSize;
        const int h = textureSize;
        // road
        path = ContentUtilities.CreateTexture2DRectangleAsset("road_light_texture", texturesPath, w, h, roadLight);
        spritePaths[(int)SpritePath.RoadLight] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("road_dark_texture", texturesPath, w, h, roadDark);
        spritePaths[(int)SpritePath.RoadDark] = path;
        // divider
        path = ContentUtilities.CreateTexture2DRectangleAsset("divider_light_texture", texturesPath, w, h, dividerLight);
        spritePaths[(int)SpritePath.RumbleLight] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("divider_dark_texture", texturesPath, w, h, dividerDark);
        spritePaths[(int)SpritePath.RumbleDark] = path;
        // grass
        path = ContentUtilities.CreateTexture2DRectangleAsset("grass_light_texture", texturesPath, w, h, grassLight);
        spritePaths[(int)SpritePath.GrassLight] = path;
        path = ContentUtilities.CreateTexture2DRectangleAsset("grass_dark_texture", texturesPath, w, h, grassDark);
        spritePaths[(int)SpritePath.GrassDark] = path;
        // tree
        path = ContentUtilities.CreateTexture2DTriangleAsset("tree_pine_texture", texturesPath, w, h * 4, treeGreen);
        spritePaths[(int)SpritePath.TreePine] = path;
        // bush
        path = ContentUtilities.CreateTexture2DTriangleAsset("tree_scrub_texture", texturesPath, w * 2, h * 2, treeGreen);
        spritePaths[(int)SpritePath.TreeScrub] = path;

        // Generate car texture
        path = ContentUtilities.CreateTexture2DOctagonAsset("car_red_texture", texturesPath, w * 4, h * 2, Color.red);
        spritePaths[(int)SpritePath.CarRed] = path;

        // Create BG texture
        CreateBgTexture();
    }

    private static void GenerateMaterials()
    {
        Material material;
        Texture texture;
        string pathOnly = "Assets/" + texturesPath + "/";

        // Dark grass material
        material = new Material(Shader.Find("Standard"));
        material.name = "DarkGrassMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.GrassDark], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "DarkGrassMaterial.mat");
        // Dark rumble material
        material = new Material(Shader.Find("Standard"));
        material.name = "DarkRumbleMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.RumbleDark], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "DarkRumbleMaterial.mat");
        // Dark road material
        material = new Material(Shader.Find("Standard"));
        material.name = "DarkRoadMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.RoadDark], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "DarkRoadMaterial.mat");

        // Light grass material
        material = new Material(Shader.Find("Standard"));
        material.name = "LightGrassMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.GrassLight], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "LightGrassMaterial.mat");
        // Light rumble material
        material = new Material(Shader.Find("Standard"));
        material.name = "LightRumbleMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.RumbleLight], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "LightRumbleMaterial.mat");
        // Light road material
        material = new Material(Shader.Find("Standard"));
        material.name = "LightRoadMaterial";
        texture = AssetDatabase.LoadAssetAtPath("Assets/" + spritePaths[(int)SpritePath.RoadLight], typeof(Texture)) as Texture;
        material.SetTexture("_MainTex", texture);
        AssetDatabase.CreateAsset(material, pathOnly + "LightRoadMaterial.mat");
    }

    private static void GenerateObjects()
    {
        GameObject newObject;
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;

        // Create Background
        Vector2 camPos = GameObject.Find("Main Camera").transform.position;
        string texturePath = spritePaths[(int)SpritePath.Background];
        newObject = ContentUtilities.CreateTexturedFigment("Background", 0.0f, 0.0f, texturePath);
        newObject.transform.localScale = new Vector3(8.0f, 2.0f, 2.0f);
        newObject.transform.position = new Vector3(0.0f, camPos.y, 990.0f);

        // Create the game manager object
        newObject = new GameObject("GameManager");
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "UI";
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create the projector object
        newObject = new GameObject("RoadProjector");
        // Add input
        newObject.AddComponent<PlayerInput>().actions = asset;
        newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Make into prefab
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create the road segment
        AssembleRoadSegment("Dark");
        AssembleRoadSegment("Light");

        // Create trees
        // Pine
        newObject = ContentUtilities.CreateTexturedFigment("TreePine", 0.0f, 0.0f, spritePaths[(int)SpritePath.TreePine]);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        // Scrub
        newObject = ContentUtilities.CreateTexturedFigment("TreeScrub", 0.0f, 0.0f, spritePaths[(int)SpritePath.TreeScrub]);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create car
        newObject = ContentUtilities.CreateTexturedFigment("PlayerCar", 0.0f, 2.0f, spritePaths[(int)SpritePath.CarRed]);
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

        // Create speed text panel
        GameObject speedTextPanel = ContentUtilities.CreateUIBackgroundObject("SpeedTextPanel", w, h);
        ContentUtilities.AnchorUIObject(speedTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create speed text
        GameObject speedTextObject = ContentUtilities.CreateUITextObject("SpeedText", w - margin, h, "Speed: 999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(speedTextObject, speedTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create distance text panel
        w = 200;
        float offsetY = -h;
        GameObject distanceTextPanel = ContentUtilities.CreateUIBackgroundObject("DistanceTextPanel", w, h);
        ContentUtilities.AnchorUIObject(distanceTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
        // Create distance text
        GameObject distanceTextObject = ContentUtilities.CreateUITextObject("DistanceText", w - margin, h, "Distance: 99.99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(distanceTextObject, distanceTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create timer text panel
        GameObject timerTextPanel = ContentUtilities.CreateUIBackgroundObject("TimerTextPanel", w, h);
        ContentUtilities.AnchorUIObject(timerTextPanel, parent, ContentUtilities.Anchor.Top, new Vector2(0.0f, -margin));
        // Create timer text
        GameObject timerTextObject = ContentUtilities.CreateUITextObject("TimerText", w - margin, h, "3 : 00", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(timerTextObject, timerTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "Time's up!", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create reset button
        w = 150.0f;
        h = 40.0f;
        GameObject buttonObject = ContentUtilities.CreateUIButtonObject("ResetButton", w, h, "Reset", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(buttonObject, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin));

        // Create help panel
        w = 600.0f;
        h = 240.0f;
        GameObject helpPanelObject = ContentUtilities.CreateUIBackgroundObject("HelpPanel", w, h, 0.9f);
        ContentUtilities.AnchorUIObject(helpPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create help panel text
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Use Up / Down Arrows or W / S Keys to Control Car Speed");
        sb.AppendLine("Use Left / Right Arrows or A / D Keys to Move Car Horizontally");
        sb.AppendLine("Going Off the Road will Slow Your Car Down!");
        sb.AppendLine("How Far You Can Go Before Time's Up?");
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
        WriteDR2DBlinkTextScriptToFile();
        WriteDR2DBuilderScriptToFile();
        WriteDR2DDefaultRouteScriptToFile();
        WriteDR2DGameManagerScriptToFile();
        WriteDR2DProjectorScriptToFile();
        WriteDR2DQuadScriptToFile();
        WriteDR2DSegmentScriptToFile();
    }

    private static void GenerateTileMap()
    {
        // Generate tile map, pallette, and tile assets here...
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Builder", "DefaultRoute", "GameManager", "Projector", "Quad", "Segment" }))
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
        // Access prefabs
        GameObject gameManagerPrefab = ContentUtilities.LoadPrefab("GameManager", prefabsPath);
        GameObject roadProjectorPrefab = ContentUtilities.LoadPrefab("RoadProjector", prefabsPath);
        GameObject darkSegmentPrefab = ContentUtilities.LoadPrefab("DarkSegment", prefabsPath);
        GameObject lightSegmentPrefab = ContentUtilities.LoadPrefab("LightSegment", prefabsPath);
        GameObject treePinePrefab = ContentUtilities.LoadPrefab("TreePine", prefabsPath);
        GameObject treeScrubPrefab = ContentUtilities.LoadPrefab("TreeScrub", prefabsPath);

        // Access objects
        GameObject backgroundObject = GameObject.Find("Background");

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "DefaultRoute", roadProjectorPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Projector", roadProjectorPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Segment", darkSegmentPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Segment", lightSegmentPrefab);
        ConfigureSegmentComponents(darkSegmentPrefab, "Dark");
        ConfigureSegmentComponents(lightSegmentPrefab, "Light");

        // Assign prefabs array
        string className = scriptPrefix + "DefaultRoute";
        ScriptUtilities.AssignObjectFieldToObject(darkSegmentPrefab, roadProjectorPrefab, className, "darkPrefab");
        ScriptUtilities.AssignObjectFieldToObject(lightSegmentPrefab, roadProjectorPrefab, className, "lightPrefab");
        GameObject[] objects = { treePinePrefab, treeScrubPrefab };
        ScriptUtilities.AssignObjectsFieldToObject(objects, roadProjectorPrefab, className, "spritePrefabs");

        // Create initial objects
        InstantiateAndSetupProjector(roadProjectorPrefab);
        InstantiateAndSetupGameManager(gameManagerPrefab);

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

    private static void AssembleRoadSegment(string prefix)
    {
        // Combine into segment object
        GameObject segmentObject = new GameObject(prefix + "Segment");
        Transform parent = segmentObject.transform;

        // Create grass quad
        GameObject grassQuad = new GameObject(prefix + "GrassQuad");
        grassQuad.transform.position = new Vector3(0.0f, 0.0f, 3.0f);
        grassQuad.transform.SetParent(parent);

        // Create rumble quad
        GameObject rumbleQuad = new GameObject(prefix + "RumbleQuad");
        rumbleQuad.transform.position = new Vector3(0.0f, 0.0f, 2.0f);
        rumbleQuad.transform.SetParent(parent);

        // Create road quad
        GameObject roadQuad = new GameObject(prefix + "RoadQuad");
        roadQuad.transform.position = new Vector3(0.0f, 0.0f, 1.0f);
        roadQuad.transform.SetParent(parent);

        // Create prefab
        ContentUtilities.CreatePrefab(segmentObject, prefabsPath, true);
    }

    private static void ConfigureSegmentComponents(GameObject segmentPrefab, string type)
    {
        string path = "Assets/" + texturesPath + "/" + type;
        Material[] materials =
        {
            AssetDatabase.LoadAssetAtPath<Material>(path + "GrassMaterial.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(path + "RumbleMaterial.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(path + "RoadMaterial.mat"),
        };

        for (int i = 0; i < segmentPrefab.transform.childCount; i++)
        {
            GameObject childObject = segmentPrefab.transform.GetChild(i).gameObject;
            childObject.AddComponent<MeshFilter>();
            MeshRenderer mr = childObject.AddComponent<MeshRenderer>();
            mr.material = materials[i];
            ScriptUtilities.AttachScriptToObject(scriptPrefix + "Quad", childObject);
        }
    }

    private static void CreateBgTexture()
    {
        float k = 1.0f / 255.0f;
        int bgWidth = 16 * textureSize;
        int bgHeight = 8 * textureSize;
        int w;
        int h;
        Vector2Int drawPos = Vector2Int.zero;

        Color[] bg = ContentUtilities.FillBitmap(bgWidth, bgHeight, Color.clear);

        // Draw far mountains
        int mY = 4 * textureSize;
        Color farColor = new Color(102.0f * k, 90.0f * k, 78.0f * k);
        w = bgWidth * 3 / 8;
        h = bgHeight / 4;
        Color[] far1 = ContentUtilities.FillBitmapShapeTriangle(w, h, farColor);
        ContentUtilities.CopyBitmap(far1, w, h, bg, bgWidth, bgHeight, new Vector2Int(0, mY));

        w = bgWidth / 2;
        h = (int)((float)bgHeight * 2.70f / 8.0f);
        Color[] far2 = ContentUtilities.FillBitmapShapeTriangle(w, h, farColor);
        ContentUtilities.CopyBitmap(far2, w, h, bg, bgWidth, bgHeight, new Vector2Int(4 * textureSize, mY));

        // Draw near moutains
        Color nearColor = new Color(90.0f * k, 94.0f * k, 97.0f * k);
        w = bgWidth * 3 / 4 ;
        h = bgHeight / 4;
        Color[] near1 = ContentUtilities.FillBitmapShapeTriangle(w, h, nearColor);
        ContentUtilities.CopyBitmap(near1, w, h, bg, bgWidth, bgHeight, new Vector2Int(0, mY));

        w = bgWidth / 2;
        h = bgHeight / 8;
        Color[] near2 = ContentUtilities.FillBitmapShapeTriangle(w, h, nearColor);
        ContentUtilities.CopyBitmap(near2, w, h, bg, bgWidth, bgHeight, new Vector2Int(bgWidth / 2, mY));

        // Draw a ground strip
        Color groundColor = new Color(64.0f * k, 79.0f * k, 36.0f * k);
        w = bgWidth;
        h = bgHeight / 2;
        Color[] groundStrip = ContentUtilities.FillBitmapShapeRectangle(w, h, groundColor);
        ContentUtilities.CopyBitmap(groundStrip, w, h, bg, bgWidth, bgHeight /4, new Vector2Int(5*textureSize, 0));

        // Create texture from BG
        spritePaths[(int)SpritePath.Background] = ContentUtilities.CreateBitmapAsset("background_texture", bg, bgWidth, bgHeight, texturesPath);
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject projectorObject = GameObject.Find("RoadProjector");
        GameObject speedTextObject = GameObject.Find("SpeedText");
        GameObject distanceTextObject = GameObject.Find("DistanceText");
        GameObject timerTextObject = GameObject.Find("TimerText");
        GameObject resultPanelObject = GameObject.Find("ResultPanel"); 
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject resetButtonObject = GameObject.Find("ResetButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Assign objects and components
        ScriptUtilities.AssignComponentFieldToObject(projectorObject, scriptPrefix + "Projector", go, className, "projector");
        ScriptUtilities.AssignComponentFieldToObject(speedTextObject, "Text", go, className, "speedText");
        ScriptUtilities.AssignComponentFieldToObject(distanceTextObject, "Text", go, className, "distanceText");
        ScriptUtilities.AssignComponentFieldToObject(timerTextObject, "Text", go, className, "timerText");
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(resetButtonObject, "Button", go, className, "resetButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
    }

    private static void InstantiateAndSetupProjector(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "Projector";

        // Get objects
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject backgroundObject = GameObject.Find("Background");
        // Assign objects and components
        ScriptUtilities.AssignObjectFieldToObject(backgroundObject, go, className, "backgroundObject");
        ScriptUtilities.AssignComponentFieldToObject(cameraObject, "Camera", go, className, "mainCamera");
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
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DBuilder", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DDefaultRoute", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DProjector", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DQuad", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("DR2DSegment", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void WriteDR2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("DR2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DBuilderScriptToFile()
    {
        StringBuilder sb = new StringBuilder(774);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("abstract public class DR2DBuilder : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public GameObject lightPrefab;");
        sb.AppendLine("    public GameObject darkPrefab;");
        sb.AppendLine("    public GameObject[] spritePrefabs = new GameObject[8];");
        sb.AppendLine("");
        sb.AppendLine("    abstract public GameObject[] BuildRoad(float segmentLength, float segmentWidth);");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DBuilder", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DDefaultRouteScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3925);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DDefaultRoute : DR2DBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    override public GameObject[] BuildRoad(float length, float width)");
        sb.AppendLine("    {");
        sb.AppendLine("        const int numSegments = 1600;");
        sb.AppendLine("        GameObject[] segments = new GameObject[numSegments];");
        sb.AppendLine("");
        sb.AppendLine("        for (int i = 0; i < segments.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameObject segmentPrefab = i % 2 == 0 ? lightPrefab : darkPrefab;");
        sb.AppendLine("            segments[i] = Instantiate(segmentPrefab) as GameObject;");
        sb.AppendLine("            segments[i].transform.SetParent(transform);");
        sb.AppendLine("            DR2DSegment segment = segments[i].GetComponent<DR2DSegment>();");
        sb.AppendLine("            segment.gameObject.name = \"RoadSegment\" + i.ToString(\"D4\");");
        sb.AppendLine("            segment.roadWidth = width;");
        sb.AppendLine("            segment.position3D.z = i * length;");
        sb.AppendLine("");
        sb.AppendLine("            // Firstly, a length of straight and flat road segments");
        sb.AppendLine("");
        sb.AppendLine("            // Secondly, demonstrate a length of right curving road segments");
        sb.AppendLine("            if (i > 300 && i < 700)");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.curve = 0.7f;");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            // Followed by a length of steeper left curving road segments");
        sb.AppendLine("            if (i > 1100 && i < 1500) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.curve = -0.7f;");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            // Thirdly, add some undulations");
        sb.AppendLine("            if (i > 750) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.position3D.y = Mathf.Sin((float)i / 30.0f) * 25.0f;");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            // Finally add some decorative sprites");
        sb.AppendLine("            float sideDistance = 2.0f;");
        sb.AppendLine("            float bushOffset = 0.5f;");
        sb.AppendLine("            float pineOffset = 1.0f;");
        sb.AppendLine("            if (i < 300 && i % 20 == 0) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.SetSprite(spritePrefabs[1], -sideDistance, bushOffset);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (i % 17 == 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.SetSprite(spritePrefabs[0], sideDistance, pineOffset);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (i > 300 && i % 20 == 0) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.SetSprite(spritePrefabs[1], -sideDistance, bushOffset);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (i > 800 && i % 20 == 0) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.SetSprite(spritePrefabs[0], -sideDistance, pineOffset);");
        sb.AppendLine("            }");
        sb.AppendLine("            if (i == 400) ");
        sb.AppendLine("            {");
        sb.AppendLine("                segment.SetSprite(spritePrefabs[1], -sideDistance, bushOffset);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return segments;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DDefaultRoute", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(8044);

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public DR2DProjector projector;");
        sb.AppendLine("    public Text speedText;");
        sb.AppendLine("    public Text distanceText;");
        sb.AppendLine("    public Text timerText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Button resetButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    public float gameTime = 180.0f;");
        sb.AppendLine("    public float startGameWait = 0.5f;");
        sb.AppendLine("    private float currentSpeed = 0.0f;");
        sb.AppendLine("    private float distanceTraveled = 0.0f;");
        sb.AppendLine("    private float timeRemaining = 0.0f;");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static DR2DGameManager sharedInstance = null;");
        sb.AppendLine("");
        sb.AppendLine("    private void Awake()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Ensure there is only one instance");
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
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("        ResetUI();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        timeRemaining -= Time.deltaTime;");
        sb.AppendLine("        if (timeRemaining <= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            timeRemaining = 0.0f;");
        sb.AppendLine("            GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("        UpdateTimer();");
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
        sb.AppendLine("            // Start game");
        sb.AppendLine("            StartCoroutine(WaitToStartGame(startGameWait));");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if result panel is being displayed");
        sb.AppendLine("        else if (resultPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Close it");
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            // Enable the reset button");
        sb.AppendLine("            resetButton.interactable = true;");
        sb.AppendLine("            // Hide the press any key text");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            // Click it");
        sb.AppendLine("            TaskOnResetButtonClicked();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        projector.StopGame();");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return gameStarted;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyDistanceTraveled(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        distanceTraveled += value;");
        sb.AppendLine("        UpdateDistance();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifySpeed(float value)");
        sb.AppendLine("    {");
        sb.AppendLine("        currentSpeed = (int)(value * 100.0f);");
        sb.AppendLine("        UpdateSpeed();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        currentSpeed = 0;");
        sb.AppendLine("        distanceTraveled = 0.0f;");
        sb.AppendLine("        timeRemaining = gameTime;");
        sb.AppendLine("        projector.ResetGame();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ResetUI()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("        resetButton.interactable = false;");
        sb.AppendLine("        UpdateDistance();");
        sb.AppendLine("        UpdateSpeed();");
        sb.AppendLine("        UpdateTimer();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        resetButton.onClick.AddListener(TaskOnResetButtonClicked);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnResetButtonClicked()");
        sb.AppendLine("    {");
        sb.AppendLine("        ResetGame();");
        sb.AppendLine("        ResetUI();");
        sb.AppendLine("        StartCoroutine(WaitToStartGame(startGameWait));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateDistance()");
        sb.AppendLine("    {");
        sb.AppendLine("        float kilometers = distanceTraveled / 1000.0f;");
        sb.AppendLine("        distanceText.text = \"Traveled: \" + String.Format(\"{0:0.00}\", kilometers); ;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateSpeed()");
        sb.AppendLine("    {");
        sb.AppendLine("        speedText.text = \"Speed: \" + currentSpeed;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateTimer()");
        sb.AppendLine("    {");
        sb.AppendLine("        int minutes = (int)(timeRemaining / 60.0f);");
        sb.AppendLine("        int seconds = (int)timeRemaining % 60;");
        sb.AppendLine("        timerText.text = minutes + \" : \" + seconds.ToString(\"00\");");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToStartGame(float seconds)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(seconds);");
        sb.AppendLine("        gameStarted = true;");
        sb.AppendLine("        resetButton.interactable = true;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DProjectorScriptToFile()
    {
        StringBuilder sb = new StringBuilder(13077);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DProjector : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    GameObject[] segments;");
        sb.AppendLine("");
        sb.AppendLine("    static public float screenWidth = 34.0f;");
        sb.AppendLine("    static public float screenHeight = 15.0f;");
        sb.AppendLine("    static public float cameraDepth = 0.84f;");
        sb.AppendLine("");
        sb.AppendLine("    public GameObject backgroundObject;");
        sb.AppendLine("    public Camera mainCamera;");
        sb.AppendLine("    public float roadWidth = 3.0f;");
        sb.AppendLine("    public float maxSpeed = 2.0f; // Max speed");
        sb.AppendLine("    public float offRoadSpeed = 0.3f; // Off road speed");
        sb.AppendLine("    public float acceleration = 0.5f;");
        sb.AppendLine("");
        sb.AppendLine("    private const float roadBoundary = 1.8f;   // Road boundary, estimated from projected width of 1st segment when roadWidth is 1.0");
        sb.AppendLine("    private const float horizontalBoundary = roadBoundary / 0.9f; // Max horizontal movement");
        sb.AppendLine("    private const float segmentLength = 2.0f;");
        sb.AppendLine("    public float distanceTravelled = 0.0f;");
        sb.AppendLine("    private float speed = 0.0f;");
        sb.AppendLine("    private float loopLength = 0.0f;");
        sb.AppendLine("    private float playerX;");
        sb.AppendLine("    private Vector2 inputVector;");
        sb.AppendLine("    private DR2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = DR2DGameManager.sharedInstance;");
        sb.AppendLine("        segments = GetComponent<DR2DBuilder>().BuildRoad(segmentLength, roadWidth);");
        sb.AppendLine("        loopLength = segments.Length * segmentLength;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Update()");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdateControls();");
        sb.AppendLine("        UpdateMovement();");
        sb.AppendLine("        HideRoad();");
        sb.AppendLine("        RenderRoad();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnMove(InputValue input)");
        sb.AppendLine("    {");
        sb.AppendLine("        inputVector = input.Get<Vector2>();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Accelerate()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Off road acceleration");
        sb.AppendLine("        if (!IsOnRoad() && speed > offRoadSpeed)");
        sb.AppendLine("        {");
        sb.AppendLine("            speed -= acceleration * Time.deltaTime;");
        sb.AppendLine("            if (speed < offRoadSpeed)");
        sb.AppendLine("            {");
        sb.AppendLine("                speed = offRoadSpeed;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // On the road acceleration");
        sb.AppendLine("        else if (speed < maxSpeed)");
        sb.AppendLine("        {");
        sb.AppendLine("            speed += acceleration * Time.deltaTime;");
        sb.AppendLine("            if (speed > maxSpeed)");
        sb.AppendLine("            {");
        sb.AppendLine("                speed = maxSpeed;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Brake()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Brake on the road");
        sb.AppendLine("        if (speed > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            speed -= acceleration * Time.deltaTime;");
        sb.AppendLine("            if (speed <= 0.0f)");
        sb.AppendLine("            {");
        sb.AppendLine("                speed = 0.0f;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            speed = 0.0f;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Cruise()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Off road deceleration");
        sb.AppendLine("        if (!IsOnRoad() && speed > offRoadSpeed)");
        sb.AppendLine("        {");
        sb.AppendLine("            speed -= acceleration * Time.deltaTime;");
        sb.AppendLine("            if (speed < offRoadSpeed)");
        sb.AppendLine("            {");
        sb.AppendLine("                speed = offRoadSpeed;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Otherwise, just maintain current speed");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void HideRoad()");
        sb.AppendLine("    {");
        sb.AppendLine("        for (int i = 0; i < segments.Length; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            segments[i].SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool IsOnRoad()");
        sb.AppendLine("    {");
        sb.AppendLine("        return (Mathf.Abs(playerX) <= roadBoundary * roadWidth);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void RenderRoad()");
        sb.AppendLine("    {");
        sb.AppendLine("        float H = mainCamera.transform.position.y;");
        sb.AppendLine("        int startIndex = (int)(distanceTravelled / segmentLength);");
        sb.AppendLine("        float camH = segments[startIndex].GetComponent<DR2DSegment>().position3D.y + H;");
        sb.AppendLine("");
        sb.AppendLine("        // Move BG to accommodate curves here");
        sb.AppendLine("        DR2DSegment currentSegment = segments[startIndex].GetComponent<DR2DSegment>();");
        sb.AppendLine("        Vector3 bgTranslation = new Vector3(currentSegment.curve, 0.0f, 0.0f);");
        sb.AppendLine("        float mul = 0.05f * speed / maxSpeed;");
        sb.AppendLine("        if (speed > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            backgroundObject.transform.position -= (bgTranslation * mul);");
        sb.AppendLine("        }");
        sb.AppendLine("        if (speed < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            backgroundObject.transform.position += (bgTranslation * mul);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Render road segments batch");
        sb.AppendLine("        float clipY = 0.0f;");
        sb.AppendLine("        float currentCurve = 0;");
        sb.AppendLine("        float accumulatedCurve = 0;");
        sb.AppendLine("        int numSegments = segments.Length;");
        sb.AppendLine("        const int numDraw = 60;");
        sb.AppendLine("        for (int i = startIndex + 1; i < startIndex + numDraw; i++) ");
        sb.AppendLine("        {");
        sb.AppendLine("            GameObject segmentObject = segments[i % numSegments];");
        sb.AppendLine("            segmentObject.SetActive(true);");
        sb.AppendLine("            DR2DSegment segment = segmentObject.GetComponent<DR2DSegment>();");
        sb.AppendLine("");
        sb.AppendLine("            // Project the current segment");
        sb.AppendLine("            float camX = playerX * roadWidth - currentCurve;");
        sb.AppendLine("            float camZ = startIndex * segmentLength - (i >= numSegments ? numSegments * segmentLength : 0);");
        sb.AppendLine("            segment.Project(camX, camH, camZ);");
        sb.AppendLine("            currentCurve += accumulatedCurve;");
        sb.AppendLine("            accumulatedCurve += segment.curve;");
        sb.AppendLine("");
        sb.AppendLine("            Vector3 curPos = segment.transform.position;");
        sb.AppendLine("            // If the current segment is hidden, set inactive and skip it");
        sb.AppendLine("            if (curPos.y <= clipY)");
        sb.AppendLine("            {");
        sb.AppendLine("                segmentObject.SetActive(false);");
        sb.AppendLine("                continue;");
        sb.AppendLine("            }");
        sb.AppendLine("            clipY = curPos.y;");
        sb.AppendLine("");
        sb.AppendLine("            // Draw current segment, influenced by the size and shape of the previous one");
        sb.AppendLine("            int previousIndex = (i - 1) % numSegments;");
        sb.AppendLine("            previousIndex = previousIndex > 0 ? previousIndex : numSegments + previousIndex;");
        sb.AppendLine("            DR2DSegment previous = segments[(i - 1) % numSegments].GetComponent<DR2DSegment>();");
        sb.AppendLine("");
        sb.AppendLine("            Vector3 prevPos = previous.transform.position;");
        sb.AppendLine("            float prevWidth = previous.projectedWidth;");
        sb.AppendLine("            float curWidth = segment.projectedWidth;");
        sb.AppendLine("            float rumbleMul = 1.2f;");
        sb.AppendLine("            segment.RenderGrass(0.0f, prevPos.y, screenWidth, 0.0f, curPos.y, screenWidth);");
        sb.AppendLine("            segment.RenderRumble(prevPos.x, prevPos.y, prevWidth * rumbleMul, curPos.x, curPos.y, curWidth * rumbleMul);");
        sb.AppendLine("            segment.RenderRoad(prevPos.x, prevPos.y, prevWidth, curPos.x, curPos.y, curWidth);");
        sb.AppendLine("            segment.RenderSprite();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ResetGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        distanceTravelled = 0.0f;");
        sb.AppendLine("        speed = 0.0f;");
        sb.AppendLine("        playerX = 0.0f;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void StopGame()");
        sb.AppendLine("    {");
        sb.AppendLine("        speed = 0.0f;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateControls()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Handle side movement");
        sb.AppendLine("        playerX += inputVector.x * 0.1f;");
        sb.AppendLine("");
        sb.AppendLine("        // Handle forward movement");
        sb.AppendLine("");
        sb.AppendLine("        // If forward is pressed");
        sb.AppendLine("        if (inputVector.y > 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            Accelerate();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if backward is pressed");
        sb.AppendLine("        else if (inputVector.y < 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            Brake();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else (no input)");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            Cruise();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateMovement()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Report speed and distance traveled");
        sb.AppendLine("        gameManager.NotifySpeed(speed);");
        sb.AppendLine("        gameManager.NotifyDistanceTraveled(speed);");
        sb.AppendLine("        // Movement along Y-axis");
        sb.AppendLine("        distanceTravelled += speed;");
        sb.AppendLine("        int loopCount = 0;");
        sb.AppendLine("        const int threshold = 2000;");
        sb.AppendLine("        while (distanceTravelled >= loopLength)");
        sb.AppendLine("        {");
        sb.AppendLine("            distanceTravelled -= loopLength;");
        sb.AppendLine("            if (++loopCount > threshold)");
        sb.AppendLine("            {");
        sb.AppendLine("                Debug.LogError(\"Infinity loop alert!\");");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        loopCount = 0;");
        sb.AppendLine("        while (distanceTravelled < 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            distanceTravelled += loopLength;");
        sb.AppendLine("            if (++loopCount > threshold)");
        sb.AppendLine("            {");
        sb.AppendLine("                Debug.LogError(\"Infinity loop alert!\");");
        sb.AppendLine("                break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Movment along X-axis");
        sb.AppendLine("        int startIndex = (int)(distanceTravelled / segmentLength);");
        sb.AppendLine("        DR2DSegment currentSegment = segments[startIndex].GetComponent<DR2DSegment>();");
        sb.AppendLine("        const float centrifugalFactor = 5.0f;");
        sb.AppendLine("        playerX = playerX - currentSegment.curve * centrifugalFactor * speed * Time.deltaTime;");
        sb.AppendLine("        float bound = roadWidth * horizontalBoundary;");
        sb.AppendLine("        playerX = Mathf.Clamp(playerX, -bound, bound);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DProjector", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DQuadScriptToFile()
    {
        StringBuilder sb = new StringBuilder(2023);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DQuad : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private Mesh mesh;");
        sb.AppendLine("    private MeshFilter mf;");
        sb.AppendLine("    private Vector3[] vertices;");
        sb.AppendLine("    private int[] indices;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        mesh = new Mesh();");
        sb.AppendLine("        mesh.name = \"QuadMesh\";");
        sb.AppendLine("");
        sb.AppendLine("        vertices = new Vector3[] ");
        sb.AppendLine("        { ");
        sb.AppendLine("            new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), ");
        sb.AppendLine("            new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) ");
        sb.AppendLine("        };");
        sb.AppendLine("        indices = new int[] { 0, 1, 3, 1, 2, 3 };");
        sb.AppendLine("        ");
        sb.AppendLine("        mesh.vertices = vertices;");
        sb.AppendLine("        mesh.triangles = indices;");
        sb.AppendLine("");
        sb.AppendLine("        mf = GetComponent<MeshFilter>();");
        sb.AppendLine("        mf.sharedMesh = mesh;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SetVertices(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight)");
        sb.AppendLine("    {");
        sb.AppendLine("        vertices[0] = bottomLeft;");
        sb.AppendLine("        vertices[1] = topLeft;");
        sb.AppendLine("        vertices[2] = topRight;");
        sb.AppendLine("        vertices[3] = bottomRight;");
        sb.AppendLine("");
        sb.AppendLine("        mesh.vertices = vertices;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DQuad", scriptsPath, sb.ToString());
    }

    private static void WriteDR2DSegmentScriptToFile()
    {
        StringBuilder sb = new StringBuilder(6647);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class DR2DSegment : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float roadWidth = 1.0f;");
        sb.AppendLine("    public float scale = 1.0f;");
        sb.AppendLine("    public float curve = 0.0f;");
        sb.AppendLine("    public Vector3 position3D = Vector3.zero; ");
        sb.AppendLine("    public float projectedWidth;");
        sb.AppendLine("    public GameObject sprite = null;");
        sb.AppendLine("    public float spriteX;   ");
        sb.AppendLine("    public float spriteY;");
        sb.AppendLine("");
        sb.AppendLine("    private GameObject grassChild;");
        sb.AppendLine("    private GameObject rumbleChild;");
        sb.AppendLine("    private GameObject roadChild;");
        sb.AppendLine("    ");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        grassChild = transform.GetChild(0).gameObject;");
        sb.AppendLine("        rumbleChild = transform.GetChild(1).gameObject;");
        sb.AppendLine("        roadChild = transform.GetChild(2).gameObject;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Project(float camX, float camY, float camZ)");
        sb.AppendLine("    {");
        sb.AppendLine("        float cameraDepth = DR2DProjector.cameraDepth;");
        sb.AppendLine("");
        sb.AppendLine("        // Pseudo 3D projection");
        sb.AppendLine("        scale = cameraDepth / (position3D.z - camZ);");
        sb.AppendLine("        float X = ProjectValueX(position3D.x, camX, scale);");
        sb.AppendLine("        float Y = ProjectValueY(position3D.y, camY, scale);");
        sb.AppendLine("        float W = ProjectValueW(scale);");
        sb.AppendLine("        float zDepth = Y;");
        sb.AppendLine("        // Save values");
        sb.AppendLine("        transform.position = new Vector3(X, Y, zDepth);");
        sb.AppendLine("        projectedWidth = W;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float ProjectValueX(float value, float camX, float scale)");
        sb.AppendLine("    {");
        sb.AppendLine("        value = scale * (value - camX) * roadWidth;");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float ProjectValueY(float value, float camY, float scale)");
        sb.AppendLine("    {");
        sb.AppendLine("        float h = DR2DProjector.screenHeight * 0.5f;");
        sb.AppendLine("        value = h * (1.0f + scale * (position3D.y - camY));");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private float ProjectValueW(float scale)");
        sb.AppendLine("    {");
        sb.AppendLine("        return scale * DR2DProjector.screenWidth * 0.5f * roadWidth;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RenderGrass(float prevX, float prevY, float prevW, float curX, float curY, float curW)");
        sb.AppendLine("    {");
        sb.AppendLine("        DR2DQuad quad = grassChild.GetComponent<DR2DQuad>();");
        sb.AppendLine("        RenderQuad(quad, prevX, prevY, prevW, curX, curY, curW);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RenderRumble(float prevX, float prevY, float prevW, float curX, float curY, float curW)");
        sb.AppendLine("    {");
        sb.AppendLine("        DR2DQuad quad = rumbleChild.GetComponent<DR2DQuad>();");
        sb.AppendLine("        RenderQuad(quad, prevX, prevY, prevW, curX, curY, curW);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RenderRoad(float prevX, float prevY, float prevW, float curX, float curY, float curW)");
        sb.AppendLine("    {");
        sb.AppendLine("        DR2DQuad quad = roadChild.GetComponent<DR2DQuad>();");
        sb.AppendLine("        RenderQuad(quad, prevX, prevY, prevW, curX, curY, curW);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void RenderQuad(DR2DQuad quad, float prevX, float prevY, float prevW, float curX, float curY, float curW)");
        sb.AppendLine("    {");
        sb.AppendLine("        float bx = prevX - curX;");
        sb.AppendLine("        float by = prevY - curY;");
        sb.AppendLine("        Vector3 bottomLeft = new Vector3(bx - prevW, by);");
        sb.AppendLine("        Vector3 bottomRight = new Vector3(bx + prevW, by);");
        sb.AppendLine("        Vector3 topLeft = new Vector3(-curW, 0.0f);");
        sb.AppendLine("        Vector3 topRight = new Vector3(curW, 0.0f);");
        sb.AppendLine("        quad.SetVertices(bottomLeft, bottomRight, topLeft, topRight);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RenderSprite()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Bail if sprite is not assigned");
        sb.AppendLine("        if (sprite == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Compute scale");
        sb.AppendLine("        float localScale = scale * DR2DProjector.screenHeight / 2.0f;");
        sb.AppendLine("        sprite.transform.localScale = new Vector3(localScale, localScale, localScale);");
        sb.AppendLine("        // Compute position x");
        sb.AppendLine("        Vector3 newPos = transform.position;");
        sb.AppendLine("        newPos.x += spriteX * projectedWidth;");
        sb.AppendLine("        // Compute position y");
        sb.AppendLine("        newPos.y += spriteY * localScale;");
        sb.AppendLine("        sprite.transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SetSprite(GameObject prefab, float offsetX, float offsetY)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (sprite != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Instantiate sprite");
        sb.AppendLine("        sprite = Instantiate(prefab);");
        sb.AppendLine("        sprite.name = \"SpriteOf\" + name;");
        sb.AppendLine("        // Save the offsets");
        sb.AppendLine("        spriteX = offsetX;");
        sb.AppendLine("        spriteY = offsetY;");
        sb.AppendLine("        sprite.transform.SetParent(transform);");
        sb.AppendLine("        sprite.transform.localPosition = new Vector3(spriteX, 0.0f, 0.0f);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("DR2DSegment", scriptsPath, sb.ToString());
    }
}
