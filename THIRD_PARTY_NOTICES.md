# Third-party notices

The release contains these runtime libraries, with their upstream licenses in `vendor/`:

| Library | Assembly version | Upstream |
| --- | --- | --- |
| HarmonyX (`0Harmony.dll`) | 2.10.2.0 | https://github.com/BepInEx/HarmonyX/tree/v2.10.2 |
| Mono.Cecil | 0.10.4.0 | https://github.com/jbevain/cecil/tree/0.10.4 |
| MonoMod.Utils | 22.7.31.1 | https://github.com/MonoMod/MonoMod/tree/v22.07.31.01 |
| MonoMod.RuntimeDetour | 22.7.31.1 | https://github.com/MonoMod/MonoMod/tree/v22.07.31.01 |

These libraries are used as unmodified runtime dependencies. The release manifest records their SHA-256 hashes. No commercial game executable, game assembly, Unity runtime library, game asset, or game save is distributed. The installer reads and copies a compatible local game installation supplied by the user and patches only that isolated copy.
