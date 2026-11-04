# Ashen Road: Build Plan

EC-334 Game Design & Development final project. Unity 6000.6.0f1, C#, new Input System, Linux build first.
This plan is the spec I build from. The CRC cards and the proposal are the guide; where this plan differs from them, the reason is written next to it.

---

## 1. What "done" means

When I hand it over you can:

1. Open `GDD/Project/AshenRoad` in Unity 6000.6.0f1 and press Play from the MainMenu scene, or run the Linux build `Builds/Linux/AshenRoad.x86_64`.
2. Play all 8 levels start to finish: menu, level select, the knight with momentum movement, jump, roll, sword, three enemy types, hazards, checkpoints, heart pickups, a disaster front in every level, the boss on level 8, the princess ending and credits.
3. Lose and retry from a checkpoint, pause, change volume, quit, and come back later with your unlocked levels and best times saved.
4. Run every automated test headless with one command and see them pass.

Nothing is left as "TODO" in game code. The only things that stay "placeholder quality" are art and audio, which use free CC0 packs plus things I generate; every sprite and sound sits in a data slot so it can be swapped without touching code.

---

## 2. Key decisions

| Decision | Choice | Why |
|---|---|---|
| Project location | `GDD/Project/AshenRoad` | Stays inside the GDD folder |
| Version control | Local `git init` only, never pushed. Commit at each milestone | Lets me roll back a bad step. Delete the `.git` folder if you don't want it |
| Scenes | 3 scenes: `Boot`, `MainMenu`, `Level`. One Level scene is reused for all 8 levels | Every level is data (LevelData), so one scene + 8 data assets is less to break than 8 hand-built scenes. SceneLoader still loads scenes asynchronously, as in the CRC |
| Level building | Each LevelData holds a text grid (one char per tile). A `LevelBuilder` turns it into a Tilemap, colliders and spawn points at load | You can retune a level in a text box; a test can check every level is valid and completable |
| How scenes/prefabs/assets are made | An editor script `AshenRoad > Rebuild Project Assets` generates prefabs, ScriptableObjects, input actions, scenes and build settings from code | I can't drag things in the editor; this makes the whole project reproducible and reviewable. You can still edit any of it in the Inspector afterwards |
| Input | New Input System (bundled 1.20 package), one `.inputactions` asset with `Gameplay` and `UI` maps, wrapped by `InputReader` | Your choice; also matches the CRC's action-map switching |
| UI | uGUI (Canvas) with a pixel font | Stable in batch mode; TextMeshPro needs a manual import step |
| Physics | Rigidbody2D + BoxCollider2D/CapsuleCollider2D, layers + collision matrix | Lecture 11 |
| Save | `PlayerPrefs` behind an `ISaveStore` interface | Simple, cross-platform; the interface lets tests use an in-memory store |

Controls (keyboard; gamepad added as a second binding for free):

| Action | Keys |
|---|---|
| Move | A / D, Left / Right arrows |
| Jump (hold for higher) | Space, W, Up |
| Roll | Left Shift |
| Attack | J, Left mouse, 0 (top row) and Numpad 0 |
| Pause | Esc |
| Menu navigation | Arrows / mouse, Enter, Esc to go back |

---

## 3. Course concepts the code applies

Each one names the lecture it comes from, so the code can be defended in the presentation.

| Concept | Where it shows up | Lecture |
|---|---|---|
| Component model / composition over inheritance | Knight = PlayerController + PlayerMovement + PlayerCombat + Health + Rigidbody2D. Enemies reuse Health, EnemyMotor, EnemyPerception, Hitbox | 6, 8 |
| MonoBehaviour lifecycle (Awake for own refs, Start for others, OnEnable/OnDisable for event subscribe/unsubscribe) | Every script | 6, 8 |
| Caching GetComponent, no Find in Update | Every script | 7b, 8 |
| Encapsulation: private state, public methods, properties | Health owns hp rules; nothing writes hp directly | 9, 10 |
| Inheritance + polymorphism (abstract / virtual / override) | PlayerState, EnemyState, BossState, Hazard subclasses | 9 |
| Interfaces | IState, IDamageable, ISaveStore | 8b, 9 |
| Low coupling / one owner per rule | Damage always goes through IDamageable; UI only listens to events | 10 |
| ScriptableObjects as shared data (never mutate at runtime) | PlayerStats, EnemyData, BossData, LevelData, LevelCatalog, AudioLibrary | 8b |
| C# events / Action delegates | Health events, GameEvents channel | 7, 8b |
| Coroutines for timed sequences | Invulnerability blink, boss intro, fades, debris telegraph | 7, 7b |
| Read input in Update, apply in FixedUpdate (latching) | InputReader + PlayerMovement | 12 |
| Jump from height and time, variable jump height, coyote time, jump buffer | PlayerMovement | 12 |
| Camera follow with level bounds | CameraFollow | 12 |
| Triggers vs solid collisions, layers, OnTriggerEnter2D | Hitboxes, pickups, checkpoints, kill zones | 11 |
| Formal elements (players, objectives, rules, resources, conflict, boundaries, outcome) | Already in the proposal; the game implements each one | 4 |

