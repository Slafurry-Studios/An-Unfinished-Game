# AGENTS.md

Unity 2D narrative platformer ("An Unfinished Game") by Slafurry Studios.
Unity **2022.3.62f3** (LTS), URP, Cinemachine 2.10, Input System 1.14.

`ARCHITECTURE.md` is the authoritative design doc (base classes, lifecycle, per-system
detail). This file only covers what an agent can't get from the filename tree, and
corrects the few places the code has drifted from `ARCHITECTURE.md`.

The repo also contains unrelated Python tooling (`retrieve.py`, `track.py`, `core/`,
`state/`) for syncing assets from Google Drive — see the last section.

---

## Layout

```
Assets/
├── _Game/                  ← all game code and assets
│   ├── 00_Scripts/         (no numeric prefixes below this level)
│   │   ├── Core/           Abstract/ (GameSystem, LocalSingleton, Manager, Singleton), Interface/
│   │   ├── System/         Audio/ Save/ Pause/ Loading/ Scene/ Camera/ VFX/ Input/ Story/ Localization/
│   │   ├── Manager/        Gameplay coordinators
│   │   ├── Game/           Controllers & per-instance entities (Player/, MiniGame/, Triggers/, Puzzle/…)
│   │   ├── UI/             Generic/ HUD/ Menu/
│   │   ├── Utils/          GameFeel/ Pool/ UI/ VFX/ Attributes/
│   │   └── _Debug/
│   ├── 01_Objects/         Prefabs/ Data/
│   ├── 02_Art/   ├── 03_Audio/  ├── 04_Scenes/ (Boot, Menu/, Cutscene/, Dev/)  ├── 05_Settings/
├── _Vendor/                third-party
├── Editor/                 editor-only tooling (BuildScript, asset creator, minigame inspectors)
└── Plugins/WebGL/          QuitPlugin.jslib
```

Internal rule from `ARCHITECTURE.md`: **max 6 subfolders per folder** — merge domains or
add a depth level rather than widening.

---

## Compilation & verification (there is almost no tooling)

- **No `.asmdef` anywhere.** Everything compiles into `Assembly-CSharp`, so a folder named
  `Editor` at *any* depth under `Assets/` makes those scripts editor-only. Never park runtime
  code under an `Editor/` folder, and never move existing runtime scripts into `Assets/Editor/`.
- **No tests exist.** `com.unity.test-framework` is in `Packages/manifest.json` but there are no
  test assemblies, no `Tests/` folder, no `NUnit` usage. There is no CLI lint/typecheck/test path.
  Verification is: let Unity recompile without errors, then play the scene. Don't claim
  "tests pass" — there are none.
- **No Script Execution Order is configured** (no `m_ExecutionOrder` anywhere in `ProjectSettings/`)
  and the two generated `.csproj`/`.sln` are gitignored. So Unity's undefined `Awake()` ordering is
  real — this is why the boot sequence defers cross-object work to `PostInitialize()`.
- `ProjectSettings/VersionControlSettings.asset` is `Visible Meta Files`: every asset needs its
  `.meta` committed, and GUIDs are the real identity — renaming an asset file breaks references.

### Namespaces are not enforced — match the file you're editing

There is no consistent convention. Most of `Core/`, `System/`, `Utils/` use `Slafurry.*`, but
roughly a third of `Game/` and all of `UI/` have **no namespace at all**, and there are also
`RhythmGame.*`, `Game.Dialog`, `Game.UI.HUD`, `Slafurry.Interaction`. Open a sibling file and
match it; don't "fix" a file's namespace as a drive-by change.

### Dot-suffixed partial classes

`Game/Player/Actions/Movement/PlayerMovement.*.cs` splits one class across
`.Collision`, `.Controls`, `.Gravity`, `.Horizontal`, `.Jump`, `.SFX` partials. Follow this
pattern for any script that outgrows one file.

---

## Script placement

| Base class | Folder | Lifetime |
|---|---|---|
| `GameSystem<T>` | `00_Scripts/System/` | `DontDestroyOnLoad`, cross-scene |
| `Manager` | `00_Scripts/Manager/` | per-session, registers via `LoadingSystem` |
| `LocalSingleton<T>` | `00_Scripts/Game/` | scene-bound, dies on scene change |
| `Singleton<T>` (direct) | `00_Scripts/Manager/` | scene-bound singleton |
| Plain `MonoBehaviour` | `00_Scripts/Game/` | per-entity |

