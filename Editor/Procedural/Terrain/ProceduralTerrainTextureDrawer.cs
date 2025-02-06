using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TextureGroupElement
{
    public string elementName;
    public Texture2D diffuseTexture;
    public Texture2D normalMap;
    public float strength = 1f;       // Now drawn as a slider between 0 and 1 in the UI.
    public float noiseScale = 10f;
    public Vector2 noiseOffset = Vector2.zero;
}

/// <summary>
/// This class paints the terrain using height data and blends textures in each group (Sand, Grass, Rock, Snow)
/// by using each texture’s strength and a Perlin noise sample to distribute the group’s weight.
/// </summary>
public class ProceduralTerrainTextureDrawer
{
    // Height/blending parameters.
    private float waterLevel;
    private float mountainThreshold;
    private float waterBlendRange;
    private float mountainBlendRange;
    private float snowBlendRange;
    private int terrainHeight;

    // A dictionary mapping group names to lists of TextureGroupElement.
    private Dictionary<string, List<TextureGroupElement>> textureGroups;

    public ProceduralTerrainTextureDrawer(
        float waterLevel,
        float mountainThreshold,
        float waterBlendRange,
        float mountainBlendRange,
        float snowBlendRange,
        int terrainHeight,
        Dictionary<string, List<TextureGroupElement>> textureGroups)
    {
        this.waterLevel = waterLevel;
        this.mountainThreshold = mountainThreshold;
        this.waterBlendRange = waterBlendRange;
        this.mountainBlendRange = mountainBlendRange;
        this.snowBlendRange = snowBlendRange;
        this.terrainHeight = terrainHeight;
        this.textureGroups = textureGroups;
    }

