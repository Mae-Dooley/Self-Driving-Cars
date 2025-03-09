using UnityEngine;
using TMPro;

public class TrainingManager : MonoBehaviour
{
    //Game object prefabs
    [Space(10)]
    [Header("AI Car")]
    [Space(5)]
    [SerializeField]
    private GameObject aiCarPrefab;

    //Saving and loading script
    private JSONSaveLoadManager saveManager;

    //Checkpoint system
    [Space(10)]
    [Header("Checkpoints")]
    [Space(5)]
    [SerializeField]
    private Transform[] checkpointLocations = new Transform[10];
    private int currentCheckpointIndex = 0;
    private int currentLap = 0;

    //Training variables
    [Space(10)]
    [Header("Training Variables")]
    [Space(5)]
    private int numOfCars = 100;
    private GameObject[] listOfCars;
    private int indexOfBestCar = 0;
    //The deviation is a measure of how much the new cars will change from the best in the last loop
    [SerializeField]
    private float deviationPerReset = 0.2f;
    private float distRequiredToAdvance = 5f; //How close cars need to get to the checkpoint to advance
    private float bestDistanceThisLoopStartingVal = 10000f; //Large so that a car will be the best
    private float bestDistanceThisLoop; //The best distance recorded each training loop
    private float bestDistanceThisCheckpointStartingVal = 10000f;
    private float bestDistanceThisCheckpoint; //The best distance recorded this checkpoint
    private float timeAllowedPerTrainingLoop = 10; //seconds of training time
    private float timeInCurrentTrainingLoop = 0;
    private int loopsCompleted = 0;

    //UI variables
    [Space(10)]
    [Header("UI Text Elements")]
    [Space(5)]
    public TMPro.TMP_Text numCarsText;
    public TMPro.TMP_Text currentCheckText;
    public TMPro.TMP_Text currentLapText;    
    public TMPro.TMP_Text distOfBestCarThisLoopText;
    public TMPro.TMP_Text distOfBestCarSoFarText;
    public TMPro.TMP_Text timePerLoopText;
    public TMPro.TMP_Text currentTimeInLoopText;
    public TMPro.TMP_Text loopsCompletedText;

    void Start() 
    {
        saveManager = GetComponent<JSONSaveLoadManager>();

        listOfCars = new GameObject[numOfCars];

        bestDistanceThisLoop = bestDistanceThisLoopStartingVal;
        bestDistanceThisCheckpoint = bestDistanceThisCheckpointStartingVal;

        GenerateListOfCars();  
    }

    void Update()
    {
        Transform checkpoint = checkpointLocations[currentCheckpointIndex];

        //Reset the criterion for finding the best performing car this training loop
        bestDistanceThisLoop = bestDistanceThisLoopStartingVal;

        //Find the best car and the best distance to the checkpoint
        for (int i = 0; i < listOfCars.Length; i++) {           
            CarBrain carBrain = listOfCars[i].GetComponent<CarBrain>();

            //Ignore cars marked for deletion and only consider cars on the correct lap
            if (!carBrain.deleted && carBrain.lapsCompleted == currentLap) {
                Vector3 displacementToCheckpoint = listOfCars[i].transform.position - checkpoint.position;
                float distanceToCheckpoint = displacementToCheckpoint.magnitude;

                if (distanceToCheckpoint < bestDistanceThisLoop) {
                    bestDistanceThisLoop = distanceToCheckpoint;
                    indexOfBestCar = i;
                } 
            }
        }
        
        //Get the brain of the best car (found in the loop above)
        CarBrain bestBrain = listOfCars[indexOfBestCar].GetComponent<CarBrain>();

        //Advance the checkpoint index if a car got close enough to the checkpoint 
        //(with the required number of laps)
        if (bestDistanceThisLoop < distRequiredToAdvance && bestBrain.lapsCompleted == currentLap) {
            Debug.Log("Reset: Checkpoint reached");

            //If reaching the last checkpoint then increase the laps needed counter
            currentCheckpointIndex++;

            //Upon reaching the checkpoint, lower the deviation for future generations to encourage similar
            //behaviour that is known to work with less experimentation the further around the track they get
            if (deviationPerReset > 0.07f) {
                deviationPerReset -= 0.02f;
                Debug.Log("Deviation per reset: " + deviationPerReset);
            }

            //If this new checkpoint takes you to a new lap then do so here
            if (currentCheckpointIndex >= checkpointLocations.Length) {
                currentCheckpointIndex %= checkpointLocations.Length;
                currentLap++;
            }

            //Reset the best distance to the checkpoint
            bestDistanceThisCheckpoint = bestDistanceThisCheckpointStartingVal;

            //Save the best brain and reset the loop
            SaveBestCarBrain(bestBrain);

            //Reset the training loop and then the cars
            ResetTrainingLoop();
            ResetListOfCars();

            //Award more time for the loop so that a new checkpoint can be reached
            IncreaseAllowedTime(5);
        }

        //Check if the cars have run out of time this loop
        if (timeInCurrentTrainingLoop > timeAllowedPerTrainingLoop) {
            Debug.Log("Reset: Time exceeded");

            //Time exceeded so reset the training loop           
            ResetTrainingLoop();
            
            //If the best distance has been improved then save the brain that did it 
            if (bestDistanceThisLoop < bestDistanceThisCheckpoint) {

                //Save the best car brain if the car made progress
                if (bestBrain.currentCheckpointReached == currentCheckpointIndex 
                                                && bestBrain.lapsCompleted == currentLap) {
                    //Update best dist recorded this checkpoint
                    bestDistanceThisCheckpoint = bestDistanceThisLoop;

                    SaveBestCarBrain(bestBrain);
                }

                //Finally, reset the cars
                ResetListOfCars();

            } else {
                //The best distance was not improved so we just reset the cars using the old best brain
                ResetListOfCars();
            }

        }

        //Increment the time spent in the loop
        timeInCurrentTrainingLoop += Time.deltaTime;

        //Update the UI
        numCarsText.text = numOfCars.ToString();
        currentCheckText.text = currentCheckpointIndex.ToString();
        currentLapText.text = currentLap.ToString();

        float bestDistText = Mathf.Round(bestDistanceThisLoop * 100) / 100;
        distOfBestCarThisLoopText.text = bestDistText.ToString() + "m";

        float globalBestDistText = Mathf.Round(bestDistanceThisCheckpoint * 100) / 100;
        distOfBestCarSoFarText.text = globalBestDistText.ToString() + "m";

        float timePerLoop = Mathf.Round(timeAllowedPerTrainingLoop * 10) / 10;
        timePerLoopText.text = timeAllowedPerTrainingLoop.ToString() + "s";

        float currTimeText = Mathf.Round(timeInCurrentTrainingLoop * 100) / 100;
        currentTimeInLoopText.text = currTimeText.ToString() + "s";

        loopsCompletedText.text = loopsCompleted.ToString();
    }

