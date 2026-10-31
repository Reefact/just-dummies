using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Expert.Arbitrary;

/// <summary>The small recipes every test file shares. Generators are immutable, so static fields are safe.</summary>
public static class Recipes {

    /// <summary>A control-table route name, e.g. R12 or R12A.</summary>
    public static readonly IAny<string> RouteName = Any.StringMatching("R[0-9]{1,3}[A-Z]?");

    /// <summary>An interlocking area, e.g. a station or a junction name.</summary>
    public static readonly IAny<string> AreaName = Any.String().Alpha().WithLengthBetween(3, 20);

    /// <summary>When a route was requested: any stamped instant.</summary>
    public static readonly IAny<DateTimeOffset> Instant = Any.DateTimeOffset().DifferentFrom(default);

    /// <summary>Two trains with different headcodes.</summary>
    public static (TrainId First, TrainId Second) TwoTrains() {
        List<TrainId> trains = Any.ListOf(Any.TrainId()).WithCount(2).Distinct().Generate();

        return (trains[0], trains[1]);
    }

}
