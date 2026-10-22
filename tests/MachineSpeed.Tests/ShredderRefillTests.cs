using MachineSpeed.Core;
using Xunit;

namespace MachineSpeed.Tests;

public class ShredderRefillTests
{
    [Fact]
    public void Putting_items_into_an_idle_shredder_starts_it()
    {
        Assert.True(ShredderRefill.ShouldStart(isOn: true, isIdle: true, before: 0, now: 1));
        Assert.True(ShredderRefill.ShouldStart(isOn: true, isIdle: true, before: 3, now: 5));
    }

    [Fact]
    public void With_the_setting_off_a_refilled_shredder_waits_for_its_start_button()
    {
        Assert.False(ShredderRefill.ShouldStart(isOn: false, isIdle: true, before: 0, now: 4));
    }

    [Fact]
    public void Taking_items_out_of_a_stopped_shredder_does_not_restart_it()
    {
        Assert.False(ShredderRefill.ShouldStart(isOn: true, isIdle: true, before: 5, now: 4));
    }

    [Fact]
    public void Sorting_the_hopper_does_not_start_the_shredder()
    {
        Assert.False(ShredderRefill.ShouldStart(isOn: true, isIdle: true, before: 5, now: 5));
    }

    [Fact]
    public void A_busy_shredder_is_left_alone_when_more_items_go_in()
    {
        Assert.False(ShredderRefill.ShouldStart(isOn: true, isIdle: false, before: 2, now: 3));
    }

    [Fact]
    public void A_hopper_the_mod_has_not_seen_yet_never_starts_the_shredder()
    {
        Assert.False(ShredderRefill.ShouldStart(isOn: true, isIdle: true, before: null, now: 3));
    }
}
