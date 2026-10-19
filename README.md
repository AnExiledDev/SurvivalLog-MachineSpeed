# Machine Speed

Sets how fast the machines at your base work: the shredder, the Ration Press, the dye vat, compost bins, plant growth, fuel generators, drone trips, brewing and cooking.

Each machine has its own speed. 2 means a job takes half the game time, 0.5 means twice as long. Speeds go up to 100000, so a job can be close to instant: the shortest job is 1 game second. Every speed defaults to 1, and at 1 the mod does not touch that machine at all, so you only change what you set.

## Features

- **Shredder** and **Ration Press**: a job takes less game time, down to 1 second. Their panels, and the shredder's item tooltips, show the time a job will really take.
- **Dye vat**: each bucket takes less time, and a fast vat finishes a bucket in seconds. The vat's panel shows the real time per bucket, for the batch, and left on a running job, to the minute.
- **Compost bins**: food in a compost bin turns into compost sooner. Only the bin is sped up; food anywhere else rots at the game's pace.
- **Plant growth**: plants grow faster or slower. The timer over each plant and the planting panel's estimate show the sped-up time, and plants already growing when the speed changes move to the new speed within a few seconds.
- **Fuel burn**: how fast fuel generators use up fuel. 2 burns it twice as fast; 0.5 makes a tank last twice as long. The time left on the generator's panel follows it.
- **Drone trips**: a drone comes back sooner.
- **Brewing**: a brew takes less time, and a fast brew finishes in seconds. The brewing panel shows the real time a brew will take and the real time left on a running one, to the minute.
- **Cooking** at a stove or hot pot takes less time. It burns the same fuel per dish at any speed.
- **Cooking fuel**: how much fuel a stove or hot pot uses for each dish. 0.5 is half, 0 uses none.
- **Takes your old settings**: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your `Brewing` and `Cooking` values from Action Speed's config and uses them.
- **Log file**: what the mod set up at startup, and the first job each machine did with it, go to `BepInEx\machine-speed.log`.

Speeds changed on the Mod Manager's Mods screen take effect at once with Exiled's Toolkit installed; a change made in the file applies at the next game start. A shred, ration batch, drone trip or cook that is already running keeps the length it started with. Brews, dye jobs, compost bins, plants and fuel generators follow the new speed straight away, including the ones already running. Machine Speed writes no data of its own into your save and does not edit game files. A job's sped-up length is held by the job, so it can end up in your save. You can remove the mod at any time; jobs started after that take the game's own time again.

All settings are listed in [CONFIG.md](CONFIG.md).

## Install

1. Install BepInEx 6 for IL2CPP games and start the game once.
2. Optional: install [Exiled's Toolkit](https://github.com/AnExiledDev/SurvivalLog-ExiledToolkit) 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.
3. Copy `MachineSpeed.dll` into `Survival Log\BepInEx\plugins\`.
4. Start the game. The config file is created at `BepInEx\config\com.dev.survivallog.machinespeed.cfg`.

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
