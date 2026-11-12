# Changelog

## 0.9.2

- Clearer wording in the settings and on screen.
- Made for Survival Log 1.1.20719.

## 0.9.1

- Exiled's Toolkit 0.19.0 or newer is now required.
- Made for Survival Log 1.1.20719.

## 0.9.0

- Each world can keep its own machine settings: every speed, `Preset`, `CookingFuel`, `FollowDayLength`, every setting in Amounts and `RushPointsPerHour`. With Exiled's Toolkit 0.17.0 or newer, pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mod Manager's Mods screen. `ShredderAutoStart`, the notices, the finish clock and the UI size are the same in every world.
- Fixed: the first change to `RobotCharging` only reached the robots you already had after their modules changed or you loaded a save. A change now reaches every robot right away.
- Made for Survival Log 1.1.20151.

## 0.8.1

- **Warning:** install Power Manager to keep your generator settings. FuelBurn and GeneratorEfficiency are no longer in Machine Speed. Power Manager takes over your values the first time it starts.
- Fixed: Fridge Strength stopped working once you had the fridge talent. The talent capped fridges at 10x, so a strength of 100 showed 500x early in a run and only 10x later. Now the talent's bonus counts first and Fridge Strength goes on top of it.
- Made for Survival Log 1.1.20151.

## 0.8.0

- New: a finished dish or brew has a Collect button in the Machines window. Your survivor finishes what they're doing, then walks to the stove or brewing barrel and takes it out.
- Made for Survival Log 1.1.20151.

## 0.7.0

- FuelBurn and GeneratorEfficiency moved to the new Power Manager mod. Power Manager takes over your values the first time it starts.
- Made for Survival Log 1.1.19611.

## 0.6.1

- Messages that show while the Machines window is closed, like "Load a game first.", now pop up as Exiled's Toolkit toasts. They also go to the Notifications history.
- Exiled's Toolkit 0.11.0 or newer is now required.
- Made for Survival Log 1.1.19611.

## 0.6.0

- New: `UiScale` under Interface sets the size of Machine Speed's windows by itself, from 75% to 200%, no matter what the UI size in Exiled's Toolkit is. At 0, the default, they follow the Toolkit's size. Needs Exiled's Toolkit 0.11.0 or newer.
- Made for Survival Log 1.1.19611.

## 0.5.5

- The UI size setting in Exiled's Toolkit 0.10.0 or newer now makes the Machines window and its pop-up messages bigger or smaller. A change shows right away, and the window only grows as long as it still fits on the screen.
- The Machines button stays the same size as the game's buttons around it.
- Made for Survival Log 1.1.19611.

## 0.5.4

- Fixed: a home with many machines had a short stutter every few seconds. The Machines button now checks your machines a few at a time instead of all at once.
- Fixed: the game could hitch once every game hour in a home with a lot of furniture.
- The game runs smoother while a brewing barrel, dye vat or shredder window is open.
- The game runs smoother while the Machines window is open.
- Made for Survival Log 1.1.19611.

## 0.5.3

- Idle machines (nothing loaded, or a drone at home) are no longer listed under Needs you. They have their own Idle section at the bottom of the Machines window and aren't counted.
- The Machines button shows two numbers: how many machines are working (bottom corner) and how many are finished or out of fuel and need you (top corner). Each one hides at zero.
- Made for Survival Log 1.1.19131.

## 0.5.2

- The Machines window has a new look. Each machine has its picture, and jobs of known length have a progress bar.
- The list in the Machines window is split into machines that need you and machines that are working. A machine that needs you says what to do (take out, add fuel, nothing loaded).
- The top of the Machines window shows how many machines are at home, how many need you and your survival points.
- The Rush button shows its price. When you can't afford it, the points you need show under it.
- Made for Survival Log 1.1.19131.

## 0.5.1

- New `ShredderAutoStart` setting (off by default): an idle shredder starts by itself when you put items into it. Taking items out doesn't start it.
- Made for Survival Log 1.1.19131.

