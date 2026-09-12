# Working on PennBoy

Four of us share one Unity project, and Unity projects break in ways that ordinary code does not. Read this before your first change.

## Setup, once

Install Unity 6000.3.23f1 from Unity Hub. Not a different patch version. A newer editor silently upgrades project files when it opens an older project, and the result is a diff across ProjectSettings and every asset it touched that nobody can review.

Set the merge tool for scene and prefab files:

```
git config merge.tool unityyamlmerge
git config mergetool.unityyamlmerge.trustExitCode false
git config mergetool.unityyamlmerge.cmd '"<your Unity install>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"'
```

On my machine that path is `D:\Unity\6000.3.23f1\Editor\Data\Tools\UnityYAMLMerge.exe`. Yours differs by drive and version folder.

## Branches and commits

Branch off main and name the branch after what you are building: `enemy-chase`, `pause-menu`, `dash-move`. Merge back through a pull request so someone else sees the diff first.

Commit messages are short and name the change, like `add enemy chase` or `camera offset fix`. Add a body only when the reason is not obvious from the diff. Keep one logical change per commit.

Do not force push to main, and do not rewrite a commit someone else has already pulled.

## Before you push

Press Play and move, turn, and shoot for ten seconds. There is no test suite yet, so this is the only check we have.

Watch the Console while you do it. A yellow warning you have not seen before is worth understanding before you push it to everyone else.

## Scenes and prefabs do not merge

`Main.unity` is a single file that Unity rewrites in full every time it saves. When two people change it at once, git produces a conflict that is slow to resolve and easy to resolve wrongly.

Say in chat before you start editing the scene, and say when you are done. If your change can live in a prefab or a script instead, put it there, because those collide far less often.

If you do hit a conflict in a `.unity` or `.prefab` file, run `git mergetool` instead of opening the file in a text editor. When the tool cannot resolve it, the quickest fix is to take one side, redo your change in the editor, and commit again.

## Never commit

Unity regenerates all of this, and committing it causes a conflict on every pull:

- `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`
- `.csproj`, `.sln`, and `.slnx` files
- your personal editor layout and preferences

`.gitignore` already covers them. If `git status` ever shows one, tell me rather than adding an exception.

Every asset has a `.meta` file beside it holding the id that scenes and prefabs use to find it. Move, rename, and delete assets from inside Unity so the `.meta` travels with the asset. A deleted `.meta` breaks every reference to that asset, and it shows up later as a missing script or a pink material rather than as an error where you caused it.

## Adding code

New scripts go in `Assets/Scripts`. Keep one class per file and name the file after the class, which Unity requires for a MonoBehaviour.

Values you want to tune from the inspector go in a `[SerializeField] private` field with a sensible default, so the object still works if nobody touches it:

```csharp
[SerializeField] private float moveSpeed = 6f;
```

Read input the way `PlayerController` does, with the `Input` class, until we decide together to move to the Input System. Mixing both in one project makes it hard to tell where a binding lives.

Put physics in `FixedUpdate`, input and aiming in `Update`, and camera movement in `LateUpdate` so the camera follows a position the player has already reached.

## Code style

Four space indent, braces on their own line, `UpperCamel` types, `camelCase` locals and methods.

Write a comment only for something the code cannot say: a physics quirk, an engine rule, a reason a value is what it is. Do not restate the line below it. There is one comment in the whole project right now, and that is about right.

Unity 6 renamed several things that older tutorials still use. Use `linearVelocity` instead of `velocity`, and `FindFirstObjectByType` instead of `FindObjectOfType`. Anything copied from a 2023 tutorial will need these changed.

## More

- [Troubleshooting](docs/troubleshooting.md), the errors you will actually hit
