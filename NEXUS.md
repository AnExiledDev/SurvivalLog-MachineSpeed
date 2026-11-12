Name: Machine Speed
Summary: Choose how fast each machine at your base works and how much it makes: the shredder, Ration Press, dye vat, compost bins, plants, drones, brewing and cooking. A Machines window lists every job with its time left, and you also get done notices, a finish clock, speed presets and a Rush button that finishes a job right away for survival points.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/save/2-changes-stay.png[/img][/center]

Machine Speed lets you choose how fast each machine at your base works. 2 means a job takes half the game time, 0.5 means it takes twice as long. Speeds go up to 100000, so a job can be close to instant, and the shortest job is 1 game second. Every speed defaults to 1, and at 1 the mod doesn't touch that machine at all, so only what you change is changed.

[size=5]Main features[/size]
[list]
[*][b]Shredder[/b] and [b]Ration Press[/b]: jobs go faster or slower, down to 1 game second. Their panels and the shredder's item tooltips show how long a job will really take. The shredder's clock shows game hours and minutes, like the timer over the machine.[/*]
[*][b]Dye vat[/b]: each bucket takes less time, and a fast vat finishes a bucket in seconds. The vat's panel shows the real time for each bucket, for the whole batch and for what's left of a running job, to the minute. The timer over the vat shows the real time for the bucket being dyed.[/*]
[*][b]Compost bins[/b]: food in a compost bin turns into compost sooner. Only the bin is sped up, so food anywhere else rots at its normal speed.[/*]
[*][b]Plant growth[/b]: plants grow faster or slower. The timer over each plant and the estimate in the planting panel show the sped-up time. When you change the speed, plants that are already growing switch to it within a few seconds.[/*]
[*][b]Drone trips[/b]: a drone comes back sooner.[/*]
[*][b]Brewing[/b]: a brew takes less time, and a fast brew finishes in seconds. The brewing panel and the timer over the barrel show how long a brew will really take and how much time a running one has left, to the minute.[/*]
[*][b]Cooking[/b]: dishes at a stove or hot pot cook faster. A dish uses the same fuel at any speed.[/*]
[*][b]Cooking fuel[/b]: how much fuel a stove or hot pot uses for each dish. 0.5 is half, 0 uses none.[/*]
[*][b]Presets[/b]: [font=Courier New]Preset[/font] sets every speed at once, to normal speed (Default), Relaxed (2x), Fast (5x) or Instant.[/*]
[*][b]Shredder auto-start[/b]: with [font=Courier New]ShredderAutoStart[/font] on, an idle shredder starts by itself when you put items into it.[/*]
[*][b]Follow Day Length[/b]: if you use the Day Length mod, [font=Courier New]FollowDayLength[/font] keeps machines working at the same real-time pace on longer or shorter days.[/*]
[*][b]Amounts[/b]: you can get more out of the shredder, Ration Press, brewing barrels, compost bins and harvests, make bigger brews, charge robots faster, make fridges and freezers keep food longer and cut the wear on defense devices.[/*]
[*][b]Machines window[/b]: a Machines button in the tool column lists every machine at home with its picture, what it is making, the time left and when it finishes. Machines that need you come first and say what to do, then working ones with a progress bar, then idle ones. The button shows how many machines are working and how many need you. Esc closes the window.[/*]
[*][b]Rush[/b]: a Rush button in the Machines window finishes a running job right away for survival points. You set the cost with [font=Courier New]RushPointsPerHour[/font], 0.25 points per game hour left by default, and a rush always costs at least 1 point. Compost bins, plants, generators and paused jobs can't be rushed.[/*]
[*][b]Collect[/b]: a finished dish or brew has a Collect button in the Machines window. Your survivor finishes what they're doing, then walks to the stove or brewing barrel and takes it out. Other machines are emptied at the machine as usual.[/*]
[*][b]UI size[/b]: the UI size setting in Exiled's Toolkit makes the Machines window bigger or smaller, as long as it still fits on the screen. Machine Speed has its own [font=Courier New]UiScale[/font] setting if you want a different size for this mod.[/*]
[*][b]Done notices[/b]: a notice pops up when a machine finishes, a drone comes back or a generator runs out of fuel. Click it to open the Machines window.[/*]
[*][b]Finish clock[/b]: [font=Courier New]FinishClock[/font] shows the game time a job finishes instead of the time left.[/*]
[*][b]Takes your old settings[/b]: brewing and cooking used to be in Action Speed. The first time Machine Speed starts, it reads your [font=Courier New]Brewing[/font] and [font=Courier New]Cooking[/font] values from Action Speed's config and uses them.[/*]
[/list]
When you change a speed, a shred, ration batch, drone trip or dish that is already running keeps the length it started with. Brews, dye jobs, compost bins and plants switch to the new speed right away, even the ones already running.

Machine Speed doesn't write any data of its own into your save and doesn't edit game files. A job does hold its sped-up length though, so that length can end up in your save. You can remove the mod at any time, and jobs started after that take their normal time again.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]When you change a speed while a brew or dye job is running, the timer over the machine catches up at the next game hour. The panel shows the new time right away.[/*]
[*]A change to [font=Courier New]CookingFuel[/font] while a dish is cooking applies to the whole dish, including the part already cooked.[/*]
[*]Only change [font=Courier New]Cooking[/font] between games when no dish is cooking. A dish saved mid-cook and loaded at another speed burns its fuel at the new speed, so it can use a lot of fuel at once (or very little).[/*]
[*]If you set [font=Courier New]PlantGrowth[/font] back to 1 between games, plants planted at another speed keep that speed until harvest. Setting it back to 1 while you play moves them back to normal speed.[/*]
[*]In a cold room, a fridge uses whichever is colder, the room or the fridge. [font=Courier New]FridgeStrength[/font] doesn't stack with the cold of the room.[/*]
[*]With [font=Courier New]EquipmentWear[/font] changed, the wear line in the wave report still shows the normal amount of wear.[/*]
[*]With [font=Courier New]CompostOutput[/font] above 1, any compost that doesn't fit in the bin is lost.[/*]
[*]The Ration Press panel shows the normal output, without [font=Courier New]RationPressOutput[/font]. The batch still gives you the bigger amount.[/*]
[*]A rushed job also shows its done notice.[/*]
[*]With [font=Courier New]FinishClock[/font] on, the shredder, brewing and dye vat panels still show the time left.[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.20719. A game update can break the mod until it is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url] (BepInEx 6.0.0-be.788, IL2CPP). Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 or newer. Machine Speed doesn't load without it. It shows the done notices and Machine Speed's other messages as toasts, applies settings changed on the Mod Manager's Mods screen without a restart, and its UI size setting sizes the Machines window.[/*]
[*]Optional: Day Length, for [font=Courier New]FollowDayLength[/font].[/*]
[*]Optional: [url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]. With it, you can change every setting in game on its Mods screen and see what each one does, without opening the config file.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if you haven't installed any mods before, start the game once so BepInEx finishes setting up, then quit)[/i].[/*]
[*]Install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. You should end up with:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]MachineSpeed.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder.

[size=5]Configuration[/size]
You can change the settings in game on the Mod Manager's Mods screen, where changes apply right away, or edit [font=Courier New]BepInEx\config\com.dev.survivallog.machinespeed.cfg[/font] while the game is closed. The cfg file explains each setting in English and Chinese, and [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/blob/main/CONFIG.md]CONFIG.md[/url] lists them all. Each world can keep its own machine speeds, amounts and Rush cost. Pick them for a new world in Customizable Difficulty, or change them for the world you're playing on the Mods screen.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\machine-speed.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord. You can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed]github.com/AnExiledDev/SurvivalLog-MachineSpeed[/url]

[line]
[size=5]简体中文[/size]

Machine Speed 让你设置基地里每台机器的工作速度。2 表示一项工作只花一半的游戏时间，0.5 表示花两倍的时间。速度最高可设为 100000，所以一项工作几乎可以瞬间完成，最短只要 1 游戏秒。每项速度默认都是 1，为 1 时模组完全不改动那台机器，所以只有你改过的才会变。

[size=5]主要功能[/size]
[list]
[*][b]粉碎机[/b]和[b]口粮压制机[/b]：工作可以更快或更慢，最短 1 游戏秒。它们的面板以及粉碎机的物品提示会显示工作实际要花的时间。粉碎机的时钟以游戏小时和分钟显示，和机器上方的计时器一致。[/*]
[*][b]染料桶[/b]：每一桶花的时间更少，速度够快的染料桶几秒就能染完一桶。染料桶的面板会显示每桶、整批以及正在进行的工作剩余的实际时间，精确到分钟。染料桶上方的计时器显示正在染的这一桶的实际时间。[/*]
[*][b]堆肥箱[/b]：堆肥箱里的食物更快变成堆肥。只有堆肥箱会加速，其他地方的食物仍按正常速度腐烂。[/*]
[*][b]植物生长[/b]：植物长得更快或更慢。每株植物上方的计时器和种植面板的预估都显示加速后的时间。改变速度后，已经在生长的植物几秒内就会按新速度生长。[/*]
[*][b]无人机出行[/b]：无人机更早返回。[/*]
[*][b]酿酒[/b]：酿一次酒花的时间更少，速度够快时几秒就能酿好。酿酒面板和酿酒桶上方的计时器会显示一次酿造实际要花的时间，以及正在酿造的酒实际还剩多少时间，精确到分钟。[/*]
[*][b]烹饪[/b]：在燃气灶或火锅上做菜更快。无论速度多快，每道菜消耗的燃料都一样。[/*]
[*][b]烹饪燃料[/b]：燃气灶或火锅每做一道菜消耗多少燃料。0.5 是一半，0 表示不消耗。[/*]
[*][b]预设[/b]：[font=Courier New]Preset[/font] 一次设置所有速度：Default（正常速度）、Relaxed（两倍）、Fast（五倍）或 Instant（几乎立即完成）。[/*]
[*][b]粉碎机自动开始[/b]：开启 [font=Courier New]ShredderAutoStart[/font] 后，向闲置的粉碎机放入物品时它会自动开始粉碎。[/*]
[*][b]跟随昼夜时长[/b]：装了「昼夜时长」模组时，[font=Courier New]FollowDayLength[/font] 让机器在一天变长或变短时保持现实时间里的工作节奏。[/*]
[*][b]产量[/b]：可以让粉碎机、口粮压制机、酿酒桶、堆肥箱和收获的产出更多，一次酿得更多，机器人充电更快，冰箱和冰柜保鲜更久，防御装置损耗更少。[/*]
[*][b]机器总览窗口[/b]：工具栏中的「机器总览」按钮列出基地里的所有机器，显示机器图标、正在制作什么、剩余时间和完成时间。需要你处理的机器排在最前面，并写明该做什么；其次是显示进度条的进行中机器，闲置的机器排在最后。按钮上的两个数字分别表示进行中的机器数量和需要你处理的机器数量。按 Esc 关闭窗口。[/*]
[*][b]加速完成[/b]：「机器总览」窗口中的「加速完成」按钮花费生存点立即完成正在进行的工作。花费由 [font=Courier New]RushPointsPerHour[/font] 设置，默认每剩余一个游戏小时 0.25 点，至少 1 点。堆肥箱、植物、发电机和已暂停的工作不能加速完成。[/*]
[*][b]收取[/b]：「机器总览」窗口中做好的菜或酿好的酒旁有「收取」按钮。角色会在做完手头的事后走到燃气灶或酿酒桶前取出成品。其他机器仍需在机器前取出。[/*]
[*][b]界面大小[/b]：可以用 Exiled's Toolkit 的界面大小设置放大或缩小「机器总览」窗口，只要窗口仍能完整显示在屏幕上。想让本模组用不同的大小，可以改 Machine Speed 自己的 [font=Courier New]UiScale[/font] 设置。[/*]
[*][b]完成通知[/b]：机器完成、无人机返回或发电机燃料耗尽会弹出通知，点击通知打开「机器总览」窗口。[/*]
[*][b]完成时间[/b]：[font=Courier New]FinishClock[/font] 显示工作完成时的游戏时间，而不是剩余时间。[/*]
[*][b]沿用你原来的设置[/b]：酿酒和烹饪以前在 Action Speed 里。Machine Speed 第一次启动时，会读取你在 Action Speed 配置中的 [font=Courier New]Brewing[/font] 和 [font=Courier New]Cooking[/font] 数值并沿用。[/*]
[/list]
改变速度时，已经在进行的粉碎、口粮压制、无人机出行或烹饪会保持开始时的时长。酿酒、染色、堆肥箱和植物会立即按新速度运行，包括已经在进行的。

Machine Speed 不会往存档里写入自己的数据，也不会修改游戏文件。不过加速后的工作时长保存在工作本身上，所以可能会进入你的存档。你可以随时移除模组，移除后开始的工作会恢复正常时间。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]在酿酒或染色进行中改变速度时，机器上方的计时器要到下一个游戏整点才会更新。面板会立即显示新的时间。[/*]
[*]烹饪进行中改动 [font=Courier New]CookingFuel[/font] 时，新数值会作用于整道菜，包括已经烹饪过的部分。[/*]
[*]请只在没有菜在烹饪时，于两次游戏之间改动 [font=Courier New]Cooking[/font]。烹饪中途存档的菜在以其他速度读档后，会按新速度消耗燃料，可能一下子用掉很多燃料（或用得很少）。[/*]
[*]如果在两次游戏之间把 [font=Courier New]PlantGrowth[/font] 改回 1，按其他速度种下的植物会保持原来的速度直到收获。在游戏中改回 1 时，它们会恢复正常速度。[/*]
[*]在寒冷房间里，冰箱取房间和冰箱中更冷的一方。[font=Courier New]FridgeStrength[/font] 不会与房间的低温叠加。[/*]
[*]改动 [font=Courier New]EquipmentWear[/font] 后，波次战报的「装置损耗」仍显示原本的损耗。[/*]
[*][font=Courier New]CompostOutput[/font] 大于 1 时，箱内放不下的堆肥会丢失。[/*]
[*]口粮压制机面板显示的是没有乘以 [font=Courier New]RationPressOutput[/font] 的产量，实际产出是加成后的数量。[/*]
[*]加速完成的工作也会弹出完成通知。[/*]
[*]开启 [font=Courier New]FinishClock[/font] 时，粉碎机、酿酒和染料桶的面板仍显示剩余时间。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.20719。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]（BepInEx 6.0.0-be.788，IL2CPP），请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 或更新版本。没有它本模组不会加载。它把完成通知和本模组的其他提示以弹窗显示，让在 Mod Manager 的「模组」界面改动的设置无需重启游戏即可生效，它的界面大小设置还能调整「机器总览」窗口的大小。[/*]
[*]可选：「昼夜时长」（Day Length），用于 [font=Courier New]FollowDayLength[/font]。[/*]
[*]可选：[url=https://www.nexusmods.com/survivallog/mods/73]Mod Manager[/url]。装了它，可以在游戏里它的「模组」界面修改每项设置，并看到每项的说明，不用打开配置文件。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\MachineSpeed.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]MachineSpeed.dll[/font]。

[size=5]配置[/size]
可以在游戏内 Mod Manager 的「模组」界面修改设置（改动立即生效），也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.machinespeed.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。每个存档都可以有自己的机器速度、产量和「加速完成」花费：新存档可以在「自定义难度」里选，游戏中也可以在「模组」界面只为当前存档修改。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\machine-speed.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-MachineSpeed]github.com/AnExiledDev/SurvivalLog-MachineSpeed[/url]
