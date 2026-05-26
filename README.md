# OP2MissionScanner

![Screenshot](https://images.outpostuniverse.org/OP2MissionScanner.png)

A clean Windows GUI that lists Outpost 2 mission DLLs in a folder, showing each mission's map, tech tree, and level description.

| DLL Name | Map Name | Techtree Name | Level Description |
|---|---|---|---|
| `mf4_hso.dll` | `survaxen.map` | `survtech.txt` | `4P, SRV, 'Forsaken World'` |
| `mf4_06.dll` | `mp4_06.map` | `MULTITEK.TXT` | `4 Player, SpaceRace, 'BoreHole' map.` |
| `e01.dll` | `eden01.map` | `EDENTEK.TXT` | `Eden Mission #1` |
| … | … | … | … |

OP2 mission DLLs are 32-bit Windows PE files that publish their metadata via named exports. Every mission DLL exports four symbols:

- `LevelDesc` — null-terminated string (the description shown to players)
- `MapName` — null-terminated string (e.g. `mp4_06.map`)
- `TechtreeName` — null-terminated string (e.g. `MULTITEK.TXT`)
- `DescBlock` — a binary `AIModDesc` struct with mission type, player count, etc.

This scanner reads the first three. The detection rule for "is this a mission DLL?" is just: *does it export `LevelDesc`?* Other DLLs in an OP2 install (game code, helpers) don't, and are silently skipped.

### `PeExportReader`
The only thing that touches DLL bytes. Walks the standard PE32 layout:

1. DOS header at offset `0x3C` ? PE signature offset
2. Verify `"PE\0\0"` magic
3. COFF header ? require `IMAGE_FILE_DLL` (`characteristics & 0x2000`)
4. Optional header ? require PE32 magic `0x10B` (32-bit)
5. Data directory [0] ? Export Table RVA
6. Section table ? map RVAs to file offsets
7. Export directory ? name pointer table, ordinal table, export address table
8. Build a `name ? RVA` lookup; expose `HasExport(name)` and `ReadExportString(name)`




# MissionScannerGUI

![Screenshot](https://images.outpostuniverse.org/OP2MissionScannerGUI.png)

A Windows GUI front-end for the [`MissionScanner`](https://github.com/OutpostUniverse/MissionScanner) command-line tool. Launches the CLI, captures its stdout, and renders the results in a sortable grid.

Use this when you want the extra columns (mission type, player count, unit-only flag) that the OPU scanner exposes via the `DescBlock` struct, without dealing with a console window.

| DLL Name | Type | # | U | Map Name | Techtree Name | Mission Description |
|---|---|---|---|---|---|---|
| `CEP1` | Col | 2 | F | `cm02.map` | `MULTITEK.TXT` | Colony Builder - Eden, Population |
| `ademo1` | Dem | 2 | F | `eden04.map` | `PLY_TEK.TXT` | AutoDemo Mission #1 |
| `e01` | Cam | 1 | F | `eden01.map` | `EDENTEK.TXT` | Eden Mission #1 |
| `mf4_06` | MSP | 4 | F | `mp4_06.map` | `MULTITEK.TXT` | 4 Player, SpaceRace, 'BoreHole' map. |

**Type** values (tooltip in the column header):
| Code | Meaning |
|---|---|
| `Cam` | Campaign |
| `Col` | Colony |
| `Dem` | Demo |
| `Tut` | Tutorial |
| `MLR` | Multiplayer, Land Rush |
| `MSP` | Multiplayer, Space Race |
| `MRR` | Multiplayer, Resource Race |
| `MM` | Multiplayer, Midas |
| `ML` | Multiplayer, Last One Standing |

`#` = max combined human and AI player count. `U` = unit-only mission (T/F).

