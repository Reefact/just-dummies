using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

public sealed partial class AnyWell {

    public AnyWell Injector() {
        return WithKind(WellKind.Injector);
    }

    public AnyWell Producer() {
        return WithKind(WellKind.Producer);
    }

}
