using UnityEngine;

public enum GeneEffectType
{
    None,
    Scale,
    Lifetime,
    DivisionInterval
}

[System.Serializable]
public class MIC_Gene : MonoBehaviour
{
    public string geneName;

    public Material material;

    public GeneEffectType effectType;

    [Header("Effets fixes")]
    public float scaleDelta;
    public float lifetimeDelta;
    public float divisionIntervalDelta;
}
