using Xunit;

namespace MachineSpeed.Tests;

// ===== UI size (Exiled's Toolkit) =====

public class OverviewUiSizeTests
{
    // Stands in for Exiled's Toolkit's window.ExiledUi and keeps what each call handed it.
    private const string FakeUi = "var handed = []; window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } }";

    [Fact]
    public void The_window_is_handed_to_the_toolkit_once_with_the_plate_to_fit_and_the_notice_never_shrunk()
    {
        var page = new OverviewScript();
        page.Run(FakeUi);

        page.Open();
        page.Close();
        page.Open();

        // Opening again reuses the window it built, so the Toolkit is not handed it twice.
        Assert.Equal("2", page.Text("handed.length"));
        Assert.Equal("2", page.Text("handed.filter(function (h) { return h.options.mod === 'MachineSpeed'; }).length"));
        Assert.Equal("mso-root", page.Text("handed[0].el.id"));
        // The mask always fills the screen; the window on it is what must fit.
        Assert.True(page.Is("handed[0].options.fit === document.querySelector('.mso-plate')"));
        Assert.Equal("mso-toast", page.Text("handed[1].el.id"));
        Assert.True(page.Is("handed[1].options.fit === false"));
    }

    [Fact]
    public void A_window_the_page_lost_is_handed_again_when_it_is_built_anew()
    {
        var page = new OverviewScript();
        page.Run(FakeUi);
        page.Open();

        page.Run("document.getElementById('mso-root').remove(); document.getElementById('mso-toast').remove()");
        page.Open();

        Assert.Equal("4", page.Text("handed.length"));
        Assert.True(page.Is("handed[2].el === document.getElementById('mso-root')"));
        Assert.True(page.Is("handed[2].options.fit === document.querySelector('.mso-plate')"));
        Assert.True(page.Is("handed[3].el === document.getElementById('mso-toast')"));
    }

    [Fact]
    public void Before_the_toolkit_has_loaded_the_window_waits_in_the_list_it_reads()
    {
        var page = new OverviewScript();

        page.Open();

        Assert.Equal("2", page.Text("window.__exiledUiWait.length"));
        Assert.Equal("mso-root", page.Text("window.__exiledUiWait[0][0].id"));
        Assert.True(page.Is("window.__exiledUiWait[0][1].fit === document.querySelector('.mso-plate')"));
        Assert.Equal("mso-toast", page.Text("window.__exiledUiWait[1][0].id"));
    }
}
