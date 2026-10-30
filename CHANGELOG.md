# Changelog

## 0.8.1

- **Warning:** install Power Manager to keep your generator settings. FuelBurn and GeneratorEfficiency are no longer in Machine Speed. Power Manager carries over your values the first time it starts.
- Fridge Strength now keeps working once you have the fridge talent. The talent capped fridges at 10x, so a strength of 100 showed 500x early in a run and only 10x later. The talent's bonus now counts first and the strength goes on top.
- Made for Survival Log 1.1.20151.

## 0.8.0

- New: a finished dish or brew has a Collect button in the Machines window. Your survivor walks to the stove or brewing barrel and takes it out, after whatever they are doing now.
- Made for Survival Log 1.1.20151.

## 0.7.0

- FuelBurn and GeneratorEfficiency moved to the new Power Manager mod. Power Manager carries over your values the first time it starts.
- Made for Survival Log 1.1.19611.

## 0.6.1

- Machine Speed's messages shown while the Machines window is shut, such as "Load a game first.", are now Exiled's Toolkit toasts. Those toasts also go to the Notifications history.
- Exiled's Toolkit 0.11.0 or newer is now required.
- Made for Survival Log 1.1.19611.

## 0.6.0

- New: `UiScale` under Interface sets the size of Machine Speed's own windows on their own, from 75% to 200%, whatever Exiled's Toolkit's UI size is. At 0, the default, they follow the Toolkit's. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.5.5

- The Machines window and its pop-up messages can be drawn bigger or smaller with the UI size setting of Exiled's Toolkit 0.10.0 or newer. A change shows at once, and the window only grows as far as it still fits on the screen.
- The Machines button keeps the game's size, like the game's own buttons around it.
- Made for Survival Log 1.1.19611.

## 0.5.4

- Fixed: a home with many machines had a short stutter every few seconds. The Machines button now checks your machines a few at a time instead of all at once.
- Fixed: the game could hitch once every game hour in a home with a lot of furniture.
- The brewing barrel, dye vat and shredder windows show their times with less work behind the scenes, so the game runs smoother while one is open.
- The Machines window and its button update only what changed, so the game runs smoother while the window is open.
- Made for Survival Log 1.1.19611.

## 0.5.3

- Idle machines (nothing loaded, a drone at home) are no longer listed under Needs you. They have their own Idle section at the bottom of the Machines window and are not counted.
- The Machines button shows two numbers: how many machines are working (bottom corner) and how many are finished or out of fuel and need you (top corner). Each hides at zero.
- Made for Survival Log 1.1.19131.

## 0.5.2

- The Machines window has a new look to match the other hub windows: each machine has its picture, a progress bar on jobs of known length, and the list is split into what needs you and what is working. A machine that needs you says what to do (take out, add fuel, nothing loaded). The header shows how many machines are at home, how many need you and your survival points.
- The Rush button shows its price beside it, and when you cannot afford it, says how many points it needs under it.
- Made for Survival Log 1.1.19131.

## 0.5.1

- New `ShredderAutoStart` setting (off by default): an idle shredder starts by itself when you put items into it. Taking items out does not start it.
- Made for Survival Log 1.1.19131.

## 0.5.0

- A Machines button in the tool column opens a window listing every machine at home: what it is making, time left, finish time, and which are idle, finished or out of fuel. The button shows how many need you.
- A Rush button on a running job finishes it now for survival points: `RushPointsPerHour` for each game hour left, at least 1 point.
- With Exiled's Toolkit installed, a notice pops up when a machine finishes or a generator runs out of fuel. Clicking it opens the Machines window. `Toasts` turns them off.
- `FinishClock` makes the timers show the game time a job finishes instead of the time left.
- `Preset` sets every machine speed at once: Vanilla, Relaxed (2x), Fast (5x) or Instant. Custom keeps each machine's own speed.
- `FollowDayLength` speeds machines up to match Day Length, so they keep their real-time pace on a longer day.
- New `[Amounts]` settings: more output from the shredder, Ration Press, brewing, compost bins and harvests; more bottles per brew; faster robot charging; stronger fridges and freezers; more power per unit of generator fuel; and less (or no) wear on defense devices.
- Made for Survival Log 1.1.19131.

## 0.4.2

- After a game update, Mod Manager can tell you if this mod may need an update.
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

- Mod Manager's Mods screen links to this changelog.
- Settings changed on the Mod Manager's Mods screen take effect at once. A shred, press batch, cook or drone trip already running keeps the speed it started with; brews, dye jobs, compost bins, plants and generators follow the new speed straight away.
- Mod Manager's Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings you change on the Mods screen take effect right away. Without it, they take effect the next time you start the game.

## 0.2.0

- Speeds now go up to 100000, and the shortest job is 1 game second instead of 1 minute or 1 hour. A shred, ration batch, dye bucket or brew can be close to instant.
- New `Compost` setting: how fast compost bins turn food into compost.
- New `CookingFuel` setting: how much fuel a stove or hot pot uses for each dish.
- Faster cooking used less fuel per dish. It now uses the game's amount at any speed (then `CookingFuel` sets it).
- Brews and dye jobs that were already running follow the speed after a game start.
- The shredder and Ration Press panels show the time a job will really take.

## 0.1.0

- First release.
- Speeds for the shredder, the Ration Press, the dye vat, plant growth, fuel generator fuel burn, drone trips, brewing and cooking.
- Brewing and cooking come from Action Speed, and your values for them are taken from its config the first time the mod starts.
- Every speed defaults to the game's own pace.
