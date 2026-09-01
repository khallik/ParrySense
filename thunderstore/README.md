# ParrySense

Post-impact parry timing feedback for Valheim.

ParrySense shows when you started blocking relative to Valheim's **250 ms timed-block window**.

It is designed as a training tool: understand your timing without changing the combat mechanics.

## Features

- Post-impact timing feedback
- `TOO EARLY`, `PARRY`, and `TOO LATE` results
- Timing displayed in milliseconds
- Visual representation of the 250 ms parry window
- English and French HUD
- Toggle training feedback with `F8`
- Configurable HUD position and timing limits
- Client-side only

## How it works

ParrySense displays feedback **after the attack reaches you**.

It does not:

- predict incoming attacks
- automatically block or parry
- increase the parry window
- modify damage, stamina, or combat mechanics

## Timing

| Result | Block started |
| --- | --- |
| 🔵 `TOO EARLY` | More than 250 ms before impact |
| 🟢 `PARRY` | Inside the timed-block window |
| 🔴 `TOO LATE` | After impact |

The timing value is training feedback only. Blocking closer to impact does **not** provide an additional gameplay bonus.

## Installation

Requires **BepInExPack for Valheim**.

Install with a compatible mod manager, or manually place:

`ParrySense.dll`

in:

`BepInEx/plugins/`

## Multiplayer

ParrySense is **client-side only**.

No server installation is required, and it can be used while playing on a vanilla dedicated server.

## Source

Source code, documentation, and issue tracking are available on GitHub.
