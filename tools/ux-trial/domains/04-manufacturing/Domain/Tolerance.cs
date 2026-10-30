namespace Manufacturing.Domain;

/// <summary>A dimensional tolerance in millimetres: nominal -minus/+plus, at most three decimal places.</summary>
public sealed class Tolerance {

    public Tolerance(decimal nominalMm, decimal minusMm, decimal plusMm) {
        Validate(nominalMm, minusMm, plusMm);
        NominalMm = nominalMm;
        MinusMm   = minusMm;
        PlusMm    = plusMm;
    }

    public decimal NominalMm { get; }
    public decimal MinusMm   { get; }
    public decimal PlusMm    { get; }

    public decimal LowerLimitMm => NominalMm - MinusMm;
    public decimal UpperLimitMm => NominalMm + PlusMm;

    public bool Accepts(decimal measuredMm) => measuredMm >= LowerLimitMm && measuredMm <= UpperLimitMm;

    private static void Validate(decimal nominalMm, decimal minusMm, decimal plusMm) {
        if (nominalMm <= 0m || nominalMm > 5_000m) {
            throw new ArgumentOutOfRangeException(nameof(nominalMm), nominalMm, "A nominal dimension lies in (0, 5000] mm.");
        }
        if (minusMm < 0m) {
            throw new ArgumentOutOfRangeException(nameof(minusMm), minusMm, "A deviation is never negative.");
        }
        if (plusMm < 0m) {
            throw new ArgumentOutOfRangeException(nameof(plusMm), plusMm, "A deviation is never negative.");
        }
        if (minusMm + plusMm == 0m) {
            throw new ArgumentException("A tolerance band cannot be empty.");
        }
        if (minusMm >= nominalMm) {
            throw new ArgumentException("The lower limit must stay above zero.");
        }
        if (nominalMm.Scale > 3 || minusMm.Scale > 3 || plusMm.Scale > 3) {
            throw new ArgumentException("Dimensions are recorded to the micrometre (three decimal places).");
        }
    }

}
