# ⏳ Chrono Thinker

Chrono Thinker is a 2D top-down puzzle-combat game built in Unity, combining real-time strategy, enemy evasion, and reactive gameplay under time pressure. Players must navigate enemy-patrolled dungeons using smart positioning and timed attacks to break out of a looping time trap.

## 🎮 Gameplay Overview

- **Levels**: Two handcrafted levels with increasing difficulty.
- **Enemies**: Reactive AI that patrols and chases based on player proximity.
- **Mechanics**: Punchbags can be charged and launched into enemies to defeat them.
- **Time Loop**: Each level must be completed before time runs out. Failure results in a restart, echoing the narrative loop.

## 🎯 Core Features

- ⚔️ **Punch Combat Mechanics**: Knockback-enabled punchbags to damage enemies.
- 💡 **Enemy AI**: Patrol + reactive chase using raycasting and player detection logic.
- ⏱️ **Game Timer**: Adds urgency with a decreasing countdown visible on-screen.
- ❤️ **Health System**: Both players and enemies have health bars; visual and audio feedback enhance immersion.
- 🎨 **Pixel-Art Aesthetics**: Minimalist top-down visual style with clear geometric sprites and contrasting UI.
- 🔁 **Narrative Integration**: Time-based progression mimics a story of survival within a time loop.

## 📂 Project Structure

- `Scenes/`: Contains MainMenu, Level1, and Level2.
- `Scripts/`: Modular scripts for player movement, enemy AI, health management, UI, and level control.
- `Prefabs/`: Reusable components like characters, punchbags, and environment elements.
- `Animations/`: Animator controllers for smooth transitions based on state.

## 🛠️ Technologies

- **Engine**: Unity 2D
- **Language**: C#
- **Tools**: Animator Controller, Unity Canvas, Physics 2D, Raycasting

## 📈 Game Design Highlights

- **Visual Feedback**: Damage flashes, health bars, and charged attack indicators improve player decision-making.
- **Scalable Complexity**: Level 2 builds upon Level 1 with more enemies, tighter paths, and greater strategic challenge.
- **Performance**: Optimized AI, modular design, prefab reuse, and low-overhead sprite rendering ensure 60 FPS.

## 🧪 Playtesting & Iteration

- Iteratively refined based on tester feedback.
- Improved enemy pathfinding, fairer obstacle spacing, and consistent damage cues in Level 2.

## 🎬 Screenshots

<p align="center">
  <img src="images/chrono_thinker_screenshot.png" width="600" alt="Chrono Thinker Gameplay">
</p>

## 👥 Team

- Aekamjot Singh — [asing888@uwo.ca](mailto:asing888@uwo.ca)
- Yash Gupta — [ygupta26@uwo.ca](mailto:ygupta26@uwo.ca)
- Adam Wilson — [awils323@uwo.ca](mailto:awils323@uwo.ca)

