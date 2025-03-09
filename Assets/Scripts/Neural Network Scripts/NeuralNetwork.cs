using UnityEngine;
using System.Collections;

public class NeuralNetwork
{
    public NetworkLayer[] layers;

    //The full network is composed of network layers. It is one less layer than layer sizes passed to it as
    //the input layer need not have its own object as it has no weights or biases.
    public NeuralNetwork(params int[] layerSizes)
    {
        layers = new NetworkLayer[layerSizes.Length - 1];
        
        //Go through the list of layers and initialise each one
        for (int i = 0; i < layers.Length; i++) {
            //The number of outputs of a layer is the next layer's number of inputs
            layers[i] = new NetworkLayer(layerSizes[i], layerSizes[i+1]);
        }
    }

    //We have the layers all defined and the weights and bias arrays correctly sized.
    //Given no save state we request the layers randomisze their values but given a brain we can copy
    //over the values from a saved car brain
    public void SetWeightsAndBiases(bool customBrain = false, float[][,] customWeights = null, 
                                                                            float[][] customBiases = null)
    {
        for (int i = 0; i < layers.Length; i++) {
            if (customBrain) {
                layers[i].SetCustomWeightsAndBiases(customWeights[i], customBiases[i]);
            } else {
                layers[i].SetRandomWeightsAndBiases();
            }
        } 
    }

    //We need to be able to pass data into the network and have it propagate through each layer
    public float[] ForwardPass(float[] inputs)
    {
        foreach (NetworkLayer layer in layers) {
            //Pass in the inputs and then reassign it as the output of the layer to feed to the next one
            inputs = layer.CalculateOutput(inputs);
        }

        return inputs;
    }

    //Mutate the brain by adding random noise to its weights
    //The deviation denotes how far away from the given weights we should stray
    public void Mutate(float deviation)
    {
        //Mutate the weights and biases on each layer
        foreach (NetworkLayer layer in layers) {
            layer.MutateWeightsAndBiases(deviation);
        }
    }
}
