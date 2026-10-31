using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Discovery.Support;

/// <summary>Domain-level building blocks shared by the tests: what a bus or a line must be, never what a test asserts.</summary>
internal static class Grid {

    /// <summary>A base voltage a bus may carry: (0, 1000] kV.</summary>
    public static AnyDouble AnyBaseKv() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Bus.MaximumBaseKv);
    }

    /// <summary>Distinct bus ids, since a network refuses a bus that appears twice.</summary>
    public static BusId[] DistinctBusIds(int count) {
        return Any.SetOf(Any.BusId()).WithCount(count).Generate().ToArray();
    }

    /// <summary>Distinct branch ids, since lines and transformers share one id space.</summary>
    public static string[] DistinctBranchIds(int count) {
        return Any.SetOf(Any.String().NotBlank().WithMaxLength(32)).WithCount(count).Generate().ToArray();
    }

    public static Bus Slack(BusId id, double baseKv) {
        return Any.Bus().WithId(id).WithType(BusType.Slack).WithBaseKv(baseKv).WithLoad(PowerInjection.Zero).Generate();
    }

    /// <summary>A pure load bus. Its generation is pinned to zero: reactive generation would enter the solved injection.</summary>
    public static Bus Load(BusId id, double baseKv, PowerInjection load) {
        return Any.Bus().WithId(id).WithType(BusType.Pq).WithBaseKv(baseKv).WithGeneration(PowerInjection.Zero).WithLoad(load).Generate();
    }

    public static Line Line(string id, BusId from, BusId to) {
        return Any.Line().WithId(id).WithFromBus(from).WithToBus(to).Generate();
    }

    public static Line Line(string id, BusId from, BusId to, Impedance impedance) {
        return Any.Line().WithId(id).WithFromBus(from).WithToBus(to).WithImpedance(impedance).WithChargingSusceptancePu(0.0).Generate();
    }

}
