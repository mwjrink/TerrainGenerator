using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        Mesh,
        FalloffMap
    }

    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Erosion erosion;

    public Material terrainMaterial;

    [Range(0, MeshSettings.numberSupportedLODs - 1)]
    public int editorPreviewLevelOfDetail;

    public bool autoUpdate;

    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    void DebugWriteFile(string name, float[,] data, int size)
    {
        using (var file = new StreamWriter(@"C:\Users\Max\Desktop\" + name + ".txt", true))
        {
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    file.Write(data[x, y] + ",");
                }
                file.WriteLine();
            }
        }
    }

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        var heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, heightMapSettings, erosion, Vector3.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLevelOfDetail));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numberOfVerticesPerLine), 0, 1)));
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10.0f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

}
