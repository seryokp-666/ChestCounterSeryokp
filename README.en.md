<div align="center">

[Español](README.md) · **English**

# Fortnite Chest Counter

![Windows 10 | 11](https://img.shields.io/badge/Windows-10%20%7C%2011-3a7bd5?style=flat-square) [![MIT license](https://img.shields.io/badge/license-MIT-555?style=flat-square)](LICENSE)

**Counts every chest you open in Fortnite, match after match: window, report, in-game overlay and your team's chests.**

<img src="docs/main.png" alt="Main window" width="49%">&nbsp;<img src="docs/report-hero.png" alt="Report" width="49%">

[**⬇ Download ChestCounter.exe**](../../releases/latest) · Windows 10 / 11 · free · no install · 10 languages

</div>

---

ChestCounter reads the replays Fortnite saves of every match and keeps count of the chests you open, the **treasure hunt treasures** and, if you want, your **teammates'** chests. Everything is saved on your PC: even when Fortnite deletes the replays, your history is kept.

- **Main window**: total, last match, today, best day, average, best match, treasures, 7-day chart and your team.
- **Report** in your browser, with tabs: overview, chests, treasures, matches and teammates.
- **Overlay** on top of the game and an **OBS page**.
- **Teammates**: who you play with, wins together, each one's chests per match and a leaderboard.
- **Safe**: it only reads replay files; it never touches the game or its memory.

> It counts the chests **you** opened (and those your team was seen opening). It does not show where chests are on the map.

## Getting started

1. Download **[`ChestCounter.exe`](../../releases/latest)**. That single file is all you need.
2. In Fortnite turn on *Settings → Game → Replays → **Record Replays*** (and *Large Team Modes* if you play them). Without replays there is nothing to read.
3. Run `ChestCounter.exe`.
   > Windows may show *"Windows protected your PC"* because the exe is not signed. Click *More info → Run anyway*.
4. The welcome screen reminds you about replays and, if there are already replays on your PC, lets you **count them** or start with **your next matches only**.

<p align="center"><img src="docs/setup.png" alt="First-run welcome" width="520"></p>

5. Leave the window open (you can minimize it) while you play. During the match the counter reads the replay being recorded and adds the chests **live**; when the match ends they are confirmed.

> [!TIP]
> Matches played while the counter is closed are counted next time you open it, as long as Fortnite has not deleted those replays (it keeps a limited number). To not think about it, turn on **Open with Fortnite** in Settings.

## The main window

<p align="center"><img src="docs/main.png" alt="Main window" width="680"></p>

- **Top left**: your chest total, the last match (or the current one, in green, while you play) and the session.
- **Right**: today, best day, average, best match, history (saved matches and since when) and treasures.
- **Last 7 days**: a chart with each day's chests.
- **Team** (with *Teammates* on): your chests and each teammate's in the current or last match, the team total and their share.
- **Buttons**: **Overlay** (green dot = on), **Report** and **Settings**. With several accounts on the PC, a profile picker shows up at the top.

The window can be resized and maximized: everything grows in proportion.

## The report

`report.html` opens in your browser (the **Report** button) and refreshes by itself. It always starts on **Overview**; the tabs are at the top:

| Tab | What it shows |
|---|---|
| **Overview** | Chest total, session, Victory Royales (and %), top 10s, matches, average, best match, best day, treasures, 30-day chart and the latest matches. |
| **Chests** | By chest type, other containers (ammo boxes, food boxes, loot cannons; shows whether they count) and a **by game mode** table. |
| **Treasures** | How many, from golden chests, with a certain source, % of matches with a treasure, where they come from and the list with the exact moment. |
| **Matches** | The latest 100: mode, placement (🏆 win, top 10 in green), eliminations, chests, treasures and, with teammates, the team. |
| **Teammates** | Team chests, **top teammates**, the **per match** table and all your teammates (see below). |

<p align="center"><img src="docs/report-companeros.png" alt="Teammates tab of the report" width="820"></p>

<details>
<summary><b>See the other tabs</b></summary>

<p align="center"><img src="docs/report-resumen.png" alt="Overview" width="820"></p>
<p align="center"><img src="docs/report-cofres.png" alt="Chests" width="820"></p>
<p align="center"><img src="docs/report-tesoros.png" alt="Treasures" width="820"></p>
<p align="center"><img src="docs/report-partidas.png" alt="Matches" width="820"></p>

</details>

It uses the window's colors (dark, or light when Windows is in light mode) and adapts to phones. The screenshots are in Spanish; the report follows the app's language.

## Teammates and team

Turn it on in **Settings → Teammates in the report** (off by default). You get:

- **Exact**: who you play with, matches together and wins together.
- **Approximate**: each teammate's chests. They come from **your** replay, and only include openings Fortnite sent to the recorder. There is no guaranteed maximum radius: they are a **minimum (≥)**.
- **Team chests**: you + your teammates, in total and per match, with each one's share; handy when you hunt for a random item that drops from chests.
- **Top teammates**: most chests seen, best average, best single match and most wins together.

With **Add their chests to your total** (also off), your total, average, records and overlay include your team's chests. Your history does not change: turning it off shows only yours again.

## The overlay

<p align="center"><img src="docs/overlay-espera.png" alt="Overlay between two matches" width="300">&nbsp;&nbsp;<img src="docs/overlay-en-vivo.png" alt="Overlay live during a match" width="300"></p>

- **Big numbers**: your chest **total** and, in the box, the **previous** match (or the **current** one, live, while you play).
- **Session**, session **average**, **previous** (during a match) and **treasures**.

To see it over the game, set Fortnite to **Windowed Fullscreen** (*Settings → Video → Window Mode*): in exclusive fullscreen Windows cannot draw on top. The overlay starts **off**: turn it on with the **Overlay** button or Ctrl+Shift+O.

Streaming? While the counter is open, the same overlay is at **http://localhost:7373/** (copy it from **Settings**). In OBS, add a **browser source** with that address: transparent background, refreshes by itself.

### Shortcuts

| Shortcut | What it does |
|---|---|
| **Ctrl+Shift+O** | Show / hide the overlay. Works anywhere, even in game. |
| **Ctrl+Shift+L** | Lock / unlock the overlay. **Locked**: clicks go through and it does not move. **Unlocked**: drag it with the left button. |
| Right-click on the overlay | Hide it. |

## Settings

<p align="center"><img src="docs/settings.png" alt="Settings" width="620"></p>

- **Language**: the Windows language by default; at the top you can pick another (Spanish, English, Portuguese, Russian, French, German, Italian, Polish, Turkish or Japanese).
- **Open with Fortnite**: a small invisible process opens the counter (minimized) when Fortnite starts and closes it 30 s after Fortnite closes. It reads nothing from the game: it only looks at the list of running programs.
- **Open the report at startup** (off by default).
- **Teammates in the report** and **Add their chests to your total** (see above).
- **Also count as a chest**: ammo boxes, food boxes and loot cannons (off; nothing is deleted when you change it). Chests you **destroy** are always counted.
- **Chests before the counter**: an estimate added to your total.
- **Updates**, **overlay for OBS**, **replays folder** and **Reset statistics** (with a backup).

## How it works (and why it is safe)

Fortnite can record a **replay file** of every match on your PC. ChestCounter reads those files and nothing else.

- ✅ **Only reads replay files**: the `.replay` files in `%LOCALAPPDATA%\FortniteGame\Saved\Demos`, read-only.
- ✅ **No game modification**: the Fortnite installation folder is never touched.
- ✅ **No access to the game's memory**: it never reads, writes or injects anything into the Fortnite process.
- ✅ **No data sent anywhere**: no login, no Epic account, no telemetry. The only network access is checking the releases of **this** GitHub repository for new versions (can be turned off).
- ✅ **Lightweight**: low priority ("efficiency mode" on Windows 11) and it sleeps between matches.

## Treasures

- **★★ Golden chest (certain).** When a chest gives a treasure hunt prize (cosmetic, music track, vehicle…), the server spawns a golden chest carrying the **prize id**, which the tool reads from the replay.
- **★ Every treasure received (exact moment)**, with the chest it came from; it only counts if that chest is **yours**. When the item cannot be identified it shows as **"Treasure gizmo"**.
- A treasure obtained from something other than a chest (loot cannon, ammo box…) is flagged with ⚑.

The game only sends prize ids: `treasures.json` names them and can be edited with Notepad.

## Profiles (several accounts on one PC)

There is no name to type: the player who recorded each replay is counted. With replays from several accounts, each one gets its profile and a **drop-down** at the top right picks which one is shown.

## Updates

At startup, the counter checks the releases of the GitHub repository that built the exe for a new version and, by default, **installs it by itself** between two matches (your history and settings are kept). In Settings you can choose *Ask* or *Never* (no network access).

The repository is written into the exe when GitHub Actions builds it (`-p:UpdateRepo=user/repo`), so each exe only ever updates itself from the repository it came from. An exe built on your PC with `build.bat` does not check for updates.

## Your data

Everything is saved in `%APPDATA%\ChestCounter`. **Your history does not depend on the replays**: each match (with its chests, treasures and teammates) is saved to `data.json` as soon as it is read, and every statistic comes from there.

| File | Contents |
|---|---|
| `data.json` | Your whole history. **Back it up** if you change PC. |
| `report.html` | The report. |
| `matches.csv` | One line per match, for Excel or Google Sheets. |
| `config.json` | Settings, profile, replays folder and overlay position. |
| `treasures.json` | Names of the treasure hunt prizes. |
| `overlay.html` | Copy of the overlay (for OBS, http://localhost:7373/ is better). |
| `errors.log` | Replays that could not be read, to attach to a bug report. |

## FAQ

**Nothing is counted.** Check that replays are on in Fortnite. The window tells you if the replays folder is missing or empty.

**Why are my teammates' chests low?** Your replay may omit distant openings. There is no guaranteed fixed radius. It is a minimum, not their total.

**How do I start over?** **Settings → Reset statistics…** (with confirmation and a `data-backup-….json` backup).

**Creative modes?** Only Battle Royale matches are counted.

**"Cannot read this replay".** A Fortnite update may change the format. Matches already counted are kept. Open an [issue](../../issues) and attach `errors.log`.

**Can a match be counted twice?** No, even if you rename or save the replay.

## Command line options

```
ChestCounter.exe --demos <folder>   another replays folder
ChestCounter.exe --data <folder>    another data folder
ChestCounter.exe --once             analyze the finished replays and quit
ChestCounter.exe --reset-config     run the first-time setup again
ChestCounter.exe --overlay-preview <file.png>   save overlay previews and quit
```

## Building from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows x64) and double-click **`build.bat`**: the exe goes to `dist\ChestCounter.exe` (no automatic updates). GitHub Actions builds the exe on every push and publishes it as a release when a `v*` tag is pushed (for example `v2.0.25`); that exe updates itself from this repository's releases.

## License and credits

[MIT](LICENSE). Based on [ChestCounter](https://github.com/xdoofy92/ChestCounter) by LED-N (MIT); this copy points updates at its own repository and does not include the original author's social links. Replay parsing is based on [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (MIT), lightly modified: see [`lib/PATCHES.md`](lib/PATCHES.md).

This project is not affiliated with, endorsed or sponsored by Epic Games. Fortnite is a registered trademark of Epic Games, Inc.

Statistics only include recognized Battle Royale and Zero Build playlists (v43+). Settings can be resized and maximized. **Include team treasures** adds shared team rewards to your treasure totals without changing stored history. The treasure report separates own, team and other observed rewards. The permanent Pirate Shop Token card below the chart and action buttons shows its status even with no detections. Confirmed detections in the current or previous match are highlighted in gold with recipient details; clicking opens Treasures. Settings groups teammate visibility and shared chest/treasure totals under Team and shared treasures. Unknown numeric prize IDs are retained without guessing their identity.
