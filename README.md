# Ashen Road

A 2D action platformer for EC-334 Game Design and Development, built in Unity 6000.6.0f1 with C#.
A knight runs the royal road through a collapsing kingdom to reach the princess before the disaster front behind him catches up.

## Play it

**Built game (Linux):**
```
./Builds/Linux-Gothicvania/AshenRoad.x86_64   # current look (Gothicvania art)
./Builds/Linux/AshenRoad.x86_64               # first version (Kenney art), kept for comparison
```

**In the Unity editor:** open this folder in Unity Hub (version 6000.6.0f1), open `Assets/_Project/Scenes/MainMenu.unity` and press Play.
You can also open `Level.unity` and press Play to jump straight into a level; set `Editor Level Index` on the `Level` object to pick which.

## Controls

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | A / D or Left / Right | Left stick, D-pad |
| Jump (hold for higher) | Space, W, Up | South button |
| Roll (invulnerable, passes through enemies) | Left / Right Shift | East button, RB |
| Sword | J, 0, Numpad 0, Left click | West button |
| Pause | Esc, P | Start |
| Menus | Arrows / mouse, Enter, Esc to go back | D-pad, South, East |

## The game

* 8 levels: Burning Village, Broken Bridge, Drowned Fields, Graveyard Gate, Collapsing Mines, Storm Cliffs, Ruined City, Castle Ramparts.
* A disaster front advances from the left in every level; it speeds up over time and catches up faster if you get far ahead.
* Enemies: **Shambler** (2 hits), **Runner** (1 hit, charges), **Brute** (immune to the sword: jump over, roll through, or lure into fire).
* Hazards: fire patches (hurt enemies too), falling debris, spikes, pits, moving platforms.
* 3 hearts, heart pickups, 2 checkpoints per level, retry from the last checkpoint.
* Level 8 ends with the Ash Warden (two phases, summons Shamblers, charges in phase two), then the princess and the credits.
* Progress, best times and volume are saved. Level Select unlocks as you go; Settings has Reset Progress.

## Commands

| Script | What it does |
|---|---|
| `./run_tests.sh` | Runs all EditMode and PlayMode tests headless; prints a summary. `./run_tests.sh EditMode` runs one platform. Results in `TestResults/` (XML and screenshots in `TestResults/Screens`). |
| `./build_linux.sh [folder]` | Builds the Linux player into `folder` (default `Builds/Linux`). |
| `./rebuild_assets.sh` | Regenerates data assets, prefabs and scenes from code (same as the menu item **Ashen Road > Rebuild Project Assets**). |
| `python3 Tools/compose_levels.py` | Regenerates the 8 level layouts and checks each is finishable. |
| `python3 Tools/gen_art.py`, `python3 Tools/gen_sfx.py` | Regenerate the procedural art and sound effects. |
| `python3 Tools/extract_gothicvania.py <packs>` | Re-cuts the Gothicvania packs (unzipped) into tiles, character frames, backgrounds and props. |

Close the project in the Unity editor before running these scripts; Unity can't open the same project twice.

## Where to change things

| To change | Edit |
|---|---|
| Knight feel (speed, jump, roll, sword, hearts) | `Assets/_Project/Data/PlayerStats.asset` |
| Enemy stats | `Assets/_Project/Data/Enemies/*.asset` |
| Boss | `Assets/_Project/Data/Boss/AshWarden.asset` |
| A level's look, front speed, hints, music | `Assets/_Project/Data/Levels/LevelN.asset` |
| A level's layout | `Assets/_Project/Levels/levelN.txt`, then rebuild assets (legend in `LevelLayout.cs`) |
| Sounds and music | `Assets/_Project/Data/AudioLibrary.asset` |
| UI colours and fonts | `Assets/_Project/Data/UIStyle.asset` |

Inspector edits to data assets are overwritten by `rebuild_assets.sh`; to keep a change permanently, also put it in `Assets/_Project/Editor/ProjectAssetBuilder.Data.cs` (or the level `.txt`).

## Code map

```
Assets/_Project/Scripts/
  Core/      GameManager (Singleton), GameEvents (Observer), StateMachine + IState (State), GameState rules,
             SceneLoader, SaveSystem, ObjectPool, GameBootstrap
  Input/     InputReader (only class that touches the Input System), InputBindings (action maps)
  Data/      ScriptableObjects: PlayerStats, EnemyData, BossData, LevelData, LevelCatalog, LevelPrefabs, AudioLibrary
  Combat/    IDamageable, DamageInfo, Health + HealthModel, Hitbox, ContactDamage
  Player/    PlayerController (Facade), PlayerMovement, PlayerCombat, States/
  Enemies/   EnemyAI, EnemyMotor, EnemyPerception, EnemyFactory (Factory), States/
  Boss/      BossController, States/
  Level/     LevelController, LevelBuilder, LevelLayout, LevelReachability, DisasterFront + FrontModel,
             Checkpoint, ExitGate, KillZone, HeartPickup, MovingPlatform, ArenaWall, EndingController, Hazards/ (Template Method)
  Cameras/   CameraFollow
  Audio/     AudioManager (Singleton)
  UI/        UIFlowController and one panel class per screen, HUD, HeartDisplay, ScreenFader
  Visual/    code-driven animation, EffectPlayer (pooled particles)
Assets/_Project/Editor/   ProjectAssetBuilder (generates everything), BuildScript
Assets/Tests/             EditMode and PlayMode tests
```

## Credits

* Characters, tiles, backgrounds and scenery: "Gothicvania" packs by Luis Zuno (ansimuz), CC0 (Town, Cemetery, Church, Swamp, Rocky Pass, Junk Wastelands, Bridge expansion, Patreon collection).
* Items, fonts, UI and RPG sounds (and the first version's characters): Kenney (kenney.nl), CC0.
* Music: Juhani Junkala, "Retro Game Music Pack", CC0.
* Tiles, backgrounds, disaster fronts and the remaining sound effects are generated by the scripts in `Tools/`.
