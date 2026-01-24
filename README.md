# PixelPool

Yet another remake of the classic 8-ball pool game, simplified and optimized for experimenting purposes, such as comparing algorithms and AIs, benchmarking and so on.  
It's a C# Based Console App, with UIs (Raylib_cs) to view results, debug, or even just to play the game normally:

<img width="1093" height="614" alt="image" src="https://github.com/user-attachments/assets/00eaa3b7-dc51-4f36-a395-620779246d75" />

...And a CLI mode with commands for everything: running algorithms, AIs, benchmarking, and at the end the results won't just be nosense numbers, but can actually be seen.

<img width="625" height="450" alt="image" src="https://github.com/user-attachments/assets/3717a680-2196-4c71-b4df-0a4b0cd80ad2" />

## Getting Started
todo

## Project Structure

Inside `algorithms-ai/` the algorithms and reinforcement learning AIs are implemented, wheter through C# or Python.NET  
Inside `src/game/` the **environment** is implemented:  

`PhysicsBody.cs`        - **Class-based physics**  
`PhysicsBodyStruct.cs`  - **Optimized struct-based and hardcoded physics**  
`PoolEnv.cs`            - **Standard environment, safer, uses PhysicsBody and SaveState**  
`PoolEnvMini.cs`        - **Optimized environment, hardcoded, uses PhysicsBodyStruct and SaveStateStruct**  
*It is reccomended to use PoolEnvMini wherever performance is needed, both environments act about the same, but due to some physics difference, you can't, for example, train an AI on PoolEnvMini and make it play on PoolEnv or viceversa*

