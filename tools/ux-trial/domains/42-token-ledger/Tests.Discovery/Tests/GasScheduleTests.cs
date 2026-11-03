using JustDummies;

using TokenLedger.Domain;

using Xunit;

namespace TokenLedger.Tests.Discovery;

public sealed class GasScheduleTests {

    private const ulong Intrinsic = GasSchedule.IntrinsicGas;
    private const ulong Block     = GasSchedule.BlockGasLimit;

    [Fact]
    public void The_standard_schedule_has_the_documented_costs() {
        GasSchedule standard = GasSchedule.Standard;

        Assert.Equal(35_000UL, standard.Transfer);
        Assert.Equal(30_000UL, standard.Approve);
        Assert.Equal(45_000UL, standard.TransferFrom);
        Assert.Equal(40_000UL, standard.Mint);
        Assert.Equal(32_000UL, standard.Burn);
        Assert.Equal(20_000UL, standard.NewHolderSurcharge);
    }

    [Fact]
    public void The_limits_are_the_documented_constants() {
        Assert.Equal(21_000UL, GasSchedule.IntrinsicGas);
        Assert.Equal(30_000_000UL, GasSchedule.BlockGasLimit);
        Assert.Equal(1_000_000UL, GasSchedule.MaxNewHolderSurcharge);
    }

    [Fact]
    public void A_schedule_keeps_its_six_costs() {
        GasSchedule schedule = new GasSchedule(30_000, 31_000, 32_000, 33_000, 34_000, 5_000);

        Assert.Equal(30_000UL, schedule.Transfer);
        Assert.Equal(31_000UL, schedule.Approve);
        Assert.Equal(32_000UL, schedule.TransferFrom);
        Assert.Equal(33_000UL, schedule.Mint);
        Assert.Equal(34_000UL, schedule.Burn);
        Assert.Equal(5_000UL, schedule.NewHolderSurcharge);
    }

    [Fact]
    public void A_drawn_schedule_respects_every_rule_of_the_constructor() {
        for (int i = 0; i < 100; i++) {
            GasSchedule anySchedule = Any.GasSchedule().Generate();

            Assert.True(anySchedule.TransferFrom >= anySchedule.Transfer);
            Assert.True(Math.Max(Math.Max(anySchedule.Transfer, anySchedule.TransferFrom), anySchedule.Mint) + anySchedule.NewHolderSurcharge <= Block);
        }
    }

    [Fact]
    public void An_operation_may_cost_exactly_the_intrinsic_gas_and_exactly_a_block() {
        GasSchedule cheapest = new GasSchedule(Intrinsic, Intrinsic, Intrinsic, Intrinsic, Intrinsic, 0);
        GasSchedule dearest  = new GasSchedule(Block, Block, Block, Block, Block, 0);

        Assert.Equal(Intrinsic, cheapest.Burn);
        Assert.Equal(Block, dearest.Burn);
    }

