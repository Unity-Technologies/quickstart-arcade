using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public class HexStrategy2D : Editor
{
    private const string templateName = "HexStrategy2D";
    private const string templateSpacedName = "Hex Strategy 2D";
    private const string prefKey = templateName + "Processing";
    private const string scriptPrefix = "HS2D";
    private const int textureSize = 64;
    private const int mapWidth = 8;
    private const int mapHeight = 8;
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
        Plains = 0,
        Forest,
        Hill,
        Water,
        Sea,
        Castle,
        Army,
        Fog,
        Max
    }
    static string[] spritePaths = new string[(int)SpritePath.Max];
    
    private enum TilePath
    {
        Plains = 0,
        Forest,
        Hill,
        Water,
        Sea,
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
        ScriptUtilities.RemoveTag(scriptPrefix + "Red");
        ScriptUtilities.RemoveTag(scriptPrefix + "Blue");
        // Layers
        ScriptUtilities.RemoveLayer(scriptPrefix + "World");
        ScriptUtilities.RemoveLayer(scriptPrefix + "Buildings");
        ScriptUtilities.RemoveLayer(scriptPrefix + "Units");
        // Sorting Layers
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Sea");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Ground");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Buildings");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Units");
        ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Fog");
        // Settings
        GraphicsSettings.transparencySortMode = TransparencySortMode.Default;
        GraphicsSettings.transparencySortAxis = new Vector3(0.0f, 0.0f, 1.0f);
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
        // Layout Level
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
        mainCameraObject.GetComponent<Camera>().orthographicSize = 1.0f + mapHeight / 2;
        // Create tags and layers
        GenerateTagsAndLayers();
    }

    private static void GenerateTagsAndLayers()
    {
        // Tags
        ScriptUtilities.CreateTag(scriptPrefix + "Red");
        ScriptUtilities.CreateTag(scriptPrefix + "Blue");
        // Layers
        ScriptUtilities.CreateLayer(scriptPrefix + "World");
        ScriptUtilities.CreateLayer(scriptPrefix + "Buildings");
        ScriptUtilities.CreateLayer(scriptPrefix + "Units");
        // Sorting Layers
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Sea");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Ground");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Buildings");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Units");
        ScriptUtilities.CreateSortingLayer(scriptPrefix + "Fog");
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
        // Create Select action and add bindings
        var action = map.AddAction("Select", interactions: "tap");
        action.AddBinding(new InputBinding("<Mouse>/leftButton"));
        // Create Act action and add bindings
        action = map.AddAction("Act", interactions: "tap");
        action.AddBinding(new InputBinding("<Mouse>/rightButton"));

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
        Color plainsGreen = new Color(231.0f * k, 200.0f * k, 93.0f * k);
        Color forestGreen = new Color(93.0f * k, 106.0f * k, 49.0f * k);
        Color hillGray = new Color(146.0f * k, 145.0f * k, 126.0f * k);
        Color waterBlue = new Color(93.0f * k, 174.0f * k, 219.0f * k);
        Color seaBlue = new Color(9.0f * k, 40.0f * k, 88.0f * k);

        // Generate textures
        int w = textureSize;
        int h = textureSize;
        // Plains
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_plains", texturesPath, w, h, plainsGreen);
        spritePaths[(int)SpritePath.Plains] = path;
        // Forest
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_forest", texturesPath, w, h, forestGreen);
        spritePaths[(int)SpritePath.Forest] = path;
        // Hill
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_hill", texturesPath, w, h, hillGray);
        spritePaths[(int)SpritePath.Hill] = path;
        // Water
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_water", texturesPath, w, h, waterBlue);
        spritePaths[(int)SpritePath.Water] = path;
        // Sea
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_sea", texturesPath, w, h, seaBlue);
        spritePaths[(int)SpritePath.Sea] = path;
        // Fog
        path = ContentUtilities.CreateTexture2DHexagonAsset("texture_hex_fog", texturesPath, w, h, Color.white);
        spritePaths[(int)SpritePath.Fog] = path;

        // Castle
        w = (int)(0.5f * (float)w);
        h = (int)(0.5f * (float)h);
        path = ContentUtilities.CreateTexture2DFrameAsset("texture_castle", texturesPath, w, h, Color.white);
        spritePaths[(int)SpritePath.Castle] = path;
        // Army
        DrawArmyTexture();
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

        // Player and enemy tags
        string playerTag = scriptPrefix + "Blue";
        string enemyTag = scriptPrefix + "Red";

        // Create castle prefab
        newObject = ContentUtilities.CreateTexturedFigment("Castle", 0.0f, 0.0f, spritePaths[(int)SpritePath.Castle]);
        newObject.GetComponent<SpriteRenderer>().sortingLayerName = scriptPrefix + "Buildings";
        GameObject playerCastle = CreateFactionGamePiece("PlayerCastle", newObject, playerTag, Color.blue);
        GameObject enemyCastle = CreateFactionGamePiece("EnemyCastle", newObject, enemyTag, Color.red);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        ContentUtilities.CreatePrefab(playerCastle, prefabsPath, true);
        ContentUtilities.CreatePrefab(enemyCastle, prefabsPath, true);

        // Create army unit prefab
        newObject = ContentUtilities.CreateTexturedFigment("Army", 0.0f, 0.0f, spritePaths[(int)SpritePath.Army]);
        newObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "Units");
        newObject.AddComponent<BoxCollider2D>();
        newObject.GetComponent<SpriteRenderer>().sortingLayerName = scriptPrefix + "Units";
        GameObject playerArmy = CreateFactionGamePiece("PlayerArmy", newObject, playerTag, Color.blue);
        GameObject enemyArmy = CreateFactionGamePiece("EnemyArmy", newObject, enemyTag, Color.red);
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
        ContentUtilities.CreatePrefab(playerArmy, prefabsPath, true);
        ContentUtilities.CreatePrefab(enemyArmy, prefabsPath, true);

        // Create faction prefabs
        newObject = new GameObject("Faction");
        ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

        // Create hex overlay prefab
        newObject = ContentUtilities.CreateTexturedFigment("HexOverlay", 0.0f, 0.0f, spritePaths[(int)SpritePath.Fog]);
        newObject.GetComponent<SpriteRenderer>().sortingLayerName = scriptPrefix + "Fog";
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

        // Create gold text panel
        GameObject goldTextPanel = ContentUtilities.CreateUIBackgroundObject("GoldTextPanel", w, h);
        ContentUtilities.AnchorUIObject(goldTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin));
        // Create gold text
        GameObject goldTextObject = ContentUtilities.CreateUITextObject("GoldText", w - margin, h, "Gold: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(goldTextObject, goldTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create turns text panel
        float offset = -h - margin;
        GameObject turnsTextPanel = ContentUtilities.CreateUIBackgroundObject("TurnsTextPanel", w, h);
        ContentUtilities.AnchorUIObject(turnsTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offset));
        // Create turns text
        GameObject turnsTextObject = ContentUtilities.CreateUITextObject("TurnsText", w - margin, h, "Turns: 99", TextAnchor.MiddleLeft, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(turnsTextObject, turnsTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

        // Create result panel
        w = 600.0f;
        h = 240.0f;
        GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
        ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
        // Create result text
        GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w, h, "VICTORY!", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(resultTextObject, resultPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create end turn button
        w = 150.0f;
        h = 40.0f;
        GameObject buttonObject = ContentUtilities.CreateUIButtonObject("EndTurnButton", w, h, "End Turn", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(buttonObject, parent, ContentUtilities.Anchor.TopRight, new Vector2(-margin, -margin));

        // Create hex info window
        string[] hexNames = { "TypeText", "PositionText", "TaxText", "DefenseText", "MoveText", "FactionText" };
        string[] hexTexts = { "Type: --", "Pos: (--, --)", "Tax: --", "Defense: --", "Move Cost: --", "Faction: --" };
        w = 120.0f;
        h = 20.0f;
        CreateInfoWindow(parent, "HexInfo", hexNames, hexTexts, margin, w, h);

        // Create unit info window
        string[] unitNames = { "TypeText", "PositionText", "StrengthText", "UpkeepText", "MoveText", "FactionText" };
        string[] unitTexts = { "Type: --", "Pos: (--, --)", "Str.: --", "Upkeep: --", "Move: --", "Faction: --" };
        w = 120.0f;
        h = 20.0f;
        CreateInfoWindow(parent, "UnitInfo", unitNames, unitTexts, margin, w, h);

        // Create recruit button
        w = 150.0f;
        h = 60.0f;
        buttonObject = ContentUtilities.CreateUIButtonObject("RecruitButton", w, h, "Recruit\n(10 G)", fontSize, Color.white);
        ContentUtilities.AnchorUIObject(buttonObject, parent, ContentUtilities.Anchor.BottomRight, new Vector2(-margin, margin));

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
        sb.AppendLine("Use Left Click to Select");
        sb.AppendLine("And use Right Click for Actions");
        sb.AppendLine("Destroy the Enemy Castle before You Run out of Turns");
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
        WriteHS2DBlinkTextScriptToFile();
        WriteHS2DBoardScriptToFile();
        WriteHS2DBuildingScriptToFile();
        WriteHS2DContextHelpScriptToFile();
        WriteHS2DDeployableScriptToFile();
        WriteHS2DEnemyAiScriptToFile();
        WriteHS2DFactionScriptToFile();
        WriteHS2DGameManagerScriptToFile();
        WriteHS2DHexInfoScriptToFile();
        WriteHS2DInfoScriptToFile();
        WriteHS2DInputControllerScriptToFile();
        WriteHS2DNodeScriptToFile();
        WriteHS2DPathFinderScriptToFile();
        WriteHS2DUnitInfoScriptToFile();
        WriteHS2DUnitScriptToFile();
    }

    private static void GenerateTileMap()
    {
        // Generate tile map, pallette, and tile assets here...
        string path;

        path = ContentUtilities.CreateTileAsset("tile_plains", spritePaths[(int)SpritePath.Plains], tilesPath);
        tilePaths[(int)TilePath.Plains] = path;
        path = ContentUtilities.CreateTileAsset("tile_forest", spritePaths[(int)SpritePath.Forest], tilesPath);
        tilePaths[(int)TilePath.Forest] = path;
        path = ContentUtilities.CreateTileAsset("tile_hill", spritePaths[(int)SpritePath.Hill], tilesPath);
        tilePaths[(int)TilePath.Hill] = path;
        path = ContentUtilities.CreateTileAsset("tile_water", spritePaths[(int)SpritePath.Water], tilesPath);
        tilePaths[(int)TilePath.Water] = path;
        path = ContentUtilities.CreateTileAsset("tile_sea", spritePaths[(int)SpritePath.Sea], tilesPath);
        tilePaths[(int)TilePath.Sea] = path;

        // Create tile palette
        GameObject tilePalette = ContentUtilities.CreateHexagonalTilePaletteObject(scriptPrefix + "TilePalette", tilesPath);
        // Create grid and tile map objects
        GameObject seaLayerObject = ContentUtilities.CreateHexagonalTilemapObject("SeaLayer");
        seaLayerObject.GetComponent<TilemapRenderer>().sortingLayerName = scriptPrefix + "Sea";
        GameObject gridObject = seaLayerObject.transform.parent.gameObject;
        gridObject.layer = ScriptUtilities.IndexOfLayer(scriptPrefix + "World");
        GameObject groundLayerObject = ContentUtilities.CreateHexagonalTilemapObject("GroundLayer", gridObject);
        groundLayerObject.GetComponent<TilemapRenderer>().sortingLayerName = scriptPrefix + "Ground";
        // Add input
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath("Assets/" + settingsPath + "/" + scriptPrefix + "InputActions.inputactions", typeof(InputActionAsset)) as InputActionAsset;
        gridObject.AddComponent<PlayerInput>().actions = asset;
        gridObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
        // Settings
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0.0f, 1.0f, 0.0f);

        // Create an object to contain hex nodes, attaching to Grid
        GameObject newObject = new GameObject("NodesContainer");
        newObject.transform.SetParent(gridObject.transform);

        // Associate tile(s) to palette
        Tilemap paletteTilemap = tilePalette.GetComponentInChildren<Tilemap>();
        Tile tile;
        // Plains
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Plains]);
        paletteTilemap.SetTile(Vector3Int.zero, tile);
        // Forest
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Forest]);
        paletteTilemap.SetTile(new Vector3Int(1, 0, 0), tile);
        // Hill
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Hill]);
        paletteTilemap.SetTile(new Vector3Int(2, 0, 0), tile);
        // Water
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Water]);
        paletteTilemap.SetTile(new Vector3Int(1, 1, 0), tile);
        // Sea
        tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/" + tilePaths[(int)TilePath.Sea]);
        paletteTilemap.SetTile(new Vector3Int(2, 1, 0), tile);
        // ... Add more tiles if needed
    }

    private static void EnableOnScriptsReloadedProcessing()
    {
        if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
            "Board", "Building", "Deployable", "EnemyAi", "Faction", "GameManager", "HexInfo", "Info",
            "InputController", "Node", "PathFinder", "UnitInfo", "Unit" }))
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
        // Access the prefabs
        GameObject gameManagerPrefab = ContentUtilities.LoadPrefab("GameManager", prefabsPath);
        GameObject castlePrefab = ContentUtilities.LoadPrefab("Castle", prefabsPath);
        GameObject armyPrefab = ContentUtilities.LoadPrefab("Army", prefabsPath);
        GameObject factionPrefab = ContentUtilities.LoadPrefab("Faction", prefabsPath);
        GameObject hexOverlayPrefab = ContentUtilities.LoadPrefab("HexOverlay", prefabsPath);
        GameObject playerArmyPrefab = ContentUtilities.LoadPrefab("PlayerArmy", prefabsPath);
        GameObject playerCastlePrefab = ContentUtilities.LoadPrefab("PlayerCastle", prefabsPath);
        GameObject enemyArmyPrefab = ContentUtilities.LoadPrefab("EnemyArmy", prefabsPath);
        GameObject enemyCastlePrefab = ContentUtilities.LoadPrefab("EnemyCastle", prefabsPath);

        // And other objects
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        GameObject hexInfoObject = GameObject.Find("HexInfo");
        GameObject unitInfoObject = GameObject.Find("UnitInfo");
        GameObject contextHelpObject = GameObject.Find("ContextHelp");
        GameObject gridObject = GameObject.Find("Grid");

        // Attach scripts
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Building", castlePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Building", playerCastlePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Building", enemyCastlePrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Unit", armyPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Unit", playerArmyPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Unit", enemyArmyPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Faction", factionPrefab);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "HexInfo", hexInfoObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "UnitInfo", unitInfoObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "ContextHelp", contextHelpObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Board", gridObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "InputController", gridObject);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "Node", hexOverlayPrefab);

        // Assign parameters
        // Board
        string className;
        className = scriptPrefix + "Board";
        ScriptUtilities.AssignLayerMaskToObject(LayerMask.GetMask(scriptPrefix + "Units"), gridObject, className, "unitsLayerMask");
        ScriptUtilities.AssignObjectFieldToObject(hexOverlayPrefab, gridObject, className, "hexNodePrefab");
        // Army and Building Prefabs
        className = "HS2DDeployable";
        ScriptUtilities.AssignIntFieldToObject(0, castlePrefab, className, "category");
        ScriptUtilities.AssignIntFieldToObject(0, playerCastlePrefab, className, "category");
        ScriptUtilities.AssignIntFieldToObject(0, enemyCastlePrefab, className, "category");
        ScriptUtilities.AssignIntFieldToObject(1, armyPrefab, className, "category");
        ScriptUtilities.AssignIntFieldToObject(1, playerArmyPrefab, className, "category");
        ScriptUtilities.AssignIntFieldToObject(1, enemyArmyPrefab, className, "category");

        // Create faction objects
        string playerTag = scriptPrefix + "Blue";
        string enemyTag = scriptPrefix + "Red";
        GameObject playerFaction = InitializeFactionObject(factionPrefab, "PlayerFaction", playerTag, new Vector3Int(3, 2, 0), 20, Color.blue);
        GameObject enemyFaction = InitializeFactionObject(factionPrefab, "EnemyFaction", enemyTag, new Vector3Int(-3, -2, 0), 0, Color.red);
        ScriptUtilities.AttachScriptToObject(scriptPrefix + "EnemyAi", enemyFaction);
        ScriptUtilities.AssignObjectFieldToObject(playerArmyPrefab, playerFaction, scriptPrefix + "Faction", "armyPrefab");
        ScriptUtilities.AssignObjectFieldToObject(enemyArmyPrefab, enemyFaction, scriptPrefix + "Faction", "armyPrefab");
        // Add initial units and buildings
        Tilemap tilemap = gridObject.transform.GetChild(0).GetComponent<Tilemap>();
        AddPlayerUnitsAndBuildings(tilemap, playerFaction, playerArmyPrefab, playerCastlePrefab);
        AddEnemyUnitsAndBuildings(tilemap, enemyFaction, enemyArmyPrefab, enemyCastlePrefab);
        // Set player faction to game board
        ScriptUtilities.AssignObjectFieldToObject(playerFaction, gridObject, scriptPrefix + "Board", "playerFaction");
        ScriptUtilities.AssignObjectFieldToObject(enemyFaction, gridObject, scriptPrefix + "Board", "enemyFaction");

        // Assign hex info objects
        AssignHexInfoTextObjects(hexInfoObject);
        AssignUnitInfoTextObjects(unitInfoObject);

        // Create initial objects
        InstantiateAndSetupGameManager(gameManagerPrefab);
        // Set up object fields
        SetupGridObject();

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

    private static void AddCastle(GameObject faction, GameObject castlePrefab, Vector3 worldPosition, bool isCapital = false)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(castlePrefab) as GameObject;
        newObject.transform.position = worldPosition;
        newObject.transform.SetParent(faction.transform);
        if (isCapital)
        {
            ScriptUtilities.AssignObjectFieldToObject(newObject, faction, scriptPrefix + "Faction", "capital");
        }
    }

    private static void AddEnemyUnitsAndBuildings(Tilemap tilemap, GameObject factionObject, GameObject armyPrefab, GameObject castlePrefab)
    {
        AddCastle(factionObject, castlePrefab, tilemap.CellToWorld(new Vector3Int(-3, 2, 0)));
        AddUnit(factionObject, armyPrefab, tilemap.CellToWorld(new Vector3Int(-1, 0, 0)));
        AddUnit(factionObject, armyPrefab, tilemap.CellToWorld(new Vector3Int(-2, 2, 0)));
        AddUnit(factionObject, armyPrefab, tilemap.CellToWorld(new Vector3Int(-2, -2, 0)));
        AddUnit(factionObject, armyPrefab, tilemap.CellToWorld(new Vector3Int(-3, 2, 0)));
    }

    private static void AddPlayerUnitsAndBuildings(Tilemap tilemap, GameObject factionObject, GameObject armyPrefab, GameObject castlePrefab)
    {
        ScriptUtilities.AssignObjectFieldToObject(armyPrefab, factionObject, scriptPrefix + "Faction", "armyPrefab");
        AddCastle(factionObject, castlePrefab, tilemap.CellToWorld(new Vector3Int(3, -2, 0)));
        AddUnit(factionObject, armyPrefab, tilemap.CellToWorld(new Vector3Int(2, 0, 0)));
    }

    private static void AddUnit(GameObject faction, GameObject unitPrefab, Vector3 worldPosition)
    {
        GameObject newObject = PrefabUtility.InstantiatePrefab(unitPrefab) as GameObject;
        newObject.transform.position = worldPosition;
        newObject.transform.SetParent(faction.transform);
    }

    private static void AssignHexInfoTextObjects(GameObject hexInfoObject)
    {
        string className = scriptPrefix + "HexInfo";
        Text text;

        // Type
        text = hexInfoObject.transform.GetChild(0).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "typeText");
        // Position
        text = hexInfoObject.transform.GetChild(1).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "positionText");
        // Tax
        text = hexInfoObject.transform.GetChild(2).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "taxText");
        // Defense
        text = hexInfoObject.transform.GetChild(3).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "defenseText");
        // Move
        text = hexInfoObject.transform.GetChild(4).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "moveText");
        // Faction
        text = hexInfoObject.transform.GetChild(5).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, hexInfoObject, className, "factionText");
    }

    private static void AssignUnitInfoTextObjects(GameObject unitInfoObject)
    {
        string className = scriptPrefix + "UnitInfo";
        Text text;

        // Type
        text = unitInfoObject.transform.GetChild(0).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "typeText");
        // Position
        text = unitInfoObject.transform.GetChild(1).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "positionText");
        // Strength
        text = unitInfoObject.transform.GetChild(2).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "strengthText");
        // Cost
        text = unitInfoObject.transform.GetChild(3).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "upkeepText");
        // Move
        text = unitInfoObject.transform.GetChild(4).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "moveText");
        // Faction
        text = unitInfoObject.transform.GetChild(5).gameObject.GetComponent<Text>();
        ScriptUtilities.AssignTextFieldToObject(text, unitInfoObject, className, "factionText");
    }

    private static GameObject CreateFactionGamePiece(string name, GameObject prefab, string tag, Color color)
    {
        // From a basic prefab (unit or building)
        GameObject newObject = Instantiate(prefab);
        newObject.name = name;
        newObject.tag = tag;
        newObject.GetComponent<SpriteRenderer>().color = color;

        return newObject;
    }

    private static void CreateInfoWindow(Transform parent, string objectName, string[] names, string[] texts, float margin, float textW, float textH)
    {
        // Create background parent
        float w = textW + 2.0f * margin;
        float h = textH * names.Length + 2.0f * margin;
        GameObject newObject = ContentUtilities.CreateUIBackgroundObject(objectName, w, h);
        ContentUtilities.AnchorUIObject(newObject, parent, ContentUtilities.Anchor.Right, new Vector2(-margin, 0));
        Transform localParent = newObject.transform;

        // Create text contents iteratively
        w = textW;
        h = textH;
        int fontSize = 14;
        Color color = Color.white;
        ContentUtilities.Anchor anchor = ContentUtilities.Anchor.TopLeft;
        Vector2 offset = new Vector2(margin, -margin);
        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];
            string text = texts[i];
            GameObject textObject = ContentUtilities.CreateUITextObject(name, w, h, text, TextAnchor.MiddleLeft, fontSize, color);
            ContentUtilities.AnchorUIObject(textObject, localParent, anchor, offset);
            offset.y -= h;
        }
    }

    private static void DrawArmyTexture()
    {
        int h = 32;
        int w = 16;
        int tH = h - w / 2;

        Color[] bitmap = ContentUtilities.FillBitmap(w, h, Color.clear);
        Color[] circle = ContentUtilities.FillBitmapShapeCircle(w, w, Color.white);
        Color[] triangle = ContentUtilities.FillBitmapShapeTriangle(w, tH, Color.white);
        bitmap = ContentUtilities.CopyBitmap(triangle, w, tH, bitmap, w, h, Vector2Int.zero);
        bitmap = ContentUtilities.CopyBitmap(circle, w, w, bitmap, w, h, new Vector2Int(0, h - w - 1));

        string path = ContentUtilities.CreateBitmapAsset("texture_army", bitmap, w, h, texturesPath);
        spritePaths[(int)SpritePath.Army] = path;
    }

    private static int[] GetGroundLayerMapArray()
    {
        return new int[]
        {
            // Array size (23, 15)
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1,  0,  3,  3,  1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  3,  1,  1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1,  0,  0,  1,  1,  1,  0,  0, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  1,  1,  3,  0,  2,  2, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  1,  3,  0,  0,  2,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  0,  1,  1,  0,  1,  3,  0,  1,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  0,  1,  1,  1,  0,  0,  0,  1,  1,  0,  0, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1,  0,  1,  1,  0,  3,  0,  0,  1,  0,  0, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  0,  3,  0,  2,  0,  2,  2, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1,  0,  0,  2,  3,  2,  0,  0,  2, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1,  1,  2,  2,  2,  2,  1,  0, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1,  1,  2,  2,  2,  1,  1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1,  1,  2,  2,  1,  0, -1, -1, -1, -1, -1, -1, -1, -1, -1,
             -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };
    }

    private static int[] GetSeaLayerMapArray()
    {
        return new int[]
        {
            // Array size(23, 15)
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,
              4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4,  4, -1
        };
    }

    private static GameObject InitializeFactionObject(GameObject prefab, string name, string tag, Vector3Int position, int gold, Color color)
    {
        string className = scriptPrefix + "Faction";
        GameObject factionObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        factionObject.name = name;
        factionObject.tag = tag;
        factionObject.transform.position = new Vector3(position.x, position.y, position.z);
        ScriptUtilities.AssignIntFieldToObject(gold, factionObject, className, "gold");
        ScriptUtilities.AssignColorFieldToObject(color, factionObject, className, "color");

        return factionObject;
    }

    private static void LayoutLevel()
    {
        // Get the tile maps
        GameObject groundLayerObject = GameObject.Find("GroundLayer");
        GameObject seaLayerObject = GameObject.Find("SeaLayer");
        Tilemap groundTilemap = groundLayerObject.GetComponent<Tilemap>();
        Tilemap seaTilemap = seaLayerObject.GetComponent<Tilemap>();
        // Create a tiles guide (order matters)
        Tile[] tilesGuide = new Tile[tilePaths.Length];
        for (int i = 0; i < tilesGuide.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTileAtPath(tilePaths[i]);
        }
        // Configure size and start points
        Vector2Int size = new Vector2Int(GetSeaLayerMapArray().Length / 15, 15);
        Vector2Int start = new Vector2Int(-11, -7);
        // Layout according to a map array
        ContentUtilities.PlotTiles(seaTilemap, tilesGuide, GetSeaLayerMapArray(), size, start);
        ContentUtilities.PlotTiles(groundTilemap, tilesGuide, GetGroundLayerMapArray(), size, start);
    }

    private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
        GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        string className = scriptPrefix + "GameManager";

        // Get objects
        GameObject boardObject = GameObject.Find("Grid");
        GameObject hexInfoObject = GameObject.Find("HexInfo");
        GameObject unitInfoObject = GameObject.Find("UnitInfo");
        GameObject contextHelpObject = GameObject.Find("ContextHelp");
        GameObject goldTextObject = GameObject.Find("GoldText");
        GameObject turnsTextObject = GameObject.Find("TurnsText");
        GameObject resultPanelObject = GameObject.Find("ResultPanel");
        GameObject resultTextObject = GameObject.Find("ResultText");
        GameObject endTurnButtonObject = GameObject.Find("EndTurnButton");
        GameObject recruitButtonObject = GameObject.Find("RecruitButton");
        GameObject helpPanelObject = GameObject.Find("HelpPanel");
        GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
        // Set fields
        ScriptUtilities.AssignComponentFieldToObject(boardObject, scriptPrefix + "Board", go, className, "board");
        ScriptUtilities.AssignComponentFieldToObject(hexInfoObject, scriptPrefix + "HexInfo", go, className, "hexInfo");
        ScriptUtilities.AssignComponentFieldToObject(unitInfoObject, scriptPrefix + "UnitInfo", go, className, "unitInfo");
        ScriptUtilities.AssignComponentFieldToObject(contextHelpObject, scriptPrefix + "ContextHelp", go, className, "contextHelp");
        ScriptUtilities.AssignComponentFieldToObject(goldTextObject, "Text", go, className, "goldText");
        ScriptUtilities.AssignComponentFieldToObject(turnsTextObject, "Text", go, className, "turnsText");
        ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
        ScriptUtilities.AssignComponentFieldToObject(endTurnButtonObject, "Button", go, className, "endTurnButton");
        ScriptUtilities.AssignComponentFieldToObject(recruitButtonObject, "Button", go, className, "recruitButton");
        ScriptUtilities.AssignObjectFieldToObject(helpPanelObject, go, className, "helpPanelObject");
        ScriptUtilities.AssignComponentFieldToObject(pressAnyKeyTextObject, "Text", go, className, "pressAnyKeyText");
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
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DBlinkText", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DBoard", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DBuilding", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DContextHelp", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DDeployable", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DEnemyAi", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DFaction", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DGameManager", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DHexInfo", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DInfo", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DInputController", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DNode", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DPathFinder", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DUnitInfo", scriptsPath);
        ScriptUtilities.ConvertScriptToStringBuilder("HS2DUnit", scriptsPath);
        // Refresh
        AssetDatabase.Refresh();
    }

    private static void ReverseEngineerTilemaps()
    {
        // Get tilemaps
        Tilemap seaTilemap = GameObject.Find("SeaLayer").GetComponent<Tilemap>();
        Tilemap groundTilemap = GameObject.Find("GroundLayer").GetComponent<Tilemap>();
        int w = seaTilemap.size.x;
        int h = seaTilemap.size.y;
        int startX = -11;
        int startY = -7;
        // Build tile guide
        string[] tileNames = new string[] { "tile_plains", "tile_forest", "tile_hill", "tile_water", "tile_sea" };
        Tile[] tilesGuide = new Tile[(int)TilePath.Max];
        Assert.IsTrue(tileNames.Length == (int)TilePath.Max);
        for (int i = 0; i < tilePaths.Length; i++)
        {
            tilesGuide[i] = ContentUtilities.LoadTile(tileNames[i], tilesPath);
        }
        // Reverse sea layer
        int[] mapArray = ContentUtilities.ConvertTilemapToMapArray(seaTilemap, tilesGuide, w, h, startX, startY);
        StringBuilder sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        Debug.Log("Printing " + seaTilemap.name);
        Debug.Log(sb.ToString());
        // Reverse ground layer
        mapArray = ContentUtilities.ConvertTilemapToMapArray(groundTilemap, tilesGuide, w, h, startX, startY);
        sb = ContentUtilities.ConvertMapArrayToString(mapArray, w, h);
        Debug.Log("Printing " + groundTilemap.name);
        Debug.Log(sb.ToString());
    }

    private static void SetupGridObject()
    {
        string className = scriptPrefix + "Board";
        // Get objects
        GameObject gridObject = GameObject.Find("Grid");
        GameObject cameraObject = GameObject.Find("Main Camera");
        GameObject groundLayerObject = GameObject.Find("GroundLayer");
        GameObject seaLayerObject = GameObject.Find("SeaLayer");
        GameObject nodesContainerObject = GameObject.Find("NodesContainer");
        // Assign objects and components
        ScriptUtilities.AssignComponentFieldToObject(cameraObject, "Camera", gridObject, className, "mainCamera");
        ScriptUtilities.AssignComponentFieldToObject(groundLayerObject, "Tilemap", gridObject, className, "groundTilemap");
        ScriptUtilities.AssignComponentFieldToObject(seaLayerObject, "Tilemap", gridObject, className, "seaTilemap");
        ScriptUtilities.AssignObjectFieldToObject(nodesContainerObject, gridObject, className, "nodesContainer");
        // Load tiles
        Tile plainsTile = ContentUtilities.LoadTile("tile_plains", tilesPath);
        Tile forestTile = ContentUtilities.LoadTile("tile_forest", tilesPath);
        Tile hillTile = ContentUtilities.LoadTile("tile_hill", tilesPath);
        Tile waterTile = ContentUtilities.LoadTile("tile_water", tilesPath);
        Tile seaTile = ContentUtilities.LoadTile("tile_sea", tilesPath);
        // Assign tiles
        ScriptUtilities.AssignAnyFieldToObject(plainsTile, gridObject, className, "plainsTile");
        ScriptUtilities.AssignAnyFieldToObject(forestTile, gridObject, className, "forestTile");
        ScriptUtilities.AssignAnyFieldToObject(hillTile, gridObject, className, "hillTile");
        ScriptUtilities.AssignAnyFieldToObject(waterTile, gridObject, className, "waterTile");
        ScriptUtilities.AssignAnyFieldToObject(seaTile, gridObject, className, "seaTile");
    }

    private static void WriteHS2DBlinkTextScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1458);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DBlinkText : MonoBehaviour");
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

        ScriptUtilities.CreateScriptFile("HS2DBlinkText", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DBoardScriptToFile()
    {
        StringBuilder sb = new StringBuilder(26959);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DBoard : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    [HideInInspector] ");
        sb.AppendLine("    public HS2DUnit selectedUnit = null;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public HS2DNode selectedNode = null;");
        sb.AppendLine("");
        sb.AppendLine("    public Camera mainCamera;");
        sb.AppendLine("    public Tilemap groundTilemap;");
        sb.AppendLine("    public Tilemap seaTilemap;");
        sb.AppendLine("    public GameObject nodesContainer;");
        sb.AppendLine("    public GameObject hexNodePrefab;");
        sb.AppendLine("    public GameObject playerFaction;");
        sb.AppendLine("    public GameObject enemyFaction;");
        sb.AppendLine("    public LayerMask unitsLayerMask;");
        sb.AppendLine("    public Tile plainsTile;");
        sb.AppendLine("    public Tile forestTile;");
        sb.AppendLine("    public Tile hillTile;");
        sb.AppendLine("    public Tile waterTile;");
        sb.AppendLine("    public Tile seaTile;");
        sb.AppendLine("    public int width = 23;");
        sb.AppendLine("    public int height = 15;");
        sb.AppendLine("    public int armyPrice = 10;");
        sb.AppendLine("    public float animTime = 1.0f;");
        sb.AppendLine("    public static int Impassable = -1;");
        sb.AppendLine("    public static int Open = 1;");
        sb.AppendLine("    public static int Difficult = 2;");
        sb.AppendLine("    public List<HS2DUnit> units = new List<HS2DUnit>();");
        sb.AppendLine("    private Dictionary<Vector3Int, HS2DNode> nodesDictionary = new Dictionary<Vector3Int, HS2DNode>();");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("    private HS2DPathFinder pathFinder = new HS2DPathFinder();");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public List<HS2DNode> nodesInMoveRange;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public List<HS2DNode> nodesInAttackRange;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public List<HS2DNode> nodesInChargeRange = new List<HS2DNode>(18); // Size = (max move + 1) * 6");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get objects references");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("        // Initialize hex nodes");
        sb.AppendLine("        BuildNodes();");
        sb.AppendLine("        ConnectNodes();");
        sb.AppendLine("        pathFinder.Initialize(nodesDictionary);");
        sb.AppendLine("        SetOverlayForAllNodes(HS2DNode.Status.Clear);");
        sb.AppendLine("        // Initialize factions");
        sb.AppendLine("        playerFaction.GetComponent<HS2DFaction>().Initialize(this);");
        sb.AppendLine("        enemyFaction.GetComponent<HS2DFaction>().Initialize(this);");
        sb.AppendLine("        gameManager.NotifyGoldChanged(playerFaction.GetComponent<HS2DFaction>().gold);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AddNode(HS2DNode node, Vector3Int position)");
        sb.AppendLine("    {");
        sb.AppendLine("        nodesDictionary.Add(position, node);");
        sb.AppendLine("        node.transform.SetParent(nodesContainer.transform);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void BuildNodes()");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3Int marker = Vector3Int.zero;");
        sb.AppendLine("");
        sb.AppendLine("        // Build a path finding node for each ground tile");
        sb.AppendLine("        int hWidth = width / 2;");
        sb.AppendLine("        int hHeight = height / 2;");
        sb.AppendLine("        for (int x = -hWidth; x <= hWidth; x++)");
        sb.AppendLine("        {");
        sb.AppendLine("            marker.x = x;");
        sb.AppendLine("            for (int y = -hHeight; y <= hHeight; y++)");
        sb.AppendLine("            {");
        sb.AppendLine("                marker.y = y;");
        sb.AppendLine("                Tile tile = groundTilemap.GetTile(marker) as Tile;");
        sb.AppendLine("                if (tile == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                Vector3 worldPosition = groundTilemap.CellToWorld(marker);");
        sb.AppendLine("                HS2DNode node = Instantiate(hexNodePrefab).GetComponent<HS2DNode>();");
        sb.AppendLine("                node.Initialize(tile, GetTileTerrain(tile), marker, worldPosition);");
        sb.AppendLine("                AddNode(node, marker);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ClearOverlay()");
        sb.AppendLine("    {");
        sb.AppendLine("        SetOverlayForAllNodes(HS2DNode.Status.Clear);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ConnectNodes()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Find neighbors for each node");
        sb.AppendLine("        foreach (KeyValuePair<Vector3Int, HS2DNode> element in nodesDictionary)");
        sb.AppendLine("        {");
        sb.AppendLine("            element.Value.FindNeighbors();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void EndTurn()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Update enemy AI actions");
        sb.AppendLine("        HS2DEnemyAi enemyAi = enemyFaction.GetComponent<HS2DEnemyAi>();");
        sb.AppendLine("        enemyAi.PerformAiActions();");
        sb.AppendLine("        // End turn for factions then update UI");
        sb.AppendLine("        StartCoroutine(WaitToEndAndUpdateUI(enemyAi.aiUpdateDuration));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public HS2DNode GetNode(Vector3Int cellPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        const int unused = 0;");
        sb.AppendLine("");
        sb.AppendLine("        cellPosition.z = unused;");
        sb.AppendLine("        HS2DNode rv;");
        sb.AppendLine("        if (!nodesDictionary.TryGetValue(cellPosition, out rv))");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        return rv;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private HS2DNode.Terrain GetTileTerrain(Tile tile)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Return terrain according to type of tile");
        sb.AppendLine("        if (tile == plainsTile)");
        sb.AppendLine("        {");
        sb.AppendLine("            return HS2DNode.Terrain.Plains;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (tile == forestTile)");
        sb.AppendLine("        {");
        sb.AppendLine("            return HS2DNode.Terrain.Forest;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (tile == hillTile)");
        sb.AppendLine("        {");
        sb.AppendLine("            return HS2DNode.Terrain.Hill;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (tile == waterTile)");
        sb.AppendLine("        {");
        sb.AppendLine("            return HS2DNode.Terrain.Water;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Default is sea tile");
        sb.AppendLine("        return HS2DNode.Terrain.Sea;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void HighlightNodesInAttackRange()");
        sb.AppendLine("    {");
        sb.AppendLine("        // If no unit is selected");
        sb.AppendLine("        if (selectedUnit == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If selected unit has no attacks left");
        sb.AppendLine("        if (selectedUnit.attacksLeft <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Highlight node with an enemy unit or building in range");
        sb.AppendLine("        foreach (HS2DNode node in nodesInAttackRange)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (node.occupyingUnit && node.occupyingUnit.CompareTag(enemyFaction.tag))");
        sb.AppendLine("            {");
        sb.AppendLine("                node.SetOverlayStatus(HS2DNode.Status.Attackable);");
        sb.AppendLine("            }");
        sb.AppendLine("            else if (node.localBuilding && node.localBuilding.CompareTag(enemyFaction.tag))");
        sb.AppendLine("            {");
        sb.AppendLine("                node.SetOverlayStatus(HS2DNode.Status.Attackable);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Also highlight nodes not adjacent but within move range");
        sb.AppendLine("        foreach (HS2DNode node in nodesInChargeRange)");
        sb.AppendLine("        {");
        sb.AppendLine("            node.SetOverlayStatus(HS2DNode.Status.Attackable);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void HighlightNodesInMoveRange()");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (HS2DNode node in nodesInMoveRange)");
        sb.AppendLine("        {");
        sb.AppendLine("            node.SetOverlayStatus(HS2DNode.Status.Interactable);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void HighlightSelectedNode()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (selectedNode)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Highlight selected node");
        sb.AppendLine("            selectedNode.SetOverlayStatus(HS2DNode.Status.Selected);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (selectedUnit)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Highlight the underlying node of the selected unit");
        sb.AppendLine("            GetNode(WorldToCellPosition(selectedUnit.transform.position)).SetOverlayStatus(HS2DNode.Status.Selected);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void MoveUnit(HS2DUnit unit, Vector3Int destination)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Clear highlights");
        sb.AppendLine("        SetOverlayForAllNodes(HS2DNode.Status.Clear);");
        sb.AppendLine("        // Find a path to the destination");
        sb.AppendLine("        Vector3Int origin = WorldToCellPosition(unit.transform.position);");
        sb.AppendLine("        List<HS2DNode> path = pathFinder.FindPath(origin, destination, unit.type);");
        sb.AppendLine("        if (path == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail and log a warning if a path is not found");
        sb.AppendLine("            Debug.LogWarning(\"Path not found!\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If a path is found");
        sb.AppendLine("        if (path.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Move unit to the destination");
        sb.AppendLine("            unit.Move(path);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyUnitBusy(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set busy flag");
        sb.AppendLine("        gameManager.busy = true;");
        sb.AppendLine("        // Start coroutine to wait");
        sb.AppendLine("        StartCoroutine(WaitToUnsetBusyFlag(wait));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyFactionDestroyed(HS2DFaction faction)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (faction.CompareTag(enemyFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Game won if the enemy faction has been destroyed");
        sb.AppendLine("            gameManager.GetComponent<HS2DGameManager>().GameWon();");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Game over if the player's faction has been destroyed");
        sb.AppendLine("            gameManager.GetComponent<HS2DGameManager>().GameOver();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyGamePieceDestruction(HS2DDeployable gamePiece)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Remove game piece from its faction");
        sb.AppendLine("        if (playerFaction.CompareTag(gamePiece.gameObject.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            playerFaction.GetComponent<HS2DFaction>().RemoveGamePiece(gamePiece);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            enemyFaction.GetComponent<HS2DFaction>().RemoveGamePiece(gamePiece);");
        sb.AppendLine("        }");
        sb.AppendLine("        // Remove references in location node");
        sb.AppendLine("        HS2DNode locationNode = GetNode(WorldToCellPosition(gamePiece.transform.position));");
        sb.AppendLine("        if (gamePiece.category == HS2DDeployable.Category.Building)");
        sb.AppendLine("        {");
        sb.AppendLine("            locationNode.localBuilding = null;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            locationNode.occupyingUnit = null;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Destroy game object");
        sb.AppendLine("        Destroy(gamePiece.gameObject);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RecruitNewArmy()");
        sb.AppendLine("    {");
        sb.AppendLine("        playerFaction.GetComponent<HS2DFaction>().RecruitArmy();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public Vector3Int ScreenToCellPosition(Vector3 screenPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3 worldPosition = ScreenToWorldPosition(screenPosition);");
        sb.AppendLine("        return WorldToCellPosition(worldPosition);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public Vector3 ScreenToWorldPosition(Vector3 screenPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        return mainCamera.ScreenToWorldPoint(screenPosition);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SelectAtPosition(Vector3 screenPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);");
        sb.AppendLine("        worldPosition.z = 0.0f;");
        sb.AppendLine("");
        sb.AppendLine("        // Check if any unit is picked");
        sb.AppendLine("        RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPosition.x, worldPosition.y), Vector2.zero, Mathf.Infinity, unitsLayerMask);");
        sb.AppendLine("        if (hit.collider != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DUnit unit = hit.collider.gameObject.GetComponent<HS2DUnit>();");
        sb.AppendLine("            if (unit != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                SelectUnit(unit);");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("        }               ");
        sb.AppendLine("");
        sb.AppendLine("        // Then, check ground layer if any tile is picked");
        sb.AppendLine("        Vector3Int cellPosition = groundTilemap.WorldToCell(worldPosition);");
        sb.AppendLine("        Tile tile = groundTilemap.GetTile(cellPosition) as Tile;");
        sb.AppendLine("        // Select node, if not null");
        sb.AppendLine("        if (tile != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            SelectNode(cellPosition);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SelectNode(Vector3Int cellPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        UnselectPrevious();");
        sb.AppendLine("        HS2DNode output;");
        sb.AppendLine("        if (nodesDictionary.TryGetValue(cellPosition, out output))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Select node");
        sb.AppendLine("            selectedNode = output;");
        sb.AppendLine("            gameManager.NotifyNodeSelection(selectedNode);");
        sb.AppendLine("            // Highlight the node");
        sb.AppendLine("            HighlightSelectedNode();");
        sb.AppendLine("            // If castle selected and has the  money to recruit");
        sb.AppendLine("            if (!selectedNode.occupyingUnit ");
        sb.AppendLine("                && selectedNode.localBuilding ");
        sb.AppendLine("                && selectedNode.localBuilding.CompareTag(playerFaction.tag)");
        sb.AppendLine("                && playerFaction.GetComponent<HS2DFaction>().gold >= armyPrice)");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.ActivateRecruitButton(true);");
        sb.AppendLine("            }");
        sb.AppendLine("            else");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.ActivateRecruitButton(false);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Nothing is selected");
        sb.AppendLine("            gameManager.NotifyNodeSelection(null);");
        sb.AppendLine("            gameManager.ActivateRecruitButton(false);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SelectUnit(HS2DUnit unit)");
        sb.AppendLine("    {");
        sb.AppendLine("        UnselectPrevious();");
        sb.AppendLine("        selectedUnit = unit;");
        sb.AppendLine("        // Find nodes in range");
        sb.AppendLine("        UpdateSelectedUnitInfo();");
        sb.AppendLine("        // Update UI");
        sb.AppendLine("        gameManager.NotifyUnitSelection(unit);");
        sb.AppendLine("        gameManager.ActivateRecruitButton(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void SetOverlayForAllNodes(HS2DNode.Status status)");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (KeyValuePair<Vector3Int, HS2DNode> pair in nodesDictionary)");
        sb.AppendLine("        {");
        sb.AppendLine("            pair.Value.SetOverlayStatus(status);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void StartCharge(HS2DUnit attacker, HS2DDeployable defender, HS2DNode terrainNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Find a path to the destination");
        sb.AppendLine("        Vector3Int origin = WorldToCellPosition(attacker.transform.position);");
        sb.AppendLine("        Vector3Int destination = WorldToCellPosition(terrainNode.attackedFromNode.transform.position);");
        sb.AppendLine("        List<HS2DNode> path = pathFinder.FindPath(origin, destination, attacker.type);");
        sb.AppendLine("        // If a path is not found");
        sb.AppendLine("        if (path == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail");
        sb.AppendLine("            Debug.LogWarning(\"Can't find a path to the target at \" + destination);");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If a path still exists");
        sb.AppendLine("        if (path.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Move unit to the destination");
        sb.AppendLine("            attacker.MoveAndAttack(path, defender, terrainNode, animTime);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void StartCombat(HS2DUnit attacker, HS2DDeployable defender, HS2DNode terrainNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        attacker.Attack(defender, terrainNode, animTime);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UnselectPrevious()");
        sb.AppendLine("    {");
        sb.AppendLine("        selectedUnit = null;");
        sb.AppendLine("        selectedNode = null;");
        sb.AppendLine("        SetOverlayForAllNodes(HS2DNode.Status.Clear);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateNodesInAttackRange(HS2DNode startNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Get neighbor nodes, these are all within attack range");
        sb.AppendLine("        nodesInAttackRange = startNode.neighbors;");
        sb.AppendLine("");
        sb.AppendLine("        // First, clear the list");
        sb.AppendLine("        nodesInChargeRange.Clear();");
        sb.AppendLine("");
        sb.AppendLine("        // For each node that is reachable");
        sb.AppendLine("        foreach (HS2DNode reachableNode in nodesInMoveRange)");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (HS2DNode neighbor in reachableNode.neighbors)");
        sb.AppendLine("            {");
        sb.AppendLine("                // If already in the list");
        sb.AppendLine("                if (nodesInChargeRange.Contains(neighbor))");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Skip to the next node");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // If this node has an enemy unit");
        sb.AppendLine("                if (neighbor.occupyingUnit && neighbor.occupyingUnit.CompareTag(enemyFaction.tag))");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Find the closest node to the player's unit");
        sb.AppendLine("                    List<HS2DNode> backTrace = pathFinder.FindPath(neighbor.position, startNode.position, selectedUnit.type, ignoreDestination: true);");
        sb.AppendLine("                    neighbor.attackedFromNode = backTrace[1];");
        sb.AppendLine("                    // Add it to the list");
        sb.AppendLine("                    nodesInChargeRange.Add(neighbor);");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // If this node has an enemy building");
        sb.AppendLine("                if (neighbor.localBuilding && neighbor.localBuilding.CompareTag(enemyFaction.tag))");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Find the closest node to the player's unit");
        sb.AppendLine("                    List<HS2DNode> backTrace = pathFinder.FindPath(neighbor.position, startNode.position, selectedUnit.type, ignoreDestination: true);");
        sb.AppendLine("                    neighbor.attackedFromNode = backTrace[1];");
        sb.AppendLine("                    // Add it to the list");
        sb.AppendLine("                    nodesInChargeRange.Add(neighbor);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void UpdateSelectedUnitInfo()");
        sb.AppendLine("    {");
        sb.AppendLine("        ClearOverlay();");
        sb.AppendLine("        if (selectedUnit == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Bail if no unit is selected");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Highlight the node");
        sb.AppendLine("        HighlightSelectedNode();");
        sb.AppendLine("        // If not the player's faction");
        sb.AppendLine("        if (!selectedUnit.CompareTag(playerFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Nothing else to do");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Call the path finder to find nodes in move range");
        sb.AppendLine("        Vector3Int cellPosition = WorldToCellPosition(selectedUnit.transform.position);");
        sb.AppendLine("        nodesInMoveRange = pathFinder.FindNodesInRange(cellPosition, selectedUnit.moveLeft, selectedUnit.type);");
        sb.AppendLine("        HS2DNode node = GetNode(cellPosition);");
        sb.AppendLine("        // Update nodes in attack range");
        sb.AppendLine("        UpdateNodesInAttackRange(node);");
        sb.AppendLine("        // Update high lights");
        sb.AppendLine("        HighlightNodesInMoveRange();");
        sb.AppendLine("        HighlightNodesInAttackRange();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEndAndUpdateUI(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set the busy flag");
        sb.AppendLine("        gameManager.busy = true;");
        sb.AppendLine("        // Wait");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        // End turn calculations");
        sb.AppendLine("        playerFaction.GetComponent<HS2DFaction>().EndTurn();");
        sb.AppendLine("        enemyFaction.GetComponent<HS2DFaction>().EndTurn();");
        sb.AppendLine("        // Update UI and highlights");
        sb.AppendLine("        gameManager.NotifyGoldChanged(playerFaction.GetComponent<HS2DFaction>().gold);");
        sb.AppendLine("        gameManager.UpdateTurnsRemaining();");
        sb.AppendLine("        if (selectedUnit)");
        sb.AppendLine("        {");
        sb.AppendLine("            UpdateSelectedUnitInfo();");
        sb.AppendLine("            gameManager.NotifyUnitSelection(selectedUnit);");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (selectedNode)");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.NotifyNodeSelection(selectedNode);");
        sb.AppendLine("        }");
        sb.AppendLine("        gameManager.busy = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToUnsetBusyFlag(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Wait");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        // Unset flag");
        sb.AppendLine("        gameManager.busy = false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public Vector3Int WorldToCellPosition(Vector3 worldPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        const int unused = 0;");
        sb.AppendLine("        Vector3Int position = groundTilemap.WorldToCell(worldPosition);");
        sb.AppendLine("        position.z = unused;");
        sb.AppendLine("        return position;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DBoard", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DBuildingScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1696);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DBuilding : HS2DDeployable");
        sb.AppendLine("{");
        sb.AppendLine("    public int defense = 1;");
        sb.AppendLine("    public int tax = 5;");
        sb.AppendLine("    public int durability = 1000;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public HS2DNode node;");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public override int RollDamage()");
        sb.AppendLine("    {");
        sb.AppendLine("        return Random.Range(1, 3) * 100;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public override void TakeDamage(int damage)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Take damage");
        sb.AppendLine("        durability -= damage;");
        sb.AppendLine("        if (durability <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Tell the gameboard that this piece has been destroyed");
        sb.AppendLine("            durability = 0;");
        sb.AppendLine("            gameManager.board.NotifyGamePieceDestruction(this);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DBuilding", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DContextHelpScriptToFile()
    {
        StringBuilder sb = new StringBuilder(7733);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DContextHelp : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float textDuration = 30.0f;");
        sb.AppendLine("    public float fadeDuration = 0.75f;");
        sb.AppendLine("    private Text guideText;");
        sb.AppendLine("    private Image backGround;");
        sb.AppendLine("    private Color backGroundColor;");
        sb.AppendLine("    private Color guideTextColor;");
        sb.AppendLine("    private Color transparence = new Color(0.0f, 0.0f, 0.0f, 0.0f);");
        sb.AppendLine("    private bool[] shownFlags;");
        sb.AppendLine("    private IEnumerator oldFadeForDuration;");
        sb.AppendLine("    private IEnumerator oldShowForDuration;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Guide");
        sb.AppendLine("    {");
        sb.AppendLine("        Select = 0,");
        sb.AppendLine("        Action,");
        sb.AppendLine("        Recruit,");
        sb.AppendLine("        EndTurn,");
        sb.AppendLine("        GoodLuck,");
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
        sb.AppendLine("    public void Show(Guide guide)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Stop all local coroutines so they don't interfere with the new calls");
        sb.AppendLine("        StopAllLocalCoroutines();");
        sb.AppendLine("");
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
        sb.AppendLine("            case Guide.Select:");
        sb.AppendLine("                SetText(\"Left click on a Blue Troop or Castle to Select it\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.Action:");
        sb.AppendLine("                SetText(\"Right Click on a green-lit hex to move or a red-lit one to attack.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.Recruit:");
        sb.AppendLine("                SetText(\"Click the Recruit button to recruit troops\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.EndTurn:");
        sb.AppendLine("                SetText(\"After making your moves, click the End Turn button to start a new turn.\");");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Guide.GoodLuck:");
        sb.AppendLine("                SetText(\"Destroy the Enemy's Castle before the Turns run out. Good Luck!\");");
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
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DContextHelp", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DDeployableScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1036);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("abstract public class HS2DDeployable : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public Category category;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Category");
        sb.AppendLine("    {");
        sb.AppendLine("        Building = 0,");
        sb.AppendLine("        Unit,");
        sb.AppendLine("        Unknown,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    virtual public int RollDamage()");
        sb.AppendLine("    {");
        sb.AppendLine("        return 0;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    abstract public void TakeDamage(int damage);");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DDeployable", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DEnemyAiScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3684);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DEnemyAi : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public float animTime = 0.75f;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public float aiUpdateDuration = 0.0f;");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void PerformAiActions()");
        sb.AppendLine("    {");
        sb.AppendLine("        HS2DFaction faction = GetComponent<HS2DFaction>();");
        sb.AppendLine("        HS2DBoard board = gameManager.board;");
        sb.AppendLine("        const float additionalWait = 0.5f;");
        sb.AppendLine("        aiUpdateDuration = 0.0f;");
        sb.AppendLine("        ");
        sb.AppendLine("        // Each unit will attack adjacent enemy");
        sb.AppendLine("        foreach (HS2DUnit unit in faction.units)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DNode node = gameManager.board.GetNode(board.WorldToCellPosition(unit.transform.position));");
        sb.AppendLine("            int lowestStrength = int.MaxValue;");
        sb.AppendLine("            HS2DNode target = null;");
        sb.AppendLine("            // Search for appropriate hostile target within reach");
        sb.AppendLine("            foreach (HS2DNode neighbor in node.neighbors)");
        sb.AppendLine("            {");
        sb.AppendLine("                HS2DUnit occupyingUnit = neighbor.occupyingUnit;");
        sb.AppendLine("                if (occupyingUnit == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (occupyingUnit.CompareTag(tag))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                if (occupyingUnit.strength >= lowestStrength)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                lowestStrength = occupyingUnit.strength;");
        sb.AppendLine("                target = neighbor;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Target found");
        sb.AppendLine("            if (target != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Attack it");
        sb.AppendLine("                StartCoroutine(WaitToAttack(aiUpdateDuration, unit, target.occupyingUnit, target));");
        sb.AppendLine("                // Increase the wait amount with the animation duration plus a small gap");
        sb.AppendLine("                aiUpdateDuration += animTime + additionalWait;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    IEnumerator WaitToAttack(float wait, HS2DUnit attacker, HS2DUnit defender, HS2DNode terrainNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        attacker.Attack(defender, terrainNode, animTime);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DEnemyAi", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DFactionScriptToFile()
    {
        StringBuilder sb = new StringBuilder(8386);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using static HS2DDeployable;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DFaction : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int gold;");
        sb.AppendLine("    public Color color;");
        sb.AppendLine("    public GameObject armyPrefab;");
        sb.AppendLine("    public List<HS2DUnit> units = new List<HS2DUnit>();");
        sb.AppendLine("    public List<HS2DBuilding> buildings = new List<HS2DBuilding>();");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public GameObject capital;");
        sb.AppendLine("    private HS2DBoard board;");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(HS2DBoard board)");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("        this.board = board;");
        sb.AppendLine("        // Get all children as faction assets");
        sb.AppendLine("        for (int i = 0; i < transform.childCount; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DDeployable deloyableChild = transform.GetChild(i).GetComponent<HS2DDeployable>();");
        sb.AppendLine("            Vector3Int cellPosition = board.WorldToCellPosition(deloyableChild.transform.position);");
        sb.AppendLine("            HS2DNode node = board.GetNode(cellPosition);");
        sb.AppendLine("            switch (deloyableChild.category)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Add buildings");
        sb.AppendLine("                case Category.Building:");
        sb.AppendLine("                    node.localBuilding = deloyableChild as HS2DBuilding;");
        sb.AppendLine("                    node.localBuilding.node = node;                    ");
        sb.AppendLine("                    buildings.Add(node.localBuilding);");
        sb.AppendLine("                    if (buildings.Count == 1)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        capital = node.localBuilding.gameObject; // Make the first building the capital");
        sb.AppendLine("                    }");
        sb.AppendLine("                    break;");
        sb.AppendLine("");
        sb.AppendLine("                // Add units");
        sb.AppendLine("                case Category.Unit:");
        sb.AppendLine("                    node.occupyingUnit = deloyableChild as HS2DUnit;");
        sb.AppendLine("                    units.Add(node.occupyingUnit);                    ");
        sb.AppendLine("                    break;");
        sb.AppendLine("");
        sb.AppendLine("                default:");
        sb.AppendLine("                    break;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void AddGold(int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        gold += value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public int CalculateExpenses()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate total expenses for this turn");
        sb.AppendLine("        int expenses = 0;");
        sb.AppendLine("        // Add units' upkeep costs");
        sb.AppendLine("        foreach (HS2DUnit unit in units)");
        sb.AppendLine("        {");
        sb.AppendLine("            expenses += unit.upkeep;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return expenses;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public int CalculateIncome()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate total incomes for this turn");
        sb.AppendLine("        int income = 0;");
        sb.AppendLine("        // Add incomes from buildings");
        sb.AppendLine("        foreach (HS2DBuilding building in buildings)");
        sb.AppendLine("        {");
        sb.AppendLine("            income += building.node.CalculateTaxTotal();");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return income;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void EndTurn()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Call end turn for each unit");
        sb.AppendLine("        foreach (HS2DUnit unit in units)");
        sb.AppendLine("        {");
        sb.AppendLine("            unit.EndTurn();");
        sb.AppendLine("        }");
        sb.AppendLine("        // Calculate new gold amount");
        sb.AppendLine("        gold = gold + CalculateIncome() - CalculateExpenses();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsDestroyed()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (buildings.Count == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RecruitArmy()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if has enough gold");
        sb.AppendLine("        if (gold < board.armyPrice)");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.Log(\"Not enough gold!\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Check if this faction has a capital");
        sb.AppendLine("        HS2DNode capitalNode = board.GetNode(board.WorldToCellPosition(capital.transform.position));");
        sb.AppendLine("        if (capitalNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.Log(\"Capital Null\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Check if the capital is already occupied (i.e. recruited but has not moved the new unit out)");
        sb.AppendLine("        if (capitalNode.occupyingUnit != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Debug.Log(\"Capital is occupied!\");");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Recruit a new unit");
        sb.AppendLine("        GameObject newArmy = Instantiate(armyPrefab);");
        sb.AppendLine("        newArmy.name = armyPrefab.name;");
        sb.AppendLine("        newArmy.transform.position = capital.transform.position;");
        sb.AppendLine("        newArmy.transform.SetParent(transform);");
        sb.AppendLine("        capitalNode.occupyingUnit = newArmy.GetComponent<HS2DUnit>();");
        sb.AppendLine("        units.Add(capitalNode.occupyingUnit);");
        sb.AppendLine("        // If this is the player's faction");
        sb.AppendLine("        if (CompareTag(board.playerFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            // Refresh this unit");
        sb.AppendLine("            capitalNode.occupyingUnit.EndTurn();");
        sb.AppendLine("            // Select this unit");
        sb.AppendLine("            board.SelectUnit(capitalNode.occupyingUnit);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Minus the gold");
        sb.AppendLine("        gold -= board.armyPrice;");
        sb.AppendLine("        gameManager.NotifyGoldChanged(gold);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void RemoveGamePiece(HS2DDeployable gamePiece)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (gamePiece.category == HS2DDeployable.Category.Building)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Remove a destroyed building from the list");
        sb.AppendLine("            buildings.Remove(gamePiece as HS2DBuilding);");
        sb.AppendLine("            if (IsDestroyed())");
        sb.AppendLine("            {");
        sb.AppendLine("                board.NotifyFactionDestroyed(this);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Remove a destroyed unit from the list");
        sb.AppendLine("            units.Remove(gamePiece as HS2DUnit);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DFaction", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DGameManagerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(11673);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.SceneManagement;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DGameManager : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public int maxTurns = 24;");
        sb.AppendLine("    public HS2DBoard board;");
        sb.AppendLine("    public HS2DHexInfo hexInfo;");
        sb.AppendLine("    public  HS2DUnitInfo unitInfo;");
        sb.AppendLine("    public HS2DContextHelp contextHelp;");
        sb.AppendLine("    public Text goldText;");
        sb.AppendLine("    public Text turnsText;");
        sb.AppendLine("    public GameObject resultPanelObject;");
        sb.AppendLine("    public Text resultText;");
        sb.AppendLine("    public Button endTurnButton;");
        sb.AppendLine("    public Button recruitButton;");
        sb.AppendLine("    public GameObject helpPanelObject;");
        sb.AppendLine("    public Text pressAnyKeyText;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public bool busy = false;");
        sb.AppendLine("    private int currentTurn = 0;");
        sb.AppendLine("    private static bool gameStarted = false;");
        sb.AppendLine("    public static HS2DGameManager sharedInstance = null;");
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
        sb.AppendLine("        ResetUI();");
        sb.AppendLine("        if (gameStarted == true)");
        sb.AppendLine("        {");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            helpPanelObject.SetActive(false);");
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
        sb.AppendLine("            // Hide press any key");
        sb.AppendLine("            pressAnyKeyText.gameObject.SetActive(false);");
        sb.AppendLine("            // Show initial context help, after a short wait for ContextHelp to sync up");
        sb.AppendLine("            StartCoroutine(WaitToShowHelp(0.5f, HS2DContextHelp.Guide.Select));");
        sb.AppendLine("            gameStarted = true;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else if result panel is being displayed");
        sb.AppendLine("        else if (resultPanelObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Close it");
        sb.AppendLine("            resultPanelObject.SetActive(false);");
        sb.AppendLine("            // Restart Game");
        sb.AppendLine("            SceneManager.LoadScene(SceneManager.GetActiveScene().name);");
        sb.AppendLine("            gameStarted = true;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void ActivateRecruitButton(bool active)");
        sb.AppendLine("    {");
        sb.AppendLine("        recruitButton.gameObject.SetActive(active);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameWon()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"Victory!\";");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        contextHelp.gameObject.SetActive(false);");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameOutOfTurns()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"No more turns!\";");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        contextHelp.gameObject.SetActive(false);");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void GameOver()");
        sb.AppendLine("    {");
        sb.AppendLine("        resultPanelObject.SetActive(true);");
        sb.AppendLine("        resultText.text = \"You lost!\";");
        sb.AppendLine("        gameStarted = false;");
        sb.AppendLine("        contextHelp.gameObject.SetActive(false);");
        sb.AppendLine("        StartCoroutine(WaitToEnablePressAnyKeyText(1.75f));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool IsGameActive()");
        sb.AppendLine("    {");
        sb.AppendLine("        return gameStarted && !busy && currentTurn < maxTurns;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyGoldChanged(int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        UpdateGold(value);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyNodeSelection(HS2DNode node)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (node == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (hexInfo.gameObject.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                hexInfo.gameObject.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        unitInfo.gameObject.SetActive(false);");
        sb.AppendLine("        hexInfo.gameObject.SetActive(true);");
        sb.AppendLine("        hexInfo.Show(node);");
        sb.AppendLine("        // Show context help");
        sb.AppendLine("        if (node.localBuilding && node.localBuilding.CompareTag(board.playerFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            contextHelp.Show(HS2DContextHelp.Guide.Recruit);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyUnitAction()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Show context help");
        sb.AppendLine("        contextHelp.Show(HS2DContextHelp.Guide.EndTurn);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void NotifyUnitSelection(HS2DUnit unit)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (unit == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (unitInfo.gameObject.activeInHierarchy)");
        sb.AppendLine("            {");
        sb.AppendLine("                unitInfo.gameObject.SetActive(false);");
        sb.AppendLine("            }");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Display unit info");
        sb.AppendLine("        unitInfo.gameObject.SetActive(true);");
        sb.AppendLine("        hexInfo.gameObject.SetActive(false);");
        sb.AppendLine("        unitInfo.Show(unit);");
        sb.AppendLine("        // Show context help");
        sb.AppendLine("        if (unit.CompareTag(board.playerFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            contextHelp.Show(HS2DContextHelp.Guide.Action);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    private void ResetUI()");
        sb.AppendLine("    {");
        sb.AppendLine("        contextHelp.Restart();");
        sb.AppendLine("        UpdateTurnsRemaining();");
        sb.AppendLine("        UpdateGold(0);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void SetupObjects()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Set up objects");
        sb.AppendLine("        if (hexInfo.gameObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            hexInfo.gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("        if (unitInfo.gameObject.activeInHierarchy)");
        sb.AppendLine("        {");
        sb.AppendLine("            unitInfo.gameObject.SetActive(false);");
        sb.AppendLine("        }");
        sb.AppendLine("        endTurnButton.onClick.AddListener(TaskOnEndTurnButtonClicked);");
        sb.AppendLine("        recruitButton.onClick.AddListener(TaskOnRecruitButtonClicked);");
        sb.AppendLine("        recruitButton.gameObject.SetActive(false);");
        sb.AppendLine("        resultPanelObject.SetActive(false);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnEndTurnButtonClicked()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        currentTurn++;");
        sb.AppendLine("        if (currentTurn >= maxTurns)");
        sb.AppendLine("        {");
        sb.AppendLine("            GameOutOfTurns();");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        board.EndTurn();");
        sb.AppendLine("        // Show context help");
        sb.AppendLine("        contextHelp.Show(HS2DContextHelp.Guide.GoodLuck);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void TaskOnRecruitButtonClicked()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        board.RecruitNewArmy();");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void UpdateGold(int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        goldText.text = \"Gold: \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void UpdateTurnsRemaining()");
        sb.AppendLine("    {");
        sb.AppendLine("        int turnsLeft = maxTurns - currentTurn;");
        sb.AppendLine("        turnsText.text = \"Turns: \" + turnsLeft;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToEnablePressAnyKeyText(float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        pressAnyKeyText.gameObject.SetActive(true);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToShowHelp(float wait, HS2DContextHelp.Guide guide)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        contextHelp.Show(guide);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DGameManager", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DHexInfoScriptToFile()
    {
        StringBuilder sb = new StringBuilder(3653);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DHexInfo : HS2DInfo");
        sb.AppendLine("{");
        sb.AppendLine("    public Text typeText;");
        sb.AppendLine("    public Text positionText;");
        sb.AppendLine("    public Text taxText;");
        sb.AppendLine("    public Text defenseText;");
        sb.AppendLine("    public Text moveText;");
        sb.AppendLine("    public Text factionText;");
        sb.AppendLine("");
        sb.AppendLine("    private string GetTextForDefense(HS2DNode hexNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate the hex's total defense value");
        sb.AppendLine("        string value = \"Defense: \" + hexNode.CalculateDefenseTotal();");
        sb.AppendLine("        if (hexNode.localBuilding != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            value += \" (\" + hexNode.localBuilding.durability + \")\";");
        sb.AppendLine("        }");
        sb.AppendLine("        return value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private string GetTextForTerrain(HS2DNode hexNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Convenient method to get the hex's terrain / building info");
        sb.AppendLine("        string value;");
        sb.AppendLine("        if (hexNode.localBuilding != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            value = \"Castle\";");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            switch (hexNode.terrain)");
        sb.AppendLine("            {");
        sb.AppendLine("                case HS2DNode.Terrain.Plains:");
        sb.AppendLine("                    value = \"Plains\";");
        sb.AppendLine("                    break;");
        sb.AppendLine("                case HS2DNode.Terrain.Forest:");
        sb.AppendLine("                    value = \"Forest\";");
        sb.AppendLine("                    break;");
        sb.AppendLine("                case HS2DNode.Terrain.Hill:");
        sb.AppendLine("                    value = \"Hill\";");
        sb.AppendLine("                    break;");
        sb.AppendLine("                case HS2DNode.Terrain.Water:");
        sb.AppendLine("                    value = \"Water\";");
        sb.AppendLine("                    break;");
        sb.AppendLine("                default:");
        sb.AppendLine("                    value = \"Sea\";");
        sb.AppendLine("                    break;");
        sb.AppendLine("            }");
        sb.AppendLine("        } ");
        sb.AppendLine("");
        sb.AppendLine("        return \"Terrain: \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Show(HS2DNode hexNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Display this hex's information");
        sb.AppendLine("        typeText.text = GetTextForTerrain(hexNode);");
        sb.AppendLine("        positionText.text = GetTextForVector3Int(\"Pos\", hexNode.position);");
        sb.AppendLine("        taxText.text = GetTextForInteger(\"Tax\", hexNode.CalculateTaxTotal());");
        sb.AppendLine("        defenseText.text = GetTextForDefense(hexNode);");
        sb.AppendLine("        moveText.text = GetTextForInteger(\"Move Cost\", hexNode.movementCost);");
        sb.AppendLine("        factionText.text = GetTextForFaction(hexNode.factionTag);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DHexInfo", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DInfoScriptToFile()
    {
        StringBuilder sb = new StringBuilder(1813);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DInfo : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    protected string GetTextForFaction(string faction)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Solve the faction's name");
        sb.AppendLine("        string value;");
        sb.AppendLine("        if (faction == \"HS2DRed\")");
        sb.AppendLine("        {");
        sb.AppendLine("            value = \"Red\";");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            value = \"Blue\";");
        sb.AppendLine("        }");
        sb.AppendLine("        return \"Faction: \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected string GetTextForInteger(string label, int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        return label + \": \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected string GetTextForString(string label, string value)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Return type after capitalizing the first letter");
        sb.AppendLine("        return label + \": \" + char.ToUpper(value[0]) + value.Substring(1);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    protected string GetTextForVector3Int(string label, Vector3Int value)");
        sb.AppendLine("    {");
        sb.AppendLine("        return label + \": \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DInfo", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DInputControllerScriptToFile()
    {
        StringBuilder sb = new StringBuilder(4316);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.InputSystem;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DInputController : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    private HS2DBoard board;");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        board = GetComponent<HS2DBoard>();");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnAct()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If no unit selected, bail");
        sb.AppendLine("        if (board.selectedUnit == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If selected unit is not under player's control, bail");
        sb.AppendLine("        if (!board.selectedUnit.CompareTag(board.playerFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Act according to context");
        sb.AppendLine("        Vector3Int cellPos = board.ScreenToCellPosition(Mouse.current.position.ReadValue());");
        sb.AppendLine("        HS2DNode targetNode = gameManager.board.GetNode(cellPos);");
        sb.AppendLine("        if (targetNode == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Determine the interaction target");
        sb.AppendLine("        HS2DDeployable defender = targetNode.occupyingUnit;");
        sb.AppendLine("        if (defender == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            defender = targetNode.localBuilding;");
        sb.AppendLine("        }");
        sb.AppendLine("        // If there is an target to attack");
        sb.AppendLine("        if (defender != null && defender.CompareTag(board.enemyFaction.tag))");
        sb.AppendLine("        {");
        sb.AppendLine("            // If the defender is adjacent");
        sb.AppendLine("            if (board.nodesInAttackRange.Contains(targetNode))");
        sb.AppendLine("            {");
        sb.AppendLine("                // Attack directly");
        sb.AppendLine("                gameManager.NotifyUnitAction();");
        sb.AppendLine("                board.StartCombat(board.selectedUnit, defender, targetNode);");
        sb.AppendLine("            }");
        sb.AppendLine("            // Hostile, and in charge range, charge!");
        sb.AppendLine("            else if (board.nodesInChargeRange.Contains(targetNode))");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.NotifyUnitAction();");
        sb.AppendLine("                board.StartCharge(board.selectedUnit, defender, targetNode);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        // Else - empty node");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // Move unit to that hex");
        sb.AppendLine("            if (board.nodesInMoveRange.Contains(targetNode))");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.NotifyUnitAction();");
        sb.AppendLine("                board.MoveUnit(board.selectedUnit, cellPos);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void OnSelect()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!gameManager.IsGameActive())");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        board.SelectAtPosition(Mouse.current.position.ReadValue());");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DInputController", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DNodeScriptToFile()
    {
        StringBuilder sb = new StringBuilder(12218);

        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.Tilemaps;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DNode : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public string factionTag;");
        sb.AppendLine("    public HS2DUnit occupyingUnit;");
        sb.AppendLine("    public HS2DBuilding localBuilding;");
        sb.AppendLine("    public Vector3Int position;");
        sb.AppendLine("    public Vector3 worldPosition;");
        sb.AppendLine("    public Terrain terrain;");
        sb.AppendLine("    public int tax = 0;");
        sb.AppendLine("    public int movementCost = 1;");
        sb.AppendLine("    public int defense = 0;");
        sb.AppendLine("    public List<string> exploredByFactions = new List<string>();");
        sb.AppendLine("    public List<HS2DNode> neighbors = new List<HS2DNode>();");
        sb.AppendLine("    // Used for path finding");
        sb.AppendLine("    public HS2DNode attackedFromNode;");
        sb.AppendLine("    public HS2DNode cameFromNode;");
        sb.AppendLine("    public Passable passable;");
        sb.AppendLine("    public int gCost;");
        sb.AppendLine("    public int hCost;");
        sb.AppendLine("    public int fCost;");
        sb.AppendLine("    public int mCost;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Terrain");
        sb.AppendLine("    {");
        sb.AppendLine("        Sea = 0,");
        sb.AppendLine("        Water,");
        sb.AppendLine("        Plains,");
        sb.AppendLine("        Forest,");
        sb.AppendLine("        Hill,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public enum Passable");
        sb.AppendLine("    { ");
        sb.AppendLine("        Army = 0,");
        sb.AppendLine("        Navy,");
        sb.AppendLine("        None,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    // A hex node can only have up to 6 neighbours");
        sb.AppendLine("    public enum Direction");
        sb.AppendLine("    {");
        sb.AppendLine("        NE = 0,");
        sb.AppendLine("        E,");
        sb.AppendLine("        SE,");
        sb.AppendLine("        SW,");
        sb.AppendLine("        W,");
        sb.AppendLine("        NW,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    // Fog of war status");
        sb.AppendLine("    public enum Status");
        sb.AppendLine("    {");
        sb.AppendLine("        Covered = 0,");
        sb.AppendLine("        Shadowed,");
        sb.AppendLine("        Clear,");
        sb.AppendLine("        Interactable,");
        sb.AppendLine("        Attackable,");
        sb.AppendLine("        Selected,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Initialize(Tile tile, Terrain terrain, Vector3Int cellPosition, Vector3 worldPosition)");
        sb.AppendLine("    {");
        sb.AppendLine("        this.gameObject.name = cellPosition.ToString();");
        sb.AppendLine("        this.position = cellPosition;");
        sb.AppendLine("        this.worldPosition = worldPosition;");
        sb.AppendLine("        this.transform.position = worldPosition;");
        sb.AppendLine("        this.terrain = terrain;");
        sb.AppendLine("        ConfigureNodeStats();");
        sb.AppendLine("        SetOverlayStatus(Status.Covered);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AddExploreFaction(string factionTag)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!exploredByFactions.Contains(factionTag))");
        sb.AppendLine("        {");
        sb.AppendLine("            exploredByFactions.Add(factionTag);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void AddNeighbour(HS2DNode neighbour)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (neighbour == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        neighbors.Add(neighbour);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public int CalculateDefenseTotal()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (localBuilding != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return defense + localBuilding.defense;");
        sb.AppendLine("        }");
        sb.AppendLine("        return defense;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public int CalculateTaxTotal()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (localBuilding != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return tax + localBuilding.tax;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Node can only be taxed if there is a building");
        sb.AppendLine("        return 0;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void CalculateFCost()");
        sb.AppendLine("    {");
        sb.AppendLine("        fCost = gCost + hCost;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void ConfigureNodeStats()");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (terrain)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Plains are open, indefensible, and rich ");
        sb.AppendLine("            case Terrain.Plains:");
        sb.AppendLine("                tax = 3;");
        sb.AppendLine("                movementCost = HS2DBoard.Open;");
        sb.AppendLine("                passable = Passable.Army;");
        sb.AppendLine("                defense = 0;");
        sb.AppendLine("                break;");
        sb.AppendLine("            // Forests are difficult to cross, defensible, and not so rich");
        sb.AppendLine("            case Terrain.Forest:");
        sb.AppendLine("                tax = 2;");
        sb.AppendLine("                movementCost = HS2DBoard.Difficult;");
        sb.AppendLine("                passable = Passable.Army;");
        sb.AppendLine("                defense = 1;");
        sb.AppendLine("                break;");
        sb.AppendLine("            // Hills are difficult to cross, defensible, and not so rich");
        sb.AppendLine("            case Terrain.Hill:");
        sb.AppendLine("                tax = 2;");
        sb.AppendLine("                movementCost = HS2DBoard.Difficult;");
        sb.AppendLine("                passable = Passable.Army;");
        sb.AppendLine("                defense = 2;");
        sb.AppendLine("                break;");
        sb.AppendLine("            // Water are not accessible by land units");
        sb.AppendLine("            case Terrain.Water:");
        sb.AppendLine("                tax = 0;");
        sb.AppendLine("                movementCost = 1;");
        sb.AppendLine("                passable = Passable.Navy;");
        sb.AppendLine("                defense = 0;");
        sb.AppendLine("                break;");
        sb.AppendLine("            default:");
        sb.AppendLine("                tax = 0;");
        sb.AppendLine("                movementCost = HS2DBoard.Impassable;");
        sb.AppendLine("                passable = Passable.None;");
        sb.AppendLine("                defense = 0;");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public bool ExploredByFaction(string factionTag)");
        sb.AppendLine("    {");
        sb.AppendLine("        return exploredByFactions.Contains(factionTag);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private HS2DNode FindNeighbor(Direction direction)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Find a neighbor in the given direction");
        sb.AppendLine("        bool isEvenRow = (position.y % 2 == 0);");
        sb.AppendLine("        int posY = position.y;");
        sb.AppendLine("        int posX = position.x;");
        sb.AppendLine("        Vector3Int nPos = new Vector3Int();");
        sb.AppendLine("        switch (direction)");
        sb.AppendLine("        {");
        sb.AppendLine("            case Direction.NE:");
        sb.AppendLine("                nPos.x = isEvenRow ? posX : posX + 1;");
        sb.AppendLine("                nPos.y = posY + 1;");
        sb.AppendLine("                break;");
        sb.AppendLine("            case Direction.E:");
        sb.AppendLine("                nPos.x = posX + 1;");
        sb.AppendLine("                nPos.y = posY;");
        sb.AppendLine("                break;");
        sb.AppendLine("            case Direction.SE:");
        sb.AppendLine("                nPos.x = isEvenRow ? posX : posX + 1;");
        sb.AppendLine("                nPos.y = posY - 1;");
        sb.AppendLine("                break;");
        sb.AppendLine("            case Direction.SW:");
        sb.AppendLine("                nPos.x = isEvenRow ? posX - 1 : posX;");
        sb.AppendLine("                nPos.y = posY - 1;");
        sb.AppendLine("                break;");
        sb.AppendLine("            case Direction.W:");
        sb.AppendLine("                nPos.x = posX - 1;");
        sb.AppendLine("                nPos.y = posY;");
        sb.AppendLine("                break;");
        sb.AppendLine("            case Direction.NW:");
        sb.AppendLine("                nPos.x = isEvenRow ? posX - 1 : posX;");
        sb.AppendLine("                nPos.y = posY + 1;");
        sb.AppendLine("                break;");
        sb.AppendLine("            default:");
        sb.AppendLine("                return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        return HS2DGameManager.sharedInstance.board.GetNode(nPos);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void FindNeighbors()");
        sb.AppendLine("    {");
        sb.AppendLine("        neighbors.Clear();");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.NE));");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.E));");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.SE));");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.SW));");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.W));");
        sb.AppendLine("        AddNeighbour(FindNeighbor(Direction.NW));");
        sb.AppendLine("    }");
        sb.AppendLine("    ");
        sb.AppendLine("    public void SetOverlayStatus(Status status)");
        sb.AppendLine("    {");
        sb.AppendLine("        const float k = 1.0f / 255.0f;");
        sb.AppendLine("        Color clear = new Color(1.0f, 1.0f, 1.0f, 0.0f);");
        sb.AppendLine("        Color interactable = new Color(0.0f, 1.0f, 0.0f, 0.5f);");
        sb.AppendLine("        Color shadowed = new Color(0.0f * k, 51.0f * k, 102.0f * k, 0.5f);");
        sb.AppendLine("        Color covered = new Color(0.0f * k, 51.0f * k, 102.0f * k, 1.0f);");
        sb.AppendLine("        Color attackable = new Color(255.0f * k, 0.0f * k, 0.0f * k, 0.5f);");
        sb.AppendLine("        Color selected = new Color(1.0f, 1.0f, 1.0f, 0.5f);");
        sb.AppendLine("");
        sb.AppendLine("        SpriteRenderer sr = GetComponent<SpriteRenderer>();");
        sb.AppendLine("        switch (status)");
        sb.AppendLine("        {");
        sb.AppendLine("            case Status.Clear:");
        sb.AppendLine("                sr.color = clear;");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Status.Interactable:");
        sb.AppendLine("                sr.color = interactable;");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Status.Shadowed:");
        sb.AppendLine("                sr.color = shadowed;");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Status.Attackable:");
        sb.AppendLine("                sr.color = attackable;");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            case Status.Selected:");
        sb.AppendLine("                sr.color = selected;");
        sb.AppendLine("                break;");
        sb.AppendLine("");
        sb.AppendLine("            default:");
        sb.AppendLine("                sr.color = covered;");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DNode", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DPathFinderScriptToFile()
    {
        StringBuilder sb = new StringBuilder(13983);

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DPathFinder");
        sb.AppendLine("{");
        sb.AppendLine("    private Dictionary<Vector3Int, HS2DNode> hexNodes;");
        sb.AppendLine("    List<HS2DNode> openList;");
        sb.AppendLine("    List<HS2DNode> closedList;");
        sb.AppendLine("");
        sb.AppendLine("    private List<HS2DNode> CalculatePath(HS2DNode endNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Last node found, build a path");
        sb.AppendLine("        List<HS2DNode> path = new List<HS2DNode>();");
        sb.AppendLine("        path.Add(endNode);");
        sb.AppendLine("        // Connect the nodes");
        sb.AppendLine("        HS2DNode currentNode = endNode;");
        sb.AppendLine("        while (currentNode.cameFromNode != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            path.Add(currentNode.cameFromNode);");
        sb.AppendLine("            currentNode = currentNode.cameFromNode;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Reverse order so the list starts from the start node");
        sb.AppendLine("        path.Reverse();");
        sb.AppendLine("        return path;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public int CalculateTaxiCabDistanceCost(HS2DNode node1, HS2DNode node2)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate distance from simple horizontal and vertical distances");
        sb.AppendLine("        int dx = node2.position.x - node1.position.x;");
        sb.AppendLine("        int dy = node2.position.y - node1.position.y;");
        sb.AppendLine("");
        sb.AppendLine("        int d;");
        sb.AppendLine("        if (Math.Sign(dx) == Math.Sign(dy))");
        sb.AppendLine("        {");
        sb.AppendLine("            d = Math.Abs(dx + dy);");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            d = Math.Max(Math.Abs(dx), Math.Abs(dy));");
        sb.AppendLine("        }");
        sb.AppendLine("        return d;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private bool CheckPassable(HS2DNode pathNode, HS2DUnit.Type unitType)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check node passability for a particular unit type");
        sb.AppendLine("        if (pathNode.passable == HS2DNode.Passable.Army)");
        sb.AppendLine("        {");
        sb.AppendLine("            // This node is passable for armies");
        sb.AppendLine("            return unitType == HS2DUnit.Type.Army;");
        sb.AppendLine("        }");
        sb.AppendLine("        else if (pathNode.passable == HS2DNode.Passable.Navy)");
        sb.AppendLine("        {");
        sb.AppendLine("            // This node is passable for navies");
        sb.AppendLine("            return unitType == HS2DUnit.Type.Navy;");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine("            // This node is not passable for any unit");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public List<HS2DNode> FindNodesInRange(Vector3Int start, int range, HS2DUnit.Type unitType)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Find all nodes within the specified range for this unit type");
        sb.AppendLine("");
        sb.AppendLine("        HS2DNode startNode = GetNode(start);");
        sb.AppendLine("        // Init the lists, put the start node in the open list");
        sb.AppendLine("        openList = new List<HS2DNode>() { startNode };");
        sb.AppendLine("        closedList = new List<HS2DNode>();");
        sb.AppendLine("");
        sb.AppendLine("        // Reset move cost values");
        sb.AppendLine("        foreach (KeyValuePair<Vector3Int, HS2DNode> element in hexNodes)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DNode node = element.Value;");
        sb.AppendLine("            node.mCost = 0;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // While the open list is not empty");
        sb.AppendLine("        while (openList.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DNode currentNode = openList[0];");
        sb.AppendLine("            // Move the current node to the closed list");
        sb.AppendLine("            openList.Remove(currentNode);");
        sb.AppendLine("            closedList.Add(currentNode);");
        sb.AppendLine("            // Go through each neighbor of the current node");
        sb.AppendLine("            foreach (HS2DNode neighborNode in currentNode.neighbors)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Check that it is not already examined");
        sb.AppendLine("                if (closedList.Contains(neighborNode))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Check that it is not already in the open list");
        sb.AppendLine("                if (openList.Contains(neighborNode))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Check that it is passable for this type of unit");
        sb.AppendLine("                if (!CheckPassable(neighborNode, unitType))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Check that it is not occupied");
        sb.AppendLine("                if (neighborNode.occupyingUnit != null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Calculate the mCost (total movement cost coming from the start node)");
        sb.AppendLine("                int mCost = neighborNode.movementCost + currentNode.mCost;");
        sb.AppendLine("                // Check that it is in range");
        sb.AppendLine("                if (mCost > range)");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // All checks succeed");
        sb.AppendLine("                // Add this node to the open list");
        sb.AppendLine("                neighborNode.mCost = mCost;");
        sb.AppendLine("                openList.Add(neighborNode);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Return eligible nodes minus the start node");
        sb.AppendLine("        closedList.Remove(startNode);");
        sb.AppendLine("        return closedList;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public List<HS2DNode> FindPath(Vector3Int start, Vector3Int end, HS2DUnit.Type unitType, bool ignoreDestination = false)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Find a path from start to end coordinates");
        sb.AppendLine("");
        sb.AppendLine("        // Get nodes on the start and end coordinates");
        sb.AppendLine("        HS2DNode startNode = GetNode(start);");
        sb.AppendLine("        HS2DNode endNode = GetNode(end);");
        sb.AppendLine("        // Initialize the lists");
        sb.AppendLine("        openList = new List<HS2DNode>() { startNode };");
        sb.AppendLine("        closedList = new List<HS2DNode>();");
        sb.AppendLine("");
        sb.AppendLine("        // Reset values");
        sb.AppendLine("        foreach (KeyValuePair<Vector3Int, HS2DNode> element in hexNodes)");
        sb.AppendLine("        {");
        sb.AppendLine("            HS2DNode node = element.Value;");
        sb.AppendLine("            node.gCost = int.MaxValue;");
        sb.AppendLine("            node.CalculateFCost();");
        sb.AppendLine("            node.cameFromNode = null;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Calculate costs for the start node");
        sb.AppendLine("        startNode.gCost = 0;");
        sb.AppendLine("        startNode.hCost = CalculateTaxiCabDistanceCost(startNode, endNode);");
        sb.AppendLine("        startNode.CalculateFCost();");
        sb.AppendLine("");
        sb.AppendLine("        // While there is a node in the open list");
        sb.AppendLine("        while (openList.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Get the current node, with the lowest cost, from the open list");
        sb.AppendLine("            HS2DNode currentNode = GetLowestFCostNode(openList);");
        sb.AppendLine("            // If this is the end node");
        sb.AppendLine("            if (currentNode == endNode)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Calculate path and return");
        sb.AppendLine("                return CalculatePath(endNode);");
        sb.AppendLine("            }");
        sb.AppendLine("");
        sb.AppendLine("            // Move the current node to the closed list");
        sb.AppendLine("            openList.Remove(currentNode);");
        sb.AppendLine("            closedList.Add(currentNode);");
        sb.AppendLine("");
        sb.AppendLine("            // Go through each neighbor of the current node");
        sb.AppendLine("            foreach (HS2DNode neighborNode in currentNode.neighbors)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Check that the neighbor exists");
        sb.AppendLine("                if (neighborNode == null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    break;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Check that it is not already in the closed list");
        sb.AppendLine("                if (closedList.Contains(neighborNode))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // Check that it is not occupied");
        sb.AppendLine("                if (neighborNode.occupyingUnit != null && !(ignoreDestination && neighborNode == endNode))");
        sb.AppendLine("                {");
        sb.AppendLine("                    continue;");
        sb.AppendLine("                }");
        sb.AppendLine("                // If the tentative path leading to this neighbor is the cheapest so far");
        sb.AppendLine("                int tentativeGCost = currentNode.gCost + neighborNode.movementCost;");
        sb.AppendLine("                if (tentativeGCost < neighborNode.gCost)");
        sb.AppendLine("                {");
        sb.AppendLine("                    // If the neighbor node is passable for this unit");
        sb.AppendLine("                    if (!CheckPassable(neighborNode, unitType))");
        sb.AppendLine("                    {");
        sb.AppendLine("                        // Add it to the path (closed list)");
        sb.AppendLine("                        closedList.Add(neighborNode);");
        sb.AppendLine("                        continue;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    // Otherwise, we still need to consider the neighboring nodes of this neighbor node");
        sb.AppendLine("                    // Update the various movement costs");
        sb.AppendLine("                    neighborNode.cameFromNode = currentNode;");
        sb.AppendLine("                    neighborNode.gCost = tentativeGCost;");
        sb.AppendLine("                    neighborNode.hCost = CalculateTaxiCabDistanceCost(neighborNode, endNode);");
        sb.AppendLine("                    neighborNode.CalculateFCost();");
        sb.AppendLine("                    // If it is not in the open list");
        sb.AppendLine("                    if (!openList.Contains(neighborNode))");
        sb.AppendLine("                    {");
        sb.AppendLine("                        // Add it now");
        sb.AppendLine("                        openList.Add(neighborNode);");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Can't find a path");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private HS2DNode GetLowestFCostNode(List<HS2DNode> pathNodeList)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Calculate the f cost (current best guess of movement cost) of the list of nodes");
        sb.AppendLine("        HS2DNode lowestFCostNode = pathNodeList[0];");
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
        sb.AppendLine("    public void Initialize(Dictionary<Vector3Int, HS2DNode> hexNodes)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Initialize the path finder");
        sb.AppendLine("        this.hexNodes = hexNodes;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public HS2DNode GetNode(Vector3Int position)");
        sb.AppendLine("    {");
        sb.AppendLine("        HS2DNode rv;");
        sb.AppendLine("        if (hexNodes.TryGetValue(position, out rv))");
        sb.AppendLine("        {");
        sb.AppendLine("            return rv;");
        sb.AppendLine("        }");
        sb.AppendLine("        Debug.LogError(\"Cannot locate hex node at \" + position + \"!\");");
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DPathFinder", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DUnitScriptToFile()
    {
        StringBuilder sb = new StringBuilder(9830);

        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DUnit : HS2DDeployable");
        sb.AppendLine("{");
        sb.AppendLine("    public Type type;");
        sb.AppendLine("    public int numAttacks = 1;");
        sb.AppendLine("    public int upkeep = 1;");
        sb.AppendLine("    public int move = 2;");
        sb.AppendLine("    public int strength = 1000;");
        sb.AppendLine("    public int experience = 1;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public int attacksLeft;");
        sb.AppendLine("    [HideInInspector]");
        sb.AppendLine("    public int moveLeft;");
        sb.AppendLine("    private HS2DGameManager gameManager;");
        sb.AppendLine("");
        sb.AppendLine("    public enum Type");
        sb.AppendLine("    {");
        sb.AppendLine("        Army = 0,");
        sb.AppendLine("        Navy,");
        sb.AppendLine("        Max");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private void Start()");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager = HS2DGameManager.sharedInstance;");
        sb.AppendLine("        moveLeft = move;");
        sb.AppendLine("        attacksLeft = numAttacks;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator AnimateAttack(float duration, Vector3 destination)");
        sb.AppendLine("    {");
        sb.AppendLine("        gameManager.board.NotifyUnitBusy(duration);");
        sb.AppendLine("");
        sb.AppendLine("        Vector3 lastPosition = transform.position;");
        sb.AppendLine("        // Prepare dynamic formula");
        sb.AppendLine("        float t = duration * 0.5f;");
        sb.AppendLine("        Vector3 d = (destination - transform.position) * 0.75f;");
        sb.AppendLine("        Vector3 u = d * 0.125f;");
        sb.AppendLine("        Vector3 a = 2.0f * (d - u * t) / (t * t);");
        sb.AppendLine("        // Animate forward");
        sb.AppendLine("        float tElapsed = 0.0f;");
        sb.AppendLine("        while (tElapsed < t)");
        sb.AppendLine("        {");
        sb.AppendLine("            tElapsed += Time.deltaTime;");
        sb.AppendLine("            tElapsed = Mathf.Min(t, tElapsed);");
        sb.AppendLine("            transform.position = lastPosition + u * tElapsed + 0.5f * a * tElapsed * tElapsed;");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Animate backward");
        sb.AppendLine("        while (tElapsed >= 0.0f)");
        sb.AppendLine("        {");
        sb.AppendLine("            transform.position = lastPosition + u * tElapsed + 0.5f * a * tElapsed * tElapsed;");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("            tElapsed -= Time.deltaTime;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Reset to old position");
        sb.AppendLine("        transform.position = lastPosition;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Attack(HS2DDeployable defender, HS2DNode terrainNode, float animTime)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (defender == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        if (attacksLeft <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        attacksLeft--;");
        sb.AppendLine("        StartCoroutine(AnimateAttack(animTime, terrainNode.worldPosition));");
        sb.AppendLine("        StartCoroutine(WaitToResolveCombat(animTime, defender, terrainNode));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void EndTurn()");
        sb.AppendLine("    {");
        sb.AppendLine("        moveLeft = move;");
        sb.AppendLine("        attacksLeft = numAttacks;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public float Move(List<HS2DNode> path, float animTime = 1.25f)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Tell the game it is busy");
        sb.AppendLine("        gameManager.board.NotifyUnitBusy(animTime);");
        sb.AppendLine("        // Move unit one hex at a time");
        sb.AppendLine("        float duration = animTime / path.Count;");
        sb.AppendLine("        for (int i = 1; i < path.Count; i++)");
        sb.AppendLine("        {");
        sb.AppendLine("            Vector3 destination = path[i].worldPosition;");
        sb.AppendLine("            StartCoroutine(MoveToDestination(destination, path[i].movementCost, duration, duration * (i - 1)));");
        sb.AppendLine("        }");
        sb.AppendLine("        StartCoroutine(WaitToFinishMove(animTime, path[0], path[path.Count-1]));");
        sb.AppendLine("        return animTime;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void MoveAndAttack(List<HS2DNode> path, HS2DDeployable defender, HS2DNode terrainNode, float attackAnimTime)");
        sb.AppendLine("    {");
        sb.AppendLine("        float moveAnimTime = Move(path);");
        sb.AppendLine("        StartCoroutine(WaitToAttack(moveAnimTime, defender, terrainNode, attackAnimTime));");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator MoveToDestination(Vector3 destination, int moveCost, float duration, float wait)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        // Animate the unit's movement to the destination");
        sb.AppendLine("        Vector3 startPosition = transform.position;");
        sb.AppendLine("        Vector3 translation = destination - startPosition;");
        sb.AppendLine("        float timeElapsed = 0.0f;");
        sb.AppendLine("        while (timeElapsed <= duration)");
        sb.AppendLine("        {");
        sb.AppendLine("            timeElapsed += Time.deltaTime;");
        sb.AppendLine("            float t = timeElapsed / duration;");
        sb.AppendLine("            transform.position = startPosition + translation * t;");
        sb.AppendLine("            yield return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        transform.position = destination;");
        sb.AppendLine("        moveLeft -= moveCost;");
        sb.AppendLine("        gameManager.board.SelectUnit(this);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public override int RollDamage()");
        sb.AppendLine("    {");
        sb.AppendLine("        return Random.Range(1, 6) * 100;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    override public void TakeDamage(int damage)");
        sb.AppendLine("    {");
        sb.AppendLine("        HS2DBoard board = gameManager.board;");
        sb.AppendLine("        strength -= damage;");
        sb.AppendLine("        // If strength reduced to zero");
        sb.AppendLine("        if (strength <= 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Unit is destroyed, clean up");
        sb.AppendLine("            strength = 0;");
        sb.AppendLine("            board.NotifyGamePieceDestruction(this);");
        sb.AppendLine("            if (board.selectedUnit == this)");
        sb.AppendLine("            {");
        sb.AppendLine("                gameManager.NotifyUnitSelection(null);");
        sb.AppendLine("                board.selectedUnit = null;");
        sb.AppendLine("                board.ClearOverlay();");
        sb.AppendLine("            }");
        sb.AppendLine("            return;");
        sb.AppendLine("        }");
        sb.AppendLine("        // Update HUD");
        sb.AppendLine("        if (board.selectedUnit == this)");
        sb.AppendLine("        {");
        sb.AppendLine("            gameManager.NotifyUnitSelection(this);");
        sb.AppendLine("            board.UpdateSelectedUnitInfo();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToAttack(float wait, HS2DDeployable defender, HS2DNode terrainNode, float animTime)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("        Attack(defender, terrainNode, animTime);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToFinishMove(float wait, HS2DNode start, HS2DNode end)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        start.occupyingUnit = null;");
        sb.AppendLine("        end.occupyingUnit = this;");
        sb.AppendLine("        gameManager.board.SelectUnit(this);");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private IEnumerator WaitToResolveCombat(float wait, HS2DDeployable defender, HS2DNode terrainNode)");
        sb.AppendLine("    {");
        sb.AppendLine("        yield return new WaitForSeconds(wait);");
        sb.AppendLine("");
        sb.AppendLine("        if (defender != null)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Resolve combat if defender is still valid");
        sb.AppendLine("            const int mul = 100;");
        sb.AppendLine("            int attackerDamage = RollDamage();");
        sb.AppendLine("            int defenderDamage = defender.RollDamage() + terrainNode.CalculateDefenseTotal() * mul;");
        sb.AppendLine("");
        sb.AppendLine("            TakeDamage(defenderDamage);");
        sb.AppendLine("            defender.TakeDamage(attackerDamage);");
        sb.AppendLine("");
        sb.AppendLine("            // Update board highlights");
        sb.AppendLine("            gameManager.board.UpdateSelectedUnitInfo();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DUnit", scriptsPath, sb.ToString());
    }

    private static void WriteHS2DUnitInfoScriptToFile()
    {
        StringBuilder sb = new StringBuilder(2627);

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEngine.UI;");
        sb.AppendLine("");
        sb.AppendLine("public class HS2DUnitInfo : HS2DInfo");
        sb.AppendLine("{");
        sb.AppendLine("    public Text typeText;");
        sb.AppendLine("    public Text positionText;");
        sb.AppendLine("    public Text strengthText;");
        sb.AppendLine("    public Text upkeepText;");
        sb.AppendLine("    public Text moveText;");
        sb.AppendLine("    public Text factionText;");
        sb.AppendLine("");
        sb.AppendLine("    private string GetTextForMove(int moveLeft, int move, int attacksLeft)");
        sb.AppendLine("    {");
        sb.AppendLine("        return \"Move: \" + moveLeft + \" / \" + move + \", Att: \" + attacksLeft;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    private string GetTextForType(HS2DUnit.Type type)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Solve the text of the unit type");
        sb.AppendLine("        string value;");
        sb.AppendLine("        switch (type)");
        sb.AppendLine("        {");
        sb.AppendLine("            case HS2DUnit.Type.Army:");
        sb.AppendLine("                value = \"Army\";");
        sb.AppendLine("                break;");
        sb.AppendLine("            default:");
        sb.AppendLine("                value = \"Unknown\";");
        sb.AppendLine("                break;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        return \"Type: \" + value;");
        sb.AppendLine("    }");
        sb.AppendLine("");
        sb.AppendLine("    public void Show(HS2DUnit unit)");
        sb.AppendLine("    {");
        sb.AppendLine("        Vector3 pos = unit.transform.position;");
        sb.AppendLine("        Vector3Int position = new Vector3Int((int)pos.x, (int)pos.y, (int)pos.z);");
        sb.AppendLine("");
        sb.AppendLine("        typeText.text = GetTextForType(unit.type);");
        sb.AppendLine("        positionText.text = GetTextForVector3Int(\"Pos\", position);");
        sb.AppendLine("        strengthText.text = GetTextForInteger(\"Str.\", unit.strength);");
        sb.AppendLine("        upkeepText.text = GetTextForInteger(\"Upkeep\", unit.upkeep);");
        sb.AppendLine("        moveText.text = GetTextForMove(unit.moveLeft, unit.move, unit.attacksLeft);");
        sb.AppendLine("        factionText.text = GetTextForFaction(unit.gameObject.tag);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        ScriptUtilities.CreateScriptFile("HS2DUnitInfo", scriptsPath, sb.ToString());
    }
}
