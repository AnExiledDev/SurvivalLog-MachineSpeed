# Machine Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.machinespeed.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese.

Every speed goes from 0.25 to 20. 1 is the game's own pace, and at 1 the mod leaves that machine alone. 2 takes half the game time, 0.5 takes twice as long. 0 or less counts as 1.

## Machines

| Setting | Default | What it does |
| --- | --- | --- |
| `Shredder` | `1` | Speed of the shredder. A job never takes under 1 minute. |
| `RationPress` | `1` | Speed of the Ration Press. A batch never takes under 1 minute. |
| `DyeVat` | `1` | Speed of the dye vat. A bucket never takes under 1 hour. |
| `PlantGrowth` | `1` | Speed of plant growth. |
| `FuelBurn` | `1` | How fast fuel generators burn fuel. 2 burns it twice as fast, 0.5 makes it last twice as long. |
| `DroneTrips` | `1` | Speed of drone trips: a faster trip brings the drone back sooner. |
| `Brewing` | `1` | Speed of brewing alcohol. A brew never takes under 1 hour. |
| `Cooking` | `1` | Speed of cooking at a stove or hot pot. |

The first time the mod starts, `Brewing` and `Cooking` take your values from Action Speed's config, where these two settings used to be.
