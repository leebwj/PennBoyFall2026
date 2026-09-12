# Troubleshooting

The problems this project actually produces, and what each one means.

## The scene looks like the old version after pulling

Unity holds the open scene in memory and does not reload it when the file changes underneath. After a pull that touched `Main.unity`, reopen the scene from the Project window. If you save first, you will write your stale copy over what you just pulled.

## Everything is pink

The URP render pipeline asset is not assigned. Check Edit, Project Settings, Graphics and Quality, and make sure they point at the assets in `Assets/Settings`. If they look right, delete `Library/` and reopen the project.

## A prefab says the script is missing

The `.meta` file for that script was deleted, or the script was moved outside Unity so its id changed. Scenes and prefabs point at scripts by id, not by name, so the reference cannot recover on its own.

Undo the move, or reassign the script in the inspector and commit the fixed prefab. Move and rename files from inside Unity to avoid this.

## Nothing responds to input

Check the Console for a line naming a missing action. `InputRef` logs the action path it could not find, which usually means the binding was added on someone else's machine and the `.inputactions` file was not committed.

If the Console is clean, open `Assets/InputSystem_Actions.inputactions` and confirm the Player map still has Move, Point, Aim, Dash, Sprint, Attack, and Zoom. Running the EditMode tests checks the same thing.

## Another Unity instance is running with this project open

You cannot run a command line Unity job against a project the editor already has open, because the editor holds a lock on `Library/`. Close the editor, or do the work through the menu inside the editor instead.

Do not remove the lock file to get around this. Two processes writing `Library/` at once corrupts it.

## The project takes minutes to open

That is normal on a fresh clone, and normal after deleting `Library/`. Unity is importing every asset and building its cache. It happens once.

## Merge conflict in a .unity or .prefab file

Run `git mergetool`, which is set up to use UnityYAMLMerge. It understands the file format well enough to merge changes in different parts of a scene.

When it cannot resolve the conflict, do not hand edit the file. Take one side with `git checkout --theirs` or `--ours`, reopen the scene, redo your change in the editor, and commit. Losing ten minutes of editor work is better than a scene that loads with silent breakage.

The real fix is upstream: tell the others before you edit the scene.

## Tests fail after I renamed a field

`GameAssets.Wire` sets serialized fields by name as a string, so renaming a field breaks the builder without a compiler error. `PrefabWiringTests` exists to catch exactly this. Update the name in `Assets/Editor/GameAssets.cs`, rebuild from the PennBoy menu, and run the tests again.

## The editor is stuck compiling, or behaving strangely

Close Unity, delete `Library/`, and reopen. It is in `.gitignore` and rebuilt from scratch, so nothing is lost except the time it takes to reimport.

If that does not fix it, delete `Temp/` and `obj/` as well.

## git status shows files I did not touch

If they are under `Library/`, `Temp/`, `obj/`, `Logs/`, or `UserSettings/`, or they end in `.csproj` or `.sln`, `.gitignore` should already be hiding them. Something added an exception. Tell me rather than committing them.

If they are `.meta` files next to assets you did not touch, Unity regenerated them, usually because it imported an asset that arrived without its `.meta`. Commit them, and check with whoever added that asset that they committed the `.meta` too.
