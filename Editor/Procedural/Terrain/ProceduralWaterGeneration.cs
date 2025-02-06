using UnityEngine;
using UnityEditor;

public static class ProceduralWaterGeneration
{
    /// <summary>
    /// Instantiates (or repositions) the water prefab at the appropriate level, centered on the terrain.
    /// The prefab should be located in Resources/Level Generator/Terrain/Water.prefab.
    /// </summary>
    /// <param name="terrainWidth">The width of the terrain in world units.</param>
    /// <param name="terrainLength">The length of the terrain in world units.</param>
    /// <param name="terrainHeight">The maximum height of the terrain in world units.</param>
    /// <param name="waterLevel">The normalized water level (0 to 1).</param>
    public static void GenerateWater(int terrainWidth, int terrainLength, int terrainHeight, float waterLevel)
    {
        // Load the water prefab from Resources (do not include the .prefab extension).
        GameObject waterPrefab = Resources.Load<GameObject>("Level Generator/Terrain/Water");
        if (waterPrefab == null)
        {
            Debug.LogError("Water prefab not found at Resources/Level Generator/Terrain/Water");
            return;
        }

        // Calculate the world-space water height.
        float waterY = waterLevel * terrainHeight;

        // Center the water on the terrain.
        // Assuming the terrain is at some position other than (0, 0, 0)
        Vector3 terrainPosition = Terrain.activeTerrain.transform.position;

        // Center the water on the terrain, considering its position
        float centerX = terrainPosition.x + terrainWidth;
        float centerZ = terrainPosition.z + terrainLength / 2f;

        Vector3 waterPosition = new Vector3(centerX, waterY, centerZ);

        // Look for an existing water instance by name.
        GameObject existingWater = GameObject.Find(waterPrefab.name);
        if (existingWater == null)
        {
            // Instantiate a new water prefab if one doesn't exist.
            GameObject waterInstance = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab);
            waterInstance.transform.position = waterPosition;
            waterInstance.name = waterPrefab.name;
        }
        else
        {
            // Reposition the existing water instance.
            existingWater.transform.position = waterPosition;
        }
    }
}