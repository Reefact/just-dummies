using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>A transmission line between two buses of the same voltage level (pi model).</summary>
public sealed class Line
{
    public const double MaximumChargingSusceptancePu = 5.0;

    public const double MaximumRatingMva = 10_000.0;

    public Line(string id, BusId fromBus, BusId toBus, Impedance impedance, double chargingSusceptancePu, double ratingMva)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(fromBus);
        ArgumentNullException.ThrowIfNull(toBus);
        ArgumentNullException.ThrowIfNull(impedance);
        if (id.Length > 32)
        {
            throw new ArgumentException("Line id must be at most 32 characters.", nameof(id));
        }

        if (fromBus.Equals(toBus))
        {
            throw new ArgumentException($"A line cannot connect bus {fromBus} to itself.", nameof(toBus));
        }

        if (!double.IsFinite(chargingSusceptancePu) || chargingSusceptancePu < 0.0 || chargingSusceptancePu > MaximumChargingSusceptancePu)
        {
            throw new ArgumentOutOfRangeException(nameof(chargingSusceptancePu), chargingSusceptancePu, $"Charging susceptance must be within [0, {MaximumChargingSusceptancePu}] pu.");
        }

        if (!double.IsFinite(ratingMva) || ratingMva <= 0.0 || ratingMva > MaximumRatingMva)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingMva), ratingMva, $"Rating must be within (0, {MaximumRatingMva}] MVA.");
        }

        Id = id;
        FromBus = fromBus;
        ToBus = toBus;
        Impedance = impedance;
        ChargingSusceptancePu = chargingSusceptancePu;
        RatingMva = ratingMva;
        InService = true;
    }

    public string Id { get; }

    public BusId FromBus { get; }

    public BusId ToBus { get; }

    public Impedance Impedance { get; }

    public double ChargingSusceptancePu { get; }

    public double RatingMva { get; }

    public bool InService { get; private set; }

    /// <summary>Admittance-matrix contributions (from-from, from-to, to-from, to-to).</summary>
    public (Complex Ff, Complex Ft, Complex Tf, Complex Tt) Stamp()
    {
        Complex y = Impedance.Admittance;
        Complex halfCharging = new(0.0, ChargingSusceptancePu / 2.0);
        return (y + halfCharging, -y, -y, y + halfCharging);
    }

    internal void Open()
    {
        if (!InService)
        {
            throw new InvalidOperationException($"Line {Id} is already out of service.");
        }

        InService = false;
    }

    internal void Close()
    {
        if (InService)
        {
            throw new InvalidOperationException($"Line {Id} is already in service.");
        }

        InService = true;
    }
}
