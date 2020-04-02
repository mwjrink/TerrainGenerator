using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size)
    {
        var map = new float[size, size];

        for (var j = 0; j < size; j++)
            for (var i = 0; i < size; i++)
            {
                var x = (i / (float)size * 2) - 1;
                var y = (j / (float)size * 2) - 1;

                map[i, j] = Evaluate(Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)));
            }

        return map;
    }

    public static float[,] GenerateFalloffChunkMap(int mapSize, int chunkSize, Vector2 sampleCenter)
    {
        float dist = mapSize * 0.1f;
        var map = new float[chunkSize, chunkSize];

        for (var j = 0; j < chunkSize; j++)
            for (var i = 0; i < chunkSize; i++)
            {
                var x = i + sampleCenter.x - (chunkSize / 2);
                var y = j + sampleCenter.y - (chunkSize / 2);

                map[i, j] = 1;

                if (x + dist > mapSize)
                {
                    map[i, j] *= (mapSize - x) / dist;
                }

                if (y + dist > mapSize)
                {
                    map[i, j] *= (mapSize - y) / dist;
                }

                // map[i, j] = Evaluate(Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)));
            }

        return map;
    }

    // TODO: @Max, this is slow, but it is only run once so meh?
    static float Evaluate(float value)
    {
        float a = 3.0f;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
