using UnityEngine;
using System.Collections;

public class NetworkLayer
{
    int numInputNodes;
    int numOutputNodes;

    public float[,] weights;
    public float[] biases;

    public NetworkLayer(int numInputNodes, int numOutputNodes) 
    {
        this.numInputNodes = numInputNodes;
        this.numOutputNodes = numOutputNodes;

        this.weights = new float[numInputNodes, numOutputNodes];
        this.biases = new float[numOutputNodes];
    }

    //With the sizes of the weights and biases established, we can now assign values
    //this function does so randomly
    public void SetRandomWeightsAndBiases() 
    {
        //Randomise the weights and biases
        for (int i = 0; i < numOutputNodes; i++) {
            for (int j = 0; j < numInputNodes; j++) {
                weights[j, i] = Random.Range(-1.0f, 1.0f);
            }
            biases[i] = Random.Range(-1.0f, 1.0f);
        }
    }

    //this function applies custom (given) weights and biases
    public void SetCustomWeightsAndBiases(float[,] customWeights, float[] customBiases)
    {
        for (int i = 0; i < numOutputNodes; i++) {
            for (int j = 0; j < numInputNodes; j++) {
                weights[j, i] = customWeights[j, i];
            }
            biases[i] = customBiases[i];
        }
    }

    //Here we alter each weight and bias be a random value that is "close" to the current one.
    //deviation tells us how far to stray from the current weights and biases
    public void MutateWeightsAndBiases(float deviation)
    {
        for (int i = 0; i < numOutputNodes; i++) {
            for (int j = 0; j < numInputNodes; j++) {
                //Generate a random weight
                float randomWeight = Random.Range(-1.0f, 1.0f);
                //Interpolate between the current weight and the random  weight
                //The distance we interpolate is the deviation. Low deviation means minimal change.
                weights[j, i] = Mathf.Lerp(weights[j, i], randomWeight, deviation);
            }
            
            //Repeat for biases
            float randomBias = Random.Range(-1.0f, 1.0f);

            biases[i] = Mathf.Lerp(biases[i], randomBias, deviation);
        }
    }

    //Given a vector of inputs, calculate the output of the layer (output_j = input_i*weight_ij + bias_j)
    public float[] CalculateOutput(float[] inputs)
    {
        float[] activations = new float[numOutputNodes];

        //Calculate the weighted inputs for each output node
        for (int outputNode = 0; outputNode < numOutputNodes; outputNode++) {
            //Sum over the product of each of the inputs and weights leading to the node
            for (int inputNode = 0; inputNode < numInputNodes; inputNode++) {
                activations[outputNode] += inputs[inputNode] * weights[inputNode, outputNode];
            }
            //Add the bias of each output node
            activations[outputNode] += biases[outputNode];

            //Finally we pass each output through an activation function 
            activations[outputNode] = ReLUActivationFunction(activations[outputNode]);
        }

        return activations;
    } 

    //We employ a sigmoid activation functRectified Linear Unit activation function to allow for non-linear
    //behaviour
    private float ReLUActivationFunction(float output)
    {
        return (output < 0) ? 0 : output;
    } 
}
