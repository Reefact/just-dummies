namespace Aviation.Domain;

/// <summary>One operation of a flight number on a given day: the aggregate whose status moves through the day.</summary>
public sealed class Flight {

    private static readonly TimeSpan MaximumBlockTime = TimeSpan.FromHours(20);

    private readonly List<TimeSpan> _delays = new List<TimeSpan>();

    public Flight(FlightNumber number, Route route, Aircraft aircraft, DateTime scheduledDeparture, TimeSpan blockTime) {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(aircraft);
        if (blockTime <= TimeSpan.Zero || blockTime > MaximumBlockTime) {
            throw new ArgumentOutOfRangeException(nameof(blockTime), blockTime, "Block time is positive and at most twenty hours.");
        }

        Number             = number;
        Route              = route;
        Aircraft           = aircraft;
        ScheduledDeparture = scheduledDeparture;
        BlockTime          = blockTime;
        Status             = FlightStatus.Scheduled;
    }

    public FlightNumber Number { get; }

    public Route Route { get; }

    public Aircraft Aircraft { get; }

    public DateTime ScheduledDeparture { get; }

    public TimeSpan BlockTime { get; }

    public FlightStatus Status { get; private set; }

    public TimeSpan TotalDelay => _delays.Aggregate(TimeSpan.Zero, (total, delay) => total + delay);

    public DateTime EstimatedDeparture => ScheduledDeparture + TotalDelay;

    public DateTime EstimatedArrival => EstimatedDeparture + BlockTime;

    public DelayCategory DelayCategory => TotalDelay.TotalMinutes switch {
        < 15  => DelayCategory.OnTime,
        < 60  => DelayCategory.Minor,
        < 180 => DelayCategory.Significant,
        _     => DelayCategory.Major
    };

    public void Delay(TimeSpan by) {
        if (by <= TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(by), by, "A delay is positive.");
        }

        if (Status is not (FlightStatus.Scheduled or FlightStatus.Boarding)) {
            throw new InvalidOperationException($"A {Status} flight cannot be delayed.");
        }

        _delays.Add(by);
    }

    public void StartBoarding() => MoveTo(FlightStatus.Boarding, FlightStatus.Scheduled);

    public void Depart() => MoveTo(FlightStatus.Departed, FlightStatus.Boarding);

    public void Arrive() => MoveTo(FlightStatus.Arrived, FlightStatus.Departed);

    public void Cancel() {
        if (Status is FlightStatus.Departed or FlightStatus.Arrived) {
            throw new InvalidOperationException($"A {Status} flight cannot be cancelled.");
        }

        Status = FlightStatus.Cancelled;
    }

    private void MoveTo(FlightStatus target, FlightStatus expected) {
        if (Status != expected) {
            throw new InvalidOperationException($"Cannot move from {Status} to {target}.");
        }

        Status = target;
    }

}
