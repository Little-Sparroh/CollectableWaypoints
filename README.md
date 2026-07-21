# Collectable Waypoints

A client-side BepInEx mod for Mycopunk that adds waypoints for undiscovered collectables so you can find them more easily during missions.

## Features

- **Data Log Waypoints**: Marks undiscovered data logs on the map. Waypoints are removed automatically when a log is opened.
- **Pumpkin Waypoints**: Marks undiscovered pumpkins (`col_pump*`). Waypoints clear when punched.
- **Bear Waypoints**: Marks undiscovered teddy bears / bear collectables. Waypoints clear when punched. Does not mark the Bruiser character.
- **Other Punch Collectables**: Automatically marks any future punch-collectable sets the game adds (eggs, event items, etc.) without a mod update.
- **Colored Waypoints**: Each category uses its own color (configurable).
- **Independent Toggles**: Enable or disable each category separately in the config.

## Getting Started

### Dependencies

* Mycopunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8
* [HarmonyLib](https://github.com/pardeike/Harmony) (included via NuGet)

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode to generate the .dll file

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Via Thunderstore (Recommended)**:
1. Download and install via Thunderstore Mod Manager
2. The mod will be automatically installed to the correct directory

**Manual Installation**:
1. Place the built `CollectableWaypoints.dll` in your `<Mycopunk Directory>/BepInEx/plugins/` folder

### Executing program

The mod loads automatically through BepInEx when the game starts. Check the BepInEx console for a `CollectableWaypoints loaded successfully.` message.

## Configuration

Access mod settings through the BepInEx configuration file at `<Mycopunk Directory>/BepInEx/config/sparroh.collectablewaypoints.cfg`.

### Toggles

- **Data Log Waypoints** (default: true) — Show waypoints for undiscovered data logs
- **Pumpkin Waypoints** (default: true) — Show waypoints for pumpkins
- **Bear Waypoints** (default: true) — Show waypoints for bears
- **Other Punch Collectable Waypoints** (default: true) — Show waypoints for any other/future punch collectables

### Colors

Colors accept `#RRGGBB`, `#RRGGBBAA`, or `R,G,B[,A]` (0-255 or 0-1):

- **Data Log Waypoint Color** (default: `#33E64D` green)
- **Bear Waypoint Color** (default: `#408CFF` blue)
- **Pumpkin Waypoint Color** (default: `#FF8C1A` orange)
- **Other Punch Collectable Waypoint Color** (default: `#D973FF` purple)

The config file is watched while the game is running. Saving changes reloads settings automatically and applies waypoint toggles/colors immediately in the current mission (no restart required).

## Help

* **Mod not loading?** Verify BepInEx is installed correctly and check console logs for errors
* **No waypoints appearing?** Confirm the relevant config toggles are enabled and that you are not in the hub
* **Data log waypoints not clearing?** Make sure you fully open the log so discovery is registered
* **Curious what the game called a collectable?** Check BepInEx logs for `[PunchCollectable] kind=... apiName=...` lines once per mission scene

## Authors

- Sparroh

## License

This project is licensed under the MIT License - see the LICENSE file for details
