using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class RushCostTests
{
    [Fact]
    public void Rushing_costs_the_hours_left_at_the_points_per_hour_rounded_up()
    {
        // 10 hours at 0.25 a point is 2.5, and part of a point is a whole one.
        Assert.Equal(3, RushCost.Of(10 * 3600, 0.25));
        Assert.Equal(2, RushCost.Of(8 * 3600, 0.25));
    }

    [Fact]
    public void Rushing_always_costs_at_least_one_point()
    {
        Assert.Equal(1, RushCost.Of(60, 0.25));
        Assert.Equal(1, RushCost.Of(0, 0.25));
    }

    [Fact]
    public void A_job_a_hair_over_a_whole_number_of_points_does_not_cost_one_more()
    {
        Assert.Equal(2, RushCost.Of(8 * 3600 + 0.001, 0.25));
    }

    [Fact]
    public void A_broken_points_per_hour_setting_falls_back_to_the_default_and_a_huge_one_is_capped()
    {
        Assert.Equal(RushCost.DefaultPointsPerHour, RushCost.Rate(0));
        Assert.Equal(RushCost.DefaultPointsPerHour, RushCost.Rate(-3));
        Assert.Equal(RushCost.DefaultPointsPerHour, RushCost.Rate(double.NaN));
        Assert.Equal(RushCost.MostPointsPerHour, RushCost.Rate(1e9));
        Assert.Equal(1000, RushCost.Of(3600, 1e9));
    }

    [Fact]
    public void A_very_long_job_at_a_high_rate_costs_the_most_an_int_holds_rather_than_wrapping()
    {
        Assert.Equal(int.MaxValue, RushCost.Of(1e15, 1000));
    }

    [Fact]
    public void A_running_job_the_player_can_pay_for_is_affordable()
    {
        Assert.Equal(new RushPlan.Affordable(3), RushCost.Plan(Running(10 * 3600), 3, 0.25));
    }

    [Fact]
    public void A_running_job_with_too_few_points_shows_the_cost_and_the_points()
    {
        Assert.Equal(new RushPlan.TooFew(3, 2), RushCost.Plan(Running(10 * 3600), 2, 0.25));
    }

    [Fact]
    public void Only_a_running_job_with_time_left_on_a_machine_that_can_be_finished_is_rushable()
    {
        Assert.IsType<RushPlan.NotRushable>(RushCost.Plan(Running(3600) with { State = MachineState.Paused }, 99, 0.25));
        Assert.IsType<RushPlan.NotRushable>(RushCost.Plan(Running(3600) with { CanRush = false }, 99, 0.25));
        Assert.IsType<RushPlan.NotRushable>(RushCost.Plan(Running(0), 99, 0.25));
        Assert.IsType<RushPlan.NotRushable>(RushCost.Plan(Running(3600) with { RemainSeconds = null }, 99, 0.25));
    }

    private static MachineRow Running(double remain) => new(MachineKind.Brewing, 7, "Barrel", null, MachineState.Running, remain, CanRush: true);
}

public class GameClockTests
{
    [Fact]
    public void Time_left_counts_whole_minutes_like_the_timer_over_the_machine()
    {
        Assert.Equal("1:05", GameClock.Left(3959, isChinese: false));
        Assert.Equal("0:00", GameClock.Left(59, isChinese: false));
    }

    [Fact]
    public void Time_left_of_a_day_or_more_shows_the_days()
    {
        Assert.Equal("2d 3:00", GameClock.Left(2 * 86400 + 3 * 3600, isChinese: false));
        Assert.Equal("2天 3:00", GameClock.Left(2 * 86400 + 3 * 3600, isChinese: true));
    }

    [Fact]
    public void A_job_ending_today_shows_the_game_clock_time_it_ends()
    {
        // Day 5 at 08:00, 90 minutes left.
        Assert.Equal("09:30", GameClock.Finish(5 * 86400 + 8 * 3600, 5400, isChinese: false));
    }

    [Fact]
    public void A_job_ending_on_a_later_day_says_how_many_days_ahead()
    {
        Assert.Equal("+1d 02:00", GameClock.Finish(5 * 86400 + 22 * 3600, 4 * 3600, isChinese: false));
        Assert.Equal("+1天 02:00", GameClock.Finish(5 * 86400 + 22 * 3600, 4 * 3600, isChinese: true));
    }

    [Fact]
    public void Part_of_a_second_left_is_never_shown_as_already_done()
    {
        // 08:59:59.5 plus half a second ends at 09:00, not 08:59.
        Assert.Equal("09:00", GameClock.Finish(8 * 3600 + 3599, 0.5, isChinese: false));
    }

