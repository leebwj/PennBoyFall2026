# PennBoy 2026 Fall

A top down puzzle game about red and blue. Team: Andrew, Kaibo, Lucas, Brian.

## Setup

1. Install Unity 6000.3.23f1 through Unity Hub. That exact version.
2. Clone the repo, then in Unity Hub choose Add, Add project from disk, and pick the folder.
3. Open `Assets/Scenes/Main.unity` and press Play. The first open takes a few minutes.

## The rule

Everything switchable is red or blue, and so are you. Same color blocks you, opposite color lets you through. Shoot an opposite-color thing and you swap colors with it. Shoot a same-color thing and the shot passes through. Black walls stop everything. You start red.

## Controls

WASD to move, mouse to aim, left click to shoot.

## Scripts

All in `Assets/Scripts`.

| File | Does |
|---|---|
| `GameColor.cs` | Red or Blue |
| `Colorable.cs` | gives an object a color and paints it |
| `ColoredWall.cs` | solid when you match it, passable when you do not |
| `Bullet.cs` | flies forward, does the color swap |
| `PlayerController.cs` | movement, aiming, shooting |
| `LevelExit.cs` | the goal pad |
| `CameraFollow.cs` | isometric camera that follows you |

## Before you push

Read [CONTRIBUTING.md](CONTRIBUTING.md). Short version: do not edit `Main.unity` at the same time as someone else, always commit `.meta` files with their assets, never commit `Library/`.

Stuck on a Unity error? See [docs/troubleshooting.md](docs/troubleshooting.md).
