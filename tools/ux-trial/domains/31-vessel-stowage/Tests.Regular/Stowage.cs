using JustDummies;
using JustDummies.Xunit;

using VesselStowage.Domain;

[assembly: Reproducible]

namespace VesselStowage.Tests.Regular;

/// <summary>Shared arrangements: a voyage over three arbitrary ports, a vessel whose limits do not bite.</summary>
internal static class Stowage {

    /// <summary>
    ///     A vessel large enough, and with stability margins wide enough, that a handful of boxes never
    ///     reaches a limit the test is not about. A test about a limit pins it.
    /// </summary>
    public static AnyVesselProfile AnyRoomyVessel() {
        return Arbitrary.VesselProfile()
                        .WithBayCount(b => b.GreaterThanOrEqualTo(10))
                        .WithRowCount(r => r.GreaterThanOrEqualTo(5))
                        .WithHoldTiers(t => t.GreaterThanOrEqualTo(4))
                        .WithDeckTiers(t => t.GreaterThanOrEqualTo(4))
                        .WithMaxStackWeightTonnes(w => w.GreaterThanOrEqualTo(200m))
                        .WithLightshipTonnes(l => l.GreaterThanOrEqualTo(20_000m))
                        .WithMaxVcgMetres(v => v.GreaterThanOrEqualTo(25))
                        .WithMaxTcgMetres(t => t.GreaterThanOrEqualTo(1));
    }

    /// <summary>A rotation of exactly three ports; which ports they are does not matter, their order does.</summary>
    public static Rotation AnyThreePortRotation() {
        return Arbitrary.Rotation().WithPorts(p => p.WithCount(3)).Generate();
    }

    /// <summary>General cargo (no dangerous goods) travelling between two ports of the rotation.</summary>
    public static AnyContainer AnyGeneralCargo(PortCode from, PortCode to) {
        return Arbitrary.Container()
                        .WithHazard(HazardClass.None)
                        .WithPortOfLoading(from)
                        .WithPortOfDischarge(to);
    }

    /// <summary>A bay (within any roomy vessel) that takes boxes of the given size.</summary>
    public static int BayFor(ContainerSize size) {
        return size == ContainerSize.TwentyFoot ? 1 : 2;
    }

}
