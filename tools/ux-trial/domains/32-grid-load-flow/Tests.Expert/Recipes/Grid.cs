using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Expert.Recipes;

/// <summary>
///     Recipes for the parts of a network a test needs to exist but does not look at:
///     identities, names, voltage levels, operating bands, ratings. Electrical data a test is about
///     (impedances, loads, setpoints) is always passed in by the test, as a literal.
/// </summary>
internal static class Grid {

    /// <summary>The system base of every load-flow test: impedances are written per-unit on it.</summary>
    public const double BaseMva = 100.0;

    public static IReadOnlyList<BusId> AnyDistinctBusIds(int count) {
        return Any.ListOf(Any.BusId()).WithCount(count).Distinct().Generate();
    }

    /// <summary>Branch ids are unique across lines and transformers, so they are drawn together.</summary>
    public static IReadOnlyList<string> AnyDistinctBranchIds(int count) {
        return Any.ListOf(AnyLine.Ids()).WithCount(count).Distinct().Generate();
    }

    public static double AnyBaseKv() {
        return AnyBus.BaseKvs().Generate();
    }

    /// <summary>Two voltage levels, the higher first: what a transformer joins.</summary>
    public static (double HighKv, double LowKv) AnyTwoVoltageLevels() {
        List<double> levels = Any.ListOf(AnyBus.BaseKvs()).WithCount(2).Distinct().Generate();

        return (levels.Max(), levels.Min());
    }

    public static double AnyBaseMva() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(GridNetwork.MaximumBaseMva).Generate();
    }

    public static string AnyNetworkName() {
        return Any.String().NotBlank().WithMaxLength(64).Generate();
    }

    // ---- Buses: the type and the voltage level are what a topology needs; the rest is drawn.

    public static AnyBus AnySlack(BusId id, double baseKv) {
        return Any.Bus().WithId(id).WithType(BusType.Slack).WithBaseKv(baseKv);
    }

    public static AnyBus AnyLoadBus(BusId id, double baseKv) {
        return Any.Bus().WithId(id).WithType(BusType.Pq).WithBaseKv(baseKv);
    }

    public static AnyLine AnyLineBetween(string id, BusId from, BusId to) {
        return Any.Line().WithId(id).WithFromBus(from).WithToBus(to);
    }

    public static AnyTransformer AnyTransformerBetween(string id, BusId highVoltage, BusId lowVoltage) {
        return Any.Transformer().WithId(id).WithHighVoltageBus(highVoltage).WithLowVoltageBus(lowVoltage);
    }

    public static GridNetwork AnyNetwork(IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        return new GridNetwork(AnyNetworkName(), AnyBaseMva(), buses, lines, transformers ?? []);
    }

    // ---- Electrical recipes: the physics is pinned, only identity and bookkeeping are drawn.

    /// <summary>The reference: 1.0 pu at angle 0, no local load, nothing scheduled.</summary>
    public static Bus Reference(BusId id, double baseKv) {
        return AnySlack(id, baseKv).WithVoltageSetpointPu(1.0)
                                   .WithGeneration(PowerInjection.Zero)
                                   .WithLoad(PowerInjection.Zero)
                                   .Generate();
    }

    public static Bus Consumer(BusId id, double baseKv, PowerInjection load) {
        return AnyLoadBus(id, baseKv).WithGeneration(PowerInjection.Zero).WithLoad(load).Generate();
    }

    /// <summary>A plain series line (no charging): only its impedance takes part in the solution.</summary>
    public static Line Feeder(string id, BusId from, BusId to, Impedance impedance) {
        return AnyLineBetween(id, from, to).WithImpedance(impedance).WithChargingSusceptancePu(0.0).Generate();
    }

    public static GridNetwork Network(IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        return new GridNetwork(AnyNetworkName(), BaseMva, buses, lines, transformers ?? []);
    }

}
