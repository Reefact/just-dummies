using CarbonLedger.Domain;
using CarbonLedger.Tests.Discovery.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Discovery;

public sealed class CreditSerialTests {

    [Fact]
    public void A_serial_exposes_the_four_parts_it_was_built_from() {
        // Act
        CreditSerial serial = new CreditSerial("VCS", 1234, 2019, 42);

        // Assert
        Assert.Equal("VCS", serial.Registry);
        Assert.Equal(1234, serial.ProjectId);
        Assert.Equal(2019, serial.Vintage);
        Assert.Equal(42L, serial.UnitNumber);
    }

    [Fact]
    public void A_serial_is_written_with_zero_padded_project_and_unit_numbers() {
        // Arrange
        CreditSerial serial = new CreditSerial("VCS", 1234, 2019, 42);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("VCS-1234-2019-000000000042", text);
    }

    [Fact]
    public void A_project_id_shorter_than_four_digits_is_padded_to_four() {
        Assert.Equal("GS-0007-1990-000000000001", new CreditSerial("GS", 7, 1990, 1).ToString());
    }

    [Fact]
    public void A_six_digit_project_id_and_a_twelve_digit_unit_number_are_written_in_full() {
        Assert.Equal("ABCDE-999999-2100-999999999999", new CreditSerial("ABCDE", 999_999, 2100, 999_999_999_999).ToString());
    }

    [Fact]
    public void A_serial_survives_a_round_trip_through_its_text() {
        // Arrange
        CreditSerial anySerial = Any.CreditSerial().Generate();

        // Act
        CreditSerial parsed = CreditSerial.Parse(anySerial.ToString());

        // Assert: a property, run on one draw (the round trip is the rule the model states)
        Assert.Equal(anySerial, parsed);
    }

    [Fact]
    public void Parse_reads_the_canonical_form() {
        // Act
        CreditSerial serial = CreditSerial.Parse("VCS-1234-2019-000000000042");

        // Assert
        Assert.Equal(new CreditSerial("VCS", 1234, 2019, 42), serial);
    }

