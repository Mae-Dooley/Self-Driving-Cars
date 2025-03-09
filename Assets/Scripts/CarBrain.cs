using UnityEngine;


public class CarBrain : MonoBehaviour
{
    //Connect to the car controller
    private PrometeoCarControllerAI PCC;

    //Variables needed for detection
    [SerializeField]
    private float sightDistance = 40f;
    [SerializeField]
    private float floorOffset = 0.05f;
    [SerializeField]
    private LayerMask sightMask; //The Layers of gameobjects are stored as binary layers in this mask and we can 
    //check an objects layer on collision or only collide with objects of the correct layer. 
    //This mask ignores other cars

    //Neural network variables
    public NeuralNetwork neuralNetwork;
    private int numOfDetectionRays = 11;
    private float[] inputNodeData; //Data to be inputted to the network
    private float[] outputNodeData; //The data to be outputted (to control the car)
    //outputData will hold the distance of collisions from the 11 rays, the car speed, throttleAxis
    // steeringAxis, drifting state
    //The outputs will be whether to accelerate, reverse, turn left, turn right or handbrake
    //this means there are 15 input nodes and 5 output nodes. A single hidden layer of 10 nodes is used
    public int[] layerSizes = {15, 10, 5};

    //Runtime performance data
    public bool deleted;
    public int currentCheckpointReached = 0;
    public int lapsCompleted = 0;
    
    //If we are creating a car to give it a custom brain then these variables will have that data before start
    //is run
    public bool useSavedBrain = false;
    public float[][,] customWeights;
    public float[][] customBiases;
    
    public bool mutate = false; //Marks if the brain should add in noise to its weights
    public float deviation; //How much we should deviate from the given brain when mutating

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        //the car starts not deleted
        deleted = false;

        //Define the mask used for detection rays
        sightMask = LayerMask.GetMask("Default");

        //Connect to the car controller
        PCC = GetComponent<PrometeoCarControllerAI>();
        
        //Create a neural network for the car
        neuralNetwork = new NeuralNetwork(layerSizes);

        if (useSavedBrain) {
            //Set the brain to a custom one
            neuralNetwork.SetWeightsAndBiases(useSavedBrain, customWeights, customBiases);

            //Mutate the brain to add in some (hopefully) novel behaviour
            if (mutate) {
                neuralNetwork.Mutate(deviation);
            }

        } else {
            //Set the brain to a random one
            neuralNetwork.SetWeightsAndBiases();
        }
        
        inputNodeData = new float[layerSizes[0]];
        outputNodeData = new float[layerSizes[layerSizes.Length - 1]];
    }

    // Update is called once per frame
    void Update()
    {
        //Collect all the data for the input vector to the neural network
        DetectSurroundings();
        inputNodeData[numOfDetectionRays] = PCC.carSpeed;
        inputNodeData[numOfDetectionRays + 1] = PCC.steeringAxis;
        inputNodeData[numOfDetectionRays + 2] = PCC.throttleAxis;
        //convert the isDrifting bool to a float representation
        if (PCC.isDrifting) {
            inputNodeData[numOfDetectionRays + 3] = 1f;
        } else {
            inputNodeData[numOfDetectionRays + 3] = 0f;
        }

        //Now we pass the data to the neural network and compute the output vector
        outputNodeData = neuralNetwork.ForwardPass(inputNodeData);

        //Convert the output vector to actions taken on the car
        PCC.goForwardTriggered = (outputNodeData[0] > 0) ? true : false;
        PCC.goReverseTriggered = (outputNodeData[1] > 0) ? true : false;
        PCC.goLeftTriggered = (outputNodeData[2] > 0) ? true : false;
        PCC.goRightTriggered = (outputNodeData[3] > 0) ? true : false;
        if (outputNodeData[4] > 0) {
            PCC.handbrakeTriggered = true;
        } else if (PCC.handbrakeTriggered) {
            PCC.handbrakeTriggered = false;
            PCC.recoverTractionTriggered = true;
        } else {
            PCC.handbrakeTriggered = false;
        }      
    }

    //If a car collides with the grass then we prevent it from being marked as the best car
    void OnCollisionEnter(Collision collision)
    {        
        if (collision.gameObject.name == "Grass") {
            // Debug.Log("Collided with grass.");
            deleted = true;
        }
    }

    //Keep track of the car's checkpoints passed and laps completed
    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Finish") {
            int checkpointIndex = collider.gameObject.GetComponent<CheckpointScript>().index;
            // Debug.Log("Collided with checkpoint " + checkpointIndex);

            if (currentCheckpointReached == 9 && checkpointIndex == 0) {
                lapsCompleted++;
            }

            currentCheckpointReached = checkpointIndex;
        }
    }

    //the majority of the data given to the car is its distance from the surroundings along 
    // rays cast from its position
    private void DetectSurroundings() 
    {
        for (int i = 0; i < numOfDetectionRays; i++) {
            //Get the forward direction of the car and then rotate it to a unique ray
            Vector3 rayDirection = transform.forward;
            rayDirection = Quaternion.AngleAxis(18 * (i - 5), transform.up) * rayDirection;
            
            //Raise the ray slightly off the ground to avoid constant collision detection
            Vector3 rayOrigin = transform.position;
            rayOrigin.y += floorOffset;

            //Create a ray from the car to detect objects
            Ray ray = new Ray(rayOrigin, rayDirection);

            //Variable used to store the collision information
            RaycastHit hitInfo; 

            //Cast the ray and detect collisions. The information of the hit is assigned using the 
            //'out' keyword to hitInfo. 
            //The Raycast function returns a bool so we can use an if statement around it to only store
            //collision info if there is a coliision with an object.
            if (Physics.Raycast(ray, out hitInfo, sightDistance, sightMask))
            {
                Debug.DrawRay(rayOrigin, rayDirection * hitInfo.distance, Color.green);
                inputNodeData[i] = hitInfo.distance;
            } else {
                //If there is no collision along the ray then we set the detected distance to a large number
                Debug.DrawRay(ray.origin, ray.direction * sightDistance);
                inputNodeData[i] = sightDistance * 5;
            }
        }
    }
}
