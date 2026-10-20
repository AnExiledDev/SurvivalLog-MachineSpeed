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
    [InlineData(200000f, 100000f)]
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
        Assert.Contains("Machines/Shredder = 200000", Speed.From(200000f).Complaint("Machines/Shredder"));
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
    public void A_job_in_game_seconds_rounds_to_the_nearest_second()
    {
        Assert.Equal(150, Scaling.Seconds(300, Double));
        Assert.Equal(600, Scaling.Seconds(300, Half));
        Assert.Equal(2, Scaling.Seconds(3, Double));
    }

    [Fact]
    public void A_very_fast_job_still_takes_one_second()
    {
        Assert.Equal(1, Scaling.Seconds(3600, Speed.From(100000f)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void A_job_the_game_does_not_time_is_left_alone(int gameSeconds)
    {
        Assert.Equal(gameSeconds, Scaling.Seconds(gameSeconds, Double));
    }

    [Fact]
    public void A_panel_rounds_up_so_a_running_job_never_reads_zero()
    {
        Assert.Equal(3, Scaling.Shown(5, Double));
        Assert.Equal(1, Scaling.Shown(5, Speed.From(100000f)));
        Assert.Equal(0, Scaling.Shown(0, Double));
        Assert.Equal(7, Scaling.Shown(7, Speed.GamePace));
    }

    [Fact]
    public void Faster_cooking_keeps_the_games_fuel_per_dish()
    {
        // Fuel per dish is rate x cook length; the cook is half as long, so the rate must double.
        var rate = Scaling.CookingFuelRate(0.5f, Double, FuelUse.GameAmount);
        var cookLength = Scaling.Duration(60f, Double);

        Assert.Equal(0.5f * 60f, rate * cookLength);
    }

    [Fact]
    public void The_fuel_amount_sets_the_fuel_per_dish()
    {
        Assert.Equal(0.25f, Scaling.CookingFuelRate(0.5f, Speed.GamePace, FuelUse.From(0.5f)));
        Assert.Equal(0f, Scaling.CookingFuelRate(0.5f, Double, FuelUse.From(0f)));
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

public class FuelUseTests
{
    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void An_amount_below_zero_keeps_the_games_amount(float requested)
    {
        var fuel = FuelUse.From(requested);

        Assert.True(fuel.IsGameAmount);
        Assert.Equal(SpeedVerdict.Rejected, fuel.Verdict);
    }

    [Fact]
    public void No_fuel_at_all_is_allowed()
    {
        Assert.Equal(0f, FuelUse.From(0f).Value);
        Assert.Null(FuelUse.From(0f).Complaint("Machines/CookingFuel"));
    }

    [Fact]
    public void An_amount_over_the_most_is_pulled_down()
    {
        var fuel = FuelUse.From(50f);

        Assert.Equal(FuelUse.Most, fuel.Value);
        Assert.Contains("Machines/CookingFuel = 50", fuel.Complaint("Machines/CookingFuel"));
    }
}

public class SpedUpClockTests
{
    private const int Hour = 3600;

    [Fact]
    public void The_first_reading_only_starts_the_count()
    {
        Assert.Equal(0, new SpedUpClock().ExtraHours(10 * Hour, Speed.From(100f)));
    }

    [Fact]
    public void Time_under_an_hour_carries_over_until_it_makes_an_hour()
    {
        var clock = new SpedUpClock();
        var triple = Speed.From(3f);

        clock.ExtraHours(0, triple);

        // Each 900 game seconds at x3 adds 1800 extra seconds: half an hour, then a whole one.
        Assert.Equal(0, clock.ExtraHours(900, triple));
        Assert.Equal(1, clock.ExtraHours(1800, triple));
        Assert.Equal(0, clock.ExtraHours(2700, triple));
        Assert.Equal(1, clock.ExtraHours(3600, triple));
    }

    [Fact]
    public void A_slower_speed_moves_the_job_back()
    {
        var clock = new SpedUpClock();
        var half = Speed.From(0.5f);

        clock.ExtraHours(0, half);

        Assert.Equal(-1, clock.ExtraHours(2 * Hour, half));
    }

    [Fact]
    public void A_clock_that_went_back_starts_counting_again()
    {
        var clock = new SpedUpClock();
        var triple = Speed.From(3f);

        clock.ExtraHours(10 * Hour, triple);
        clock.ExtraHours(10 * Hour + 900, triple);

        Assert.Equal(0, clock.ExtraHours(Hour, triple));
        Assert.Equal(0, clock.ExtraHours(Hour + 900, triple));
    }

    [Fact]
    public void A_very_fast_job_finishes_on_the_next_reading()
    {
        var clock = new SpedUpClock();
        var fastest = Speed.From(100000f);

        clock.Elapsed(0, 0, 24, fastest);

        Assert.Equal(24, clock.Elapsed(60, 0, 24, fastest));
    }

    [Fact]
    public void Time_cut_off_at_the_end_is_not_kept_for_later()
    {
        var clock = new SpedUpClock();
        var fast = Speed.From(13f);

        clock.Elapsed(0, 1, 1, fast);

        // 599 s at x13 is 1 hour and 3588 s extra; the job is already done, so the hour and the 3588 s are dropped.
        Assert.Equal(1, clock.Elapsed(599, 1, 1, fast));

        // Kept, one more second (12 extra) would have made a whole hour.
        Assert.Equal(0, clock.Elapsed(600, 0, 2, fast));
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

public class CookSpeedsTests
{
    private const long Stove = 1;

    [Fact]
    public void A_speed_change_mid_cook_leaves_that_cooks_fuel_per_dish_alone()
    {
        // The game owes progress x length x rate in fuel; a cook started at x1 must keep the x1 rate when the speed goes
        // to x100, or the fuel already owed is charged a hundred times over in one tick.
        var cooks = new CookSpeeds(Speed.GamePace);
        var fast = Speed.From(100f);
        cooks.Started(Stove, fast);
        cooks.Started(2, Speed.GamePace);

        var rate = Scaling.CookingFuelRate(0.5f, cooks.For(Stove), FuelUse.GameAmount);

        Assert.Equal(Scaling.CookingFuelRate(0.5f, fast, FuelUse.GameAmount), rate);
        Assert.NotEqual(0.5f, rate);
    }

    [Fact]
    public void A_cook_in_another_loaded_world_does_not_take_a_speed_from_the_last_one()
    {
        // A stove in the next save can sit at the same address as one in the last save.
        var startSpeed = Speed.From(4f);
        var cooks = new CookSpeeds(startSpeed);
        cooks.Started(Stove, Speed.From(100f));

        cooks.Forget();

        Assert.Same(startSpeed, cooks.For(Stove));
    }

    [Fact]
    public void The_next_cook_on_the_same_stove_burns_at_its_own_speed()
    {
        var cooks = new CookSpeeds(Speed.GamePace);
        var fast = Speed.From(100f);
        cooks.Started(Stove, Speed.GamePace);

        cooks.Started(Stove, fast);

        Assert.Same(fast, cooks.For(Stove));
    }

    [Fact]
    public void A_cook_it_never_saw_start_burns_at_the_speed_the_game_started_with()
    {
        var startSpeed = Speed.From(4f);
        var cooks = new CookSpeeds(startSpeed);
        cooks.Started(Stove, Speed.From(100f));

        Assert.Same(startSpeed, cooks.For(2));
    }
}
