namespace Clinical.Domain;

/// <summary>A treatment length in whole days, from one day to one year.</summary>
public sealed class TreatmentDuration {

    public const int MaxDays = 365;

    public TreatmentDuration(int days) {
        Validate(days);
        Days = days;
    }

    public int Days { get; }

    public DateOnly LastDayFrom(DateOnly start) {
        return start.AddDays(Days - 1);
    }

    private static void Validate(int days) {
        if (days < 1 || days > MaxDays) {
            throw new ArgumentOutOfRangeException(nameof(days), $"A treatment lasts between 1 and {MaxDays} days.");
        }
    }

}
