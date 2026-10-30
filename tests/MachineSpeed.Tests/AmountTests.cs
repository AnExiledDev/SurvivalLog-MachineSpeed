using System.Linq;
using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class MultiplierTests
{
    [Fact]
    public void A_value_inside_the_range_is_used_as_written()
    {
        var multiplier = Multiplier.From(2.5f, 1f, 100f);

        Assert.Equal(2.5f, multiplier.Value);
        Assert.Equal(SpeedVerdict.AsWritten, multiplier.Verdict);
        Assert.Null(multiplier.Complaint("Amounts/ShredderOutput"));
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(500f, 100f)]
    public void A_value_outside_the_range_is_pulled_to_its_edge_and_logged(float requested, float expected)
    {
        var multiplier = Multiplier.From(requested, 1f, 100f);

        Assert.Equal(expected, multiplier.Value);
        Assert.Equal(SpeedVerdict.Clamped, multiplier.Verdict);
        Assert.Contains("outside 1 to 100", multiplier.Complaint("Amounts/ShredderOutput"));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0f)]
    public void An_unusable_value_falls_back_to_the_game_amount(float requested)
    {
        var multiplier = Multiplier.From(requested, 0.1f, 100f);

        Assert.True(multiplier.IsGameAmount);
        Assert.Equal(SpeedVerdict.Rejected, multiplier.Verdict);
        Assert.Contains("the game's (1)", multiplier.Complaint("Amounts/RobotCharging"));
    }

    [Fact]
    public void Zero_is_kept_when_the_range_starts_at_zero_so_wear_can_be_turned_off()
    {
        var multiplier = Multiplier.From(0f, 0f, 100f);

        Assert.Equal(0f, multiplier.Value);
        Assert.Equal(SpeedVerdict.AsWritten, multiplier.Verdict);
    }

    [Fact]
    public void A_value_between_zero_and_the_least_is_raised_to_the_least()
    {
        Assert.Equal(0.1f, Multiplier.From(0.05f, 0.1f, 100f).Value);
    }
}

public class RemainderCarryTests
{
    private static Multiplier Times(float value) => Multiplier.From(value, 0f, 100f);

    [Fact]
    public void One_and_a_half_on_single_items_gives_one_and_two_by_turns()
    {
        var carry = new RemainderCarry<int>();

        var counts = Enumerable.Range(0, 4).Select(_ => carry.Take(7, 1, Times(1.5f))).ToArray();

        Assert.Equal(new[] { 1, 2, 1, 2 }, counts);
    }

    [Fact]
    public void Peeking_shows_the_next_count_without_using_up_the_fraction()
    {
        var carry = new RemainderCarry<int>();

        carry.Take(7, 1, Times(1.5f));

        Assert.Equal(2, carry.Peek(7, 1, Times(1.5f)));
        Assert.Equal(2, carry.Peek(7, 1, Times(1.5f)));
        Assert.Equal(2, carry.Take(7, 1, Times(1.5f)));
        Assert.Equal(1, carry.Peek(7, 1, Times(1.5f)));
    }

    [Fact]
    public void Each_item_keeps_its_own_fraction()
    {
        var carry = new RemainderCarry<int>();

        carry.Take(1, 1, Times(1.5f));

        Assert.Equal(1, carry.Take(2, 1, Times(1.5f)));
        Assert.Equal(2, carry.Take(1, 1, Times(1.5f)));
    }

    [Fact]
    public void The_total_never_drifts_from_the_exact_amount()
    {
        var carry = new RemainderCarry<int>();

        var total = Enumerable.Range(0, 10).Sum(_ => carry.Take(3, 3, Times(1.1f)));

        Assert.Equal(33, total);
    }

