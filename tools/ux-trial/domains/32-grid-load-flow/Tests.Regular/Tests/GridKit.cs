using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Regular;

/// <summary>
///     The parts of one small network: distinct bus identifiers, distinct branch identifiers and a common base
///     voltage are dummies; what the load flow depends on (loads, setpoints, impedances, taps) is passed in as literals.
/// </summary>
internal sealed class GridKit {

    private readonly IReadOnlyList<BusId>  _ids;
    private readonly IReadOnlyList<string> _branchIds;

    private GridKit(IReadOnlyList<BusId> ids, IReadOnlyList<string> branchIds, double baseKv) {
        _ids       = ids;
        _branchIds = branchIds;
        BaseKv     = baseKv;
    }

    public double BaseKv { get; }

    public static GridKit With(int buses, int branches) {
        List<BusId>  anyIds       = Any.ListOf(Any.BusId()).WithCount(buses).Distinct().Generate();
        List<string> anyBranchIds = Any.ListOf(Any.String().NotBlank().WithMaxLength(32)).WithCount(branches).Distinct().Generate();
        double       anyBaseKv    = Any.Double().Between(1.0, 400.0).Generate();

        return new GridKit(anyIds, anyBranchIds, anyBaseKv);
    }

    public BusId Id(int bus) {
        return _ids[bus];
    }

    public string BranchId(int branch) {
        return _branchIds[branch];
    }

    public Bus Slack(int bus, double setpointPu = 1.0) {
        return Any.Bus().WithId(_ids[bus]).WithType(BusType.Slack).WithBaseKv(BaseKv)
                  .WithVoltageSetpointPu(setpointPu).WithLoad(PowerInjection.Zero).Generate();
    }

    public Bus Load(int bus, double activeMw, double reactiveMvar, double? baseKv = null) {
        return Any.Bus().WithId(_ids[bus]).WithType(BusType.Pq).WithBaseKv(baseKv ?? BaseKv)
                  .WithGeneration(PowerInjection.Zero).WithLoad(new PowerInjection(activeMw, reactiveMvar)).Generate();
    }

    public Bus Generator(int bus, double activeMw, double setpointPu) {
        return Any.Bus().WithId(_ids[bus]).WithType(BusType.Pv).WithBaseKv(BaseKv)
                  .WithGeneration(new PowerInjection(activeMw, 0.0)).WithLoad(PowerInjection.Zero).WithVoltageSetpointPu(setpointPu).Generate();
    }

    public Line Line(int branch, int from, int to, double resistancePu, double reactancePu, double chargingSusceptancePu = 0.0) {
        return Any.Line().WithId(_branchIds[branch]).WithFromBus(_ids[from]).WithToBus(_ids[to])
                  .WithImpedance(new Impedance(resistancePu, reactancePu)).WithChargingSusceptancePu(chargingSusceptancePu).Generate();
    }

    public Transformer Transformer(int branch, int highVoltageBus, int lowVoltageBus, double resistancePu, double reactancePu, double tapRatio) {
        return Any.Transformer().WithId(_branchIds[branch]).WithHighVoltageBus(_ids[highVoltageBus]).WithLowVoltageBus(_ids[lowVoltageBus])
                  .WithImpedance(new Impedance(resistancePu, reactancePu)).WithTapRatio(tapRatio).Generate();
    }

}
