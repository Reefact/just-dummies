using ParimutuelBetting.Domain.Tests.Expert.Generators;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class StakeTests {

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    public void A_ticket_carries_from_one_to_ten_thousand_units(int units) {
        Stake stake = Any.Stake().WithUnits(units).Generate();

        Assert.Equal(units, stake.Units);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void A_ticket_with_no_unit_or_more_than_ten_thousand_is_refused(int units) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Stake().WithUnits(units).Generate());

        Assert.Equal("units", refusal.ParamName);
    }

    [Fact]
    public void A_ticket_needs_a_combination() {
        int anyUnits = Any.Int32().Between(1, Stake.MaximumUnits).Generate();

        Assert.Throws<ArgumentNullException>(() => new Stake(null!, anyUnits));
    }

}