    [Fact]
    public void At_the_game_amount_counts_pass_through_and_nothing_is_carried()
    {
        var carry = new RemainderCarry<int>();

        Assert.Equal(5, carry.Take(1, 5, Multiplier.GameAmount));
        Assert.Equal(1, carry.Take(1, 1, Times(1.5f)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_count_of_zero_or_less_is_left_alone(int count)
    {
        Assert.Equal(count, new RemainderCarry<int>().Take(1, count, Times(2f)));
    }

    [Fact]
    public void A_multiplier_of_zero_takes_everything_away()
    {
        var carry = new RemainderCarry<long>();

        Assert.Equal(0, carry.Take(9L, 40, Times(0f)));
        Assert.Equal(0, carry.Take(9L, 40, Times(0f)));
    }

    [Fact]
    public void Half_wear_on_one_point_hits_loses_a_point_every_other_hit()
    {
        var carry = new RemainderCarry<long>();

        var losses = Enumerable.Range(0, 4).Select(_ => carry.Take(9L, 1, Times(0.5f))).ToArray();

        Assert.Equal(new[] { 0, 1, 0, 1 }, losses);
    }
}

public class SharesTests
{
    [Fact]
    public void The_parts_add_up_to_the_new_total()
    {
        var scaled = Shares.Rescale(new[] { 1, 1, 1 }, 3, 5);

        Assert.Equal(5, scaled.Sum());
        Assert.All(scaled, part => Assert.InRange(part, 1, 2));
    }

    [Fact]
    public void The_parts_keep_their_proportions()
    {
        Assert.Equal(new[] { 6, 2 }, Shares.Rescale(new[] { 3, 1 }, 4, 8));
    }

    [Fact]
    public void Parts_with_no_old_total_are_left_alone()
    {
        Assert.Equal(new[] { 2, 3 }, Shares.Rescale(new[] { 2, 3 }, 0, 10));
    }
}

public class AmountsTests
{
    private static Multiplier Times(float value) => Multiplier.From(value, 0f, 100f);

    [Fact]
    public void A_stronger_fridge_divides_the_spoil_rate()
    {
        Assert.Equal(0.1f, Amounts.ColdRate(0.2f, Times(2f)), 5);
        Assert.Equal(0.4f, Amounts.ColdRate(0.2f, Times(0.5f)), 5);
    }

    [Fact]
    public void A_fridge_never_spoils_slower_than_a_sealed_cold_store_or_faster_than_no_fridge()
    {
        Assert.Equal(Amounts.ColdestRate, Amounts.ColdRate(0.2f, Times(100f)));
        Assert.Equal(1f, Amounts.ColdRate(0.8f, Times(0.1f)));
    }

    [Fact]
    public void The_fridge_talent_takes_its_share_off_but_never_goes_under_a_tenth()
    {
        Assert.Equal(0.14f, Amounts.TalentColdRate(0.2f, 0.3f), 5);
        Assert.Equal(Amounts.TalentFloor, Amounts.TalentColdRate(0.2f, 0.8f));
        Assert.Equal(0.2f, Amounts.TalentColdRate(0.2f, 0f));
    }

    [Fact]
    public void Strength_still_counts_once_the_player_has_the_fridge_talent()
    {
        // Strength 10 with a talent of 0.3: the talent makes 0.2 into 0.14, then strength 10 makes it 0.014.
        Assert.Equal(0.014f, Amounts.FridgeRate(0.2f, 0.3f, Times(10f)), 5);
        // The game's talent floor of a tenth would leave strength 100 at 10x; it reaches the coldest rate instead.
        Assert.Equal(Amounts.ColdestRate, Amounts.FridgeRate(0.2f, 0.8f, Times(100f)));
    }

    [Fact]
    public void Without_the_talent_a_fridge_rate_is_the_strength_alone()
    {
        Assert.Equal(Amounts.ColdRate(0.2f, Times(4f)), Amounts.FridgeRate(0.2f, 0f, Times(4f)));
        Assert.Equal(Amounts.ColdRate(0.2f, Times(4f)), Amounts.FridgeRate(0.2f, float.NaN, Times(4f)));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(0f)]
    [InlineData(-0.2f)]
    public void A_rate_that_is_not_a_fridge_rate_is_left_alone(float rate)
    {
        Assert.Equal(rate, Amounts.ColdRate(rate, Times(2f)));
    }

    [Fact]
    public void A_capacity_is_rounded_to_whole_units_and_never_drops_below_one()
    {
        Assert.Equal(15f, Amounts.Capacity(10f, Times(1.45f)));
        Assert.Equal(5f, Amounts.Capacity(3f, Times(1.5f)));
        Assert.Equal(1f, Amounts.Capacity(1f, Times(0.1f)));
        Assert.Equal(0f, Amounts.Capacity(0f, Times(2f)));
    }

    [Fact]
    public void A_rate_is_multiplied_only_when_the_game_uses_it()
    {
        Assert.Equal(6f, Amounts.Rate(3f, Times(2f)));
        Assert.Equal(0f, Amounts.Rate(0f, Times(2f)));
        Assert.Equal(3f, Amounts.Rate(3f, Multiplier.GameAmount));
    }

    [Fact]
    public void A_rate_already_multiplied_moves_to_the_new_setting_without_stacking()
    {
        Assert.Equal(9f, Amounts.Reapply(6f, 2f, Times(3f)), 5);
        Assert.Equal(3f, Amounts.Reapply(6f, 2f, Multiplier.GameAmount), 5);
    }
}