Corrections to `ARCHITECTURE.md`:

- The `Manager` base class **currently has zero subclasses**. The only file in `Manager/` is
  `ObjectiveManager : Singleton<ObjectiveManager>` — it is a `Singleton<T>`, not a `Manager`.
  Treat `Manager` as an unused template.
- If you do use `Manager`: it seals both `Awake()` and `PostInitialize()`. You override
  `OnManagerAwake()` / `OnPostInitialize()` / `OnManagerDestroyed()` and must implement the
  abstract `RegisterToGameManager()` + `UnregisterFromGameManager()`. Registration is wired in
  for you in `Awake()`; it cannot be forgotten.
- `StoryManager` is a `GameSystem<StoryManager>` in `System/Story/` despite the `Manager`
  suffix. Trust the base class over the name.

## Lifecycle order (do not violate)

1. `Awake()` — cache own components + register. **Do NOT touch other objects.**
2. `Initialize()` — internal setup/data loading. **Do NOT touch other objects.**
3. `PostInitialize()` — safe to reference other systems. Called after **every** object
   finishes `Initialize()`.
4. `Start()` — avoid. If unavoidable, guard with an `_isReady` flag.

---

## Boot: everything is a manual Inspector drag

`BootstrapLoader` (on the `====== BOOT ======` object in `Boot.unity`) has two serialized
fields that are **hand-assigned, not auto-discovered**: `targetSceneName` (default `MainMenu`)
and `MonoBehaviour[] systemsToWaitFor`. It waits one frame, then for each entry casts to
`IGameSystemLifecycle` and runs `Initialize()` + `PostInitialize()`.

**Adding a new `GameSystem<T>` therefore requires three edits, not one:**
1. Add the component to a GameObject in `Assets/_Game/04_Scenes/Boot.unity`.
2. Drag that component into `BootstrapLoader.systemsToWaitFor`.
3. Assign its serialized fields (e.g. `InputHub.inputActions`) on that object.

Miss any of these and `System.Instance` is `null` at runtime — the static helpers
(`VFX.Play`, `Story.GetFlag`, `Controls.*`, `Audio.*`, …) dereference `Instance` with **no null
guard**, so the failure is a `NullReferenceException` at the call site, not a helpful log.

Also: `Singleton<T>.Awake()` calls `LoadingSystem.Instance.Register(this)`, so a `LoadingSystem`
component **must** exist in `Boot.unity` or every singleton NREs on `Awake`.

Objects present in `Boot.unity` today: `BootstrapLoader`, `LoadingSystem`, `SaveSystem`,
`PauseSystem`, `InputHub`, `AudioSystem`, `SceneLoader`, `LocalizationSystem`, `MusicPlayer`,
`SFXPlayer`, a `Manager.prefab` instance (`ObjectiveManager`), and a loading-screen `Canvas`.
**`VFXSystem` and `StoryManager` are not in any scene or prefab in the repo** — `VFX.Play(...)`
and `Story.*` will throw until someone adds those GameObjects. Don't assume they work.

`LoadingSystem` picks up late arrivals (e.g. a `Player` prefab spawned in a later scene) via a
one-frame "late batch" coroutine so `Initialize()` still precedes `PostInitialize()` within a
batch. It has a `perObjectTimeoutSeconds` guard (default 10s) that logs a warning instead of
freezing boot.

---

## Naming & static helpers

- `...System` → `GameSystem<T>` (`AudioSystem`, `SaveSystem`, `PauseSystem`, `LocalizationSystem`, `SceneLoader`, `VFXSystem`)
- `...Manager` → per-session coordinator (`ObjectiveManager`, `StoryManager`)
- `...Controller` → plain `MonoBehaviour`, one per entity (`BurnoutController`, `SunkCostPatrol`, `PlayerMovement`)

Most systems expose a `static` facade in the same file, in the same namespace as the system:
`Audio.*`, `Save.*`, `Pause.*`, `VFX.*`, `Story.*`, `Localize.*`, `SceneSystem.*` (in `SceneLoader.cs`),
`Controls.*` (in `InputHub.cs`).

---

## Systems not covered by ARCHITECTURE.md

