namespace Aviation.Domain;

/// <summary>A registered airframe of a type taken from the operator's fleet reference list.</summary>
public sealed class Aircraft {

    private static readonly Dictionary<string, int> MaximumSeatsByType = new Dictionary<string, int>(StringComparer.Ordinal) {
        ["A320"] = 186,
        ["A321"] = 230,
        ["B738"] = 189,
        ["B77W"] = 396,
        ["E190"] = 114,
        ["AT76"] = 78
    };

    public Aircraft(AircraftRegistration registration, string typeDesignator, int seats) {
        ArgumentNullException.ThrowIfNull(registration);
        if (!MaximumSeatsByType.TryGetValue(typeDesignator, out int maximumSeats)) {
            throw new ArgumentException($"'{typeDesignator}' is not in the fleet reference list.", nameof(typeDesignator));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seats);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(seats, maximumSeats);

        Registration   = registration;
        TypeDesignator = typeDesignator;
        Seats          = seats;
    }

    public static IReadOnlyCollection<string> KnownTypes => MaximumSeatsByType.Keys;

    public AircraftRegistration Registration { get; }

    public string TypeDesignator { get; }

    public int Seats { get; }

    public bool IsWideBody => TypeDesignator == "B77W";

}
