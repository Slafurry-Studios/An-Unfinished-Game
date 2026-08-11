# Slafurry — Architecture

Internal starter template for Slafurry. This document explains the folder
structure, base classes, and reasoning behind each decision — so the team
(or future us) never has to guess twice.

---

## Root folder structure
```
Assets/
├── 00_Scripts/
├── 01_Objects/
├── 02_Art/
├── 03_Audio/
├── 04_Scenes/
└── 05_Settings/
```
Numeric prefixes are used at root level because several root folders get
opened back-to-back all the time (`Scenes` especially) — prefixes lock the
folder's position in the Project window so it doesn't shift alphabetically
whenever a new folder is added.

Internal rule: **max 6 subfolders per folder**. If a folder is about to
exceed 6, merge closely related domains, or add a new depth level instead
of widening the same level.

---

## `00_Scripts/` (no numeric prefix)
```
Scripts/
├── Core/
│   ├── Interface/     — IInitializable, IResettable
│   └── Abstract/      — Singleton, GameSystem, LocalSingleton, Manager
├── System/             — GameSystem<T> subclasses, persistent cross-scene services
├── Manager/            — Manager subclasses, per-session gameplay coordinators
├── Game/               — Controllers & per-instance entities, plus GameManager
├── UI/
│   └── Generic/        — reusable UI components (LocalizedText, etc.)
└── Utils/
    └── GameFeel/       — reusable gamefeel effects
```
No numeric prefix here unlike root — only 6 folders and no alphabetical
collision that actually disrupts the workflow, so there's no real problem
that prefixing would solve.

### Where does this script go?
Check its base class:
- `GameSystem<T>` → `System/`
- `Manager` → `Manager/`
- Plain `MonoBehaviour` / `LocalSingleton<T>` that's gameplay-specific → `Game/`
- Reactive UI observer tied to one system → `UI/`
- Reactive UI observer that's generic/portable across projects → `UI/Generic/`
- Generic, no dependency on game state → `Utils/`

---

## Lifecycle rules
- `Awake()` — cache own components + register only. Do NOT touch other objects.
- `Initialize()` — internal setup/data loading. Do NOT touch other objects.
- `PostInitialize()` — safe to grab references to other systems, called
  after EVERY object has finished `Initialize()`.
- `Start()` — avoid it, or guard with an `_isReady` flag if unavoidable
  (Unity still calls `Start()` on the first frame regardless of our custom
  boot order).

## Base class map (`Core/Abstract/`)
- `Singleton<T>` — shared logic, NEVER used directly.
- `GameSystem<T>` — Singleton subclass, persistent (`DontDestroyOnLoad`).
  Used for `...System` (e.g. `AudioSystem`, `SaveSystem`, `LoadingSystem`,
  `SceneLoader`, `PauseSystem`, `LocalizationSystem`).
- `LocalSingleton<T>` — Singleton subclass, scene-bound. Has `.Instance`
  but dies when the scene changes. Used for controllers/objects that are
  convenient to access via `.Instance` but must not persist into the next
  scene (e.g. `CameraShake`).
- `Manager` — NOT a singleton, registers manually with `GameManager`. Used
  for `...Manager` (e.g. `EnemyManager`, `UIManager`).

## Interfaces (`Core/Interface/`)
- `IInitializable` — one-time boot sequence contract: `Priority`,
  `Initialize()`, `PostInitialize()`.
- `IResettable` — `ResetState()` contract, called repeatedly on
  session/level restart, NOT through `LoadingSystem`. Only implemented by
  objects with per-session state (score, active enemies, timers) — not
  ones that only need one-time setup.

---

## Naming & responsibility (System vs Manager vs Controller)
- **System** — generic, cross-scene service, `GameSystem<T>`. Doesn't care
  about gameplay state. E.g. `AudioSystem`, `SaveSystem`, `SceneLoader`.
- **Manager** — gameplay state coordinator, tied to a session/scene,
  `Manager`. Hallmark: "owns many" (EnemyManager owns many Enemies).
- **Controller** — owns the logic of one specific entity, usually not a
  singleton. If there are many instances in the scene, each instance gets
  its own Controller (`EnemyController`, `PlayerController`).

Quick test: "how many instances of this exist?" — 1, cross-scene → System;
1 but per-session, coordinating many other objects → Manager; many, one
per entity → Controller.

### Communication contract between programmers
`Manager` is the public front door. Internals (pools, private lists) stay
`private`. Other programmers only need to call a Manager's public methods
or `GameManager.Instance.X` to integrate — no need to open up someone
else's system.

---

## Child-object composition (per entity)
```
Enemy (root)
  ├── EnemyController.cs   ← thin hub, the only link between children
  ├── Stats/                (abstract Stats)
  ├── Brain/                (abstract Brain)
  └── GameFeel/
        ├── GameFeelController.cs
        ├── HitBlink.cs
        └── SquashStretch.cs
```
- Children must NOT call each other directly / `GetComponentInParent` to
  reach a sibling.
- All coordination goes through the hub (`Controller`) at root — either
  direct method calls, or events when a child needs to notify the hub back.
- `Stats` / `Brain` are abstract → polymorphic per entity type
  (`EnemyStats` vs `BossStats`).
- Null-safety: guard + `Debug.LogWarning` in the hub if a child is missing,
  NEVER fail silently.

---

## GameFeel system (`Utils/GameFeel/`)
- `IGameFeelEffect` — single contract (`Play(GameFeelContext ctx)`) for
  every effect (blink, squash, shake).
