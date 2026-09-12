# PennBoy 2026 Fall

Top down game for the Fall 2026 semester. This repo is the starting point the team builds on.

## Team

Andrew, Kaibo, Lucas, Brian

## Requirements

Unity 6000.3.23f1 with the Universal Render Pipeline. Install that exact version through Unity Hub so the project opens without a reimport or upgrade prompt.

## Opening the project

1. Clone the repo.
2. In Unity Hub choose Add, then Add project from disk, and pick the repo folder.
3. Open `Assets/Scenes/Main.unity` and press Play.

The first open takes a few minutes while Unity fills the Library folder. That folder is ignored by git, so never commit it.

## Controls

WASD or the left stick moves the player. The player faces the mouse cursor, or the right stick on a gamepad. Shift sprints, space dashes, left click fires, and the scroll wheel zooms the camera. Bindings live in `Assets/InputSystem_Actions.inputactions` under the Player map, and code reads them through `InputSystem.actions` rather than polling keys.

## What is in the test scene

`Main.unity` is a plain 40 by 40 walled arena, deliberately bare so it is easy to change:

- The player starts near the south wall.
- Six boxes to move around and shoot over.
- Three enemies, plus a spawner that keeps three alive.
- One health pickup and one score pickup, both recharging after six seconds.

Dying respawns you at the start after 1.2 seconds. Health, score, deaths, and dash charge are drawn in the corner by a debug overlay.

## Code layout

Runtime scripts are in the `PennBoy` assembly under `Assets/Scripts`:

- `Common/HealthPool.cs` and `Common/CooldownTimer.cs` are plain C# classes with no Unity dependency, so EditMode tests cover them directly.
- `Common/Damageable.cs` wraps a health pool in a MonoBehaviour and raises `Changed` and `Died`. The player, enemies, and target dummies all use it.
- `Player/` holds movement and aiming, firing, and death and respawn.
- `Enemies/` holds the chase behaviour and the spawner.
- `World/` holds pickups.
- `Systems/` holds the camera rig, the score and respawn manager, and the debug overlay.

`Assets/Editor/SceneBuilder.cs` regenerates the whole scene and `Assets/Editor/GameAssets.cs` creates the materials and prefabs it uses. Run it from the PennBoy menu in the editor. Everything it produces is committed, so you only need it to change the layout or start over. Keeping the scene reproducible from code is also why none of these files should be edited as YAML by hand.

Tests are in `Assets/Tests/EditMode`. Run them from Window, General, Test Runner.

## Working together

Commit scenes and prefabs with their `.meta` files. Avoid two people editing `Main.unity` at the same time, since scene files do not merge well. Put new mechanics in their own prefabs and scripts where you can, and keep logic that does not need a GameObject in a plain class so it can be tested.

Set up UnityYAMLMerge once so scene and prefab conflicts are survivable:

```
git config merge.tool unityyamlmerge
git config mergetool.unityyamlmerge.trustExitCode false
git config mergetool.unityyamlmerge.cmd '"<Unity install>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```
