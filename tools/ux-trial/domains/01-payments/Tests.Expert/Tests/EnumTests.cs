using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class EnumTests {

    [Theory]
    [InlineData(ChargeBearer.Debtor, 1)]
    [InlineData(ChargeBearer.Creditor, 2)]
    [InlineData(ChargeBearer.Shared, 3)]
    [InlineData(ChargeBearer.FollowingServiceLevel, 4)]
    public void A_charge_bearer_keeps_its_code(ChargeBearer bearer, int expectedCode) {
        // Assert
        Assert.Equal(expectedCode, (int)bearer);
    }

    [Fact]
    public void The_supported_currencies_are_six() {
        // Act
        Currency[] currencies = Enum.GetValues<Currency>();

        // Assert
        Assert.Equal([Currency.EUR, Currency.CHF, Currency.GBP, Currency.SEK, Currency.DKK, Currency.PLN], currencies);
    }

}
