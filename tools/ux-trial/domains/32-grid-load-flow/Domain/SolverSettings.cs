namespace GridLoadFlow.Domain;

/// <summary>Convergence settings of the Gauss-Seidel solver.</summary>
public sealed class SolverSettings
{
    public static readonly SolverSettings Default = new(1e-6, 500, 1.4);

    public SolverSettings(double tolerancePu, int maximumIterations, double accelerationFactor)
    {
        if (!double.IsFinite(tolerancePu) || tolerancePu < 1e-12 || tolerancePu > 1e-2)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerancePu), tolerancePu, "Tolerance must be within [1e-12, 1e-2] pu.");
        }

        if (maximumIterations < 1 || maximumIterations > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "Maximum iterations must be within [1, 10000].");
        }

        if (!double.IsFinite(accelerationFactor) || accelerationFactor < 1.0 || accelerationFactor >= 2.0)
        {
            throw new ArgumentOutOfRangeException(nameof(accelerationFactor), accelerationFactor, "Acceleration factor must be within [1.0, 2.0).");
        }

        TolerancePu = tolerancePu;
        MaximumIterations = maximumIterations;
        AccelerationFactor = accelerationFactor;
    }

    public double TolerancePu { get; }

    public int MaximumIterations { get; }

    public double AccelerationFactor { get; }
}
