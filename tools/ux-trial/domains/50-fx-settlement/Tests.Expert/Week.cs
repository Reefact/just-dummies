namespace FxSettlement.Domain.Tests.Expert;

/// <summary>Two real weeks of the calendar, so a test says which weekday it is about. 2025-03-03 is a Monday.</summary>
public static class Week {

    public static readonly DateOnly Monday    = new DateOnly(2025, 3, 3);
    public static readonly DateOnly Tuesday   = new DateOnly(2025, 3, 4);
    public static readonly DateOnly Wednesday = new DateOnly(2025, 3, 5);
    public static readonly DateOnly Thursday  = new DateOnly(2025, 3, 6);
    public static readonly DateOnly Friday    = new DateOnly(2025, 3, 7);
    public static readonly DateOnly Saturday  = new DateOnly(2025, 3, 8);
    public static readonly DateOnly Sunday    = new DateOnly(2025, 3, 9);

    public static readonly DateOnly NextMonday  = new DateOnly(2025, 3, 10);
    public static readonly DateOnly NextTuesday = new DateOnly(2025, 3, 11);

}
