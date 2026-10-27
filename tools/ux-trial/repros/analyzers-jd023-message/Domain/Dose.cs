namespace Jd.Domain;

public sealed class Dose {
    public Dose(int intakes) {
        if (intakes is < 1 or > 6) { throw new ArgumentOutOfRangeException(nameof(intakes)); }
        Intakes = intakes;
    }

    public int Intakes { get; }
}
