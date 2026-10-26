using System;
using GameCore.HotUpdate.Battle.Logic;
using MachineSpeed.Core;
using UnityEngine;

namespace MachineSpeed.Adapters;

/// <summary>
/// The Machines window's game side, run once a frame: its open state, the view it sends to overview.js, the HUD
/// button's badge, the Rush presses and the done toasts. Page messages arrive on the web view's thread, so they are
/// queued and handled here, in this frame.
/// </summary>
internal sealed class OverviewSession
{
    private const float OpenRefreshSeconds = 1f;
    private const float ClosedRefreshSeconds = 5f;

    private readonly MachineReader _reader;
    private readonly Rusher _rusher;
    private readonly OverviewPage _page;
    private readonly DoneToasts _toasts;
    private readonly DoneWatch _watch = new();
    private readonly Action<string> _log;

    private bool _isToasting = true;
    private double _pointsPerHour = RushCost.DefaultPointsPerHour;
    private bool _isFinishClock;

    private bool _isOpen;
    private bool _isOpenAsked;
    private string _shownView = "";
    private (int Working, int Needs)? _sentBadge;
    private (bool IsLive, bool IsChinese)? _sentLive;
    private float _nextRefreshAt;
    private bool _isScanning;
    private IntPtr _world = IntPtr.Zero;

    public OverviewSession(MachineReader reader, OverviewPage page, Action<string> log)
    {
        _reader = reader;
        _rusher = new Rusher(reader, log);
        _page = page;
        _toasts = new DoneToasts(AskToOpen, log);
        _log = log;
    }

    private static bool IsChinese => GameLanguage.IsChinese();

    /// <summary>Called on the main thread at start and after each settings change; an open window is redrawn with them at once.</summary>
    public void UseSettings(bool isToasting, double pointsPerHour, bool isFinishClock)
    {
        _isToasting = isToasting;
        _pointsPerHour = RushCost.Rate(pointsPerHour);
        _isFinishClock = isFinishClock;
        _shownView = "";
        RefreshNow();
    }

    public void Update()
    {
        var now = Time.unscaledTime;

        _page.KeepInstalled(now);

        while (OverviewMessages.TryTake(out var command)) HandleSafely(command);

        if (_isOpenAsked)
        {
            _isOpenAsked = false;

            if (!_isOpen) Open();
        }

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();

            return;
        }

