namespace RealEstate.Domain;

/// <summary>A surface in square metres, measured to the hundredth (Carrez law precision).</summary>
public sealed class Surface : IEquatable<Surface> {

    /// <summary>1 000 hectares: beyond that the land registry splits the parcel.</summary>
    public const double MaxSquareMeters = 10_000_000d;

    public Surface(double squareMeters) {
        if (double.IsNaN(squareMeters)) {
            throw new ArgumentException("A surface is a number.", nameof(squareMeters));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(squareMeters);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(squareMeters, MaxSquareMeters);
        if (Math.Round(squareMeters, 2) != squareMeters) {
            throw new ArgumentException("A surface is measured to the hundredth of a square metre.", nameof(squareMeters));
        }
        SquareMeters = squareMeters;
    }

    public double SquareMeters { get; }

    public double Hectares => SquareMeters / 10_000d;

    /// <summary>Under the Carrez law a co-owned lot under 8 m² is not counted.</summary>
    public bool CountsUnderCarrezLaw => SquareMeters >= 8d;

    public Surface Add(Surface other) => new Surface(Math.Round(SquareMeters + other.SquareMeters, 2));

    public bool Equals(Surface? other) => other is not null && SquareMeters.Equals(other.SquareMeters);

    public override bool Equals(object? obj) => Equals(obj as Surface);

    public override int GetHashCode() => SquareMeters.GetHashCode();

    public override string ToString() => $"{SquareMeters:0.00} m²";
}
