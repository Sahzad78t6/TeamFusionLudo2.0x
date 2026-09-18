# 3D Mobile Survival Game (Unity)

A first-person 3D survival game prototype for Android & PC built in Unity 2020.3 (URP).

## Key Features
- **Mobile Touch Controls & PC Dual Input**: On-screen Virtual Joystick, Touch Drag Look, Action Buttons (Attack, Build, Interact, Jump, Inventory, Rotate).
- **Survival Mechanics**: Dynamic Health, Hunger, Thirst, and Sleep stat management.
- **Resource Gathering**: Tree chopping, stone mining, and fruit gathering with impact particles and spatial hit audio.
- **Building & Crafting**: Crafting Table crafting system and modular building placement (Shelters, Tents, Beds, Campfires).
- **Enemies & Wildlife AI**: NavMesh pathfinding AI with Bear, Wolf, Zombie enemies and fleeing Rabbit wildlife.
- **Atmospheric Wilderness**: Dynamic Day/Night lighting cycle, URP ShaderGraph water, and filmic ACES post-processing volume profiles.

## Setup & Build Instructions
1. Open the project in **Unity 2020.3.35f1** or newer LTS.
2. Load scene `Assets/Survival 3D/Menu.unity` or `Assets/Survival 3D/Game.unity`.
3. To build for Android:
   - Target Architecture: ARMv7 & ARM64.
   - Package Name: `com.TeamFusion82.Survival3D`.
