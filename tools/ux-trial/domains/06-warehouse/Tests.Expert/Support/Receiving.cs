using JustDummies;

using Warehouse.Domain.Tests.Generators;

namespace Warehouse.Domain.Tests.Expert.Support;

/// <summary>
///     Arrangement helpers for the goods-in tests. Each one builds goods that raise no inspection reason, so a
///     test that wants exactly one reason spells out that one thing and leaves everything else to these.
/// </summary>
internal static class Receiving {

    /// <summary>A unit weight light enough that a few lines of at most ten thousand units stay far below the gross weight limit.</summary>
    public const decimal LightUnitKg = 0.01m;

    /// <summary>A reception instant and a best-before date at least the minimum shelf life after it.</summary>
    public sealed record ShelfLife(DateTime ReceivedAt, DateOnly BestBefore);

    /// <summary>The date arithmetic of the tests never reaches the end of the calendar: the reception is bounded.</summary>
    public static IAny<ShelfLife> AmpleShelfLife() {
        return Any.Combine(Any.DateTime().Between(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2040, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
                           Any.Int32().Between(PalletReceipt.MinimumShelfLifeDays, 1_000),
                           (receivedAt, shelfLifeDays) => new ShelfLife(receivedAt, DateOnly.FromDateTime(receivedAt).AddDays(shelfLifeDays)));
    }

    /// <summary>A line that arrived exactly as announced, light, and with a comfortable shelf life.</summary>
    public static ReceiptLine ConformingLine(ShelfLife shelfLife) {
        Quantity anyCount = Any.Quantity().Generate();

        return Any.ReceiptLine()
                  .WithExpected(anyCount)
                  .WithReceived(anyCount)
                  .WithUnitWeightKg(LightUnitKg)
                  .WithBestBefore(shelfLife.BestBefore)
                  .Generate();
    }

    public static ReceiptLine Line(int expected, int received, decimal unitWeightKg, DateOnly bestBefore) {
        return Any.ReceiptLine()
                  .WithExpected(new Quantity(expected))
                  .WithReceived(new Quantity(received))
                  .WithUnitWeightKg(unitWeightKg)
                  .WithBestBefore(bestBefore)
                  .Generate();
    }

    public static PalletReceipt Receipt(DateTime receivedAt, params ReceiptLine[] lines) {
        return Any.PalletReceipt().WithReceivedAt(receivedAt).WithLines(lines).Generate();
    }

}
