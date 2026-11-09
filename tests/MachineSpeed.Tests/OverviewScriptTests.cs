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
        var page = new WebScript();
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
        var page = new WebScript();
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
        var page = new WebScript();
        page.Run(FakeUi);
        page.Open();

        page.Run("document.getElementById('mso-root').remove()");
        page.Open();

        Assert.Equal("2", page.Text("handed.length"));
        Assert.True(page.Is("handed[1].el === document.getElementById('mso-root')"));
        Assert.True(page.Is("handed[1].options.fit === document.querySelector('.mso-plate')"));
    }

    [Fact]
    public void Before_the_toolkits_ui_size_has_loaded_the_window_waits_in_the_list_it_reads()
    {
        var page = new WebScript();

        page.Open();

        Assert.Equal("1", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("mso-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.Equal("MachineSpeed", page.Text("window.__exiledUiWait[0][1].mod"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.mso-plate')"));
    }
}

// ===== HUD button =====

public class OverviewHudTests
{
    private const string Button = "hudButton()";
    private const string Needs = "hudButton().querySelector('.mso-hud-needs')";
    private const string Working = "hudButton().querySelector('.mso-hud-working')";

    private static WebScript HudShown()
    {
        var page = new WebScript();
        page.Run("openHud()");
        page.Live(true);

        return page;
    }

    [Fact]
    public void With_a_game_loaded_the_button_is_first_in_the_tool_column_with_its_title_and_tip()
    {
        var page = HudShown();

        Assert.True(page.Is("hudButton().parentNode.firstChild === hudButton()"));
        Assert.Equal("Machines", page.Text("hudButton().querySelector('.entry-tip-title').textContent"));
        Assert.Equal("Every machine at home", page.Text("hudButton().querySelector('.entry-tip-body').textContent"));
        Assert.True(page.Is("hudPage().getElementById('mso-hud-style')"));
        Assert.True(page.HasPosted("diag|HUD button placed in the tool column"));
    }

    [Fact]
    public void Without_a_game_there_is_no_button()
    {
        var page = HudShown();

        page.Live(false);

        Assert.False(page.Is(Button));
    }

    [Fact]
    public void The_button_shows_how_many_machines_need_you_and_how_many_are_working()
    {
        var page = HudShown();

        page.Badge(working: 3, needs: 2);

        Assert.Equal("2", page.Text(Needs + ".textContent"));
        Assert.Equal("3", page.Text(Working + ".textContent"));
        // Both counts sit on the button itself, before its tip, as the game's bag button has its badge.
        Assert.True(page.Is(Working + ".nextElementSibling === hudButton().querySelector('.entry-tip')"));
    }

    [Fact]
    public void A_count_of_zero_has_no_badge_and_a_big_count_is_99_plus()
    {
        var page = HudShown();
        page.Badge(working: 3, needs: 2);

        page.Badge(working: 0, needs: 0);

        Assert.False(page.Is(Needs));
        Assert.False(page.Is(Working));

        page.Badge(working: 150, needs: 120);

        Assert.Equal("99+", page.Text(Needs + ".textContent"));
        Assert.Equal("99+", page.Text(Working + ".textContent"));
    }

    [Fact]
    public void A_hud_the_game_built_again_gets_the_button_back_with_both_counts_at_the_next_check()
    {
        var page = HudShown();
        page.Badge(working: 4, needs: 1);

        page.Run("document.querySelector('iframe').remove(); openHud()");

        Assert.False(page.Is(Button));

        page.Poll();

        Assert.Equal("1", page.Text(Needs + ".textContent"));
        Assert.Equal("4", page.Text(Working + ".textContent"));
    }

    [Fact]
    public void Clicking_the_button_asks_the_plugin_to_open_or_shut_the_window()
    {
        var page = HudShown();

        page.Run("click(hudButton())");

        Assert.True(page.HasPosted("toggle"));
    }
}

// ===== Load order (Exiled's Toolkit's page API) =====

public class OverviewLoadOrderTests
{
    /// <summary>The plugin sends everything again when the page says it is ready, so the page must say it once it can work.</summary>
    [Fact]
    public void With_the_toolkit_loaded_first_the_page_says_ready_at_once()
    {
        var page = new WebScript(isToolkitFirst: true);

        Assert.True(page.HasPosted("ready"));
    }

    /// <summary>
    /// The game can run Machine Speed's script before the Toolkit's. Until the Toolkit's arrives the page says nothing and
    /// has no button; then it says it is ready and puts up the button with what the plugin already sent.
    /// </summary>
    [Fact]
    public void With_the_page_loaded_first_it_waits_for_the_toolkit_then_says_ready_and_shows_the_button()
    {
        var page = new WebScript(isToolkitFirst: false);
        page.Run("openHud()");
        page.Live(true);
        page.Badge(working: 2, needs: 1);

        Assert.Equal("0", page.Text("posted.length"));
        Assert.False(page.Is("hudButton()"));

        page.LoadToolkit();

        Assert.True(page.HasPosted("ready"));
        Assert.True(page.Is("hudButton().parentNode.firstChild === hudButton()"));
        Assert.Equal("1", page.Text("hudButton().querySelector('.mso-hud-needs').textContent"));
        Assert.Equal("2", page.Text("hudButton().querySelector('.mso-hud-working').textContent"));
    }

