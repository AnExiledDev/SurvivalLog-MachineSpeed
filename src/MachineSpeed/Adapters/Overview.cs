using System;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// Wires the Machines window: its settings, its hooks, and the ticker that runs it. The window, its HUD button, the
/// done toasts and Rush all live in <see cref="OverviewSession"/>; the finish clock over machines is
/// <see cref="FinishClockHeadBar"/>.
/// </summary>
internal static class Overview
{
    private const string Section = "Overview";

    public static void Install(BasePlugin plugin, ConfigFile config, Harmony harmony, SpeedLog log)
    {
        var toasts = config.Bind(Section, "Toasts", true,
            "Show a notice when a machine finishes, a drone comes back or a fuel generator runs out of fuel. Clicking it opens the Machines window.\n" +
            "机器完成、无人机返回或燃油发电机燃料耗尽时显示通知，点击通知打开「机器总览」窗口。");
        var rushPointsPerHour = config.Bind(Section, "RushPointsPerHour", (float)RushCost.DefaultPointsPerHour,
            "Survival points the Rush button in the Machines window costs for each game hour a job has left. The cost is rounded up to whole points, at least 1. From 0.01 to 1000; 0 or less counts as 0.25.\n" +
            "「机器总览」窗口中「加速完成」按钮按任务剩余的每个游戏小时花费的生存点。花费向上取整，至少 1 点。范围 0.01 到 1000，0 或更小按 0.25 计算。");
        var finishClock = config.Bind(Section, "FinishClock", false,
            "Show the game time a job will be done instead of the time it has left, in the Machines window and in the timers over plants, brewing barrels and dye vats.\n" +
            "在「机器总览」窗口以及植物、酿酒桶和染料桶上方的计时中，显示任务完成时的游戏时间，而不是剩余时间。");
        var session = new OverviewSession(new MachineReader(log.Write), new OverviewPage(log.Write), log.Write);

        OverviewInputGate.Apply(harmony, log.Write);
        OverviewMessages.Apply(harmony, log.Write);
        FinishClockHeadBar.Apply(harmony, log);
        OptionalLive.Watch(() =>
        {
            session.UseSettings(toasts.Value, rushPointsPerHour.Value, finishClock.Value);
            FinishClockHeadBar.Use(finishClock.Value);
        }, toasts, rushPointsPerHour, finishClock);
        OverviewTicker.Session = session;
        OverviewTicker.Log = log.Write;

        try
        {
            plugin.AddComponent<OverviewTicker>();
        }
        catch (Exception ex)
        {
            log.Write($"could not start the Machines window, its HUD button, toasts and Rush are off: {ex.Message}");
        }
    }
}
