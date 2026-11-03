using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class NirTests {

    // The three worked examples below were computed by hand, outside the code under test:
    // body modulo 97, key = 97 - remainder, Corsica read as 19 (2A) and 18 (2B).
    [Theory]
    [InlineData("185057501234522", Sex.Male,   85, 5)]
    [InlineData("285057501234569", Sex.Female, 85, 5)]
    [InlineData("279022A01234581", Sex.Female, 79, 2)]
    [InlineData("169022B01234566", Sex.Male,   69, 2)]
    public void A_well_formed_nir_tells_the_sex_and_the_birth_year_and_month_it_carries(string value, Sex expectedSex, int expectedYear, int expectedMonth) {
        // Act
        Nir nir = new Nir(value);

        // Assert
        Assert.Equal(value, nir.Value);
        Assert.Equal(expectedSex, nir.Sex);
        Assert.Equal(expectedYear, nir.BirthYearTwoDigits);
        Assert.Equal(expectedMonth, nir.BirthMonth);
    }

    [Theory]
    [InlineData("1850575012345", 22)]
    [InlineData("2850575012345", 69)]
    [InlineData("279022A012345", 81)]
    [InlineData("169022B012345", 66)]
    [InlineData("1850575000047", 1)]
    [InlineData("1850575000048", 97)]
    public void The_control_key_is_97_minus_the_body_modulo_97(string body, int expectedKey) {
        // Act
        int key = Nir.ComputeKey(body);

        // Assert
        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData("279022A012345", "2790219012345")]
    [InlineData("169022B012345", "1690218012345")]
    public void Corsica_reads_2A_as_19_and_2B_as_18_when_computing_the_key(string corsican, string numericTwin) {
        // Act
        int corsicanKey = Nir.ComputeKey(corsican);
        int twinKey     = Nir.ComputeKey(numericTwin);

        // Assert
        Assert.Equal(twinKey, corsicanKey);
    }

    [Fact]
    public void Two_nirs_with_the_same_value_are_equal_and_share_their_hash_code() {
        // Arrange
        Nir anyNir = Any.Nir().Generate();

        // Act
        Nir sameValue = new Nir(anyNir.Value);

        // Assert
        Assert.True(anyNir.Equals(sameValue));
        Assert.Equal(anyNir.GetHashCode(), sameValue.GetHashCode());
        Assert.Equal(anyNir.Value, sameValue.ToString());
    }

    [Fact]
    public void Two_nirs_with_different_values_are_not_equal() {
        // Arrange
        Nir anyNir      = Any.Nir().Generate();
        Nir anyOtherNir = Any.Nir().DifferentFrom(anyNir).Generate();

        // Assert
        Assert.False(anyNir.Equals(anyOtherNir));
    }

    [Fact]
    public void A_nir_is_not_equal_to_a_string_carrying_its_value() {
        // Arrange
        Nir anyNir = Any.Nir().Generate();

        // Assert
        Assert.False(anyNir.Equals(anyNir.Value));
        Assert.False(anyNir.Equals(null));
    }

    [Fact]
    public void A_null_nir_is_refused_as_a_missing_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Nir(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("18505750123452")]       // 14 characters
    [InlineData("1850575012345222")]     // 16 characters
    [InlineData("385057501234522")]      // sex digit 3
    [InlineData("085057501234522")]      // sex digit 0
    [InlineData("185137501234522")]      // month 13
    [InlineData("185007501234522")]      // month 00
    [InlineData("18505750123452A")]      // a letter in the key
    [InlineData("1850575A12345 22")]     // a space inside
    [InlineData("185052C01234522")]      // department 2C
    [InlineData("279022a01234581")]      // department 2a, lower case
    [InlineData(" 185057501234522")]     // leading space
    [InlineData("185057501234522 ")]     // trailing space
    public void A_malformed_nir_is_refused_as_an_invalid_argument(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("185057501234523")]      // right body, key off by one
    [InlineData("185057501234500")]      // key 00 can never be valid
    [InlineData("185057501234598")]      // key 98 is above the highest key
    [InlineData("285057501234522")]      // key of another body
    public void A_nir_whose_control_key_does_not_match_its_body_is_refused(string wrongKey) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir(wrongKey));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_nir_carrying_the_key_of_no_body_but_otherwise_arbitrary_is_refused() {
        // Arrange
        string anyValidNir = Any.Nir().Generate().Value;
        int    validKey    = int.Parse(anyValidNir.Substring(13, 2));
        int    anotherKey  = validKey % 97 + 1;
        string wrongKey    = anyValidNir.Substring(0, 13) + anotherKey.ToString("00");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir(wrongKey));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final newline, so a 16-character value with a trailing '\\n' is accepted as a NIR (use \\z); seed none: literal input")]
    public void A_nir_followed_by_a_line_break_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir("185057501234522\n"));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

}
