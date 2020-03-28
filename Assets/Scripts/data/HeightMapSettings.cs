using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;

    public float heightScale;
    public AnimationCurve heightCurve;

    public bool useFalloff;
    public bool erode;

    public float minHeight => heightScale * heightCurve.Evaluate(0);
    public float maxHeight => heightScale * heightCurve.Evaluate(1);

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateFields();
        base.OnValidate();
    }

#endif
}
