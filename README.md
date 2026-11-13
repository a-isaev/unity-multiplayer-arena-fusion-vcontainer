# Coin Rush

Top-down mobile multiplayer arena for 2–4 players. Collect coins, dash into rivals to knock them away. Two-minute rounds; most coins wins.

> 🚧 Work in progress. See [Roadmap](#roadmap).

<!-- TODO: gameplay GIF -->
<!-- TODO: [▶ Play WebGL](...) · [⬇ Download APK](...) -->

## Tech stack

| Area | Tech |
|---|---|
| Engine | Unity 6 (6000.6), URP |
| Networking | Photon Fusion 2 — Host mode (host authority, client-side prediction) |
| DI | VContainer (project scope + per-scene scopes) |
| Async | UniTask |
| Content | Addressables (arena, skins) |
| UI | MVP: passive views, presenters injected via VContainer |
| Graphics | Shader Graph, URP post-processing |
| Tests | Unity Test Framework (NUnit) on pure C# game logic |
| Targets | Android (APK), WebGL |

## Architecture

<!-- TODO: mermaid diagram -->

- **Pure C# game rules** (score, round timer, rules) live outside of MonoBehaviours and are unit-tested.
- **Network layer** (`NetworkBehaviour`) is thin: it reads input and calls the game logic.
- **Server-authoritative:** clients only send `NetworkInput`. The host validates and decides everything that matters (coin pickup distance, dash cooldown, round end).
- **No singletons / `FindObjectOfType`.** Everything is wired through VContainer.

```
Assets/_Project/
  Scripts/
    Core/            pure game logic (no UnityEngine)
    Gameplay/        Unity-side gameplay components
    Network/         Fusion NetworkBehaviours, runner bootstrap
    UI/              MVP views & presenters
    Infrastructure/  DI scopes, scene loading, config, app flow
    Editor/          project setup tooling
  Scenes/ Prefabs/ Art/ Shaders/ Addressables/ Settings/ Tests/
```

## Getting started

1. Unity **6000.6.x** with Android and/or WebGL build support.
2. Clone and open the project. Packages (VContainer, UniTask) resolve from git via UPM.
3. Photon: create a **Fusion 2** app at [dashboard.photonengine.com](https://dashboard.photonengine.com), then in Unity run
   **Coin Rush → Create Local Photon Config** and paste the App ID into the created `photon_config.json` (git-ignored).
4. Open `Assets/_Project/Scenes/Bootstrap.unity` and press Play.

## Roadmap

- [x] Project scaffold: DI root scope, bootstrap → menu flow
- [ ] Fusion 2 connection, lobby (create / join by code), player movement with prediction
- [ ] Coins, score, dash with cooldown & knockback, round timer, results, restart
- [ ] MVP UI, Addressables loading screen
- [ ] Shader Graph effects, bloom, mobile profiling (before/after numbers)
- [ ] Android & WebGL builds, unit tests, final README
