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

    // TODO: @Max, this is slow, but it is only run once so meh?
    static float Evaluate(float value)
    {
        float a = 3.0f;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
