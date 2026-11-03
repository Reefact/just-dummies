using JustDummies;

using PassiveComponents.Domain;

using Xunit;

namespace PassiveComponents.Tests.Discovery;

public sealed class GeneratorSmokeTests {

    // The generators are the foundation of every other test here: if one draws a value its own constructor
    // refuses, the failure belongs to the generator, not to the domain.
    [Fact]
    public void Every_generator_draws_values_its_constructor_accepts_over_many_draws() {
        for (int i = 0; i < 500; i++) {
            Any.Capacitance().Generate();
            Any.Inductance().Generate();
            Any.Resistance().Generate();
            Any.Tolerance().Generate();
            Any.TemperatureCoefficient().Generate();
            Any.PreferredValue().Generate();
            Any.MarkingCode().Generate();
            Any.ReferenceDesignator().Generate();
            Any.Part().Generate();
            Any.Placement().Generate();
            Any.BomLine().Generate();
            Any.BillOfMaterials().Generate();
        }
    }

}
