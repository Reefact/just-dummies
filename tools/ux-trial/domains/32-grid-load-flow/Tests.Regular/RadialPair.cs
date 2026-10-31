using System.Collections.Generic;
using System.Linq;

using GridLoadFlow.Domain;

namespace GridLoadFlow.Tests.Regular;

/// <summary>A slack bus feeding one load bus through one or more parallel lines.</summary>
internal sealed record RadialPair(GridNetwork Network, BusId SlackId, BusId LoadId, IReadOnlyList<string> LineIds) {

    public static RadialPair Build(int parallelLines, PowerInjection? load = null, double? ratingMva = null, double? baseMva = null, Impedance? impedance = null) {
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(parallelLines);

        AnyBus anyLoadBus = Grids.Load(anyIds[1], anyBaseKv);
        if (load is not null) {
            anyLoadBus = anyLoadBus.WithLoad(load);
        }

        List<Line> lines = anyLineIds.Select(id => {
                                         AnyLine anyLine = Grids.Line(id, anyIds[0], anyIds[1]);
                                         if (impedance is not null) {
                                             anyLine = anyLine.WithImpedance(impedance).WithChargingSusceptancePu(0.0);
                                         }

                                         return ratingMva is { } rating ? anyLine.WithRatingMva(rating).Generate() : anyLine.Generate();
                                     })
                                     .ToList();
        GridNetwork network = Grids.Network(baseMva ?? Grids.AnyBaseMva(), [Grids.Slack(anyIds[0], anyBaseKv).Generate(), anyLoadBus.Generate()], lines);

        return new RadialPair(network, anyIds[0], anyIds[1], anyLineIds);
    }

}
