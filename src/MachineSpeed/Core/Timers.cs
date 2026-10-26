using System;
using System.Globalization;
using System.Text;

namespace MachineSpeed.Core;

/// <summary>
/// The time left on a job the game times in whole hours, worked out from how far the sped-up clock has really moved
/// it. Every result is in game seconds at the machine's speed, which is what a countdown on screen has to show.
/// </summary>
public static class Countdown
{
    private const double SecondsPerHour = 3600.0;

    /// <summary>
    /// Game seconds of a job done: its whole hours, the seconds into the current game hour, and the part of an hour
    /// the sped-up clock is carrying that the game has not been handed yet.
    /// </summary>
    public static double Done(int elapsedHours, int nowSeconds, int nowHour, double carrySeconds)
    {
        var intoHour = Math.Clamp(nowSeconds - (double)nowHour * SecondsPerHour, 0, SecondsPerHour - 1);

        return elapsedHours * SecondsPerHour + intoHour + carrySeconds;
    }

    /// <summary>How long a job of <paramref name="gameHours"/> takes at this speed; null when the game does not time it.</summary>
    public static double? Estimate(int gameHours, Speed speed) => gameHours <= 0 ? null : gameHours * SecondsPerHour / speed.Value;

    /// <summary>The time left until a job of <paramref name="totalHours"/> is done; never under 0.</summary>
    public static double Remaining(int totalHours, double done, Speed speed) => Math.Max(0, totalHours * SecondsPerHour - done) / speed.Value;

    /// <summary>The time left on the bucket being dyed now; 0 once the last bucket is done.</summary>
    public static double BucketRemaining(int hoursPerBucket, int buckets, double done, Speed speed)
    {
        var perBucket = hoursPerBucket * SecondsPerHour;

        if (perBucket <= 0 || done >= perBucket * buckets) return 0;

        var intoBucket = done <= 0 ? 0 : done % perBucket;

        return (perBucket - intoBucket) / speed.Value;
    }
}

/// <summary>
/// The timer over a brewing barrel or dye vat: its whole length and how much of it has run, in game seconds at the
/// machine's speed. The game sends its own hours there, so a sped-up job read as days while it took seconds.
/// </summary>
public readonly record struct HeadBar(float Total, float Elapsed)
{
    private const double SecondsPerHour = 3600.0;

    /// <summary>A job of <paramref name="totalHours"/> with <paramref name="done"/> game seconds of it done.</summary>
    public static HeadBar Job(int totalHours, double done, Speed speed)
    {
        var total = totalHours * SecondsPerHour;

        return new HeadBar((float)(total / speed.Value), (float)(Math.Clamp(done, 0, total) / speed.Value));
    }

    /// <summary>The bucket being dyed now; full once the last bucket is done.</summary>
    public static HeadBar Bucket(int hoursPerBucket, int buckets, double done, Speed speed)
    {
        var total = (float)(hoursPerBucket * SecondsPerHour / speed.Value);
        var left = (float)Countdown.BucketRemaining(hoursPerBucket, buckets, done, speed);

        return new HeadBar(total, Math.Clamp(total - left, 0f, total));
    }
}

/// <summary>
/// A plant keeps its growth time from when it was planted, so one planted at another speed grows, and counts down on
/// its head bar, at that old speed. Its time is put right by keeping how far it has grown and changing what it grows
/// toward. Small differences are the game's own (power, the greenhouse) and are left to it.
/// </summary>
public static class PlantResync
{
    private const float MostDrift = 1.25f;

    /// <summary>True when the growth time a plant kept is far enough from the one it should have to be a change of speed.</summary>
    public static bool IsStale(float kept, float target)
    {
        if (!float.IsFinite(kept) || !float.IsFinite(target) || kept <= 0f || target <= 0f) return false;

        var ratio = kept / target;

        return ratio > MostDrift || ratio < 1f / MostDrift;
    }

