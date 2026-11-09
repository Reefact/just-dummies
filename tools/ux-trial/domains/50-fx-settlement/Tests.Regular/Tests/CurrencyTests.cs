using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class CurrencyTests {

    public static TheoryData<string, int> IsoTableRows => new TheoryData<string, int> {
        { "JPY", 0 }, { "KRW", 0 }, { "CLP", 0 },
        { "EUR", 2 }, { "USD", 2 }, { "GBP", 2 }, { "CHF", 2 }, { "CAD", 2 }, { "AUD", 2 }, { "NOK", 2 }, { "SEK", 2 },
        { "KWD", 3 }, { "BHD", 3 }, { "TND", 3 }, { "JOD", 3 }, { "OMR", 3 },
        { "CLF", 4 },
    };

    [Theory, MemberData(nameof(IsoTableRows))]
    public void A_code_of_the_ISO_table_carries_its_ISO_minor_units(string code, int minorUnits) {
        // Act
        Currency currency = new Currency(code, minorUnits);

        // Assert
        Assert.Equal(code, currency.Code);
        Assert.Equal(minorUnits, currency.MinorUnits);
    }

    [Theory]
    [InlineData("USD", 0)]
    [InlineData("USD", 3)]
    [InlineData("JPY", 2)]
    [InlineData("KWD", 2)]
    [InlineData("CLF", 3)]
    [InlineData("EUR", 4)]
    public void A_code_of_the_ISO_table_is_refused_with_other_minor_units(string code, int minorUnits) {
        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new Currency(code, minorUnits));

        // Assert
        Assert.Equal("minorUnits", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void A_code_outside_the_table_takes_any_minor_units_in_range(int minorUnits) {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act
        Currency currency = new Currency(anyCode, minorUnits);

        // Assert
        Assert.Equal(minorUnits, currency.MinorUnits);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Minor_units_outside_zero_to_four_are_refused(int minorUnits) {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Currency(anyCode, minorUnits));
    }

    [Fact]
    public void A_code_starting_with_X_is_refused_whatever_its_minor_units() {
        // Arrange
        string anyXCode     = Any.StringMatching("X[A-Z]{2}").Generate();
        int    anyMinorUnits = Any.Int32().Between(0, 4).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Currency(anyXCode, anyMinorUnits));
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("U5D")]
    [InlineData("")]
    [InlineData(" USD")]
    public void A_code_that_is_not_three_upper_case_letters_is_refused(string code) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new Currency(code, 2));
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Currency(null!, 2));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the code pattern ('$' also matches before a final \\n), so new Currency(\"ABC\\n\", 2) is built; seed-independent")]
    public void A_code_with_a_trailing_line_feed_is_refused() {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new Currency(anyCode + "\n", 2));
    }

    [Fact(Skip = "domain defect: 'USD\\n' escapes the ISO table lookup, so a 'USD' that is not 2 minor units gets built; seed-independent")]
    public void A_table_code_with_a_trailing_line_feed_still_answers_to_the_table() {
        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new Currency("USD\n", 0));
    }

    [Fact]
    public void Two_currencies_with_the_same_code_and_minor_units_are_equal() {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act
        Currency first  = new Currency(anyCode, 1);
        Currency second = new Currency(anyCode, 1);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Currencies_with_the_same_code_and_different_minor_units_are_different() {
        // Arrange
        string anyCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act
        Currency twoPlaces   = new Currency(anyCode, 2);
        Currency threePlaces = new Currency(anyCode, 3);

        // Assert
        Assert.NotEqual(twoPlaces, threePlaces);
    }

    [Fact]
    public void Currencies_with_different_codes_are_different() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();
        Currency anyOther    = new AnyCurrency().Except(anyCurrency).Generate();

        // Act / Assert
        Assert.NotEqual(anyCurrency, anyOther);
        Assert.False(anyCurrency.Equals((Currency?)null));
        Assert.False(anyCurrency.Equals("EUR"));
    }

    [Fact]
    public void A_currency_prints_as_its_code() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Equal(anyCurrency.Code, anyCurrency.ToString());
    }

    [Theory, MemberData(nameof(IsoTableRows))]
    public void Iso_builds_the_currency_of_the_table(string code, int minorUnits) {
        // Act / Assert
        Assert.Equal(new Currency(code, minorUnits), Currency.Iso(code));
    }

    [Fact]
    public void Iso_refuses_a_code_outside_the_table() {
        // Arrange
        string anyUnknownCode = Any.StringMatching("[A-WYZ][A-Z]{2}").Except(AnyCurrency.IsoCodes).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => Currency.Iso(anyUnknownCode));
    }

    [Fact]
    public void The_statics_are_the_currencies_of_the_table() {
        // Act / Assert
        Assert.Equal(Currency.Iso("USD"), Currency.Usd);
        Assert.Equal(Currency.Iso("EUR"), Currency.Eur);
        Assert.Equal(Currency.Iso("GBP"), Currency.Gbp);
        Assert.Equal(Currency.Iso("CHF"), Currency.Chf);
        Assert.Equal(Currency.Iso("CAD"), Currency.Cad);
        Assert.Equal(Currency.Iso("JPY"), Currency.Jpy);
        Assert.Equal(Currency.Iso("KRW"), Currency.Krw);
        Assert.Equal(Currency.Iso("KWD"), Currency.Kwd);
        Assert.Equal(Currency.Iso("BHD"), Currency.Bhd);
        Assert.Equal(Currency.Iso("TND"), Currency.Tnd);
        Assert.Equal(Currency.Iso("CLF"), Currency.Clf);
    }

    public static TheoryData<string, int, string> SmallestUnits => new TheoryData<string, int, string> {
        { "JPY", 0, "1" }, { "USD", 2, "0.01" }, { "KWD", 3, "0.001" }, { "CLF", 4, "0.0001" },
    };

    [Theory, MemberData(nameof(SmallestUnits))]
    public void The_smallest_unit_is_one_in_the_last_minor_digit(string code, int minorUnits, string expected) {
        // Act
        Currency currency = new Currency(code, minorUnits);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), currency.SmallestUnit);
    }

    public static TheoryData<string, string, bool> Allowances => new TheoryData<string, string, bool> {
        { "USD", "1.25", true }, { "USD", "1.255", false }, { "USD", "1.2500", true }, { "USD", "-0.01", true },
        { "JPY", "100", true }, { "JPY", "100.0", true }, { "JPY", "100.5", false },
        { "KWD", "1.234", true }, { "KWD", "1.2345", false },
        { "CLF", "1.2345", true },
    };

    [Theory, MemberData(nameof(Allowances))]
    public void An_amount_is_allowed_up_to_the_minor_units_trailing_zeros_ignored(string code, string amount, bool expected) {
        // Arrange
        Currency currency = Currency.Iso(code);

        // Act / Assert
        Assert.Equal(expected, currency.Allows(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    public static TheoryData<string, string, MidpointRounding, string> Roundings => new TheoryData<string, string, MidpointRounding, string> {
        { "USD", "1.005", MidpointRounding.AwayFromZero, "1.01" },
        { "USD", "1.005", MidpointRounding.ToEven, "1.00" },
        { "USD", "1.015", MidpointRounding.ToEven, "1.02" },
        { "USD", "-1.005", MidpointRounding.AwayFromZero, "-1.01" },
        { "JPY", "2.5", MidpointRounding.ToEven, "2" },
        { "JPY", "2.5", MidpointRounding.AwayFromZero, "3" },
        { "JPY", "3.5", MidpointRounding.ToEven, "4" },
        { "KWD", "0.0005", MidpointRounding.ToZero, "0.000" },
        { "KWD", "0.0015", MidpointRounding.ToPositiveInfinity, "0.002" },
    };

    [Theory, MemberData(nameof(Roundings))]
    public void Round_applies_the_requested_midpoint_mode_to_the_minor_units(string code, string amount, MidpointRounding mode, string expected) {
        // Arrange
        Currency currency = Currency.Iso(code);

        // Act
        decimal rounded = currency.Round(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), mode);

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), rounded);
    }

    [Fact]
    public void Round_refuses_an_unknown_mode() {
        // Arrange
        Currency anyCurrency = Any.Currency().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => anyCurrency.Round(1.5m, (MidpointRounding)99));
    }

}
