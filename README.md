# Machine Speed

Sets how fast the machines at your base work: the shredder, the Ration Press, the dye vat, compost bins, plant growth, drone trips, brewing and cooking.

Generator fuel burn and generator efficiency moved to the Power Manager mod. It takes over your values the first time it starts.

Each machine has its own speed. 2 means a job takes half the game time, 0.5 means it takes twice as long. Speeds go up to 100000, so a job can be close to instant, and the shortest job is 1 game second. Every speed defaults to 1, and at 1 the mod doesn't touch that machine at all, so only what you change is changed.

## Features

- **Shredder** and **Ration Press**: jobs go faster or slower, down to 1 game second. Their panels and the shredder's item tooltips show how long a job will really take. The shredder's clock shows game hours and minutes, like the timer over the machine.
- **Dye vat**: each bucket takes less time, and a fast vat finishes a bucket in seconds. The vat's panel shows the real time for each bucket, for the whole batch and for what's left of a running job, to the minute. The timer over the vat shows the real time for the bucket being dyed.
- **Compost bins**: food in a compost bin turns into compost sooner. Only the bin is sped up, so food anywhere else rots at its normal speed.
- **Plant growth**: plants grow faster or slower. The timer over each plant and the estimate in the planting panel show the sped-up time. When you change the speed, plants that are already growing switch to it within a few seconds.
- **Drone trips**: a drone comes back sooner.
- **Brewing**: a brew takes less time, and a fast brew finishes in seconds. The brewing panel shows how long a brew will really take and how much time a running one has left, to the minute. The timer over the barrel shows the same time.
- **Cooking**: dishes at a stove or hot pot cook faster. A dish uses the same fuel at any speed.
- **Cooking fuel**: how much fuel a stove or hot pot uses for each dish. 0.5 is half, 0 uses none.
- **Takes your old settings**: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your `Brewing` and `Cooking` values from Action Speed's config and uses them.
- **Presets**: `Preset` sets every speed at once, to normal speed (Default), Relaxed (2x), Fast (5x) or Instant.
- **Shredder auto-start**: with `ShredderAutoStart` on, an idle shredder starts by itself when you put items into it.
- **Follow Day Length**: if you use the Day Length mod, `FollowDayLength` keeps machines working at the same real-time pace on longer or shorter days.
- **Amounts**: you can get more out of the shredder, Ration Press, brewing barrels, compost bins and harvests, make bigger brews, charge robots faster, make fridges and freezers keep food longer and cut the wear on defense devices. See the Amounts section of [CONFIG.md](CONFIG.md).
- **Machines window**: a Machines button in the tool column lists every machine at home with its picture, what it is making, the time left and when it finishes. Machines that need you come first and say what to do, then working ones with a progress bar, then idle ones. The button shows how many machines are working and how many need you. Esc closes the window.
- **Rush**: a Rush button in the Machines window finishes a running job right away for survival points. You set the cost with `RushPointsPerHour`, 0.25 points per game hour left by default, and a rush always costs at least 1 point. Compost bins, plants, generators and paused jobs can't be rushed.
- **Collect**: a finished dish or brew has a Collect button in the Machines window. Your survivor finishes what they're doing, then walks to the stove or brewing barrel and takes it out. Other machines are emptied at the machine as usual.
- **UI size**: the UI size setting in Exiled's Toolkit (`UiScale`) makes the Machines window bigger or smaller, as long as it still fits on the screen. The Machines button stays the same size as the game's buttons. Machine Speed has its own `UiScale` under Interface if you want a different size for this mod. At 0, the default, it follows the Toolkit's size.
- **Done notices**: a notice pops up when a machine finishes, a drone comes back or a generator runs out of fuel. Click it to open the Machines window.
- **Finish clock**: `FinishClock` shows the game time a job finishes instead of the time left.
- **Log file**: the mod writes what it set up at startup, and the first job each machine ran with it, to `BepInEx\machine-speed.log`.

