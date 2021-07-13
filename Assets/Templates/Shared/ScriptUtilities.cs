using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UI;

public class ScriptUtilities : Editor
{
    private static AddRequest request;
    private static string templates2dBuilding = "template2dBuilding";
    private static string templates2dCompleted = "templates2dCompleted";
    private static readonly List<string> templates = new List<string>{
        "AlienAttack2D",
        "BattleTank2D",
        "Drive2D",
        "EndlessRunner2D",
        "HexStrategy2D",
        "JewelHunter2D",
        "MatchThree2D",
        "MiniGolf2D",
        "RockAttack2D",
        "RocketDocker2D",
        "WallBreaker2D"
    };

    private static void UpdateOnce()
    {
        EditorApplication.update -= UpdateOnce;
        string templateName = EditorPrefs.GetString(templates2dCompleted, "");
        if (templateName == "")
        {
            Debug.LogWarning("Template name not set!");
            return;
        }
        EditorPrefs.DeleteKey(templates2dCompleted);
        int index = templates.IndexOf(templateName);
        // If all registered templates have been built, delete int editor prefs and bail
        if (index >= templates.Count - 1)
        {
            EditorPrefs.DeleteKey(templates2dBuilding);
            Debug.Log("All templates built!");
            return;
        }
        // Build the next registered template
        index++;
        BuildTemplate(index);
    }

    [MenuItem("Templates/2D Templates Delete All", false, -1000)]
    private static void DeleteAll()
    {
        for (int i = 0; i < templates.Count; i++)
        {
            DeleteTemplate(i);
        }
        DeletePrefsKeys();
        AssetDatabase.Refresh();
        Debug.Log("All templates deleted!");
    }

    private static void DeletePrefsKeys()
    {
        EditorPrefs.DeleteKey(templates2dCompleted);
        EditorPrefs.DeleteKey(templates2dBuilding);
    }

    [MenuItem("Templates/2D Templates Generate All", false, -1000)]
    private static void GenerateAll()
    {
        DeletePrefsKeys();
        int index = 0;
        EditorPrefs.SetBool(templates2dBuilding, true);
        BuildTemplate(index);
    }

    private static void BuildTemplate(int index)
    {
        if (index > templates.Count - 1)
        {
            Debug.LogWarning("Index " + index + " not in range of registered templates list.");
            return;
        }
        if (templates[index] == null)
        {
            Debug.LogWarning("Template name at index " + index + " is null.");
            return;
        }
        System.Reflection.MethodInfo methodInfo = Type.GetType(templates[index]).GetMethod("GenerateGameTemplate");
        if (methodInfo == null)
        {
            Debug.LogWarning("Template's methodInfo is null. Please make sure the GenerateGameTemplate method has public access.");
            return;
        }
        Debug.Log("Generating " + templates[index] + "...");
        methodInfo.Invoke(null, null);
    }

    private static void DeleteTemplate(int index)
    {
        if (index > templates.Count - 1)
        {
            Debug.LogWarning("Index " + index + " not in range of registered templates list.");
            return;
        }
        Type.GetType(templates[index]).GetMethod("DeleteGameTemplate").Invoke(null, null);
        Debug.Log("Deleting " + templates[index] + "...");
    }

    public static void NotifyBuildComplete(string templateName)
    {
        bool isBuilding = EditorPrefs.GetBool(templates2dBuilding, false);
        // Nothing to do if not building all templates
        if (isBuilding == false)
        {
            Debug.Log("Single build completed.");
            return;
        }
        // Set editor prefs of completed template
        EditorPrefs.SetString(templates2dCompleted, templateName);
        EditorApplication.update += UpdateOnce;
    }

    public static void AssignAnyFieldToObject(System.Object value, GameObject container, string className, string fieldName)
    {
        Type classType = Type.GetType(className);
        var component = container.GetComponent(classType);
        component.GetType().GetField(fieldName).SetValue(component, value);
    }

