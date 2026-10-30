namespace Drafting.Domain;

/// <summary>A toleranced size on a drawing, e.g. Ø25 H7.</summary>
public sealed class Dimension {

    public const double MillimetresPerInch = 25.4;

    public Dimension(double nominal, LengthUnit unit, IsoTolerance? tolerance) {
        if (double.IsNaN(nominal)) {
            throw new ArgumentException("A nominal size is a number.", nameof(nominal));
        }
        if (nominal <= 0) {
            throw new ArgumentOutOfRangeException(nameof(nominal), nominal, "A nominal size is strictly positive.");
        }

        Nominal   = nominal;
        Unit      = unit;
        Tolerance = tolerance;
        Validate();
    }

    public double        Nominal   { get; }
    public LengthUnit    Unit      { get; }
    public IsoTolerance? Tolerance { get; }

    public double NominalInMillimetres => Unit == LengthUnit.Inch ? Nominal * MillimetresPerInch : Nominal;

    public double UpperLimitInMillimetres => NominalInMillimetres + UpperDeviationInMillimetres();
    public double LowerLimitInMillimetres => NominalInMillimetres + LowerDeviationInMillimetres();

    private double ToleranceInMillimetres() {
        return Tolerance is null ? 0 : StandardTolerance.InMicrometres(NominalInMillimetres, Tolerance.Grade) / 1000.0;
    }

    private double UpperDeviationInMillimetres() {
        return Tolerance?.Zone switch {
            null          => 0,
            "H"           => ToleranceInMillimetres(),
            "h"           => 0,
            "JS" or "js"  => ToleranceInMillimetres() / 2,
            _             => throw new InvalidOperationException()
        };
    }

    private double LowerDeviationInMillimetres() {
        return Tolerance?.Zone switch {
            null          => 0,
            "H"           => 0,
            "h"           => -ToleranceInMillimetres(),
            "JS" or "js"  => -ToleranceInMillimetres() / 2,
            _             => throw new InvalidOperationException()
        };
    }

    public bool Accepts(double measuredMillimetres) {
        return measuredMillimetres >= LowerLimitInMillimetres && measuredMillimetres <= UpperLimitInMillimetres;
    }

    private void Validate() {
        if (NominalInMillimetres > StandardTolerance.LargestNominalMillimetres) {
            throw new ArgumentOutOfRangeException(nameof(Nominal), Nominal, "Nominal sizes above 500 mm are outside the tables this office uses.");
        }
    }

    public override string ToString() {
        string unit = Unit == LengthUnit.Inch ? "in" : "mm";
        return Tolerance is null ? $"Ø{Nominal} {unit}" : $"Ø{Nominal} {Tolerance} {unit}";
    }

}
