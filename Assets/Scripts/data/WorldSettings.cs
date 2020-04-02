using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WorldSettings : UpdatableData
{
    public int seed;

    public int radius;
    public float heightScale;
    public AnimationCurve heightCurve;

    public bool useFalloff;
    public bool erode;

    //public float minHeight => heightScale * heightCurve.Evaluate(0);
    //public float maxHeight => heightScale * heightCurve.Evaluate(1);
}
