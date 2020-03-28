using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }
    //private static Perlin perlin;

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
    {
        var noiseMap = new float[mapWidth, mapHeight];
        //perlin = new Perlin(seed);

        var prng = new System.Random(settings.seed);
        var octaveOffsets = new Vector2[settings.octaves];

        var maxPossibleHeight = 0.0f;
        var amplitude = 1.0f;
        var frequency = 1.0f;

        for (var o = 0; o < settings.octaves; o++)
        {
            var offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            var offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;

            octaveOffsets[o] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistence;
        }

        if (settings.scale <= 0)
            settings.scale = 0.0001f;

        var maxLocalNoiseHeight = System.Single.MinValue;
        var minLocalNoiseHeight = System.Single.MaxValue;

        var halfWidth = mapWidth / 2.0f;
        var halfHeight = mapHeight / 2.0f;

        for (var y = 0; y < mapHeight; y++)
            for (var x = 0; x < mapHeight; x++)
            {
                amplitude = 1.0f;
                frequency = 1.0f;
                var noiseHeight = 0.0f;

                for (var o = 0; o < settings.octaves; o++)
                {
                    var sampleX = (x - halfWidth + octaveOffsets[o].x) / settings.scale * frequency;
                    var sampleY = (y - halfHeight + octaveOffsets[o].y) / settings.scale * frequency;

                    var perlinValue = (Mathf.PerlinNoise(sampleX, sampleY) * 2) - 1; //perlin.OctavePerlin(sampleX, sampleY, noiseScale, octaves, persistence, lacunarity) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistence;
                    frequency *= settings.lacunarity;
                }

                maxLocalNoiseHeight = Mathf.Max(noiseHeight, maxLocalNoiseHeight);
                minLocalNoiseHeight = Mathf.Min(noiseHeight, minLocalNoiseHeight);

                noiseMap[x, y] = noiseHeight;

                if (settings.normalizeMode == NormalizeMode.Global)
                {
                    // / 2.0f is an estimation because almost never will the maxPossibleHeight ever be reached
                    // TODO: @Max, something better here (this is true, it should almost never be reached, but then you have everest so
                    noiseMap[x, y] = Mathf.Clamp((noiseMap[x, y] + 1) / (maxPossibleHeight), Int32.MinValue, Int32.MaxValue); // / (2.0f * maxPossibleHeight / 2.0f)
                }
            }

        if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (var y = 0; y < mapHeight; y++)
                for (var x = 0; x < mapHeight; x++)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
        }

        return noiseMap;
    }
}

[Serializable]
public class NoiseSettings
{
    [Range(0, 1)]
    public float persistence = 0.6f;
    public int octaves = 6;
    public float lacunarity = 2f;
    public Vector2 offset; public int seed;
    public float scale = 50;
    public Noise.NormalizeMode normalizeMode;

    public void ValidateFields()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistence = Mathf.Clamp(persistence, 0, 1);
    }
}