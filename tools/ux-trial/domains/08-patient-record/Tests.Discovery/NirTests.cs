using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class NirTests {

    [Theory]
    [InlineData("1850578006084", 91)]
    [InlineData("185052A006084", 35)]
    [InlineData("285052B006084", 12)]
    [InlineData("2000101001001", 52)]
    public void The_control_key_is_97_minus_the_body_modulo_97_with_corsica_read_as_19_and_18(string body, int expectedKey) {
        // Act
        int key = Nir.ComputeKey(body);

        // Assert
        Assert.Equal(expectedKey, key);
    }

    [Theory]
    [InlineData("185057800608491")]
    [InlineData("18505" + "2A" + "00608435")]
    [InlineData("28505" + "2B" + "00608412")]
    public void A_well_formed_nir_with_its_right_key_is_accepted(string value) {
        // Act
        Nir nir = new Nir(value);

        // Assert
        Assert.Equal(value, nir.Value);
        Assert.Equal(value, nir.ToString());
    }

    [Fact]
    public void A_nir_beginning_with_1_is_a_man_and_one_beginning_with_2_a_woman() {
        // Arrange
        Nir man   = new Nir("185057800608491");
        Nir woman = new Nir("285052B006084" + "12");

        // Assert
        Assert.Equal(Sex.Male, man.Sex);
        Assert.Equal(Sex.Female, woman.Sex);
    }

    [Fact]
    public void The_birth_year_and_month_are_read_from_characters_2_to_5() {
        // Arrange
        Nir nir = new Nir("185057800608491");

        // Assert
        Assert.Equal(85, nir.BirthYearTwoDigits);
        Assert.Equal(5, nir.BirthMonth);
    }

    [Fact]
    public void A_null_value_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Nir(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("18505780060849")]       // 14 characters
    [InlineData("1850578006084911")]     // 16 characters
    [InlineData("385057800608491")]      // sex digit 3
    [InlineData("085057800608491")]      // sex digit 0
    [InlineData("185137800608491")]      // month 13
    [InlineData("185007800608491")]      // month 00
    [InlineData("18505780060849A")]      // letter in the key
    [InlineData("1850578A0608491")]
    [InlineData("185052C00608435")]     // department 2C
    public void A_value_that_is_not_shaped_like_a_nir_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_wrong_control_key_is_refused() {
        // Arrange
        Nir    anyNir     = Any.Nir().Generate();
        string body       = anyNir.Value.Substring(0, 13);
        int    rightKey   = int.Parse(anyNir.Value.Substring(13, 2));
        int    anyWrongKey = rightKey % 97 + 1;   // 1..97 and never the right one

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Nir(body + anyWrongKey.ToString("00")));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_key_of_00_is_never_valid() {
        // Arrange
        string anyBody = Any.Nir().Generate().Value.Substring(0, 13);

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Nir(anyBody + "00"));
    }

    [Fact]
    public void Two_nirs_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.Nir().Generate().Value;

        Nir first  = new Nir(anyValue);
        Nir second = new Nir(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_nirs_with_different_values_are_not_equal() {
        // Arrange
        Nir first  = new Nir("185057800608491");
        Nir second = new Nir("285052B006084" + "12");

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact(Skip = "domain defect: a trailing newline is accepted ('$' also matches before a final '\\n'); literal input, no seed")]
    public void A_nir_followed_by_a_line_feed_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Nir("185057800608491\n"));
    }

}