---

## 4. Design patterns

The proposal promised five. I keep all five and add four more where the code genuinely needs them.

| Pattern | Class(es) | Responsibility in Ashen Road |
|---|---|---|
| **Singleton** | `GameManager`, `AudioManager` | The only two. Created once in the Boot scene, `DontDestroyOnLoad`, duplicates destroy themselves. GameManager owns game flow (current level, checkpoint, state); AudioManager plays music and SFX |
| **Observer** | `GameEvents` (static event channel), instance events on `Health`, `DisasterFront`, `Checkpoint`, `BossController` | HUD, audio and GameManager react to deaths, damage, checkpoints, state changes without the sender knowing them. Subscribe in OnEnable, unsubscribe in OnDisable |
| **State** | `IState` + `StateMachine`; player states (Grounded, Airborne, Rolling, Attacking, Hurt, Dead), enemy states (Patrol, Chase, Attack, Stunned, Dead), boss states (Intro, PhaseOne, PhaseTwo, Defeated); game flow `GameState` | Behaviour changes by state instead of one giant if/else |
| **Component** | All gameplay objects | A Brute is the same components as a Shambler with different EnemyData |
| **Factory** | `EnemyFactory` | The only place enemies are instantiated: level spawn tables and boss summons go through it |
| **Object Pool** (added) | `ObjectPool<T>` used for hit sparks, dust puffs, debris rocks, damage flashes | These spawn and despawn constantly; the guidelines name Object Pool for exactly this |
| **Template Method** (added) | `Hazard` (abstract) with `FirePatch`, `FallingDebris`, `Spikes` | Base class runs the fixed cycle Telegraph -> Activate -> Deactivate; subclasses fill in each step |
| **Facade** (added) | `PlayerController` | Other systems talk to one knight object, not to its five internal components |
| **Data-driven design / Type Object** (added) | `EnemyData`, `LevelData`, `BossData` ScriptableObjects | A new enemy type or level is a data asset, not new code |

---

## 5. Architecture

### 5.1 Folder layout

```
AshenRoad/
  Assets/
    _Project/
      Scripts/
        Core/        GameManager, GameEvents, GameState, SceneLoader, StateMachine, IState, ObjectPool, SaveSystem
        Input/       InputReader, AshenRoadControls.inputactions
        Data/        PlayerStats, EnemyData, BossData, LevelData, LevelCatalog, SpawnEntry, AudioLibrary
        Combat/      IDamageable, DamageInfo, DamageType, Health, Hitbox, KnockbackReceiver
        Player/      PlayerController, PlayerMovement, PlayerCombat, PlayerAnimator, States/*
        Enemies/     EnemyAI, EnemyMotor, EnemyPerception, EnemyFactory, States/*
        Boss/        BossController, States/*
        Level/       LevelController, LevelBuilder, LevelLayout(parser), DisasterFront, Checkpoint, ExitGate,
                     KillZone, MovingPlatform, HeartPickup, Hazards/*
        Camera/      CameraFollow, CameraShake
        Audio/       AudioManager
        UI/          UIFlowController, HUDController, HeartDisplay, FrontDistanceMeter, PausePanel,
                     GameOverPanel, LevelCompletePanel, MainMenuController, LevelSelectPanel,
                     SettingsPanel, ScreenFader, EndingController, CreditsPanel
        Visual/      SpriteAnimator (code-driven frame/bob animation), Flash, ParallaxLayer
      Editor/        ProjectAssetBuilder (generates everything), BuildScript (Linux build)
      Data/          generated ScriptableObject assets (Levels/, Enemies/, Player, Boss, Catalog, Audio)
      Prefabs/       generated prefabs
      Scenes/        Boot, MainMenu, Level
      Art/           sprites, tiles, font
      Audio/         music, sfx
    Tests/
      EditMode/      unit tests (assembly AshenRoad.Tests.EditMode)
      PlayMode/      scene tests (assembly AshenRoad.Tests.PlayMode)
  Builds/Linux/
  run_tests.sh, build_linux.sh, README.md
```

