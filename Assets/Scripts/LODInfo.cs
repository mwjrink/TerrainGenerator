using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numberSupportedLODs - 1)]
    public int lod;
    public float visibleDistanceThreshold;

    public float sqrVisibleDistanceThreshold => visibleDistanceThreshold * visibleDistanceThreshold;
}
