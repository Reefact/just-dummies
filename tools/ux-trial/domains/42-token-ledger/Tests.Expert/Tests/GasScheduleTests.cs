using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

public sealed class GasScheduleTests {

    private const ulong BlockLimit = GasSchedule.BlockGasLimit;

    // ---- the standard schedule ------------------------------------------------------------------------------------

    [Fact]
    public void The_standard_schedule_prices_each_operation_as_documented() {
        // Act
        GasSchedule standard = GasSchedule.Standard;

        // Assert
        Assert.Equal(35_000UL, standard.Transfer);
        Assert.Equal(30_000UL, standard.Approve);
        Assert.Equal(45_000UL, standard.TransferFrom);
        Assert.Equal(40_000UL, standard.Mint);
        Assert.Equal(32_000UL, standard.Burn);
        Assert.Equal(20_000UL, standard.NewHolderSurcharge);
    }

    [Fact]
    public void The_limits_are_those_of_a_block_and_of_a_bare_transaction() {
        Assert.Equal(21_000UL, GasSchedule.IntrinsicGas);
        Assert.Equal(30_000_000UL, GasSchedule.BlockGasLimit);
        Assert.Equal(1_000_000UL, GasSchedule.MaxNewHolderSurcharge);
    }

    // ---- each base cost lies between the intrinsic gas and the block limit ---------------------------------------

    // The standard costs, any of which a test overrides by name: the one value it names is the one it is about.
    private static GasSchedule Schedule(ulong transfer = 35_000, ulong approve = 30_000, ulong transferFrom = 45_000, ulong mint = 40_000, ulong burn = 32_000, ulong surcharge = 0) {
        return new GasSchedule(transfer, approve, transferFrom, mint, burn, surcharge);
    }

