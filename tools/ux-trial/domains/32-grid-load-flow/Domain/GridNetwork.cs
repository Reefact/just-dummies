using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>The network aggregate: buses connected by lines and transformers, with exactly one slack bus.</summary>
public sealed class GridNetwork
{
    public const double MaximumBaseMva = 10_000.0;

    private readonly Dictionary<BusId, Bus> buses;
    private readonly Dictionary<string, Line> lines;
    private readonly Dictionary<string, Transformer> transformers;

    public GridNetwork(string name, double baseMva, IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer> transformers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(buses);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(transformers);
        if (!double.IsFinite(baseMva) || baseMva <= 0.0 || baseMva > MaximumBaseMva)
        {
            throw new ArgumentOutOfRangeException(nameof(baseMva), baseMva, $"Base power must be within (0, {MaximumBaseMva}] MVA.");
        }

        this.buses = new Dictionary<BusId, Bus>();
        foreach (Bus bus in buses)
        {
            ArgumentNullException.ThrowIfNull(bus, nameof(buses));
            if (!this.buses.TryAdd(bus.Id, bus))
            {
                throw new ArgumentException($"Bus {bus.Id} appears twice.", nameof(buses));
            }
        }

        if (this.buses.Count < 2)
        {
            throw new ArgumentException("A network needs at least two buses.", nameof(buses));
        }

        int slackCount = this.buses.Values.Count(b => b.Type == BusType.Slack);
        if (slackCount != 1)
        {
            throw new ArgumentException($"A network needs exactly one slack bus; found {slackCount}.", nameof(buses));
        }

        HashSet<string> branchIds = new(StringComparer.Ordinal);
        this.lines = new Dictionary<string, Line>(StringComparer.Ordinal);
        foreach (Line line in lines)
        {
            ArgumentNullException.ThrowIfNull(line, nameof(lines));
            if (!branchIds.Add(line.Id))
            {
                throw new ArgumentException($"Branch id {line.Id} appears twice.", nameof(lines));
            }

            Bus from = RequireBus(line.FromBus, line.Id, nameof(lines));
            Bus to = RequireBus(line.ToBus, line.Id, nameof(lines));
            if (from.BaseKv != to.BaseKv)
            {
                throw new ArgumentException($"Line {line.Id} joins {from.BaseKv} kV to {to.BaseKv} kV; a voltage change needs a transformer.", nameof(lines));
            }

            this.lines.Add(line.Id, line);
        }

        this.transformers = new Dictionary<string, Transformer>(StringComparer.Ordinal);
        foreach (Transformer transformer in transformers)
        {
            ArgumentNullException.ThrowIfNull(transformer, nameof(transformers));
            if (!branchIds.Add(transformer.Id))
            {
                throw new ArgumentException($"Branch id {transformer.Id} appears twice.", nameof(transformers));
            }

            Bus hv = RequireBus(transformer.HighVoltageBus, transformer.Id, nameof(transformers));
            Bus lv = RequireBus(transformer.LowVoltageBus, transformer.Id, nameof(transformers));
            if (hv.BaseKv <= lv.BaseKv)
            {
                throw new ArgumentException($"Transformer {transformer.Id} high-voltage side ({hv.BaseKv} kV) must be above its low-voltage side ({lv.BaseKv} kV).", nameof(transformers));
            }

            this.transformers.Add(transformer.Id, transformer);
        }

        if (!IsConnected(excludedLineId: null))
        {
            throw new ArgumentException("The network is islanded: every bus must be reachable from the slack bus.", nameof(lines));
        }

        Name = name;
        BaseMva = baseMva;
    }

    public string Name { get; }

    public double BaseMva { get; }

    public IReadOnlyCollection<Bus> Buses => buses.Values;

    public IReadOnlyCollection<Line> Lines => lines.Values;

    public IReadOnlyCollection<Transformer> Transformers => transformers.Values;

