using System;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class ObservedTests {

    [Fact]
    public void An_observed_field_carries_its_value_and_no_reason() {
        // Act
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
    public void A_missing_field_is_missing_only_for_its_own_reason() {
        // Arrange
        Observed<int> missingAsRefused = Observed<int>.Missing(MissingReason.Refused);

        // Assert
        Assert.False(missingAsRefused.IsMissing(MissingReason.NotDone));
    }

    [Fact]
    public void An_observed_field_is_missing_for_no_reason() {
        // Arrange
        MissingReason anyReason   = Any.Enum<MissingReason>().Generate();
        Observed<int> anyObserved = Observed<int>.Of(Any.Int32().Generate());

        // Assert
        Assert.False(anyObserved.IsMissing(anyReason));
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
    public void A_resolved_field_cannot_be_neither_observed_nor_missing() {
        Assert.Throws<ArgumentException>(() => new Observed<DateOnly>(null, null));
    }

    [Fact]
    public void A_missing_reason_must_be_a_declared_one() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Observed<int>(null, (MissingReason)99));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void Two_fields_observing_the_same_value_are_equal() {
        // Arrange
        decimal anyValue = Any.Decimal().Between(0m, 1000m).Generate();

        // Act
        Observed<decimal> first  = Observed<decimal>.Of(anyValue);
        Observed<decimal> second = new Observed<decimal>(anyValue, null);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_fields_observing_different_values_are_not_equal() {
        // Arrange
        int anyValue      = Any.Int32().Generate();
        int anyOtherValue = Any.Int32().DifferentFrom(anyValue).Generate();

        // Assert
        Assert.NotEqual(Observed<int>.Of(anyValue), Observed<int>.Of(anyOtherValue));
    }

    [Fact]
    public void Two_fields_missing_for_different_reasons_are_not_equal() {
        Assert.NotEqual(Observed<int>.Missing(MissingReason.NotDone), Observed<int>.Missing(MissingReason.Unknown));
    }

    [Fact]
    public void Two_fields_missing_for_the_same_reason_are_equal() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Assert
        Assert.Equal(Observed<int>.Missing(anyReason), Observed<int>.Missing(anyReason));
    }

    [Fact]
    public void An_observed_field_is_never_equal_to_a_missing_one() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Assert
        Assert.NotEqual(Observed<int>.Of(0), Observed<int>.Missing(anyReason));
    }

    [Fact]
    public void A_field_is_not_equal_to_nothing() {
        Assert.False(Observed<int>.Of(1).Equals(null));
    }

    [Fact]
    public void An_observed_field_prints_its_value_and_a_missing_one_its_reason() {
        Assert.Equal("37.5", Observed<decimal>.Of(37.5m).ToString());
        Assert.Equal("<LostToFollowUp>", Observed<decimal>.Missing(MissingReason.LostToFollowUp).ToString());
    }

}
