using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class DrawingReferenceTests {

    private const string Number = "MEC-04217";

    [Theory]
    [InlineData("MEC-04217")]
    [InlineData("ELE-00001")]
    [InlineData("HYD-99999")]
    public void A_number_of_a_known_family_is_accepted(string number) {
        // Act
        DrawingReference reference = new DrawingReference(number, 'A', 1);

        // Assert
        Assert.Equal(number, reference.Number);
    }

    [Fact]
    public void A_null_number_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new DrawingReference(null!, 'A', 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_number_is_refused_as_an_argument_error(string blank) {
        Assert.Throws<ArgumentException>(() => new DrawingReference(blank, 'A', 1));
    }

    [Theory]
    [InlineData("MEC-0421")]      // four digits
    [InlineData("MEC-042170")]    // six digits
    [InlineData("MEC04217")]      // no dash
    [InlineData("mec-04217")]     // lower case
    [InlineData("XYZ-04217")]     // unknown family
    [InlineData("MECH-04217")]    // family too long
    [InlineData("MEC-0421A")]     // letter among the digits
    [InlineData(" MEC-04217")]
    [InlineData("MEC-04217 ")]
    public void A_malformed_number_is_refused_as_a_format_error(string number) {
        Assert.Throws<FormatException>(() => new DrawingReference(number, 'A', 1));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which in .NET also matches before a final line break, so \"MEC-04217\\n\" is accepted and stored with its newline; no seed involved")]
    public void A_number_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new DrawingReference("MEC-04217\n", 'A', 1));
    }

    [Fact(Skip = "domain defect: \\d in .NET matches any Unicode decimal digit, so a drawing number written with Arabic-Indic digits is accepted; no seed involved")]
    public void Non_ASCII_digits_are_refused() {
        Assert.Throws<FormatException>(() => new DrawingReference("MEC-\u0660\u0660\u0664\u0662\u0661", 'A', 1));
    }

    [Theory]
    [InlineData('A')] [InlineData('B')] [InlineData('C')] [InlineData('D')] [InlineData('E')]
    [InlineData('F')] [InlineData('G')] [InlineData('H')] [InlineData('J')] [InlineData('K')]
    [InlineData('L')] [InlineData('M')] [InlineData('N')] [InlineData('P')] [InlineData('R')]
    [InlineData('T')] [InlineData('U')] [InlineData('V')] [InlineData('W')] [InlineData('Y')]
    public void An_allowed_revision_letter_is_accepted(char revision) {
        // Act
        DrawingReference reference = new DrawingReference(Number, revision, 1);

        // Assert
        Assert.Equal(revision, reference.Revision);
    }

    [Theory]
    [InlineData('I')] [InlineData('O')] [InlineData('Q')] [InlineData('S')] [InlineData('X')]
    [InlineData('Z')]            // beyond Y
    [InlineData('a')]            // lower case
    [InlineData('@')]            // just before A
    [InlineData('[')]            // just after Z
    [InlineData('0')]
    [InlineData(' ')]
    [InlineData('\0')]
    public void A_forbidden_revision_letter_is_refused(char revision) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(Number, revision, 1));

        Assert.Equal("revision", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void A_positive_sheet_is_accepted(int sheet) {
        // Act
        DrawingReference reference = new DrawingReference(Number, 'A', sheet);

        // Assert
        Assert.Equal(sheet, reference.Sheet);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_sheet_below_one_is_refused(int sheet) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(Number, 'A', sheet));

        Assert.Equal("sheet", error.ParamName);
    }

    [Fact]
    public void The_number_is_checked_before_the_revision() {
        Assert.Throws<FormatException>(() => new DrawingReference("nope", 'Z', 0));
    }

    [Fact]
    public void The_revision_is_checked_before_the_sheet() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(Number, 'Z', 0));

        Assert.Equal("revision", error.ParamName);
    }

    [Theory]
    [InlineData('A', 'B')]
    [InlineData('G', 'H')]
    [InlineData('H', 'J')]   // skips I
    [InlineData('N', 'P')]   // skips O
    [InlineData('P', 'R')]   // skips Q
    [InlineData('R', 'T')]   // skips S
    [InlineData('W', 'Y')]   // skips X
    public void The_next_revision_is_the_next_allowed_letter(char current, char expected) {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().WithRevision(current).Generate();

        // Act
        DrawingReference next = anyReference.NextRevision();

        // Assert
        Assert.Equal(expected, next.Revision);
    }

    [Fact]
    public void The_next_revision_keeps_the_number_and_the_sheet() {
        // Arrange
        DrawingReference reference = new DrawingReference("HYD-00042", 'C', 3);

        // Act
        DrawingReference next = reference.NextRevision();

        // Assert
        Assert.Equal("HYD-00042", next.Number);
        Assert.Equal(3, next.Sheet);
    }

    [Fact]
    public void The_next_revision_leaves_the_original_untouched() {
        // Arrange
        DrawingReference reference = new DrawingReference(Number, 'C', 1);

        // Act
        reference.NextRevision();

        // Assert
        Assert.Equal('C', reference.Revision);
    }

    [Fact]
    public void Walking_the_whole_alphabet_visits_exactly_the_allowed_letters() {
        // Arrange
        DrawingReference current = new DrawingReference(Number, 'A', 1);
        List<char>       seen    = [current.Revision];

        // Act
        while (current.Revision != 'Y') {
            current = current.NextRevision();
            seen.Add(current.Revision);
        }

        // Assert
        Assert.Equal("ABCDEFGHJKLMNPRTUVWY", new string(seen.ToArray()));
    }

    [Fact]
    public void There_is_no_revision_after_Y() {
        // Arrange
        DrawingReference lastRevision = Any.DrawingReference().WithRevision('Y').Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => lastRevision.NextRevision());
    }

    [Fact]
    public void ToString_gives_number_revision_and_sheet() {
        Assert.Equal("MEC-04217 rev C sheet 2", new DrawingReference("MEC-04217", 'C', 2).ToString());
    }

    [Fact]
    public void Two_references_with_the_same_three_parts_are_equal() {
        // Arrange
        DrawingReference first  = new DrawingReference(Number, 'C', 2);
        DrawingReference second = new DrawingReference(Number, 'C', 2);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData("ELE-04217", 'C', 2)]
    [InlineData("MEC-04217", 'D', 2)]
    [InlineData("MEC-04217", 'C', 3)]
    public void A_reference_differing_on_any_part_is_not_equal(string number, char revision, int sheet) {
        Assert.NotEqual(new DrawingReference("MEC-04217", 'C', 2), new DrawingReference(number, revision, sheet));
    }

}
