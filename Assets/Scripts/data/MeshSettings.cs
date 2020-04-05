using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public const int numberSupportedLODs = 5;
    public const int numberSupportedChunkSizes = 11;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240, 480, 960 };

    public float meshScale = 2.5f;

    [Range(0, numberSupportedChunkSizes - 1)]
    public int chunkSizeIndex;

    // num vertices per line of mesh rendered at LOD = 0. in addition to the bordered vertices
    public int numberOfVerticesPerLine => supportedChunkSizes[chunkSizeIndex] + 5;

    public float MeshWorldSize => (numberOfVerticesPerLine - 3) * meshScale;
}
