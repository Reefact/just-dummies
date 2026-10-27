using System.Globalization;

using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

[Reproducible]
public sealed class NirTests {

    [Fact]
    public void A_NIR_starting_with_2_belongs_to_a_woman() {
        // Arrange
        string anyRestOfBody = Any.StringMatching("[0-9]{2}(0[1-9]|1[0-2])([0-9]{2}|2[AB])[0-9]{6}").Generate();
        string body          = "2" + anyRestOfBody;
        Nir    nir           = new Nir(body + Nir.ComputeKey(body).ToString("00", CultureInfo.InvariantCulture));

        // Act
        Sex sex = nir.Sex;

        // Assert
        Assert.Equal(Sex.Female, sex);
    }

    [Fact]
    public void A_NIR_whose_control_key_is_off_by_one_is_refused() {
        // Arrange
        Nir    anyNir      = Any.Nir().Generate();
        int    rightKey    = int.Parse(anyNir.Value.Substring(13), CultureInfo.InvariantCulture);
        string tamperedNir = anyNir.Value.Substring(0, 13) + (rightKey % 97 + 1).ToString("00", CultureInfo.InvariantCulture);

        // Act
        Action creating = () => new Nir(tamperedNir);

        // Assert
        Assert.Throws<ArgumentException>("value", creating);
    }

    [Fact]
    public void A_Corsican_NIR_is_keyed_as_if_2A_read_19() {
        // Arrange: 1850319123456 has the key 01, so 185032A123456 must too.
        const string corsicanNir = "185032A12345601";

        // Act
        Nir nir = new Nir(corsicanNir);

        // Assert
        Assert.Equal(corsicanNir, nir.Value);
    }

}