    [Fact]
    public void A_transfer_cost_is_bounded_by_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(21_000UL, Schedule(transfer: 21_000).Transfer);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(transfer: 20_999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(transfer: BlockLimit + 1, transferFrom: BlockLimit + 1));
    }

    [Fact]
    public void An_approve_cost_is_bounded_by_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(21_000UL, Schedule(approve: 21_000).Approve);
        Assert.Equal(BlockLimit, Schedule(approve: BlockLimit).Approve);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(approve: 20_999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(approve: BlockLimit + 1));
    }

    [Fact]
    public void A_transfer_from_cost_is_bounded_by_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(BlockLimit, Schedule(transferFrom: BlockLimit).TransferFrom);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(transfer: 21_000, transferFrom: 20_999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(transferFrom: BlockLimit + 1));
    }

    [Fact]
    public void A_mint_cost_is_bounded_by_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(21_000UL, Schedule(mint: 21_000).Mint);
        Assert.Equal(BlockLimit, Schedule(mint: BlockLimit).Mint);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(mint: 20_999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(mint: BlockLimit + 1));
    }

    [Fact]
    public void A_burn_cost_is_bounded_by_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(21_000UL, Schedule(burn: 21_000).Burn);
        Assert.Equal(BlockLimit, Schedule(burn: BlockLimit).Burn);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(burn: 20_999));
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(burn: BlockLimit + 1));
    }

    // ---- the surcharge --------------------------------------------------------------------------------------------

    [Fact]
    public void The_new_holder_surcharge_may_be_zero_or_one_million_and_no_more() {
        Assert.Equal(0UL, Schedule(surcharge: 0).NewHolderSurcharge);
        Assert.Equal(1_000_000UL, Schedule(surcharge: 1_000_000).NewHolderSurcharge);
        Assert.Throws<ArgumentOutOfRangeException>(() => Schedule(surcharge: 1_000_001));
    }

    // ---- ties between the costs -----------------------------------------------------------------------------------

    [Fact]
    public void A_delegated_transfer_costs_at_least_a_plain_transfer() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => Schedule(transfer: 40_000, transferFrom: 39_999));
    }

    [Fact]
    public void A_delegated_transfer_may_cost_exactly_a_plain_transfer() {
        Assert.Equal(40_000UL, Schedule(transfer: 40_000, transferFrom: 40_000).TransferFrom);
    }

    [Theory]
    [InlineData(29_000_001UL, 29_000_001UL, 40_000UL)]
    [InlineData(35_000UL, 29_000_001UL, 29_000_001UL)]
    [InlineData(35_000UL, 35_000UL, 29_000_001UL)]
    public void The_dearest_of_transfer_transfer_from_and_mint_plus_the_surcharge_must_fit_in_a_block(ulong transfer, ulong transferFrom, ulong mint) {
        Assert.Throws<ArgumentException>(() => Schedule(transfer: transfer, transferFrom: transferFrom, mint: mint, surcharge: 1_000_000));
    }

    [Theory]
    [InlineData(29_000_000UL, 29_000_000UL, 40_000UL)]
    [InlineData(35_000UL, 29_000_000UL, 29_000_000UL)]
    [InlineData(35_000UL, 35_000UL, 29_000_000UL)]
    public void The_dearest_of_transfer_transfer_from_and_mint_plus_the_surcharge_may_fill_a_block_exactly(ulong transfer, ulong transferFrom, ulong mint) {
        Assert.Null(Record.Exception(() => Schedule(transfer: transfer, transferFrom: transferFrom, mint: mint, surcharge: 1_000_000)));
    }

    [Fact]
    public void Approve_and_burn_are_not_held_to_the_surcharge_ceiling() {
        // Neither credits a holder, so they may cost the whole block even with the largest surcharge.
        Assert.Null(Record.Exception(() => Schedule(approve: BlockLimit, burn: BlockLimit, surcharge: 1_000_000)));
    }

    // ---- gas for an operation -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(TransactionKind.Transfer, false, 35_000UL)]
    [InlineData(TransactionKind.Transfer, true, 55_000UL)]
    [InlineData(TransactionKind.TransferFrom, false, 45_000UL)]
    [InlineData(TransactionKind.TransferFrom, true, 65_000UL)]
    [InlineData(TransactionKind.Mint, false, 40_000UL)]
    [InlineData(TransactionKind.Mint, true, 60_000UL)]
    [InlineData(TransactionKind.Approve, false, 30_000UL)]
    [InlineData(TransactionKind.Approve, true, 30_000UL)]
    [InlineData(TransactionKind.IncreaseAllowance, false, 30_000UL)]
    [InlineData(TransactionKind.IncreaseAllowance, true, 30_000UL)]
    [InlineData(TransactionKind.DecreaseAllowance, false, 30_000UL)]
    [InlineData(TransactionKind.DecreaseAllowance, true, 30_000UL)]
    [InlineData(TransactionKind.Burn, false, 32_000UL)]
    [InlineData(TransactionKind.Burn, true, 32_000UL)]
    public void The_standard_gas_for_an_operation_adds_the_surcharge_only_where_a_holder_is_credited(TransactionKind kind, bool createsHolder, ulong expectedGas) {
        Assert.Equal(expectedGas, GasSchedule.Standard.GasFor(kind, createsHolder));
    }

    [Theory]
    [InlineData(TransactionKind.Approve)]
    [InlineData(TransactionKind.IncreaseAllowance)]
    [InlineData(TransactionKind.DecreaseAllowance)]
    public void The_three_allowance_operations_all_cost_the_approve_gas(TransactionKind allowanceKind) {
        // Arrange
        GasSchedule anySchedule = Any.GasSchedule().Generate();

        // Act
        ulong gas = anySchedule.GasFor(allowanceKind, createsHolder: true);

        // Assert
        Assert.Equal(anySchedule.Approve, gas);
    }

    [Fact]
    public void A_burn_costs_the_burn_gas_even_when_asked_for_a_new_holder() {
        // Arrange
        GasSchedule anySchedule = Any.GasSchedule().Generate();

        // Act
        ulong gas = anySchedule.GasFor(TransactionKind.Burn, createsHolder: true);

        // Assert
        Assert.Equal(anySchedule.Burn, gas);
    }

    [Fact]
    public void A_kind_the_schedule_does_not_know_is_refused() {
        // Arrange
        GasSchedule anySchedule = Any.GasSchedule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anySchedule.GasFor((TransactionKind)99, createsHolder: false));
    }

    // ---- equality -------------------------------------------------------------------------------------------------

    [Fact]
    public void Two_schedules_with_the_same_six_costs_are_equal_and_share_their_hash_code() {
        // Arrange
        GasSchedule first  = new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);
        GasSchedule second = new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(GasSchedule.Standard, first);
    }

    [Fact]
    public void A_schedule_differing_by_any_of_the_six_costs_is_not_equal() {
        // Arrange
        GasSchedule reference = GasSchedule.Standard;

        // Assert
        Assert.False(reference.Equals(new GasSchedule(35_001, 30_000, 45_000, 40_000, 32_000, 20_000)));
        Assert.False(reference.Equals(new GasSchedule(35_000, 30_001, 45_000, 40_000, 32_000, 20_000)));
        Assert.False(reference.Equals(new GasSchedule(35_000, 30_000, 45_001, 40_000, 32_000, 20_000)));
        Assert.False(reference.Equals(new GasSchedule(35_000, 30_000, 45_000, 40_001, 32_000, 20_000)));
        Assert.False(reference.Equals(new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_001, 20_000)));
        Assert.False(reference.Equals(new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_001)));
        Assert.False(reference.Equals((GasSchedule?)null));
    }

    [Fact]
    public void A_schedule_prints_its_six_costs() {
        Assert.Equal("transfer 35000, approve 30000, transferFrom 45000, mint 40000, burn 32000, new holder +20000", GasSchedule.Standard.ToString());
    }

}
