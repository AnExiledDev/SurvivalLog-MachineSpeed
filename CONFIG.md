# Machine Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.machinespeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they take effect at once, or edit the file while the game is closed. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese.

Every speed goes from 0.25 to 100000. 1 is the game's own pace, and at 1 the mod leaves that machine alone. 2 takes half the game time, 0.5 takes twice as long, and a very high speed makes a job almost instant (the shortest job is 1 game second). 0 or less counts as 1.

Generator fuel burn and generator efficiency are now in the Power Manager mod, which carries over your values the first time it starts.

## Machines

| Setting | Default | What it does |
| --- | --- | --- |
| `Shredder` | `1` | Speed of the shredder. |
| `RationPress` | `1` | Speed of the Ration Press. |
| `DyeVat` | `1` | Speed of the dye vat. |
| `Compost` | `1` | Speed of compost bins turning food into compost. |
| `PlantGrowth` | `1` | Speed of plant growth. |
| `DroneTrips` | `1` | Speed of drone trips: a faster trip brings the drone back sooner. |
| `Brewing` | `1` | Speed of brewing alcohol. |
| `Cooking` | `1` | Speed of cooking at a stove or hot pot. It burns the same fuel per dish at any speed. |
| `CookingFuel` | `1` | How much fuel a stove or hot pot uses for each dish. 1 is the game's amount, 0.5 is half, 0 uses none, 2 is twice as much. From 0 to 20; less than 0 counts as 1. |
| `Preset` | `Custom` | Sets every machine speed at once. `Custom` uses each machine's own speed; `Vanilla` is the game's pace; `Relaxed` is 2x; `Fast` is 5x; `Instant` is 100000x. `CookingFuel` keeps its own setting. |
| `ShredderAutoStart` | `false` | Starts an idle shredder by itself when you put items into it, as if you pressed Start. Taking items out does not start it, so a shredder you stopped stays stopped until you add more. It still needs power and room in its tray. |
| `FollowDayLength` | `false` | With the Day Length mod, machines keep their pace in real time: with days twice as long, every machine speed is doubled, on top of the preset. Follows Day Length from when a save loads. Does nothing without Day Length. |

The first time the mod starts, `Brewing` and `Cooking` take your values from Action Speed's config, where these two settings used to be.

## Amounts

How much machines make, hold and use. 1 is the game's amount, and at 1 the mod leaves it alone. The output settings carry fractions over to the next job, so 1.5 gives 1 and 2 by turns.

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `ShredderOutput` | `1` | 1 to 100 | Items the shredder gives for each item it shreds. |
| `RationPressOutput` | `1` | 1 to 100 | Ration blocks and crumbs the Ration Press makes from a batch. |
| `BrewingOutput` | `1` | 1 to 100 | Bottles a brewing barrel gives for a brew. |
| `CompostOutput` | `1` | 1 to 100 | Compost a bin makes from the food that rots in it. Extra compost that does not fit in the bin is lost. |
| `HarvestOutput` | `1` | 1 to 100 | Crops a harvest gives, by hand or by robot. Seeds are not changed. |
| `BarrelCapacity` | `1` | 1 to 100 | The most bottles a barrel can make in one brew, rounded to whole bottles. The extra bottles still need enough ingredients. |
| `RobotCharging` | `1` | 0.1 to 100 | How fast robots charge. Drones do not charge. |
| `FridgeStrength` | `1` | 0.1 to 100 | How strongly powered fridges and freezers slow spoiling: 2 halves the spoiling. Never faster than outside a fridge or slower than a sealed cold store. |
| `EquipmentWear` | `1` | 0 to 100 | Durability defense devices lose to wear and attacks. 0 means no wear. Doors and windows are not changed. |

Below the range counts as 1 (or as the bottom of the range for a small positive number on the 0.1 settings); above it counts as the top.

## Overview

The Machines button in the tool column opens a window with every machine at home. Its badge counts the machines that are finished, idle or out of fuel. Close the window with its close button or Esc.

| Setting | Default | What it does |
| --- | --- | --- |
| `Toasts` | `true` | Shows a notice when a machine finishes, a drone comes back or a fuel generator runs out of fuel. Clicking it opens the Machines window. |
| `RushPointsPerHour` | `0.25` | Survival points the Rush button costs for each game hour a job has left, rounded up, at least 1. From 0.01 to 1000; 0 or less counts as 0.25. |
| `FinishClock` | `false` | Shows the game time a job will be done instead of the time left, in the Machines window and the timers over plants, brewing barrels and dye vats. |

## Interface

| Setting | Default | What it does |
| --- | --- | --- |
| `UiScale` | `0` | How big Machine Speed's own windows, panels and messages are drawn, in percent, from 75 to 200. At `0` they follow the UI size set in Exiled's Toolkit (its `UiScale`). Needs Exiled's Toolkit 0.11.0 or newer. |
