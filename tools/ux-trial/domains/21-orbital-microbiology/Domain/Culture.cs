namespace OrbitalMicrobiology.Domain;

/// <summary>
/// A culture grown in one chamber of the station: the aggregate root. It is inoculated with
/// one to <see cref="MaxSpecies"/> species at time zero, then advanced in time under its
/// experimental conditions (logistic growth with Lotka–Volterra competition between species),
/// receives observations that correct its estimated state, and forecasts ahead. Once
/// terminated it refuses every change.
/// </summary>
public sealed class Culture
{
    public const int MaxSpecies = 4;
    public const double MaxAdvanceHours = 720;
    public const double ObservationTimeToleranceHours = 0.5;
    public const double InitialRelativeUncertainty = 0.05;
    public const double DeathRatePerHour = 0.1;
    public const double MinInteraction = -1;
    public const double MaxInteraction = 2;

    private const double StepHours = 0.1;
    private const double StationaryLoad = 0.95;

    private readonly List<MicrobialPopulation> populations = [];
    private readonly Dictionary<Species, Concentration> inocula = [];
    private readonly Dictionary<(Species Affected, Species Competitor), double> interactions = [];
    private readonly List<Observation> observations = [];

    public Culture(CultureId id, ExperimentalConditions conditions, DateTimeOffset startedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(conditions);
        if (startedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"Station time is UTC; got offset {startedAtUtc.Offset}.", nameof(startedAtUtc));
        }

