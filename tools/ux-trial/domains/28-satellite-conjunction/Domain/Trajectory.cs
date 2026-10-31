namespace SatelliteConjunction.Domain;

/// <summary>The predicted ephemeris of one object: at least two states, in strictly increasing epoch order.</summary>
public sealed class Trajectory
{
    public static readonly TimeSpan MaximumStep = TimeSpan.FromMinutes(10);

    private readonly List<OrbitalState> states;

    public Trajectory(SpaceObject spaceObject, IEnumerable<OrbitalState> states)
    {
        ArgumentNullException.ThrowIfNull(spaceObject);
        ArgumentNullException.ThrowIfNull(states);

        List<OrbitalState> ordered = states.ToList();
        if (ordered.Count < 2)
        {
            throw new ArgumentException("A trajectory needs at least two states to be interpolated.", nameof(states));
        }

        for (int index = 0; index < ordered.Count; index++)
        {
            if (ordered[index] is null)
            {
                throw new ArgumentException($"State {index} is null.", nameof(states));
            }

            if (index == 0)
            {
                continue;
            }

            double step = ordered[index].Epoch.SecondsSince(ordered[index - 1].Epoch);
            if (step <= 0)
            {
                throw new ArgumentException($"State {index} does not follow state {index - 1}: epochs must strictly increase.", nameof(states));
            }

            if (step > MaximumStep.TotalSeconds)
            {
                throw new ArgumentException($"States {index - 1} and {index} are more than {MaximumStep.TotalMinutes} minutes apart.", nameof(states));
            }
        }

        SpaceObject = spaceObject;
        this.states = ordered;
    }

    public SpaceObject SpaceObject { get; }

    public IReadOnlyList<OrbitalState> States => states;

    public Epoch Start => states[0].Epoch;

    public Epoch End => states[^1].Epoch;

    public bool Covers(Epoch epoch) => epoch.CompareTo(Start) >= 0 && epoch.CompareTo(End) <= 0;

    /// <summary>Linear interpolation of position and velocity; the covariance of the nearer node is kept.</summary>
    public OrbitalState StateAt(Epoch epoch)
    {
        ArgumentNullException.ThrowIfNull(epoch);
        if (!Covers(epoch))
        {
            throw new ArgumentOutOfRangeException(nameof(epoch), epoch, $"The trajectory of {SpaceObject} only covers {Start} to {End}.");
        }

        int after = 1;
        while (states[after].Epoch.CompareTo(epoch) < 0)
        {
            after++;
        }

        OrbitalState before = states[after - 1];
        OrbitalState next = states[after];
        double fraction = epoch.SecondsSince(before.Epoch) / next.Epoch.SecondsSince(before.Epoch);
        Vector3 position = before.PositionKm.Plus(next.PositionKm.Minus(before.PositionKm).Scale(fraction));
        Vector3 velocity = before.VelocityKmPerSecond.Plus(next.VelocityKmPerSecond.Minus(before.VelocityKmPerSecond).Scale(fraction));
        PositionCovariance covariance = fraction < 0.5 ? before.Covariance : next.Covariance;
        return new OrbitalState(epoch, position, velocity, covariance);
    }
}
