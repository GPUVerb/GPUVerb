GPUVerb
==================================
**University of Pennsylvania, CIS 565: GPU Programming and Architecture, Final Project**

Evan S, Runshi Gu, Tongwei Dai

# Background
Currently, the landscape of audio in real-time environments, like video games, requires heavy precomputing to accurately represent the real nature of sound. In dynamic environments, precomputing for scenes becomes a logistical impossibility. The paper [Interactive sound propagation for dynamic scenes using 2D wave simulation](https://www.microsoft.com/en-us/research/uploads/prod/2020/08/Planeverb_CameraReady_wFonts.pdf) and its corresponding Unity plugin [Planeverb](https://github.com/themattrosen/Planeverb/tree/master/), addresses this by enabling use of realistic and dynamic acoustics for real-time environments. 

GPUVerb accelerates the speed of the aforementioned paper and its implementation by moving the logic of its core components from CPU to GPU. Unlike Planeverb, GPUVerb is also fully integrated into Unity's sound engine, through Unity's supplied [Spatializer SDK](https://docs.unity3d.com/Manual/AudioSpatializerSDK.html).

# Overview
There are three primary components to GPUVerb.

- Green([Finite-difference time-domain solver](https://en.wikipedia.org/wiki/Finite-difference_time-domain_method)):
  - **Input**: scene geometry
  - **Output**: particle velocities and pressure in discretized scene grid
- Yellow(Analyzer):
  - **Input**: particle velocities and pressure in discretized scene grid(FDTD output)
  - **Output**: acoustic parameters
- Red(Digital signal processor)
  - **Input**: acoustic parameters(analyzer output) + audio signal(s)
  - **Output**: transformed audio signal(s)

Below is a flowchart of how this would operate within a game engine.  
![](./ReadmeImgs/workflow.png)

## Finite Difference Time Domain (FDTD) Solver
The FDTD solver allows us to accurately simulate the wave-based nature of sound. The scene geometry is first rasterized to a 2D plane fixed at the listener's head position. 

![](./ReadmeImgs/rasterization.png)

Then 2D sound wave propagation is simulated within this plane, as an approximation of the simulation in 3D. We optimize the FDTD solver further by moving some computation from CPU to GPU (using compute shader). (more about this in the [Performance Optimization Section](#performance-analysis))

Below is a simple visualization of the pressure output of the solver.

![](./ReadmeImgs/fdtd_demo.gif)

## Analyzer
The analyzer is wholly based on the implementation described in the paper. (Compute shader usage?)

The digital signal processor (DSP) is built with Unity's Spatializer SDK, which is in turn built on Unity's Native Audio SDK. Essentially, a C++ processor is built into a .dll to be incorporated into the Unity engine as a per-source spatializing plugin. This is currently not incorporated within the project; instead the original C++ DSP is used from the base Planeverb project.

# Performance Optimization
## FDTD Solver using Compute Shader
Our GPU implementation of the FDTD solver parallelizes the computations of update equations of each iteration.

Specifically, each cell in the grid updates its data in parallel based on its own data and the data of the neighboring cells from the previous iteration.

The process is almost embarassingly parallel. Dependency within the grid is minimal, as each cell would need to fetch data from 4 of its closest neighbors.

To test whether the GPU FDTD solver outperforms the original planeverb implementation, we measure the time it takes to simulate the propagation of a single gaussian pulse as grid size increases, for both implementations. Naturally, the lower the runtime, the more efficient the implementation.
- The hardware used for this test is i7-8700 @ 3.20 GHz 16GB and RTX 2070
- Free parameters that impact solver accuracy and performance are set to be the same for both GPU and CPU implementations.
![](./ReadmeImgs/FDTD_time.png)

The result shows that the GPU implementation scales much better with grid dimension. 

This makes it possible to simulate wave propagation in relatively large scenes, because the FDTD solver is run every time the listener or a sound source moves, a running time greater than 400ms may cause noticeable delay in the audio output.
