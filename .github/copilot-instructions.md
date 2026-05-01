# Project_Egg — AI Agent Instructions

## Project Overview
2D co-op multiplayer platformer built with **Unity 6 + Universal Render Pipeline (URP)**. Two playable characters (Duck & Bird) with distinct movesets. Networking via **Photon Fusion** (host/client model). Firebase used for backend analytics and database.

## Architecture

### Key Directories
| Path | Purpose |
|------|---------|
| `Assets/Scripts/Character/` | Player controller, animation, stats, movesets |
| `Assets/Scripts/Session/` | Session lifecycle and networking management |
| `Assets/Scripts/Environment/` | Level objects, platforms, traps |
| `Assets/Scripts/Monster/` | Enemy AI and base class |
| `Assets/Scripts/Utility/` | Singletons, interfaces, scene utilities |
| `Assets/Scripts/U_interface/` | In-game UI/UX |
| `Assets/Scenes/` | Game scenes (see below) |

### Scene Map
| Scene | Role |
|-------|------|
| `SampleScene` | Main menu / lobby entry |
| `SessionScene` | Photon Fusion session setup |
| `Stage1-S1`, `Stage1-S2` | Playable campaign levels |
| `Environment-Test` | Dev/level testing |

### Core Patterns
- **Singletons**: Inherit `Singleton<T>` (MonoBehaviour) or `SingletonNetwork<T>` (NetworkBehaviour) — see `Assets/Scripts/Utility/`.
- **Interfaces**: `IDamageable`, `Interactable`, `MoveableObject`, `ThrowAbleItem` — define cross-system contracts.
- **Character type dispatch**: `characterType` enum (`Duck` / `Bird`) drives which moveset, animator, and stats are applied at runtime.

## Photon Fusion Networking
- **Framework**: Photon Fusion host/client model. `NetworkRunner` manages tick updates.
- **State authority**: Host has authority by default. Use `[Networked]` for auto-synced properties.
- **Change callbacks**: `[Networked, OnChangedRender(nameof(Callback))]` for reactive updates.
- **RPCs**: `[Rpc(RpcSources.All, RpcTargets.StateAuthority)]` pattern.
- **Tick timers**: Use `TickTimer` (not `Time.deltaTime`) for network-safe timing.
- **Update loop**: Game logic that must be network-safe goes in `FixedUpdateNetwork()`, not `Update()`.
- **Network objects**: Inherit `NetworkBehaviour`; non-networked singletons inherit `MonoBehaviour`.

## Coding Conventions
- **Classes**: PascalCase. Prefixes: `Base*` (abstract), `*Manager` (singletons), `*Character` (player logic).
- **Networked properties**: lowerCamelCase with `[Networked]`.
- **Private serialized fields**: `[SerializeField] private` or just `[SerializeField]` — prefer `_camelCase`.
- **Interfaces**: `I` prefix (`IDamageable`), except legacy files that omit it (`Interactable`).
- **Enums**: lowerCamelCase (`characterType`, `SessionState`, `MonState`).
- **Constants**: `UPPER_SNAKE_CASE`.
- **Namespace**: No custom namespaces — all scripts are in the global namespace.

## Common Pitfalls
- Do **not** use `Update()` for game logic that needs to be deterministic across clients — use `FixedUpdateNetwork()`.
- Do **not** directly set a `[Networked]` property from a non-state-authority client; use an RPC.
- Scene transitions must go through `G_SceneManager` — do not call `SceneManager.LoadScene` directly.
- Input is handled via Unity's **Input System** (`InputSystem_Actions.inputactions`) — do not use legacy `Input.GetKey`.
- Firebase credentials (`google-services.json`) must be present in `Assets/` to avoid Android build errors.

## Testing & Building
- Open the project in Unity Editor (Unity 6).
- Play-mode multiplayer testing: use **Multiplayer Play Mode** (`com.unity.multiplayer.playmode`) to launch multiple editor instances.
- Unit tests: `com.unity.test-framework` — test runner in Unity Editor → Window → General → Test Runner.
- No CLI build script is configured; builds are launched from Unity Editor → File → Build Settings.
