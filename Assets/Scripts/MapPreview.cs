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
        FalloffMap,
        NewMapGen,
        DomainWarping,
        DomainWarpedNewMap,
        DomainWarpedAndMeshedNewMap
    }

    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public Erosion erosion;

    public Material terrainMaterial;
    public Material MapMaterial;

    [Range(0, MeshSettings.numberSupportedLODs - 1)]
    public int editorPreviewLevelOfDetail;

    public bool autoUpdate;

    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Range(1, 9)]
    public int numberOfBiomes;
    [Range(0, 100000)]
    public int warpingAmplitude = 80;
    [Range(0.0f, 0.99f)]
    public float edgeCuttoffPercent;
    [Range(0.0f, 10000.0f)]
    public float adjustmentFactor;
    [Range(0.0f, 10000.0f)]
    public float noiseScale;
    [Range(0.0f, 10000.0f)]
    public float heightScale;
    public bool useGrid;

    void DebugWriteFile(string name, float[,] data, int size)
    {
        using (var file = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/" + name + ".txt", true))
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

        if (drawMode == DrawMode.NoiseMap)
        {
            var heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, heightMapSettings, erosion, Vector3.zero);
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            var heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, heightMapSettings, erosion, Vector3.zero);
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLevelOfDetail));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numberOfVerticesPerLine), 0, 1)));
        }
        else if (drawMode == DrawMode.NewMapGen)
        {
            var map = TestGenerator.GenerateVoronoiMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, numberOfBiomes, heightMapSettings.noiseSettings.seed, edgeCuttoffPercent, useGrid);
            var color = new Color[meshSettings.numberOfVerticesPerLine * meshSettings.numberOfVerticesPerLine];

            for (var i = 0; i < meshSettings.numberOfVerticesPerLine * meshSettings.numberOfVerticesPerLine; i++)
            {
                color[i] = map[i % meshSettings.numberOfVerticesPerLine, i / meshSettings.numberOfVerticesPerLine].biome.color;
            }

            DrawTexture(TextureGenerator.TextureFromColorMap(color, meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine));
        }
        else if (drawMode == DrawMode.DomainWarping)
        {
            DrawTexture(
                TextureGenerator.TextureFromColorMap(
                    TestGenerator.DomainWarpMap(
                        meshSettings.numberOfVerticesPerLine,
                        meshSettings.numberOfVerticesPerLine,
                        heightMapSettings.noiseSettings.seed
                        ),
                    meshSettings.numberOfVerticesPerLine,
                    meshSettings.numberOfVerticesPerLine)
                );
        }
        else if (drawMode == DrawMode.DomainWarpedNewMap)
        {
            var map = TestGenerator.GenerateVoronoiMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, numberOfBiomes, heightMapSettings.noiseSettings.seed, edgeCuttoffPercent, useGrid);
            var warpedMap = TestGenerator.DomainWarpMap(map, meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings.noiseSettings.seed, map[0, 0], warpingAmplitude * meshSettings.numberOfVerticesPerLine / 240f, adjustmentFactor, meshSettings.numberOfVerticesPerLine * 0.1f);
            var warpedSize = Mathf.RoundToInt(meshSettings.numberOfVerticesPerLine * adjustmentFactor);
            var color = new Color[warpedSize * warpedSize];

            for (var i = 0; i < color.Length; i++)
            {
                var x = i % warpedSize;
                var y = i / warpedSize;
                color[i] = warpedMap[x, y].biome.color;
            }

            DrawTexture(TextureGenerator.TextureFromColorMap(color, warpedSize, warpedSize));
        }
        else if (drawMode == DrawMode.DomainWarpedAndMeshedNewMap)
        {
            var map = TestGenerator.GenerateVoronoiMap(meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, numberOfBiomes, heightMapSettings.noiseSettings.seed, edgeCuttoffPercent, useGrid);
            var warpedMap = TestGenerator.DomainWarpMap(map, meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine, heightMapSettings.noiseSettings.seed, map[0, 0], warpingAmplitude * meshSettings.numberOfVerticesPerLine / 240f, adjustmentFactor, meshSettings.numberOfVerticesPerLine * 0.1f);
            var warpedSize = Mathf.RoundToInt(meshSettings.numberOfVerticesPerLine * adjustmentFactor);

            var heightMap = TestGenerator.TestFillHeightMap(warpedMap, warpedSize, warpedSize, heightMapSettings.noiseSettings.seed, noiseScale, heightScale);
            var color = new Color[warpedSize * warpedSize];

            for (var i = 0; i < color.Length; i++)
            {
                var x = i % warpedSize;
                var y = i / warpedSize;
                color[i] = warpedMap[x, y].biome.color;
            }

            DebugWriteFile("warpedHeightMap", heightMap, warpedSize);

            meshSettings.numberOfVerticesPerLine = warpedSize;
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap, meshSettings, editorPreviewLevelOfDetail), TextureGenerator.TextureFromColorMap(color, warpedSize, warpedSize));
        }
    }

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10.0f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture = null)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);

        if (texture)
        {
            meshFilter.gameObject.GetComponent<MeshRenderer>().sharedMaterial = MapMaterial;
            textureRenderer.sharedMaterial.mainTexture = texture;
        }
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
