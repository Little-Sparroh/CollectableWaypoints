# Changelog

## 1.0.5

- Refactor

## 1.0.4

- Configurable waypoint colors per category (defaults: data logs green, bears blue, pumpkins orange, other purple)
- Shared `WaypointUtil` helper for colored add/remove
- Cleaned up data log patches to match punch-collectable structure (still a separate system)
- Color and toggle changes rebuild waypoints immediately

## 1.0.3

- Future-proof punch collectables: unknown `PunchCollectable` sets are waypointed under Other Waypoints
- Pumpkins match known API ids (`col_pump*`) as well as name tokens
- Unique punch-collectable profiles are logged once per scene with classified kind

## 1.0.2

- Fixed bear waypoints incorrectly marking the Bruiser player/character
- Bears and pumpkins now use the game's `PunchCollectable` type instead of loose object-name matching
- Shared punch-collectable waypoint helper for bears and pumpkins
- Waypoints clear immediately when a bear or pumpkin is punched
- Improved pumpkin matching via profile API name, display name, and object name
- Logs punch-collectable profile ids once per scene to help identify API names

## 1.0.1

- Config file hot-reloads when changed on disk (no restart required)
- Toggling waypoint types applies immediately in the current mission

## 1.0.0

- Initial release
- Waypoints for undiscovered data logs
- Waypoints for pumpkins
- Waypoints for bears
- Independent config toggles for each collectable type
