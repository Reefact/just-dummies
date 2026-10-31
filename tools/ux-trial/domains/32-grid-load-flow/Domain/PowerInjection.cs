using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>Complex power: active power in MW and reactive power in Mvar.</summary>
public sealed class PowerInjection : IEquatable<PowerInjection>
{
    public const double MaximumMagnitude = 10_000.0;

    public static readonly PowerInjection Zero = new(0.0, 0.0);

    public PowerInjection(double activeMw, double reactiveMvar)
    {
        if (!double.IsFinite(activeMw) || Math.Abs(activeMw) > MaximumMagnitude)
        {
            throw new ArgumentOutOfRangeException(nameof(activeMw), activeMw, $"Active power must be finite and within ±{MaximumMagnitude} MW.");
        }

        if (!double.IsFinite(reactiveMvar) || Math.Abs(reactiveMvar) > MaximumMagnitude)
        {
            throw new ArgumentOutOfRangeException(nameof(reactiveMvar), reactiveMvar, $"Reactive power must be finite and within ±{MaximumMagnitude} Mvar.");
        }

        ActiveMw = activeMw;
        ReactiveMvar = reactiveMvar;
    }

    public double ActiveMw { get; }

    public double ReactiveMvar { get; }

    public double ApparentMva => Math.Sqrt((ActiveMw * ActiveMw) + (ReactiveMvar * ReactiveMvar));

    /// <summary>Power factor, 1 for a zero injection.</summary>
    public double PowerFactor => ApparentMva == 0.0 ? 1.0 : Math.Abs(ActiveMw) / ApparentMva;

    public Complex ToPerUnit(double baseMva) => new(ActiveMw / baseMva, ReactiveMvar / baseMva);

    public bool Equals(PowerInjection? other) => other is not null && ActiveMw == other.ActiveMw && ReactiveMvar == other.ReactiveMvar;

    public override bool Equals(object? obj) => obj is PowerInjection other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ActiveMw, ReactiveMvar);

    public override string ToString() => $"{ActiveMw} MW + j{ReactiveMvar} Mvar";
}
