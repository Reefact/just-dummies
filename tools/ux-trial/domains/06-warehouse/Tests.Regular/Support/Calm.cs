using JustDummies;

namespace Warehouse.Domain.Tests.Regular.Support;

/// <summary>Receipts and lines that raise no inspection reason, to start a test from and then break one rule.</summary>
internal static class Calm {

    public static DateTime AnyReceivedAt() {
        return Any.DateTime()
                  .Between(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2040, 12, 31, 23, 59, 59, DateTimeKind.Utc))
                  .Generate();
    }

    /// <summary>A line that is not short, light, and fresh for 30 days or more after <paramref name="receivedAt" />.</summary>
    public static AnyReceiptLine Line(DateTime receivedAt) {
        DateOnly receivedOn = DateOnly.FromDateTime(receivedAt);
        int      expected   = Any.Int32().Between(0, 500).Generate();
        int      received   = Any.Int32().Between(expected, expected + 500).Generate();
        int      shelfLife  = Any.Int32().Between(PalletReceipt.MinimumShelfLifeDays, 1_000).Generate();

        return new AnyReceiptLine().WithExpected(new Quantity(expected))
                                   .WithReceived(new Quantity(received))
                                   .WithBestBefore(receivedOn.AddDays(shelfLife))
                                   .WithUnitWeightKg(w => w.Between(0.01m, 0.1m));
    }

    public static PalletReceipt Receipt(DateTime receivedAt, params ReceiptLine[] lines) {
        return new AnyPalletReceipt().WithReceivedAt(receivedAt).WithLines(lines).Generate();
    }

}
