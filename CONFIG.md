# Machine Speed settings

Settings live in `BepInEx\config\com.dev.survivallog.machinespeed.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, where they apply right away, or edit the file while the game is closed. Delete the file to get the defaults back. Each setting in the file says what it does, in English and Chinese.

Each world can keep its own machine settings. That covers every setting in Machines and Amounts except `ShredderAutoStart`, plus `RushPointsPerHour`. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mods screen. `ShredderAutoStart`, the notices, the finish clock and the UI size are the same in every world.

Every speed goes from 0.25 to 100000. 1 is normal speed, and at 1 the mod leaves that machine alone. 2 takes half the game time, 0.5 takes twice as long, and a very high speed makes a job almost instant (the shortest job is 1 game second). 0 or less counts as 1.

Generator fuel burn and generator efficiency moved to the Power Manager mod. It takes over your values the first time it starts.

## Machines

| Setting | Default | What it does |
| --- | --- | --- |
| `Shredder` | `1` | Speed of the shredder. |
| `RationPress` | `1` | Speed of the Ration Press. |
| `DyeVat` | `1` | Speed of the dye vat. |
| `Compost` | `1` | How fast compost bins turn food into compost. |
| `PlantGrowth` | `1` | Speed of plant growth. |
| `DroneTrips` | `1` | Speed of drone trips. A faster trip brings the drone back sooner. |
| `Brewing` | `1` | Speed of brewing alcohol. |
| `Cooking` | `1` | Speed of cooking at a stove or hot pot. A dish uses the same fuel at any speed. |
| `CookingFuel` | `1` | How much fuel a stove or hot pot uses for each dish. 1 is the normal amount, 0.5 is half, 0 uses none, 2 is twice as much. From 0 to 20, and less than 0 counts as 1. |
| `Preset` | `Custom` | Sets every machine speed at once. `Custom` uses the speed you set for each machine. The others set every machine to normal speed (`Default`), 2x (`Relaxed`), 5x (`Fast`) or 100000x (`Instant`). The preset doesn't change `CookingFuel`. |
| `ShredderAutoStart` | `false` | Starts an idle shredder by itself when you put items into it, as if you pressed Start. Taking items out doesn't start it, so a shredder you stopped stays stopped until you add more. It still needs power and room in its tray. |
| `FollowDayLength` | `false` | For the Day Length mod. Machines keep the same pace in real time, so with days twice as long, every machine speed is doubled, on top of the preset. It picks up Day Length's setting when a save loads. Does nothing without Day Length. |

The first time the mod starts, `Brewing` and `Cooking` take your values from Action Speed's config, where these two settings used to be.

## Amounts

How much machines make, hold and use. 1 is the normal amount, and at 1 the mod leaves it alone. Fractions on the output settings add up over time, so 1.5 gives 1 and 2 in turn.

| Setting | Default | Range | What it does |
| --- | --- | --- | --- |
| `ShredderOutput` | `1` | 1 to 100 | Items the shredder gives for each item it shreds. |
| `RationPressOutput` | `1` | 1 to 100 | Ration blocks and crumbs the Ration Press makes from a batch. |
| `BrewingOutput` | `1` | 1 to 100 | Bottles a brewing barrel gives for a brew. |
| `CompostOutput` | `1` | 1 to 100 | Compost a bin makes from the food that rots in it. Extra compost that doesn't fit in the bin is lost. |
| `HarvestOutput` | `1` | 1 to 100 | Crops a harvest gives, by hand or by robot. Seeds are not changed. |
| `BarrelCapacity` | `1` | 1 to 100 | The most bottles a barrel can make in one brew, rounded to whole bottles. The extra bottles still need enough ingredients. |
| `RobotCharging` | `1` | 0.1 to 100 | How fast robots charge. Drones don't charge. |
| `FridgeStrength` | `1` | 0.1 to 100 | How much powered fridges and freezers slow spoiling. 2 makes food in them spoil half as fast. Food never spoils faster than outside a fridge, or slower than in a sealed cold store. |
| `EquipmentWear` | `1` | 0 to 100 | Durability defense devices lose to wear and attacks. 0 means no wear. Doors and windows are not changed. |

A value below the range counts as 1. On the settings that start at 0.1, a number between 0 and 0.1 counts as 0.1. A value above the range counts as the top of the range.

## Overview

The Machines button in the tool column opens a window with every machine at home. The button shows two numbers: how many machines are working, and how many are finished or out of fuel and need you. Close the window with its close button or Esc.

| Setting | Default | What it does |
| --- | --- | --- |
| `Toasts` | `true` | Shows a notice when a machine finishes, a drone comes back or a fuel generator runs out of fuel. Click it to open the Machines window. |
| `RushPointsPerHour` | `0.25` | Survival points the Rush button costs for each game hour a job has left, rounded up, at least 1. From 0.01 to 1000, and 0 or less counts as 0.25. |
| `FinishClock` | `false` | Shows the game time a job will be done instead of the time left, in the Machines window and in the timers over plants, brewing barrels and dye vats. |

## Interface

| Setting | Default | What it does |
| --- | --- | --- |
| `UiScale` | `0` | How big Machine Speed's windows, panels and messages are, in percent, from 75 to 200. At `0` they follow the UI size set in Exiled's Toolkit (its `UiScale`). |
