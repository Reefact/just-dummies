using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class DrawingReferenceTests {

    private const string AllowedRevisions = "ABCDEFGHJKLMNPRTUVWY";

    public static TheoryData<char> AllowedRevisionLetters() {
        TheoryData<char> letters = new TheoryData<char>();
        foreach (char letter in AllowedRevisions) {
            letters.Add(letter);
        }

        return letters;
    }

    // ----- construction -----

    [Theory]
    [InlineData("MEC-04217", 'C', 2)]
    [InlineData("ELE-00001", 'A', 1)]
    [InlineData("HYD-99999", 'Y', 40)]
    public void A_reference_keeps_the_number_the_revision_and_the_sheet_it_was_built_from(string number, char revision, int sheet) {
        // Act
        DrawingReference reference = new DrawingReference(number, revision, sheet);

        // Assert
        Assert.Equal(number, reference.Number);
        Assert.Equal(revision, reference.Revision);
        Assert.Equal(sheet, reference.Sheet);
    }

    [Fact]
    public void A_number_cannot_be_null() {
        char anyRevision = AnyDrawingReference.ValidRevision().Generate();
        int  anySheet    = AnyDrawingReference.ValidSheet().Generate();

        Assert.Throws<ArgumentNullException>(() => new DrawingReference(null!, anyRevision, anySheet));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_number_cannot_be_blank(string blank) {
        char anyRevision = AnyDrawingReference.ValidRevision().Generate();
        int  anySheet    = AnyDrawingReference.ValidSheet().Generate();

        Assert.Throws<ArgumentException>(() => new DrawingReference(blank, anyRevision, anySheet));
    }

    [Theory]
    [InlineData("MEC-0421")]       // four digits
    [InlineData("MEC-042170")]     // six digits
    [InlineData("MEC04217")]       // no hyphen
    [InlineData("MEC_04217")]
    [InlineData("mec-04217")]      // lower case
    [InlineData("ABC-04217")]      // unknown discipline
    [InlineData("MECH-04217")]
    [InlineData("ME-04217")]
    [InlineData("MEC-0421A")]
    [InlineData(" MEC-04217")]
    [InlineData("MEC-04217 ")]
    [InlineData("MEC-04217-2")]
    public void A_malformed_number_is_refused_with_a_format_error(string malformed) {
        char anyRevision = AnyDrawingReference.ValidRevision().Generate();
        int  anySheet    = AnyDrawingReference.ValidSheet().Generate();

        Assert.Throws<FormatException>(() => new DrawingReference(malformed, anyRevision, anySheet));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes `^...$` in .NET, so \"MEC-04217\\n\" is accepted as a drawing number; use \\z")]
    public void A_number_cannot_end_with_a_line_feed() {
        Assert.Throws<FormatException>(() => new DrawingReference("MEC-04217\n", 'A', 1));
    }

    [Fact(Skip = "domain defect: `\\d` matches any Unicode decimal digit, so \"MEC-\\u0664\\u0662\\u0661\\u0667\\u0660\" is accepted as a drawing number; use [0-9]")]
    public void A_number_is_made_of_ASCII_digits() {
        Assert.Throws<FormatException>(() => new DrawingReference("MEC-٤٢١٧٠", 'A', 1));
    }

    [Theory]
    [MemberData(nameof(AllowedRevisionLetters))]
    public void Every_allowed_letter_is_a_revision(char revision) {
        string anyNumber = AnyDrawingReference.ValidNumber().Generate();
        int    anySheet  = AnyDrawingReference.ValidSheet().Generate();

        DrawingReference reference = new DrawingReference(anyNumber, revision, anySheet);

        Assert.Equal(revision, reference.Revision);
    }

    [Theory]
    [InlineData('I')]
    [InlineData('O')]
    [InlineData('Q')]
    [InlineData('S')]
    [InlineData('X')]
    [InlineData('Z')]
    [InlineData('@')]   // the character before A
    [InlineData('[')]   // the character after Z
    [InlineData('a')]
    [InlineData('y')]
    [InlineData('0')]
    [InlineData(' ')]
    [InlineData('\0')]
    public void A_letter_that_is_not_allowed_is_not_a_revision(char revision) {
        string anyNumber = AnyDrawingReference.ValidNumber().Generate();
        int    anySheet  = AnyDrawingReference.ValidSheet().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(anyNumber, revision, anySheet));

        Assert.Equal("revision", error.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void A_sheet_is_a_number_from_one_up(int sheet) {
        string anyNumber   = AnyDrawingReference.ValidNumber().Generate();
        char   anyRevision = AnyDrawingReference.ValidRevision().Generate();

        DrawingReference reference = new DrawingReference(anyNumber, anyRevision, sheet);

        Assert.Equal(sheet, reference.Sheet);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_sheet_below_one_is_refused(int sheet) {
        string anyNumber   = AnyDrawingReference.ValidNumber().Generate();
        char   anyRevision = AnyDrawingReference.ValidRevision().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(anyNumber, anyRevision, sheet));

        Assert.Equal("sheet", error.ParamName);
    }

    [Fact]
    public void The_number_is_checked_before_the_revision_and_the_sheet() {
        Assert.Throws<FormatException>(() => new DrawingReference("nonsense", 'Z', 0));
    }

    [Fact]
    public void The_revision_is_checked_before_the_sheet() {
        string anyNumber = AnyDrawingReference.ValidNumber().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new DrawingReference(anyNumber, 'Z', 0));

        Assert.Equal("revision", error.ParamName);
    }

    // ----- next revision -----

    [Theory]
    [InlineData('A', 'B')]
    [InlineData('G', 'H')]
    [InlineData('H', 'J')]   // I is skipped
    [InlineData('N', 'P')]   // O is skipped
    [InlineData('P', 'R')]   // Q is skipped
    [InlineData('R', 'T')]   // S is skipped
    [InlineData('W', 'Y')]   // X is skipped
    public void The_next_revision_is_the_next_allowed_letter(char current, char expected) {
        DrawingReference reference = Any.DrawingReference().WithRevision(current).Generate();

        DrawingReference next = reference.NextRevision();

        Assert.Equal(expected, next.Revision);
    }

    [Fact]
    public void The_next_revision_keeps_the_number_and_the_sheet() {
        DrawingReference reference = Any.DrawingReference().WithANextRevision().Generate();

        DrawingReference next = reference.NextRevision();

        Assert.Equal(reference.Number, next.Number);
        Assert.Equal(reference.Sheet, next.Sheet);
    }

    [Fact]
    public void The_next_revision_leaves_the_reference_it_comes_from_untouched() {
        DrawingReference reference = new DrawingReference("MEC-04217", 'C', 2);

        reference.NextRevision();

        Assert.Equal("MEC-04217 rev C sheet 2", reference.ToString());
    }

    [Fact]
    public void Revisions_walk_the_twenty_allowed_letters_in_order() {
        DrawingReference reference = Any.DrawingReference().WithRevision('A').Generate();
        string           walked    = reference.Revision.ToString();

        while (reference.Revision != 'Y') {
            reference =  reference.NextRevision();
            walked    += reference.Revision;
        }

        Assert.Equal(AllowedRevisions, walked);
    }

    [Fact]
    public void There_is_no_revision_after_Y() {
        DrawingReference reference = Any.DrawingReference().WithRevision('Y').Generate();

        Assert.Throws<InvalidOperationException>(() => reference.NextRevision());
    }

    // ----- text and equality -----

    [Fact]
    public void A_reference_reads_as_number_revision_and_sheet() {
        DrawingReference reference = new DrawingReference("MEC-04217", 'C', 2);

        Assert.Equal("MEC-04217 rev C sheet 2", reference.ToString());
    }

    [Fact]
    public void References_with_the_same_three_parts_are_equal() {
        DrawingReference reference = Any.DrawingReference().Generate();
        DrawingReference twin      = new DrawingReference(reference.Number, reference.Revision, reference.Sheet);

        Assert.Equal(reference, twin);
        Assert.True(reference.Equals((object)twin));
        Assert.Equal(reference.GetHashCode(), twin.GetHashCode());
    }

    [Fact]
    public void A_different_number_makes_a_different_reference() {
        DrawingReference reference = Any.DrawingReference().Generate();
        DrawingReference other     = Any.DrawingReference()
                                        .WithNumber(n => n.DifferentFrom(reference.Number))
                                        .WithRevision(reference.Revision)
                                        .WithSheet(reference.Sheet)
                                        .Generate();

        Assert.NotEqual(reference, other);
    }

    [Fact]
    public void A_different_revision_makes_a_different_reference() {
        DrawingReference reference = Any.DrawingReference().WithANextRevision().Generate();

        Assert.NotEqual(reference, reference.NextRevision());
    }

    [Fact]
    public void A_different_sheet_makes_a_different_reference() {
        DrawingReference firstSheet  = new DrawingReference("MEC-04217", 'C', 1);
        DrawingReference secondSheet = new DrawingReference("MEC-04217", 'C', 2);

        Assert.NotEqual(firstSheet, secondSheet);
    }

    [Fact]
    public void A_reference_is_not_equal_to_null_or_to_another_kind_of_object() {
        DrawingReference reference = Any.DrawingReference().Generate();

        Assert.False(reference.Equals((DrawingReference?)null));
        Assert.False(reference.Equals((object?)null));
        Assert.False(reference.Equals((object)reference.ToString()));
    }

}