    /// <summary>
    /// Paints the terrain by computing a base weight per terrain type from the height data,
    /// then distributes that weight among the textures in the corresponding group based on
    /// each texture's strength and Perlin noise.
    /// </summary>
    public void PaintTerrain(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        List<TerrainLayer> allLayers = new List<TerrainLayer>();

        // We expect textureGroups to contain keys: "Sand", "Grass", "Rock", "Snow"
        string[] groups = new string[] { "Sand", "Grass", "Rock", "Snow" };

        // Build a mapping from group name to the indices in the allLayers list.
        Dictionary<string, List<int>> groupLayerIndices = new Dictionary<string, List<int>>();

        foreach (string group in groups)
        {
            groupLayerIndices[group] = new List<int>();

            if (!textureGroups.ContainsKey(group) || textureGroups[group].Count == 0)
            {
                Debug.LogWarning("No textures defined for group: " + group);
                continue;
            }

            // Create a TerrainLayer for each texture in the group.
            foreach (TextureGroupElement element in textureGroups[group])
            {
                TerrainLayer layer = CreateTerrainLayer(group + "_" + element.elementName, element.diffuseTexture, element.normalMap);
                allLayers.Add(layer);
                groupLayerIndices[group].Add(allLayers.Count - 1);
            }
        }

        // Assign the terrain layers.
        terrainData.terrainLayers = allLayers.ToArray();

        int alphaMapWidth = terrainData.alphamapWidth;
        int alphaMapHeight = terrainData.alphamapHeight;
        int totalLayers = allLayers.Count;
        float[,,] alphaMaps = new float[alphaMapHeight, alphaMapWidth, totalLayers];

        // Loop over every point in the alphamap.
        for (int y = 0; y < alphaMapHeight; y++)
        {
            for (int x = 0; x < alphaMapWidth; x++)
            {
                // Convert splatmap coordinates into normalized terrain coordinates.
                float normX = (float)x / (alphaMapWidth - 1);
                float normY = (float)y / (alphaMapHeight - 1);

                // Get the normalized height at this point.
                float height = terrainData.GetInterpolatedHeight(normX, normY) / terrainData.size.y;

                // Compute base weights for the primary terrain types (Sand, Grass, Rock, Snow)
                // based on your height & blend settings.
                float[] baseWeights = new float[4];
                if (height < waterLevel - waterBlendRange)
                {
                    baseWeights[0] = 1f; // Pure Sand
                }
                else if (height < waterLevel + waterBlendRange)
                {
                    float t = Mathf.InverseLerp(waterLevel + waterBlendRange, waterLevel - waterBlendRange, height);
                    baseWeights[0] = 1f - t;
                    baseWeights[1] = t;
                }
                else if (height < mountainThreshold - mountainBlendRange)
                {
                    baseWeights[1] = 1f; // Pure Grass
                }
                else if (height < mountainThreshold + mountainBlendRange)
                {
                    float t = Mathf.InverseLerp(mountainThreshold - mountainBlendRange, mountainThreshold + mountainBlendRange, height);
                    baseWeights[1] = 1f - t;
                    baseWeights[2] = t;
                }
                else if (height < 1f - snowBlendRange)
                {
                    baseWeights[2] = 1f; // Pure Rock
                }
                else if (height < 1f)
                {
                    float t = Mathf.InverseLerp(1f - snowBlendRange, 1f, height);
                    baseWeights[2] = 1f - t;
                    baseWeights[3] = t;
                }
                else
                {
                    baseWeights[3] = 1f; // Pure Snow
                }

                // Prepare final weights for all terrain layers.
                float[] finalWeights = new float[totalLayers];

                // For each primary group, distribute the group base weight among its textures.
                for (int g = 0; g < groups.Length; g++)
                {
                    string group = groups[g];
                    float groupWeight = baseWeights[g];

                    // Get the indices for this group.
                    List<int> indices = groupLayerIndices[group];
                    if (indices == null || indices.Count == 0)
                        continue;

                    // If only one texture is defined, assign the entire group weight.
                    if (indices.Count == 1)
                    {
                        finalWeights[indices[0]] = groupWeight;
                    }
                    else
                    {
                        float[] factors = new float[indices.Count];
                        float sumFactors = 0f;

                        float exponent = 2.0f;  // user setting for contrast
                        float threshold = 0.2f; // user setting for "ignore weaker factors"

                        List<TextureGroupElement> elements = textureGroups[group];
                        for (int i = 0; i < indices.Count; i++)
                        {
                            TextureGroupElement element = elements[i];
                            
                            float rawNoise = Mathf.PerlinNoise(
                                normX * element.noiseScale + element.noiseOffset.x,
                                normY * element.noiseScale + element.noiseOffset.y
                            );

                            // raise noise for more contrast
                            float contrastNoise = Mathf.Pow(rawNoise, exponent);

                            // multiply by the texture's strength
                            factors[i] = contrastNoise * element.strength;
                        }

                        // 1) Find the largest factor
                        float maxFactor = 0f;
                        for (int i = 0; i < factors.Length; i++)
                            if (factors[i] > maxFactor)
                                maxFactor = factors[i];

                        // 2) Discard factors below threshold * maxFactor
                        for (int i = 0; i < factors.Length; i++)
                        {
                            if (factors[i] < threshold * maxFactor)
                            {
                                factors[i] = 0f;
                            }
                        }

                        // 3) Sum again after thresholding
                        sumFactors = 0f;
                        for (int i = 0; i < factors.Length; i++)
                            sumFactors += factors[i];

                        // 4) Fallback if everything is zero
                        if (sumFactors <= 0f)
                        {
                            // Distribute evenly or do something else
                            for (int i = 0; i < factors.Length; i++)
                            {
                                factors[i] = 1f / factors.Length;
                            }
                        }
                        else
                        {
                            // 5) Normalize
                            for (int i = 0; i < factors.Length; i++)
                                factors[i] /= sumFactors;
                        }

                        // 6) Multiply by the group’s baseWeight
                        for (int i = 0; i < factors.Length; i++)
                        {
                            finalWeights[indices[i]] = groupWeight * factors[i];
                        }
                    }
                }

                // (Optional) Normalize finalWeights to ensure the sum is 1 (this can help avoid rounding errors).
                float total = 0f;
                for (int i = 0; i < totalLayers; i++)
                    total += finalWeights[i];
                if (total > 0f)
                {
                    for (int i = 0; i < totalLayers; i++)
                        finalWeights[i] /= total;
                }

                // Set the alpha maps for this point.
                for (int i = 0; i < totalLayers; i++)
                {
                    alphaMaps[y, x, i] = finalWeights[i];
                }
            }
        }

        // Apply the alphamaps to the terrain.
        terrainData.SetAlphamaps(0, 0, alphaMaps);
    }

    /// <summary>
    /// Creates a new TerrainLayer using the given diffuse texture and optional normal map.
    /// </summary>
    private TerrainLayer CreateTerrainLayer(string layerName, Texture2D diffuse, Texture2D normal)
    {
        TerrainLayer layer = new TerrainLayer();
        layer.name = layerName;
        layer.diffuseTexture = diffuse;
        layer.normalMapTexture = normal;
        layer.tileSize = new Vector2(15, 15);
        return layer;
    }
}