    //Create a new set of cars. We can choose, based on values passed to the function, if the brains should
    //utilise a previously saved brain, if they should mutate from this brain, and the deviation of how much
    private void GenerateListOfCars(bool useSavedBrain = false, bool mutate = false, float deviation = 0f)
    {
        for (int i = 0; i < numOfCars; i++) {
            //Instantiate a car
            //USE THIS STARTING LOCATION FOR TRACKS 1 AND 2
            // listOfCars[i] = Instantiate(aiCarPrefab, new Vector3(-45.5f, 0f, -79.5f), Quaternion.identity);
            //USE THIS STARTING LOCATION FOR TRACK 3
            listOfCars[i] = Instantiate(aiCarPrefab, new Vector3(-28.5f, 0f, -78.5f), Quaternion.identity);

            //Rotate the car
            listOfCars[i].transform.rotation = Quaternion.AngleAxis(90, Vector3.up);

            //Give the car a brain
            CarBrain newCarBrain = listOfCars[i].AddComponent<CarBrain>(); 
            
            //If we are using a saved brain then assign the weights here from the json
            if (useSavedBrain) {
                newCarBrain.useSavedBrain = true; 

                BrainSaveState savedBrainData = saveManager.LoadBrainFromJSON();

                //Extract the data from the json string and format it for the CarBrain object
                int[] layersInCarBrain = savedBrainData.layerSizes;
                float[][,] customWeights = saveManager.PackageWeights(layersInCarBrain, savedBrainData.weights);
                float[][] customBiases = saveManager.PackageBiases(layersInCarBrain, savedBrainData.biases);

                newCarBrain.customWeights = customWeights;
                newCarBrain.customBiases = customBiases;

                //Control if the brain should be mutated
                newCarBrain.mutate = mutate;
                newCarBrain.deviation = deviation; 
            } 
        }
    }

    //If called then destroy all the cars and create a new batch of them
    private void ResetListOfCars()
    {
        for (int i = 0; i < numOfCars; i++) {
            Destroy(listOfCars[i]);
        }

        //Upon destroying all the cars we now instantiate a new list but load them 
        //with the brain saved in the json
        GenerateListOfCars(true, true, deviationPerReset);
    }

    //Load all the brain data (of a given brain) into the save manager then save it to json
    private void SaveBestCarBrain(CarBrain brain) 
    {
        //Save layer sizes
        saveManager.layerSizesToSave = brain.layerSizes;
        //Save weights and biases (unpacked as 1d arrays)
        saveManager.weightsToSave = saveManager.UnpackWeights(brain);
        saveManager.biasesToSave = saveManager.UnpackBiases(brain);

        //Save the data in the save manager to a Json
        saveManager.SaveBrainToJSON();

        //If a brain is saved then increase the time allowed so that a new best distance gets rewarded with a 
        //chance to get further next run. This is aimed to allow for progress if insufficient time has been
        //allocated for a checkpoint
        IncreaseAllowedTime(0.1f);
        Debug.Log("New brain saved. Time Increased.");
    }

    //Simply increase the allowed time in a loop
    private void IncreaseAllowedTime(float seconds)
    {
        timeAllowedPerTrainingLoop += seconds;
    }

    //Unlike resetting the cars, resetting the training loop just controls data about the trianing process
    private void ResetTrainingLoop()
    {
        //Reset time in loop and count the loop
        timeInCurrentTrainingLoop = 0;
        loopsCompleted++;
    }
}


