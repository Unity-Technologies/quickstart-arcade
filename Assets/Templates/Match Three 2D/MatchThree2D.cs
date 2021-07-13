using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

public class MatchThree2D : Editor
{
	private const string templateName = "MatchThree2D";
	private const string templateSpacedName = "Match Three 2D";
	private const string prefKey = templateName + "Processing";
	private const string scriptPrefix = "MT2D";
	private const int textureSize = 64;
	private const int mapWidth = 6;
	private const int mapHeight = 6;
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
		GemRed = 0,
		GemOrange,
		GemYellow,
		GemGreen,
		GemBlue,
		GemIndigo,
		GemViolet,
		GemBackground,
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
		ScriptUtilities.RemoveTag(scriptPrefix + "Red");
		ScriptUtilities.RemoveTag(scriptPrefix + "Orange");
		ScriptUtilities.RemoveTag(scriptPrefix + "Yellow");
		ScriptUtilities.RemoveTag(scriptPrefix + "Green");
		ScriptUtilities.RemoveTag(scriptPrefix + "Blue");
		ScriptUtilities.RemoveTag(scriptPrefix + "Indigo");
		ScriptUtilities.RemoveTag(scriptPrefix + "Violet");
		// Sorting Layers
		ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Background");
		ScriptUtilities.RemoveSortingLayer(scriptPrefix + "Foreground");
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
		// Set up camera
		GameObject mainCameraObject = GameObject.Find("Main Camera");
		mainCameraObject.GetComponent<Camera>().orthographicSize = 1.0f + mapWidth / 2;
		mainCameraObject.transform.position = new Vector3(mapWidth / 2, mapHeight / 2, -10.0f);
		// Create tags and layers
		GenerateTagsAndLayers();
	}

	private static void GenerateTagsAndLayers()
	{
		// Tags
		ScriptUtilities.CreateTag(scriptPrefix + "Red");
		ScriptUtilities.CreateTag(scriptPrefix + "Orange");
		ScriptUtilities.CreateTag(scriptPrefix + "Yellow");
		ScriptUtilities.CreateTag(scriptPrefix + "Green");
		ScriptUtilities.CreateTag(scriptPrefix + "Blue");
		ScriptUtilities.CreateTag(scriptPrefix + "Indigo");
		ScriptUtilities.CreateTag(scriptPrefix + "Violet");
		// Sorting Layers
		ScriptUtilities.CreateSortingLayer(scriptPrefix + "Background");
		ScriptUtilities.CreateSortingLayer(scriptPrefix + "Foreground");
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
		// Create Mouse Down action and add bindings
		var action = map.AddAction("MouseDown", interactions: "Press", type: InputActionType.Button);
		action.AddBinding(new InputBinding("<Mouse>/leftButton"));
		// Create Mouse Up action and add bindings
		action = map.AddAction("MouseUp", interactions: "Press(behavior=1)", type: InputActionType.Button);
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
		Color red = new Color(255.0f * k, 0.0f * k, 0.0f * k);
		Color orange = new Color(255.0f * k, 127.0f * k, 0.0f * k);
		Color yellow = new Color(255.0f * k, 255.0f * k, 0.0f * k);
		Color green = new Color(80.0f * k, 220.0f * k, 100.0f * k);
		Color blue = new Color(0.0f * k, 0.0f * k, 255.0f * k);
		Color indigo = new Color(75.0f * k, 0.0f * k, 130.0f * k);
		Color violet = new Color(143.0f * k, 0.0f * k, 255.0f * k);

		// Generate textures
		int w = textureSize - 4;
		int h = textureSize - 4;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_red", texturesPath, w, h, red);
		spritePaths[(int)SpritePath.GemRed] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_orange", texturesPath, w, h, orange);
		spritePaths[(int)SpritePath.GemOrange] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_yellow", texturesPath, w, h, yellow);
		spritePaths[(int)SpritePath.GemYellow] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_green", texturesPath, w, h, green);
		spritePaths[(int)SpritePath.GemGreen] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_blue", texturesPath, w, h, blue);
		spritePaths[(int)SpritePath.GemBlue] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_indigo", texturesPath, w, h, indigo);
		spritePaths[(int)SpritePath.GemIndigo] = path;
		path = ContentUtilities.CreateTexture2DDiamondAsset("texture_gem_violet", texturesPath, w, h, violet);
		spritePaths[(int)SpritePath.GemViolet] = path;

		path = ContentUtilities.CreateTexture2DFrameAsset("texture_gem_background", texturesPath, textureSize, textureSize, Color.gray);
		spritePaths[(int)SpritePath.GemBackground] = path;
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

		// Create an object pool game object
		newObject = new GameObject("ObjectsPool");
		ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

		// Create the game board object
		newObject = new GameObject("Board");
		// Add input
		newObject.AddComponent<PlayerInput>().actions = asset;
		newObject.GetComponent<PlayerInput>().defaultActionMap = "Gameplay";
		// Make into prefab
		ContentUtilities.CreatePrefab(newObject, prefabsPath, true);

		// Create the background object
		newObject = new GameObject("Background");
		SpriteRenderer spriteRenderer = newObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = ContentUtilities.LoadSpriteAtPath(spritePaths[(int)SpritePath.GemBackground]);
		spriteRenderer.sortingLayerName = scriptPrefix + "Background";
		newObject.transform.position = new Vector2(mapWidth / 2.0f, mapHeight / 2.0f);
		newObject.transform.localScale = new Vector2(mapWidth + 2.0f, mapHeight + 2.0f);

		// Create Gems
		CreateGemPrefab();

		// Create Background
		newObject = new GameObject("Background");
		SpriteRenderer renderer = newObject.AddComponent<SpriteRenderer>();
		renderer.sprite = ContentUtilities.LoadSpriteAtPath(spritePaths[(int)SpritePath.GemBackground]);
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
		GameObject scoreTextObject = ContentUtilities.CreateUITextObject("ScoreText", w - margin, h, "Score: 9999", TextAnchor.MiddleLeft, fontSize, Color.white);
		ContentUtilities.AnchorUIObject(scoreTextObject, scoreTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

		// Create moves text panel
		float offsetY = -h;
		GameObject movesTextPanel = ContentUtilities.CreateUIBackgroundObject("MovesTextPanel", w, h);
		ContentUtilities.AnchorUIObject(movesTextPanel, parent, ContentUtilities.Anchor.TopLeft, new Vector2(margin, -margin + offsetY));
		// Create moves text
		GameObject movesTextObject = ContentUtilities.CreateUITextObject("MovesText", w - margin, h, "Moves: 24", TextAnchor.MiddleLeft, fontSize, Color.white);
		ContentUtilities.AnchorUIObject(movesTextObject, movesTextPanel.transform, ContentUtilities.Anchor.Center, new Vector2(margin / 2, 0.0f));

		// Create result panel
		w = 600.0f;
		h = 240.0f;
		GameObject resultPanelObject = ContentUtilities.CreateUIBackgroundObject("ResultPanel", w, h);
		ContentUtilities.AnchorUIObject(resultPanelObject, parent, ContentUtilities.Anchor.Center, Vector2.zero);
		// Create result text
		string text = "No more moves...\nClick the Reset Button to Start a New Game";
		GameObject resultTextObject = ContentUtilities.CreateUITextObject("ResultText", w - 2 * margin, h, text, TextAnchor.MiddleCenter, fontSize, Color.white);
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
        sb.AppendLine("Swipe on a Gem and its Neighbor to Swap Their Positions");
        sb.AppendLine("Match 3 or More Gems of the Same Color to Match Them");
        sb.AppendLine("Get As High a Score As Possible In 24 Moves!");
        GameObject helpPanelTextObject = ContentUtilities.CreateUITextObject("Text", w - 2 * margin, h, sb.ToString(), TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(helpPanelTextObject, helpPanelObject.transform, ContentUtilities.Anchor.Center, Vector2.zero);

        // Create press any key text
        w = 200;
        h = 40;
        GameObject pressAnyKeyTextObject = ContentUtilities.CreateUITextObject("PressAnyKeyText", w, h, "Press Any Key", TextAnchor.MiddleCenter, fontSize, Color.white);
        ContentUtilities.AnchorUIObject(pressAnyKeyTextObject, parent, ContentUtilities.Anchor.Bottom, new Vector2(0.0f, margin));
	}

	private static void GenerateScripts()
	{
		WriteMT2DBlinkTextScriptToFile();
		WriteMT2DBoardScriptToFile();
		WriteMT2DGameManagerScriptToFile();
		WriteMT2DGemScriptToFile();
		WriteMT2DObjectsPoolScriptToFile();
	}

	private static void GenerateTileMap()
	{
		// Generate tile map, pallette, and tile assets here...
	}

	private static void EnableOnScriptsReloadedProcessing()
	{
		if (ScriptUtilities.CheckTypes(scriptPrefix, new string[] {
			"Board", "GameManager", "Gem" }))
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
		GameObject boardPrefab = ContentUtilities.LoadPrefab("Board", prefabsPath);
		GameObject gemPrefab = ContentUtilities.LoadPrefab("Gem", prefabsPath);

		// Attach scripts
		ScriptUtilities.AttachScriptToObject(scriptPrefix + "BlinkText", pressAnyKeyTextObject);
		ScriptUtilities.AttachScriptToObject(scriptPrefix + "GameManager", gameManagerPrefab);
		ScriptUtilities.AttachScriptToObject(scriptPrefix + "ObjectsPool", objectsPoolPrefab);
		ScriptUtilities.AttachScriptToObject(scriptPrefix + "Board", boardPrefab);
		ScriptUtilities.AttachScriptToObject(scriptPrefix + "Gem", gemPrefab);

		// Assign prefabs
		ScriptUtilities.AssignObjectFieldToObject(gemPrefab, objectsPoolPrefab, scriptPrefix + "ObjectsPool", "gemPrefab");

		// Assign sprites
		Sprite redSprite = ContentUtilities.LoadSprite("texture_gem_red", texturesPath);
		Sprite orangeSprite = ContentUtilities.LoadSprite("texture_gem_orange", texturesPath);
		Sprite yellowSprite = ContentUtilities.LoadSprite("texture_gem_yellow", texturesPath);
		Sprite greenSprite = ContentUtilities.LoadSprite("texture_gem_green", texturesPath);
		Sprite blueSprite = ContentUtilities.LoadSprite("texture_gem_blue", texturesPath);
		Sprite indigoSprite = ContentUtilities.LoadSprite("texture_gem_indigo", texturesPath);
		Sprite violetSprite = ContentUtilities.LoadSprite("texture_gem_violet", texturesPath);
		string className = scriptPrefix + "Gem";
		ScriptUtilities.AssignSpriteFieldToObject(redSprite, gemPrefab, className, "redSprite");
		ScriptUtilities.AssignSpriteFieldToObject(orangeSprite, gemPrefab, className, "orangeSprite");
		ScriptUtilities.AssignSpriteFieldToObject(yellowSprite, gemPrefab, className, "yellowSprite");
		ScriptUtilities.AssignSpriteFieldToObject(greenSprite, gemPrefab, className, "greenSprite");
		ScriptUtilities.AssignSpriteFieldToObject(blueSprite, gemPrefab, className, "blueSprite");
		ScriptUtilities.AssignSpriteFieldToObject(indigoSprite, gemPrefab, className, "indigoSprite");
		ScriptUtilities.AssignSpriteFieldToObject(violetSprite, gemPrefab, className, "violetSprite");

		// Create initial objects
		PrefabUtility.InstantiatePrefab(objectsPoolPrefab);
		InstantiateAndSetupBoard(boardPrefab);
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

	private static void CreateGemPrefab()
	{
		GameObject newObject = new GameObject("Gem");
		SpriteRenderer renderer = newObject.AddComponent<SpriteRenderer>();
		renderer.sprite = ContentUtilities.LoadSpriteAtPath(spritePaths[(int)SpritePath.GemYellow]);
		renderer.sortingLayerName = scriptPrefix + "Foreground";
		newObject.AddComponent<CircleCollider2D>();
		ContentUtilities.CreatePrefab(newObject, prefabsPath, true);
	}

	private static void InstantiateAndSetupBoard(GameObject prefab)
	{
		GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		string className = scriptPrefix + "Board";

		// Get objects
		GameObject objectsPoolObject = GameObject.Find("ObjectsPool");

		// Assign objects or components
		ScriptUtilities.AssignComponentFieldToObject(objectsPoolObject, scriptPrefix + "ObjectsPool", go, className, "objectsPool");
	}

	private static void InstantiateAndSetupGameManager(GameObject prefab)
    {
		GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
		string className = scriptPrefix + "GameManager";

		// Get objects
		GameObject boardObject = GameObject.Find("Board");
		GameObject scoreTextObject = GameObject.Find("ScoreText");
		GameObject movesTextObject = GameObject.Find("MovesText");
		GameObject resultPanelObject = GameObject.Find("ResultPanel");
		GameObject resultTextObject = GameObject.Find("ResultText");
		GameObject resetButtonObject = GameObject.Find("ResetButton");
		GameObject helpPanelObject = GameObject.Find("HelpPanel");
		GameObject pressAnyKeyTextObject = GameObject.Find("PressAnyKeyText");
		// Assign objects and components
		ScriptUtilities.AssignObjectFieldToObject(resultPanelObject, go, className, "resultPanelObject");
		ScriptUtilities.AssignComponentFieldToObject(boardObject, scriptPrefix + "Board", go, className, "board");
		ScriptUtilities.AssignComponentFieldToObject(scoreTextObject, "Text", go, className, "scoreText");
		ScriptUtilities.AssignComponentFieldToObject(movesTextObject, "Text", go, className, "movesText");
		ScriptUtilities.AssignComponentFieldToObject(resultTextObject, "Text", go, className, "resultText");
		ScriptUtilities.AssignComponentFieldToObject(resetButtonObject, "Button", go, className, "resetButton");
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
		ScriptUtilities.ConvertScriptToStringBuilder("MT2DBlinkText", scriptsPath);
		ScriptUtilities.ConvertScriptToStringBuilder("MT2DBoard", scriptsPath);
		ScriptUtilities.ConvertScriptToStringBuilder("MT2DGameManager", scriptsPath);
		ScriptUtilities.ConvertScriptToStringBuilder("MT2DGem", scriptsPath);
		ScriptUtilities.ConvertScriptToStringBuilder("MT2DObjectsPool", scriptsPath);
		// Refresh
		AssetDatabase.Refresh();
	}

	private static void WriteMT2DBlinkTextScriptToFile()
	{
		StringBuilder sb = new StringBuilder(1458);

		sb.AppendLine("using System.Collections;");
		sb.AppendLine("using UnityEngine;");
		sb.AppendLine("using UnityEngine.UI;");
		sb.AppendLine("");
		sb.AppendLine("public class MT2DBlinkText : MonoBehaviour");
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

		ScriptUtilities.CreateScriptFile("MT2DBlinkText", scriptsPath, sb.ToString());
	}

	private static void WriteMT2DBoardScriptToFile()
	{
		StringBuilder sb = new StringBuilder(20389);

		sb.AppendLine("using System.Collections;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using UnityEngine;");
		sb.AppendLine("using UnityEngine.InputSystem;");
		sb.AppendLine("");
		sb.AppendLine("public class MT2DBoard : MonoBehaviour");
		sb.AppendLine("{");
		sb.AppendLine("    public MT2DObjectsPool objectsPool;");
		sb.AppendLine("    public Content[,] configurations = new Content[width, height];");
		sb.AppendLine("    [HideInInspector]");
		sb.AppendLine("    public GameObject[] gems = new GameObject[width * height];");
		sb.AppendLine("    [HideInInspector]");
		sb.AppendLine("    public bool dirtied = false;");
		sb.AppendLine("    public bool busy = false;");
		sb.AppendLine("    private const int width = 6;");
		sb.AppendLine("    private const int height = 6;");
		sb.AppendLine("    private MT2DGem selectedGem = null;");
		sb.AppendLine("    private MT2DGameManager gameManager;");
		sb.AppendLine("    ");
		sb.AppendLine("    public enum Content");
		sb.AppendLine("    {");
		sb.AppendLine("        Empty = 0,");
		sb.AppendLine("        Random,");
		sb.AppendLine("        Max");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void Start()");
		sb.AppendLine("    {");
		sb.AppendLine("        gameManager = MT2DGameManager.sharedInstance;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void Update()");
		sb.AppendLine("    {");
		sb.AppendLine("        if (dirtied)");
		sb.AppendLine("        {");
		sb.AppendLine("            dirtied = false;");
		sb.AppendLine("            TidyUpGems();");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void OnMouseDown()");
		sb.AppendLine("    {");
		sb.AppendLine("        // Get mouse position in world space");
		sb.AppendLine("        Vector2 mousePosition = Mouse.current.position.ReadValue();");
		sb.AppendLine("        Vector2 worldPosition = ScreenToWorld(mousePosition);");
		sb.AppendLine("        // Find gem");
		sb.AppendLine("        GameObject gemObject = GetGem((int)worldPosition.x, (int)worldPosition.y);");
		sb.AppendLine("        if (gemObject != null)");
		sb.AppendLine("        {");
		sb.AppendLine("            selectedGem = gemObject.GetComponent<MT2DGem>();");
		sb.AppendLine("            // Call gem's mouse down method");
		sb.AppendLine("            selectedGem.MouseDown(worldPosition);");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void OnMouseUp()");
		sb.AppendLine("    {");
		sb.AppendLine("        if (selectedGem != null)");
		sb.AppendLine("        {");
		sb.AppendLine("            // Get mouse position in world space");
		sb.AppendLine("            Vector2 mousePosition = Mouse.current.position.ReadValue();");
		sb.AppendLine("            Vector2 worldPosition = ScreenToWorld(mousePosition);");
		sb.AppendLine("            // Call gem's mouse up method");
		sb.AppendLine("            selectedGem.MouseUp(worldPosition);");
		sb.AppendLine("            selectedGem = null;");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    void CollapseGems()");
		sb.AppendLine("    {");
		sb.AppendLine("        // Collapse gems onto empty slots");
		sb.AppendLine("        for (int i = 0; i < gems.Length; i++)");
		sb.AppendLine("        {");
		sb.AppendLine("            GameObject gemObject = gems[i];");
		sb.AppendLine("            if (gemObject == null)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            if (!gemObject.activeInHierarchy)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            int row = i / width;");
		sb.AppendLine("            if (row <= 0)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            // Count number of missing gems");
		sb.AppendLine("            int column = i % width;");
		sb.AppendLine("            int numMissing = 0;");
		sb.AppendLine("            for (int j = 0; j < row; j++)");
		sb.AppendLine("            {");
		sb.AppendLine("                if (gems[column + j * width] == null)");
		sb.AppendLine("                {");
		sb.AppendLine("                    numMissing++;");
		sb.AppendLine("                }");
		sb.AppendLine("            }");
		sb.AppendLine("            // Skip if no missing gems");
		sb.AppendLine("            if (numMissing <= 0)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            // Otherwise, move this gem down");
		sb.AppendLine("            gems[i] = null;");
		sb.AppendLine("            SetGem(gemObject, column, row - numMissing);");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("    ");
		sb.AppendLine("    GameObject CheckDownMatches(int column, int row)");
		sb.AppendLine("    {");
		sb.AppendLine("        // Use at initialization to find existing matches");
		sb.AppendLine("        if (row < 2)");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        GameObject object1 = GetGem(column, row - 1);");
		sb.AppendLine("        GameObject object2 = GetGem(column, row - 2);");
		sb.AppendLine("        if (!object1.CompareTag(object2.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        return object1;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    GameObject CheckLeftMatches(int column, int row)");
		sb.AppendLine("    {");
		sb.AppendLine("        // Use at initialization to find existing matches");
		sb.AppendLine("        if (column < 2)");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        GameObject object1 = GetGem(column - 1, row);");
		sb.AppendLine("        GameObject object2 = GetGem(column - 2, row);");
		sb.AppendLine("        if (!object1.CompareTag(object2.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        return object1;");
		sb.AppendLine("    }");
		sb.AppendLine("    ");
		sb.AppendLine("    private void DestroyContents()");
		sb.AppendLine("    {");
		sb.AppendLine("        for (int i = 0; i < width * height; i++)");
		sb.AppendLine("        {");
		sb.AppendLine("            if (gems[i] != null)");
		sb.AppendLine("            {");
		sb.AppendLine("                gems[i].SetActive(false);");
		sb.AppendLine("                gems[i] = null;");
		sb.AppendLine("            }");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void FadeOutMatches(float fadeTime)");
		sb.AppendLine("    {");
		sb.AppendLine("        // Fade out matched gems");
		sb.AppendLine("        int score = 0;");
		sb.AppendLine("        for (int i = 0; i < gems.Length; i++)");
		sb.AppendLine("        {");
		sb.AppendLine("            if (gems[i] == null)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            MT2DGem gem = gems[i].GetComponent<MT2DGem>();");
		sb.AppendLine("            if (!gem.matched)");
		sb.AppendLine("            {");
		sb.AppendLine("                continue;");
		sb.AppendLine("            }");
		sb.AppendLine("            gem.StartFade(fadeTime);");
		sb.AppendLine("            gems[i] = null;");
		sb.AppendLine("            score += gem.value;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (score != 0)");
		sb.AppendLine("        {");
		sb.AppendLine("            gameManager.AddScore(score);");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public GameObject GetGem(int column, int row)");
		sb.AppendLine("    {");
		sb.AppendLine("        if (column < 0 || column >= width || row < 0 || row >= height)");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        GameObject gem = gems[column + row * width];");
		sb.AppendLine("        if (gem && !gem.activeInHierarchy)");
		sb.AppendLine("        {");
		sb.AppendLine("            return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        return gem;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public Vector2 GetPosition(int column, int row)");
		sb.AppendLine("    {");
		sb.AppendLine("        return new Vector2((float)column + 0.5f, (float)row + 0.5f);");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private bool IdentifyMatches()");
		sb.AppendLine("    {");
		sb.AppendLine("        // Iterate through the entire board to indentify matches");
		sb.AppendLine("        bool matched = false;");
		sb.AppendLine("        for (int row = 0; row < height; row++)");
		sb.AppendLine("        {");
		sb.AppendLine("            for (int column = 0; column < width; column++)");
		sb.AppendLine("            {");
		sb.AppendLine("                GameObject gemObject = GetGem(column, row);");
		sb.AppendLine("                if (gemObject != null)");
		sb.AppendLine("                {");
		sb.AppendLine("                    if (IdentifyMatchesDown(gemObject))");
		sb.AppendLine("                    {");
		sb.AppendLine("                        matched = true;");
		sb.AppendLine("                    }");
		sb.AppendLine("                    if (IdentifyMatchesLeft(gemObject))");
		sb.AppendLine("                    {");
		sb.AppendLine("                        matched = true;");
		sb.AppendLine("                    }");
		sb.AppendLine("                }");
		sb.AppendLine("            }");
		sb.AppendLine("        }");
		sb.AppendLine("        return matched;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private bool IdentifyMatchesDown(GameObject gemObject)");
		sb.AppendLine("    {");
		sb.AppendLine("        MT2DGem gem = gemObject.GetComponent<MT2DGem>();");
		sb.AppendLine("        int column = gem.column;");
		sb.AppendLine("        int row = gem.row;");
		sb.AppendLine("");
		sb.AppendLine("        GameObject down1 = GetGem(column, row - 1);");
		sb.AppendLine("        if (down1 == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (!down1.CompareTag(gemObject.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        GameObject down2 = GetGem(column, row - 2);");
		sb.AppendLine("        if (down2 == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (!down2.CompareTag(gemObject.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        gem.matched = true;");
		sb.AppendLine("        down1.GetComponent<MT2DGem>().matched = true;");
		sb.AppendLine("        down2.GetComponent<MT2DGem>().matched = true;");
		sb.AppendLine("        return true;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private bool IdentifyMatchesLeft(GameObject gemObject)");
		sb.AppendLine("    {");
		sb.AppendLine("        MT2DGem gem = gemObject.GetComponent<MT2DGem>();");
		sb.AppendLine("        int column = gem.column;");
		sb.AppendLine("        int row = gem.row;");
		sb.AppendLine("");
		sb.AppendLine("        GameObject left1 = GetGem(column - 1, row);");
		sb.AppendLine("        if (left1 == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (!left1.CompareTag(gemObject.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        GameObject left2 = GetGem(column - 2, row);");
		sb.AppendLine("        if (left2 == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (!left2.CompareTag(gemObject.tag))");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        gem.matched = true;");
		sb.AppendLine("        left1.GetComponent<MT2DGem>().matched = true;");
		sb.AppendLine("        left2.GetComponent<MT2DGem>().matched = true;");
		sb.AppendLine("        return true;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void InitializeContents()");
		sb.AppendLine("    {");
		sb.AppendLine("        // Initialize a boardful of gems with no existing matches");
		sb.AppendLine("        const int maxItems = width * height;");
		sb.AppendLine("        for (int i = 0; i < maxItems; i++)");
		sb.AppendLine("        {");
		sb.AppendLine("            int column = i % width;");
		sb.AppendLine("            int row = i / width;");
		sb.AppendLine("            GameObject avoid1 = CheckLeftMatches(column, row);");
		sb.AppendLine("            GameObject avoid2 = CheckDownMatches(column, row);");
		sb.AppendLine("            GameObject gemObject = RandomGem(avoid1, avoid2);");
		sb.AppendLine("            SetGem(gemObject, column, row, true);");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private GameObject RandomGem(GameObject avoid1 = null, GameObject avoid2 = null)");
		sb.AppendLine("    {");
		sb.AppendLine("        // Create a list of gems, minus the avoids");
		sb.AppendLine("        List<string> gemList = new List<string>() { \"MT2DRed\", \"MT2DOrange\", \"MT2DYellow\", \"MT2DGreen\", \"MT2DBlue\", \"MT2DIndigo\", \"MT2DViolet\" };");
		sb.AppendLine("");
		sb.AppendLine("        if (avoid1 != null)");
		sb.AppendLine("        {");
		sb.AppendLine("            gemList.Remove(avoid1.tag);");
		sb.AppendLine("        }");
		sb.AppendLine("        if (avoid2 != null)");
		sb.AppendLine("        {");
		sb.AppendLine("            gemList.Remove(avoid2.tag);");
		sb.AppendLine("        }");
		sb.AppendLine("");
		sb.AppendLine("        // Randomly pick a gem type to create this gem");
		sb.AppendLine("        int randomInteger = Random.Range(0, gemList.Count);");
		sb.AppendLine("        string gemTag = gemList[randomInteger];");
		sb.AppendLine("        GameObject gemObject = objectsPool.RequestObject();");
		sb.AppendLine("        gemObject.GetComponent<MT2DGem>().Initialize(gemTag);");
		sb.AppendLine("        return gemObject;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void RefillGems()");
		sb.AppendLine("    {");
		sb.AppendLine("        // Create gems on top of columns with missing gems");
		sb.AppendLine("        for (int column = 0; column < width; column++)");
		sb.AppendLine("        {");
		sb.AppendLine("            int numMissing = 0;");
		sb.AppendLine("            for (int  row = 0; row < height; row++)");
		sb.AppendLine("            {");
		sb.AppendLine("                if (GetGem(column, row) != null)");
		sb.AppendLine("                {");
		sb.AppendLine("                    continue;");
		sb.AppendLine("                }");
		sb.AppendLine("                GameObject newGemObject = RandomGem();");
		sb.AppendLine("                newGemObject.transform.position = GetPosition(column, height + numMissing);");
		sb.AppendLine("                SetGem(newGemObject, column, row);");
		sb.AppendLine("                numMissing++;");
		sb.AppendLine("            }");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void Restart()");
		sb.AppendLine("    {");
		sb.AppendLine("        DestroyContents();");
		sb.AppendLine("        InitializeContents();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private Vector2 ScreenToWorld(Vector2 screenPosition)");
		sb.AppendLine("    {");
		sb.AppendLine("        return Camera.main.ScreenToWorldPoint(screenPosition);");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void SetGem(GameObject gemObject, int column, int row, bool teleport=false)");
		sb.AppendLine("    {");
		sb.AppendLine("        int index = column + row * width;");
		sb.AppendLine("        // If gem object is null");
		sb.AppendLine("        if (gemObject == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            // Set and return");
		sb.AppendLine("            gems[index] = null;");
		sb.AppendLine("            return;");
		sb.AppendLine("        }");
		sb.AppendLine("");
		sb.AppendLine("        gems[index] = gemObject;");
		sb.AppendLine("        MT2DGem gem = gemObject.GetComponent<MT2DGem>();");
		sb.AppendLine("        gem.column = column;");
		sb.AppendLine("        gem.row = row;");
		sb.AppendLine("        if (teleport == true)");
		sb.AppendLine("        {");
		sb.AppendLine("            // Set the gem to the position directly");
		sb.AppendLine("            gemObject.transform.position = GetPosition(column, row);");
		sb.AppendLine("        }");
		sb.AppendLine("        else");
		sb.AppendLine("        {");
		sb.AppendLine("            // Move the gem there slowly");
		sb.AppendLine("            gem.StartMovingToCoordinates();");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void TidyUpGems()");
		sb.AppendLine("    {");
		sb.AppendLine("        busy = true;");
		sb.AppendLine("        float totalWait = 0.1f;");
		sb.AppendLine("        const float fadeTime = 0.5f;");
		sb.AppendLine("        const float collapseTime = 0.5f;");
		sb.AppendLine("        const float refillTime = 0.5f;");
		sb.AppendLine("");
		sb.AppendLine("        // If matches have been identified");
		sb.AppendLine("        if (IdentifyMatches())");
		sb.AppendLine("        {");
		sb.AppendLine("            // Fade out the matches");
		sb.AppendLine("            FadeOutMatches(totalWait);");
		sb.AppendLine("            totalWait += fadeTime;");
		sb.AppendLine("            // Wait and then collapse the columns");
		sb.AppendLine("            StartCoroutine(WaitAndCollapseColumns(totalWait));");
		sb.AppendLine("            totalWait += collapseTime;");
		sb.AppendLine("            // Wait and then refill gems");
		sb.AppendLine("            StartCoroutine(WaitAndRefillGems(totalWait));");
		sb.AppendLine("            totalWait += refillTime;");
		sb.AppendLine("        }");
		sb.AppendLine("        StartCoroutine(WaitTillNotBusy(totalWait));");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator WaitAndCollapseColumns(float duration)");
		sb.AppendLine("    {");
		sb.AppendLine("        yield return new WaitForSeconds(duration);");
		sb.AppendLine("        CollapseGems();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator WaitAndRefillGems(float duration)");
		sb.AppendLine("    {");
		sb.AppendLine("        yield return new WaitForSeconds(duration);");
		sb.AppendLine("        RefillGems();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator WaitTillNotBusy(float duration)");
		sb.AppendLine("    {");
		sb.AppendLine("        yield return new WaitForSeconds(duration);");
		sb.AppendLine("");
		sb.AppendLine("        if (IdentifyMatches() == true)");
		sb.AppendLine("        {");
		sb.AppendLine("            dirtied = true;");
		sb.AppendLine("        }");
		sb.AppendLine("        else");
		sb.AppendLine("        {");
		sb.AppendLine("            busy = false;");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("}");

		ScriptUtilities.CreateScriptFile("MT2DBoard", scriptsPath, sb.ToString());
	}

	private static void WriteMT2DGameManagerScriptToFile()
	{
		StringBuilder sb = new StringBuilder(6061);

		sb.AppendLine("using UnityEngine;");
		sb.AppendLine("using UnityEngine.UI;");
		sb.AppendLine("");
		sb.AppendLine("public class MT2DGameManager : MonoBehaviour");
		sb.AppendLine("{");
		sb.AppendLine("    public int maxMoves = 24;");
		sb.AppendLine("    public MT2DBoard board;");
		sb.AppendLine("    public Text scoreText;");
		sb.AppendLine("    public Text movesText;");
		sb.AppendLine("    public GameObject resultPanelObject;");
		sb.AppendLine("    public Text resultText;");
		sb.AppendLine("    public Button resetButton;");
		sb.AppendLine("    public GameObject helpPanelObject;");
		sb.AppendLine("    public Text pressAnyKeyText;");
		sb.AppendLine("    private int score = 0;");
		sb.AppendLine("    private int movesRemaining = 0;");
		sb.AppendLine("    private static bool gameStarted = true;");
		sb.AppendLine("    public static MT2DGameManager sharedInstance = null;");
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
		sb.AppendLine("        ResetGame();");
		sb.AppendLine("        ResetUI();");
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
		sb.AppendLine("        }");
		sb.AppendLine("        // Else if result panel is being displayed");
		sb.AppendLine("        else if (resultPanelObject.activeInHierarchy)");
		sb.AppendLine("        {");
		sb.AppendLine("            // Close it");
		sb.AppendLine("            resultPanelObject.SetActive(false);");
		sb.AppendLine("            // Enable the reset button");
		sb.AppendLine("            resetButton.interactable = true;");
		sb.AppendLine("            // Click it");
		sb.AppendLine("            TaskOnResetButtonClicked();");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void AddScore(int value)");
		sb.AppendLine("    {");
		sb.AppendLine("        score += value;");
		sb.AppendLine("        UpdateScore();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public bool IsGameActive()");
		sb.AppendLine("    {");
		sb.AppendLine("        return gameStarted && movesRemaining > 0 && !board.busy;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void NotifySwap()");
		sb.AppendLine("    {");
		sb.AppendLine("        // A gem swap occurs");
		sb.AppendLine("        // Mark the board as dirtied");
		sb.AppendLine("        board.dirtied = true;");
		sb.AppendLine("        // Decrement moves");
		sb.AppendLine("        movesRemaining--;");
		sb.AppendLine("        UpdateMovesRemaining();");
		sb.AppendLine("        // Check for loss condition");
		sb.AppendLine("        if (movesRemaining <= 0)");
		sb.AppendLine("        {");
		sb.AppendLine("            resultPanelObject.SetActive(true);");
		sb.AppendLine("            gameStarted = false;");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void ResetGame()");
		sb.AppendLine("    {");
		sb.AppendLine("        board.Restart();");
		sb.AppendLine("        score = 0;");
		sb.AppendLine("        movesRemaining = maxMoves;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void ResetUI()");
		sb.AppendLine("    {");
		sb.AppendLine("        resultPanelObject.SetActive(false);");
		sb.AppendLine("        UpdateMovesRemaining();");
		sb.AppendLine("        UpdateScore();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void SetupObjects()");
		sb.AppendLine("    {");
		sb.AppendLine("        resetButton.interactable = false;");
		sb.AppendLine("        resetButton.onClick.AddListener(TaskOnResetButtonClicked);");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void TaskOnResetButtonClicked()");
		sb.AppendLine("    {");
		sb.AppendLine("        ResetGame();");
		sb.AppendLine("        ResetUI();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void UpdateMovesRemaining()");
		sb.AppendLine("    {");
		sb.AppendLine("        movesText.text = \"Moves: \" + movesRemaining;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void UpdateScore()");
		sb.AppendLine("    {");
		sb.AppendLine("        scoreText.text = \"Score: \" + score;");
		sb.AppendLine("    }");
		sb.AppendLine("}");

		ScriptUtilities.CreateScriptFile("MT2DGameManager", scriptsPath, sb.ToString());
	}

	private static void WriteMT2DGemScriptToFile()
	{
		StringBuilder sb = new StringBuilder(10189);

		sb.AppendLine("using System.Collections;");
		sb.AppendLine("using UnityEngine;");
		sb.AppendLine("");
		sb.AppendLine("public class MT2DGem : MonoBehaviour");
		sb.AppendLine("{");
		sb.AppendLine("    public int value = 10;");
		sb.AppendLine("    public Sprite redSprite;");
		sb.AppendLine("    public Sprite orangeSprite;");
		sb.AppendLine("    public Sprite yellowSprite;");
		sb.AppendLine("    public Sprite greenSprite;");
		sb.AppendLine("    public Sprite blueSprite;");
		sb.AppendLine("    public Sprite indigoSprite;");
		sb.AppendLine("    public Sprite violetSprite;");
		sb.AppendLine("    [HideInInspector]");
		sb.AppendLine("    public bool matched = false;");
		sb.AppendLine("    [HideInInspector]");
		sb.AppendLine("    public int column = 0;");
		sb.AppendLine("    [HideInInspector]");
		sb.AppendLine("    public int row = 0;");
		sb.AppendLine("    private SpriteRenderer spriteRenderer;");
		sb.AppendLine("    private MT2DGameManager gameManager;");
		sb.AppendLine("    private Vector2 touchDownPoint;");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator FadeToOblivion(float fadeTime)");
		sb.AppendLine("    {");
		sb.AppendLine("        // Fade this gem to oblivion");
		sb.AppendLine("        float fadeSpeed = 1.0f / fadeTime;");
		sb.AppendLine("        while (true)");
		sb.AppendLine("        {");
		sb.AppendLine("            Color color = spriteRenderer.color;");
		sb.AppendLine("            color.a -= Time.deltaTime * fadeSpeed;");
		sb.AppendLine("            if (color.a <= 0.0f)");
		sb.AppendLine("            {");
		sb.AppendLine("                break;");
		sb.AppendLine("            }");
		sb.AppendLine("            spriteRenderer.color = color;");
		sb.AppendLine("            yield return null;");
		sb.AppendLine("        }");
		sb.AppendLine("        // Then destroy it");
		sb.AppendLine("        gameObject.SetActive(false);");
		sb.AppendLine("        //gameManager.board.SetGem(null, column, row, true);");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void Initialize(string tag)");
		sb.AppendLine("    {");
		sb.AppendLine("        gameManager = MT2DGameManager.sharedInstance;");
		sb.AppendLine("        spriteRenderer = GetComponent<SpriteRenderer>();");
		sb.AppendLine("        // Reset values");
		sb.AppendLine("        matched = false;");
		sb.AppendLine("        // Reset color");
		sb.AppendLine("        spriteRenderer.color = Color.white;");
		sb.AppendLine("        // Set up object");
		sb.AppendLine("        gameObject.name = tag;");
		sb.AppendLine("        gameObject.tag = tag;");
		sb.AppendLine("        // Get the correct sprite");
		sb.AppendLine("        UpdateSprite();");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void MouseDown(Vector2 worldPosition)");
		sb.AppendLine("    {");
		sb.AppendLine("        if (!gameManager.IsGameActive())");
		sb.AppendLine("        {");
		sb.AppendLine("            return;");
		sb.AppendLine("        }");
		sb.AppendLine("        // Remember touch point 1");
		sb.AppendLine("        touchDownPoint = worldPosition;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void MouseUp(Vector2 worldPosition)");
		sb.AppendLine("    {");
		sb.AppendLine("        if (!gameManager.IsGameActive())");
		sb.AppendLine("        {");
		sb.AppendLine("            return;");
		sb.AppendLine("        }");
		sb.AppendLine("");
		sb.AppendLine("        // Save touch point 2");
		sb.AppendLine("        Vector2 touchUpPoint = worldPosition;");
		sb.AppendLine("        // Attemp to swap the gems at these two points");
		sb.AppendLine("        if (SwapPoints(touchDownPoint, touchUpPoint))");
		sb.AppendLine("        {");
		sb.AppendLine("            StartCoroutine(WaitAndNotify());");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator MoveToCoordinates()");
		sb.AppendLine("    {");
		sb.AppendLine("        MT2DBoard board = gameManager.board;");
		sb.AppendLine("");
		sb.AppendLine("        Vector2 targetPosition = board.GetPosition(column, row);");
		sb.AppendLine("        const float thresholdSquare = 0.1f * 0.1f;");
		sb.AppendLine("        const float k = 10.0f;");
		sb.AppendLine("        while (true)");
		sb.AppendLine("        {");
		sb.AppendLine("            // If close enough to the target position");
		sb.AppendLine("            Vector2 difference = targetPosition - (Vector2)transform.position;");
		sb.AppendLine("            if (difference.sqrMagnitude < thresholdSquare)");
		sb.AppendLine("            {");
		sb.AppendLine("                // Set it");
		sb.AppendLine("                transform.position = targetPosition;");
		sb.AppendLine("                break;");
		sb.AppendLine("            }");
		sb.AppendLine("            // Else");
		sb.AppendLine("            else");
		sb.AppendLine("            {");
		sb.AppendLine("                // Interpolate the position");
		sb.AppendLine("                transform.position += (Vector3)difference * Time.deltaTime * k;");
		sb.AppendLine("            }");
		sb.AppendLine("            yield return null;");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void StartFade(float fadeTime)");
		sb.AppendLine("    {");
		sb.AppendLine("        StartCoroutine(\"FadeToOblivion\", fadeTime);");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    public void StartMovingToCoordinates()");
		sb.AppendLine("    {");
		sb.AppendLine("        StartCoroutine(\"MoveToCoordinates\");");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private bool SwapWith(int targetColumn, int targetRow)");
		sb.AppendLine("    {");
		sb.AppendLine("        MT2DBoard board = gameManager.board;");
		sb.AppendLine("        GameObject neighbor = board.GetGem(targetColumn, targetRow);");
		sb.AppendLine("        if (neighbor == null)");
		sb.AppendLine("        {");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        // Swap two gems");
		sb.AppendLine("        board.SetGem(neighbor, column, row);");
		sb.AppendLine("        board.SetGem(gameObject, targetColumn, targetRow);");
		sb.AppendLine("        return true;");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private bool SwapPoints(Vector2 point1, Vector2 point2)");
		sb.AppendLine("    {");
		sb.AppendLine("        Vector2 difference = point2 - point1;");
		sb.AppendLine("        const float thresholdSquare = 0.2f * 0.2f;");
		sb.AppendLine("        // If the two touch points are too close");
		sb.AppendLine("        if (difference.sqrMagnitude <= thresholdSquare)");
		sb.AppendLine("        {");
		sb.AppendLine("            // Do not swap");
		sb.AppendLine("            return false;");
		sb.AppendLine("        }");
		sb.AppendLine("        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))");
		sb.AppendLine("        {");
		sb.AppendLine("            if (difference.x > 0.0f)");
		sb.AppendLine("            {");
		sb.AppendLine("                // Swap with right neighbor");
		sb.AppendLine("                return SwapWith(column + 1, row);");
		sb.AppendLine("            }");
		sb.AppendLine("            else");
		sb.AppendLine("            {");
		sb.AppendLine("                // Swap with left neighbor");
		sb.AppendLine("                return SwapWith(column - 1, row);");
		sb.AppendLine("            }");
		sb.AppendLine("        }");
		sb.AppendLine("        else");
		sb.AppendLine("        {");
		sb.AppendLine("            if (difference.y > 0.0f)");
		sb.AppendLine("            {");
		sb.AppendLine("                // Swap with top neighbor");
		sb.AppendLine("                return SwapWith(column, row + 1);");
		sb.AppendLine("            }");
		sb.AppendLine("            else");
		sb.AppendLine("            {");
		sb.AppendLine("                // Swap with bottom neighbor");
		sb.AppendLine("                return SwapWith(column, row - 1);");
		sb.AppendLine("            }");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private void UpdateSprite()");
		sb.AppendLine("    {");
		sb.AppendLine("        if (CompareTag(\"MT2DRed\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = redSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DOrange\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = orangeSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DYellow\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = yellowSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DGreen\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = greenSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DBlue\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = blueSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DIndigo\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = indigoSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("        else if (CompareTag(\"MT2DViolet\"))");
		sb.AppendLine("        {");
		sb.AppendLine("            spriteRenderer.sprite = violetSprite;");
		sb.AppendLine("        }");
		sb.AppendLine("    }");
		sb.AppendLine("");
		sb.AppendLine("    private IEnumerator WaitAndNotify()");
		sb.AppendLine("    {");
		sb.AppendLine("        yield return new WaitForSeconds(0.25f);");
		sb.AppendLine("        gameManager.NotifySwap();");
		sb.AppendLine("    }");
		sb.AppendLine("}");

		ScriptUtilities.CreateScriptFile("MT2DGem", scriptsPath, sb.ToString());
	}

	private static void WriteMT2DObjectsPoolScriptToFile()
	{
		StringBuilder sb = new StringBuilder(1824);

		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using UnityEngine;");
		sb.AppendLine("");
		sb.AppendLine("public class MT2DObjectsPool : MonoBehaviour");
		sb.AppendLine("{");
		sb.AppendLine("    public GameObject gemPrefab;");
		sb.AppendLine("    private List<GameObject> list = new List<GameObject>(36);");
		sb.AppendLine("");
		sb.AppendLine("    public GameObject RequestObject()");
		sb.AppendLine("    {");
		sb.AppendLine("        GameObject go = null;");
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
		sb.AppendLine("            go = Instantiate(gemPrefab);");
		sb.AppendLine("            go.name = gemPrefab.name;");
		sb.AppendLine("            go.transform.SetParent(transform);");
		sb.AppendLine("            list.Add(go);");
		sb.AppendLine("        }");
		sb.AppendLine("        // Return value");
		sb.AppendLine("        return go;");
		sb.AppendLine("    }");
		sb.AppendLine("}");

		ScriptUtilities.CreateScriptFile("MT2DObjectsPool", scriptsPath, sb.ToString());
	}
}