Speeds you change on the Mod Manager's Mods screen apply right away. A change made in the config file applies the next time you start the game.

Each world can keep its own machine settings. That covers every setting in Machines and Amounts except `ShredderAutoStart`, plus `RushPointsPerHour`. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mods screen. `ShredderAutoStart`, the notices, the finish clock and the UI size are the same in every world.

When you change a speed, a shred, ration batch, drone trip or dish that is already running keeps the length it started with. Brews, dye jobs, compost bins and plants switch to the new speed right away, even the ones already running.

Machine Speed doesn't write any data of its own into your save and doesn't edit game files. A job does hold its sped-up length though, so that length can end up in your save. You can remove the mod at any time, and jobs started after that take their normal time again.

All settings are listed in [CONFIG.md](CONFIG.md).

## Known issues

- When you change a speed while a brew or dye job is running, the timer over the machine catches up at the next game hour. The panel shows the new time right away.
- A change to `CookingFuel` while a dish is cooking applies to the whole dish, including the part already cooked.
- Only change `Cooking` between games when no dish is cooking. A dish saved mid-cook and loaded at another speed burns its fuel at the new speed, so it can use a lot of fuel at once (or very little).
- If you set `PlantGrowth` back to 1 between games, plants planted at another speed keep that speed until harvest. Setting it back to 1 while you play moves them back to normal speed.
- In a cold room, a fridge uses whichever is colder, the room or the fridge. `FridgeStrength` doesn't stack with the cold of the room.
- With `EquipmentWear` changed, the wear line in the wave report still shows the normal amount of wear.
- With `CompostOutput` above 1, any compost that doesn't fit in the bin is lost.
- The Ration Press panel shows the normal output, without `RationPressOutput`. The batch still gives you the bigger amount.
- A rushed job also shows its done notice.
- With `FinishClock` on, the shredder, brewing and dye vat panels still show the time left.

## Requirements

- Survival Log 1.1.20719. A game update can break the mod until it is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.
- [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.19.0 or newer. Machine Speed doesn't load without it. It shows the done notices and Machine Speed's other messages as toasts, applies settings changed on the Mod Manager's Mods screen without a restart, and its UI size setting sizes the Machines window.
- Optional: Day Length, for `FollowDayLength`.
- Optional: [Mod Manager](https://www.nexusmods.com/survivallog/mods/73). With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12). If you haven't installed any mods before, start the game once so BepInEx finishes setting up, then quit.
2. Install Exiled's Toolkit. Download `ExiledToolkit-x.y.z.zip` from [its latest release](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/61), and extract it into the game folder (the folder with `SurvivalLog.exe`).
3. Download `MachineSpeed-x.y.z.zip` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/releases/latest) or from [Nexus Mods](https://www.nexusmods.com/survivallog/mods/83).
4. Extract the zip into the game folder (the folder with `SurvivalLog.exe`). It holds a `BepInEx` folder, so the DLL lands in the right place. When asked, merge the folders. You should end up with:
   `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll`
5. Start the game. The config file is created at `BepInEx\config\com.dev.survivallog.machinespeed.cfg`.

## Uninstall

Delete `MachineSpeed.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.machinespeed.cfg` and, if it exists, `BepInEx\machine-speed.log`.

## Contact

To reach me directly, message `exiled.dev` on Discord. You can find me in the [official Survival Log Discord](https://discord.gg/MZGg4gQAUe). I'm also on X as [@AnExiledDev](https://x.com/AnExiledDev).

A Survival Log wiki with detailed documentation for all of my mods is coming soon.

## Building

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so you need a game install with BepInEx set up and started once. You also need the .NET 8 SDK.

```
dotnet build src/MachineSpeed -c Release -p:GameDir="C:\path\to\Survival Log"
```

The unit tests cover the speed rules and need nothing from the game:

```
dotnet test tests/MachineSpeed.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
