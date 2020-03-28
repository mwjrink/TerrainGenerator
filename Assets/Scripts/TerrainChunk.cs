using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistanceThreshold = 5;
    public event Action<TerrainChunk, bool> onVisibilityChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCenter;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    HeightMap heightMap;
    bool heightMapReceived;
    int previousLodIndex = -1;

    bool hasSetCollider;
    float maxViewDst;

    HeightMapSettings heightMapSettings;
    MeshSettings meshSettings;
    Transform viewer;

    Vector2 viewerPosition => new Vector2(viewer.position.x, viewer.position.z);

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.viewer = viewer;
        this.heightMapSettings = heightMapSettings;
        this.meshSettings = meshSettings;
        this.coord = coord;
        this.colliderLODIndex = colliderLODIndex;
        this.detailLevels = detailLevels;

        sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
        var position = coord * meshSettings.MeshWorldSize;
        bounds = new Bounds(sampleCenter, Vector2.one * meshSettings.MeshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        //meshObject.isStatic = true;
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();

        meshObject.transform.localScale = Vector3.one * meshSettings.meshScale;
        
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (var i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

    }

    public void Load(Erosion erosion)
    {
        Debug.Log("Height map requested for coord: " + coord.x + ", " + coord.y);
        ThreadedDataRequester.RequestData(
            () => HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, heightMapSettings, erosion, sampleCenter),
            OnHeightMapReceived
            );
    }

    void OnHeightMapReceived(object heightMapObject)
    {
        Debug.Log("Height map received for coord: " + coord.x + ", " + coord.y);
        //mapGenerator.RequestMeshData(heightMap, OnMeshDataReceived);
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived) return;

        var wasVisible = isVisible();
        var viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
        var visible = viewerDistanceFromNearestEdge <= maxViewDst;

        if (visible)
        {
            var lodIndex = 0;

            for (var i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            if (lodIndex != previousLodIndex)
            {
                var lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh)
                {
                    meshFilter.mesh = lodMesh.mesh;
                    previousLodIndex = lodIndex;
                    meshCollider.sharedMesh = lodMesh.mesh;
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(heightMap, meshSettings);
                }
            }
        }

        if (wasVisible != visible)
        {
            SetVisible(visible);
            if (onVisibilityChanged != null)
            {
                onVisibilityChanged.Invoke(this, visible);
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            var sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasRequestedMesh)
                {
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
                    hasSetCollider = true;
                }
            }

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool isVisible()
    {
        return meshObject.activeSelf;
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        readonly int lod;

        public event Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataReceived(object meshDataObject)
        {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings settings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, settings, lod), OnMeshDataReceived);
        }
    }
}
