namespace CatastropheLosses.Domain;

/// <summary>
/// The geographic extent of an event's hazard: intensity is highest at the centre and decays
/// linearly to zero at the edge of the radius.
/// </summary>
public sealed class Footprint
{
    public const double MaxRadiusKm = 2000.0;

    public Footprint(GeoPoint center, double radiusKm, double peakIntensity)
    {
        ArgumentNullException.ThrowIfNull(center);

        if (double.IsNaN(radiusKm) || radiusKm <= 0.0 || radiusKm > MaxRadiusKm)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusKm), radiusKm, $"Footprint radius must lie within (0, {MaxRadiusKm}] km.");
        }

        if (!double.IsFinite(peakIntensity) || peakIntensity <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(peakIntensity), peakIntensity, "Peak intensity must be a finite, strictly positive number.");
        }

        Center = center;
        RadiusKm = radiusKm;
        PeakIntensity = peakIntensity;
    }

    public GeoPoint Center { get; }

    public double RadiusKm { get; }

    public double PeakIntensity { get; }

    public bool Covers(GeoPoint location) => Center.DistanceKmTo(location) <= RadiusKm;

    /// <summary>The hazard intensity felt at a location; zero outside the footprint.</summary>
    public double IntensityAt(GeoPoint location)
    {
        ArgumentNullException.ThrowIfNull(location);
        double distance = Center.DistanceKmTo(location);
        return distance >= RadiusKm ? 0.0 : PeakIntensity * (1.0 - (distance / RadiusKm));
    }
}
