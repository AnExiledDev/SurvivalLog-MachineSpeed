# Machine Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.machinespeed.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese.

Every speed goes from 0.25 to 100000. 1 is the game's own pace, and at 1 the mod leaves that machine alone. 2 takes half the game time, 0.5 takes twice as long, and a very high speed makes a job almost instant (the shortest job is 1 game second). 0 or less counts as 1.

## Machines

| Setting | Default | What it does |
| --- | --- | --- |
| `Shredder` | `1` | Speed of the shredder. |
| `RationPress` | `1` | Speed of the Ration Press. |
| `DyeVat` | `1` | Speed of the dye vat. |
| `Compost` | `1` | Speed of compost bins turning food into compost. |
| `PlantGrowth` | `1` | Speed of plant growth. |
| `FuelBurn` | `1` | How fast fuel generators burn fuel. 2 burns it twice as fast, 0.5 makes it last twice as long. |
| `DroneTrips` | `1` | Speed of drone trips: a faster trip brings the drone back sooner. |
| `Brewing` | `1` | Speed of brewing alcohol. |
| `Cooking` | `1` | Speed of cooking at a stove or hot pot. It burns the same fuel per dish at any speed. |
| `CookingFuel` | `1` | How much fuel a stove or hot pot uses for each dish. 1 is the game's amount, 0.5 is half, 0 uses none, 2 is twice as much. From 0 to 20; less than 0 counts as 1. |

The first time the mod starts, `Brewing` and `Cooking` take your values from Action Speed's config, where these two settings used to be.
