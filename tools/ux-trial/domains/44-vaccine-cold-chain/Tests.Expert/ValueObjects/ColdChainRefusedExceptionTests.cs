using ColdChain.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.ValueObjects;

public sealed class ColdChainRefusedExceptionTests {

    [Theory]
    [InlineData(ColdChainRefusal.NotPlanned)]
    [InlineData(ColdChainRefusal.ColdRoomOutOfSpecification)]
    public void A_refusal_carries_the_reason_and_the_message_it_was_raised_with(ColdChainRefusal refusal) {
        // Act
        ColdChainRefusedException exception = new ColdChainRefusedException(refusal, "not now");

        // Assert
        Assert.Equal(refusal, exception.Refusal);
        Assert.Equal("not now", exception.Message);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation() {
        Assert.IsAssignableFrom<InvalidOperationException>(new ColdChainRefusedException(ColdChainRefusal.NotPlanned, "not now"));
    }

    [Fact]
    public void A_reason_that_is_not_in_the_catalogue_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ColdChainRefusedException((ColdChainRefusal)999, "not now"));
    }

}
