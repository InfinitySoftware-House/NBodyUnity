# NBody Simulation Project

## Introduction

This project is an NBody simulation implemented using Unity, leveraging the Barnes-Hut algorithm to improve computational efficiency. Typically, NBody simulations have a computational complexity of O(n^2) due to nested loops. However, by applying the Barnes-Hut algorithm, this project reduces the complexity to O(n log n), enabling more efficient simulation of gravitational interactions between particles.

### Features

- Simulation of 100,000 particles: Runs on MacBook Pro M1 Pro with 10 cores and 16GB of UMA.
- Performance: Achieves 8 iterations per second, though not in real-time.
- Technology: Utilizes Unity for simulation and visualization. Currently, the simulation runs on CPU only.

### Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

## Prerequisites
- Unity 2021.2 or later
- A MacBook Pro M1 Pro or equivalent system for optimal performance (though not strictly necessary)

## Installing
- Clone the repository to your local machine:
  - git clone https://github.com/InfinitySoftware-House/NBodyUnity.git
- Open the project in Unity.
- Build and run the project from the Unity Editor.

## Usage

- Once the project is running, you will see a simulation of particles interacting gravitationally. You can adjust the number of particles and the simulation parameters within Unity's inspector to see how they affect performance and behavior.

## Contribution

We are currently seeking contributions to enhance this simulation, specifically:

- GPU Acceleration: Transition the computational tasks from CPU to GPU to allow for more complex simulations and a greater number of particles.
- Optimization: Improve the existing Barnes-Hut algorithm implementation for better performance and accuracy.

### If you're interested in contributing, please fork the repository and submit your pull requests for review.

## Known Issues

- The simulation is not yet optimized for real-time performance on standard hardware.
- Currently, only CPU-based computations are supported. Efforts to integrate GPU support are ongoing.
  
## Acknowledgments

The Unity community for providing an excellent platform for game development and simulation.
Contributors to the Barnes-Hut algorithm for their groundbreaking work in computational physics.
