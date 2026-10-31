namespace RadiocarbonChronology.Domain;

/// <summary>
/// The chronology of one excavated site: its layers, their samples, their succession, and the calibration curve the
/// measurements are read against. Modelling it yields one occupation estimate per layer, constrained by the stratigraphy.
/// </summary>
public sealed class SiteChronology
{
    /// <summary>Below this share of its probability left by the stratigraphy, a layer's dates are declared incompatible.</summary>
    public const double MinimumRetainedProbability = 0.001;

    private readonly List<StratigraphicLayer> _layers = new();
    private readonly List<PrecedenceRelation> _relations = new();
    private IReadOnlyList<OccupationEstimate>? _estimates;

    public SiteChronology(string siteName, CalibrationCurve curve)
    {
        if (string.IsNullOrWhiteSpace(siteName))
        {
            throw new ArgumentException("A site needs a name.", nameof(siteName));
        }

        ArgumentNullException.ThrowIfNull(curve);
        SiteName = siteName;
        Curve = curve;
    }

    public string SiteName { get; }

    public CalibrationCurve Curve { get; }

    public IReadOnlyList<StratigraphicLayer> Layers => _layers;

    public IReadOnlyList<PrecedenceRelation> Relations => _relations;

    public bool IsModelled => _estimates is not null;

    /// <summary>The estimates of the last modelling; refused while the chronology is not modelled.</summary>
    public IReadOnlyList<OccupationEstimate> Estimates =>
        _estimates ?? throw new InvalidOperationException($"The chronology of '{SiteName}' has not been modelled yet.");

    public void AddLayer(StratigraphicLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        EnsureOpen();
        if (FindLayer(layer.Code) is not null)
        {
            throw new InvalidOperationException($"Layer '{layer.Code}' is already recorded on '{SiteName}'.");
        }

        _layers.Add(layer);
    }

