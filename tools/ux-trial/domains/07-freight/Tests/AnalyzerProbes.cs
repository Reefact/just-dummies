using Freight.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Freight.Tests;

/// <summary>Code an analyzer should (or could) flag; every test still passes at run time.</summary>
[Reproducible]
public sealed class AnalyzerProbes {

    // Expected JD009: drawn once for the whole suite, through a scaffolded generator.
    private static readonly Weight SharedWeight = new AnyWeight().Generate();

    // Control for JD009: the same draw through a library generator.
    private static readonly double SharedKilograms = Any.Double().Positive().Generate();

    // Expected JD007: drawn in a [Reproducible] class's constructor.
    private readonly Volume _volumeFromConstructor;
    private readonly double _cubicMetresFromConstructor;

    public AnalyzerProbes() {
        _volumeFromConstructor = new AnyVolume().Generate();
        _cubicMetresFromConstructor = Any.Double().Positive().Generate();   // control for JD007
    }

    [Fact]
    public void A_drawn_value_reaching_the_assertion() {
        // The golden rule broken: anyKilograms is in the assertion. Is anything reported?
        double anyKilograms = Any.Double().Positive().LessThanOrEqualTo(Weight.MaxGrossKilograms).Generate();

        Weight weight = new(anyKilograms);

        Assert.Equal(anyKilograms / 1_000, weight.MetricTons);
    }

    [Fact]
    public void Constant_contradictions_on_doubles_and_instants() {
        Assert.ThrowsAny<DummyException>(() => Any.Double().GreaterThan(100).LessThan(10));
        Assert.ThrowsAny<ArgumentException>(() => Any.Double().Between(76.4, 0));
        Assert.ThrowsAny<ArgumentException>(() => Any.TimeSpan().Between(TimeSpan.FromDays(90), TimeSpan.FromHours(1)));
    }

    [Fact]
    public void Constant_contradiction_on_integers() {
        Assert.ThrowsAny<DummyException>(() => Any.Int32().Between(1, 10).MultipleOf(50));
    }

    [Fact]
    public void Distinct_legs_without_value_equality() {
        List<Leg> legs = Any.ListOf(Arbitrary.Leg()).WithCount(3).Distinct().Generate();

        Assert.Equal(3, legs.Count);
    }

    [Fact]
    public void Distinct_legs_composed_with_Combine() {
        IAny<Leg> anyLeg = Any.Combine(new AnyUnLocode(), new AnyUnLocode(), Any.DateTimeOffset().Before(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)),
                                       (origin, destination, departure) => new Leg(origin, destination, departure, departure.AddDays(3)));
        List<Leg> legs = Any.ListOf(anyLeg).WithCount(3).Distinct().Generate();

        Assert.Equal(3, legs.Count);
    }

    [Fact]
    public void Distinct_legs_projected_with_As() {
        List<Leg> legs = Any.ListOf(Any.DateTimeOffset().Before(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero))
                                       .As(departure => new Leg(new UnLocode("FRLEH"), new UnLocode("NLRTM"), departure, departure.AddDays(3))))
                            .WithCount(3)
                            .Distinct()
                            .Generate();

        Assert.Equal(3, legs.Count);
    }

    [Fact]
    public void Scaffolded_values_held_in_fields_still_build() {
        Assert.True(SharedKilograms > 0);
        Assert.True(_cubicMetresFromConstructor > 0);
        Assert.True(SharedWeight.Kilograms > 0);
        Assert.True(_volumeFromConstructor.CubicMetres > 0);
    }

}
