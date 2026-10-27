using Aviation.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Aviation.Tests;

/// <summary>Code an analyzer should catch, kept compiling so the diagnostics show in the build output.</summary>
public sealed class AnalyzerProbes {

    [Fact]
    public void Analyzer_probe_a_drawn_value_flowing_into_the_assertion() {
        string anyCode = Any.String().Alpha().InUpperCase().WithLength(3).Generate();

        IataAirportCode code = new IataAirportCode(anyCode);

        Assert.Equal(anyCode, code.Value);   // an `any` value in the assertion: a property, sample size one
    }

    [Fact]
    public void Analyzer_probe_a_constant_contradiction_on_a_flight_number() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 9999).GreaterThan(10_000).Generate());
    }

    [Fact]
    public void Analyzer_probe_a_constant_contradiction_on_a_TimeSpan() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeSpan().Positive().Negative().Generate());
    }

    [Fact]
    public void Analyzer_probe_a_designator_too_long_for_its_prefix() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.String().StartingWith("AF").WithLength(1).Generate());
    }

    [Fact]
    public void Analyzer_probe_an_enum_pool_with_a_duplicate() {
        FlightStatus anyOpen = Any.Enum<FlightStatus>().OneOf(FlightStatus.Scheduled, FlightStatus.Boarding, FlightStatus.Scheduled).Generate();

        Assert.NotEqual(FlightStatus.Arrived, anyOpen);
    }

    [Fact]
    public void Analyzer_probe_an_unused_combine_operand() {
        IAny<string> anyDesignator = Any.Combine(Any.String().AlphaNumeric().InUpperCase().WithLength(2), Any.Int32().Between(1, 9999), (airline, number) => airline);

        Assert.Equal(2, anyDesignator.Generate().Length);
    }

    [Fact]
    [Reproducible(Seed = 1234)]
    public void Analyzer_probe_a_committed_seed() {
        Assert.NotNull(Any.FlightNumber().Generate());
    }

    private static readonly FlightNumber DrawnAtTypeLoad = Any.FlightNumber().Generate();

    [Fact]
    public void Analyzer_probe_a_draw_in_a_static_initializer() {
        Assert.NotNull(DrawnAtTypeLoad);
    }

}