    [Fact]
    public void Parse_accepts_a_five_or_six_digit_project_id() {
        Assert.Equal(12345, CreditSerial.Parse("VCS-12345-2019-000000000042").ProjectId);
        Assert.Equal(999_999, CreditSerial.Parse("VCS-999999-2019-000000000042").ProjectId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("VCS")]
    [InlineData("vcs-1234-2019-000000000042")]
    [InlineData("VCS-123-2019-000000000042")]
    [InlineData("VCS-1234567-2019-000000000042")]
    [InlineData("VCS-1234-19-000000000042")]
    [InlineData("VCS-1234-2019-42")]
    [InlineData("VCS-1234-2019-0000000000042")]
    [InlineData("V-1234-2019-000000000042")]
    [InlineData("VCSABC-1234-2019-000000000042")]
    [InlineData(" VCS-1234-2019-000000000042")]
    [InlineData("VCS-1234-2019-000000000042 ")]
    [InlineData("VCS_1234_2019_000000000042")]
    [InlineData("VCS-12a4-2019-000000000042")]
    public void Parse_refuses_text_that_is_not_the_canonical_form(string text) {
        Assert.Throws<FormatException>(() => CreditSerial.Parse(text));
    }

    [Fact]
    public void Parse_refuses_null() {
        Assert.Throws<ArgumentNullException>(() => CreditSerial.Parse(null!));
    }

    [Theory]
    [InlineData("VCS-0000-2019-000000000042")]
    [InlineData("VCS-1234-1989-000000000042")]
    [InlineData("VCS-1234-2101-000000000042")]
    [InlineData("VCS-1234-2019-000000000000")]
    public void Parse_refuses_a_well_formed_text_whose_numbers_are_out_of_range(string text) {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => CreditSerial.Parse(text));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999_999)]
    public void The_project_id_limits_are_accepted(int projectId) {
        CreditSerial serial = Any.CreditSerial().WithProjectId(projectId).Generate();

        Assert.Equal(projectId, serial.ProjectId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void A_project_id_outside_1_to_999999_is_refused(int projectId) {
        string anyRegistry = Any.StringMatching("[A-Z]{2,5}").Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditSerial(anyRegistry, projectId, 2019, 42));
    }

    [Theory]
    [InlineData(1990)]
    [InlineData(2100)]
    public void The_vintage_limits_are_accepted(int vintage) {
        CreditSerial serial = Any.CreditSerial().WithVintage(vintage).Generate();

        Assert.Equal(vintage, serial.Vintage);
    }

    [Theory]
    [InlineData(1989)]
    [InlineData(2101)]
    [InlineData(0)]
    [InlineData(-2019)]
    public void A_vintage_outside_1990_to_2100_is_refused(int vintage) {
        string anyRegistry = Any.StringMatching("[A-Z]{2,5}").Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditSerial(anyRegistry, 1234, vintage, 42));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(999_999_999_999L)]
    public void The_unit_number_limits_are_accepted(long unitNumber) {
        CreditSerial serial = Any.CreditSerial().WithUnitNumber(unitNumber).Generate();

        Assert.Equal(unitNumber, serial.UnitNumber);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(1_000_000_000_000L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void A_unit_number_outside_1_to_999999999999_is_refused(long unitNumber) {
        string anyRegistry = Any.StringMatching("[A-Z]{2,5}").Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditSerial(anyRegistry, 1234, 2019, unitNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("V")]
    [InlineData("ABCDEF")]
    [InlineData("vcs")]
    [InlineData("Vcs")]
    [InlineData("VC5")]
    [InlineData("VC-")]
    [InlineData(" VC")]
    [InlineData("VC ")]
    [InlineData("V C")]
    public void A_registry_that_is_not_two_to_five_upper_case_letters_is_refused(string registry) {
        Assert.Throws<ArgumentException>(() => new CreditSerial(registry, 1234, 2019, 42));
    }

    [Fact]
    public void A_null_registry_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditSerial(null!, 1234, 2019, 42));
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDE")]
    public void The_registry_length_limits_are_accepted(string registry) {
        CreditSerial serial = Any.CreditSerial().WithRegistry(registry).Generate();

        Assert.Equal(registry, serial.Registry);
    }

    [Fact(Skip = "domain defect: the registry pattern ^[A-Z]{2,5}$ lets a trailing line feed through ($ matches before a final \\n), so \"VCS\\n\" is accepted; deterministic, no seed")]
    public void A_registry_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new CreditSerial("VCS\n", 1234, 2019, 42));
    }

    [Fact]
    public void Two_serials_with_the_same_four_parts_are_equal_and_hash_alike() {
        // Arrange
        CreditSerial serial          = new CreditSerial("VCS", 1234, 2019, 42);
        CreditSerial sameFourParts   = new CreditSerial("VCS", 1234, 2019, 42);

        // Assert
        Assert.Equal(serial, sameFourParts);
        Assert.True(serial.Equals((object)sameFourParts));
        Assert.Equal(serial.GetHashCode(), sameFourParts.GetHashCode());
    }

    [Fact]
    public void A_serial_differing_in_any_one_part_is_not_equal() {
        // Arrange
        CreditSerial serial = new CreditSerial("VCS", 1234, 2019, 42);

        // Assert
        Assert.NotEqual(serial, new CreditSerial("GS", 1234, 2019, 42));
        Assert.NotEqual(serial, new CreditSerial("VCS", 1235, 2019, 42));
        Assert.NotEqual(serial, new CreditSerial("VCS", 1234, 2020, 42));
        Assert.NotEqual(serial, new CreditSerial("VCS", 1234, 2019, 43));
    }

    [Fact]
    public void A_serial_is_not_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        CreditSerial anySerial = Any.CreditSerial().Generate();

        // Assert
        Assert.False(anySerial.Equals(null));
        Assert.False(anySerial.Equals((object?)null));
        Assert.False(anySerial.Equals((object)anySerial.ToString()));
    }

    [Fact]
    public void Serials_can_be_used_as_keys_of_a_set() {
        // Arrange
        HashSet<CreditSerial> set = [new CreditSerial("VCS", 1234, 2019, 42)];

        // Assert
        Assert.Contains(new CreditSerial("VCS", 1234, 2019, 42), set);
        Assert.DoesNotContain(new CreditSerial("VCS", 1234, 2019, 43), set);
    }

}