        if (_isScanning) ContinueRefresh();
        else if (now >= _nextRefreshAt) StartRefresh(now);
    }

    /// <summary>The next frame starts a fresh read, and any read under way is dropped for it.</summary>
    private void RefreshNow()
    {
        _nextRefreshAt = 0f;
        _isScanning = false;
    }

    /// <summary>A click on a done toast. The window opens in this mod's own frame, like a click on its HUD button.</summary>
    private void AskToOpen() => _isOpenAsked = true;

    /// <summary>One command that throws must not cost the rest of the frame its commands or the key check.</summary>
    private void HandleSafely(OverviewCommand command)
    {
        try
        {
            Handle(command);
        }
        catch (Exception ex)
        {
            _log($"Machines window command {command} failed: {ex}");

            if (command is OverviewCommand.Rush) _page.Status(OverviewTexts.RushFailed.Pick(IsChinese));
        }
    }

    private void Handle(OverviewCommand command)
    {
        switch (command)
        {
            case OverviewCommand.Toggle:
                Toggle();
                break;
            case OverviewCommand.Close:
                Close();
                break;
            case OverviewCommand.Ready:
                Reloaded();
                break;
            case OverviewCommand.NoScript:
                _page.Install();
                break;
            case OverviewCommand.TimerNoScript:
                TimerPage.AskInstall();
                break;
            case OverviewCommand.Note note:
                _log("Machines window page: " + note.Text);
                break;
            case OverviewCommand.Rush rush:
                Rush(rush);
                break;
            default:
                throw new InvalidOperationException("unhandled Machines window command " + command);
        }
    }

    /// <summary>
    /// A fresh page has no window, button or badge, so everything is sent again. A window that was open went with the
    /// old page, so it counts as closed and stops holding the game's input.
    /// </summary>
    private void Reloaded()
    {
        _log("Machines window script ready");

        if (_isOpen)
        {
            _isOpen = false;
            OverviewInputGate.WindowClosed();
        }

        _sentLive = null;
        _sentBadge = null;
        _shownView = "";
        RefreshNow();
    }

    private void Rush(OverviewCommand.Rush rush)
    {
        var isChinese = IsChinese;
        var outcome = _rusher.Rush(rush.Kind, rush.Id, _pointsPerHour, isChinese);

        _page.Status(OverviewTexts.Describe(outcome, isChinese));
        RefreshNow();
    }

    /// <summary>
    /// Starts reading the home. The read is spread over the next few frames; with no game or away from home the answer
    /// is known at once.
    /// </summary>
    private void StartRefresh(float now)
    {
        _nextRefreshAt = now + (_isOpen ? OpenRefreshSeconds : ClosedRefreshSeconds);

        if (_reader.BeginScan(out var early))
        {
            _isScanning = true;

            return;
        }

        Finish(early!, 0);
    }

    /// <summary>A read dropped on the way (another game loaded, home left) is started again on the next frame.</summary>
    private void ContinueRefresh()
    {
        if (!_reader.ContinueScan(out var read, out var nowSeconds)) return;

        _isScanning = false;

        if (read == null)
        {
            _nextRefreshAt = 0f;

            return;
        }

        Finish(read, nowSeconds);
    }

    /// <summary>
    /// One read of the home is in: the HUD button shows while a game is loaded, its badges count machines working and
    /// machines that need the player, machines that just finished get a toast, and an open window is redrawn when its
    /// view changed.
    /// </summary>
    private void Finish(MachineRead read, long nowSeconds)
    {
        var isChinese = IsChinese;

        WatchForDone(read, isChinese);
        SendLive(read is not MachineRead.NoGame, isChinese);
        SendBadge(read is MachineRead.Home home ? (MachineList.Count(home.Rows, MachineGroup.Working), MachineList.Count(home.Rows, MachineGroup.Needs)) : (0, 0));

        if (_isOpen) Show(read, nowSeconds, isOpening: false, isChinese);
    }

    /// <summary>
    /// Every read at home goes past the watch, toasts on or off, so turning toasts on later does not announce old news.
    /// Away from home, or in another game, the watch starts afresh.
    /// </summary>
    private void WatchForDone(MachineRead read, bool isChinese)
    {
        var world = GameSingleton.Existing<BattleLogicWorld>()?.Pointer ?? IntPtr.Zero;

        if (world != _world || read is not MachineRead.Home)
        {
            _world = world;
            _watch.Forget();
        }

        if (read is not MachineRead.Home home) return;

        var notes = _watch.See(home.Rows);

        if (!_isToasting) return;

        for (var i = 0; i < notes.Count; i++) _toasts.Show(notes[i], isChinese);
    }

    private void SendLive(bool isLive, bool isChinese)
    {
        if (_sentLive == (isLive, isChinese)) return;
        if (_page.Live(isLive, OverviewTexts.Title.Pick(isChinese), OverviewTexts.ButtonTip.Pick(isChinese))) _sentLive = (isLive, isChinese);
    }

    private void SendBadge((int Working, int Needs) counts)
    {
        if (counts == _sentBadge) return;
        if (_page.Badge(counts.Working, counts.Needs)) _sentBadge = counts;
    }

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>With no game loaded there is nothing to show and the window stays shut; anything else opens it.</summary>
    private void Open()
    {
        // Opening reads the home whole, at once, and takes the place of any read spread over frames.
        _isScanning = false;

        var (read, nowSeconds) = _reader.Read();
        var isChinese = IsChinese;

        if (read is MachineRead.NoGame)
        {
            _page.Status(OverviewTexts.NoGame.Pick(isChinese));
            _nextRefreshAt = 0f;

            return;
        }

        _isOpen = true;
        OverviewInputGate.WindowOpened();
        Show(read, nowSeconds, isOpening: true, isChinese);
        _nextRefreshAt = Time.unscaledTime + OpenRefreshSeconds;
        _log("Machines window opened");
    }

    private void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        OverviewInputGate.WindowClosed();
        _page.Close();
    }

    private void Show(MachineRead read, long nowSeconds, bool isOpening, bool isChinese)
    {
        if (read is MachineRead.NoGame)
        {
            Close();

            return;
        }

        var view = OverviewView.Write(read, new ViewSettings(isChinese, _pointsPerHour, _isFinishClock, nowSeconds));

        if (isOpening)
        {
            _shownView = view;
            _page.Open(view);

            return;
        }

        if (view == _shownView) return;

        _shownView = view;
        _page.Show(view);
    }
}
