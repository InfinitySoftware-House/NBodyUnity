# NBody Simulation Project

## Introduction

This project is an NBody simulation implemented using Unity, leveraging the Barnes-Hut algorithm to improve computational efficiency. Typically, NBody simulations have a computational complexity of O(n^2) due to nested loops. However, by applying the Barnes-Hut algorithm, this project reduces the complexity to O(n log n), enabling more efficient simulation of gravitational interactions between particles.

### Features

- Simulation of 242.000 particles maximum.
- Performance: Achieves 3 iterations per second with 242.000 particles on an NVIDIA 4060 8GB.
- Technology: Utilizes Unity for simulation and visualization. Currently, the simulation runs on GPU only.

### Benchmark Results

Below are the benchmark results for the simulation performance on an NVIDIA 4060 8GB GPU:

| Number of Particles | Iterations per Second |
|----------------------|-----------------------|
| 8192                | 145                   |
| 16384               | 90                    |
| 32768               | 45                    |
| 65536               | 17                    |
| 131072              | 5                     |
| 262144              | 3                     |

### Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

## Prerequisites
- Unity 2021.2 or later
- Apple Silicon (M1 Pro or later), Windows PC, or equivalent system for optimal performance (though not strictly necessary)

## Installing
- Clone the repository to your local machine:
  - git clone https://github.com/InfinitySoftware-House/NBodyUnity.git
- Open the project in Unity.
- Build and run the project from the Unity Editor.

## Usage

- Once the project is running, you will see a simulation of particles interacting gravitationally. You can adjust the number of particles and the simulation parameters within Unity's inspector to see how they affect performance and behavior.

## Contribution

- Optimization: Improve the existing Barnes-Hut algorithm implementation for better performance and accuracy.

### If you're interested in contributing, please fork the repository and submit your pull requests for review.

## Known Issues

- The simulation is not yet optimized for real-time performance on standard hardware.
  
## Acknowledgments

The Unity community for providing an excellent platform for game development and simulation.
Contributors to the Barnes-Hut algorithm for their groundbreaking work in computational physics.
