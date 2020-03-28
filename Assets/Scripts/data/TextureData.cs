using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(layer => layer.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(layer => layer.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(layer => layer.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrengths", layers.Select(layer => layer.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(layer => layer.textureScale).ToArray());
        var texturesArray = GenerateTextureArray(layers.Select(layer => layer.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMaxHeight = maxHeight;
        savedMaxHeight = minHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        var textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for(var i = 0; i< textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0,1)]
        public float tintStrength;
        [Range(0,1)]
        public float startHeight;
        [Range(0,1)]
        public float blendStrength;
        public float textureScale;
    }
}
