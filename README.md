# TerraLevel

Adds a new hoe mode to Valheim 1.0: **Level Ground (TerraLevel)**.

- Levels terrain to the ground height under the cursor, like vanilla Level Ground, but without the ±8 m cap.
- Radius is adjustable in-game: hold the modifier key (default Left Ctrl) and scroll the mouse wheel.
- Vanilla Level Ground and Raise Ground keep their 8 m limit unless you enable `UnlimitedForVanillaTools`.

## Requirements

- Valheim 1.0 (tested on 1.0.17, Steam build 25730771, 2026-10-06)
- [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) 5.4.2351+
- [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/) 2.30.2+

## Multiplayer

The mod must be installed on the **server and every client**. Terrain operations are applied by whichever peer owns the zone, and that peer has to know the TerraLevel tool. Jötunn blocks players without the mod from joining a server that has it.

Height and radius limits are admin-only settings and are pushed from the server to all clients.

## Configuration

`BepInEx/config/com.terra.terralevel.cfg`

| Key | Default | Synced | Meaning |
|---|---|---|---|
| General.MaxHeightDelta | 10000 | yes | Max height change in meters relative to original terrain for the TerraLevel tool |
| General.UnlimitedForVanillaTools | false | yes | Also lift the 8 m cap for vanilla Level Ground / Raise Ground |
| General.SmoothEdges | false | yes | Run vanilla smoothing after levelling. Off = exactly flat disc, hard edge. On = soft edge but the disc tilts up to 1 m per click on slopes (vanilla artefact) |
| Radius.DefaultRadius | 0 (= vanilla) | no | Radius when the tool is selected |
| Radius.MinRadius | 0.5 | no | Smallest selectable radius |
| Radius.MaxRadius | 20 | yes | Largest selectable radius |
| Radius.RadiusStep | 0.5 | no | Radius change per scroll notch |
| Radius.ModifierKey | LeftControl | no | Hold + scroll to change radius |

## Building

1. Copy `Environment.props.example` to `Environment.props` and set `VALHEIM_INSTALL`, `BEPINEX_PATH`, `MOD_DEPLOYPATH`.
2. `dotnet build -c Debug` (needs .NET SDK 8). The DLL is copied to `$(MOD_DEPLOYPATH)\TerraLevel\`.

Game assemblies are referenced from the game folder and publicized at compile time; nothing from the game is committed.

## How it works

- The tool is a Jötunn `CustomPiece` cloned from `mud_road_v2` and registered in the hoe piece table. Jötunn also registers its `TerrainOp` in `ObjectDB` so the zone owner can resolve it.
- Harmony transpilers replace the `±8` literals in `TerrainComp.LevelTerrain`, `RaiseTerrain` and `ApplyToHeightmap` with calls that return the configured limit for TerraLevel ops (and always for `ApplyToHeightmap`, which rebuilds terrain on every client).
- Since Valheim 1.0, `TerrainOp.Settings` is sent as a prefab hash only. The chosen radius is appended after the hash in the same `ZPackage`; vanilla readers ignore the tail, the TerraLevel reader applies it to a clone of the settings.
- A postfix on `ZNetScene.Awake`, ordered after Jötunn's, re-adds the TerraLevel `TerrainOp` to ObjectDB's terrain-op registry if Jötunn ever stops doing it (no-op otherwise).
