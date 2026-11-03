using JustDummies;
using VesselStowage.Domain.Tests.Generators;

namespace VesselStowage.Domain.Tests;

/// <summary>
///     The itinerary a plan test works on: four distinct ports called at in a known order. Which ports they are
///     never matters; that the first is called before the second, and so on, often does.
/// </summary>
internal sealed class Voyage {

    private Voyage(Rotation rotation) {
        Rotation = rotation;
    }

    public Rotation Rotation { get; }

    public PortCode FirstCall => Rotation.Ports[0];

    public PortCode SecondCall => Rotation.Ports[1];

    public PortCode ThirdCall => Rotation.Ports[2];

    public PortCode FourthCall => Rotation.Ports[3];

    /// <summary>A port the vessel does not call at.</summary>
    public PortCode Elsewhere() {
        return Any.PortCode().DifferentFrom(FirstCall).DifferentFrom(SecondCall).DifferentFrom(ThirdCall).DifferentFrom(FourthCall).Generate();
    }

    public static Voyage Draw() {
        return new Voyage(Any.Rotation().WithPorts(ports => ports.WithCount(4)).Generate());
    }

    /// <summary>An empty plan of a roomy vessel along this voyage.</summary>
    public StowagePlan EmptyPlan() {
        return EmptyPlanOf(Any.VesselProfile().Roomy());
    }

    public StowagePlan EmptyPlanOf(AnyVesselProfile vessel) {
        return new StowagePlan(vessel.Generate(), Rotation);
    }

    /// <summary>
    ///     A general-cargo twenty-foot box sailing from the first call to the second. Segregation, the size/bay
    ///     match and the route are not what the tests using it are about, unless they say otherwise.
    /// </summary>
    public AnyContainer Box() {
        return Any.Container().GeneralCargo().OfTwentyFeet().Sailing(FirstCall, SecondCall);
    }

}
