Name: Machine Speed
Summary: Sets how fast the machines at your base work: the shredder, the Ration Press, the dye vat, compost bins, plant growth, fuel generators, drone trips, brewing and cooking, each with its own speed. 设置基地里各台机器的工作速度：粉碎机、口粮压制机、染料桶、堆肥箱、植物生长、燃油发电机、无人机出行、酿酒和烹饪，每项都可单独设置。

Description (BBCode):
Machine Speed lets you choose how fast each machine at your base works. 2 means a job takes half the game time, 0.5 means twice as long. Speeds go up to 100000, so a job can be close to instant: the shortest job is 1 game second. Every speed defaults to 1, and at 1 the mod does not touch that machine at all, so you only change what you set.

[size=5]Main features[/size]
[list]
[*][b]Shredder[/b] and [b]Ration Press[/b]: a job takes less game time, down to 1 second. Their panels, and the shredder's item tooltips, show the time a job will really take. The shredder's clock shows game hours and minutes, the same as the timer over the machine.[/*]
[*][b]Dye vat[/b]: each bucket takes less time, and a fast vat finishes a bucket in seconds. The vat's panel shows the real time per bucket, for the batch, and left on a running job, to the minute, and the timer over the vat shows the real time for the bucket being dyed.[/*]
[*][b]Compost bins[/b]: food in a compost bin turns into compost sooner. Only the bin is sped up; food anywhere else rots at the game's pace.[/*]
[*][b]Plant growth[/b]: plants grow faster or slower. The timer over each plant and the planting panel's estimate show the sped-up time, and plants already growing when the speed changes move to the new speed within a few seconds.[/*]
[*][b]Fuel burn[/b]: how fast fuel generators use up fuel. 2 burns it twice as fast; 0.5 makes a tank last twice as long. The time left on the generator's panel follows it.[/*]
[*][b]Drone trips[/b]: a drone comes back sooner.[/*]
[*][b]Brewing[/b]: a brew takes less time, and a fast brew finishes in seconds. The brewing panel and the timer over the barrel show the real time a brew will take and the real time left on a running one, to the minute.[/*]
[*][b]Cooking[/b] at a stove or hot pot takes less time. It burns the same fuel per dish at any speed.[/*]
[*][b]Cooking fuel[/b]: how much fuel a stove or hot pot uses for each dish. 0.5 is half, 0 uses none.[/*]
[*][b]Takes your old settings[/b]: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your [font=Courier New]Brewing[/font] and [font=Courier New]Cooking[/font] values from Action Speed's config and uses them.[/*]
[/list]
A shred, ration batch, drone trip or cook that is already running keeps the length it started with. Brews, dye jobs, compost bins, plants and fuel generators follow a new speed straight away, including the ones already running.

Machine Speed writes no data of its own into your save and does not edit game files. A job's sped-up length is held by the job, so it can end up in your save. You can remove the mod at any time; jobs started after that take the game's own time again.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]When you change a speed while a brew or dye job is running, the timer over the machine catches up at the next game hour. The panel shows the new time at once.[/*]
[*]A change to [font=Courier New]CookingFuel[/font] while a dish is cooking applies to the whole dish, including the part already cooked.[/*]
[*]Change [font=Courier New]Cooking[/font] between games only when no dish is cooking. A dish saved mid-cook and loaded at another speed burns its fuel at the new speed, so it can use a lot of fuel at once (or very little).[/*]
[*]Plants planted at another speed keep it until harvest when [font=Courier New]PlantGrowth[/font] is set back to 1 between games. Within a game, setting it back to 1 moves them to the game's pace.[/*]
[/list]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.18902. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url] (BepInEx 6.0.0-be.788, IL2CPP). Install it first.[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 or newer. With it, settings changed on the Mod Manager's Mods screen take effect without restarting the game. Without it, they take effect at the next game start.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Optional, to change settings without a restart: install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]MachineSpeed.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
To change a speed, edit [font=Courier New]BepInEx\config\com.dev.survivallog.machinespeed.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen, where changes take effect at once (see [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). Without Exiled's Toolkit installed, changes made on the Mods screen take effect at the next game start instead.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\machine-speed.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].
Coming soon: a Survival Log wiki with detailed documentation for all of my mods.

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed]github.com/AnExiledDev/SurvivalLog-MachineSpeed[/url]

[line]
[size=5]简体中文[/size]

Machine Speed 让你设置基地里每台机器的工作速度。2 表示一项工作只花一半的游戏时间，0.5 表示花两倍的时间。速度最高可设为 100000，所以一项工作几乎可以瞬间完成：最短的工作是 1 游戏秒。每项速度默认都是 1，为 1 时模组完全不改动那台机器，所以只有你设置过的才会改变。

[size=5]主要功能[/size]
[list]
[*][b]粉碎机[/b]和[b]口粮压制机[/b]：一项工作花的游戏时间更少，最短 1 秒。它们的面板以及粉碎机的物品提示会显示工作实际要花的时间。粉碎机的时钟以游戏小时和分钟显示，和机器上方的计时器一致。[/*]
[*][b]染料桶[/b]：每一桶花的时间更少，速度够快的染料桶几秒就能染完一桶。染料桶的面板会显示每桶、整批以及正在进行的工作剩余的实际时间，精确到分钟；染料桶上方的计时器显示正在染的这一桶的实际时间。[/*]
[*][b]堆肥箱[/b]：堆肥箱里的食物更快变成堆肥。只有堆肥箱会加速，其他地方的食物仍按游戏原本的速度腐烂。[/*]
[*][b]植物生长[/b]：植物长得更快或更慢。每株植物上方的计时器和种植面板的预估都显示加速后的时间；改变速度时已经在生长的植物，几秒内就会按新速度生长。[/*]
[*][b]燃油消耗[/b]：燃油发电机消耗燃油的快慢。2 表示消耗快一倍；0.5 表示一箱油能用两倍久。发电机面板上的剩余时间会随之变化。[/*]
[*][b]无人机出行[/b]：无人机更早返回。[/*]
[*][b]酿酒[/b]：酿一次酒花的时间更少，速度够快时几秒就能酿好。酿酒面板和酿酒桶上方的计时器会显示一次酿造实际要花的时间，以及正在酿造的酒实际还剩多少时间，精确到分钟。[/*]
[*]在燃气灶或火锅上[b]烹饪[/b]花的时间更少。无论速度多快，每道菜消耗的燃料都一样。[/*]
[*][b]烹饪燃料[/b]：燃气灶或火锅每做一道菜消耗多少燃料。0.5 是一半，0 表示不消耗。[/*]
[*][b]沿用你原来的设置[/b]：酿酒和烹饪以前在 Action Speed 里。Machine Speed 第一次启动时，会读取你在 Action Speed 配置中的 [font=Courier New]Brewing[/font] 和 [font=Courier New]Cooking[/font] 数值并沿用。[/*]
[/list]
已经在进行的粉碎、口粮压制、无人机出行或烹饪，会保持开始时的时长。酿酒、染色、堆肥箱、植物和燃油发电机会立即按新速度运行，包括已经在进行的。

Machine Speed 不会往存档里写入自己的数据，也不会修改游戏文件。加速后的工作时长保存在工作本身上，所以可能会进入你的存档。你可以随时移除模组；移除后开始的工作会恢复游戏原本的时间。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]在酿酒或染色进行中改变速度时，机器上方的计时器要到下一个游戏整点才会更新。面板会立即显示新的时间。[/*]
[*]烹饪进行中改动 [font=Courier New]CookingFuel[/font] 时，新数值会作用于整道菜，包括已经烹饪过的部分。[/*]
[*]请只在没有菜在烹饪时，于两次游戏之间改动 [font=Courier New]Cooking[/font]。烹饪中途存档的菜在以其他速度读档后，会按新速度消耗燃料，可能一下子用掉很多燃料（或用得很少）。[/*]
[*]如果在两次游戏之间把 [font=Courier New]PlantGrowth[/font] 改回 1，按其他速度种下的植物会保持原来的速度直到收获。在游戏中改回 1 时，它们会恢复游戏原本的速度。[/*]
[/list]

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.18902。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]（BepInEx 6.0.0-be.788，IL2CPP），请先安装。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.4.0 或更新版本。装了它，在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效；没装的话，改动在下次启动游戏时生效。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]可选（改动设置无需重启游戏）：用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]MachineSpeed.dll[/font]。

[size=5]配置[/size]
如需修改速度，可以在游戏内 Mod Manager 的「模组」界面修改（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.machinespeed.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。没装 Exiled's Toolkit 的话，在「模组」界面的改动改为在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\machine-speed.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。
即将推出：Survival Log 维基，收录我所有模组的详细说明。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed]github.com/AnExiledDev/SurvivalLog-MachineSpeed[/url]
