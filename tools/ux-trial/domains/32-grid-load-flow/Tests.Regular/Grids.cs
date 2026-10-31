using System.Collections.Generic;
using System.Linq;

using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     Ordinary, solvable grids for the tests: moderate loads on a system base of a few hundred MVA,
///     transmission-like impedances and voltage bands around nominal. Nothing here is what a test
///     asserts on; a test pins what it cares about.
/// </summary>
internal static class Grids {

    public static AnyVoltageLimits OrdinaryLimits(AnyVoltageLimits limits) {
        return limits.WithMinimumPu(m => m.Between(0.85, 0.95)).WithMaximumPu(m => m.Between(1.05, 1.15));
    }

    public static AnyImpedance OrdinaryImpedance(AnyImpedance impedance) {
        return impedance.WithResistancePu(r => r.Between(0.005, 0.03)).WithReactancePu(x => x.Between(0.05, 0.15));
    }

    public static AnyPowerInjection ModerateLoad(AnyPowerInjection load) {
        return load.WithActiveMw(p => p.Between(5.0, 50.0)).WithReactiveMvar(q => q.Between(0.0, 20.0));
    }

    public static double AnyBaseMva() {
        return Any.Double().Between(100.0, 1_000.0).Generate();
    }

    public static double AnyBaseKv() {
        return Any.Double().Between(10.0, 400.0).Generate();
    }

    public static List<BusId> AnyDistinctBusIds(int count) {
        return Any.SetOf(Arbitrary.BusId()).WithCount(count).Generate().ToList();
    }

    public static List<string> AnyDistinctBranchIds(int count) {
        return Any.SetOf(Any.String().NotBlank().WithMaxLength(32)).WithCount(count).Generate().ToList();
    }

    public static string AnyNetworkName() {
        return Any.String().NotBlank().WithMaxLength(40).Generate();
    }

    public static AnyBus Slack(BusId id, double baseKv) {
        // The slack's own generation is solved, not scheduled: zero is what a case file carries for it.
        return Arbitrary.Bus()
                        .WithId(id)
                        .WithType(BusType.Slack)
                        .WithBaseKv(baseKv)
                        .WithGeneration(PowerInjection.Zero)
                        .WithLoad(ModerateLoad)
                        .WithLimits(OrdinaryLimits);
    }

    public static AnyBus Load(BusId id, double baseKv) {
        return Arbitrary.Bus()
                        .WithId(id)
                        .WithType(BusType.Pq)
                        .WithBaseKv(baseKv)
                        .WithGeneration(PowerInjection.Zero)
                        .WithLoad(ModerateLoad)
                        .WithLimits(OrdinaryLimits);
    }

    public static AnyLine Line(string id, BusId from, BusId to) {
        return Arbitrary.Line()
                        .WithId(id)
                        .WithFromBus(from)
                        .WithToBus(to)
                        .WithImpedance(OrdinaryImpedance)
                        .WithChargingSusceptancePu(b => b.Between(0.0, 0.05));
    }

    public static GridNetwork Network(double baseMva, IEnumerable<Bus> buses, IEnumerable<Line> lines, IEnumerable<Transformer>? transformers = null) {
        return new GridNetwork(AnyNetworkName(), baseMva, buses, lines, transformers ?? []);
    }

}
