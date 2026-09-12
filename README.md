# PennBoy 2026 Fall

This is the starting point we build on, and it is deliberately small. We add things one at a time as we learn them.

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

WASD moves the player. The player turns to face the mouse cursor. Left click fires a bullet, and a bullet destroys the target it hits.

## What is in the scene

A ground plane 40 units across, a player capsule, five target cubes, a camera that follows the player from above, and one directional light. That is the whole scene.

## Scripts

Four files in `Assets/Scripts`, all of them short enough to read in one sitting:

- `PlayerController.cs` moves the Rigidbody, turns the player toward the cursor, and spawns a bullet on left click.
- `CameraFollow.cs` keeps the camera at a fixed offset above the player and eases toward it.
- `Bullet.cs` flies forward, destroys itself after three seconds, and destroys a target it touches.
- `Target.cs` is the thing a bullet can destroy.

The scene is a normal Unity scene. Nothing generates it, so change it in the editor the same way you would in any other project.

Input uses the `Input` class, the one from the tutorial. Active Input Handling is set to Both in Project Settings, so the newer Input System also works if we move to it later.

## Guides

- [Working on PennBoy](CONTRIBUTING.md), read this before your first change
- [Troubleshooting](docs/troubleshooting.md), the errors you will actually hit

## Working together

Commit scenes and prefabs with their `.meta` files. Avoid two people editing `Main.unity` at the same time, since scene files do not merge well.

Set up UnityYAMLMerge once so scene and prefab conflicts are survivable:

```
git config merge.tool unityyamlmerge
git config mergetool.unityyamlmerge.trustExitCode false
git config mergetool.unityyamlmerge.cmd '"<Unity install>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```
