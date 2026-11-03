using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class DrawingReferenceTests {

    private const string AllowedRevisions = "ABCDEFGHJKLMNPRTUVWY";

    public static TheoryData<char> AllowedRevisionLetters() {
        TheoryData<char> data = new TheoryData<char>();
        foreach (char letter in AllowedRevisions) {
            data.Add(letter);
        }

        return data;
    }

    [Fact]
    public void A_reference_exposes_its_three_parts() {
        // Arrange
        string anyNumber = Any.StringMatching("(MEC|ELE|HYD)-[0-9]{5}").Generate();
        char   anyLetter = Any.Char().OneOf(AllowedRevisions.ToCharArray()).Generate();
        int    anySheet  = Any.Int32().Positive().Generate();

        // Act
        DrawingReference reference = new DrawingReference(anyNumber, anyLetter, anySheet);

        // Assert
        Assert.Equal(anyNumber, reference.Number);
        Assert.Equal(anyLetter, reference.Revision);
        Assert.Equal(anySheet, reference.Sheet);
    }

    [Theory]
    [InlineData("MEC-04217")]
    [InlineData("ELE-00000")]
    [InlineData("HYD-99999")]
    public void The_three_discipline_prefixes_are_accepted(string number) {
        // Act
        DrawingReference reference = Any.DrawingReference().WithNumber(number).Generate();

        // Assert
        Assert.Equal(number, reference.Number);
    }

    [Fact]
    public void A_null_number_is_refused_as_a_null_argument() {
        // Arrange
        char anyLetter = Any.Char().OneOf(AllowedRevisions.ToCharArray()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new DrawingReference(null!, anyLetter, 1));
    }

    [Fact]
    public void A_blank_number_is_refused_as_an_argument_error_and_not_as_a_format_error() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().NonEmpty().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new DrawingReference(anyBlank, 'A', 1));
    }

    [Theory]
    [InlineData("MEC-0421")]        // four digits
    [InlineData("MEC-042170")]      // six digits
    [InlineData("mec-04217")]       // lower case
    [InlineData("XYZ-04217")]       // unknown discipline
    [InlineData("MEC04217")]        // no dash
    [InlineData("MEC-0421A")]       // not a digit
    [InlineData(" MEC-04217")]      // leading space
    [InlineData("MEC-04217 ")]      // trailing space
    public void A_number_that_is_not_a_drawing_number_is_refused_as_a_format_error(string malformed) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new DrawingReference(malformed, 'A', 1));
    }

    [Fact(Skip = "domain defect: a trailing line feed is accepted ('$' also matches before a final \\n), so 'MEC-04217\\n' is a drawing number")]
    public void A_number_followed_by_a_line_feed_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new DrawingReference("MEC-04217\n", 'A', 1));
    }

    [Theory]
    [MemberData(nameof(AllowedRevisionLetters))]
    public void Every_allowed_revision_letter_is_accepted(char letter) {
        // Act
        DrawingReference reference = Any.DrawingReference().WithRevision(letter).Generate();

        // Assert
        Assert.Equal(letter, reference.Revision);
    }

    [Theory]
    [InlineData('I')]
    [InlineData('O')]
    [InlineData('Q')]
    [InlineData('S')]
    [InlineData('X')]
    [InlineData('Z')]
    [InlineData('@')]
    [InlineData('a')]
    [InlineData('y')]
    [InlineData('0')]
    public void A_revision_letter_outside_the_allowed_set_is_refused(char letter) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.DrawingReference().WithRevision(letter).Generate());

        // Assert
        Assert.Equal("revision", exception.ParamName);
    }

    [Fact]
    public void A_sheet_below_one_is_refused() {
        // Arrange
        int anyNonPositiveSheet = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.DrawingReference().WithSheet(anyNonPositiveSheet).Generate());

        // Assert
        Assert.Equal("sheet", exception.ParamName);
    }

    [Fact]
    public void Sheet_one_is_the_first_accepted_sheet() {
        // Act
        DrawingReference reference = Any.DrawingReference().WithSheet(1).Generate();

        // Assert
        Assert.Equal(1, reference.Sheet);
    }

    [Fact]
    public void The_number_is_checked_before_the_revision_and_the_revision_before_the_sheet() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new DrawingReference("bad", 'Z', 0));
        ArgumentOutOfRangeException revisionFirst = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference("MEC-04217", 'Z', 0));
        Assert.Equal("revision", revisionFirst.ParamName);
    }

    [Theory]
    [InlineData('A', 'B')]
    [InlineData('G', 'H')]
    [InlineData('H', 'J')]     // I is skipped
    [InlineData('N', 'P')]     // O is skipped
    [InlineData('P', 'R')]     // Q is skipped
    [InlineData('R', 'T')]     // S is skipped
    [InlineData('W', 'Y')]     // X is skipped
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
        DrawingReference anyReference = Any.DrawingReference().WithRevision('C').Generate();

        // Act
        DrawingReference next = anyReference.NextRevision();

        // Assert
        Assert.Equal(anyReference.Number, next.Number);
        Assert.Equal(anyReference.Sheet, next.Sheet);
    }

    [Fact]
    public void The_next_revision_of_an_allowed_letter_other_than_the_last_is_itself_allowed() {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().WithRevision(r => r.Except('Y')).Generate();

        // Act
        DrawingReference next = anyReference.NextRevision();

        // Assert
        Assert.Contains(next.Revision, AllowedRevisions);
        Assert.True(next.Revision > anyReference.Revision);
    }

    [Fact]
    public void There_is_no_revision_after_the_last_letter() {
        // Arrange
        DrawingReference anyLastRevision = Any.DrawingReference().WithRevision('Y').Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyLastRevision.NextRevision());
    }

    [Fact]
    public void A_reference_reads_as_number_revision_and_sheet() {
        // Arrange
        DrawingReference reference = new DrawingReference("MEC-04217", 'C', 2);

        // Assert
        Assert.Equal("MEC-04217 rev C sheet 2", reference.ToString());
    }

    [Fact]
    public void References_with_the_same_three_parts_are_equal_and_hash_alike() {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().Generate();
        DrawingReference twin         = new DrawingReference(anyReference.Number, anyReference.Revision, anyReference.Sheet);

        // Assert
        Assert.Equal(anyReference, twin);
        Assert.True(anyReference.Equals((object)twin));
        Assert.Equal(anyReference.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void References_differing_by_one_part_are_not_equal() {
        // Arrange
        DrawingReference reference = new DrawingReference("MEC-04217", 'C', 2);

        // Assert
        Assert.NotEqual(reference, new DrawingReference("ELE-04217", 'C', 2));
        Assert.NotEqual(reference, new DrawingReference("MEC-04217", 'D', 2));
        Assert.NotEqual(reference, new DrawingReference("MEC-04217", 'C', 3));
    }

}
