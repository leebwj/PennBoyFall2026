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

Branch off main and name the branch after what you are building: `enemy-ranged`, `pause-menu`, `dash-trail`. Merge back through a pull request so someone else sees the diff first.

Commit messages are short and name the change, like `add ranged enemy` or `camera bounds fix`. Add a body only when the reason is not obvious from the diff. Keep one logical change per commit.

Do not force push to main, and do not rewrite a commit someone else has already pulled.

## Before you push

Open Window, General, Test Runner and run the EditMode tests. There are 20 and they finish in a few seconds. A failure is either a real break or a test that needs updating, and either way it is yours to resolve before pushing.

Then press Play and move around for ten seconds. Nothing in the test suite covers a running scene, so this is the only check on anything physics or input related.

## Scenes and prefabs do not merge

`Main.unity` is a single file that Unity rewrites in full every time it saves. When two people change it at once, git produces a conflict that is slow to resolve and easy to resolve wrongly.

Say in chat before you start editing the scene, and say when you are done. If your change can live in a prefab instead, put it there, because prefabs collide far less often.

If you do hit a conflict in a `.unity` or `.prefab` file, run `git mergetool` instead of opening the file in a text editor. When the tool cannot resolve it, the quickest fix is to take one side, redo your change in the editor, and commit again.

## The scene is generated, so tune values in code

`Assets/Editor/SceneBuilder.cs` rebuilds `Main.unity`, every prefab, and every material from scratch. Running it overwrites the prefab assets, so an inspector tweak you made by hand disappears the next time anyone rebuilds.

Change a number in the inspector while you are trying to find a value that feels right. Once you have it, move that number into the builder or into the script's serialized default, then commit both. If you are unsure where a value lives, search the builder for the field name.

## Never commit

Unity regenerates all of this, and committing it causes a conflict on every pull:

- `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`
- `.csproj`, `.sln`, and `.slnx` files
- your personal editor layout and preferences

`.gitignore` already covers them. If `git status` ever shows one, tell me rather than adding an exception.

Every asset has a `.meta` file beside it holding the id that scenes and prefabs use to find it. Move, rename, and delete assets from inside Unity so the `.meta` travels with the asset. A deleted `.meta` breaks every reference to that asset, and it shows up later as a missing script or a pink material rather than as an error where you caused it.

## Where new code goes

Logic that does not need a GameObject goes in `Assets/Scripts/Common` as a plain class, so a test can reach it without loading a scene. `HealthPool` and `CooldownTimer` are the two examples to copy.

Everything else goes in the folder for its area: `Player`, `Enemies`, `Combat`, `World`, or `Systems`. Keep MonoBehaviours thin and let them call into the plain classes.

Read input through the action map, never by polling a key:

```csharp
InputAction move = InputSystem.actions.FindAction("Player/Move");
```

New bindings go in `Assets/InputSystem_Actions.inputactions` under the Player map. `PrefabWiringTests` fails if a script asks for an action that does not exist, which is how you find out before someone else does.

## Code style

Four space indent, braces on their own line, `UpperCamel` types, `camelCase` locals and methods. Private fields the inspector needs are `[SerializeField] private` with a sensible default, so a prefab works without hand wiring.

Write a comment only for something the code cannot say: a physics quirk, an engine rule, a reason a value is what it is. Do not restate the line below it. There are two comments in the whole codebase right now, and that is about right.

Unity 6 renamed several things that older tutorials still use. Use `linearVelocity` instead of `velocity`, `FindFirstObjectByType` instead of `FindObjectOfType`, and `InputSystem.actions` instead of the old `Input` class. Anything copied from a 2023 tutorial will need these changed.

## More

- [Project structure](docs/project-structure.md), what lives in each folder
- [Adding a feature](docs/adding-a-feature.md), a worked example end to end
- [Troubleshooting](docs/troubleshooting.md), the errors you will actually hit
