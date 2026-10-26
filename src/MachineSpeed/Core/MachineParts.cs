using System;

namespace MachineSpeed.Core;

/// <summary>
/// The machine parts a piece of furniture was built with. The game gives furniture its parts once, from its table row,
/// so every piece of one kind of furniture has the same parts, and they are looked up once for that kind instead of
/// asked of every piece on every read.
/// </summary>
[Flags]
public enum MachineParts
{
    None = 0,
    Drone = 1,
    CompostBin = 2,
    Shredder = 4,
    RationPress = 8,
    DyeVat = 16,
    Brewing = 32,
    Cooking = 64,

    /// <summary>Any fuel burner, heaters included; a heater is told apart only when its row is read.</summary>
    Generator = 128,
    Plant = 256,
}

public static class MachinePartsOrder
{
    /// <summary>
    /// The window's kind for furniture with these parts, null when it lists none. The first part wins, in the order the
    /// window has always checked them, so a piece with two parts is listed as the same kind as before.
    /// </summary>
    public static MachineKind? KindOf(MachineParts parts)
    {
        if ((parts & MachineParts.Drone) != 0) return MachineKind.Drone;
        if ((parts & MachineParts.CompostBin) != 0) return MachineKind.Compost;
        if ((parts & MachineParts.Shredder) != 0) return MachineKind.Shredder;
        if ((parts & MachineParts.RationPress) != 0) return MachineKind.RationPress;
        if ((parts & MachineParts.DyeVat) != 0) return MachineKind.DyeVat;
        if ((parts & MachineParts.Brewing) != 0) return MachineKind.Brewing;
        if ((parts & MachineParts.Cooking) != 0) return MachineKind.Cooking;
        if ((parts & MachineParts.Generator) != 0) return MachineKind.Generator;
        if ((parts & MachineParts.Plant) != 0) return MachineKind.Plants;

        return null;
    }

    /// <summary>A fuel heater is not a generator the window lists, so it is read as whatever else it is.</summary>
    public static MachineKind? KindOfHeater(MachineParts parts) => KindOf(parts & ~MachineParts.Generator);
}

/// <summary>
/// The game asks every piece of furniture at its hourly check whether a brew or a dye job is done. Only a piece with
/// that part, whose job is not already being moved along, is worth a closer look.
/// </summary>
public static class HourJobLook
{
    public static bool ShouldLook(MachineParts parts, MachineParts needed, bool isTracked) => (parts & needed) != 0 && !isTracked;
}
