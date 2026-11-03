using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class ObservedTests {

    [Fact]
    public void An_observed_field_carries_its_value_and_no_reason() {
        // Act
        Observed<int> observed = Observed<int>.Of(7);

        // Assert
        Assert.True(observed.IsObserved);
        Assert.Equal(7, observed.Value);
        Assert.Null(observed.Reason);
    }

    [Fact]
    public void An_observed_zero_is_observed_not_absent() {
        // Act
        Observed<int> observed = Observed<int>.Of(0);

        // Assert
        Assert.True(observed.IsObserved);
        Assert.Equal(0, observed.Value);
    }

    [Fact]
    public void A_missing_field_carries_its_reason_and_no_value() {
        // Act
        Observed<decimal> missing = Observed<decimal>.Missing(MissingReason.Refused);

        // Assert
        Assert.False(missing.IsObserved);
        Assert.Null(missing.Value);
        Assert.True(missing.IsMissing(MissingReason.Refused));
        Assert.False(missing.IsMissing(MissingReason.NotDone));
    }

    [Fact]
    public void An_observed_field_is_not_missing_for_any_reason() {
        // Arrange
        Observed<int>  anyObserved = AnyObservedOf.OfValues(Any.Int32()).Generate();
        MissingReason  anyReason   = Any.Enum<MissingReason>().Generate();

        // Act
        bool missing = anyObserved.IsMissing(anyReason);

        // Assert
        Assert.False(missing);
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
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Observed<int>(null, null));

        // Assert
        Assert.Equal("reason", refusal.ParamName);
    }

    [Fact]
    public void A_missing_reason_must_be_one_the_domain_knows() {
        // Arrange
        MissingReason unknownReason = (MissingReason)99;

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Observed<decimal>.Missing(unknownReason));
    }

    [Fact]
    public void Two_observations_of_the_same_value_are_equal() {
        // Arrange
        decimal           anyValue = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();
        Observed<decimal> first    = Observed<decimal>.Of(anyValue);
        Observed<decimal> second   = Observed<decimal>.Of(anyValue);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_missing_fields_are_equal_when_their_reason_is() {
        // Assert
        Assert.Equal(Observed<int>.Missing(MissingReason.Unknown), Observed<int>.Missing(MissingReason.Unknown));
        Assert.NotEqual(Observed<int>.Missing(MissingReason.Unknown), Observed<int>.Missing(MissingReason.NotDone));
    }

    [Fact]
    public void An_observation_differs_from_another_value_and_from_a_missing_field() {
        // Arrange
        int           anyValue      = Any.Int32().Generate();
        int           anyOtherValue = Any.Int32().DifferentFrom(anyValue).Generate();
        MissingReason anyReason     = Any.Enum<MissingReason>().Generate();

        // Act / Assert
        Assert.NotEqual(Observed<int>.Of(anyValue), Observed<int>.Of(anyOtherValue));
        Assert.NotEqual(Observed<int>.Of(anyValue), Observed<int>.Missing(anyReason));
    }

    [Fact]
    public void It_prints_its_value_or_its_reason_in_brackets() {
        // Assert
        Assert.Equal("37", Observed<int>.Of(37).ToString());
        Assert.Equal("<LostToFollowUp>", Observed<int>.Missing(MissingReason.LostToFollowUp).ToString());
    }

}
