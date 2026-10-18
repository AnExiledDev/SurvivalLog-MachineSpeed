using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class SpeedTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void A_speed_that_is_not_above_zero_keeps_the_games_pace(float requested)
    {
        var speed = Speed.From(requested);

        Assert.True(speed.IsGamePace);
        Assert.Equal(SpeedVerdict.Rejected, speed.Verdict);
    }

    [Theory]
    [InlineData(0.1f, 0.25f)]
    [InlineData(50f, 20f)]
    public void A_speed_out_of_range_is_pulled_to_the_nearest_end(float requested, float expected)
    {
        var speed = Speed.From(requested);

        Assert.Equal(expected, speed.Value);
        Assert.Equal(SpeedVerdict.Clamped, speed.Verdict);
    }

    [Fact]
    public void Only_a_setting_not_taken_as_written_is_complained_about()
    {
        Assert.Null(Speed.From(2f).Complaint("Machines/Shredder"));
        Assert.Contains("Machines/Shredder = 50", Speed.From(50f).Complaint("Machines/Shredder"));
        Assert.Contains("game's pace", Speed.From(0f).Complaint("Machines/Shredder"));
    }
}

public class ScalingTests
{
    private static readonly Speed Double = Speed.From(2f);
    private static readonly Speed Half = Speed.From(0.5f);

    [Fact]
    public void A_faster_machine_takes_less_time()
    {
        Assert.Equal(30f, Scaling.Duration(60f, Double));
        Assert.Equal(120f, Scaling.Duration(60f, Half));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-5f)]
    public void A_duration_the_game_does_not_time_is_left_alone(float gameValue)
    {
        Assert.Equal(gameValue, Scaling.Duration(gameValue, Double));
    }

    [Fact]
    public void Whole_minutes_round_to_the_nearest_and_never_reach_zero()
    {
        Assert.Equal(3, Scaling.WholeUnits(5, Double));
        Assert.Equal(1, Scaling.WholeUnits(1, Speed.From(20f)));
        Assert.Equal(10, Scaling.WholeUnits(5, Half));
    }

    [Fact]
    public void At_the_games_pace_whole_units_are_unchanged()
    {
        Assert.Equal(7, Scaling.WholeUnits(7, Speed.GamePace));
    }

    [Fact]
    public void A_faster_burn_takes_more_fuel_and_a_slower_one_less()
    {
        Assert.Equal(4f, Scaling.Burn(2f, Double));
        Assert.Equal(1f, Scaling.Burn(2f, Half));
        Assert.Equal(0f, Scaling.Burn(0f, Double));
    }

    [Fact]
    public void A_faster_drone_returns_sooner_from_the_same_departure()
    {
        Assert.Equal(1000 + 300, Scaling.ReturnTime(1000, 1600, Double));
        Assert.Equal(1000 + 1200, Scaling.ReturnTime(1000, 1600, Half));
    }

    [Fact]
    public void A_drone_trip_is_never_shorter_than_one_second()
    {
        Assert.Equal(1001, Scaling.ReturnTime(1000, 1002, Speed.From(20f)));
    }

    [Fact]
    public void A_drone_return_before_its_departure_is_left_alone()
    {
        Assert.Equal(900, Scaling.ReturnTime(1000, 900, Double));
    }
}

public class DyePanelTests
{
    private static readonly Speed Double = Speed.From(2f);

    [Fact]
    public void An_idle_vat_shows_the_hours_the_job_will_take()
    {
        var panel = Scaling.Dye(new DyePreview(State: 0, BucketCount: 3, HoursPerBucket: 4, PreviewTotalHours: 12), Double);

        Assert.Equal(2, panel.HoursPerBucket);
        Assert.Equal(6, panel.PreviewTotalHours);
    }

    [Fact]
    public void A_running_vat_already_shows_its_sped_up_job()
    {
        var running = new DyePreview(State: 1, BucketCount: 3, HoursPerBucket: 2, PreviewTotalHours: 6);

        Assert.Equal(running, Scaling.Dye(running, Double));
    }

    [Fact]
    public void An_idle_vat_with_nothing_to_dye_is_left_alone()
    {
        var empty = new DyePreview(State: 0, BucketCount: 0, HoursPerBucket: 4, PreviewTotalHours: 0);

        Assert.Equal(empty, Scaling.Dye(empty, Double));
    }
}

public class ConfigTextTests
{
    private const string OldActionSpeed = "[Actions]\n\nFood = 2\n\n[Speed]\n\n## Speed of brewing alcohol.\n# Setting type: Single\nBrewing = 3.5\r\nCooking = 2\nCrafting = 4\n";

    [Fact]
    public void Brewing_and_cooking_are_read_from_action_speeds_speed_section()
    {
        var old = ConfigText.ReadOldActionSpeed(OldActionSpeed);

        Assert.Equal(3.5f, old.Brewing);
        Assert.Equal(2f, old.Cooking);
    }

    [Fact]
    public void A_value_with_the_same_key_in_another_section_is_not_taken()
    {
        var old = ConfigText.ReadOldActionSpeed("[Other]\nBrewing = 9\n[Speed]\nCooking = 2\n");

        Assert.Null(old.Brewing);
        Assert.Equal(2f, old.Cooking);
    }

    [Fact]
    public void An_unreadable_value_is_skipped()
    {
        Assert.Null(ConfigText.ReadOldActionSpeed("[Speed]\nBrewing = fast\n").Brewing);
        Assert.Null(ConfigText.ReadOldActionSpeed("").Cooking);
    }

    [Fact]
    public void A_config_that_already_has_the_machines_section_is_not_a_first_start()
    {
        Assert.True(ConfigText.HasSection("[Machines]\r\nShredder = 2\n", "Machines"));
        Assert.False(ConfigText.HasSection("## [Machines] is new\n[Other]\n", "Machines"));
    }
}
