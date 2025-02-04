using UnityEngine;
using UnityEditor;

public class StylizedWaterPlugin
{
    [MenuItem("GameObject/Create Stylized Water", false, 10)]
    public static void CreateStylizedWater(MenuCommand menuCommand)
    {
        // Create a plane to serve as the water surface.
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "StylizedWater";

        // Locate the URP water shader.
        Shader waterShader = Shader.Find("Custom/URPStylizedWater");
        if (waterShader == null)
        {
            Debug.LogError("Could not find the URPStylizedWater shader. " +
                "Please ensure 'URPStylizedWater.shader' is in your project.");
            return;
        }

        // Create a new material using the water shader.
        Material waterMaterial = new Material(waterShader);

        // Optionally, set a normal map if you have one.
        // Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/WaterNormal.png");
        // if (normalMap != null)
        //     waterMaterial.SetTexture("_NormalMap", normalMap);

        // Set default shader properties.
        waterMaterial.SetColor("_WaterColor", new Color(0.1f, 0.3f, 0.6f, 1f));
        waterMaterial.SetColor("_DeepColor", new Color(0.0f, 0.1f, 0.3f, 1f));
        waterMaterial.SetFloat("_WaveAmplitude", 0.1f);
        waterMaterial.SetFloat("_WaveFrequency", 1.0f);
        waterMaterial.SetFloat("_WaveSpeed", 1.0f);
        waterMaterial.SetFloat("_NormalScale", 1.0f);
        waterMaterial.SetColor("_SpecularColor", Color.white);
        waterMaterial.SetFloat("_Shininess", 50f);

        // Assign the material to the plane’s MeshRenderer.
        MeshRenderer renderer = waterPlane.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = waterMaterial;

        // Position the water (here at the origin).
        waterPlane.transform.position = Vector3.zero;

        // Register the creation for undo and select the object in the scene.
        Undo.RegisterCreatedObjectUndo(waterPlane, "Create Stylized Water");
        Selection.activeObject = waterPlane;
    }

    [MenuItem("GameObject/Create Stylized Water with Foam", false, 10)]
    public static void CreateStylizedWaterWithFoam(MenuCommand menuCommand)
    {
        // Create a plane to serve as the water surface.
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "StylizedWaterWithFoam";

        // Locate the URP water with foam shader.
        Shader waterShader = Shader.Find("Custom/URPStylizedWaterWithFoam");
        if (waterShader == null)
        {
            Debug.LogError("Could not find the URPStylizedWaterWithFoam shader. " +
                "Please ensure 'URPStylizedWaterWithFoam.shader' is in your project.");
            return;
        }

        // Create a new material using the water with foam shader.
        Material waterMaterial = new Material(waterShader);

        // Optionally, set a normal map if you have one.
        // Texture2D normalMap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/WaterNormal.png");
        // if (normalMap != null)
        //     waterMaterial.SetTexture("_NormalMap", normalMap);

        // Set default shader properties.
        waterMaterial.SetColor("_WaterColor", new Color(0.1f, 0.3f, 0.6f, 1f));
        waterMaterial.SetColor("_DeepColor", new Color(0.0f, 0.1f, 0.3f, 1f));
        waterMaterial.SetFloat("_WaveAmplitude", 0.1f);
        waterMaterial.SetFloat("_WaveFrequency", 1.0f);
        waterMaterial.SetFloat("_WaveSpeed", 1.0f);
        waterMaterial.SetFloat("_NormalScale", 1.0f);
        waterMaterial.SetColor("_SpecularColor", Color.white);
        waterMaterial.SetFloat("_Shininess", 50f);

        // Set foam-related shader properties.
        waterMaterial.SetFloat("_CoastLevel", 0.0f);
        waterMaterial.SetFloat("_FoamScale", 3.0f);
        waterMaterial.SetFloat("_FoamSpeed", 0.5f);
        waterMaterial.SetFloat("_FoamIntensity", 5.0f);

        // Optionally, set a foam texture.
        // Texture2D foamTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/FoamTexture.png");
        // if (foamTexture != null)
        //     waterMaterial.SetTexture("_FoamTex", foamTexture);

        // Assign the material to the plane’s MeshRenderer.
        MeshRenderer renderer = waterPlane.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = waterMaterial;

        // Position the water (here at the origin).
        waterPlane.transform.position = Vector3.zero;

        // Register the creation for undo and select the object in the scene.
        Undo.RegisterCreatedObjectUndo(waterPlane, "Create Stylized Water with Foam");
        Selection.activeObject = waterPlane;
    }
}