## 0.5.0

- A Machines button in the tool column opens a window listing every machine at home: what it is making, the time left, when it finishes, and which machines are idle, finished or out of fuel. The button shows how many need you.
- A Rush button on a running job finishes it right away for survival points. It costs `RushPointsPerHour` for each game hour left, at least 1 point.
- With Exiled's Toolkit installed, a notice pops up when a machine finishes or a generator runs out of fuel. Click it to open the Machines window. `Toasts` turns them off.
- `FinishClock` makes the timers show the game time a job finishes instead of the time left.
- `Preset` sets every machine speed at once: normal speed, Relaxed (2x), Fast (5x) or Instant. Custom keeps the speed you set for each machine.
- `FollowDayLength` speeds machines up to match Day Length, so they keep the same real-time pace on a longer day.
- New `[Amounts]` settings. You can get more out of the shredder, Ration Press, brewing, compost bins and harvests, get more bottles per brew, charge robots faster, make fridges and freezers stronger, get more power from each unit of generator fuel and cut the wear on defense devices (or turn it off).
- Made for Survival Log 1.1.19131.

## 0.4.2

- After a game update, Mod Manager can tell you if this mod may need an update.
- Made for Survival Log 1.1.19131.

## 0.4.1

- The timer over a brewing barrel shows the sped-up time. It used to show the full normal time (a day or more) while the brew took seconds.
- The timer over a dye vat shows the sped-up time for the bucket being dyed.
- The shredder's clock shows game hours and minutes, like the timer over the machine. It used to show minutes and seconds, which looked like a long wait and jumped when the shred started.
- The brewing and dye vat panels now count minutes the same way as the timers over the machines, so the two always match.
- Changing the cooking speed while a dish is cooking no longer burns a pile of fuel at once (or makes the rest of the dish free). A dish burns fuel at the speed it started with.
- Fixed: the planting panel's estimate could stay at the normal time when you switched to a seed that takes as long as the last one.
- A shredder panel that was already open shows the sped-up clock right away.

## 0.4.0

- The brewing barrel's time for a brew and its countdown show the sped-up time, to the minute, and count down live.
- The dye vat's time per bucket, batch estimate, bucket countdown and time to finish the batch show the sped-up time, to the minute.
- The timer over each plant shows the sped-up time. Plants that were already growing when you changed the speed, or that were planted in an earlier game at another speed, switch to the new speed within a few seconds and keep how far they have grown.
- The growth estimate in the planting panel shows the sped-up time.
- The shredder's clock before you start it, and the time in each item's tooltip, show the sped-up time.

## 0.3.0

- Mod Manager's Mods screen links to this changelog.
- Settings changed on the Mod Manager's Mods screen apply right away. A shred, press batch, dish or drone trip that is already running keeps the speed it started with. Brews, dye jobs, compost bins, plants and generators switch to the new speed right away.
- Mod Manager's Mods screen shows the mod's name, description, category and links.
- Exiled's Toolkit 0.4.0 or newer is optional. With it, settings you change on the Mods screen apply right away. Without it, they apply the next time you start the game.

## 0.2.0

- Speeds now go up to 100000, and the shortest job is 1 game second instead of 1 minute or 1 hour. A shred, ration batch, dye bucket or brew can be close to instant.
- New `Compost` setting: how fast compost bins turn food into compost.
- New `CookingFuel` setting: how much fuel a stove or hot pot uses for each dish.
- Fixed: faster cooking used less fuel per dish. A dish now uses the normal amount at any speed, and `CookingFuel` changes it from there.
- Brews and dye jobs that were already running now follow the speed setting when you start the game again.
- The shredder and Ration Press panels show how long a job will really take.

## 0.1.0

- First release.
- Speeds for the shredder, the Ration Press, the dye vat, plant growth, fuel generator fuel burn, drone trips, brewing and cooking.
- Brewing and cooking come from Action Speed, and the first time the mod starts it takes your values for them from Action Speed's config.
- Every speed starts at normal speed.
