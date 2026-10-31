namespace GeothermalReservoir.Domain;

/// <summary>
/// Explicit finite-volume simulation of single-phase water flow and heat transport in a geological grid.
/// Each step conserves fluid volume (storage change = injected - produced + boundary inflow) and refuses
/// a time step beyond the explicit scheme's stability limit.
/// </summary>
public sealed class ReservoirSimulation
{
    /// <summary>Water dynamic viscosity, Pa·s (hot water).</summary>
    public const double WaterViscosity = 3e-4;

    /// <summary>Total (rock + water) compressibility, 1/Pa.</summary>
    public const double TotalCompressibility = 1.5e-9;

    /// <summary>Water volumetric heat capacity, J/(m³·K).</summary>
    public const double WaterVolumetricHeatCapacity = 1000.0 * 4180.0;

    /// <summary>Rock-grain volumetric heat capacity, J/(m³·K).</summary>
    public const double RockVolumetricHeatCapacity = 2.5e6;

    private readonly Dictionary<string, Well> _wells;
    private readonly HashSet<string> _shutIn = new(StringComparer.Ordinal);
    private readonly Dictionary<CellCoordinate, double> _initialPressures;

    public ReservoirSimulation(GeologicalGrid grid, IReadOnlyCollection<Well> wells, BoundaryCondition boundary)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(wells);
        ArgumentNullException.ThrowIfNull(boundary);

        _wells = new Dictionary<string, Well>(StringComparer.Ordinal);
        HashSet<CellCoordinate> completed = [];
        foreach (Well well in wells)
        {
            if (well is null) throw new ArgumentException("The well list cannot contain null.", nameof(wells));
            if (!grid.Contains(well.Completion))
            {
                throw new ArgumentException($"Well {well.Id} is completed at {well.Completion}, outside the grid.", nameof(wells));
            }

            if (!_wells.TryAdd(well.Id, well))
            {
                throw new ArgumentException($"Well id {well.Id} is used twice.", nameof(wells));
            }

            if (!completed.Add(well.Completion))
            {
                throw new ArgumentException($"Two wells are completed in cell {well.Completion}.", nameof(wells));
            }
        }

