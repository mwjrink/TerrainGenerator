﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        var width = heightMap.values.GetLength(0);
        var height = heightMap.values.GetLength(1);

        var colorMap = new Color[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                colorMap[x + (y * width)] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]));
            }

        return TextureFromColorMap(colorMap, width, height);
    }
}
