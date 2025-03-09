using UnityEngine;
// using UnityEngine.JSONSerializeModule;

[System.Serializable]
public class BrainSaveState
{
    public int[] layerSizes;
    public float[] weights;
    public float[] biases;
}
