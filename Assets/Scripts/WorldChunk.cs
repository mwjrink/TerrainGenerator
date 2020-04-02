using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WorldChunk
{
    const float colliderGenerationDistanceThreshold = 5;
    public event Action<WorldChunk, bool> onVisibilityChanged;
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

    Curvature curvature;

    Vector2 viewerPosition => new Vector2(viewer.position.x, viewer.position.z);

    public WorldChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material, WorldSettings worldSettings)
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

        meshObject = new GameObject("World Chunk");
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
            lodMeshes[i].updateCallback += UpdateWorldChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        curvature = HeightMapGenerator.GenerateWorldCurvatureMap(meshSettings.numberOfVerticesPerLine, worldSettings);
    }

    public void Load(Erosion erosion, WorldSettings worldSettings)
    {
        // Debug.Log("Height map requested for coord: " + coord.x + ", " + coord.y);
        ThreadedDataRequester.RequestData(() =>
        {
            var realHeightMap = new float[meshSettings.numberOfVerticesPerLine, meshSettings.numberOfVerticesPerLine];

            var heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numberOfVerticesPerLine, heightMapSettings, erosion, sampleCenter);
            var worldLocalHeightMap = HeightMapGenerator.GenerateWorldLocalHeightMap(meshSettings.numberOfVerticesPerLine, worldSettings, erosion, sampleCenter);

            for (var y = 0; y < meshSettings.numberOfVerticesPerLine; y++)
                for (var x = 0; x < meshSettings.numberOfVerticesPerLine; x++)
                {
                    realHeightMap[x, y] = heightMap.values[x, y] * (worldLocalHeightMap.values[x, y] / worldSettings.heightScale) * 1 + worldLocalHeightMap.values[x, y];
                }

            return new HeightMap(realHeightMap, Single.MinValue, Single.MaxValue);
        }, OnHeightMapReceived);
    }

    void OnHeightMapReceived(dynamic heightMap)
    {
        this.heightMap = heightMap;
        heightMapReceived = true;

        UpdateWorldChunk();
    }

    public void UpdateWorldChunk()
    {
        try
        {
            Debug.Log("1");
            if (!heightMapReceived) return;

            Debug.Log("2");
            var wasVisible = isVisible();

            Debug.Log("3");
            var viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

            Debug.Log("4");
            var visible = viewerDistanceFromNearestEdge <= maxViewDst;

            Debug.Log(visible);

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
                        Debug.Log("Mesh Requested for coord: " + coord.x + ", " + coord.y);
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
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
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
