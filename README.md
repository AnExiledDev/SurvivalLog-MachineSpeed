# Machine Speed

Sets how fast the machines at your base work: the shredder, the Ration Press, the dye vat, plant growth, fuel generators, drone trips, brewing and cooking.

Each machine has its own speed. 2 means a job takes half the game time, 0.5 means twice as long. Every speed defaults to 1, and at 1 the mod does not touch that machine at all, so you only change what you set.

## Features

- **Shredder** and **Ration Press**: a job takes less game time. Both time a job in whole minutes, so a job never takes under 1 minute.
- **Dye vat**: each bucket takes less time. The vat times a bucket in whole hours, so a bucket never takes under 1 hour, and the vat's panel shows the hours the job will really take.
- **Plant growth**: plants grow faster or slower.
- **Fuel burn**: how fast fuel generators use up fuel. 2 burns it twice as fast; 0.5 makes a tank last twice as long. The time left on the generator's panel follows it.
- **Drone trips**: a drone comes back sooner.
- **Brewing**: a brew takes fewer hours, and the brewing panel shows the hours it will really take.
- **Cooking** at a stove or hot pot takes less time. It burns the same fuel per dish.
- **Takes your old settings**: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your `Brewing` and `Cooking` values from Action Speed's config and uses them.
- **Log file**: what the mod set up at startup, and the first job each machine did with it, go to `BepInEx\machine-speed.log`.

Speed changes apply at the next game start. A shred, ration batch, dye job, drone trip, brew or cook that is already running keeps the length it started with. Machine Speed writes no data of its own into your save and does not edit game files. A job's sped-up length is held by the job, so it can end up in your save. You can remove the mod at any time; jobs started after that take the game's own time again.

All settings are listed in [CONFIG.md](CONFIG.md).

## Install

1. Install BepInEx 6 for IL2CPP games and start the game once.
2. Copy `MachineSpeed.dll` into `Survival Log\BepInEx\plugins\`.
3. Start the game. The config file is created at `BepInEx\config\com.dev.survivallog.machinespeed.cfg`.

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
