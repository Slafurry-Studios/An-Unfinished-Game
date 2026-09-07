# AGENTS.md

## Project overview

Unity 2D narrative platformer ("An Unfinished Game") by Slafurry Studios.
Unity **2022.3.62f3** (LTS). Git LFS required for all binary assets.

The repo also contains Python tooling (`retrieve.py`, `track.py`, `core/`) for syncing assets from Google Drive — unrelated to the Unity project.

---

## Unity project layout

```
Assets/
├── _Game/              ← all game code and assets (numeric prefixes)
│   ├── 00_Scripts/
│   ├── 01_Objects/     (Prefabs/, Data/)
│   ├── 02_Art/
│   ├── 03_Audio/
│   ├── 04_Scenes/
│   └── 05_Settings/
├── _Vendor/            ← third-party (TextMesh Pro, FolderColor)
└── Editor/
```

The `_Game` prefix is the project's own content. `_Vendor` is external.
Numeric prefixes lock folder order in Unity's Project window.

---

## Script placement rules (ARCHITECTURE.md is authoritative)

| Base class | Folder | Lifetime |
|---|---|---|
| `GameSystem<T>` | `00_Scripts/System/` | `DontDestroyOnLoad`, cross-scene |
| `Manager` | `00_Scripts/Manager/` | Per-session, registers via `LoadingSystem` |
| `LocalSingleton<T>` | `00_Scripts/Game/` | Scene-bound, dies on scene change |
| Plain `MonoBehaviour` (gameplay-specific) | `00_Scripts/Game/` | Per-entity |

---

## Lifecycle order (do not violate)

1. `Awake()` — cache own components + register. **Do NOT touch other objects.**
2. `Initialize()` — internal setup/data loading. **Do NOT touch other objects.**
3. `PostInitialize()` — safe to reference other systems. Called after **every** object finishes `Initialize()`.
4. `Start()` — avoid. If unavoidable, guard with `_isReady` flag (Unity calls `Start()` on the first frame regardless of custom boot order).

---

## Boot sequence

`BootstrapLoader` (Boot scene) waits for all `GameSystem<T>` instances to finish `Initialize()` + `PostInitialize()`, then loads the target scene (default: `MainMenu`). Do not bypass this.

---

## Naming conventions

- `...System` → `GameSystem<T>` subclass (e.g. `AudioSystem`, `SaveSystem`, `PauseSystem`, `LocalizationSystem`, `VFXSystem`, `InputHub`)
- `...Manager` → `Manager` subclass or `Singleton<T>` (e.g. `ObjectiveManager`)
- `...Controller` → plain `MonoBehaviour`, one per entity instance (e.g. `PlayerController`)

---

## Key systems

- **AudioSystem** — mixer/volume hub. `MusicPlayer` / `SFXPlayer` accessed via `AudioSystem.Music` / `AudioSystem.SFX`. Static helpers: `Audio.PlayMusic(...)`, `Audio.StopMusic(...)`, `Audio.PlaySFX2D(...)`, `Audio.PlaySFX3D(...)`, `Audio.StopSFX(...)`. Linear volume (0–1) is converted to dB: `Mathf.Log10(linear) * 20`.
- **SaveSystem** — uses `Newtonsoft.Json` (`com.unity.nuget.newtonsoft-json`), not Unity's `JsonUtility`. Supports `Dictionary<K,V>` and complex types. Static helpers: `Save.To<T>(...)`, `Save.From<T>(...)`, `Save.Exists(...)`.
- **PauseSystem** — key-based pause stack. `Time.timeScale` is the single source of truth. Static helpers: `Pause.On(key)`, `Pause.Off(key)`, `Pause.Toggle(key)`, `Pause.IsPaused`, `Pause.ForceResume()`.
- **LoadingSystem** — runs all `IInitializable` ordered by `Priority`. Has a per-object timeout guard.
- **LocalizationSystem** — `LocalizationTable` (ScriptableObject) + `LocalizeText` observer (auto-refreshes on language change).
- **VFXSystem** — pooled VFX spawning. `VFX.Play(key, position)`.
- **InputHub** — Unity Input System wrapper. Jump, Move, Crouch, Interact actions with enable/disable control.

---

## Communication rules

- `Manager` is the **public front door**. Internal state (pools, private lists) stays `private`.
- Child objects must NOT call each other directly or use `GetComponentInParent` to reach a sibling. All coordination goes through the hub `Controller` at root.
- Missing child references → guard + `Debug.LogWarning`, **never** fail silently.

---

## GameFeel system

- `IGameFeelEffect` interface in `Utils/GameFeel/` — `PlayEffect()` / `StopEffect()`, no params.
- Local effects (per-entity) → `GameFeel` MonoBehaviour on the `GameFeel` child object, holds `IGameFeelEffect[]` field.
- `CameraShake` is a `MonoBehaviour` implementing `IGameFeelEffect`, wired to Cinemachine Impulse Source/Listener — not a `LocalSingleton<T>`, not manually lerp.

---

## Prefab & data layout

`01_Objects/` splits `Prefabs/` and `Data/` at the top level (prefab-to-SO ratio is often 1:5+).
Data variant naming: `[EntityName]_[Variant].asset` (e.g. `Enemy_Basic_Easy`).

---

## Git / LFS

- **Required on every machine before cloning:** `git lfs install`
- Binary assets (images, audio, models, video) are tracked via LFS — see `.gitattributes`.
- `.gitattributes` uses the official [gitattributes Unity template](https://github.com/gitattributes/gitattributes/blob/master/Unity.gitattributes) (MIT).

---

## Python tooling (outside Unity)

- `retrieve.py` — downloads files from Google Drive, sends Discord notification. Requires `--service-account-file` or `--service-account-b64`, plus `--discord-webhook`.
- `track.py` — detects Drive changes and notifies Discord (no download).
- Both use `core/` module (`drive_client`, `discord_notifier`, `state`, `gemini_flavor`).
- Install: `pip install -r requirements.txt` (google-api-python-client, google-auth, requests).
- Gemini flavor text can be set via `GEMINI_API_KEY`, `GEMINI_MODEL`, `GEMINI_PERSONA` env vars.

---

## Gotchas

- `BootstrapLoader` comments are in Indonesian (Bahasa) — this is normal, not an error.
- `LocalSingleton<T>` dies on scene change — do not use it for cross-scene services.
- `IResettable.ResetState()` is for per-session restarts only, NOT for loading.
- `.sln` and `.csproj` files are autogenerated by Unity and gitignored — do not commit them.
