using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>Series impedance of a branch, in per-unit on the system base. Branches are inductive.</summary>
public sealed class Impedance : IEquatable<Impedance>
{
    public const double MaximumPu = 10.0;

    public Impedance(double resistancePu, double reactancePu)
    {
        if (!double.IsFinite(resistancePu) || resistancePu < 0.0 || resistancePu > MaximumPu)
        {
            throw new ArgumentOutOfRangeException(nameof(resistancePu), resistancePu, $"Resistance must be within [0, {MaximumPu}] pu.");
        }

        if (!double.IsFinite(reactancePu) || reactancePu <= 0.0 || reactancePu > MaximumPu)
        {
            throw new ArgumentOutOfRangeException(nameof(reactancePu), reactancePu, $"Reactance must be within (0, {MaximumPu}] pu: branches are inductive.");
        }

        ResistancePu = resistancePu;
        ReactancePu = reactancePu;
    }

    public double ResistancePu { get; }

    public double ReactancePu { get; }

    public Complex Value => new(ResistancePu, ReactancePu);

    public Complex Admittance => Complex.Reciprocal(Value);

    public double XOverR => ResistancePu == 0.0 ? double.PositiveInfinity : ReactancePu / ResistancePu;

    public bool Equals(Impedance? other) => other is not null && ResistancePu == other.ResistancePu && ReactancePu == other.ReactancePu;

    public override bool Equals(object? obj) => obj is Impedance other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ResistancePu, ReactancePu);

    public override string ToString() => $"{ResistancePu} + j{ReactancePu} pu";
}