    [Fact]
    public void A_broken_time_left_reads_as_none()
    {
        Assert.Equal("0:00", GameClock.Left(double.NaN, isChinese: false));
        Assert.Equal("08:00", GameClock.Finish(8 * 3600, double.PositiveInfinity, isChinese: false));
    }
}

public class DoneWatchTests
{
    [Fact]
    public void A_machine_already_done_when_first_seen_is_not_news()
    {
        var watch = new DoneWatch();

        Assert.Empty(watch.See(new[] { Row(MachineKind.Brewing, MachineState.Finished) }));
    }

    [Fact]
    public void A_job_that_finishes_between_reads_is_noted_once()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(MachineKind.Brewing, MachineState.Running) });
        var notes = watch.See(new[] { Row(MachineKind.Brewing, MachineState.Finished) });

        Assert.Equal(DoneWhat.Finished, Assert.Single(notes).What);
        Assert.Empty(watch.See(new[] { Row(MachineKind.Brewing, MachineState.Finished) }));
    }

    [Fact]
    public void A_generator_that_runs_dry_is_noted_but_one_found_dry_is_not()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(MachineKind.Generator, MachineState.Running, 1), Row(MachineKind.Generator, MachineState.Idle, 2) });
        var notes = watch.See(new[] { Row(MachineKind.Generator, MachineState.OutOfFuel, 1), Row(MachineKind.Generator, MachineState.OutOfFuel, 2) });

        var note = Assert.Single(notes);
        Assert.Equal(DoneWhat.OutOfFuel, note.What);
        Assert.Equal(1, note.Row.Id);
    }

    [Fact]
    public void Compost_bins_and_plants_never_toast()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(MachineKind.Compost, MachineState.Running), Row(MachineKind.Plants, MachineState.Running) });

        Assert.Empty(watch.See(new[] { Row(MachineKind.Compost, MachineState.Finished), Row(MachineKind.Plants, MachineState.Finished) }));
    }

    [Fact]
    public void A_machine_missing_from_a_read_starts_afresh_when_seen_again()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(MachineKind.Shredder, MachineState.Running) });
        watch.See(System.Array.Empty<MachineRow>());

        Assert.Empty(watch.See(new[] { Row(MachineKind.Shredder, MachineState.Finished) }));
    }

    [Fact]
    public void A_new_world_forgets_every_machine_seen_before()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(MachineKind.Drone, MachineState.Running) });
        watch.Forget();

        Assert.Empty(watch.See(new[] { Row(MachineKind.Drone, MachineState.Finished) }));
    }

    private static MachineRow Row(MachineKind kind, MachineState state, long id = 1) => new(kind, id, "", null, state, null, CanRush: false);
}

public class OverviewCommandTests
{
    [Fact]
    public void The_windows_messages_are_read()
    {
        Assert.IsType<OverviewCommand.Toggle>(OverviewCommand.Parse("slmod|machinespeed|toggle"));
        Assert.IsType<OverviewCommand.NoScript>(OverviewCommand.Parse("slmod|machinespeed|noscript"));
        Assert.Equal(new OverviewCommand.Rush(MachineKind.RationPress, 42), OverviewCommand.Parse("slmod|machinespeed|rush|press|42"));
        Assert.Equal(new OverviewCommand.Note("a|b"), OverviewCommand.Parse("slmod|machinespeed|diag|a|b"));
    }

    [Fact]
    public void Other_mods_messages_are_not_ours_and_broken_ones_do_nothing()
    {
        Assert.False(OverviewCommand.IsOurs("slmod|farmhub|toggle"));
        Assert.Null(OverviewCommand.Parse("slmod|farmhub|toggle"));
        Assert.Null(OverviewCommand.Parse("slmod|machinespeed|rush|oven|42"));
        Assert.Null(OverviewCommand.Parse("slmod|machinespeed|rush|press|x"));
        Assert.Null(OverviewCommand.Parse("slmod|machinespeed|toggle|extra"));
    }
}

public class MachineListTests
{
    [Fact]
    public void The_badge_counts_machines_waiting_for_the_player()
    {
        var rows = new[]
        {
            Row(MachineState.Running, 1, 60),
            Row(MachineState.Idle, 2),
            Row(MachineState.Finished, 3),
            Row(MachineState.OutOfFuel, 4),
            Row(MachineState.Paused, 5),
        };

        Assert.Equal(3, MachineList.WaitingCount(rows));
    }

