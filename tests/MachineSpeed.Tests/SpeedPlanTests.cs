using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class PresetTests
{
    private static readonly Speed OwnSpeed = Speed.From(3f);

    [Fact]
    public void Custom_keeps_each_machines_own_speed()
    {
        Assert.Same(OwnSpeed, SpeedPlan.Effective(OwnSpeed, Preset.Custom, DayFactor.None));
    }

    [Theory]
    [InlineData(Preset.Vanilla, 1f)]
    [InlineData(Preset.Relaxed, 2f)]
    [InlineData(Preset.Fast, 5f)]
    [InlineData(Preset.Instant, Speed.Fastest)]
    public void A_preset_other_than_custom_overrides_the_machines_own_speed(Preset preset, float expected)
    {
        Assert.Equal(expected, SpeedPlan.Effective(OwnSpeed, preset, DayFactor.None).Value);
    }

    [Fact]
    public void The_vanilla_preset_puts_every_machine_back_at_the_games_pace()
    {
        Assert.True(SpeedPlan.Effective(Speed.From(50f), Preset.Vanilla, DayFactor.None).IsGamePace);
    }

    [Fact]
    public void A_preset_not_in_the_list_counts_as_custom()
    {
        var unknown = (Preset)99;

        Assert.Equal(Preset.Custom, SpeedPlan.Known(unknown));
        Assert.Same(OwnSpeed, SpeedPlan.Effective(OwnSpeed, unknown, DayFactor.None));
    }
}

public class DayFactorTests
{
    [Fact]
    public void Days_made_twice_as_long_double_the_factor()
    {
        // Day Length 2 on the home chapter: the game's 240 game seconds per real second becomes 120.
        Assert.Equal(2.0, DayFactor.From(240, 120).Value);
    }

    [Fact]
    public void Shorter_days_give_a_factor_under_one()
    {
        Assert.Equal(0.5, DayFactor.From(120, 240).Value);
    }

    [Theory]
    [InlineData(240, 240)]
    [InlineData(0, 120)]
    [InlineData(240, 0)]
    [InlineData(-5, 120)]
    public void An_unchanged_or_unset_time_scale_changes_nothing(int chapterScale, int actualScale)
    {
        Assert.True(DayFactor.From(chapterScale, actualScale).IsNone);
    }
}

public class FollowDayLengthTests
{
    private static readonly DayFactor TwiceAsLong = DayFactor.From(240, 120);

    [Fact]
    public void Longer_days_speed_a_machine_up_by_the_same_amount()
    {
        Assert.Equal(6f, SpeedPlan.Effective(Speed.From(3f), Preset.Custom, TwiceAsLong).Value);
    }

    [Fact]
    public void Longer_days_speed_up_a_preset_too()
    {
        Assert.Equal(10f, SpeedPlan.Effective(Speed.From(3f), Preset.Fast, TwiceAsLong).Value);
        Assert.Equal(2f, SpeedPlan.Effective(Speed.From(3f), Preset.Vanilla, TwiceAsLong).Value);
    }

    [Fact]
    public void A_machine_at_the_games_pace_follows_the_day_length()
    {
        Assert.Equal(2f, SpeedPlan.Effective(Speed.GamePace, Preset.Custom, TwiceAsLong).Value);
    }

    [Fact]
    public void Shorter_days_slow_a_machine_down()
    {
        Assert.Equal(2f, SpeedPlan.Effective(Speed.From(4f), Preset.Custom, DayFactor.From(120, 240)).Value);
    }

    [Fact]
    public void The_fastest_speed_stays_the_fastest_on_long_days()
    {
        var speed = SpeedPlan.Effective(Speed.From(3f), Preset.Instant, TwiceAsLong);

        Assert.Equal(Speed.Fastest, speed.Value);
    }

    [Fact]
    public void The_slowest_speed_stays_the_slowest_on_short_days()
    {
        var speed = SpeedPlan.Effective(Speed.From(Speed.Slowest), Preset.Custom, DayFactor.From(60, 240));

        Assert.Equal(Speed.Slowest, speed.Value);
    }
}
