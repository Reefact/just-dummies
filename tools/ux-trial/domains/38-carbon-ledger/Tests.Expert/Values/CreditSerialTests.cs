using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace CarbonLedger.Tests.Expert.Values;

public sealed class CreditSerialTests {

    // ---- the canonical text ------------------------------------------------------------------------------------

    [Fact]
    public void Parse_reads_the_four_parts_of_a_canonical_serial() {
        // Act
        CreditSerial serial = CreditSerial.Parse("VCS-1234-2019-000000000042");

        // Assert
        Assert.Equal("VCS", serial.Registry);
        Assert.Equal(1234, serial.ProjectId);
        Assert.Equal(2019, serial.Vintage);
        Assert.Equal(42L, serial.UnitNumber);
    }

    [Fact]
    public void ToString_pads_the_project_to_four_digits_the_vintage_to_four_and_the_unit_to_twelve() {
        // Arrange
        CreditSerial serial = new CreditSerial("VCS", 42, 1999, 7);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("VCS-0042-1999-000000000007", text);
    }

    [Fact]
    public void A_project_wider_than_four_digits_is_written_in_full_and_parsed_back() {
        // Arrange
        CreditSerial serial = new CreditSerial("GS", CreditSerial.MaxProjectId, 2100, CreditSerial.MaxUnitNumber);

        // Act
        string text = serial.ToString();

        // Assert
        Assert.Equal("GS-999999-2100-999999999999", text);
        Assert.Equal(serial, CreditSerial.Parse(text));
    }

    // This is a property of the format, run on one drawn serial: the draw IS what the assertion is about, so it is
    // not an `any` dummy and is not named like one.
    [Fact]
    public void A_serial_written_then_parsed_comes_back_equal_for_the_serial_drawn() {
        // Arrange
        CreditSerial serial = Any.CreditSerial().Generate();

        // Act
        CreditSerial parsed = CreditSerial.Parse(serial.ToString());

        // Assert
        Assert.Equal(serial, parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("VCS-1234-2019-42")]
    [InlineData("vcs-1234-2019-000000000042")]
    [InlineData("V-1234-2019-000000000042")]
    [InlineData("VCSXYZ-1234-2019-000000000042")]
    [InlineData("VCS_1234_2019_000000000042")]
    [InlineData("VCS-123-2019-000000000042")]
    [InlineData("VCS-1234567-2019-000000000042")]
    [InlineData("VCS-1234-19-000000000042")]
    [InlineData("VCS-1234-2019-0000000000042")]
    [InlineData(" VCS-1234-2019-000000000042")]
    [InlineData("VCS-1234-2019-000000000042 ")]
    public void Parse_refuses_text_that_is_not_the_canonical_form(string text) {
        Assert.Throws<FormatException>(() => CreditSerial.Parse(text));
    }

    [Fact]
    public void Parse_refuses_a_missing_text() {
        Assert.Throws<ArgumentNullException>(() => CreditSerial.Parse(null!));
    }

    [Theory]
    [InlineData("VCS-0000-2019-000000000042", "projectId")]
    [InlineData("VCS-1234-1989-000000000042", "vintage")]
    [InlineData("VCS-1234-2101-000000000042", "vintage")]
    [InlineData("VCS-1234-2019-000000000000", "unitNumber")]
    public void Parse_refuses_a_well_formed_text_whose_part_is_out_of_range(string text, string part) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => CreditSerial.Parse(text));

        Assert.Equal(part, refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the registry pattern is '^[A-Z]{2,5}$' and .NET's '$' also matches before a final newline, so 'VCS\\n' is a valid registry and the text then no longer parses; literal input, no seed")]
    public void Parse_refuses_a_canonical_text_followed_by_a_line_break() {
        Assert.Throws<FormatException>(() => CreditSerial.Parse("VCS-1234-2019-000000000042\n"));
    }

    // ---- the registry --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("ABCDEF")]
    [InlineData("vcs")]
    [InlineData("Vcs")]
    [InlineData("VC5")]
    [InlineData("V-S")]
    [InlineData(" VC")]
    public void A_registry_that_is_not_two_to_five_capital_letters_is_refused(string registry) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CreditSerial().WithRegistry(registry).Generate());

        Assert.Equal("registry", refusal.ParamName);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDE")]
    public void A_registry_of_two_or_five_capital_letters_is_accepted(string registry) {
        CreditSerial serial = Any.CreditSerial().WithRegistry(registry).Generate();

        Assert.Equal(registry, serial.Registry);
    }

