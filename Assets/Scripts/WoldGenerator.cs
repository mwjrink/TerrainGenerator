using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoldGenerator : MonoBehaviour
{
    const float viewerMoveThresholdForChunkUpdate = 25.0f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public Transform viewer;
    public Material mapMaterial;
    public Erosion erosion;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;
    public WorldSettings worldSettings;

    Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    float meshWorldSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, WorldChunk> worldChunkDict = new Dictionary<Vector2, WorldChunk>();
    List<WorldChunk> visibleWorldChunks = new List<WorldChunk>();

    void Start()
    {
        //textureSettings.ApplyToMaterial(mapMaterial);
        //textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        meshWorldSize = meshSettings.MeshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDist / meshWorldSize);

        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosition != viewerPositionOld)
        {
            foreach (var chunk in visibleWorldChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            UpdateVisibleChunks();
            viewerPositionOld = viewerPosition;
        }
    }

    void UpdateVisibleChunks()
    {
        var alreadyUpdatedChunkCoords = new List<Vector2>();
        for (var i = visibleWorldChunks.Count - 1; i >= 0; i--)
        {
            visibleWorldChunks[i].UpdateWorldChunk();
            alreadyUpdatedChunkCoords.Add(visibleWorldChunks[i].coord);
        }

        var currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        var currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

        for (var yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            for (var xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    alreadyUpdatedChunkCoords.Add(viewedChunkCoord);
                    if (worldChunkDict.TryGetValue(viewedChunkCoord, out var worldChunk))
                    {
                        worldChunk.UpdateWorldChunk();
                    }
                    else
                    {
                        var newChunk = new WorldChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial, worldSettings);
                        worldChunkDict.Add(viewedChunkCoord, newChunk);

                        newChunk.onVisibilityChanged += OnWorldChunkVisibilityChanged;
                        newChunk.Load(erosion, worldSettings);
                    }
                }
            }
    }

    void OnWorldChunkVisibilityChanged(WorldChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            visibleWorldChunks.Add(chunk);
        }
        else
        {
            visibleWorldChunks.Remove(chunk);
        }
    }
}
