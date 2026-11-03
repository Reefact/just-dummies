using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class ObservedTests {

    [Fact]
    public void An_observed_field_carries_its_value_and_no_reason() {
        // Arrange
        Observed<int> observed = Observed<int>.Of(120);

        // Assert
        Assert.True(observed.IsObserved);
        Assert.Equal(120, observed.Value);
        Assert.Null(observed.Reason);
    }

    [Theory]
    [InlineData(MissingReason.NotDone)]
    [InlineData(MissingReason.NotApplicable)]
    [InlineData(MissingReason.Unknown)]
    [InlineData(MissingReason.LostToFollowUp)]
    [InlineData(MissingReason.Refused)]
    public void A_missing_field_carries_its_reason_and_no_value(MissingReason reason) {
        // Act
        Observed<decimal> missing = Observed<decimal>.Missing(reason);

        // Assert
        Assert.False(missing.IsObserved);
        Assert.Null(missing.Value);
        Assert.Equal(reason, missing.Reason);
        Assert.True(missing.IsMissing(reason));
    }

    [Fact]
    public void A_missing_field_is_missing_for_its_own_reason_only() {
        // Arrange
        Observed<int> missing = Observed<int>.Missing(MissingReason.Refused);

        // Assert
        Assert.False(missing.IsMissing(MissingReason.NotDone));
    }

    [Fact]
    public void An_observed_field_is_missing_for_no_reason() {
        // Arrange
        MissingReason anyReason   = Any.Enum<MissingReason>().Generate();
        Observed<int> observed    = Observed<int>.Of(Any.Int32().Generate());

        // Assert
        Assert.False(observed.IsMissing(anyReason));
    }

    [Fact]
    public void A_field_cannot_be_both_observed_and_missing() {
        // Arrange
        int           anyValue  = Any.Int32().Generate();
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Observed<int>(anyValue, anyReason));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void A_resolved_field_cannot_carry_neither_a_value_nor_a_reason() {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => new Observed<DateOnly>(null, null));
    }

    [Fact]
    public void A_missing_reason_must_be_a_declared_one() {
        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Observed<int>.Missing((MissingReason)99));
    }

    [Fact]
    public void Two_observed_fields_with_the_same_value_are_equal() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act
        Observed<DateOnly> first  = Observed<DateOnly>.Of(anyDate);
        Observed<DateOnly> second = Observed<DateOnly>.Of(anyDate);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_missing_fields_are_equal_when_their_reasons_are() {
        Assert.Equal(Observed<int>.Missing(MissingReason.Unknown), Observed<int>.Missing(MissingReason.Unknown));
        Assert.NotEqual(Observed<int>.Missing(MissingReason.Unknown), Observed<int>.Missing(MissingReason.Refused));
    }

    [Fact]
    public void An_observed_field_never_equals_a_missing_one() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Assert
        Assert.NotEqual(Observed<int>.Of(0), Observed<int>.Missing(anyReason));
    }

    [Fact]
    public void An_observed_field_prints_its_value_and_a_missing_one_its_reason() {
        Assert.Equal("120", Observed<int>.Of(120).ToString());
        Assert.Equal("<LostToFollowUp>", Observed<int>.Missing(MissingReason.LostToFollowUp).ToString());
    }

}
