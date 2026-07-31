# Collectable Waypoints

A client-side BepInEx mod for Mycopunk that adds map waypoints for undiscovered collectables so you can find them more
easily during missions.

## Features

- **Data Log Waypoints** — Marks undiscovered data logs. Waypoints are removed when a log is opened.
- **Pumpkin Waypoints** — Marks undiscovered pumpkins (`col_pump*`). Waypoints clear when punched.
- **Bear Waypoints** — Marks undiscovered teddy bears / bear collectables. Waypoints clear when punched. Does not mark
  the Bruiser character.
- **Other Punch Collectables** — Automatically marks any other or future punch-collectable sets (eggs, event items,
  etc.) without a mod update.
- **Colored Waypoints** — Each category uses its own color (configurable).
- **Independent Toggles** — Enable or disable each category separately in the config.
- **Live Config Reload** — Saving the config file applies toggles and colors immediately in the current mission (no
  restart required).

## Installation

### Dependencies

- Mycopunk
- [BepInEx Pack for Mycopunk](https://thunderstore.io/c/mycopunk/p/BepInEx/BepInExPack_Mycopunk/) (5.4.2403 or
  compatible)

### Thunderstore (recommended)

1. Install via Thunderstore Mod Manager / r2modman.
2. The mod is placed in the correct plugins folder automatically.

### Manual

1. Build or download `CollectableWaypoints.dll`.
2. Place it in `<Mycopunk Directory>/BepInEx/plugins/`.

The mod loads automatically with BepInEx. On success, the console shows:

```text
CollectableWaypoints loaded successfully.
```

## Configuration

Settings live in:

```text
<Mycopunk Directory>/BepInEx/config/sparroh.collectablewaypoints.cfg
```

The config file is watched while the game is running. Saving changes reloads settings and rebuilds waypoints in the
current mission.

### General

| Setting              | Default | Description                                          |
|----------------------|---------|------------------------------------------------------|
| `Data Log Waypoints` | `true`  | Show waypoints for undiscovered data logs            |
| `Pumpkin Waypoints`  | `true`  | Show waypoints for pumpkins                          |
| `Bear Waypoints`     | `true`  | Show waypoints for bears                             |
| `Other Waypoints`    | `true`  | Show waypoints for other / future punch collectables |

### Colors

Colors accept `#RRGGBB`, `#RRGGBBAA`, or `R,G,B[,A]` (components as 0–255 or 0–1).

| Setting                   | Default   | Description                       |
|---------------------------|-----------|-----------------------------------|
| `Data Log Waypoint Color` | `#33E64D` | Green — data logs                 |
| `Bear Waypoint Color`     | `#408CFF` | Blue — bears                      |
| `Pumpkin Waypoint Color`  | `#FF8C1A` | Orange — pumpkins                 |
| `Other Waypoint Color`    | `#D973FF` | Purple — other punch collectables |

## Building

1. Clone this repository.
2. Open the solution in Visual Studio, Rider, or another C# IDE.
3. Build in Release mode (`netstandard2.1`).

Or with the .NET CLI:

```bash
dotnet build --configuration Release
```

Output DLL:

```text
bin/Release/netstandard2.1/CollectableWaypoints.dll
```

## Troubleshooting

- **Mod not loading?** Confirm BepInEx is installed correctly and check the BepInEx console for errors.
- **No waypoints appearing?** Enable the relevant config toggles and make sure you are in a mission (not the hub).
- **Data log waypoints not clearing?** Fully open the log so discovery is registered.
- **What did the game call a collectable?** Check BepInEx logs for lines like
  `[PunchCollectable] kind=... apiName=...` (logged once per mission scene).

## Authors

- Sparroh

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
