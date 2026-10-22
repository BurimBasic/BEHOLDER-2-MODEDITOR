# Beholder 2 Mod Editor

**An experimental in-game scene editor and first-floor management mod for Beholder 2.** Edit objects with XYZ gizmos, explore game asset metadata, and build saved mod projects from inside the game.

**[Download the Windows addon](https://github.com/BurimBasic/BEHOLDER-2-MODEDITOR/releases/download/v0.2.0-alpha.1/Beholder2-ModEditor-v0.2.0-alpha.1-Windows.zip)** · **[Get Beholder 2 on Steam](https://store.steampowered.com/app/761620/Beholder_2/)** · [Русская инструкция](README.ru.md) · [Report a bug](https://github.com/BurimBasic/BEHOLDER-2-MODEDITOR/issues)

> **Alpha software.** This is a playable development build, not a finished Unity Editor replacement. A compatible, separately installed copy of Beholder 2 is required. The download contains the addon and installer, **not the commercial game**.

## Install in three steps

1. Download the **Windows addon ZIP** above and extract it **outside** your original game folder. Do not download GitHub's “Source code” ZIP unless you want to build the project.
2. Run **Install.cmd** and select the folder containing your original `Beholder2.exe`. The installer copies the game into an isolated `prototype` folder, patches that copy, and indexes local asset metadata. Allow enough disk space for another game installation.
3. Run **LaunchMod.exe**. Start a **new game** for the management scenario, or press **F8** to open the editor. The launcher uses a separate `fresh-profile` save folder.

The original game installation is not patched. To update, close the addon, extract the new release into the same addon directory and rerun `Install.cmd`. Keep `fresh-profile` and `prototype/ModEditorProjects` to preserve your work. To uninstall, remove the addon directory after backing up these folders.

## What works now

| Area | Included in this alpha |
| --- | --- |
| Scene editor | Scene chooser from the main menu, hierarchy, selection, scene camera and inspector |
| Transform tools | Move, rotate and scale on X/Y/Z; local/world axes; snapping; numeric fields; undo/redo |
| Camera | Hold right mouse + WASD to fly; Q/E for down/up; Shift to move faster |
| Asset library | File-wide metadata index of Unity serialized assets: objects, meshes, materials, textures, audio and other types; search by name, type or source file |
| Prop placement | Place static models from the selected scene, transform them, delete them and save/restore them |
| Native interactions | Edit an existing action's label, price and duration while retaining its original conditions and scenario |
| Mod projects | Separate JSON projects, save/load, undo/redo and export |
| Management prototype | Boss office access, employee dossiers/reports, template mail, reception queue/guard, employee visits and petition processing |

The complete asset index is **not the same as universal asset spawning**. Unloaded resources can be inspected in the catalog; scene GameObjects can be located in their scene. Static prop placement currently uses models available in that scene. Automatic cross-scene import, fully functional NPC/terminal duplication, and arbitrary prefab instantiation are not implemented yet. The editor UI and management dialogue are primarily Russian in this version.

## Editor quick start

- Press **F8** or select **Mod Editor** in the main menu.
- Click **Сцена: выбрать** at the top left; `BossRoom` is the boss office. Opening a scene here is an editor preview, not a gameplay scene transition.
- Select an object in the hierarchy or scene view. **W / E / R** choose move / rotate / scale when right mouse is not held. **F** frames the selection; **Esc** cancels a drag.
- Hold **right mouse + WASD** to fly. Use **Q/E**, **Shift**, the wheel and middle mouse for navigation.
- In **Библиотека всех файлов**, search asset names, types or files. **Найти в сцене** locates a scene object. Switch to **Предметы сцены** to use **Поставить** for a static prop.
- Click **Сохранить** before restarting. Projects live in `prototype/ModEditorProjects`; exports are in its `Exports` subfolder.

Run **IndexAssets.cmd** to rebuild the file catalog. The indexer reads metadata from your own installation; it does not export textures, audio, geometry or game scripts.

## Compatibility

- Windows; tested game engine: **Unity 2018.4.4f1, Mono**.
- The installer checks the original `Assembly-CSharp.dll` SHA-256 and stops on unsupported/modified builds: `A290DCE4A52C17158CF10B88BA72B8F1713C76E52E08B58C88C9AE7647B0C322`.
- No claim of compatibility with every store/build, IL2CPP, Linux/macOS, or other mods.
- .NET Framework 4.x and Windows PowerShell are used by the installer/indexer. Building from source additionally requires the .NET SDK.

## Development

All development continues in this repository. Original C# mod code, launcher, installer, metadata indexer and development tooling are included. Game binaries, proprietary assets, decompiled game source, personal saves and local test profiles are excluded.

```powershell
git clone https://github.com/BurimBasic/BEHOLDER-2-MODEDITOR.git
cd BEHOLDER-2-MODEDITOR
# Dependencies = the payload directory from a downloaded addon release.
./tools/build.ps1 -GamePath 'D:\SteamLibrary\steamapps\common\Beholder 2' -Dependencies 'D:\Addon\payload'
```

See [architecture and workflow](docs/DEVELOPMENT.md), [editor guide in Russian](docs/EDITOR.ru.md), [changes](CHANGELOG.md) and [third-party notices](THIRD_PARTY_NOTICES.md).

## Roadmap and limitations

- Safe cross-scene resource import and a persistent prefab library.
- NPC/character editing with navigation-aware placement.
- Dialogue, quest and event editing through native game systems.
- Better preview lighting, selection outlines, asset thumbnails and English UI.
- Broader gameplay regression testing. Do not treat this alpha as a replacement for your main save.

Object references currently depend on scene paths and sibling indices; changing the game's scene structure can invalidate a reference. Some movement and camera behaviours are controlled by native game scripts after returning to play mode.

## License and credits

Project-authored source: [MIT](LICENSE). Third-party runtime libraries retain their own licenses. Beholder 2 and its game assets belong to their respective owners. This is an unofficial community project, not an official Alawar or Warm Lamp Games product.
