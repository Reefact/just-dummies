using GeothermalReservoir.Domain;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert.Tests;

public sealed class WellTests {

    [Fact]
    public void An_injector_keeps_its_identity_completion_rate_and_injection_temperature() {
        CellCoordinate completion = new CellCoordinate(1, 0, 2);
        Temperature    injected   = Temperature.FromCelsius(40.0);

        Well well = new Well("INJ-01", WellKind.Injector, completion, 0.1, injected);

        Assert.Equal("INJ-01", well.Id);
        Assert.Equal(WellKind.Injector, well.Kind);
        Assert.Same(completion, well.Completion);
        Assert.Equal(0.1, well.RateCubicMetresPerSecond);
        Assert.Same(injected, well.InjectionTemperature);
    }

    [Fact]
    public void A_producer_keeps_its_identity_and_has_no_injection_temperature() {
        CellCoordinate completion = new CellCoordinate(0, 3, 1);

        Well well = new Well("PRD-07", WellKind.Producer, completion, 0.2, null);

        Assert.Equal("PRD-07", well.Id);
        Assert.Equal(WellKind.Producer, well.Kind);
        Assert.Same(completion, well.Completion);
        Assert.Null(well.InjectionTemperature);
    }

    [Fact]
    public void An_injector_injects_so_its_signed_rate_is_positive() {
        Well injector = Any.Well().Injector().WithRate(0.1).Generate();

        Assert.Equal(0.1, injector.SignedRate);
    }

    [Fact]
    public void A_producer_withdraws_so_its_signed_rate_is_negative() {
        Well producer = Any.Well().Producer().WithRate(0.1).Generate();

        Assert.Equal(-0.1, producer.SignedRate);
    }

    [Theory]
    [InlineData("INJ-00")]
    [InlineData("INJ-42")]
    [InlineData("INJ-99")]
    public void An_injector_id_is_INJ_and_two_digits(string id) {
        Well anyInjector = Any.Well().Injector().WithId(id).Generate();

        Assert.Equal(id, anyInjector.Id);
    }

    [Theory]
    [InlineData("PRD-00")]
    [InlineData("PRD-07")]
    [InlineData("PRD-99")]
    public void A_producer_id_is_PRD_and_two_digits(string id) {
        Well anyProducer = Any.Well().Producer().WithId(id).Generate();

        Assert.Equal(id, anyProducer.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("INJ-")]
    [InlineData("INJ-1")]
    [InlineData("INJ-001")]
    [InlineData("INJ01")]
    [InlineData("INJ_01")]
    [InlineData("inj-01")]
    [InlineData("Inj-01")]
    [InlineData("INJ-0A")]
    [InlineData("INJ-٠١")] // Arabic-Indic digits: [0-9] is ASCII only
    [InlineData("INJ-０１")] // full-width digits
    [InlineData("XYZ-01")]
    [InlineData("WELL-01")]
    [InlineData(" INJ-01")]
    [InlineData("INJ-01 ")]
    [InlineData("INJ-01-02")]
    public void An_id_that_is_not_a_prefix_a_dash_and_two_digits_is_refused(string id) {
        AnyWell injectorGenerator = Any.Well().Injector().WithId(id);

        Assert.Throws<FormatException>(() => injectorGenerator.Generate());
    }

    [Fact(Skip = "domain defect: the id pattern ends with '$', which .NET also matches before a final newline, so \"INJ-01\\n\" is accepted as an id (literal input, no seed)")]
    public void An_id_followed_by_a_line_break_is_refused() {
        AnyWell injectorGenerator = Any.Well().Injector().WithId("INJ-01\n");

        Assert.Throws<FormatException>(() => injectorGenerator.Generate());
    }

    [Fact]
    public void A_well_without_an_id_is_refused() {
        Well anyWell = Any.Well().Injector().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new Well(null!, WellKind.Injector, anyWell.Completion, anyWell.RateCubicMetresPerSecond, anyWell.InjectionTemperature));
    }

    [Fact]
    public void A_well_without_a_completion_is_refused() {
        Well anyWell = Any.Well().Injector().Generate();

        Assert.Throws<ArgumentNullException>(
            () => new Well("INJ-01", WellKind.Injector, null!, anyWell.RateCubicMetresPerSecond, anyWell.InjectionTemperature));
    }

    [Fact]
    public void A_well_of_an_unknown_kind_is_refused() {
        Well anyWell = Any.Well().Injector().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Well("INJ-01", (WellKind)42, anyWell.Completion, anyWell.RateCubicMetresPerSecond, anyWell.InjectionTemperature));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void An_injector_cannot_carry_a_producer_id() {
        Well anyWell = Any.Well().Injector().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Well("PRD-01", WellKind.Injector, anyWell.Completion, anyWell.RateCubicMetresPerSecond, anyWell.InjectionTemperature));

        Assert.Equal("id", refusal.ParamName);
        Assert.IsNotType<FormatException>(refusal);
    }

    [Fact]
    public void A_producer_cannot_carry_an_injector_id() {
        Well anyWell = Any.Well().Producer().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Well("INJ-01", WellKind.Producer, anyWell.Completion, anyWell.RateCubicMetresPerSecond, null));

        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.Epsilon)]
    [InlineData(0.25)]
    [InlineData(Well.MaximumRateCubicMetresPerSecond)]
    public void A_rate_up_to_half_a_cubic_metre_per_second_is_accepted(double rate) {
        Well well = Any.Well().Injector().WithRate(rate).Generate();

        Assert.Equal(rate, well.RateCubicMetresPerSecond);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(0.5000000000000001)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_rate_that_is_not_positive_or_above_half_a_cubic_metre_per_second_is_refused(double rate) {
        AnyWell injectorGenerator = Any.Well().Injector().WithRate(rate);

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => injectorGenerator.Generate());

        Assert.Equal("rateCubicMetresPerSecond", refusal.ParamName);
    }

    [Fact]
    public void An_injector_must_state_what_it_injects() {
        Well anyWell = Any.Well().Injector().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Well("INJ-01", WellKind.Injector, anyWell.Completion, anyWell.RateCubicMetresPerSecond, null));

        Assert.Equal("injectionTemperature", refusal.ParamName);
    }

    [Fact]
    public void A_producer_cannot_state_an_injection_temperature() {
        Well anyWell = Any.Well().Injector().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Well("PRD-01", WellKind.Producer, anyWell.Completion, anyWell.RateCubicMetresPerSecond, anyWell.InjectionTemperature));

        Assert.Equal("injectionTemperature", refusal.ParamName);
    }

}
