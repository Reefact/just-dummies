namespace TokenLedger.Domain.Tests.Regular;

public sealed class GasScheduleTests {

    [Fact]
    public void The_standard_schedule_is_the_documented_one() {
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
    public void The_limits_are_the_intrinsic_gas_and_the_block_limit() {
        Assert.Equal(21_000UL, GasSchedule.IntrinsicGas);
        Assert.Equal(30_000_000UL, GasSchedule.BlockGasLimit);
        Assert.Equal(1_000_000UL, GasSchedule.MaxNewHolderSurcharge);
    }

    [Fact]
    public void A_schedule_exposes_what_it_was_built_with() {
        // Act
        GasSchedule schedule = new GasSchedule(21_000, 22_000, 23_000, 24_000, 25_000, 26_000);

        // Assert
        Assert.Equal(21_000UL, schedule.Transfer);
        Assert.Equal(22_000UL, schedule.Approve);
        Assert.Equal(23_000UL, schedule.TransferFrom);
        Assert.Equal(24_000UL, schedule.Mint);
        Assert.Equal(25_000UL, schedule.Burn);
        Assert.Equal(26_000UL, schedule.NewHolderSurcharge);
    }

    [Theory]
    [InlineData(20_999UL)]
    [InlineData(30_000_001UL)]
    [InlineData(0UL)]
    public void A_base_cost_outside_the_intrinsic_gas_and_the_block_limit_is_refused(ulong cost) {
        // Arrange
        GasSchedule valid = Any.GasSchedule().Generate();

        // Act & Assert: each of the five base costs is checked.
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(cost, valid.Approve, 30_000_000, valid.Mint, valid.Burn, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(21_000, cost, 30_000_000, valid.Mint, valid.Burn, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(21_000, valid.Approve, cost, valid.Mint, valid.Burn, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(21_000, valid.Approve, 21_000, cost, valid.Burn, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(21_000, valid.Approve, 21_000, valid.Mint, cost, 0));
    }

    [Fact]
    public void Costs_at_both_ends_of_the_range_are_accepted() {
        // Act
        GasSchedule low  = new GasSchedule(21_000, 21_000, 21_000, 21_000, 21_000, 0);
        GasSchedule high = new GasSchedule(29_000_000, 30_000_000, 29_000_000, 29_000_000, 30_000_000, 1_000_000);

        // Assert
        Assert.Equal(21_000UL, low.Burn);
        Assert.Equal(30_000_000UL, high.Burn);
    }

    [Fact]
    public void A_surcharge_past_one_million_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 1_000_001));
    }

    [Fact]
    public void A_delegated_transfer_cannot_cost_less_than_a_plain_one() {
        Assert.Throws<ArgumentException>(() => new GasSchedule(40_000, 30_000, 39_999, 40_000, 32_000, 0));
    }

    [Fact]
    public void A_delegated_transfer_may_cost_exactly_as_much_as_a_plain_one() {
        // Act
        GasSchedule schedule = new GasSchedule(40_000, 30_000, 40_000, 40_000, 32_000, 0);

        // Assert
        Assert.Equal(schedule.Transfer, schedule.TransferFrom);
    }

    [Theory]
    [InlineData(29_000_000UL, 29_000_000UL, 29_000_001UL, 1_000_000UL, true)]
    [InlineData(29_000_000UL, 29_000_000UL, 29_000_000UL, 1_000_000UL, false)]
    [InlineData(21_000UL, 21_000UL, 29_999_999UL, 2UL, true)]            // the mint is the most expensive credit
    [InlineData(21_000UL, 29_999_999UL, 21_000UL, 2UL, true)]            // so is the delegated transfer
    [InlineData(29_999_999UL, 29_999_999UL, 21_000UL, 1UL, false)]       // exactly the block limit
    public void The_most_expensive_credit_plus_the_surcharge_must_fit_in_a_block(ulong transfer, ulong transferFrom, ulong mint, ulong surcharge, bool refused) {
        // Act
        Func<GasSchedule> build = () => new GasSchedule(transfer, 30_000, transferFrom, mint, 32_000, surcharge);

        // Assert
        if (refused) {
            Assert.Throws<ArgumentException>(build);
        } else {
            Assert.Equal(surcharge, build().NewHolderSurcharge);
        }
    }

    [Fact]
    public void An_expensive_burn_or_approval_does_not_count_against_the_surcharge_budget() {
        // Act: burn and approve at the block limit, credits low.
        GasSchedule schedule = new GasSchedule(21_000, 30_000_000, 21_000, 21_000, 30_000_000, 1_000_000);

        // Assert
        Assert.Equal(30_000_000UL, schedule.Burn);
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
    public void The_gas_of_a_transaction_follows_its_kind_and_the_new_holder_surcharge(TransactionKind kind, bool createsHolder, ulong expected) {
        Assert.Equal(expected, GasSchedule.Standard.GasFor(kind, createsHolder));
    }

    [Fact]
    public void An_undefined_kind_has_no_gas() {
        Assert.Throws<ArgumentOutOfRangeException>(() => GasSchedule.Standard.GasFor((TransactionKind)99, false));
    }

    [Fact]
    public void Schedules_with_the_same_costs_are_equal() {
        // Arrange
        GasSchedule anySchedule = Any.GasSchedule().Generate();

        // Act
        GasSchedule copy = new GasSchedule(anySchedule.Transfer, anySchedule.Approve, anySchedule.TransferFrom, anySchedule.Mint, anySchedule.Burn, anySchedule.NewHolderSurcharge);

        // Assert
        Assert.Equal(anySchedule, copy);
        Assert.Equal(anySchedule.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Schedules_differing_in_one_cost_are_not_equal() {
        // Arrange
        GasSchedule reference = new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_000);

        // Assert
        Assert.NotEqual(reference, new GasSchedule(35_001, 30_000, 45_000, 40_000, 32_000, 20_000));
        Assert.NotEqual(reference, new GasSchedule(35_000, 30_001, 45_000, 40_000, 32_000, 20_000));
        Assert.NotEqual(reference, new GasSchedule(35_000, 30_000, 45_001, 40_000, 32_000, 20_000));
        Assert.NotEqual(reference, new GasSchedule(35_000, 30_000, 45_000, 40_001, 32_000, 20_000));
        Assert.NotEqual(reference, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_001, 20_000));
        Assert.NotEqual(reference, new GasSchedule(35_000, 30_000, 45_000, 40_000, 32_000, 20_001));
    }

    [Fact]
    public void A_schedule_is_described_by_its_costs() {
        Assert.Equal("transfer 35000, approve 30000, transferFrom 45000, mint 40000, burn 32000, new holder +20000", GasSchedule.Standard.ToString());
    }

}