    public Bus SlackBus => buses.Values.Single(b => b.Type == BusType.Slack);

    public Bus GetBus(BusId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return buses.TryGetValue(id, out Bus? bus) ? bus : throw new KeyNotFoundException($"Unknown bus {id}.");
    }

    public void ChangeLoad(BusId busId, PowerInjection load) => GetBus(busId).ChangeLoad(load);

    /// <summary>Opens a line, refusing when that would island part of the network.</summary>
    public void TakeLineOutOfService(string lineId)
    {
        Line line = GetLine(lineId);
        if (line.InService && !IsConnected(excludedLineId: lineId))
        {
            throw new InvalidOperationException($"Opening line {lineId} would island part of the network.");
        }

        line.Open();
    }

    public void ReturnLineToService(string lineId) => GetLine(lineId).Close();

    public Transformer GetTransformer(string transformerId)
    {
        ArgumentNullException.ThrowIfNull(transformerId);
        return transformers.TryGetValue(transformerId, out Transformer? t) ? t : throw new KeyNotFoundException($"Unknown transformer {transformerId}.");
    }

    public Line GetLine(string lineId)
    {
        ArgumentNullException.ThrowIfNull(lineId);
        return lines.TryGetValue(lineId, out Line? line) ? line : throw new KeyNotFoundException($"Unknown line {lineId}.");
    }

    /// <summary>Total load minus total scheduled generation of non-slack buses, in MW: what the slack must at least supply.</summary>
    public double SlackActiveDemandMw =>
        buses.Values.Sum(b => b.Load.ActiveMw) - buses.Values.Where(b => b.Type != BusType.Slack).Sum(b => b.Generation.ActiveMw);

    /// <summary>Bus ordering used by the admittance matrix.</summary>
    internal IReadOnlyList<BusId> BusOrder() => buses.Keys.ToList();

    internal Complex[,] BuildAdmittanceMatrix(IReadOnlyList<BusId> order)
    {
        Dictionary<BusId, int> index = order.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i);
        Complex[,] y = new Complex[order.Count, order.Count];
        foreach (Line line in lines.Values.Where(l => l.InService))
        {
            Stamp(y, index[line.FromBus], index[line.ToBus], line.Stamp());
        }

        foreach (Transformer transformer in transformers.Values)
        {
            Stamp(y, index[transformer.HighVoltageBus], index[transformer.LowVoltageBus], transformer.Stamp());
        }

        return y;
    }

    private static void Stamp(Complex[,] y, int f, int t, (Complex Ff, Complex Ft, Complex Tf, Complex Tt) s)
    {
        y[f, f] += s.Ff;
        y[f, t] += s.Ft;
        y[t, f] += s.Tf;
        y[t, t] += s.Tt;
    }

    private Bus RequireBus(BusId id, string branchId, string paramName) =>
        buses.TryGetValue(id, out Bus? bus) ? bus : throw new ArgumentException($"Branch {branchId} refers to unknown bus {id}.", paramName);

    private bool IsConnected(string? excludedLineId)
    {
        Dictionary<BusId, List<BusId>> adjacency = buses.Keys.ToDictionary(k => k, _ => new List<BusId>());
        foreach (Line line in lines.Values.Where(l => l.InService && l.Id != excludedLineId))
        {
            adjacency[line.FromBus].Add(line.ToBus);
            adjacency[line.ToBus].Add(line.FromBus);
        }

        foreach (Transformer transformer in transformers.Values)
        {
            adjacency[transformer.HighVoltageBus].Add(transformer.LowVoltageBus);
            adjacency[transformer.LowVoltageBus].Add(transformer.HighVoltageBus);
        }

        BusId start = buses.Values.Single(b => b.Type == BusType.Slack).Id;
        HashSet<BusId> seen = new() { start };
        Queue<BusId> queue = new();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            foreach (BusId next in adjacency[queue.Dequeue()])
            {
                if (seen.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return seen.Count == buses.Count;
    }
}
