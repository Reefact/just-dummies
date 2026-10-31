namespace GeothermalReservoir.Domain;

/// <summary>One control volume of the discretised reservoir; its pressure and temperature evolve.</summary>
public sealed class RockCell
{
    public const double MinimumThermalConductivity = 0.5;
    public const double MaximumThermalConductivity = 10.0;

    public RockCell(
        CellCoordinate coordinate,
        Porosity porosity,
        Permeability permeability,
        double thermalConductivity,
        Pressure pressure,
        Temperature temperature)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        ArgumentNullException.ThrowIfNull(porosity);
        ArgumentNullException.ThrowIfNull(permeability);
        ArgumentNullException.ThrowIfNull(pressure);
        ArgumentNullException.ThrowIfNull(temperature);
        if (double.IsNaN(thermalConductivity)
            || thermalConductivity < MinimumThermalConductivity
            || thermalConductivity > MaximumThermalConductivity)
        {
            throw new ArgumentOutOfRangeException(nameof(thermalConductivity), thermalConductivity,
                $"Rock thermal conductivity must be in [{MinimumThermalConductivity}, {MaximumThermalConductivity}] W/(m·K).");
        }

        Coordinate = coordinate;
        Porosity = porosity;
        Permeability = permeability;
        ThermalConductivity = thermalConductivity;
        Pressure = pressure;
        Temperature = temperature;
    }

    public CellCoordinate Coordinate { get; }
    public Porosity Porosity { get; }
    public Permeability Permeability { get; }

    /// <summary>Rock thermal conductivity in W/(m·K).</summary>
    public double ThermalConductivity { get; }

    public Pressure Pressure { get; private set; }
    public Temperature Temperature { get; private set; }

    internal void UpdateState(Pressure pressure, Temperature temperature)
    {
        Pressure = pressure;
        Temperature = temperature;
    }
}
