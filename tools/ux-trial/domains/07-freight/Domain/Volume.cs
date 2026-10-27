namespace Freight.Domain;

/// <summary>A cargo volume in cubic metres, up to the capacity of the largest standard container.</summary>
public sealed class Volume : IEquatable<Volume> {

    public const double MaxCubicMetres = 76.4;

    public Volume(double cubicMetres) {
        if (double.IsNaN(cubicMetres)) {
            throw new ArgumentOutOfRangeException(nameof(cubicMetres), cubicMetres, "A volume must be a number.");
        }
        if (cubicMetres is <= 0 or > MaxCubicMetres) {
            throw new ArgumentOutOfRangeException(nameof(cubicMetres), cubicMetres, $"A volume lies in ]0, {MaxCubicMetres}] m3.");
        }

        CubicMetres = cubicMetres;
    }

    public double CubicMetres { get; }

    public bool Equals(Volume? other) => other is not null && CubicMetres.Equals(other.CubicMetres);

    public override bool Equals(object? obj) => Equals(obj as Volume);

    public override int GetHashCode() => CubicMetres.GetHashCode();

    public override string ToString() => $"{CubicMetres} m3";

}
