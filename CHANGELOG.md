# Changelog

## 0.3.0

- Settings changed on the Mod Manager's Mods screen take effect at once. A shred, press batch, cook or drone trip already running keeps the speed it started with; brews, dye jobs, compost bins, plants and generators follow the new speed straight away.
- The Mods screen shows the mod's name, description, category and links.
- Needs Exiled's Toolkit 0.4.0 or newer.

## 0.2.0

- Speeds now go up to 100000, and the shortest job is 1 game second instead of 1 minute or 1 hour. A shred, ration batch, dye bucket or brew can be close to instant.
- New `Compost` setting: how fast compost bins turn food into compost.
- New `CookingFuel` setting: how much fuel a stove or hot pot uses for each dish.
- Faster cooking used less fuel per dish. It now uses the game's amount at any speed (then `CookingFuel` sets it).
- Brews and dye jobs that were already running follow the speed after a game start.
- The shredder and Ration Press panels show the time a job will really take.

## 0.1.0

First version. Speeds for the shredder, the Ration Press, the dye vat, plant growth, fuel generator fuel burn, drone trips, brewing and cooking. Brewing and cooking come from Action Speed, and your values for them are taken from its config the first time the mod starts. Every speed defaults to the game's own pace.