- **StoryManager** (`System/Story/`) — story flags via `PlayerPrefs`, keys auto-prefixed `story_`.
  `Story.GetFlag/SetFlag/GetInt/SetInt/GetFloat/SetFloat/GetString/SetString/Delete/DeleteAll`.
  **Do NOT route story state through `SaveSystem`.** Fires `OnFlagChanged` / `OnIntChanged` /
  `OnFloatChanged` / `OnStringChanged`.
- **VFXSystem** (`System/VFX/`) — key→pool registry (`VFXEntry[]` with `key`, `prefab`,
  `defaultCapacity`, `maxSize`). `VFX.Play(key, position[, rotation])`; unknown key logs a
  warning and returns `null`. Pools are built in `Initialize()`, so `Play` before init fails.
- **InputHub** (`System/Input/`) — wraps `Assets/_Game/05_Settings/Input/Main Input.inputactions`,
  which has exactly one action map, `Gameplay`, with `Move` (Vector2), `Jump`, `Crouch`, `Interact`.
  Actions are resolved **by string name** (`FindAction("Jump")`) with no null checks, so renaming
  an action in the `.inputactions` file breaks input at runtime.
  `Controls.*` is **event-only** (`OnJumpPressed`, `OnMoveChanged`, …) plus `IsInputEnabled` /
  `EnableInput` / `DisableInput` / `SetInputEnabled` — there is no polling API like `Held()`.
  Input is force-re-enabled on `SceneLoader.OnSceneLoadCompleted`, so anything that calls
  `DisableInput()` (the minigames do) leaks that state across scene loads unless it re-enables.
- **Triggers** live in `Game/Triggers/` (`BaseTrigger` + subclasses), *not* in
  `Core/Abstract/Triggers/` — that folder is an empty untracked leftover. `DetectArea` is in
  `Game/Level/`.

`GameSystem`/`SaveSystem`/`PauseSystem`/`AudioSystem`/`LocalizationSystem`/`CameraSystem` details
are already written up in `ARCHITECTURE.md` — read it rather than relying on memory.

---

## Communication rules

- `Manager` is the **public front door**. Internal state (pools, private lists) stays `private`.
- Child objects must NOT call each other directly or use `GetComponentInParent` to reach a
  sibling. All coordination goes through the hub `Controller` at root.
- Missing child references → guard + `Debug.LogWarning`, **never** fail silently.

## GameFeel

- `IGameFeelEffect` in `Utils/GameFeel/` — `PlayEffect()` / `StopEffect()`, no params; each
  effect reads its own serialized fields (no shared parameter struct).
- `GameFeel` MonoBehaviour holds an `IGameFeelEffect[]`; attach it to the entity's `GameFeel` child.
- Effects in `Utils/GameFeel/Effects/`: `Blink`, `CameraShake`, `HitStop`, `Knockback`,
  `ObjectShake`, `ScreenFlash`, `SquashObject`, `WorldFade`, `WorldScalePop`.
  `CameraShake` is wired to Cinemachine Impulse (not a `LocalSingleton<T>`, no manual lerp);
  `ScreenFlash` **is** a `LocalSingleton<T>`.
- `GameAssetCreator` (`Assets/Editor/GameAssetCreator*.cs`, `Slafurry.Editor.GameAssetCreator`) is
  the team's asset pipeline; prefer it over hand-authoring new prefab/SO pairs.

## Scenes & build

- `ProjectSettings/EditorBuildSettings.asset` is an **explicit list** and `Boot.unity` must stay at
  index 0. A new level scene is silently excluded from every build until it's added there.
  `AudioTesting.unity` exists on disk but is intentionally not in the build list.
- Build entry point: `Assets/Editor/BuildScript.cs` → `-executeMethod BuildScript.Build`.
  - Requires `-buildTarget <StandaloneWindows64|StandaloneLinux64|StandaloneOSX|WebGL>`; it
    `Fail()`s (exit 1) if missing/unknown.
  - `BuildScript.Build` calls `EditorApplication.Exit(0)` itself on success — **do not pass
    `-quit`**, or the build is cut off mid-flush.
  - Output is relative to the editor's CWD: `build/<Target>/`. WebGL output is a directory;
    the other three are a single file inside it.
  - CI runs it via `buildalon/unity-action@v3` with `-quit -batchmode -nographics`.

## CI workflows (`.github/workflows/`)

