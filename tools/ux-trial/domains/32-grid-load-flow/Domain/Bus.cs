namespace GridLoadFlow.Domain;

/// <summary>How the load flow treats a bus.</summary>
public enum BusType
{
    /// <summary>Load bus: P and Q are specified, V and angle are solved.</summary>
    Pq,

    /// <summary>Generator bus: P and |V| are specified, Q and angle are solved.</summary>
    Pv,

    /// <summary>Reference bus: |V| and angle 0 are specified, P and Q balance the network.</summary>
    Slack,
}

/// <summary>A node of the network, with its generation, load and voltage regulation.</summary>
public sealed class Bus
{
    public const double MaximumBaseKv = 1_000.0;

    public Bus(
        BusId id,
        string name,
        BusType type,
        double baseKv,
        PowerInjection generation,
        PowerInjection load,
        double? voltageSetpointPu,
        VoltageLimits limits)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(generation);
        ArgumentNullException.ThrowIfNull(load);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 64)
        {
            throw new ArgumentException("Bus name must be at most 64 characters.", nameof(name));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown bus type.");
        }

        if (!double.IsFinite(baseKv) || baseKv <= 0.0 || baseKv > MaximumBaseKv)
        {
            throw new ArgumentOutOfRangeException(nameof(baseKv), baseKv, $"Base voltage must be within (0, {MaximumBaseKv}] kV.");
        }

        if (generation.ActiveMw < 0.0)
        {
            throw new ArgumentException("Generation cannot have negative active power; model consumption as load.", nameof(generation));
        }

        if (type == BusType.Pq && voltageSetpointPu is not null)
        {
            throw new ArgumentException("A PQ bus does not regulate its voltage: it takes no setpoint.", nameof(voltageSetpointPu));
        }

        if (type != BusType.Pq)
        {
            if (voltageSetpointPu is not { } setpoint)
            {
                throw new ArgumentException($"A {type} bus regulates its voltage: it needs a setpoint.", nameof(voltageSetpointPu));
            }

            if (!limits.Contains(setpoint))
            {
                throw new ArgumentOutOfRangeException(nameof(voltageSetpointPu), setpoint, $"Voltage setpoint must lie within the bus limits {limits}.");
            }
        }

        if (type == BusType.Pq && generation.ActiveMw != 0.0)
        {
            throw new ArgumentException("A PQ bus carries no active generation; make it a PV bus.", nameof(generation));
        }

        Id = id;
        Name = name;
        Type = type;
        BaseKv = baseKv;
        Generation = generation;
        Load = load;
        VoltageSetpointPu = voltageSetpointPu;
        Limits = limits;
    }

    public BusId Id { get; }

    public string Name { get; }

    public BusType Type { get; }

    public double BaseKv { get; }

    public PowerInjection Generation { get; private set; }

    public PowerInjection Load { get; private set; }

    public double? VoltageSetpointPu { get; }

    public VoltageLimits Limits { get; }

    /// <summary>Net injection into the network: generation minus load.</summary>
    public PowerInjection NetInjection => new(Generation.ActiveMw - Load.ActiveMw, Generation.ReactiveMvar - Load.ReactiveMvar);

    public void ChangeLoad(PowerInjection load)
    {
        ArgumentNullException.ThrowIfNull(load);
        Load = load;
    }

    public void Dispatch(PowerInjection generation)
    {
        ArgumentNullException.ThrowIfNull(generation);
        if (Type == BusType.Pq)
        {
            throw new InvalidOperationException($"Bus {Id} is a PQ bus and cannot be dispatched.");
        }

        if (generation.ActiveMw < 0.0)
        {
            throw new ArgumentException("Generation cannot have negative active power.", nameof(generation));
        }

        Generation = generation;
    }
}
