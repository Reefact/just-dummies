namespace GeothermalReservoir.Domain;

/// <summary>
/// The lateral boundary: a far-field aquifer at fixed pressure and temperature, connected to every lateral
/// boundary face through a transmissibility multiplier. A multiplier of 0 is a sealed (no-flow, insulated) boundary.
/// </summary>
public sealed class BoundaryCondition
{
    public const double MaximumMultiplier = 1.0;

    public BoundaryCondition(Pressure farFieldPressure, Temperature farFieldTemperature, double transmissibilityMultiplier)
    {
        ArgumentNullException.ThrowIfNull(farFieldPressure);
        ArgumentNullException.ThrowIfNull(farFieldTemperature);
        if (double.IsNaN(transmissibilityMultiplier) || transmissibilityMultiplier < 0.0 || transmissibilityMultiplier > MaximumMultiplier)
        {
            throw new ArgumentOutOfRangeException(nameof(transmissibilityMultiplier), transmissibilityMultiplier,
                $"The boundary transmissibility multiplier must be in [0, {MaximumMultiplier}].");
        }

        FarFieldPressure = farFieldPressure;
        FarFieldTemperature = farFieldTemperature;
        TransmissibilityMultiplier = transmissibilityMultiplier;
    }

    public Pressure FarFieldPressure { get; }
    public Temperature FarFieldTemperature { get; }
    public double TransmissibilityMultiplier { get; }
    public bool IsSealed => TransmissibilityMultiplier == 0.0;
}
