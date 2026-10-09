# TerraLevel

A hoe mode for Valheim 1.0 that actually flattens terrain.

Vanilla **Level Ground** only nudges the ground toward the cursor height by about 1 m per click and refuses to move terrain more than 8 m from its original height. Hills and cliffs never get flat. TerraLevel adds a second hoe entry, **Level Ground (TerraLevel)**, that sets every point in the circle to the exact height under the cursor in one click, with no height cap, and lets you pick the radius on the fly.

## Features

- One-click flat disc at cursor height. No ±8 m limit (configurable cap, default 10 000 m).
- **Hold Left Ctrl + scroll** to change the radius (0.5–20 m by default). A ring on the placement ghost shows the current size.
- Hard, clean edge by default. Optional vanilla-style smoothing.
- Vanilla Level Ground and Raise Ground are untouched unless you opt in with `UnlimitedForVanillaTools`.
- Server-synced admin settings for height limit, radius limit and smoothing.

## Install

With r2modman / Thunderstore Mod Manager: install, done. Dependencies are pulled in automatically.

Manual: install [BepInExPack_Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/), then drop `TerraLevel.dll` into `BepInEx/plugins/`.

## Multiplayer

The mod must be installed on the **server and on every client**. Terrain changes are applied by whichever player (or the server) owns the area, and that peer has to know the TerraLevel tool. Players without the mod cannot join a server that has it.

Settings marked *synced* below are taken from the server and pushed to all clients.

## Configuration

`BepInEx/config/com.terra.terralevel.cfg` (also editable in-game with Configuration Manager).

| Setting | Default | Synced | Meaning |
|---|---|---|---|
| General.MaxHeightDelta | 10000 | yes | Max height change in meters relative to the original terrain for the TerraLevel tool |
| General.UnlimitedForVanillaTools | false | yes | Also lift the 8 m cap for vanilla Level Ground / Raise Ground |
| General.SmoothEdges | false | yes | Run vanilla smoothing after levelling. Off = exactly flat disc, hard edge. On = soft edge, but the disc tilts up to 1 m per click on slopes (vanilla behaviour) |
| Radius.DefaultRadius | 0 (= vanilla 3 m) | no | Radius when the tool is selected |
| Radius.MinRadius | 0.5 | no | Smallest selectable radius |
| Radius.MaxRadius | 20 | yes | Largest selectable radius |
| Radius.RadiusStep | 0.5 | no | Radius change per scroll notch |
| Radius.ModifierKey | LeftControl | no | Hold this and scroll to change the radius |

## Known limitations

- Levelling a huge radius across a steep slope creates vertical walls. That is the point, but the terrain mesh can look stretched on very tall cuts.
- Trees, rocks and buildings are not moved.
- Console / Xbox / PlayStation players cannot use BepInEx mods.

## Compatibility

- Tested on Valheim 1.0.17 (Steam build 25730771) with BepInExPack_Valheim 5.4.2351 and Jötunn 2.30.2.
- Should coexist with other hoe mods; it only touches its own tool and the three terrain clamp constants.

## Source

https://github.com/AgronIb/TerraLevel

Build: copy `Environment.props.example` to `Environment.props`, set your game and BepInEx paths, `dotnet build -c Release` (needs .NET SDK 8). Game assemblies are referenced from your install and never redistributed.
