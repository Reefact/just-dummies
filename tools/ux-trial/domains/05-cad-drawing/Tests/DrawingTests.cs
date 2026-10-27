using Drafting.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Drafting.Tests;

[Reproducible]
public sealed class DrawingTests {

    private static readonly DateOnly ReleaseDay = new(2025, 3, 14);

    [Fact]
    public void Releasing_an_approved_drawing_records_the_release_day() {
        // Arrange
        Drawing anyDrawing = Arbitrary.Drawing()
                                      .WithIssuedOn(d => d.BeforeOrEqualTo(ReleaseDay))
                                      .WithDimensions(l => l.NonEmpty())
                                      .Generate();
        anyDrawing.Check();
        anyDrawing.Approve();

        // Act
        anyDrawing.Release(ReleaseDay);

        // Assert
        Assert.Equal(DrawingStatus.Released, anyDrawing.Status);
        Assert.Equal(ReleaseDay, anyDrawing.ReleasedOn);
    }

    [Fact]
    public void A_drawing_without_dimensions_cannot_be_checked() {
        // Arrange
        Drawing anyBlankDrawing = Arbitrary.Drawing().WithDimensions(l => l.Empty()).Generate();

        // Act
        Exception? refusal = Record.Exception(anyBlankDrawing.Check);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_drawing_cannot_be_released_the_day_before_it_was_issued() {
        // Arrange
        DateOnly issuedOn   = new(2024, 5, 10);
        Drawing  anyDrawing = Arbitrary.Drawing().WithIssuedOn(issuedOn).WithDimensions(l => l.NonEmpty()).Generate();
        anyDrawing.Check();
        anyDrawing.Approve();

        // Act
        Exception? refusal = Record.Exception(() => anyDrawing.Release(issuedOn.AddDays(-1)));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal(DrawingStatus.Approved, anyDrawing.Status);
    }

    [Fact]
    public void Revising_a_released_drawing_at_revision_H_reopens_it_as_a_draft_at_revision_J() {
        // Arrange: I is never a revision letter.
        Drawing anyDrawing = Arbitrary.Drawing()
                                      .WithReference(r => r.WithRevision('H'))
                                      .WithIssuedOn(d => d.BeforeOrEqualTo(ReleaseDay))
                                      .WithDimensions(l => l.NonEmpty())
                                      .Generate();
        anyDrawing.Check();
        anyDrawing.Approve();
        anyDrawing.Release(ReleaseDay);

        // Act
        anyDrawing.Revise();

        // Assert
        Assert.Equal('J', anyDrawing.Reference.Revision);
        Assert.Equal(DrawingStatus.Draft, anyDrawing.Status);
        Assert.Null(anyDrawing.ReleasedOn);
    }

    [Fact]
    public void Revision_Y_has_no_next_letter() {
        // Arrange
        DrawingReference anyLastRevision = Any.DrawingReference().WithRevision('Y').Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyLastRevision.NextRevision());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_draft_cannot_be_approved_without_being_checked() {
        // Arrange
        Drawing anyDraft = Arbitrary.Drawing().Generate();

        // Act
        Exception? refusal = Record.Exception(anyDraft.Approve);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

}
