using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Regular;

/// <summary>Shorthands that spell a written row the way a pattern prints it.</summary>
internal static class Rows {

    public static StitchRun K(int count) => new(StitchKind.Knit, count);

    public static StitchRun P(int count) => new(StitchKind.Purl, count);

    public static StitchRun Of(StitchKind kind, int count = 1) => new(kind, count);

    public static StitchRepeat Rep(int times, params StitchRun[] runs) => new(runs, times);

    public static RowInstruction Row(int number, FabricSide side, int? printedCount, params IRowElement[] elements) {
        return new RowInstruction(number, side, elements, printedCount);
    }

    public static RowInstruction Rs(int number, int? printedCount, params IRowElement[] elements) {
        return Row(number, FabricSide.RightSide, printedCount, elements);
    }

    public static RowInstruction Ws(int number, int? printedCount, params IRowElement[] elements) {
        return Row(number, FabricSide.WrongSide, printedCount, elements);
    }

    public static PatternSection Section(string name, params RowInstruction[] rows) => new(name, rows);

}
