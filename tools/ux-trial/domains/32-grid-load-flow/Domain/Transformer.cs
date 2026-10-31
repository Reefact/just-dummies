using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>A two-winding transformer with an off-nominal tap on its high-voltage side.</summary>
public sealed class Transformer
{
    public const double MinimumTap = 0.9;

    public const double MaximumTap = 1.1;

    public const double TapStep = 0.0125;

    public Transformer(string id, BusId highVoltageBus, BusId lowVoltageBus, Impedance impedance, double ratingMva, double tapRatio)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(highVoltageBus);
        ArgumentNullException.ThrowIfNull(lowVoltageBus);
        ArgumentNullException.ThrowIfNull(impedance);
        if (id.Length > 32)
        {
            throw new ArgumentException("Transformer id must be at most 32 characters.", nameof(id));
        }

        if (highVoltageBus.Equals(lowVoltageBus))
        {
            throw new ArgumentException($"A transformer cannot connect bus {highVoltageBus} to itself.", nameof(lowVoltageBus));
        }

        if (!double.IsFinite(ratingMva) || ratingMva <= 0.0 || ratingMva > Line.MaximumRatingMva)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingMva), ratingMva, $"Rating must be within (0, {Line.MaximumRatingMva}] MVA.");
        }

        if (!double.IsFinite(tapRatio) || tapRatio < MinimumTap || tapRatio > MaximumTap)
        {
            throw new ArgumentOutOfRangeException(nameof(tapRatio), tapRatio, $"Tap ratio must be within [{MinimumTap}, {MaximumTap}].");
        }

        Id = id;
        HighVoltageBus = highVoltageBus;
        LowVoltageBus = lowVoltageBus;
        Impedance = impedance;
        RatingMva = ratingMva;
        TapRatio = tapRatio;
    }

    public string Id { get; }

    public BusId HighVoltageBus { get; }

    public BusId LowVoltageBus { get; }

    public Impedance Impedance { get; }

    public double RatingMva { get; }

    public double TapRatio { get; private set; }

    /// <summary>Admittance-matrix contributions (hv-hv, hv-lv, lv-hv, lv-lv) for the current tap.</summary>
    public (Complex Ff, Complex Ft, Complex Tf, Complex Tt) Stamp()
    {
        Complex y = Impedance.Admittance;
        return (y / (TapRatio * TapRatio), -y / TapRatio, -y / TapRatio, y);
    }

    /// <summary>Moves the tap one step up, which lowers the low-voltage side voltage.</summary>
    public void RaiseTap() => MoveTap(+1);

    /// <summary>Moves the tap one step down, which raises the low-voltage side voltage.</summary>
    public void LowerTap() => MoveTap(-1);

    private void MoveTap(int direction)
    {
        double next = Math.Round(TapRatio + (direction * TapStep), 6);
        if (next < MinimumTap - 1e-9 || next > MaximumTap + 1e-9)
        {
            throw new InvalidOperationException($"Transformer {Id} tap {TapRatio} is at its end stop; it cannot move to {next}.");
        }

        TapRatio = next;
    }
}