- **`unity-itchio-deploy.yml`** — `workflow_dispatch` only. `platforms` choice
  (All/Windows/macOS/WebGL) fans out into a matrix; builds, pushes to itch.io via Butler
  (`vars.ITCH_USER` / `vars.ITCH_GAME` + `secrets.BUTLER_API_KEY`), optionally publishes a
  GitHub Release when `release_tag` is set, then posts a Gemini-generated Discord embed.
  Windows/WebGL run on their own runners; **macOS and WebGL both build on `macos-latest`**.
- **`retrieve.yml`** — `workflow_dispatch`. Downloads Drive sprites → `Assets/_Game/02_Art/Sprite`
  and audio → `Assets/_Game/03_Audio`, commits to `chore/asset`, opens/updates a PR.
- **`track.yml`** — daily `0 23 * * *`. Detects Drive changes, commits updated
  `state/*_manifest.json` to `chore/asset`. No downloads.

Both asset workflows **delete and recreate `chore/asset` from `origin/main`** every run. It is
disposable — never branch real work from it.

## Git conventions

- Branches: `feat/…`, `fix/…`, `chore/…`, `tools/…`, `update/…`, `build/alpha/vX.Y.Z`, kebab-case
  after the prefix. Work lands via squash-merged PRs titled `… (#NNN)`; `main` is the default branch.
- Commits are Conventional Commits: `feat:`, `fix:`, `fix(scope):`, `chore(assets):`,
  `update(scope):` — lowercase, imperative. (Occasional `Feat/` typos exist; don't imitate them.)
- **Git LFS is not used** — binary assets are regular git blobs, so `git lfs install`
  is unnecessary. `.gitattributes` derives from the official
  [gitattributes Unity template](https://github.com/gitattributes/gitattributes/blob/master/Unity.gitattributes)
  but replaces its `lfs` rules with `binary` (deliberate divergence). Only commits
  older than the 2026-09 migration still hold LFS pointers.
- Never commit `Library/`, `Temp/`, `Logs/`, `build/`, or generated `.sln`/`.csproj`.
- Housekeeping note: two 10 MB `mono_crash.mem.*.blob` files are accidentally **tracked** in
  history. Don't be surprised by them in `git status`/`git ls-files`; leave removal decisions
  to the team.

---

## Python tooling (unrelated to Unity)

Both scripts are Google Drive clients sharing `core/` (`drive_client`, `discord_notifier`,
`state`, `gemini_flavor`). Install with `pip install -r requirements.txt`
(google-api-python-client, google-auth, requests).

```
python retrieve.py --drive-folder-id ID --state-file state/sprite_manifest.json \
                   --download-dir Assets/_Game/02_Art/Sprite \
                   --service-account-file sa.json --discord-webhook URL
python track.py    --drive-folder-id ID --state-file state/audio_manifest.json \
                   --project-name "repo (audio)" --service-account-b64 "$B64" --discord-webhook URL
```

Required flags (all of them): `--drive-folder-id`, `--state-file`, `--download-dir`
(retrieve only), `--discord-webhook`, plus exactly one of `--service-account-file` /
`--service-account-b64`. `state/` holds `sprite_manifest.json` and `audio_manifest.json` and is
rewritten by `track.yml` — treat it as generated, don't hand-edit.
Optional Gemini flavor text via `--gemini-api-key` / `--gemini-model` / `--gemini-persona` or the
`GEMINI_API_KEY` / `GEMINI_MODEL` / `GEMINI_PERSONA` env vars (script default model is
`gemini-3.1-flash-lite`; the deploy workflow uses `gemini-2.5-flash`).

---

## Gotchas

- `BootstrapLoader` and much of `System/` + `Game/Triggers/` are commented in Indonesian — normal,
  not an error. Match the surrounding language when editing those files.
- `IGameSystemLifecycle` is declared **inline** at the bottom of `System/Scene/BootstrapLoader.cs`,
  not in `Core/Interface/`.
- `IResettable.ResetState()` is for per-session restarts only, NOT for scene loading.
- `LocalSingleton<T>` dies on scene change — never use it for cross-scene services.
- `Singleton<T>` destroys a duplicate instance on `Awake` and nulls `Instance` in `OnDestroy`, so
  two instances in the same scene silently kill one of them.
- `MusicPlayer` listens to `SceneLoader.OnSceneLoadCompleted`, not `SceneManager.sceneLoaded` — keep
  new scene-reactive audio on the same event.
