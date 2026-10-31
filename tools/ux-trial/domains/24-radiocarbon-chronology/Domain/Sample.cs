using System.Text.RegularExpressions;

namespace RadiocarbonChronology.Domain;

/// <summary>A dated sample, identified by its laboratory code (for example "OxA-12345").</summary>
public sealed partial class Sample
{
    public const int MinimumReservoirOffset = -500;
    public const int MaximumReservoirOffset = 2_000;

    public Sample(string labCode, SampleMaterial material, RadiocarbonAge measurement, int reservoirOffset)
    {
        if (labCode is null)
        {
            throw new ArgumentNullException(nameof(labCode));
        }

        if (!LabCodeFormat().IsMatch(labCode))
        {
            throw new FormatException($"'{labCode}' is not a laboratory code: expected two to five letters, a dash and three to six digits, such as 'OxA-12345'.");
        }

        if (!Enum.IsDefined(material))
        {
            throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown sample material.");
        }

        ArgumentNullException.ThrowIfNull(measurement);

        if (material != SampleMaterial.MarineShell && reservoirOffset != 0)
        {
            throw new ArgumentException($"Only a marine shell carries a reservoir offset; a {material} sample cannot have {reservoirOffset}.", nameof(reservoirOffset));
        }

        if (reservoirOffset < MinimumReservoirOffset || reservoirOffset > MaximumReservoirOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(reservoirOffset), reservoirOffset, $"A reservoir offset must lie between {MinimumReservoirOffset} and {MaximumReservoirOffset} years.");
        }

        if (measurement.YearsBp - reservoirOffset < 0)
        {
            throw new ArgumentException($"A reservoir offset of {reservoirOffset} years exceeds the measured {measurement}.", nameof(reservoirOffset));
        }

        LabCode = labCode;
        Material = material;
        Measurement = measurement;
        ReservoirOffset = reservoirOffset;
    }

    public string LabCode { get; }

    public SampleMaterial Material { get; }

    public RadiocarbonAge Measurement { get; }

    public int ReservoirOffset { get; }

    /// <summary>The age the calibration reads: the measurement less the reservoir offset.</summary>
    public RadiocarbonAge CorrectedAge => Measurement.CorrectedFor(ReservoirOffset);

    public CalibratedDate CalibrateWith(CalibrationCurve curve)
    {
        ArgumentNullException.ThrowIfNull(curve);
        return curve.Calibrate(CorrectedAge);
    }

    [GeneratedRegex("^[A-Z][A-Za-z]{1,4}-[0-9]{3,6}$")]
    private static partial Regex LabCodeFormat();
}
