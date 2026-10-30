using Xunit;

namespace MachineSpeed.Tests;

// ===== UI size (Exiled's Toolkit) =====

public class OverviewUiSizeTests
{
    // Stands in for Exiled's Toolkit's window.ExiledUi and keeps what each call handed it.
    private const string FakeUi = "var handed = []; window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } }";

    [Fact]
    public void The_window_is_handed_to_the_toolkit_once_with_the_plate_to_fit()
    {
        var page = new OverviewScript();
        page.Run(FakeUi);

        page.Open();
        page.Close();
        page.Open();

        // Opening again reuses the window it built, so the Toolkit is not handed it twice.
        Assert.Equal("1", page.Text("handed.length"));
        Assert.Equal("MachineSpeed", page.Text("handed[0].options.mod"));
        Assert.Equal("mso-root", page.Text("handed[0].el.id"));
        // The mask always fills the screen; the window on it is what must fit.
        Assert.True(page.Is("handed[0].options.fit === document.querySelector('.mso-plate')"));
    }

    [Fact]
    public void A_press_result_while_the_window_is_shut_draws_nothing_of_its_own()
    {
        var page = new OverviewScript();
        page.Run(FakeUi);
        page.Open();
        page.Close();

        page.Run("window.__slmachineoverview.status('Zq9 no game')");

        Assert.False(page.Is("!!document.getElementById('mso-toast')"));
        Assert.Equal("1", page.Text("handed.length"));
        Assert.Equal("", page.Text("document.querySelector('.mso-status').textContent"));
    }

    [Fact]
    public void A_window_the_page_lost_is_handed_again_when_it_is_built_anew()
    {
        var page = new OverviewScript();
        page.Run(FakeUi);
        page.Open();

        page.Run("document.getElementById('mso-root').remove()");
        page.Open();

        Assert.Equal("2", page.Text("handed.length"));
        Assert.True(page.Is("handed[1].el === document.getElementById('mso-root')"));
        Assert.True(page.Is("handed[1].options.fit === document.querySelector('.mso-plate')"));
    }

    [Fact]
    public void Before_the_toolkit_has_loaded_the_window_waits_in_the_list_it_reads()
    {
        var page = new OverviewScript();

        page.Open();

        Assert.Equal("1", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("mso-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.mso-plate')"));
    }
}

// ===== Collect button =====

public class OverviewCollectTests
{
    private const string Done = "{ rows: [{ key: 'cook|5', kind: 'cook', id: '5', name: 'Stove', product: 'Stew', state: 'finished', stateText: 'Done', group: 'needs', icon: '', hint: 'Ready to take out', progress: null, time: null, rush: null, collect: 'Collect' }] }";

    private static readonly string Collected = Done.Replace("collect: 'Collect'", "collect: ''");

    [Fact]
    public void A_done_row_has_a_collect_button_that_asks_the_plugin_to_collect_from_that_machine()
    {
        var page = new OverviewScript();
        page.Run($"window.__slmachineoverview.open({Done})");

        Assert.Equal("Collect", page.Text("panelButton('collect', 'cook|5').textContent"));

        page.Run("click(panelButton('collect', 'cook|5'))");

        Assert.Equal("slmod|machinespeed|collect|cook|5", page.Text("posted[posted.length - 1]"));
    }

    [Fact]
    public void A_row_with_nothing_to_collect_has_no_collect_button()
    {
        var page = new OverviewScript();
        page.Run($"window.__slmachineoverview.open({Collected})");

        Assert.False(page.Is("panelButton('collect')"));
    }

    [Fact]
    public void A_collected_row_loses_its_button_when_the_next_view_comes_in()
    {
        var page = new OverviewScript();
        page.Run($"window.__slmachineoverview.open({Done})");

        page.Run($"window.__slmachineoverview.setData({Collected})");

        Assert.False(page.Is("panelButton('collect')"));
    }
}