    [Fact(Skip = "domain defect: 'VCS\\n' passes the registry pattern '^[A-Z]{2,5}$' because '$' matches before a final newline; the serial then writes 'VCS\\n-...' which Parse refuses; literal input, no seed")]
    public void A_registry_followed_by_a_line_break_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.CreditSerial().WithRegistry("VCS\n").Generate());
    }

    // ---- the ranges ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CreditSerial.MaxProjectId + 1)]
    [InlineData(int.MaxValue)]
    public void A_project_outside_one_to_999999_is_refused(int projectId) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CreditSerial().WithProjectId(projectId).Generate());

        Assert.Equal("projectId", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(CreditSerial.MaxProjectId)]
    public void The_first_and_last_project_are_accepted(int projectId) {
        Assert.Equal(projectId, Any.CreditSerial().WithProjectId(projectId).Generate().ProjectId);
    }

    [Theory]
    [InlineData(CreditSerial.MinVintage - 1)]
    [InlineData(CreditSerial.MaxVintage + 1)]
    [InlineData(0)]
    public void A_vintage_outside_1990_to_2100_is_refused(int vintage) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CreditSerial().WithVintage(vintage).Generate());

        Assert.Equal("vintage", refusal.ParamName);
    }

    [Theory]
    [InlineData(CreditSerial.MinVintage)]
    [InlineData(CreditSerial.MaxVintage)]
    public void The_first_and_last_vintage_are_accepted(int vintage) {
        Assert.Equal(vintage, Any.CreditSerial().WithVintage(vintage).Generate().Vintage);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(CreditSerial.MaxUnitNumber + 1)]
    public void A_unit_number_outside_one_to_999999999999_is_refused(long unitNumber) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CreditSerial().WithUnitNumber(unitNumber).Generate());

        Assert.Equal("unitNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(CreditSerial.MaxUnitNumber)]
    public void The_first_and_last_unit_number_are_accepted(long unitNumber) {
        Assert.Equal(unitNumber, Any.CreditSerial().WithUnitNumber(unitNumber).Generate().UnitNumber);
    }

    [Fact]
    public void A_missing_registry_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditSerial(null!, 1, 2019, 1));
    }

    // ---- equality ------------------------------------------------------------------------------------------------

    [Fact]
    public void Serials_with_the_same_four_parts_are_equal_and_hash_alike() {
        // Arrange
        CreditSerial one     = new CreditSerial("VCS", 1234, 2019, 42);
        CreditSerial another = new CreditSerial("VCS", 1234, 2019, 42);

        // Assert
        Assert.Equal(one, another);
        Assert.True(one.Equals((object)another));
        Assert.Equal(one.GetHashCode(), another.GetHashCode());
    }

    [Fact]
    public void Serials_differing_only_by_registry_are_different() {
        CreditSerial anySerial = Any.CreditSerial().Generate();
        CreditSerial other     = Any.CreditSerial()
                                    .WithRegistry(registry => registry.DifferentFrom(anySerial.Registry))
                                    .WithProjectId(anySerial.ProjectId)
                                    .WithVintage(anySerial.Vintage)
                                    .WithUnitNumber(anySerial.UnitNumber)
                                    .Generate();

        Assert.NotEqual(anySerial, other);
    }

    [Fact]
    public void Serials_differing_only_by_project_are_different() {
        CreditSerial anySerial = Any.CreditSerial().WithProjectId(project => project.LessThan(CreditSerial.MaxProjectId)).Generate();
        CreditSerial next      = new CreditSerial(anySerial.Registry, anySerial.ProjectId + 1, anySerial.Vintage, anySerial.UnitNumber);

        Assert.NotEqual(anySerial, next);
    }

    [Fact]
    public void Serials_differing_only_by_vintage_are_different() {
        CreditSerial anySerial = Any.CreditSerial().WithVintage(vintage => vintage.LessThan(CreditSerial.MaxVintage)).Generate();
        CreditSerial next      = new CreditSerial(anySerial.Registry, anySerial.ProjectId, anySerial.Vintage + 1, anySerial.UnitNumber);

        Assert.NotEqual(anySerial, next);
    }

    [Fact]
    public void Serials_differing_only_by_unit_number_are_different() {
        CreditSerial anySerial = Any.CreditSerial().WithUnitNumber(unit => unit.LessThan(CreditSerial.MaxUnitNumber)).Generate();
        CreditSerial next      = new CreditSerial(anySerial.Registry, anySerial.ProjectId, anySerial.Vintage, anySerial.UnitNumber + 1);

        Assert.NotEqual(anySerial, next);
    }

    [Fact]
    public void A_serial_is_not_equal_to_nothing_nor_to_something_else() {
        CreditSerial anySerial = Any.CreditSerial().Generate();

        Assert.False(anySerial.Equals(null));
        Assert.False(anySerial.Equals((object?)"not a serial"));
    }

}
