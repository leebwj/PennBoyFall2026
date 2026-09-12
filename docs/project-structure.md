# Project structure

Where everything lives and which parts are written by hand.

## Top level

```
Assets/            everything Unity loads
Packages/          package list, edit through the Package Manager window
ProjectSettings/   project wide settings, edit through Edit > Project Settings
docs/              these guides
Library/           Unity's cache, not in git, safe to delete
```

`Library/` is rebuilt from `Assets/` and `ProjectSettings/` whenever it is missing. It is several hundred megabytes and it is the reason the first open of a fresh clone takes a few minutes. Deleting it is the standard fix for a project that has gone strange.

## Assets

```
Assets/
  Scripts/       runtime code, the PennBoy assembly
    Common/      plain C# with no UnityEngine dependency, plus shared components
    Player/      movement, aiming, firing, death and respawn
    Enemies/     chase behaviour and the spawner
    Combat/      projectiles
    World/       pickups
    Systems/     camera rig, score and respawn manager, debug overlay
  Editor/        editor only code, the PennBoy.Editor assembly
  Tests/
    EditMode/    tests, the PennBoy.Tests.EditMode assembly
  Scenes/        Main.unity, generated
  Prefabs/       generated
  Materials/     generated
  Settings/      URP render pipeline assets, edit in the inspector
  InputSystem_Actions.inputactions   the action map
```

## Three assemblies

Each of `Assets/Scripts`, `Assets/Editor`, and `Assets/Tests/EditMode` holds an `.asmdef` file that makes it a separate assembly.

The split is not decoration. A test assembly cannot reference Unity's default assembly, the one that holds loose scripts. Without `PennBoy.asmdef`, none of the game code would be reachable from a test. The split also means editing a script only recompiles its own assembly, which is noticeably faster than recompiling everything.

`PennBoy.Editor` and `PennBoy.Tests.EditMode` reference `PennBoy`. Nothing references them back, and nothing in `Assets/Scripts` may use `UnityEditor`, because that type does not exist in a build.

## What is generated

`Assets/Editor/SceneBuilder.cs` builds the scene. `Assets/Editor/GameAssets.cs` builds the materials, the prefabs, and the particle effects it uses. Run both from the PennBoy menu in the editor.

Everything they produce is committed, so a fresh clone opens and plays without running anything. You only need the builder when you want to change the layout or start the scene over.

The builder overwrites prefabs, so treat it as the source of truth. See the note in [CONTRIBUTING](../CONTRIBUTING.md) about where to put a value once you have tuned it.

`GameAssets.Wire` sets private `[SerializeField]` fields by their field name as a string. Renaming a serialized field therefore breaks the builder silently, which is why `PrefabWiringTests` loads each prefab and checks that its references are filled in.

## The scripts

`Common/HealthPool.cs` holds current and maximum health, clamps at both ends, and refuses negative input. No Unity types, so tests call it directly.

`Common/CooldownTimer.cs` is the same idea for anything on a cooldown. The dash, the fire rate, and the interval between contact hits all use one.

`Common/Damageable.cs` puts a health pool on a GameObject and raises `Died`. The player, enemies, and anything else that can be destroyed use it, which is why damage sources do not care what they hit.

`Common/InputRef.cs` looks up an action and logs a clear error if it is missing, instead of throwing a null reference every frame.

`Player/PlayerController.cs` reads movement, sprint, and dash, and turns the player toward the cursor by intersecting the cursor ray with a flat plane at the player's height. That plane is why aiming is not blocked by walls.

`Player/PlayerShooter.cs` spawns projectiles on a fire rate cooldown. `Player/PlayerLife.cs` listens for death, hides the player, and puts it back at the respawn point.

`Enemies/EnemyBrain.cs` moves toward the player when within sight range and damages on contact. `Enemies/EnemySpawner.cs` keeps a set number alive.

`Combat/Projectile.cs` moves forward, ignores whoever fired it, and applies damage to the first `Damageable` it touches.

`World/Pickup.cs` heals or scores, then hides itself and comes back after a delay.

`Systems/CameraRig.cs` follows the player, leads slightly toward where they are facing, clamps to the arena so the camera never shows past the walls, zooms on scroll, and shakes when you fire. `Systems/GameManager.cs` holds score, death count, and the respawn point. `Systems/DebugHud.cs` draws the corner readout and is meant to be replaced when we build real UI.

## Input

`Assets/InputSystem_Actions.inputactions` is set as the project wide action asset, so any script can reach it through `InputSystem.actions` without a reference in the inspector.

The Player map has Move, Point, Aim, Dash, Sprint, Attack, and Zoom, plus several actions left over from the Unity template that nothing reads yet. Point is the mouse position and Aim is the gamepad right stick.

Active Input Handling is set to Both in Project Settings. That keeps old `Input.GetKey` code compiling if someone pastes it in, but new code should use the action map so bindings stay in one place and gamepad support comes for free.