        Id = id;
        Conditions = conditions;
        StartedAtUtc = startedAtUtc;
    }

    public CultureId Id { get; }

    public ExperimentalConditions Conditions { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public double ElapsedHours { get; private set; }

    public DateTimeOffset CurrentTimeUtc => StartedAtUtc.AddHours(ElapsedHours);

    public bool IsTerminated { get; private set; }

    public IReadOnlyList<MicrobialPopulation> Populations => populations;

    public IReadOnlyList<Observation> Observations => observations;

    /// <summary>Adds a species at time zero. The inoculum is positive and below the species' carrying capacity here.</summary>
    public MicrobialPopulation Inoculate(Species species, Concentration inoculum)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(inoculum);
        ThrowIfTerminated();
        if (ElapsedHours > 0)
        {
            throw new InvalidOperationException($"Culture {Id} has already grown {ElapsedHours} h; species are inoculated at time zero.");
        }

        if (inocula.ContainsKey(species))
        {
            throw new InvalidOperationException($"{species} is already in culture {Id}.");
        }

        if (populations.Count == MaxSpecies)
        {
            throw new InvalidOperationException($"A culture holds at most {MaxSpecies} species.");
        }

        Concentration capacity = Conditions.CarryingCapacity(species);
        if (inoculum.CellsPerMillilitre <= 0 || inoculum.CompareTo(capacity) >= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inoculum),
                inoculum,
                $"An inoculum is positive and below the carrying capacity ({capacity}).");
        }

        MicrobialPopulation population = new(species, inoculum, InitialRelativeUncertainty, 0);
        populations.Add(population);
        inocula.Add(species, inoculum);
        return population;
    }

    /// <summary>
    /// Declares how strongly <paramref name="competitor"/> weighs on <paramref name="affected"/>'s
    /// growth (α in Lotka–Volterra): positive competes, negative facilitates.
    /// </summary>
    public void SetInteraction(Species affected, Species competitor, double coefficient)
    {
        ArgumentNullException.ThrowIfNull(affected);
        ArgumentNullException.ThrowIfNull(competitor);
        ThrowIfTerminated();
        if (affected.Equals(competitor))
        {
            throw new ArgumentException($"A species does not interact with itself ({affected}); that is its carrying capacity.", nameof(competitor));
        }

        PopulationOf(affected);
        PopulationOf(competitor);
        if (!double.IsFinite(coefficient) || coefficient < MinInteraction || coefficient > MaxInteraction)
        {
            throw new ArgumentOutOfRangeException(nameof(coefficient), coefficient, $"An interaction coefficient lies in [{MinInteraction}, {MaxInteraction}].");
        }

        interactions[(affected, competitor)] = coefficient;
    }

    public double InteractionOf(Species affected, Species competitor) =>
        interactions.TryGetValue((affected, competitor), out double coefficient) ? coefficient : 0;

    /// <summary>Lets the culture grow for the given time.</summary>
    public void Advance(double hours)
    {
        ThrowIfTerminated();
        if (!double.IsFinite(hours) || hours <= 0 || hours > MaxAdvanceHours)
        {
            throw new ArgumentOutOfRangeException(nameof(hours), hours, $"A culture advances by (0, {MaxAdvanceHours}] hours.");
        }

        if (populations.Count == 0)
        {
            throw new InvalidOperationException($"Culture {Id} has not been inoculated.");
        }

        Simulate(populations, hours);
        ElapsedHours += hours;
    }

    /// <summary>
    /// Records a measurement taken now (within <see cref="ObservationTimeToleranceHours"/>) and
    /// folds it into the estimated state of its species.
    /// </summary>
    public void Record(Observation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ThrowIfTerminated();
        if (!observation.CultureId.Equals(Id))
        {
            throw new ArgumentException($"The observation belongs to culture {observation.CultureId}, not {Id}.", nameof(observation));
        }

        if (Math.Abs(observation.ElapsedHours - ElapsedHours) > ObservationTimeToleranceHours)
        {
            throw new ArgumentException(
                $"The observation was taken at {observation.ElapsedHours} h; the culture is at {ElapsedHours} h.",
                nameof(observation));
        }

        MicrobialPopulation population = PopulationOf(observation.Species);
        population.Assimilate(observation);
        observations.Add(observation);
    }

    /// <summary>The current best estimate for a species: model state corrected by its observations.</summary>
    public EstimatedState Estimate(Species species)
    {
        MicrobialPopulation population = PopulationOf(species);
        int count = observations.Count(observation => observation.Species.Equals(species));
        return new EstimatedState(species, ElapsedHours, population.Concentration, population.RelativeUncertainty, population.Phase, count);
    }

    /// <summary>Forecasts a species' concentration <paramref name="horizonHours"/> ahead, without changing the culture.</summary>
    public Prediction Predict(Species species, double horizonHours)
    {
        PopulationOf(species);
        if (!double.IsFinite(horizonHours) || horizonHours <= 0 || horizonHours > Prediction.MaxHorizonHours)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonHours), horizonHours, $"A prediction looks (0, {Prediction.MaxHorizonHours}] hours ahead.");
        }

        List<MicrobialPopulation> copies = populations.Select(population => population.Copy()).ToList();
        Simulate(copies, horizonHours);
        MicrobialPopulation forecast = copies.Single(population => population.Species.Equals(species));
        return new Prediction(species, ElapsedHours, horizonHours, forecast.Concentration, forecast.RelativeUncertainty);
    }

    /// <summary>Generations a species has gone through since inoculation.</summary>
    public double GenerationsOf(Species species) => PopulationOf(species).GenerationsSince(inocula[species]);

    public void Terminate()
    {
        ThrowIfTerminated();
        IsTerminated = true;
    }

    private MicrobialPopulation PopulationOf(Species species)
    {
        ArgumentNullException.ThrowIfNull(species);
        return populations.SingleOrDefault(population => population.Species.Equals(species))
               ?? throw new InvalidOperationException($"{species} is not grown in culture {Id}.");
    }

    private void ThrowIfTerminated()
    {
        if (IsTerminated)
        {
            throw new InvalidOperationException($"Culture {Id} is terminated.");
        }
    }

    private void Simulate(List<MicrobialPopulation> state, double hours)
    {
        double remaining = hours;
        while (remaining > 1e-9)
        {
            double step = Math.Min(StepHours, remaining);
            double[] next = new double[state.Count];
            GrowthPhase[] phases = new GrowthPhase[state.Count];
            for (int i = 0; i < state.Count; i++)
            {
                MicrobialPopulation population = state[i];
                double cells = population.Concentration.CellsPerMillilitre;
                if (population.Species.TemperatureFactor(Conditions.TemperatureCelsius) == 0)
                {
                    next[i] = cells * Math.Exp(-DeathRatePerHour * step);
                    phases[i] = GrowthPhase.Death;
                    continue;
                }

                if (population.AgeHours < population.Species.LagHours)
                {
                    next[i] = cells;
                    phases[i] = GrowthPhase.Lag;
                    continue;
                }

                double load = cells;
                for (int j = 0; j < state.Count; j++)
                {
                    if (j != i)
                    {
                        load += InteractionOf(population.Species, state[j].Species) * state[j].Concentration.CellsPerMillilitre;
                    }
                }

                double relativeLoad = load / Conditions.CarryingCapacity(population.Species).CellsPerMillilitre;
                double rate = Conditions.EffectiveGrowthRate(population.Species).PerHour;
                double change = rate * cells * (1 - relativeLoad) * step;
                next[i] = cells + change;
                phases[i] = change < 0 ? GrowthPhase.Death : relativeLoad >= StationaryLoad ? GrowthPhase.Stationary : GrowthPhase.Exponential;
            }

            for (int i = 0; i < state.Count; i++)
            {
                state[i].Step(next[i], phases[i], step);
            }

            remaining -= step;
        }
    }
}