        Grid = grid;
        Boundary = boundary;
        _initialPressures = grid.Cells.ToDictionary(c => c.Coordinate, c => c.Pressure.Pascals);
    }

    public GeologicalGrid Grid { get; }
    public BoundaryCondition Boundary { get; }
    public IReadOnlyCollection<Well> Wells => _wells.Values;

    public double ElapsedSeconds { get; private set; }
    public int StepsTaken { get; private set; }
    public double CumulativeInjectedCubicMetres { get; private set; }
    public double CumulativeProducedCubicMetres { get; private set; }
    public double CumulativeBoundaryInflowCubicMetres { get; private set; }

    public bool IsShutIn(string wellId) => _shutIn.Contains(FindWell(wellId).Id);

    /// <summary>Stops a well; it no longer injects or produces. Refuses a well already shut in.</summary>
    public void ShutIn(string wellId)
    {
        Well well = FindWell(wellId);
        if (!_shutIn.Add(well.Id))
        {
            throw new InvalidOperationException($"Well {well.Id} is already shut in.");
        }
    }

    /// <summary>Puts a shut-in well back on stream. Refuses a well that is flowing.</summary>
    public void Reopen(string wellId)
    {
        Well well = FindWell(wellId);
        if (!_shutIn.Remove(well.Id))
        {
            throw new InvalidOperationException($"Well {well.Id} is not shut in.");
        }
    }

    /// <summary>Fluid transmissibility between two face-neighbour cells, m³/(s·Pa).</summary>
    public double Transmissibility(RockCell left, RockCell right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (!left.Coordinate.IsFaceNeighbourOf(right.Coordinate))
        {
            throw new ArgumentException($"Cells {left.Coordinate} and {right.Coordinate} do not share a face.", nameof(right));
        }

        return Permeability.HarmonicMean(left.Permeability, right.Permeability) * Grid.FaceAreaSquareMetres
            / (WaterViscosity * Grid.CellSizeMetres);
    }

    /// <summary>Largest time step, in seconds, for which the explicit pressure and heat updates stay stable.</summary>
    public double MaximumStableTimeStepSeconds()
    {
        double limit = double.PositiveInfinity;
        foreach (RockCell cell in Grid.Cells)
        {
            double fluidCoupling = 0.0;
            double thermalCoupling = 0.0;
            foreach (RockCell neighbour in Grid.NeighboursOf(cell.Coordinate))
            {
                double t = Transmissibility(cell, neighbour);
                fluidCoupling += t;
                thermalCoupling += ConductiveConductance(cell, neighbour)
                    + WaterVolumetricHeatCapacity * Math.Max(0.0, t * (neighbour.Pressure.Pascals - cell.Pressure.Pascals));
            }

            int boundaryFaces = Grid.LateralBoundaryFaceCount(cell.Coordinate);
            double boundaryT = BoundaryTransmissibility(cell) * boundaryFaces;
            fluidCoupling += boundaryT;
            thermalCoupling += WaterVolumetricHeatCapacity * Math.Max(0.0, boundaryT * (Boundary.FarFieldPressure.Pascals - cell.Pressure.Pascals));

            Well? injector = ActiveWellAt(cell.Coordinate);
            if (injector is { Kind: WellKind.Injector })
            {
                thermalCoupling += WaterVolumetricHeatCapacity * injector.RateCubicMetresPerSecond;
            }

            if (fluidCoupling > 0.0) limit = Math.Min(limit, Storage(cell) / fluidCoupling);
            if (thermalCoupling > 0.0) limit = Math.Min(limit, HeatCapacity(cell) / thermalCoupling);
        }

        return limit;
    }

    /// <summary>
    /// Advances pressure and temperature by one explicit step. Refuses a step beyond the stability limit,
    /// and a step that would drive any cell outside the physical range of pressure or temperature.
    /// </summary>
    public void Advance(TimeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        double stable = MaximumStableTimeStepSeconds();
        if (step.Seconds > stable)
        {
            throw new InvalidOperationException(
                $"A {step.Seconds} s step exceeds the explicit stability limit of {stable:0.###} s.");
        }

        double dt = step.Seconds;
        List<(RockCell Cell, double Pressure, double Temperature)> next = [];
        double injected = 0.0;
        double produced = 0.0;
        double boundaryInflow = 0.0;

        foreach (RockCell cell in Grid.Cells)
        {
            double p = cell.Pressure.Pascals;
            double temp = cell.Temperature.Kelvin;
            double netFlow = 0.0;
            double heat = 0.0;

            foreach (RockCell neighbour in Grid.NeighboursOf(cell.Coordinate))
            {
                double flow = Transmissibility(cell, neighbour) * (neighbour.Pressure.Pascals - p);
                netFlow += flow;
                heat += ConductiveConductance(cell, neighbour) * (neighbour.Temperature.Kelvin - temp);
                if (flow > 0.0) heat += WaterVolumetricHeatCapacity * flow * (neighbour.Temperature.Kelvin - temp);
            }

            int boundaryFaces = Grid.LateralBoundaryFaceCount(cell.Coordinate);
            if (boundaryFaces > 0 && !Boundary.IsSealed)
            {
                double flow = BoundaryTransmissibility(cell) * boundaryFaces * (Boundary.FarFieldPressure.Pascals - p);
                netFlow += flow;
                boundaryInflow += flow * dt;
                if (flow > 0.0) heat += WaterVolumetricHeatCapacity * flow * (Boundary.FarFieldTemperature.Kelvin - temp);
            }

            Well? well = ActiveWellAt(cell.Coordinate);
            if (well is not null)
            {
                netFlow += well.SignedRate;
                if (well.Kind == WellKind.Injector)
                {
                    injected += well.RateCubicMetresPerSecond * dt;
                    heat += WaterVolumetricHeatCapacity * well.RateCubicMetresPerSecond * (well.InjectionTemperature!.Kelvin - temp);
                }
                else
                {
                    produced += well.RateCubicMetresPerSecond * dt;
                }
            }

            next.Add((cell, p + dt * netFlow / Storage(cell), temp + dt * heat / HeatCapacity(cell)));
        }

        List<(RockCell Cell, Pressure Pressure, Temperature Temperature)> validated = [];
        foreach ((RockCell cell, double p, double t) in next)
        {
            if (double.IsNaN(p) || p <= 0.0 || p > Pressure.MaximumPascals)
            {
                throw new InvalidOperationException($"The step would drive cell {cell.Coordinate} to an unphysical pressure of {p:0} Pa.");
            }

            if (double.IsNaN(t) || t <= Temperature.FreezingKelvin || t > Temperature.MaximumKelvin)
            {
                throw new InvalidOperationException($"The step would drive cell {cell.Coordinate} to an unsupported temperature of {t:0.##} K.");
            }

            validated.Add((cell, new Pressure(p), new Temperature(t)));
        }

        foreach ((RockCell cell, Pressure p, Temperature t) in validated)
        {
            cell.UpdateState(p, t);
        }

        ElapsedSeconds += dt;
        StepsTaken++;
        CumulativeInjectedCubicMetres += injected;
        CumulativeProducedCubicMetres += produced;
        CumulativeBoundaryInflowCubicMetres += boundaryInflow;
    }

    /// <summary>Fluid volume stored by compression since the start, m³.</summary>
    public double StoredVolumeChangeCubicMetres() =>
        Grid.Cells.Sum(c => Storage(c) * (c.Pressure.Pascals - _initialPressures[c.Coordinate]));

    /// <summary>Storage change minus net inflow; zero up to rounding for a conservative scheme.</summary>
    public double MassBalanceErrorCubicMetres() =>
        StoredVolumeChangeCubicMetres()
        - (CumulativeInjectedCubicMetres - CumulativeProducedCubicMetres + CumulativeBoundaryInflowCubicMetres);

    private Well FindWell(string wellId)
    {
        ArgumentNullException.ThrowIfNull(wellId);
        return _wells.TryGetValue(wellId, out Well? well)
            ? well
            : throw new ArgumentException($"No well {wellId} in this simulation.", nameof(wellId));
    }

    private Well? ActiveWellAt(CellCoordinate coordinate) =>
        _wells.Values.FirstOrDefault(w => w.Completion.Equals(coordinate) && !_shutIn.Contains(w.Id));

    private double Storage(RockCell cell) => cell.Porosity.Fraction * TotalCompressibility * Grid.CellVolumeCubicMetres;

    private double HeatCapacity(RockCell cell) =>
        ((1.0 - cell.Porosity.Fraction) * RockVolumetricHeatCapacity + cell.Porosity.Fraction * WaterVolumetricHeatCapacity)
        * Grid.CellVolumeCubicMetres;

    private double ConductiveConductance(RockCell left, RockCell right) =>
        2.0 * left.ThermalConductivity * right.ThermalConductivity / (left.ThermalConductivity + right.ThermalConductivity)
        * Grid.FaceAreaSquareMetres / Grid.CellSizeMetres;

    private double BoundaryTransmissibility(RockCell cell) =>
        Boundary.TransmissibilityMultiplier * cell.Permeability.SquareMetres * Grid.FaceAreaSquareMetres
        / (WaterViscosity * Grid.CellSizeMetres * 0.5);
}
