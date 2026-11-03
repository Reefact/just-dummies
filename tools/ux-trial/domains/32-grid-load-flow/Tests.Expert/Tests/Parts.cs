using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Domain.Tests.Expert;

/// <summary>The few building blocks several fixtures share: each fixes what its name says and draws the rest.</summary>
internal static class Parts {

    internal static List<BusId> AnyDistinctBusIds(int count) {
        return Any.ListOf(new AnyBusId()).WithCount(count).Distinct().Generate();
    }

    internal static double AnyBaseKv() {
        return Any.Double().Between(1d, Bus.MaximumBaseKv).Generate();
    }

    internal static Bus Slack(BusId id, double baseKv) {
        return Any.Bus().WithId(id).WithBaseKv(baseKv).WithType(BusType.Slack).Generate();
    }

    internal static Bus Load(BusId id, double baseKv) {
        return Any.Bus().WithId(id).WithBaseKv(baseKv).WithType(BusType.Pq).Generate();
    }

    internal static Line Between(BusId from, BusId to) {
        return Any.Line().WithFromBus(from).WithToBus(to).Generate();
    }

    internal static Line Between(string id, BusId from, BusId to) {
        return Any.Line().WithId(id).WithFromBus(from).WithToBus(to).Generate();
    }

    internal static GridNetwork Network(IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        string anyName = Any.String().NotBlank().Generate();
        double anyBaseMva = Any.Double().Between(1d, GridNetwork.MaximumBaseMva).Generate();

        return new GridNetwork(anyName, anyBaseMva, buses, lines, transformers ?? Array.Empty<Transformer>());
    }

}
