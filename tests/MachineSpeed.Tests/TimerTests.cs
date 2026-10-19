using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class CountdownTests
{
    private static readonly Speed Ten = Speed.From(10f);

    [Fact]
    public void Work_done_counts_whole_hours_the_seconds_into_this_hour_and_the_carried_time()
    {
        // 3 hours in, 15 minutes into hour 100, and 1800 seconds of sped-up time not yet handed to the game.
        Assert.Equal(3 * 3600 + 900 + 1800, Countdown.Done(3, 100 * 3600 + 900, 100, 1800));
    }

    [Fact]
    public void A_queued_job_shows_its_length_at_the_machines_speed()
    {
        Assert.Equal(48 * 3600 / 10.0, Countdown.Estimate(48, Ten));
        Assert.Null(Countdown.Estimate(0, Ten));
    }

    [Fact]
    public void A_running_brew_counts_down_what_is_left_at_its_speed()
    {
        var done = Countdown.Done(12, 500 * 3600, 500, 0);

        // 36 game hours left, at x10 that is 3.6 hours of game time.
        Assert.Equal(36 * 3600 / 10.0, Countdown.Remaining(48, done, Ten), 6);
    }

    [Fact]
    public void A_job_already_past_its_end_shows_no_time_left()
    {
        Assert.Equal(0, Countdown.Remaining(4, 5 * 3600, Ten));
    }

    [Fact]
    public void The_dye_bucket_countdown_starts_over_for_each_bucket()
    {
        // 6 hours a bucket, 3 buckets, 7 hours done: 5 hours left on the second bucket.
        Assert.Equal(5 * 3600 / 10.0, Countdown.BucketRemaining(6, 3, 7 * 3600, Ten), 6);
    }

    [Fact]
    public void A_dye_job_whose_last_bucket_is_done_has_no_bucket_time_left()
    {
        Assert.Equal(0, Countdown.BucketRemaining(6, 3, 18 * 3600, Ten));
    }
}

public class HeadBarTests
{
    private static readonly Speed Hundred = Speed.From(100f);

    [Fact]
    public void A_brews_head_bar_counts_down_the_sped_up_time_not_the_games_hours()
    {
        // A 30 hour brew 6 hours in, at x100: the bar's whole length is 18 game minutes and 14.4 of them are left.
        var bar = HeadBar.Job(30, 6 * 3600, Hundred);

        Assert.Equal(30 * 3600 / 100f, bar.Total, 3);
        Assert.Equal(24 * 3600 / 100f, bar.Total - bar.Elapsed, 3);
    }

    [Fact]
    public void A_brew_head_bar_never_runs_past_its_ends()
    {
        Assert.Equal(0f, HeadBar.Job(30, -500, Hundred).Elapsed);
        Assert.Equal(HeadBar.Job(30, 0, Hundred).Total, HeadBar.Job(30, 40 * 3600, Hundred).Elapsed);
    }

    [Fact]
    public void A_dye_vats_head_bar_counts_down_the_bucket_being_dyed_at_its_speed()
    {
        // 6 hours a bucket, 3 buckets, 7 hours done at x100: the second bucket has 5 of its 6 hours left.
        var bar = HeadBar.Bucket(6, 3, 7 * 3600, Hundred);

        Assert.Equal(6 * 3600 / 100f, bar.Total, 3);
        Assert.Equal(5 * 3600 / 100f, bar.Total - bar.Elapsed, 3);
    }

    [Fact]
    public void A_dye_vat_whose_last_bucket_is_done_shows_a_full_bar()
    {
        var bar = HeadBar.Bucket(6, 3, 18 * 3600, Hundred);

        Assert.Equal(bar.Total, bar.Elapsed);
    }
}

public class PlantResyncTests
{
    [Theory]
    [InlineData(1000f, 100f)]
    [InlineData(100f, 1000f)]
    public void A_growth_time_from_another_speed_is_stale(float kept, float target)
    {
        Assert.True(PlantResync.IsStale(kept, target));
    }

    [Theory]
    [InlineData(1000f, 1000f)]
    [InlineData(1100f, 1000f)]
    [InlineData(0f, 1000f)]
    [InlineData(1000f, float.NaN)]
    public void A_small_difference_or_an_untimed_plant_is_left_to_the_game(float kept, float target)
    {
        Assert.False(PlantResync.IsStale(kept, target));
    }

    [Fact]
    public void A_resynced_plant_keeps_how_far_it_has_grown()
    {
        // Planted at 1000 with 10000 seconds to grow, 4000 grown (40%). At a tenth of the time, 40% is 400 seconds.
        var start = PlantResync.NewStart(5000, 1000, 10000f, 1000f);

        Assert.Equal(4600, start);
        Assert.Equal(0.4, (5000 - start) / 1000.0, 6);
    }

    [Fact]
    public void A_plant_with_no_growth_time_keeps_its_start()
    {
        Assert.Equal(1000, PlantResync.NewStart(5000, 1000, 0f, 1000f));
    }
}

public class ShredderTimesTests
{
    [Fact]
    public void An_idle_shredder_shows_the_next_items_sped_up_time()
    {
        Assert.Equal(30, ShredderTimes.IdleSeconds(300, 5, Speed.From(10f)));
    }

    [Fact]
    public void A_running_shredders_clock_is_already_sped_up_and_left_alone()
    {
        Assert.Equal(17, ShredderTimes.IdleSeconds(17, 5, Speed.From(10f)));
    }
}

public class PageStateTests
{
    [Fact]
    public void Machines_at_the_games_pace_send_nothing_so_their_panels_stay_the_games()
    {
        Assert.Equal("{\"shredder\":null,\"brew\":null,\"dye\":null}", PageState.Json(Speed.GamePace, null, null));
    }

    [Fact]
    public void Times_are_whole_seconds_rounded_up_and_missing_ones_are_null()
    {
        var json = PageState.Json(Speed.From(2.5f), new BrewTimes(17280.2, null), new DyeTimes(2160, 6480, 0.4, 4000.9));

        Assert.Equal("{\"shredder\":2.5,\"brew\":{\"preview\":17281,\"remain\":null},\"dye\":{\"perBucket\":2160,\"preview\":6480,\"bucketRemain\":1,\"totalRemain\":4001}}", json);
    }
}

public class SpedUpClockCarryTests
{
    [Fact]
    public void Time_under_an_hour_is_carried_for_the_countdown()
    {
        var clock = new SpedUpClock();

        clock.ExtraHours(0, Speed.From(3f));
        clock.ExtraHours(1000, Speed.From(3f));

        // 1000 seconds at x3 is 2000 seconds extra, under an hour, so all of it is carried.
        Assert.Equal(2000, clock.CarrySeconds, 6);
    }
}