    [Fact]
    public void A_cost_below_the_intrinsic_gas_is_refused_for_each_operation() {
        // Arrange
        ulong anyTooCheap = Any.UInt64().LessThan(Intrinsic).Generate();

        // Act
        ArgumentOutOfRangeException transfer     = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(anyTooCheap, 30_000, 45_000, 40_000, 32_000, 0));
        ArgumentOutOfRangeException approve      = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, anyTooCheap, 45_000, 40_000, 32_000, 0));
        ArgumentOutOfRangeException transferFrom = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, anyTooCheap, 40_000, 32_000, 0));
        ArgumentOutOfRangeException mint         = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, anyTooCheap, 32_000, 0));
        ArgumentOutOfRangeException burn         = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, 40_000, anyTooCheap, 0));

        // Assert
        Assert.Equal("transfer", transfer.ParamName);
        Assert.Equal("approve", approve.ParamName);
        Assert.Equal("transferFrom", transferFrom.ParamName);
        Assert.Equal("mint", mint.ParamName);
        Assert.Equal("burn", burn.ParamName);
    }

    [Fact]
    public void A_cost_above_a_block_is_refused_for_each_operation() {
        ulong anyTooDear = Any.UInt64().GreaterThan(Block).Generate();

        Assert.Equal("transfer", Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(anyTooDear, 30_000, 45_000, 40_000, 32_000, 0)).ParamName);
        Assert.Equal("approve", Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, anyTooDear, 45_000, 40_000, 32_000, 0)).ParamName);
        Assert.Equal("transferFrom", Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, anyTooDear, 40_000, 32_000, 0)).ParamName);
        Assert.Equal("mint", Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, anyTooDear, 32_000, 0)).ParamName);
        Assert.Equal("burn", Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, 40_000, anyTooDear, 0)).ParamName);
    }

    [Fact]
    public void The_cost_just_outside_each_edge_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(Intrinsic - 1, 30_000, 45_000, 40_000, 32_000, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, 40_000, Block + 1, 0));
    }

    [Fact]
    public void A_surcharge_above_one_million_is_refused() {
        ulong anyTooHigh = Any.UInt64().GreaterThan(GasSchedule.MaxNewHolderSurcharge).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, anyTooHigh));

        Assert.Equal("newHolderSurcharge", refusal.ParamName);
    }

    [Fact]
    public void A_surcharge_of_zero_or_exactly_one_million_is_accepted() {
        Assert.Equal(0UL, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 0).NewHolderSurcharge);
        Assert.Equal(1_000_000UL, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 1_000_000).NewHolderSurcharge);
    }

    [Fact]
    public void A_delegated_transfer_cannot_cost_less_than_a_plain_one_but_may_cost_the_same() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GasSchedule(40_000, 30_000, 39_999, 40_000, 32_000, 0));

        Assert.Equal(typeof(ArgumentException), refusal.GetType());
        Assert.Equal("transferFrom", refusal.ParamName);
        Assert.Equal(40_000UL, new GasSchedule(40_000, 30_000, 40_000, 40_000, 32_000, 0).TransferFrom);
    }

    [Theory]
    [InlineData(29_999_999UL, 29_999_999UL, 35_000UL, 2UL)]
    [InlineData(35_000UL, 29_999_999UL, 35_000UL, 2UL)]
    [InlineData(35_000UL, 35_000UL, 29_999_999UL, 2UL)]
    [InlineData(35_000UL, 35_000UL, 30_000_000UL, 1UL)]
    public void A_credit_to_a_new_holder_must_fit_in_a_block(ulong transfer, ulong transferFrom, ulong mint, ulong surcharge) {
        // The dearest crediting operation plus the surcharge is one gas past the block: refused.
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GasSchedule(transfer, 30_000, transferFrom, mint, 32_000, surcharge));

        Assert.Equal("newHolderSurcharge", refusal.ParamName);
    }

    [Fact]
    public void A_crediting_operation_plus_the_surcharge_may_reach_a_block_exactly() {
        GasSchedule schedule = new GasSchedule(35_000, 30_000, 45_000, Block - 1_000, 32_000, 1_000);

        Assert.Equal(Block - 1_000, schedule.Mint);
    }

    [Fact]
    public void Burning_and_approving_are_not_part_of_the_crediting_rule() {
        GasSchedule schedule = new GasSchedule(35_000, Block, 45_000, 40_000, Block, 1_000_000);

        Assert.Equal(Block, schedule.Approve);
        Assert.Equal(Block, schedule.Burn);
    }

    [Theory]
    [InlineData(TransactionKind.Transfer, false, 35_000UL)]
    [InlineData(TransactionKind.Transfer, true, 55_000UL)]
    [InlineData(TransactionKind.Approve, false, 30_000UL)]
    [InlineData(TransactionKind.Approve, true, 30_000UL)]
    [InlineData(TransactionKind.IncreaseAllowance, false, 30_000UL)]
    [InlineData(TransactionKind.IncreaseAllowance, true, 30_000UL)]
    [InlineData(TransactionKind.DecreaseAllowance, false, 30_000UL)]
    [InlineData(TransactionKind.DecreaseAllowance, true, 30_000UL)]
    [InlineData(TransactionKind.TransferFrom, false, 45_000UL)]
    [InlineData(TransactionKind.TransferFrom, true, 65_000UL)]
    [InlineData(TransactionKind.Mint, false, 40_000UL)]
    [InlineData(TransactionKind.Mint, true, 60_000UL)]
    [InlineData(TransactionKind.Burn, false, 32_000UL)]
    [InlineData(TransactionKind.Burn, true, 32_000UL)]
    public void The_gas_of_a_transaction_depends_on_its_kind_and_on_whether_it_creates_a_holder(TransactionKind kind, bool createsHolder, ulong expected) {
        Assert.Equal(expected, GasSchedule.Standard.GasFor(kind, createsHolder));
    }

    [Fact]
    public void The_three_allowance_kinds_cost_the_approve_price_of_any_schedule() {
        GasSchedule anySchedule = Any.GasSchedule().Generate();

        Assert.Equal(anySchedule.Approve, anySchedule.GasFor(TransactionKind.Approve, false));
        Assert.Equal(anySchedule.Approve, anySchedule.GasFor(TransactionKind.IncreaseAllowance, true));
        Assert.Equal(anySchedule.Approve, anySchedule.GasFor(TransactionKind.DecreaseAllowance, true));
    }

    [Fact]
    public void An_undefined_kind_is_refused() {
        GasSchedule anySchedule = Any.GasSchedule().Generate();
        bool        anyFlag     = Any.Boolean().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anySchedule.GasFor((TransactionKind)99, anyFlag));

        Assert.Equal("kind", refusal.ParamName);
    }

    [Fact]
    public void Two_schedules_with_the_same_costs_are_equal_and_hash_alike() {
        GasSchedule first  = new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);
        GasSchedule second = new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);

        Assert.Equal(first, second);
        Assert.Equal(GasSchedule.Standard, first);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_schedule_differs_when_any_one_cost_differs() {
        GasSchedule baseline = GasSchedule.Standard;

        Assert.NotEqual(baseline, new GasSchedule(35_001, 30_000, 45_000, 40_000, 32_000, 20_000));
        Assert.NotEqual(baseline, new GasSchedule(35_000, 30_001, 45_000, 40_000, 32_000, 20_000));
        Assert.NotEqual(baseline, new GasSchedule(35_000, 30_000, 45_001, 40_000, 32_000, 20_000));
        Assert.NotEqual(baseline, new GasSchedule(35_000, 30_000, 45_000, 40_001, 32_000, 20_000));
        Assert.NotEqual(baseline, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_001, 20_000));
        Assert.NotEqual(baseline, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_001));
        Assert.False(baseline.Equals(null));
    }

    [Fact]
    public void A_schedule_describes_itself() {
        Assert.Equal("transfer 35000, approve 30000, transferFrom 45000, mint 40000, burn 32000, new holder +20000", GasSchedule.Standard.ToString());
    }

}