Assembly definitions: `AshenRoad.Runtime`, `AshenRoad.Editor`, and the two test assemblies. This keeps compile times short and lets tests reference the game code.

### 5.2 Main classes and what changed from the CRC

Kept from the CRC (with the responsibilities on the cards): IState, StateMachine, IDamageable, GameEvents, PlayerStats, EnemyData, BossData, LevelData, LevelCatalog, GameManager, SceneLoader, InputReader, Health, Hitbox, PlayerController, PlayerMovement, PlayerCombat, all player/enemy/boss states, EnemyAI, EnemyPerception, EnemyMotor, EnemyFactory, BossController, LevelController, DisasterFront, ExitGate, Checkpoint, KillZone, Hazard, FirePatch, FallingDebris, HeartPickup, CameraFollow, EndingController, UIFlowController, HUDController, HeartDisplay, GameOverPanel, LevelCompletePanel.

Changes and additions:

| Change | Reason |
|---|---|
| `DamageInfo` is a struct with amount, `DamageType` (Sword, Contact, Hazard, Fall, Front), source, knockback direction, `IsLethal` | The CRC uses it everywhere but never defines it. Brute's sword immunity becomes "EnemyData.immunities contains Sword" |
| `EnemyStunnedState` added | A Brute hit by the sword reacts (flinch, "clang") instead of nothing; it also gives hit feedback for Runners |
| `LevelLayout` (pure C# parser) + `LevelBuilder` added | Turns the text grid into tiles and spawn points; parser is unit-tested |
| `AudioManager` + `AudioLibrary` added | In the proposal, missing from the CRC |
| `SaveSystem` + `ISaveStore` added | Level unlocks and best times must survive a restart |
| `GameState` enum + transition table in GameManager | The CRC's "validate state transitions"; the table is unit-tested |
| `MovingPlatform`, `Spikes` added | Level 6 needs moving platforms; spikes give non-timed hazards |
| `MainMenuController`, `LevelSelectPanel`, `SettingsPanel`, `PausePanel`, `ScreenFader`, `CreditsPanel` added | Menus the proposal mentions but the CRC lacks |
| `ObjectPool<T>`, `CameraShake`, `SpriteAnimator`, `ParallaxLayer` added | Feedback and visuals |
| `PlayerAnimator` added | Keeps visuals out of movement code |
| Princess is spawned by `EndingController`, not a separate scene | One less scene; ending runs after the boss in level 8 |

### 5.3 How a level runs (scenario walk-through)

1. `GameManager.StartLevel(i)` sets state `Loading`, `SceneLoader` loads the `Level` scene async while `ScreenFader` fades out.
2. `LevelController.Start` reads `LevelCatalog[i]`, `LevelBuilder` builds tiles, spawns checkpoints, pickups, hazards, platforms, exit gate, kill zone. `EnemyFactory` spawns the spawn table (spawned ahead of the knight in chunks, despawned once far behind the front, to keep things light).
3. Knight is placed at the start or the last checkpoint; `DisasterFront` is placed `respawnFrontOffset` tiles behind; `CameraFollow` gets bounds. State -> `Playing`.
4. During play: the front advances in FixedUpdate with a speed ramp; the HUD's distance meter listens to `OnDistanceChanged`. Touching the front, falling into a pit or hitting 0 hearts raises `GameEvents.PlayerDied(cause)`.
5. Death: state `GameOver`, panel shows the cause; Retry restarts from the checkpoint, Restart from the start.
6. ExitGate reached: state `LevelComplete`, `LevelResult` (time, hearts) built, next level unlocked, best time saved.
7. Level 8: the front stops at the arena; `BossController` locks the arena, runs Intro -> PhaseOne -> PhaseTwo (below 50% hp) -> Defeated; then `EndingController` walks the knight to the princess, plays the reunion and goes to credits; state `GameFinished`.

Game states: `Boot, MainMenu, Loading, Playing, Paused, GameOver, LevelComplete, Ending, GameFinished`. Pause sets `Time.timeScale = 0` and switches the input map to UI.

---

## 6. Gameplay specification

### 6.1 Knight (PlayerStats defaults, tuned during build)

| Stat | Value |
|---|---|
| Max speed / acceleration / deceleration | 8 u/s / 60 u/s² / 70 u/s² (air control 65%) |
| Jump | 3.2 tiles high, 0.38 s to apex (gravity and jump velocity computed from these, as in Lecture 12); releasing early cuts the jump |
| Coyote time / jump buffer | 0.1 s / 0.12 s |
| Roll | 0.35 s, 12 u/s, invulnerable for the whole roll, passes through enemies (layer change), 0.8 s cooldown |
| Sword | 1 damage, 0.12 s active window, 0.35 s cooldown, small lunge; works in the air |
| Health | 3 hearts; 1 s invulnerability after a hit with blink; knockback |

### 6.2 Enemies (EnemyData)

| | Shambler | Runner | Brute |
|---|---|---|---|
| HP | 2 | 1 | 5 (immune to Sword) |
| Speed patrol / chase | 0.8 / 1.4 | 1.5 / 6.5 (charge) | 0.8 / 2.0 |
| Sight range | 5 | 9 | 6 |
| Attack | 1 dmg swipe, 0.5 s telegraph | charge into contact | 1 dmg slam, 0.8 s telegraph, big knockback |
| Killed by | Sword, hazards, pits | Sword, hazards, pits | Hazards (fire, debris), pits only |

All enemies: patrol around their spawn, turn at ledges and walls, chase when they see the knight (line-of-sight raycast), forget after 2 s out of sight, contact damage 1.

### 6.3 Hazards

* **FirePatch**: flickers on/off on a cycle (telegraph glow -> burning -> off); damages anyone inside, including enemies (lure Brutes).
* **FallingDebris**: triggered when the knight passes; shadow telegraph 0.8 s, then a rock falls, hits once, breaks. Damages enemies too.
* **Spikes**: always on, 1 damage + knockback up.
* **Pits / KillZone**: lethal fall, for enemies too.
* **MovingPlatform**: horizontal or vertical path; carries the knight.

### 6.4 Disaster front

A full-height wall moving right. `speed = baseSpeed + ramp * timeInLevel`, capped at `maxSpeed`. It catches up faster when far behind (rubber band, capped) so it stays a threat, and never moves backwards. Touching it is instant death with cause "Caught by the fire / flood / ...". Visual changes per level (fire, flood, collapsing floor, fissures, cave-in, lightning, fire+debris, crumbling wall) via a color/particle preset in LevelData. It pauses for the boss fight.

### 6.5 Levels

| # | Level | Front | New | Length (tiles) | Front speed |
|---|---|---|---|---|---|
| 1 | Burning Village | wall of fire | Tutorial signs; Shamblers only | ~180 | slow |
| 2 | Broken Bridge | collapsing floor | Pits; roll tutorial; debris | ~200 | slow+ |
| 3 | Drowned Fields | rising flood | Runners | ~220 | medium |
| 4 | Graveyard Gate | fissures | Brutes; fire patches to lure them | ~230 | medium |
| 5 | Collapsing Mines | cave-in | Falling rocks, low ceilings | ~240 | medium+ |
| 6 | Storm Cliffs | lightning | Moving platforms, running jumps | ~250 | fast |
| 7 | Ruined City | fire + debris | Dense mixed groups | ~260 | fast |
| 8 | Castle Ramparts | crumbling wall | Short run, then boss arena, princess | ~140 + arena | medium, stops at arena |

Each level has 2 to 3 checkpoints and 2 to 4 heart pickups. Target 3 to 5 minutes each.

Grid legend (one char = one 1x1 tile): `#` ground, `=` one-way platform, `^` spikes, `F` fire patch, `D` debris trigger, `M`/`V` moving platform (horizontal/vertical), `S` Shambler, `R` Runner, `B` Brute, `H` heart, `C` checkpoint, `P` knight start, `E` exit gate, `K` boss, `Q` princess, `!` tutorial sign, `.` air. Pits are just missing ground; the KillZone runs under the whole level.

### 6.6 Boss: "The Ash Warden"

* 20 HP. Intro: arena walls close, roar, camera shake.
* Phase one: walks, telegraphed overhead swing (0.7 s), summons 2 Shamblers every 12 s (max 3 alive).
* Phase two (at 50% HP): faster swings, a ground-charge across the arena with a 0.6 s telegraph, shorter recovery, summons every 18 s.
* Defeated: arena opens, `GameEvents.BossDefeated`, ending starts.

### 6.7 Feedback

Hit stop (0.05 s), camera shake, white flash on hit, dust on jump/land/roll, sparks on sword hit, "clang" on Brute block, heart animations, fades between screens, music per level and for the boss, SFX for every action.

---

## 7. Art and audio

* First choice: **Kenney CC0 packs** (free for any use, no attribution needed): *Tiny Dungeon* (knight, undead, princess, boss) and *Pixel Platformer* (tiles), *Impact Sounds*/*Interface Sounds*/*RPG Audio* (SFX), and a free pixel font. Downloaded from kenney.nl into `Assets/_Project/Art` and `Audio`, with a `CREDITS.md`.
* If a download fails or something is missing (music especially), I generate it: simple pixel sprites with a Python script, chiptune music loops and SFX with a small synth script. I don't stop for art.
* Sprites are animated in code (`SpriteAnimator`: idle bob, walk squash, attack lean, hurt flash) so single-frame art still feels alive; frame lists are supported if real animation sheets arrive later.

---

## 8. Tests

Run with `./run_tests.sh` (Unity batch mode, both platforms, writes XML results).

**EditMode (pure logic, fast)**
* StateMachine: enter/exit order, ignore re-entry unless forced, revert to previous, tick forwarding.
* Health: damage, death once, heal cap, invulnerability, immunity by DamageType, lethal bypass, events fire with the right values.
* GameManager transition table: every allowed transition passes, illegal ones are rejected.
* LevelLayout parser: legend, sizes, spawn extraction, errors on bad input.
* Every LevelData asset: exactly one start and exit, checkpoints in order, spawn table uses known enemies, boss only on level 8.
* Level completability: a grid search with the knight's real jump height and distance proves the exit is reachable from the start in every level (and from each checkpoint).
* DisasterFront speed math (ramp, cap, rubber band, never backwards).
* LevelCatalog lookups, SaveSystem with in-memory store (unlocks, best times), EnemyData immunities, ObjectPool reuse.
* Input actions asset: every action exists and has the agreed bindings (including the 0 key).

**PlayMode (real scenes and physics)**
* Knight falls onto ground and becomes Grounded; walking reaches max speed; jump reaches the designed height (±10%); coyote jump works; roll grants invulnerability.
* Sword kills a Shambler in 2 hits, a Runner in 1, does nothing to a Brute (Stunned instead); fire kills a Brute.
* Enemy patrol -> chase -> attack transitions when the knight is placed in range.
* Front catches a standing knight -> GameOver with cause "front".
* Pit fall -> death; checkpoint respawn places the knight at the checkpoint with the front reset.
* Heart pickup heals; ExitGate -> LevelComplete and next level unlocked.
* Boss enters phase two at the threshold and is defeated -> ending runs -> GameFinished.
* Smoke test: every one of the 8 levels loads from the menu flow, builds, runs 5 s with no errors or exceptions logged.
* Screenshot test: renders each level and the menus to PNG in `TestResults/Screens` so I (and you) can eyeball the visuals.

Gate: the build only counts as done when all tests pass and the Linux build launches and reaches the main menu with an empty error log.

---

## 9. Build order

Each milestone ends with tests green and a local git commit.

1. **Project skeleton**: Unity project, packages (Input System, 2D Sprite/Tilemap, uGUI, Test Framework), layers, physics matrix, asmdefs, test runner script.
2. **Core**: StateMachine, GameEvents, GameManager, SceneLoader, SaveSystem, ObjectPool + EditMode tests.
3. **Assets**: download/generate art, audio, font; import settings (pixel art, point filter).
4. **Player**: InputReader + actions asset, PlayerMovement, PlayerCombat, Health, Hitbox, player states, CameraFollow + tests.
5. **Levels**: LevelLayout, LevelBuilder, LevelController, Checkpoint, ExitGate, KillZone, HeartPickup, DisasterFront; level 1 playable end to end.
6. **Enemies**: EnemyData, EnemyAI + states, Perception, Motor, EnemyFactory; Shambler, Runner, Brute + tests.
7. **Hazards**: FirePatch, FallingDebris, Spikes, MovingPlatform.
8. **UI and flow**: Boot, MainMenu, level select, settings, HUD, pause, game over, level complete, fades.
9. **Audio**: AudioManager, music per level, all SFX hooked to events.
10. **Content**: levels 2 to 8 designed and validated by the completability test.
11. **Boss and ending**: BossController + states, arena, princess, credits.
12. **Polish**: juice (shake, hit stop, particles, parallax), balance pass from automated metrics (front vs knight speed margins).
13. **Verification**: full test run, screenshot review, Linux build, launch check.
14. **Handover**: README (how to open, run, build, test, controls, where to tune things) and a report of anything I couldn't verify.

---

## 10. Risks and how I handle them

| Risk | Handling |
|---|---|
| Unity license fails in batch mode | Check first thing; if it fails I'll ask you to sign in to Unity Hub once |
| Asset downloads blocked | Generate art/audio instead (section 7) |
| Physics feel can't be judged by tests | I test numbers (heights, speeds, timings); you playtest feel afterwards, and all tuning lives in PlayerStats/LevelData assets |
| Balance (front too fast/slow) | Completability test + a margin check: at the knight's average speed the front must stay behind by a set gap; you fine-tune by playing |
| Large scope | Order above puts a complete playable level 1 early, so the game is always in a runnable state |