    /// <summary>The planting time that keeps the plant as far grown under the new growth time as it is now.</summary>
    public static int NewStart(int nowSeconds, int startSeconds, float kept, float target)
    {
        if (!float.IsFinite(kept) || !float.IsFinite(target) || kept <= 0f || target <= 0f) return startSeconds;

        var grown = (nowSeconds - (double)startSeconds) * target / kept;

        return nowSeconds - (int)Math.Round(grown, MidpointRounding.AwayFromZero);
    }
}

/// <summary>The shredder panel's times.</summary>
public static class ShredderTimes
{
    /// <summary>
    /// With nothing shredding, the panel's clock is the next item's minutes in seconds. That is sped up the same way
    /// the item's job will be; a clock counting down a running job is already sped up and is left alone.
    /// </summary>
    public static int IdleSeconds(int totalSeconds, int gameMinutes, Speed speed) =>
        gameMinutes > 0 && totalSeconds == gameMinutes * 60 ? Scaling.Seconds(totalSeconds, speed) : totalSeconds;
}

/// <summary>A brewing barrel's times in game seconds at its speed; null where the panel has nothing to show.</summary>
public readonly record struct BrewTimes(double? Preview, double? Remain);

/// <summary>A dye vat's times in game seconds at its speed; null where the panel has nothing to show.</summary>
public readonly record struct DyeTimes(double? PerBucket, double? Preview, double? BucketRemain, double? TotalRemain);

/// <summary>
/// Everything the page script is told, in the whole seconds it is told them. Two prints are equal exactly when the
/// page would be sent the same text, so an unchanged second is found without building any text.
/// </summary>
public readonly record struct PagePrint(
    float? Shredder,
    bool HasBrew,
    long? BrewPreview,
    long? BrewRemain,
    bool HasDye,
    long? DyePerBucket,
    long? DyePreview,
    long? DyeBucketRemain,
    long? DyeTotalRemain);

/// <summary>
/// What the page script is told, as JSON. A machine left at the game's pace is sent as null, so its panel stays
/// exactly as the game draws it.
/// </summary>
public static class PageState
{
    public static PagePrint Print(Speed shredder, BrewTimes? brew, DyeTimes? dye) => new(
        shredder.IsGamePace ? null : shredder.Value,
        brew.HasValue,
        Seconds(brew?.Preview),
        Seconds(brew?.Remain),
        dye.HasValue,
        Seconds(dye?.PerBucket),
        Seconds(dye?.Preview),
        Seconds(dye?.BucketRemain),
        Seconds(dye?.TotalRemain));

    public static string Json(Speed shredder, BrewTimes? brew, DyeTimes? dye) => Json(Print(shredder, brew, dye));

    public static string Json(PagePrint print)
    {
        var json = new StringBuilder("{\"shredder\":");

        json.Append(print.Shredder is { } shredder ? shredder.ToString("R", CultureInfo.InvariantCulture) : "null");
        json.Append(",\"brew\":");

        if (print.HasBrew) json.Append("{\"preview\":").Append(Text(print.BrewPreview)).Append(",\"remain\":").Append(Text(print.BrewRemain)).Append('}');
        else json.Append("null");

        json.Append(",\"dye\":");

        if (print.HasDye)
        {
            json.Append("{\"perBucket\":").Append(Text(print.DyePerBucket)).Append(",\"preview\":").Append(Text(print.DyePreview))
                .Append(",\"bucketRemain\":").Append(Text(print.DyeBucketRemain)).Append(",\"totalRemain\":").Append(Text(print.DyeTotalRemain)).Append('}');
        }
        else
        {
            json.Append("null");
        }

        return json.Append('}').ToString();
    }

    /// <summary>Whole seconds, rounded up so a job still running never reads 0; null for no time.</summary>
    private static long? Seconds(double? value) => value is { } v && double.IsFinite(v) ? (long)Math.Ceiling(Math.Max(0, v)) : null;

    private static string Text(long? seconds) => seconds is { } s ? s.ToString(CultureInfo.InvariantCulture) : "null";
}
