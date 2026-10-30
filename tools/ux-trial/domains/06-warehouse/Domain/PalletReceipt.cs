using System.Text.RegularExpressions;

namespace Warehouse.Domain;

/// <summary>A pallet received at the dock, from goods-in until it is put away in a bin.</summary>
public sealed class PalletReceipt {

    public const decimal MaximumGrossWeightKg = 1_200m;
    public const int     MinimumShelfLifeDays = 30;

    private static readonly Regex SupplierFormat = new(@"^SUP-\d{5}$");

    private readonly List<ReceiptLine> _lines;

    public PalletReceipt(Sscc sscc, string supplierCode, DateTime receivedAt, TemperatureRange? requiredTemperature, IReadOnlyList<ReceiptLine> lines) {
        ArgumentNullException.ThrowIfNull(sscc);
        if (!SupplierFormat.IsMatch(supplierCode)) {
            throw new ArgumentException($"'{supplierCode}' is not a supplier code like SUP-00042.", nameof(supplierCode));
        }
        if (receivedAt.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("Receipt times are recorded in UTC.", nameof(receivedAt));
        }
        if (lines.Count == 0) {
            throw new ArgumentException("A pallet carries at least one line.", nameof(lines));
        }

        Sscc                = sscc;
        SupplierCode        = supplierCode;
        ReceivedAt          = receivedAt;
        RequiredTemperature = requiredTemperature;
        _lines              = lines.ToList();
        Status              = ReceiptStatus.Received;
    }

    public Sscc                       Sscc                { get; }
    public string                     SupplierCode        { get; }
    public DateTime                   ReceivedAt          { get; }
    public TemperatureRange?          RequiredTemperature { get; }
    public IReadOnlyList<ReceiptLine> Lines               => _lines;
    public ReceiptStatus              Status              { get; private set; }
    public BinLocation?               Bin                 { get; private set; }
    public string?                    QuarantineReason    { get; private set; }

    public int TotalUnits => _lines.Sum(line => line.Received.Units);

    public decimal NetWeightKg => _lines.Sum(line => line.WeightKg);

    /// <summary>
    ///     Goods-in policy: a pallet is held back when a line is short, when its gross weight exceeds what a rack
    ///     level carries, or when a line has less than <see cref="MinimumShelfLifeDays" /> days of shelf life left.
    /// </summary>
    public IReadOnlyList<string> InspectionReasons(decimal palletTareKg) {
        List<string> reasons = [];
        if (_lines.Any(line => line.Discrepancy < 0)) {
            reasons.Add("short delivery");
        }
        if (NetWeightKg + palletTareKg > MaximumGrossWeightKg) {
            reasons.Add("overweight");
        }
        DateOnly receivedOn = DateOnly.FromDateTime(ReceivedAt);
        if (_lines.Any(line => line.BestBefore.DayNumber - receivedOn.DayNumber < MinimumShelfLifeDays)) {
            reasons.Add("short shelf life");
        }

        return reasons;
    }

    public void Quarantine(string reason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == ReceiptStatus.PutAway) {
            throw new InvalidOperationException("A pallet already put away is quarantined from its bin, not from the dock.");
        }

        Status           = ReceiptStatus.Quarantined;
        QuarantineReason = reason;
    }

    public void PutAway(BinLocation bin) {
        ArgumentNullException.ThrowIfNull(bin);
        Status = Status switch {
            ReceiptStatus.Received => ReceiptStatus.PutAway,
            ReceiptStatus.Quarantined => throw new InvalidOperationException($"Pallet {Sscc} is quarantined: {QuarantineReason}."),
            _ => throw new InvalidOperationException($"Pallet {Sscc} is already in {Bin}.")
        };
        Bin = bin;
    }

}
