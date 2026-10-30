# Machine Speed

Sets how fast the machines at your base work: the shredder, the Ration Press, the dye vat, compost bins, plant growth, drone trips, brewing and cooking.

Generator fuel burn and generator efficiency are now in the Power Manager mod, which carries over your values the first time it starts.

Each machine has its own speed. 2 means a job takes half the game time, 0.5 means twice as long. Speeds go up to 100000, so a job can be close to instant: the shortest job is 1 game second. Every speed defaults to 1, and at 1 the mod does not touch that machine at all, so you only change what you set.

## Features

- **Shredder** and **Ration Press**: a job takes less game time, down to 1 second. Their panels, and the shredder's item tooltips, show the time a job will really take. The shredder's clock shows game hours and minutes, the same as the timer over the machine.
- **Dye vat**: each bucket takes less time, and a fast vat finishes a bucket in seconds. The vat's panel shows the real time per bucket, for the batch, and left on a running job, to the minute, and the timer over the vat shows the real time for the bucket being dyed.
- **Compost bins**: food in a compost bin turns into compost sooner. Only the bin is sped up; food anywhere else rots at the game's pace.
- **Plant growth**: plants grow faster or slower. The timer over each plant and the planting panel's estimate show the sped-up time, and plants already growing when the speed changes move to the new speed within a few seconds.
- **Drone trips**: a drone comes back sooner.
- **Brewing**: a brew takes less time, and a fast brew finishes in seconds. The brewing panel shows the real time a brew will take and the real time left on a running one, to the minute. The timer over the barrel shows the same time.
- **Cooking** at a stove or hot pot takes less time. It burns the same fuel per dish at any speed.
- **Cooking fuel**: how much fuel a stove or hot pot uses for each dish. 0.5 is half, 0 uses none.
- **Takes your old settings**: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your `Brewing` and `Cooking` values from Action Speed's config and uses them.
- **Presets**: `Preset` sets every speed at once: Vanilla, Relaxed (2x), Fast (5x) or Instant.
- **Shredder auto-start**: with `ShredderAutoStart` on, an idle shredder starts by itself when you put items into it.
- **Follow Day Length**: with the Day Length mod, `FollowDayLength` keeps machines at their real-time pace on longer or shorter days.
- **Amounts**: more output from the shredder, Ration Press, brewing, compost bins and harvests; bigger brews; faster robot charging; stronger fridges and freezers; and less wear on defense devices. See the Amounts section of [CONFIG.md](CONFIG.md).
- **Machines window**: a Machines button in the tool column lists every machine at home with its picture, what it is making, the time left and when it finishes. Machines that need you are listed first with what to do, then working ones with a progress bar, then idle ones. The button shows how many machines are working and how many need you. Esc closes the window.
- **Rush**: a Rush button in the Machines window finishes a running job now for survival points, 0.25 per game hour left by default, at least 1. Compost bins, plants, generators and paused jobs cannot be rushed.
- **Collect**: a finished dish or brew has a Collect button in the Machines window. Your survivor walks to the stove or brewing barrel and takes it out, after whatever they are doing now. Other machines are emptied at the machine as usual.
- **UI size**: Exiled's Toolkit's UI size setting (`UiScale`) draws the Machines window bigger or smaller, as far as the window still fits on the screen. The Machines button keeps the game's size. Machine Speed's own `UiScale` under Interface sets a size for this mod alone; at 0, the default, it follows the Toolkit's.
- **Done notices**: a notice pops up when a machine finishes, a drone comes back or a generator runs out of fuel.
- **Finish clock**: `FinishClock` shows the game time a job finishes instead of the time left.
- **Log file**: what the mod set up at startup, and the first job each machine did with it, go to `BepInEx\machine-speed.log`.

Speeds changed on the Mod Manager's Mods screen take effect at once; a change made in the file applies at the next game start. A shred, ration batch, drone trip or cook that is already running keeps the length it started with. Brews, dye jobs, compost bins and plants follow the new speed straight away, including the ones already running. Machine Speed writes no data of its own into your save and does not edit game files. A job's sped-up length is held by the job, so it can end up in your save. You can remove the mod at any time; jobs started after that take the game's own time again.

All settings are listed in [CONFIG.md](CONFIG.md).

## Known issues

- When you change a speed while a brew or dye job is running, the timer over the machine catches up at the next game hour. The panel shows the new time at once.
- A change to `CookingFuel` while a dish is cooking applies to the whole dish, including the part already cooked.
- Change `Cooking` between games only when no dish is cooking. A dish saved mid-cook and loaded at another speed burns its fuel at the new speed, so it can use a lot of fuel at once (or very little).
- Plants planted at another speed keep it until harvest when `PlantGrowth` is set back to 1 between games. Within a game, setting it back to 1 moves them to the game's pace.

- Fridges in a cold room use whichever is colder, the room or the fridge; `FridgeStrength` does not stack with the room's cold.
- The wave report's wear line still counts the game's own wear when `EquipmentWear` is changed.
- Compost that does not fit in the bin with `CompostOutput` above 1 is lost.
- The Ration Press panel shows the game's output before `RationPressOutput`; the batch gives the multiplied amount.
- A rushed job also shows its done notice.
- With `FinishClock` on, the game's own shredder, brewing and dye panels still show the time left.

## Requirements

- Survival Log 1.1.20151. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.11.0 or newer. Machine Speed does not load without it. It shows the done notices and Machine Speed's other messages as toasts, applies settings changed on the Mod Manager's Mods screen without a restart, and its UI size setting sizes the Machines window.
- Optional: Day Length, for `FollowDayLength`.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `MachineSpeed-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/83).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll`
5. Start the game. The config file is created at `BepInEx\config\com.dev.survivallog.machinespeed.cfg`.

## Uninstall

Delete `MachineSpeed.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.machinespeed.cfg` and, if it exists, `BepInEx\machine-speed.log`.

## Contact

To reach me directly, message `exiled.dev` on Discord; you can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Building

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. The .NET 8 SDK is required.

```
dotnet build src/MachineSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

The unit tests cover the speed rules themselves and need nothing from the game:

```
dotnet test tests/MachineSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
