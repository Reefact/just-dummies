using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class RecordIdTests {

    [Fact]
    public void A_record_id_names_its_agency() {
        // Act
        RecordId id = new RecordId("FR00412873");

        // Assert
        Assert.Equal("FR00412873", id.Value);
        Assert.Equal("FR", id.Agency);
        Assert.Equal("FR00412873", id.ToString());
    }

    [Theory]
    [InlineData("FR00000001")]
    [InlineData("AB10000000")]
    [InlineData("ZZ99999999")]
    public void A_record_id_with_a_non_zero_record_number_is_accepted(string text) {
        Assert.Equal(text, new RecordId(text).Value);
    }

    [Theory]
    [InlineData("FR00000000")]
    [InlineData("ZZ00000000")]
    public void A_record_number_cannot_be_zero(string text) {
        Assert.Throws<FormatException>(() => new RecordId(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("FR0041287")]
    [InlineData("FR004128733")]
    [InlineData("fr00412873")]
    [InlineData("Fr00412873")]
    [InlineData("F100412873")]
    [InlineData("1R00412873")]
    [InlineData("ÉR00412873")]
    [InlineData("FR0041287X")]
    [InlineData("FR0041287 ")]
    [InlineData("FR-0412873")]
    [InlineData("FR٠٠٤١٢٨٧٣")]
    [InlineData("FR００４１２８７３")]
    [InlineData("FR00412873\n")]
    public void A_record_id_is_two_upper_case_ascii_letters_and_eight_ascii_digits(string text) {
        Assert.Throws<FormatException>(() => new RecordId(text));
    }

    [Fact]
    public void A_null_record_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new RecordId(null!));
    }

    [Fact]
    public void Two_record_ids_are_equal_when_their_text_is() {
        Assert.Equal(new RecordId("FR00412873"), new RecordId("FR00412873"));
        Assert.NotEqual(new RecordId("FR00412873"), new RecordId("DE00412873"));
        Assert.Equal(new RecordId("FR00412873").GetHashCode(), new RecordId("FR00412873").GetHashCode());
    }

}
