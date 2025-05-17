# 🤠 Showdown at Sundown

**Showdown at Sundown** is an immersive VR mini-game set in a stylized Western world, designed for the **Meta Quest 2**. Crafted with custom low-poly assets modeled, textured, and animated in Blender, the game offers an atmospheric and performance-optimized experience built in Unity.

Players begin their journey at a tense Western town, learning movement and interaction mechanics at their own pace. But peace doesn’t last long—soon, they’re thrust into a desperate fight for survival. As angry townspeople spawn and swarm from all directions, players must hold their ground and fend off the growing mob before they’re caught and hanged.

Will you survive the **Showdown at Sundown**?
---

## 🗂️ Project Structure

- **`blender/`**  
  Contains all assets created in Blender including character models (e.g., cowboy), animation cycles (idle, walking, running), props, and environment elements like the saloon.

- **`unity/VR_Final/`**  
  Unity project that integrates Blender assets into the game environment. This is the main development folder and should be opened with **Unity Editor 6000.0.40f1 (Intel)**.


- **Notable files/folders inside `VR_Final/`:**
  - `Assets/`: Core content of the game.
  - `build.apk`: Example Android build output (for Meta Quest 2).
  - `ProjectSettings/`: Contains configuration for rendering, input, and XR.
  - `*.csproj` / `*.sln`: Unity-generated build system files.

---

## 🕹️ How to Run the Game

### 📦 Prerequisites

- **Unity Editor:** 6000.0.40f1 (Intel LTS) — *not tested with 2022.3.x or Silicon builds*
- **Platform Target:** Android (Meta Quest 2)
- **XR Plugin:** Oculus XR Plugin + OpenXR

### 🏗️ Steps to Launch in Unity

1. Open Unity Hub.
2. Select `Open > unity` directory.
3. Choose version **6000.0.40f1 (Intel)** when prompted.
4. Switch platform to **Android** (File > Build Settings > Android).
5. Connect your Meta Quest 2 via USB and enable developer mode.
6. Press `Build & Run` to deploy to your headset.