    /// <summary>A window opened before the Toolkit's script ran is drawn at once and gets its style and UI size when the Toolkit arrives.</summary>
    [Fact]
    public void A_window_opened_before_the_toolkit_loaded_gets_its_style_and_size_when_it_arrives()
    {
        var page = new WebScript(isToolkitFirst: false);
        page.Run("var handed = []; window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } }");
        page.Open();

        Assert.True(page.Is("document.getElementById('mso-root')"));
        Assert.False(page.Is("document.getElementById('mso-style')"));
        Assert.Equal("0", page.Text("handed.length"));

        page.LoadToolkit();

        Assert.True(page.Is("document.getElementById('mso-style').textContent.indexOf('#mso-root') >= 0"));
        Assert.Equal("1", page.Text("handed.length"));
        Assert.Equal("mso-root", page.Text("handed[0].el.id"));
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
        var page = new WebScript();
        page.Run($"window.__slmachineoverview.open({Done})");

        Assert.Equal("Collect", page.Text("panelButton('collect', 'cook|5').textContent"));

        page.Run("click(panelButton('collect', 'cook|5'))");

        Assert.Equal("slmod|machinespeed|collect|cook|5", page.Text("posted[posted.length - 1]"));
    }

    [Fact]
    public void A_row_with_nothing_to_collect_has_no_collect_button()
    {
        var page = new WebScript();
        page.Run($"window.__slmachineoverview.open({Collected})");

        Assert.False(page.Is("panelButton('collect')"));
    }

    [Fact]
    public void A_collected_row_loses_its_button_when_the_next_view_comes_in()
    {
        var page = new WebScript();
        page.Run($"window.__slmachineoverview.open({Done})");

        page.Run($"window.__slmachineoverview.setData({Collected})");

        Assert.False(page.Is("panelButton('collect')"));
    }
}

// ===== Machine panels' times (page.js) =====

public class PanelTimesTests
{
    // 5400 game seconds left on the brew: 1 hour 30 minutes, where the game's panel reads whole hours.
    private const string Brewing = "{\"shredder\":null,\"brew\":{\"preview\":null,\"remain\":5400},\"dye\":null}";
    private const string GamePace = "{\"shredder\":null,\"brew\":null,\"dye\":null}";
    private const string BrewTime = "brew.document.getElementById('timeNum').textContent";

    [Fact]
    public void An_open_brew_panel_shows_the_exact_time_left_and_keeps_it_when_the_game_draws_again()
    {
        var page = new WebScript(WebScript.Panels);
        page.Run("var brew = openBrewPanel()");

        page.SetTimes(Brewing);

        Assert.Equal("Left 1:30", page.Text(BrewTime));

        page.Run("brew.renderFerment()");

        Assert.Equal("Left 1:30", page.Text(BrewTime));
    }

    [Fact]
    public void With_every_machine_at_the_games_pace_a_panel_is_left_as_the_game_draws_it()
    {
        var page = new WebScript(WebScript.Panels);
        page.Run("var brew = openBrewPanel(); var gameDraw = brew.renderFerment");

        page.SetTimes(GamePace);
        page.Poll();

        Assert.True(page.Is("brew.renderFerment === gameDraw"));
        Assert.Equal("Left 3h", page.Text(BrewTime));
    }

    [Fact]
    public void A_panel_opened_later_gets_the_exact_time_at_the_next_check()
    {
        var page = new WebScript(WebScript.Panels);
        page.SetTimes(Brewing);

        page.Run("var brew = openBrewPanel()");
        page.Poll();

        Assert.Equal("Left 1:30", page.Text(BrewTime));
    }

    /// <summary>The game opens a panel's page before its own script has set up the draw function; it is asked again until it has.</summary>
    [Fact]
    public void A_panel_whose_draw_function_is_not_there_yet_is_patched_once_it_is()
    {
        var page = new WebScript(WebScript.Panels);
        page.SetTimes(Brewing);
        page.Run("var brew = openPanel('BrewPanel')");
        page.Poll();

        Assert.False(page.Is("brew.__slmachinespeedPatched"));

        // The game's script for the panel has run: its time line, its state and its draw function are there.
        page.Run("var time = brew.document.body.appendChild(brew.document.createElement('span')); time.id = 'timeNum';"
            + " brew.State = { brewState: 1 }; brew.i18nText = { remainTemplate: 'Left {0}' };"
            + " brew.renderFerment = function () { time.textContent = 'Left 3h'; }; brew.renderFerment()");
        page.Poll();

        Assert.True(page.Is("brew.__slmachinespeedPatched"));
        Assert.Equal("Left 1:30", page.Text(BrewTime));
    }

    [Fact]
    public void With_the_panel_script_loaded_before_the_toolkit_a_panel_gets_its_time_when_the_toolkit_arrives()
    {
        var page = new WebScript(WebScript.Panels, isToolkitFirst: false);
        page.Run("var brew = openBrewPanel()");
        page.SetTimes(Brewing);

        Assert.Equal("Left 3h", page.Text(BrewTime));

        page.LoadToolkit();

        Assert.Equal("Left 1:30", page.Text(BrewTime));
    }
}
