using UnityEngine;

public static class ProceduralTerrainGeneration
{
    /// <summary>
    /// Generates or updates a Terrain using fractal Brownian motion noise.
    /// </summary>
    /// <param name="terrainWidth">The terrain width in world units.</param>
    /// <param name="terrainLength">The terrain length in world units.</param>
    /// <param name="terrainHeight">The terrain maximum height in world units.</param>
    /// <param name="scale">Scale of the noise.</param>
    /// <param name="octaves">Number of noise octaves.</param>
    /// <param name="lacunarity">Frequency multiplier for each octave.</param>
    /// <param name="gain">Amplitude multiplier for each octave.</param>
    /// <param name="offset">Noise offset.</param>
    /// <param name="mountainThreshold">Threshold between plains and mountains (normalized 0–1).</param>
    /// <param name="blendStrength">Blend strength for mountain transition.</param>
    /// <param name="plainHeightMultiplier">Multiplier for plain height.</param>
    /// <param name="targetTerrain">Optional: an existing Terrain to update. If null, a new one will be created.</param>
    /// <returns>The generated (or updated) Terrain.</returns>
    public static Terrain GenerateTerrain(
        int terrainWidth,
        int terrainLength,
        int terrainHeight,
        float scale,
        int octaves,
        float lacunarity,
        float gain,
        Vector2 offset,
        float mountainThreshold,
        float blendStrength,
        float plainHeightMultiplier,
        Terrain targetTerrain = null)
    {
        // If no terrain is assigned, create a new Terrain GameObject.
        if (targetTerrain == null)
        {
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = terrainWidth + 1;
            terrainData.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

            GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
            targetTerrain = terrainGO.GetComponent<Terrain>();
        }

        // Get the TerrainData for modification.
        TerrainData td = targetTerrain.terrainData;
        td.heightmapResolution = terrainWidth + 1;
        td.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        int resolution = td.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        // Generate the heightmap using fBm noise.
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float xCoord = ((float)x / (resolution - 1)) * scale + offset.x;
                float zCoord = ((float)z / (resolution - 1)) * scale + offset.y;

                // Get the fBm noise value.
                float noiseValue = FBM(xCoord, zCoord, octaves, lacunarity, gain);

                // Remap from [-1, 1] to [0, 1].
                noiseValue = Mathf.Clamp01((noiseValue + 1f) / 2f);

                // Apply plains and mountains logic.
                float finalHeight;
                if (noiseValue < mountainThreshold)
                {
                    // For plains, reduce the height.
                    float blendFactor = Mathf.SmoothStep(0, 1, noiseValue / mountainThreshold);
                    finalHeight = blendFactor * plainHeightMultiplier;
                }
                else
                {
                    // For mountains, smoothly transition to full height.
                    float blendFactor = Mathf.SmoothStep(0, 1, (noiseValue - mountainThreshold) * blendStrength);
                    finalHeight = Mathf.Lerp(plainHeightMultiplier, noiseValue, blendFactor);
                }
                heights[z, x] = finalHeight;
            }
        }

        // Apply the generated heightmap to the terrain.
        td.SetHeights(0, 0, heights);

        return targetTerrain;
    }

    /// <summary>
    /// Computes fractal Brownian motion noise for a given coordinate.
    /// </summary>
    private static float FBM(float x, float y, int octaves, float lacunarity, float gain)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float noiseHeight = 0f;
        float maxAmplitude = 0f;

        for (int i = 0; i < octaves; i++)
        {
            // Mathf.PerlinNoise returns values in [0, 1]. Map to [-1, 1].
            float perlinValue = Mathf.PerlinNoise(x * frequency, y * frequency) * 2f - 1f;
            noiseHeight += perlinValue * amplitude;
            maxAmplitude += amplitude;

            amplitude *= gain;
            frequency *= lacunarity;
        }

        return noiseHeight / maxAmplitude;
    }
}