    public void AttachSample(string layerCode, Sample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        EnsureOpen();
        StratigraphicLayer layer = FindLayer(layerCode) ?? throw new KeyNotFoundException($"No layer '{layerCode}' on '{SiteName}'.");
        if (_layers.Any(l => l.Samples.Any(s => string.Equals(s.LabCode, sample.LabCode, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException($"Sample '{sample.LabCode}' is already attached on '{SiteName}'.");
        }

        layer.Add(sample);
    }

    /// <summary>Records a succession; refuses unknown layers, duplicates, and any relation that would close a cycle.</summary>
    public void AddRelation(PrecedenceRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);
        EnsureOpen();
        if (FindLayer(relation.OlderLayerCode) is null)
        {
            throw new KeyNotFoundException($"No layer '{relation.OlderLayerCode}' on '{SiteName}'.");
        }

        if (FindLayer(relation.YoungerLayerCode) is null)
        {
            throw new KeyNotFoundException($"No layer '{relation.YoungerLayerCode}' on '{SiteName}'.");
        }

        if (_relations.Contains(relation))
        {
            throw new InvalidOperationException($"The succession {relation} is already recorded.");
        }

        if (Precedes(relation.YoungerLayerCode, relation.OlderLayerCode))
        {
            throw new StratigraphicInconsistencyException(
                relation.OlderLayerCode,
                relation.YoungerLayerCode,
                $"'{relation.YoungerLayerCode}' already lies beneath '{relation.OlderLayerCode}': {relation} would close a cycle.");
        }

        _relations.Add(relation);
    }

    /// <summary>Allows the chronology to be edited again, discarding the last estimates.</summary>
    public void Reopen() => _estimates = null;

    /// <summary>
    /// Calibrates every sample, combines each layer's dates, then applies the succession: a layer cannot start before
    /// the plausible start of the layers beneath it, nor end after the plausible end of the layers above it.
    /// </summary>
    public IReadOnlyList<OccupationEstimate> Model()
    {
        EnsureOpen();
        if (_layers.Count == 0)
        {
            throw new InvalidOperationException($"'{SiteName}' has no layer to model.");
        }

        StratigraphicLayer? undated = _layers.FirstOrDefault(l => l.Samples.Count == 0);
        if (undated is not null)
        {
            throw new InvalidOperationException($"Layer '{undated.Code}' has no dated sample.");
        }

        List<string> order = TopologicalOrder();
        Dictionary<string, CalibratedDate> dates = _layers.ToDictionary(
            l => l.Code,
            l => CalibratedDate.Combine(l.Samples.Select(s => s.CalibrateWith(Curve)).ToList()),
            StringComparer.Ordinal);

        foreach (string code in order)
        {
            foreach (PrecedenceRelation relation in _relations.Where(r => r.YoungerLayerCode == code))
            {
                int notBefore = Envelope(dates[relation.OlderLayerCode]).StartYear;
                dates[code] = Constrain(dates[code], notBefore, CalendarInterval.LatestYear, relation);
            }
        }

        for (int i = order.Count - 1; i >= 0; i--)
        {
            string code = order[i];
            foreach (PrecedenceRelation relation in _relations.Where(r => r.OlderLayerCode == code))
            {
                int notAfter = Envelope(dates[relation.YoungerLayerCode]).EndYear;
                dates[code] = Constrain(dates[code], CalendarInterval.EarliestYear, notAfter, relation);
            }
        }

        _estimates = order.Select(code => new OccupationEstimate(code, dates[code])).ToList();
        return _estimates;
    }

    private static CalibratedDate Constrain(CalibratedDate date, int notBefore, int notAfter, PrecedenceRelation relation)
    {
        double retained = 0;
        for (int year = Math.Max(notBefore, date.FirstYear); year <= Math.Min(notAfter, date.LastYear); year++)
        {
            retained += date.ProbabilityOf(year);
        }

        if (retained < MinimumRetainedProbability)
        {
            throw new StratigraphicInconsistencyException(
                relation.OlderLayerCode,
                relation.YoungerLayerCode,
                $"The dates of '{relation.OlderLayerCode}' and '{relation.YoungerLayerCode}' contradict the succession {relation}.");
        }

        return date.Truncate(notBefore, notAfter);
    }

    private static CalendarInterval Envelope(CalibratedDate date)
    {
        IReadOnlyList<CalendarInterval> region = date.HighestDensityRegion(OccupationEstimate.CredibleLevel);
        return new CalendarInterval(region[0].StartYear, region[^1].EndYear);
    }

    private StratigraphicLayer? FindLayer(string code) =>
        _layers.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.Ordinal));

    private bool Precedes(string older, string younger)
    {
        Stack<string> pending = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        pending.Push(older);
        while (pending.Count > 0)
        {
            string current = pending.Pop();
            if (string.Equals(current, younger, StringComparison.Ordinal))
            {
                return true;
            }

            if (seen.Add(current))
            {
                foreach (PrecedenceRelation relation in _relations.Where(r => r.OlderLayerCode == current))
                {
                    pending.Push(relation.YoungerLayerCode);
                }
            }
        }

        return false;
    }

    private List<string> TopologicalOrder()
    {
        Dictionary<string, int> incoming = _layers.ToDictionary(l => l.Code, _ => 0, StringComparer.Ordinal);
        foreach (PrecedenceRelation relation in _relations)
        {
            incoming[relation.YoungerLayerCode]++;
        }

        Queue<string> ready = new(_layers.Select(l => l.Code).Where(c => incoming[c] == 0));
        List<string> order = new();
        while (ready.Count > 0)
        {
            string code = ready.Dequeue();
            order.Add(code);
            foreach (PrecedenceRelation relation in _relations.Where(r => r.OlderLayerCode == code))
            {
                if (--incoming[relation.YoungerLayerCode] == 0)
                {
                    ready.Enqueue(relation.YoungerLayerCode);
                }
            }
        }

        return order;
    }

    private void EnsureOpen()
    {
        if (IsModelled)
        {
            throw new InvalidOperationException($"The chronology of '{SiteName}' is modelled; reopen it before changing it.");
        }
    }
}
