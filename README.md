# PennBoy

Top down game for Fall 2026. This repo is the starting point the team builds on.

## Requirements

Unity 6000.3.23f1 with the Universal Render Pipeline. Install that exact version through Unity Hub so the project opens without a reimport or upgrade prompt.

## Opening the project

1. Clone the repo.
2. In Unity Hub choose Add, then Add project from disk, and pick the repo folder.
3. Open `Assets/Scenes/Main.unity` and press Play.

The first open takes a few minutes while Unity fills the Library folder. That folder is ignored by git, so never commit it.

## Controls

WASD or the left stick moves the player. The player faces the mouse cursor. Turn off Face Mouse on the PlayerController component to face the movement direction instead.

## Layout

- `Assets/Scripts/PlayerController.cs` moves a Rigidbody with the Input System package and turns it toward the cursor.
- `Assets/Scripts/CameraFollow.cs` keeps the camera at a fixed offset above the player.
- `Assets/Editor/SceneBuilder.cs` generates the arena, player prefab, materials, and camera setup. Run it from the PennBoy menu in the editor to rebuild `Main.unity` from scratch.
- `Assets/InputSystem_Actions.inputactions` is the project wide action map. Add new actions there instead of reading keys directly.
- `Assets/Settings` holds the URP render pipeline assets.

## Working together

Commit scenes and prefabs with their `.meta` files. Avoid two people editing the same scene at the same time, since scene files do not merge well. Put new features in their own prefabs where possible.
