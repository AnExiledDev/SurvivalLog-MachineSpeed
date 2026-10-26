using System;
using System.IO;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using MachineSpeed.Core;

namespace MachineSpeed.Adapters;

/// <summary>
/// The brewing barrel, dye vat and shredder panels draw their times in whole game hours or minutes, so a sped-up job
/// reads too long and a countdown jumps. page.js, run in the web UI's root document, redraws those times from the
/// exact sped-up seconds this sends. The web view can reload and lose the script, so every few seconds the page is
/// asked whether it still has it, and the script is sent only when the page answers that it does not. Without the
/// message hook to hear that answer, the whole script is sent every few seconds as before.
/// </summary>
internal static class TimerPage
{
    private const float InstallSeconds = 5f;
    private const float PushSeconds = 1f;
    private const string ScriptResource = "MachineSpeed.page.js";
    private const string Handle = "window.__slmachinespeed";
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + OverviewCommand.Prefix + "timernoscript')";

    private static Action<string> _log = _ => { };
    private static string? _script;
    private static bool _hasReportedRunFailure;
    private static float _nextInstall;
    private static float _nextPush;
    private static bool _isChanged;

    // Set from the web view's message hook, read on the next tick.
    private static volatile bool _isInstallAsked;
    private static PagePrint? _lastPrint;
    private static Furniture? _brewBarrel;
    private static int _brewHours;
    private static Furniture? _dyeVat;
    private static int _dyeHoursPerBucket;
    private static int _dyePreviewHours;

    public static void UseLog(SpeedLog log) => _log = log.Write;

    /// <summary>The barrel whose panel was just built, with the game's hours for the brew picked.</summary>
    public static void ShowingBrew(Furniture? barrel, int gameHours)
    {
        (_brewBarrel, _brewHours) = (barrel, gameHours);
        Changed();
    }

    /// <summary>The vat whose panel was just built, with the game's hours a bucket and for the batch.</summary>
    public static void ShowingDye(Furniture? vat, int gameHoursPerBucket, int gamePreviewHours)
    {
        (_dyeVat, _dyeHoursPerBucket, _dyePreviewHours) = (vat, gameHoursPerBucket, gamePreviewHours);
        Changed();
    }

    /// <summary>A panel was just drawn, so the page is sent the times on the next tick rather than within a second.</summary>
    public static void Changed() => _isChanged = true;

    /// <summary>The world went away; its furniture must not be read again.</summary>
    public static void Forget()
    {
        (_brewBarrel, _dyeVat) = (null, null);
        Changed();
    }

    /// <summary>The page answered that it has no script. Called on the web view's thread; the script goes on the next tick.</summary>
    public static void AskInstall() => _isInstallAsked = true;

    public static void Tick(float realSeconds, int nowSeconds)
    {
        if (_isInstallAsked)
        {
            _isInstallAsked = false;

            if (IsAnySpedUp()) Install();
        }

        if (IsAnySpedUp() && realSeconds >= _nextInstall)
        {
            _nextInstall = realSeconds + InstallSeconds;

            if (OverviewMessages.IsListening) Run(Handle + "||" + NoScriptReply);
            else Install();
        }

        if (!_isChanged && realSeconds < _nextPush) return;

        (_isChanged, _nextPush) = (false, realSeconds + PushSeconds);

        // Compared before any text is built, so a second in which nothing the page shows has changed sends nothing.
        var print = PageState.Print(ShredderPatch.Speed, BrewTimes(nowSeconds), DyeTimes(nowSeconds));

        if (print != _lastPrint && Run(Handle + "&&" + Handle + ".set(" + PageState.Json(print) + ")")) _lastPrint = print;
    }

    // A reloaded page has the script again but none of the times, so they are sent again as well.
    private static void Install()
    {
        if (Run(Script())) _lastPrint = null;
    }

    private static bool IsAnySpedUp() => !ShredderPatch.Speed.IsGamePace || !BrewingPatch.Speed.IsGamePace || !DyeVatPatch.Speed.IsGamePace;

    private static BrewTimes? BrewTimes(int nowSeconds)
    {
        var speed = BrewingPatch.Speed;

        if (speed.IsGamePace || _brewBarrel == null || _brewBarrel._IsDead_k__BackingField) return null;

        var brew = AgentTools.GetAgentComponent<BrewComponent>(_brewBarrel);
        double? remain = null;

        if (brew != null && brew.IsBrewing())
        {
            var nowHour = Furniture.GetBrewNowHour();
            var done = Countdown.Done(nowHour - brew.StartHour, nowSeconds, nowHour, HourJobs.Carry("brew", _brewBarrel._InstanceId_k__BackingField));

            remain = Countdown.Remaining(brew.TotalHours, done, speed);
        }

        return new BrewTimes(Countdown.Estimate(_brewHours, speed), remain);
    }

    private static DyeTimes? DyeTimes(int nowSeconds)
    {
        var speed = DyeVatPatch.Speed;

        if (speed.IsGamePace || _dyeVat == null || _dyeVat._IsDead_k__BackingField) return null;

        var dye = AgentTools.GetAgentComponent<DyeComponent>(_dyeVat);
        double? bucketRemain = null;
        double? totalRemain = null;

        if (dye != null && dye.IsRunning())
        {
            var nowHour = Furniture.GetDyeNowHour();
            var done = Countdown.Done(nowHour - dye.StartHour, nowSeconds, nowHour, HourJobs.Carry("dye", _dyeVat._InstanceId_k__BackingField));

            bucketRemain = Countdown.BucketRemaining(dye.HoursPerBucket, dye.ResultCount, done, speed);
            totalRemain = Countdown.Remaining(dye.ResultCount * dye.HoursPerBucket, done, speed);
        }

        return new DyeTimes(Countdown.Estimate(_dyeHoursPerBucket, speed), Countdown.Estimate(_dyePreviewHours, speed), bucketRemain, totalRemain);
    }

    // ReduxUISystem.GetWebUILayer and the layer's canvasWebViewPrefab are how the game's own pages are reached.
    private static bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not show sped-up times on the machine panels (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private static string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(TimerPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
