namespace RadiocarbonChronology.Domain;

/// <summary>The modelled period of a layer: its constrained date and the plausible intervals at 95.4%.</summary>
public sealed class OccupationEstimate
{
    public const double CredibleLevel = 0.954;

    public OccupationEstimate(string layerCode, CalibratedDate date)
    {
        if (string.IsNullOrWhiteSpace(layerCode))
        {
            throw new ArgumentException("The layer code is required.", nameof(layerCode));
        }

        ArgumentNullException.ThrowIfNull(date);
        LayerCode = layerCode;
        Date = date;
        PlausibleIntervals = date.HighestDensityRegion(CredibleLevel);
    }

    public string LayerCode { get; }

    public CalibratedDate Date { get; }

    public IReadOnlyList<CalendarInterval> PlausibleIntervals { get; }

    public bool IsMultimodal => PlausibleIntervals.Count > 1;

    public CalendarInterval Envelope => new(PlausibleIntervals[0].StartYear, PlausibleIntervals[^1].EndYear);
}