- `GameFeelContext` — generic parameter struct (Intensity, Duration,
  Direction, Color); each effect reads only the fields it needs.
- Local effects (per-entity: blink, squash) → `GameFeelController` sitting
  on the `GameFeel` child, `GetComponentsInChildren<IGameFeelEffect>()`
  then loop `Play(ctx)`.
- Global effects (one per scene: camera) → `LocalSingleton<T>`, called via
  the static helper `GameFeel.ShakeCamera()` so callers don't need to
  distinguish local vs global calls.
- `CameraShake` is wired to Cinemachine Impulse Source/Listener (not
  manual lerp) — automatic distance falloff, and reaches every Virtual
  Camera in the scene for free.

---

## Systems already built

### AudioSystem
- `AudioSystem` (`GameSystem<AudioSystem>`) — mixer/volume hub, holds NO
  UI references (Slider/Text) at all. Exposes `OnMusicVolumeChanged` /
  `OnSFXVolumeChanged` events; UI subscribes back via an observer
  (`VolumeSliderUI` in `UI/`).
- `MusicPlayer` / `SFXPlayer` — NOT standalone singletons, accessed via
  `AudioSystem.Music` / `AudioSystem.SFX` (static shortcut properties).
- `MusicData` / `SFXData` — `ScriptableObject`, pure data.
  `SFXData` is categorized by domain (`Player`, `Enemy`, `UI`), each
  category gets its own `AudioSource` pool so one category's SFX can't be
  "stolen" by another category that's currently busy.
- `MusicPlayer` subscribes to `SceneLoader.OnSceneLoadCompleted` (not
  `SceneManager.sceneLoaded` directly) so music switches right after the
  loading process finishes, not just when the scene technically finishes
  loading.
- Static helper: `Audio.PlayMusic(...)`, `Audio.PlaySFX2D(...)`,
  `Audio.PlaySFX3D(...)`.
- Linear volume (0-1 from a Slider) is converted to decibels before going
  into `AudioMixer.SetFloat` (`Mathf.Log10(linear) * 20`).

### LoadingSystem
- Collects every `IInitializable`, runs the boot sequence ordered by
  `Priority`.
- Pure logic — fires events (`OnProgressChanged`, `OnStatusChanged`,
  `OnLoadingComplete`), holds NO Slider/Text reference. UI
  (`LoadingScreenUI`) subscribes separately in `UI/`.
- Has a per-object timeout guard so a stuck `Initialize()` doesn't silently
  freeze the loading sequence forever.

### SceneLoader
- `LoadSceneAsync` with `allowSceneActivation` held back until progress
  ≈90%, then activated — fires `OnSceneLoadStarted/Progress/Completed`.

### PauseSystem
- `Time.timeScale` as the single source of truth for pausing.
- Team rule: gameplay/animation uses `Time.deltaTime` (auto-freezes);
  UI/gamefeel that must keep running while paused uses
  `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`.

### SaveSystem
- Uses `Newtonsoft.Json` (`com.unity.nuget.newtonsoft-json`) — supports
  `Dictionary<K,V>` and complex structures that Unity's built-in
  `JsonUtility` can't handle.
- Generic: `Save<T>(data, fileName)` / `Load<T>(fileName, fallback)`.

### LocalizationSystem
- `LocalizationTable` (`ScriptableObject`) — key → text data per language.
- `LocalizationSystem` exposes `GetText(key)` + `OnLanguageChanged` event.
- `LocalizedText` (`UI/Generic/`) — observer that auto-refreshes its text
  when the language changes, uses `[RequireComponent(typeof(TMP_Text))]` +
  `GetComponent` (no manual Inspector drag needed).

---

## `01_Objects/` — Prefabs & ScriptableObjects
```
Objects/
├── Prefabs/
│   ├── Enemies/
│   └── Weapons/
├── Data/
│   ├── Enemies/    — one prefab : many data variants (Easy/Normal/Hard)
│   └── Weapons/
```
`Prefabs/` and `Data/` are split at the top level (instead of mixed
per-domain) because the prefab-to-data ratio is often 1:5+ (many
ScriptableObject variants per 1 prefab), and there's a real need to open
`Data/` on its own during balancing without prefabs cluttering the view.
Still nested per-domain inside so assigning an SO to its prefab is only
one folder level away, not scattered across separate root folders.

Data variant naming convention: `[EntityName]_[Variant].asset`
(`Enemy_Basic_Easy`, `Enemy_Basic_Hard`) — so variants of one entity
naturally cluster together alphabetically.

---

## Git
- `.gitattributes` uses the official
  [gitattributes/gitattributes Unity template](https://github.com/gitattributes/gitattributes/blob/master/Unity.gitattributes)
  (MIT) — handles C# diffing, Unity's YAML merge driver, and LFS for every
  binary asset type (audio, 3D models, images, video, archives, etc.).
- **Required**: run `git lfs install` on every machine before cloning, or
  binary assets will only fetch as LFS pointers instead of actual content.

---

## Not yet implemented (noted for later)
- Editor validator: check for duplicate singletons, Managers/children that
  forgot to register/assign.
- `enum InitOrder` — centralize `Priority` numbers so they don't collide
  across files.
- `IPoolable`, `IDamageable` — additional interfaces once there's a
  concrete combat/pooling need.
- Folder color/icon coding (`ColorfulFolders` / `Unity-Folder-Icons`) for
  the `Objects/` and `Scripts/` structure.
