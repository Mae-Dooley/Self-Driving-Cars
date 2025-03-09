using UnityEngine;
using System.IO;

public class JSONSaveLoadManager : MonoBehaviour
{
    [HideInInspector]
    public int[] layerSizesToSave;
    [HideInInspector]
    public float[] weightsToSave;
    [HideInInspector]
    public float[] biasesToSave;

    //Save a brain's layer sizes, weights and biases to a json file
    public void SaveBrainToJSON() 
    {
        BrainSaveState bestBrain = new BrainSaveState();

        //Copy over variables from save state instance to save manager
        bestBrain.layerSizes = layerSizesToSave;
        bestBrain.weights = weightsToSave;
        bestBrain.biases = biasesToSave;        

        //Create a string of the data
        string json = JsonUtility.ToJson(bestBrain, true);
        //Save to file
        File.WriteAllText(Application.dataPath + "/BestBrainDataFile.json", json);
    }

    //Load a brain from a json file and return the data inside a brain save state
    public BrainSaveState LoadBrainFromJSON()
    {
        //Create and load string of the data
        string json = File.ReadAllText(Application.dataPath + "/BestBrainDataFile.json");
        //Assign variables from the string to a brain save state instance
        BrainSaveState bestBrain = JsonUtility.FromJson<BrainSaveState>(json);

        return bestBrain;
    }

    //The Json can only store 1D arrays and so the following functions take in a car brain and unpack
    //the weights and biases from 3D and 2D arrays respectively to 1D arrays
    public float[] UnpackWeights(CarBrain brain)
    {
        int lengthOfUnpackedWeights = 0;

        //Calculate how many total weights need storing
        for (int i = 0; i < brain.layerSizes.Length - 1; i++) {
            int numWeightsInLayer = brain.layerSizes[i] * brain.layerSizes[i+1];
            lengthOfUnpackedWeights += numWeightsInLayer;
        }

        float[] unpackedWeights = new float[lengthOfUnpackedWeights];

        //Traverse through all the weights (3D array) and assign each to the unpacked weights (1D array)
        int weightsInPreviousLayers = 0;
        for (int l = 0; l < brain.layerSizes.Length - 1; l++) {
            //For each layer get the num of nodes coming in and out
            int numIncomingNodes = brain.layerSizes[l];
            int numOutgoingNodes = brain.layerSizes[l+1];
            //Calculate how many weights have already been considered
            if (l > 0) {
                weightsInPreviousLayers += brain.layerSizes[l - 1] * brain.layerSizes[l];
            }
            //Form an index for each weight and then assign the linear list with the associated weight
            for (int outputNode = 0; outputNode < numOutgoingNodes; outputNode++) {
                for (int inputNode = 0; inputNode < numIncomingNodes; inputNode++) {
                    int unpackedIndex = weightsInPreviousLayers + (outputNode * numIncomingNodes) + inputNode;

                    float weightToBeAdded = brain.neuralNetwork.layers[l].weights[inputNode, outputNode];

                    unpackedWeights[unpackedIndex] = weightToBeAdded;                                            
                }
            }
        }

        return unpackedWeights;  
    }

    //We perform a similar process with the biases but from 2D to 1D this time
    public float[] UnpackBiases(CarBrain brain)
    {
        int lengthOfUnpackedBiases = 0;

        for (int i = 1; i < brain.layerSizes.Length; i++) {
            lengthOfUnpackedBiases += brain.layerSizes[i];
        }

        float[] unpackedBiases = new float[lengthOfUnpackedBiases];

        //Traverse through all the biases (2D array) and assign each to unpacked biases (1D array)
        int biasesInPreviousLayers = 0;
        for (int l = 0; l < brain.layerSizes.Length - 1; l++) {
            //For each layer get the num of nodes going out
            int numOutgoingNodes = brain.layerSizes[l+1];
            //calculate how many biases have already been considered
            if (l > 0) {
                biasesInPreviousLayers += brain.layerSizes[l];
            }
            //Form an index for each bias and then assign the linear list with the associated bias
            for (int outputNode = 0; outputNode < numOutgoingNodes; outputNode++) {
                int unpackedIndex = biasesInPreviousLayers + outputNode;

                float biasToBeAdded = brain.neuralNetwork.layers[l].biases[outputNode];

                unpackedBiases[unpackedIndex] = biasToBeAdded;
            }
        }

        return unpackedBiases;
    }


    //The manager can now unpack data to save to json but it needs to do the reverse in order to use the
    //saved data
    //We could go through each element in the linear array and calculate where it should be placed in the 3D
    //array but we can reuse the same construction to instead assign each location in the 3D array with the 
    //linear array element belonging to the previously calculated index
    public float[][,] PackageWeights(int[] layersToFit, float[] weightsToPack) 
    {
        float[][,] finalWeights = new float[layersToFit.Length - 1][,];

        //Traverse through all the finalWeights (3D array) and assign each location to the unpacked weights
        int weightsInPreviousLayers = 0;
        for (int l = 0; l < layersToFit.Length - 1; l++) {
            //For each layer get the num of nodes coming in and out
            int numIncomingNodes = layersToFit[l];
            int numOutgoingNodes = layersToFit[l+1];

            //Set the size of the final weights array layer l
            finalWeights[l] = new float[numIncomingNodes, numOutgoingNodes];

            //Calculate how many weights have already been considered
            if (l > 0) {
                weightsInPreviousLayers += layersToFit[l - 1] * layersToFit[l];
            }
            //Form an index for each weight and then assign the 3D array element with the associated weight
            for (int outputNode = 0; outputNode < numOutgoingNodes; outputNode++) {
                for (int inputNode = 0; inputNode < numIncomingNodes; inputNode++) {
                    int unpackedIndex = weightsInPreviousLayers + (outputNode * numIncomingNodes) + inputNode;

                    finalWeights[l][inputNode, outputNode] = weightsToPack[unpackedIndex];                                        
                }
            }
        }

        return finalWeights;
    }

    //And finally we do the same for the biases
    public float[][] PackageBiases(int[] layersToFit, float[] biasesToPack)
    {
        float[][] finalBiases = new float[layersToFit.Length - 1][];

        //Traverse through all the finalBiases (2D array) and assign each to unpacked biases
        int biasesInPreviousLayers = 0;
        for (int l = 0; l < layersToFit.Length - 1; l++) {
            //For each layer get the num of nodes going out
            int numOutgoingNodes = layersToFit[l+1];

            //Set the size of the final biases array layer l
            finalBiases[l] = new float[numOutgoingNodes];

            //calculate how many biases have already been considered
            if (l > 0) {
                biasesInPreviousLayers += layersToFit[l];
            }
            //Form an index for each bias and then assign the non-linear array with the associated bias
            for (int outputNode = 0; outputNode < numOutgoingNodes; outputNode++) {
                int unpackedIndex = biasesInPreviousLayers + outputNode;

                finalBiases[l][outputNode] = biasesToPack[unpackedIndex];
            }
        }

        return finalBiases;
    }
}
