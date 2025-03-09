# Self-Driving-Cars
Neural network controlled cars attempting to learn to drive.

This unity project displays my attempts at implementing a genetic algorithm.

The premise is a set of cars are given random control over their functions and allowed to experiment for a limited time. Whichever vehichle performed best is recored and its random values are saved to file. The cars are deleted and then a new batch is created. This new batch inherits the "brain" of the best car saved and then each muteates the weights and biases of that brain to introduce some noise to their thought processes. The new batch are allowed to experiment again and the process repeats with a little more allotted time each batch (if improvements were made). This simulates a survival of the fittest trinaing regime that optimises towards cars that can progress around the track in the best time.

The neural network used is a C# implementation of the python one created a while ago which is, in turn, based on the Sebastian Lague video "How to Create a Neural Network (and Train it to Identify Doodles)": https://youtu.be/hfMk-kjRv4c?si=UZJ7DyXIoGdkdKyl

Unlike the python implementation I wrote, this neural network does not need to calculate any cost or compute and pack propogation methods as the genetic algortihm (handled in the training manager) is used for optimising the weights and biases.

The ideas behind the genetic algorithm process were obtained from the video "Self-Driving Car with JavaScript Course – Neural Networks and Machine Learning": https://www.youtube.com/watch?v=Rs_rAxEsAvI

The JSON save manager was based on the tutorial "Save/Load Data using Json File(Json Serialization/Deserialization) in Unity Game Engine": https://www.youtube.com/watch?v=g0k4XStzkLQ

For this project there are also assets from unity Asset Store creators BROKEN VECTOR and MENA. In particular, MENA created the code that controls the car. In the asset folder there are two car controller scripts. The one ending in "AI" has had basic modifications by myself to allow scripts to directly control the car rather than taking keyboard input to do so.

This project required lots of Unity documentation reading and C# learning and I had a great time making it. From this I would like to apply neural networks and genetic algorithms to new situations as well as learning about better training methods and agent evaluations.
