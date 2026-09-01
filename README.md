# ParrySense

> **Learn the timing. Keep the skill.**

ParrySense is a lightweight **client-side parry training tool for Valheim**.

It provides post-impact timing feedback to help you understand when you started blocking relative to Valheim's timed-block window — **without changing combat mechanics or making parries easier**.

---

## ⚔️ Features

- 🎯 Post-impact parry timing feedback
- ⏱️ Timing displayed in milliseconds
- 🔵 **TOO EARLY**
- 🟢 **PARRY**
- 🔴 **TOO LATE**
- 📊 Visual timeline showing Valheim's 250 ms timed-block window
- 🌐 English and French HUD localization
- ⚙️ Configurable HUD position and appearance
- 🎚️ Configurable early and late detection limits
- ⌨️ Toggle training mode with `F8`
- 🖥️ Client-side only — no server installation required

---

## 🛡️ How It Works

Valheim considers a timed block successful when the block has been active for **less than 250 milliseconds** when the attack is blocked.

ParrySense observes the timing and displays feedback **after the combat interaction**.

| Timing | Result |
| ---: | :--- |
| `+360 ms` | 🔵 **TOO EARLY** |
| `+250 ms` | 🔵 **TOO EARLY** |
| `+100 ms` | 🟢 **PARRY** |
| `0 ms` | **Impact** |
| `-150 ms` | 🔴 **TOO LATE** |

Positive values mean that blocking started **before impact**. Negative values mean that blocking started **after impact**.

The visual timeline represents the real **250 ms timed-block window** between `250 ms` and `Impact`.

Timing markers outside the displayed range are visually clamped, while the actual measured timing is still displayed.

---

## 🎯 Philosophy

ParrySense is designed as a **training aid**, not a combat-assistance mod.

It does **not**:

- increase the parry window
- modify timed-block bonuses
- modify damage or stamina costs
- predict incoming attacks
- automatically block or parry
- alter enemy behavior
- change combat mechanics

Feedback is provided after the relevant combat interaction.

> **Learn the timing. Keep the skill.**

---

## 📦 Installation

### Mod manager

Install ParrySense through a Valheim-compatible Thunderstore mod manager such as r2modman.

**BepInExPack for Valheim is required.**

### Manual installation

1. Install BepInExPack for Valheim.
2. Copy `ParrySense.dll` into `BepInEx/plugins/`.
3. Start Valheim.

The configuration file is generated automatically after the first launch.

---

## ⚙️ Configuration

### General

| Setting | Default | Description |
| :--- | :---: | :--- |
| `Enabled` | `true` | Enables or disables ParrySense. |
| `ToggleKey` | `F8` | Enables or disables the training HUD. |

### HUD

| Setting | Default |
| :--- | ---: |
| `PositionX` | `45` |
| `PositionY` | `160` |
| `DisplayDuration` | `3.0 s` |
| `PanelWidth` | `360` |
| `PanelHeight` | `75` |
| `BackgroundOpacity` | `0.45` |

### Training

| Setting | Default | Description |
| :--- | ---: | :--- |
| `TooEarlyLimit` | `1.0 s` | Maximum time before impact that can be reported as `TOO EARLY`. |
| `TooLateLimit` | `0.5 s` | Maximum time after impact that can be reported as `TOO LATE`. |
| `OutsideDisplayRangeMs` | `250 ms` | Graphical range of each dotted area outside the parry window. |

---

## 🌐 Localization

The HUD automatically follows Valheim's selected language for the currently supported languages.

| English | French |
| :--- | :--- |
| 🔵 TOO EARLY | 🔵 TROP TÔT |
| 🟢 PARRY | 🟢 PARADE |
| 🔴 TOO LATE | 🔴 TROP TARD |
| PARRY TRAINING: ENABLED | ENTRAÎNEMENT À LA PARADE : ACTIVÉ |
| PARRY TRAINING: DISABLED | ENTRAÎNEMENT À LA PARADE : DÉSACTIVÉ |

`Impact` and `250 ms` remain unchanged.

Currently supported:

- English
- French

---

## 🌍 Multiplayer

ParrySense is **client-side only**.

It does not modify the server or the underlying combat rules, so the server does not need ParrySense installed.

It is intended to work while connecting to a vanilla Valheim server.

---

## ⚠️ Limitations

`TOO LATE` detection is inferred from a recent attacker-sourced damage event followed by the start of blocking.

Because of this, some unusual damage interactions may potentially produce an incorrect late-block association.

Parry and early-block feedback are based directly on Valheim's block timing.

---

## 📋 Requirements

- Valheim
- BepInExPack for Valheim

---

## 📄 License

ParrySense is released under the **MIT License**.
