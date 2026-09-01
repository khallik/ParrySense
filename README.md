\# ParrySense



\*\*Learn the timing. Keep the skill.\*\*



ParrySense is a lightweight client-side parry training tool for Valheim.



It provides post-impact timing feedback to help you understand when you started blocking relative to Valheim's timed-block window — without changing combat mechanics or making parries easier.



\## Features



\- Post-impact parry timing feedback

\- Displays your timing in milliseconds

\- Classifies attempts as:

&#x20; - `TOO EARLY`

&#x20; - `PARRY`

&#x20; - `TOO LATE`

\- Visual timeline showing the parry window

\- English and French HUD localization

\- Configurable HUD position and appearance

\- Configurable early and late detection limits

\- Toggle the training HUD with `F8`

\- Client-side only

\- No server installation required



\## How It Works



Valheim considers a timed block successful when the block has been active for less than 250 milliseconds when the attack is blocked.



ParrySense observes this timing and displays feedback after the interaction.



Example timings:



| Timing | Result |

|---:|---|

| `+360 ms` | TOO EARLY |

| `+250 ms` | TOO EARLY |

| `+100 ms` | PARRY |

| `0 ms` | Impact |

| `-150 ms` | TOO LATE |



Positive values indicate that blocking started before impact.



Negative values indicate that blocking started after impact.



The visual timeline represents the real 250 ms timed-block window between `250 ms` and `Impact`.



Timing markers outside the displayed range are visually clamped, while the actual measured timing is still shown.



\## Philosophy



ParrySense is designed as a training aid, not a combat-assistance mod.



It does \*\*not\*\*:



\- increase the parry window

\- modify timed-block bonuses

\- modify damage or stamina costs

\- predict incoming attacks

\- automatically block or parry

\- alter enemy behavior

\- change combat mechanics



The feedback appears after the relevant combat interaction.



The goal is simple:



\*\*Learn the timing. Keep the skill.\*\*



\## Installation



\### Using a mod manager



Install ParrySense through a Valheim-compatible Thunderstore mod manager such as r2modman.



BepInExPack for Valheim is required.



\### Manual installation



1\. Install BepInExPack for Valheim.

2\. Copy `ParrySense.dll` into:



&#x20;  `BepInEx/plugins/`



3\. Start Valheim.



The configuration file is generated automatically after the first launch.



\## Configuration



The main options include:



\### General



\- `Enabled`  

&#x20; Enables or disables ParrySense.



\- `ToggleKey`  

&#x20; Key used to enable or disable the training HUD.  

&#x20; Default: `F8`



\### HUD



\- `PositionX`

\- `PositionY`

\- `DisplayDuration`

\- `PanelWidth`

\- `PanelHeight`

\- `BackgroundOpacity`



\### Training



\- `TooEarlyLimit`  

&#x20; Maximum amount of time before impact that can be reported as `TOO EARLY`.



\- `TooLateLimit`  

&#x20; Maximum amount of time after impact that can be reported as `TOO LATE`.



\- `OutsideDisplayRangeMs`  

&#x20; Controls the graphical range of the dotted areas outside the parry window.



\## Default Values



| Setting | Default |

|---|---:|

| Enabled | `true` |

| ToggleKey | `F8` |

| PositionX | `45` |

| PositionY | `160` |

| DisplayDuration | `3.0 s` |

| PanelWidth | `360` |

| PanelHeight | `75` |

| BackgroundOpacity | `0.45` |

| TooEarlyLimit | `1.0 s` |

| TooLateLimit | `0.5 s` |

| OutsideDisplayRangeMs | `250 ms` |



\## Localization



The HUD automatically follows Valheim's selected language for the currently supported languages.



Supported:



\- English

\- French



Examples:



| English | French |

|---|---|

| TOO EARLY | TROP TÔT |

| PARRY | PARADE |

| TOO LATE | TROP TARD |

| PARRY TRAINING: ENABLED | ENTRAÎNEMENT À LA PARADE : ACTIVÉ |

| PARRY TRAINING: DISABLED | ENTRAÎNEMENT À LA PARADE : DÉSACTIVÉ |



`Impact` and `250 ms` remain unchanged.



\## Multiplayer



ParrySense is client-side only.



It does not modify the server or the underlying combat rules, so the server does not need ParrySense installed.



It is intended to work while connecting to a vanilla Valheim server.



\## Limitations



`TOO LATE` detection is inferred from a recent attacker-sourced damage event followed by the start of blocking.



Because of this, some unusual damage interactions may potentially produce an incorrect late-block association.



Parry and early-block feedback are based directly on Valheim's block timing.



\## Requirements



\- Valheim

\- BepInExPack for Valheim



\## License



ParrySense is released under the MIT License.

