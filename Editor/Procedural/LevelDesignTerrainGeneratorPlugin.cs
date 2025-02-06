using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LevelDesignTerrainGeneratorPlugin : EditorWindow
{
    // Terrain parameters.
    private int terrainWidth = 512;
    private int terrainLength = 512;
    private int terrainHeight = 100;
    public float mountainThreshold = 0.4f;
    public float blendStrength = 5.0f;
    public float plainHeightMultiplier = 0.1f;

    // fBm Noise parameters.
    private float scale = 20f;
    private int octaves = 6;
    private float lacunarity = 2.0f;
    private float gain = 0.5f;
    private Vector2 offset = Vector2.zero;

    // Water & Texture settings.
    [Range(0f, 1f)]
    public float waterLevel = 0.2f; // normalized water level.
    [Range(0f, 0.2f)]
    public float waterBlendRange = 0.05f;    // For water/sand.
    [Range(0f, 0.2f)]
    public float mountainBlendRange = 0.05f; // For grass/rock.
    [Range(0f, 0.2f)]
    public float snowBlendRange = 0.05f;     // For rock/snow.

    // --- Texture Group Elements ---
    // For simplicity, we use fixed–size arrays.
    public TextureGroupElement[] sandGroupElements = new TextureGroupElement[1];
    public TextureGroupElement[] grassGroupElements = new TextureGroupElement[1];
    public TextureGroupElement[] rockGroupElements = new TextureGroupElement[1];
    public TextureGroupElement[] snowGroupElements = new TextureGroupElement[1];

    // Foldout booleans to allow minimizing each group.
    private bool sandGroupFoldout = true;
    private bool grassGroupFoldout = true;
    private bool rockGroupFoldout = true;
    private bool snowGroupFoldout = true;

    // Scroll position for the overall window.
    private Vector2 scrollPos;

    // Optionally assign an existing terrain.
    private Terrain targetTerrain;

    [MenuItem("Tools/Level Design Terrain Generator")]
    public static void ShowWindow()
    {
        GetWindow<LevelDesignTerrainGeneratorPlugin>("Level Design Terrain Generator");
    }

    private void OnEnable()
    {
        // Ensure each array has at least one element.
        if (sandGroupElements == null || sandGroupElements.Length == 0)
            sandGroupElements = new TextureGroupElement[1] { new TextureGroupElement() };
        if (grassGroupElements == null || grassGroupElements.Length == 0)
            grassGroupElements = new TextureGroupElement[1] { new TextureGroupElement() };
        if (rockGroupElements == null || rockGroupElements.Length == 0)
            rockGroupElements = new TextureGroupElement[1] { new TextureGroupElement() };
        if (snowGroupElements == null || snowGroupElements.Length == 0)
            snowGroupElements = new TextureGroupElement[1] { new TextureGroupElement() };
    }

    private void OnGUI()
    {
        // Wrap everything in a scroll view.
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Level Design Terrain Generator", EditorStyles.boldLabel);

        // Terrain selection.
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Terrain", targetTerrain, typeof(Terrain), true);

        GUILayout.Space(10);
        GUILayout.Label("Terrain Settings", EditorStyles.boldLabel);
        terrainWidth = EditorGUILayout.IntField("Width", terrainWidth);
        terrainLength = EditorGUILayout.IntField("Length", terrainLength);
        terrainHeight = EditorGUILayout.IntField("Height", terrainHeight);

        GUILayout.Space(10);
        GUILayout.Label("fBm Noise Settings", EditorStyles.boldLabel);
        scale = EditorGUILayout.FloatField("Scale", scale);
        octaves = EditorGUILayout.IntField("Octaves", octaves);
        lacunarity = EditorGUILayout.FloatField("Lacunarity", lacunarity);
        gain = EditorGUILayout.FloatField("Gain", gain);
        offset = EditorGUILayout.Vector2Field("Offset", offset);
        mountainThreshold = EditorGUILayout.FloatField("Mountain Threshold", mountainThreshold);
        blendStrength = EditorGUILayout.FloatField("Blend Strength", blendStrength);
        plainHeightMultiplier = EditorGUILayout.FloatField("Plain Height Multiplier", plainHeightMultiplier);

        GUILayout.Space(10);
        GUILayout.Label("Water & Texture Settings", EditorStyles.boldLabel);
        waterLevel = EditorGUILayout.Slider("Water Level", waterLevel, 0f, 1f);
        waterBlendRange = EditorGUILayout.Slider("Water Blend Range", waterBlendRange, 0f, 0.2f);
        mountainBlendRange = EditorGUILayout.Slider("Mountain Blend Range", mountainBlendRange, 0f, 0.2f);
        snowBlendRange = EditorGUILayout.Slider("Snow Blend Range", snowBlendRange, 0f, 0.2f);

        GUILayout.Space(10);
        // Display each texture group inside a foldout.
        sandGroupFoldout = EditorGUILayout.Foldout(sandGroupFoldout, "Sand Group");
        if (sandGroupFoldout)
        {
            sandGroupElements = DisplayTextureGroupElements(sandGroupElements);
        }

        GUILayout.Space(10);
        grassGroupFoldout = EditorGUILayout.Foldout(grassGroupFoldout, "Grass Group");
        if (grassGroupFoldout)
        {
            grassGroupElements = DisplayTextureGroupElements(grassGroupElements);
        }

        GUILayout.Space(10);
        rockGroupFoldout = EditorGUILayout.Foldout(rockGroupFoldout, "Rock Group");
        if (rockGroupFoldout)
        {
            rockGroupElements = DisplayTextureGroupElements(rockGroupElements);
        }

        GUILayout.Space(10);
        snowGroupFoldout = EditorGUILayout.Foldout(snowGroupFoldout, "Snow Group");
        if (snowGroupFoldout)
        {
            snowGroupElements = DisplayTextureGroupElements(snowGroupElements);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Terrain"))
        {
            // Generate the terrain geometry using your procedural generation code.
            targetTerrain = ProceduralTerrainGeneration.GenerateTerrain(
                terrainWidth, terrainLength, terrainHeight,
                scale, octaves, lacunarity, gain, offset,
                mountainThreshold, blendStrength, plainHeightMultiplier, targetTerrain);

            // Build a dictionary mapping group names to lists of TextureGroupElement.
            Dictionary<string, List<TextureGroupElement>> textureGroups = new Dictionary<string, List<TextureGroupElement>>();
            textureGroups["Sand"] = new List<TextureGroupElement>(sandGroupElements);
            textureGroups["Grass"] = new List<TextureGroupElement>(grassGroupElements);
            textureGroups["Rock"] = new List<TextureGroupElement>(rockGroupElements);
            textureGroups["Snow"] = new List<TextureGroupElement>(snowGroupElements);

            // Create the texture drawer with the group information.
            ProceduralTerrainTextureDrawer textureDrawer = new ProceduralTerrainTextureDrawer(
                waterLevel, mountainThreshold,
                waterBlendRange, mountainBlendRange, snowBlendRange,
                terrainHeight, textureGroups);
            textureDrawer.PaintTerrain(targetTerrain);

            // Instantiate or reposition the water prefab.
            InstantiateWaterPrefab();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Displays a list of TextureGroupElement objects with resizable count and add/remove functionality.
    /// Returns the updated array.
    /// </summary>
    private TextureGroupElement[] DisplayTextureGroupElements(TextureGroupElement[] elements)
    {
        // Convert array to list for easier manipulation.
        List<TextureGroupElement> elementList = new List<TextureGroupElement>();
        if (elements != null)
        {
            elementList.AddRange(elements);
        }
        else
        {
            elementList.Add(new TextureGroupElement());
        }

        // Optionally, display an editable count field.
        int newCount = Mathf.Max(1, EditorGUILayout.IntField("Element Count", elementList.Count));
        while (elementList.Count < newCount)
        {
            elementList.Add(new TextureGroupElement());
        }
        while (elementList.Count > newCount)
        {
            elementList.RemoveAt(elementList.Count - 1);
        }

        // Display each element inside a box.
        for (int i = 0; i < elementList.Count; i++)
        {
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Element " + (i + 1));

            // Draw fields for this element.
            elementList[i].elementName = EditorGUILayout.TextField("Name", elementList[i].elementName);
            elementList[i].diffuseTexture = (Texture2D)EditorGUILayout.ObjectField("Diffuse Texture", elementList[i].diffuseTexture, typeof(Texture2D), false);
            elementList[i].normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map", elementList[i].normalMap, typeof(Texture2D), false);
            elementList[i].strength = EditorGUILayout.Slider("Strength", elementList[i].strength, 0f, 1f);
            elementList[i].noiseScale = EditorGUILayout.FloatField("Noise Scale", elementList[i].noiseScale);
            elementList[i].noiseOffset = EditorGUILayout.Vector2Field("Noise Offset", elementList[i].noiseOffset);

            // Only show the remove button if there is more than one element.
            if (elementList.Count > 1)
            {
                if (GUILayout.Button("Remove Element"))
                {
                    elementList.RemoveAt(i);
                    // Break out of the loop after removal to avoid errors.
                    break;
                }
            }

            GUILayout.EndVertical();
        }

        // Add a button to add a new element.
        if (GUILayout.Button("Add New Element"))
        {
            elementList.Add(new TextureGroupElement());
        }

        return elementList.ToArray();
    }

    /// <summary>
    /// Instantiates (or repositions) the water prefab at the appropriate level.
    /// The prefab should be located in Resources/Level Generator/Terrain/Water.prefab.
    /// </summary>
    private void InstantiateWaterPrefab()
    {
        GameObject waterPrefab = Resources.Load<GameObject>("Level Generator/Terrain/Water");
        if (waterPrefab == null)
        {
            Debug.LogError("Water prefab not found at Resources/Level Generator/Terrain/Water");
            return;
        }

        float waterY = waterLevel * terrainHeight;
        float centerX = terrainWidth / 2f;
        float centerZ = terrainLength / 2f;
        Vector3 waterPosition = new Vector3(centerX, waterY, centerZ);

        GameObject existingWater = GameObject.Find(waterPrefab.name);
        if (existingWater == null)
        {
            GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);
            waterInstance.transform.position = waterPosition;
            waterInstance.name = waterPrefab.name;
        }
        else
        {
            existingWater.transform.position = waterPosition;
        }
    }
}