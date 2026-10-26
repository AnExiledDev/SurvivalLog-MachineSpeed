using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class StampedCacheTests
{
    private static readonly IntPtr GameA = new(1);
    private static readonly IntPtr GameB = new(2);

    [Fact]
    public void Names_kept_for_one_game_are_used_again_while_the_same_game_is_loaded()
    {
        var cache = new StampedCache<(IntPtr, string, bool), int, string?>();

        Assert.True(cache.Use((GameA, "en", false)));
        cache.Set(7, "Barrel");

        Assert.False(cache.Use((GameA, "en", false)));
        Assert.True(cache.TryGet(7, out var name));
        Assert.Equal("Barrel", name);
    }

    [Fact]
    public void Loading_another_game_drops_every_name_kept_for_the_last_one()
    {
        var cache = new StampedCache<(IntPtr, string, bool), int, string?>();

        cache.Use((GameA, "en", false));
        cache.Set(7, "Barrel");

        Assert.True(cache.Use((GameB, "en", false)));
        Assert.False(cache.TryGet(7, out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Changing_the_language_drops_every_name_kept_in_the_old_one()
    {
        var cache = new StampedCache<(IntPtr, string, bool), int, string?>();

        cache.Use((GameA, "en", false));
        cache.Set(7, "Barrel");

        Assert.True(cache.Use((GameA, "zh", true)));
        Assert.False(cache.TryGet(7, out _));
    }

    [Fact]
    public void A_lookup_that_found_nothing_is_kept_too_so_it_is_not_asked_again()
    {
        var cache = new StampedCache<(IntPtr, string, bool), int, string?>();

        cache.Use((GameA, "en", false));
        cache.Set(99, null);

        Assert.True(cache.TryGet(99, out var name));
        Assert.Null(name);
    }

    [Fact]
    public void A_new_frame_drops_what_was_kept_for_the_last_frame()
    {
        var cache = new StampedCache<int, long, bool>();

        cache.Use(100);
        cache.Set(5, true);
        cache.Use(100);

        Assert.True(cache.TryGet(5, out _));

        cache.Use(101);

        Assert.False(cache.TryGet(5, out _));
    }

    [Fact]
    public void Forgetting_starts_afresh_even_under_the_same_stamp()
    {
        var cache = new StampedCache<(IntPtr, string, bool), int, string?>();

        cache.Use((GameA, "en", false));
        cache.Set(7, "Barrel");
        cache.Forget();

        Assert.True(cache.Use((GameA, "en", false)));
        Assert.False(cache.TryGet(7, out _));
    }
}

public class MachinePartsTests
{
    [Fact]
    public void A_piece_with_one_part_is_listed_as_that_machine()
    {
        Assert.Equal(MachineKind.Shredder, MachinePartsOrder.KindOf(MachineParts.Shredder));
        Assert.Equal(MachineKind.Plants, MachinePartsOrder.KindOf(MachineParts.Plant));
        Assert.Equal(MachineKind.Generator, MachinePartsOrder.KindOf(MachineParts.Generator));
    }

    [Fact]
    public void A_piece_with_two_parts_is_listed_by_the_first_in_the_windows_order()
    {
        Assert.Equal(MachineKind.Drone, MachinePartsOrder.KindOf(MachineParts.Plant | MachineParts.Drone));
        Assert.Equal(MachineKind.Compost, MachinePartsOrder.KindOf(MachineParts.Shredder | MachineParts.CompostBin));
        Assert.Equal(MachineKind.Brewing, MachinePartsOrder.KindOf(MachineParts.Cooking | MachineParts.Brewing));
        Assert.Equal(MachineKind.Generator, MachinePartsOrder.KindOf(MachineParts.Plant | MachineParts.Generator));
    }

    [Fact]
    public void Furniture_with_no_machine_part_is_not_listed()
    {
        Assert.Null(MachinePartsOrder.KindOf(MachineParts.None));
    }

    [Fact]
    public void A_heater_is_listed_as_whatever_else_it_is_and_not_as_a_generator()
    {
        Assert.Equal(MachineKind.Plants, MachinePartsOrder.KindOfHeater(MachineParts.Generator | MachineParts.Plant));
        Assert.Equal(MachineKind.Cooking, MachinePartsOrder.KindOfHeater(MachineParts.Generator | MachineParts.Cooking));
        Assert.Null(MachinePartsOrder.KindOfHeater(MachineParts.Generator));
    }
}

public class HourJobLookTests
{
    [Fact]
    public void A_barrel_whose_brew_is_not_moving_yet_is_looked_at_so_it_gets_sped_up()
    {
        Assert.True(HourJobLook.ShouldLook(MachineParts.Brewing, MachineParts.Brewing, isTracked: false));
        Assert.True(HourJobLook.ShouldLook(MachineParts.Brewing | MachineParts.Generator, MachineParts.Brewing, isTracked: false));
        Assert.True(HourJobLook.ShouldLook(MachineParts.DyeVat, MachineParts.DyeVat, isTracked: false));
    }

    [Fact]
    public void A_barrel_whose_brew_is_already_moving_is_passed_over()
    {
        Assert.False(HourJobLook.ShouldLook(MachineParts.Brewing, MachineParts.Brewing, isTracked: true));
        Assert.False(HourJobLook.ShouldLook(MachineParts.DyeVat, MachineParts.DyeVat, isTracked: true));
    }

    [Fact]
    public void Furniture_without_the_part_is_passed_over()
    {
        Assert.False(HourJobLook.ShouldLook(MachineParts.DyeVat, MachineParts.Brewing, isTracked: false));
        Assert.False(HourJobLook.ShouldLook(MachineParts.Brewing, MachineParts.DyeVat, isTracked: false));
        Assert.False(HourJobLook.ShouldLook(MachineParts.None, MachineParts.Brewing, isTracked: false));
    }
}

public class HomeScanTests
{
    private const int Pieces = 10;

    private static readonly Func<bool> NeverSpent = () => false;

    [Fact]
    public void A_read_one_piece_at_a_time_gives_the_same_rows_as_one_whole_read()
    {
        var whole = new HomeScan();

        whole.Start(Pieces);
        Assert.True(whole.Advance(int.MaxValue, i => ReadPiece(whole, i), NeverSpent));

        var split = new HomeScan();
        var calls = 0;

        split.Start(Pieces);

        while (!split.Advance(1, i => ReadPiece(split, i), NeverSpent)) calls++;

        Assert.Equal(Pieces - 1, calls);
        Assert.Equal(whole.Rows(), split.Rows());

        // Pieces 1, 5 and 9 are growing pots; 9 is the soonest ready.
        var pots = whole.Rows()[^1];
        Assert.Equal(MachineKind.Plants, pots.Kind);
        Assert.Equal(new Contents.Plants(3, 0), pots.Contents);
        Assert.Equal(910d, pots.RemainSeconds);
    }

    [Fact]
    public void A_frame_whose_time_is_used_up_stops_after_the_piece_it_is_on()
    {
        var scan = new HomeScan();
        var visited = new List<int>();

        scan.Start(3);

        Assert.False(scan.Advance(64, visited.Add, () => true));
        Assert.Equal(new[] { 0 }, visited);
        Assert.False(scan.Advance(64, visited.Add, () => true));
        Assert.True(scan.Advance(64, visited.Add, () => true));
        Assert.Equal(new[] { 0, 1, 2 }, visited);
    }

    [Fact]
    public void A_piece_that_fails_is_not_read_again_and_the_read_goes_on_past_it()
    {
        var scan = new HomeScan();
        var visited = new List<int>();

        scan.Start(4);

        Assert.Throws<InvalidOperationException>(() => scan.Advance(64, i =>
        {
            visited.Add(i);

            if (i == 1) throw new InvalidOperationException("broken piece");
        }, NeverSpent));

        Assert.True(scan.Advance(64, visited.Add, NeverSpent));
        Assert.Equal(new[] { 0, 1, 2, 3 }, visited);
    }

    [Fact]
    public void Any_ready_pot_makes_the_pots_row_ready_with_no_time()
    {
        var scan = new HomeScan();

        scan.Start(0);
        scan.AddGrowing(600);
        scan.AddReady();

        var pots = Assert.Single(scan.Rows());
        Assert.Equal(MachineState.Finished, pots.State);
        Assert.Null(pots.RemainSeconds);
        Assert.Equal(new Contents.Plants(1, 1), pots.Contents);
    }

    [Fact]
    public void A_pot_past_its_time_counts_as_ready_now_rather_than_in_the_past()
    {
        var scan = new HomeScan();

        scan.Start(0);
        scan.AddGrowing(600);
        scan.AddGrowing(-30);

        Assert.Equal(0d, Assert.Single(scan.Rows()).RemainSeconds);
    }

    [Fact]
    public void Starting_again_drops_the_last_reads_rows_but_not_the_list_already_handed_out()
    {
        var scan = new HomeScan();

        scan.Start(Pieces);
        scan.Advance(int.MaxValue, i => ReadPiece(scan, i), NeverSpent);

        var handedOut = scan.Rows();
        var count = handedOut.Count;

        scan.Start(0);

        Assert.Empty(scan.Rows());
        Assert.Equal(count, handedOut.Count);
    }

    // Some pieces are machines, some are growing pots, some are neither.
    private static void ReadPiece(HomeScan scan, int i)
    {
        switch (i % 4)
        {
            case 0:
            case 3:
                scan.Add(new MachineRow(MachineKind.Shredder, i, "Shredder " + i, null, MachineState.Running, 100 + i, CanRush: true));
                break;
            case 1:
                scan.AddGrowing(1000 - i * 10);
                break;
        }
    }
}

public class PagePrintTests
{
    private static readonly Speed Fast = Speed.From(2.5f);

    [Fact]
    public void Times_in_the_same_whole_second_print_the_same_so_nothing_is_sent()
    {
        var before = PageState.Print(Fast, new BrewTimes(17280.2, 100.1), new DyeTimes(2160, 6480, 0.4, 4000.1));
        var after = PageState.Print(Fast, new BrewTimes(17280.9, 100.9), new DyeTimes(2160, 6480, 0.9, 4000.5));

        Assert.Equal(before, after);
    }

    [Fact]
    public void A_countdown_one_second_on_prints_differently_so_it_is_sent()
    {
        var before = PageState.Print(Fast, new BrewTimes(17280, 101), null);
        var after = PageState.Print(Fast, new BrewTimes(17280, 100), null);

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void A_panel_with_no_times_is_not_the_same_as_no_panel()
    {
        Assert.NotEqual(PageState.Print(Fast, new BrewTimes(null, null), null), PageState.Print(Fast, null, null));
        Assert.NotEqual(PageState.Print(Fast, null, new DyeTimes(null, null, null, null)), PageState.Print(Fast, null, null));
    }

    [Fact]
    public void A_change_of_shredder_speed_prints_differently()
    {
        Assert.NotEqual(PageState.Print(Fast, null, null), PageState.Print(Speed.From(3f), null, null));
        Assert.NotEqual(PageState.Print(Fast, null, null), PageState.Print(Speed.GamePace, null, null));
    }

    [Fact]
    public void The_text_sent_from_a_print_is_the_text_the_page_has_always_been_sent()
    {
        var print = PageState.Print(Fast, new BrewTimes(17280.2, null), new DyeTimes(2160, 6480, 0.4, 4000.9));

        Assert.Equal("{\"shredder\":2.5,\"brew\":{\"preview\":17281,\"remain\":null},\"dye\":{\"perBucket\":2160,\"preview\":6480,\"bucketRemain\":1,\"totalRemain\":4001}}", PageState.Json(print));
    }
}

public class TimerScriptMessageTests
{
    [Fact]
    public void The_panels_asking_for_their_script_again_is_read()
    {
        Assert.IsType<OverviewCommand.TimerNoScript>(OverviewCommand.Parse("slmod|machinespeed|timernoscript"));
        Assert.Null(OverviewCommand.Parse("slmod|machinespeed|timernoscript|extra"));
    }
}

public class DoneWatchRepeatTests
{
    [Fact]
    public void Read_after_read_each_finish_is_noted_once_in_the_read_it_happens()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(1, MachineState.Running), Row(2, MachineState.Running) });
        Assert.Empty(watch.See(new[] { Row(1, MachineState.Running), Row(2, MachineState.Running) }));

        var first = watch.See(new[] { Row(1, MachineState.Finished), Row(2, MachineState.Running) });
        var second = watch.See(new[] { Row(1, MachineState.Finished), Row(2, MachineState.Finished) });

        Assert.Equal(1, Assert.Single(first).Row.Id);
        Assert.Equal(2, Assert.Single(second).Row.Id);
        Assert.Empty(watch.See(new[] { Row(1, MachineState.Finished), Row(2, MachineState.Finished) }));
    }

    [Fact]
    public void A_machine_gone_from_one_read_and_back_later_is_new_again_while_the_rest_are_still_watched()
    {
        var watch = new DoneWatch();

        watch.See(new[] { Row(1, MachineState.Running), Row(2, MachineState.Running) });
        watch.See(new[] { Row(2, MachineState.Running) });

        var notes = watch.See(new[] { Row(1, MachineState.Finished), Row(2, MachineState.Finished) });

        Assert.Equal(2, Assert.Single(notes).Row.Id);
    }

    private static MachineRow Row(long id, MachineState state) => new(MachineKind.Brewing, id, "", null, state, null, CanRush: false);
}
