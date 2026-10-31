namespace PulmonaryNodules.Domain;

/// <summary>A nodule volume with the absolute uncertainty its segmentation carries, in cubic millimetres.</summary>
public sealed class MeasuredVolume
{
    /// <summary>The volume of a 30 mm sphere, beyond which a lesion is a mass, not a nodule (with margin).</summary>
    public const double MaxVolumeMm3 = 20_000.0;

    public MeasuredVolume(double volumeMm3, double uncertaintyMm3)
    {
        if (double.IsNaN(volumeMm3) || volumeMm3 <= 0.0 || volumeMm3 > MaxVolumeMm3)
        {
            throw new ArgumentOutOfRangeException(nameof(volumeMm3), volumeMm3, $"A nodule volume must lie within (0, {MaxVolumeMm3}] mm³.");
        }

        if (double.IsNaN(uncertaintyMm3) || uncertaintyMm3 < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(uncertaintyMm3), uncertaintyMm3, "An uncertainty cannot be negative.");
        }

        if (uncertaintyMm3 >= volumeMm3)
        {
            throw new ArgumentException($"The uncertainty ({uncertaintyMm3} mm³) must be smaller than the volume ({volumeMm3} mm³).", nameof(uncertaintyMm3));
        }

        VolumeMm3 = volumeMm3;
        UncertaintyMm3 = uncertaintyMm3;
    }

    public double VolumeMm3 { get; }

    public double UncertaintyMm3 { get; }

    public double LowerBoundMm3 => VolumeMm3 - UncertaintyMm3;

    public double UpperBoundMm3 => VolumeMm3 + UncertaintyMm3;

    /// <summary>The diameter of the sphere of the same volume, in millimetres.</summary>
    public double EquivalentDiameterMm => Math.Cbrt(6.0 * VolumeMm3 / Math.PI);

    public override string ToString() => $"{VolumeMm3:0.#} ± {UncertaintyMm3:0.#} mm³";
}
