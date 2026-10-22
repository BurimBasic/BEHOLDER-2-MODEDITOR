# Development workflow

Use this repository as the source of truth. Build against a compatible, locally installed game; never commit game assemblies, assets, decompiled source, logs or save profiles.

- `mod/`: all project-authored runtime C# code, including the management prototype and developer test driver.
- `tools/build.ps1`: portable build using the .NET SDK and the release's dependency payload.
- `tools/AssetIndex.cs`: metadata-only Unity serialized-file v17 indexer; no third-party indexing dependency or asset extraction.
- `tools/LaunchMod.cs`: isolated game/profile launcher.
- `installer/`: user-facing installation scripts. Copies the local game, validates its assembly hash and adds a bootstrap call with Mono.Cecil.
- `vendor/`: upstream dependency license notices. Binaries are release assets, not source-tree files.

## Runtime architecture

`FloorAdmin.Boot` runs from `SaveLoadManager.Start`, installs Harmony patches and creates the persistent mod object. `ModEditorWindow` owns the UI, paused editing session and camera. `ModEditorStore` manages JSON patches, references and undo/redo. `ModEditorHandles`, `ModEditorLibrary` and `ModEditorCatalog` implement transforms, scene previews/props and the disk-wide metadata catalog.

Native interactions use cloned `PlayerAction` assets while retaining their resource name and native conditions. Scene references use scene name + hierarchy path + sibling indices. Spawned static props retain a source reference. Cross-scene imports are not yet supported. Static-batched props draw only their original submesh range; bounding-box colliders avoid reading non-readable mesh data.

`OfficeSeating` aligns Evan to a hidden sample of the original seated animation. Alignment must stop while the editor is open, otherwise dragging Evan immediately snaps him back. The sampled Ferguson copy must stay inactive with renderers disabled.

`PlayerNavigation.Lock` is reference-counted. Reading `Lock` also considers open windows and scene loading. Do not restore it by assigning a previously captured `true`, and never loop on `get_Lock()` while decrementing it. Camera scopes must not acquire navigation locks they do not own.

## Validation

Compile after changes. Test in a separate fresh profile whose path contains `revision-test-profile`; the developer command inbox then becomes `admin-test-command.txt`, separate from the user game. Never load or modify the user's save to run a test.

Useful developer commands: `editor-transform-test`, `editor-library-write`, `editor-library-read`, `fixes-verify`, `lock-debug`, `screen-capture`, `quit`. Read command implementations before using them: write tests create temporary projects and must restore the active project. `screen-capture` requires a rendered non-batch game.

The last pre-publication checks cover compilation, transform undo/redo, static prop save/restart, scene preview from the menu and the metadata index. A lock-counter test alone does not prove ordinary player walking. Report the scope of a test accurately.

## Publishing

Create the addon archive with an explicit allowlist: payload DLLs, installer, launcher, indexer, guides, licenses and checksums. Never archive a development directory recursively after installing a test game into it. Publish as an alpha release until broader gameplay regression testing passes.