    [Fact]
    public void Waiting_machines_come_first_then_running_ones_soonest_done_first()
    {
        var rows = new[]
        {
            Row(MachineState.Paused, 1),
            Row(MachineState.Running, 2, 900),
            Row(MachineState.Running, 3, 60),
            Row(MachineState.Idle, 4),
            Row(MachineState.Finished, 5),
        };

        Assert.Equal(new long[] { 5, 4, 3, 2, 1 }, MachineList.Ordered(rows).Select(row => row.Id));
    }

    private static MachineRow Row(MachineState state, long id, double? remain = null) => new(MachineKind.Shredder, id, "", null, state, remain, CanRush: false);
}

public class OverviewViewTests
{
    private static readonly ViewSettings English = new(IsChinese: false, RushPointsPerHour: 0.25, IsFinishClock: false, NowSeconds: 8 * 3600);

    [Fact]
    public void A_row_with_no_name_of_its_own_is_called_by_its_kind()
    {
        Assert.Contains("\"name\":\"Brewing barrel\"", View(Brew(remain: 3600) with { Name = "" }, points: 5));
    }

    [Fact]
    public void Names_from_the_game_are_escaped_so_they_cannot_break_the_page()
    {
        var json = View(Brew(remain: 3600) with { Name = "a\"b</script>" }, points: 5);

        Assert.Contains("\"name\":\"a\\\"b\\u003c/script>\"", json);
    }

    [Fact]
    public void A_job_the_player_can_pay_for_has_a_pressable_rush_button_with_its_price()
    {
        var json = View(Brew(remain: 10 * 3600), points: 3);

        Assert.Contains("\"rush\":{\"label\":\"Rush (3 pts)\",\"ok\":true", json);
    }

    [Fact]
    public void A_job_the_player_cannot_pay_for_has_a_disabled_rush_button_that_says_why()
    {
        var json = View(Brew(remain: 10 * 3600), points: 2);

        Assert.Contains("\"ok\":false,\"tip\":\"Needs 3 survival points, you have 2\"", json);
    }

    [Fact]
    public void Unread_survival_points_hide_the_rush_button()
    {
        Assert.Contains("\"rush\":null", View(Brew(remain: 3600), points: null));
    }

    [Fact]
    public void Times_show_time_left_by_default_and_the_finish_time_with_the_finish_clock()
    {
        Assert.Contains("\"time\":\"1:30 left\"", View(Brew(remain: 5400), points: 0));
        Assert.Contains("\"time\":\"Done at 09:30\"", View(Brew(remain: 5400), points: 0, English with { IsFinishClock = true }));
    }

    [Fact]
    public void A_paused_job_shows_time_left_even_with_the_finish_clock()
    {
        var paused = Brew(remain: 5400) with { State = MachineState.Paused };

        Assert.Contains("\"time\":\"1:30 left\"", View(paused, points: 0, English with { IsFinishClock = true }));
    }

    [Fact]
    public void A_generators_time_is_its_fuel()
    {
        var generator = new MachineRow(MachineKind.Generator, 3, "", null, MachineState.Running, 5400, CanRush: false);

        Assert.Contains("\"time\":\"Fuel for 1:30\"", View(generator, points: 0));
        Assert.Contains("\"time\":\"Fuel until 09:30\"", View(generator, points: 0, English with { IsFinishClock = true }));
    }

    [Fact]
    public void Away_from_home_the_window_says_so_and_lists_nothing()
    {
        var json = OverviewView.Write(new MachineRead.Away(), English);

        Assert.Contains("\"note\":\"Machines are listed at home only.\"", json);
        Assert.Contains("\"rows\":[]", json);
    }

    [Fact]
    public void The_head_bar_finish_clock_reads_in_the_games_language()
    {
        Assert.Equal("at 09:30", OverviewTexts.HeadBarFinish(8 * 3600, 5400, isChinese: false));
        Assert.Equal("09:30完成", OverviewTexts.HeadBarFinish(8 * 3600, 5400, isChinese: true));
    }

    [Fact]
    public void A_done_toast_names_the_machine_and_what_it_made()
    {
        var note = new DoneNote(Brew(remain: 0) with { Name = "Barrel", Contents = new Contents.Named("Beer") }, DoneWhat.Finished);

        Assert.Equal("Brew ready", OverviewTexts.ToastTitle(note, isChinese: false));
        Assert.Equal("Barrel: Beer", OverviewTexts.ToastBody(note, isChinese: false));
    }

    private static MachineRow Brew(double remain) => new(MachineKind.Brewing, 7, "Barrel", null, MachineState.Running, remain, CanRush: true);

    private static string View(MachineRow row, int? points, ViewSettings? settings = null) => OverviewView.Write(new MachineRead.Home(new[] { row }, points), settings ?? English);
}