    public static void AssignBoolFieldToObject(bool flag, GameObject container, string className, string fieldName)
    {
        System.Object value = flag;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignColorFieldToObject(Color color, GameObject container, string className, string fieldName)
    {
        System.Object value = color;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignComponentFieldToObject(GameObject gameObject, string componentName, GameObject container, string className, string fieldName)
    {
        System.Object value = gameObject.GetComponent(componentName);
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignFloatFieldToObject(float number, GameObject container, string className, string fieldName)
    {
        System.Object value = number;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignIntFieldToObject(int number, GameObject container, string className, string fieldName)
    {
        System.Object value = number;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignLayerMaskToObject(LayerMask layerMask, GameObject container, string className, string fieldName)
    {
        System.Object value = layerMask;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignObjectFieldToObject(GameObject gameObject, GameObject container, string className, string fieldName)
    {
        System.Object value = gameObject;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignObjectsFieldToObject(GameObject[] objects, GameObject container, string className, string fieldName)
    {
        System.Object value = objects;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignSpriteFieldToObject(Sprite sprite, GameObject container, string className, string fieldName)
    {
        System.Object value = sprite;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignTextFieldToObject(Text text, GameObject container, string className, string fieldName)
    {
        System.Object value = text;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AssignVector2DFieldToObject(Vector2 vector, GameObject container, string className, string fieldName)
    {
        System.Object value = vector;
        AssignAnyFieldToObject(value, container, className, fieldName);
    }

    public static void AttachScriptToObject(string classTypeName, GameObject gameObject)
    {
        var type = Type.GetType(classTypeName);
        if (type != null)
        {
            var component = gameObject.GetComponent(type);
            if (component == null)
            {
                gameObject.AddComponent(type);
            }
        }
        else
        {
            Debug.LogWarning("Invalid class type " + classTypeName);
        }
    }

    public static bool CheckTypes(string prefix, string[] typeNames)
    {
        // Check if all types (prepended with prefix) in array exist
        foreach (string typeName in typeNames)
        {
            if (Type.GetType(prefix + typeName) == null)
            {
                return false;
            }
        }

        return true;
    }

    public static void Configure2DSettings()
    {
        // Set project settings
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;

        // Turn on scene view 2D mode
        foreach (SceneView sv in SceneView.sceneViews)
        {
            sv.in2DMode = true;
        }

        // Set window->rendering->lighting settings
        const float k = 1.0f / 255.0f;
        RenderSettings.ambientLight = new Color(54.0f * k, 58.0f * k, 66.0f * k);
    }

    public static void ConvertScriptToStringBuilder(string fileName, string assetsRelativePath)
    {
        // Utility to convert a script source code into a StringBuilder append statements, then save to a file

        // Create string builder and preceding strings
        StringBuilder sb = new StringBuilder();
        sb.Append(Environment.NewLine);

        // Read and format source code line by line
        string completePath = Application.dataPath + "/" + assetsRelativePath + "/" + fileName + ".cs";
        System.IO.StreamReader inFile = new System.IO.StreamReader(completePath);

        string line;
        while ((line = inFile.ReadLine()) != null)
        {
            // If the line contains escape character
            if (line.Contains("\\"))
            {
                line = ConvertSpecialCharacter(line, "\\", "\\\\");
            }
            // If the line contains double quotes
            if (line.Contains("\""))
            {
                line = ConvertSpecialCharacter(line, "\"", "\\\"");
            }
            // If the line does not contains script conversion method call
            if (!line.Contains("ConvertScriptToStringBuilder"))
            {
                // Append
                sb.Append(Environment.NewLine + "\tsb.AppendLine(\"" + line + "\");");
            }
        }
        inFile.Close();

        // Call file creation method
        sb.Append(Environment.NewLine);
        sb.Append(Environment.NewLine + "\tScriptUtilities.CreateScriptFile(\"" + fileName + "\", scriptsPath, sb.ToString());");

        // Close brace
        sb.Append(Environment.NewLine + "}");

        // Insert method name and StringBuilder declaration at start of string, in reversed order
        const int safety = 200;
        int length = sb.Length + safety;
        sb.Insert(0, Environment.NewLine + "\tStringBuilder sb = new StringBuilder(" + length + ");");
        sb.Insert(0, Environment.NewLine + "{");
        sb.Insert(0, "private static void Write" + fileName + "ScriptToFile()");

        // Write to another file
        completePath += ".txt";
        File.CreateText(completePath).Dispose();
        using (TextWriter outFile = new StreamWriter(completePath, false))
        {
            outFile.Write(sb.ToString());
            outFile.Close();
        }
    }

    public static void ConvertScriptToStringContent(string fileName, string assetsRelativePath)
    {
        // Make a StringBuilder version of this method!!!
            // then run a profiler to check both time and memory usage!

        // Utility to convert a script source code into a giant string statement, then save to a file

        // Read and format source code line by line
        string completePath = Application.dataPath + "/" + assetsRelativePath + "/" + fileName + ".cs";
        System.IO.StreamReader inFile = new System.IO.StreamReader(completePath);
        string line;
        string content = "";
        while ((line = inFile.ReadLine()) != null)
        {
            // If the line contains escape character
            if (line.Contains("\\"))
            {
                line = ConvertSpecialCharacter(line, "\\", "\\\\");
            }
            // If the line contains double quotes
            if (line.Contains("\""))
            {
                line = ConvertSpecialCharacter(line, "\"", "\\\"");
            }

            line = "\t\t\t\"" + line + "\"" + " + Environment.NewLine + " + "\n";
            content += line;
        }
        inFile.Close();

        // Add this to the end
        content += "\t\t\t\"\";";

        // Write to another file
        completePath += ".txt";
        File.CreateText(completePath).Dispose();
        using (TextWriter outFile = new StreamWriter(completePath, false))
        {
            outFile.Write(content);
            outFile.Close();
        }
    }

    private static string ConvertSpecialCharacter(string line, string character, string replacement)
    {
        StringBuilder sb = new StringBuilder(line);
        sb.Replace(character, replacement);
        return sb.ToString();
    }

    public static int CreateLayer(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Layers Property
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        int maxLayers = 32;
        if (!PropertyExists(layersProp, 0, maxLayers, layerName))
        {
            SerializedProperty sp;
            // Start at layer 9th index -> 8 (zero based) => first 8 reserved for unity / greyed out
            for (int i = 8, j = maxLayers; i < j; i++)
            {
                sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == "")
                {
                    // Assign string value to layer
                    sp.stringValue = layerName;
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
                if (i == j)
                {
                    Debug.LogWarning("Cannot add new layer. All allowed layers have been filled");
                }
            }
        }
        else
        {
            Debug.Log("Layer: " + layerName + " already exists");
            return IndexOfLayer(layerName);
        }
        return -1;
    }

    public static void CreateScriptFile(string name, string assetsRelativePath, string content)
    {
        string completePath = Application.dataPath + "/" + assetsRelativePath + "/" + name + ".cs";
        File.CreateText(completePath).Dispose();
        using (TextWriter outFile = new StreamWriter(completePath, false))
        {
            outFile.Write(content);
            outFile.Close();
        }
    }

    public static bool CreateTag(string tagName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Layers Property
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (!PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName))
        {
            int index = tagsProp.arraySize;
            tagsProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = tagsProp.GetArrayElementAtIndex(index);
            sp.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            return true;
        }
        else
        {
            Debug.Log("Tag: " + tagName + " already exists");
        }
        return false;
    }

    public static bool CreateSortingLayer(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Layers Property
        SerializedProperty layersProp = tagManager.FindProperty("m_SortingLayers");
        if (!PropertySortingLayerExists(layersProp, 0, layersProp.arraySize, layerName))
        {
            int index = layersProp.arraySize;
            // Create an unique ID
            List<int> existingIDs = new List<int>();
            for (int i = 0; i < index; i++)
            {
                SerializedProperty spElement = layersProp.GetArrayElementAtIndex(i);
                int ID = spElement.FindPropertyRelative("uniqueID").intValue;
                existingIDs.Add(ID);
            }
            int uniqueID = UnityEngine.Random.Range(0, int.MaxValue);
            while (existingIDs.Contains(uniqueID))
            {
                uniqueID = UnityEngine.Random.Range(0, int.MaxValue);
            }
            // Create the new sorting layer element
            layersProp.InsertArrayElementAtIndex(index);
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(index);
            sp.FindPropertyRelative("name").stringValue = layerName;
            sp.FindPropertyRelative("uniqueID").intValue = uniqueID;
            // Save property
            tagManager.ApplyModifiedProperties();
            return true;
        }
        else
        {
            Debug.Log("Sorting Layer: " + layerName + " already exists");
        }
        return false;
    }

    public static int IndexOfLayer(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        // layers Property
        const int maxLayers = 32;
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Index with default value 0
        int index = 0;

        for (int i = 0; i < maxLayers; i++)
        {
            SerializedProperty t = layersProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(layerName))
            {
                index = i;
                break;
            }
        }

        return index;
    }

    public static void InstallPackage(string packageName)
    {
        // Add a package to the project
        request = Client.Add(packageName);
        EditorApplication.update += InstallPackageProgress;
    }

    private static void InstallPackageProgress()
    {
        if (request.IsCompleted)
        {
            if (request.Status == StatusCode.Success)
            {
                Debug.Log("Installed: " + request.Result.packageId);
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.Log(request.Error.message);
            }

            EditorApplication.update -= InstallPackageProgress;
        }
    }

    public static bool LayerExists(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        // Layers Property
        const int maxLayers = 32;
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        return PropertyExists(layersProp, 0, maxLayers, layerName);
    }

    private static bool PropertyExists(SerializedProperty property, int start, int end, string value)
    {
        for (int i = start; i < end; i++)
        {
            SerializedProperty t = property.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(value))
            {
                return true;
            }
        }
        return false;
    }

    private static bool PropertySortingLayerExists(SerializedProperty property, int start, int end, string value)
    {
        for (int i = start; i < end; i++)
        {            
            SerializedProperty t = property.GetArrayElementAtIndex(i);
            if (t.FindPropertyRelative("name").stringValue == value)
            {
                return true;
            }
        }
        return false;
    }

    public static int RemoveLayer(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Tags Property
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        if (PropertyExists(layersProp, 0, layersProp.arraySize, layerName))
        {
            SerializedProperty sp;
            for (int i = 0, j = layersProp.arraySize; i < j; i++)
            {
                sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == layerName)
                {
                    sp.stringValue = "";
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return i;
                }
            }
        }
        return 0;
    }

    public static bool RemoveSortingLayer(string layerName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Tags Property
        SerializedProperty layersProp = tagManager.FindProperty("m_SortingLayers");
        if (PropertySortingLayerExists(layersProp, 0, layersProp.arraySize, layerName))
        {
            SerializedProperty sp;
            for (int i = 0, j = layersProp.arraySize; i < j; i++)
            {
                sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.FindPropertyRelative("name").stringValue == layerName)
                {
                    layersProp.DeleteArrayElementAtIndex(i);
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return true;
                }
            }
        }
        return false;
    }

    public static bool RemoveTag(string tagName)
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        // Tags Property
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (PropertyExists(tagsProp, 0, tagsProp.arraySize, tagName))
        {
            SerializedProperty sp;
            for (int i = 0, j = tagsProp.arraySize; i < j; i++)
            {
                sp = tagsProp.GetArrayElementAtIndex(i);
                if (sp.stringValue == tagName)
                {
                    sp.stringValue = "";
                    tagsProp.DeleteArrayElementAtIndex(i);
                    // Save settings
                    tagManager.ApplyModifiedProperties();
                    return true;
                }
            }
        }
        return false;
    }
}