# OP2MissionScanner

A clean Windows GUI that lists Outpost 2 mission DLLs in a folder, showing each mission's map, tech tree, and level description.




# MissionScannerGUI

A Windows GUI front-end for the [`MissionScanner`](../OPU-MissionScanner) command-line tool. Launches the CLI, captures its stdout, and renders the results in a sortable grid.

Use this when you want the extra columns (mission type, player count, unit-only flag) that the OPU scanner exposes via the `DescBlock` struct, without dealing with a console window.


