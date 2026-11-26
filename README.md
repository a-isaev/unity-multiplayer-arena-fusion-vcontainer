# Coin Rush

[Инструкция на русском](README.ru.md)

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
3. If `Assets/Photon` is missing, import Photon Fusion 2. Create a Fusion app in the
   [Photon Dashboard](https://dashboard.photonengine.com/dashboard). In Unity, open
   **CoinRush → Photon Settings**, paste its Fusion App ID, and click **Update App ID**.
   This creates or updates the git-ignored local config.
4. The first time the editor opens the project, it opens `Bootstrap`. Press **Play** in that scene.

### Trying the room lobby

Press **Play** in the `Bootstrap` scene. Choose **Create room** on the host, then **Copy code**.
On a second instance, enter the code and choose **Join room**. Both instances should show
two players. Use the same Photon App ID and region on both. The lobby uses `eu` when
`FixedRegion` is empty. Use matching builds on both clients. Once connected, the test
arena opens automatically. Move your cyan avatar with WASD or the arrow keys and
check that the other window shows it moving. During an active round, only disconnected
participants can return. Their score is restored while the round timer keeps running.

## Verification

- **macOS:** played a full match and checked room capacity and score restoration after reconnecting.
- **Android:** checked coin rendering, simultaneous touch movement and dash, and the enlarged lobby controls on device.
- **WebGL:** joined a macOS host, played a match and verified score restoration after refreshing the same browser tab.
- **EditMode:** 10 Unity NUnit cases passed, covering round rules and contested pickups.

### Android frame pacing

The same Android phone was measured for 20 seconds per run using the game's Android
SurfaceFlinger layer. After disabling mobile Bloom and HDR and updating coin visibility
only when it changes, the mean time between presented frames fell from **21.6 ms**
to **16.0 ms**. SurfaceFlinger reported **46.3 → 62.5 FPS**. The 32–33 ms intervals
seen in about one-third of the baseline sample disappeared in the repeat sample.
These measurements cover presented frames on one device.

## Roadmap

- [x] Project scaffold, VContainer scopes, bootstrap and menu
- [x] Fusion 2 rooms by code and predicted movement
- [x] Coins, score, round timer, results, rematch, dash and knockback
- [x] Lobby, match HUD, Android touch input and Addressables loading screen
- [x] Shader Graph glow, URP bloom and EditMode tests for round rules
- [x] Validate macOS and Android gameplay, Android touch input and frame pacing
- [x] Build and test the experimental WebGL client against a native host
- [x] Document platform checks and Android frame pacing
- [x] Record a two-client match

WebGL joins a room hosted on macOS or Android. The project treats WebGL Client–Server support as experimental because Fusion documents
[performance and connection limitations](https://doc.photonengine.com/fusion/v2/fusion-choose).
