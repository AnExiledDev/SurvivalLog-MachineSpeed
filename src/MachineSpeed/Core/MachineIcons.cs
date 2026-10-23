using System;

namespace MachineSpeed.Core;

/// <summary>
/// The picture beside a row: the game's own web icon for the furniture, as a path from the WebUI folder (the folder
/// Root.html is in). The furniture table writes it for a page two folders down ("../../Res/Furniture/x.png"); a piece
/// with no icon of its own gets its kind's usual one.
/// </summary>
public static class MachineIcons
{
    private const string Folder = "Res/Furniture/";

    public static string Of(MachineKind kind, string? tableIcon) => FromTable(tableIcon) ?? KindIcon(kind);

    /// <summary>The usual icon of a kind, for plants (one row for every pot) and for furniture with none of its own.</summary>
    public static string KindIcon(MachineKind kind) => Folder + kind switch
    {
        MachineKind.Shredder => "UI_Item_Icon_fensuiji.png",
        MachineKind.RationPress => "UI_Item_Icon_yazhiji.png",
        MachineKind.DyeVat => "UI_Item_Icon_ranliaotong.png",
        MachineKind.Brewing => "UI_Item_Icon_niangjiutong.png",
        MachineKind.Cooking => "UI_Item_Icon_laoshiranqizao.png",
        MachineKind.Drone => "UI_Item_Icon_wurenji.png",
        MachineKind.Generator => "UI_Item_Icon_Furnit_M_900200101.png",
        MachineKind.Compost => "UI_Item_Icon_duifei.png",
        MachineKind.Plants => "UI_Item_Icon_plantcommon_middle.png",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    // Only a png under Res/ is taken, so a table value can never point the page anywhere else.
    private static string? FromTable(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon)) return null;

        var path = icon.Trim().Replace('\\', '/');

        while (path.StartsWith("../", StringComparison.Ordinal) || path.StartsWith("./", StringComparison.Ordinal)) path = path.Substring(path.IndexOf('/') + 1);

        var isSafe = path.StartsWith("Res/", StringComparison.Ordinal) && !path.Contains("..", StringComparison.Ordinal) && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

        return isSafe ? path : null;
    }
}
