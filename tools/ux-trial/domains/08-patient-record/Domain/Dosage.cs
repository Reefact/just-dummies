namespace Clinical.Domain;

/// <summary>How much of a molecule, in which unit, how many times a day.</summary>
public sealed class Dosage {

    public Dosage(decimal amount, DoseUnit unit, int timesPerDay) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, 10_000m);
        if (decimal.Round(amount, 3) != amount) {
            throw new ArgumentException("A dose is expressed with at most three decimals.", nameof(amount));
        }
        if (!Enum.IsDefined(unit)) {
            throw new ArgumentOutOfRangeException(nameof(unit));
        }
        if (timesPerDay is < 1 or > 6) {
            throw new ArgumentOutOfRangeException(nameof(timesPerDay), "Between one and six intakes a day.");
        }

        Amount      = amount;
        Unit        = unit;
        TimesPerDay = timesPerDay;
    }

    public decimal Amount { get; }

    public DoseUnit Unit { get; }

    public int TimesPerDay { get; }

    public decimal DailyAmount => Amount * TimesPerDay;

}
