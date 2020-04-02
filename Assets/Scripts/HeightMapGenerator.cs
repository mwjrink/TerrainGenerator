using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int size, HeightMapSettings settings, Erosion erosion, Vector2 sampleCenter)
    {
        var values = Noise.GenerateNoiseMap(size, size, settings.noiseSettings, sampleCenter);

        var threadLocalHeightCurve = new AnimationCurve(settings.heightCurve.keys);

        var minValue = System.Single.MaxValue;
        var maxValue = System.Single.MinValue;

        for (var j = 0; j < size; j++)
            for (var i = 0; i < size; i++)
            {
                values[i, j] = threadLocalHeightCurve.Evaluate(values[i, j]) * settings.heightScale;

                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }

        if (settings.erode)
        {
            erosion.Erode(ref values, size, settings.noiseSettings.seed);
        }

        return new HeightMap(values, minValue, maxValue);
    }

    public static HeightMap GenerateWorldLocalHeightMap(int size, WorldSettings settings, Erosion erosion, Vector2 sampleCenter)
    {
        var values = Noise.SampleWorldMap(size, sampleCenter, settings.seed);
        //var falloff = FalloffGenerator.GenerateFalloffChunkMap(settings.radius, size, sampleCenter);

        var threadLocalHeightCurve = new AnimationCurve(settings.heightCurve.keys);

        var minValue = System.Single.MaxValue;
        var maxValue = System.Single.MinValue;

        for (var j = 0; j < size; j++)
            for (var i = 0; i < size; i++)
            {
                values[i, j] = threadLocalHeightCurve.Evaluate(values[i, j]) * settings.heightScale; // + falloff[i,j] * settings.heightScale

                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }

        //if (settings.erode)
        //{
        //    erosion.Erode(ref values, size, settings.noiseSettings.seed);
        //}

        return new HeightMap(values, minValue, maxValue);
    }

    public static Curvature GenerateWorldCurvatureMap(int chunkSize, WorldSettings settings)
    {
        var curvatureMap = new float[chunkSize, chunkSize];

        for (var j = 0; j < chunkSize; j++)
            for (var i = 0; i < chunkSize; i++)
            {
                curvatureMap[i,j] = Mathf.Sqrt((-i * i) - (j * j) + (settings.radius * settings.radius)) - settings.radius;
            }

        return new Curvature
        {
            curvatureMap = curvatureMap,
            radius = settings.radius
        };
    }
}

public struct Curvature
{
    public float[,] curvatureMap;
    public float radius;
}

public struct HeightMap
{
    public float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
