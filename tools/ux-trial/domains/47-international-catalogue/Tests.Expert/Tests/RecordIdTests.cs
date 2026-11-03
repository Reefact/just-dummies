using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class RecordIdTests {

    [Fact]
    public void A_record_id_starts_with_the_agency() {
        // Act
        RecordId id = new RecordId("FR00412873");

        // Assert
        Assert.Equal("FR", id.Agency);
        Assert.Equal("FR00412873", id.Value);
        Assert.Equal("FR00412873", id.ToString());
    }

    [Theory]
    [InlineData("AA00000001")]
    [InlineData("AA00000010")]
    [InlineData("ZZ99999999")]
    [InlineData("AB10000000")]
    public void Any_non_zero_record_number_is_accepted(string written) {
        Assert.Equal(written, new RecordId(written).Value);
    }

    [Fact]
    public void A_record_id_is_required() {
        Assert.Throws<ArgumentNullException>(() => new RecordId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("FR0041287")]
    [InlineData("FR004128731")]
    public void A_record_id_has_ten_characters(string wrongLength) {
        Assert.Throws<FormatException>(() => new RecordId(wrongLength));
    }

    [Theory]
    [InlineData("fr00412873")]
    [InlineData("Fr00412873")]
    [InlineData("F100412873")]
    [InlineData("0000412873")]
    [InlineData("ÉR00412873")]
    [InlineData("FR 0412873")]
    public void The_agency_is_two_upper_case_ascii_letters(string badAgency) {
        Assert.Throws<FormatException>(() => new RecordId(badAgency));
    }

    [Theory]
    [InlineData("FR0041287X")]
    [InlineData("FR-0412873")]
    [InlineData("FR٠٠٤١٢٨٧٣")]
    [InlineData("FR0041287\n")]
    public void The_record_number_is_eight_ascii_digits(string badNumber) {
        Assert.Throws<FormatException>(() => new RecordId(badNumber));
    }

    [Fact]
    public void The_record_number_cannot_be_zero() {
        Assert.Throws<FormatException>(() => new RecordId("FR00000000"));
    }

    [Fact]
    public void Record_ids_are_equal_when_written_the_same() {
        Assert.Equal(new RecordId("FR00412873"), new RecordId("FR00412873"));
        Assert.Equal(new RecordId("FR00412873").GetHashCode(), new RecordId("FR00412873").GetHashCode());
        Assert.NotEqual(new RecordId("FR00412873"), new RecordId("DE00412873"));
    }

}
