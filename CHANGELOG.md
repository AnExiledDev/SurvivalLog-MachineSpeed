# Changelog

## 0.4.2

- After a game update, a hook that can no longer be placed is logged as an error, so the Mod Manager can mark the mod as maybe needing an update.
- Made for Survival Log 1.1.19131.

## 0.4.1

- The timer over a brewing barrel shows the sped-up time. It used to show the game's full time (a day or more) while the brew took seconds.
- The timer over a dye vat shows the sped-up time for the bucket being dyed.
- The shredder's clock shows hours and minutes of game time, the same as the timer over the machine. It used to show minutes and seconds, which read like a long wait and jumped when the shred started.
- Brewing and dye vat panel times count whole minutes the same way as the timers over the machines, so the two always match.
- Changing the cooking speed while a dish is cooking no longer burns a pile of fuel at once (or makes the rest of the dish free). A dish burns fuel at the speed it started with.
- The planting panel's estimate is sped up for every seed you pick, including one whose game time matched the last estimate shown.
- A shredder panel that was already open shows the sped-up clock straight away.

## 0.4.0

- The brewing barrel's time for a brew and its countdown show the sped-up time, to the minute, and count down live.
- The dye vat's time per bucket, batch estimate, bucket countdown and time to finish the batch show the sped-up time, to the minute.
- The timer over each plant shows the sped-up time. Plants that were already growing when the speed changed (or that were planted in an earlier game at another speed) move to the new speed within a few seconds, keeping how far they have grown.
- The planting panel's growth estimate shows the sped-up time.
- The shredder's clock before you start it, and the time in each item's tooltip, show the sped-up time.

## 0.3.0

- The Mods screen links this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. A shred, press batch, cook or drone trip already running keeps the speed it started with; brews, dye jobs, compost bins, plants and generators follow the new speed straight away.
- The Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings changed on the Mods screen take effect without a restart; without it, at the next game start.

## 0.2.0

- Speeds now go up to 100000, and the shortest job is 1 game second instead of 1 minute or 1 hour. A shred, ration batch, dye bucket or brew can be close to instant.
- New `Compost` setting: how fast compost bins turn food into compost.
- New `CookingFuel` setting: how much fuel a stove or hot pot uses for each dish.
- Faster cooking used less fuel per dish. It now uses the game's amount at any speed (then `CookingFuel` sets it).
- Brews and dye jobs that were already running follow the speed after a game start.
- The shredder and Ration Press panels show the time a job will really take.

## 0.1.0

First version. Speeds for the shredder, the Ration Press, the dye vat, plant growth, fuel generator fuel burn, drone trips, brewing and cooking. Brewing and cooking come from Action Speed, and your values for them are taken from its config the first time the mod starts. Every speed defaults to the game's own pace.
