using System;
using System.Globalization;

namespace MachineSpeed.Core;

/// <summary>Game times as the window and the timer over a machine write them.</summary>
public static class GameClock
{
    private const long SecondsPerDay = 86400;

    /// <summary>
    /// Time left as "H:MM", with days in front when there are any. Whole minutes are counted the way the timer over
    /// the machine counts them, so the window and that timer read the same.
    /// </summary>
    public static string Left(double seconds, bool isChinese)
    {
        var minutes = (long)Math.Floor(Clean(seconds) / 60);
        var days = minutes / 1440;
        var clock = string.Format(CultureInfo.InvariantCulture, "{0}:{1:D2}", minutes % 1440 / 60, minutes % 60);

        return days > 0 ? $"{days}{DayUnit(isChinese)} {clock}" : clock;
    }

    /// <summary>
    /// The game-clock time a job ends, as "HH:MM", with "+Nd " in front when that is on a later day than now. A part
    /// of a second left still counts as a second, so a job is never shown as done before it is.
    /// </summary>
    public static string Finish(long nowSeconds, double remainSeconds, bool isChinese)
    {
        var now = Math.Max(0, nowSeconds);
        var finish = now + (long)Math.Ceiling(Clean(remainSeconds));
        var minuteOfDay = finish % SecondsPerDay / 60;
        var laterDays = finish / SecondsPerDay - now / SecondsPerDay;
        var clock = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", minuteOfDay / 60, minuteOfDay % 60);

        return laterDays > 0 ? $"+{laterDays}{DayUnit(isChinese)} {clock}" : clock;
    }

    private static string DayUnit(bool isChinese) => isChinese ? "天" : "d";

    // Ten years of game time is far past any job; it keeps a broken value from overflowing the clock.
    private static double Clean(double seconds) => double.IsFinite(seconds) ? Math.Clamp(seconds, 0, 3650 * SecondsPerDay) : 0;
}
