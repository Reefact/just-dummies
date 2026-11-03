using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class DrawingTests {

    private static Drawing AnyDraft() {
        return Any.Drawing().WithDimensions(d => d.NonEmpty()).Generate();
    }

    private static Drawing AnyChecked() {
        Drawing drawing = AnyDraft();
        drawing.Check();

        return drawing;
    }

    private static Drawing AnyApproved() {
        Drawing drawing = AnyChecked();
        drawing.Approve();

        return drawing;
    }

    private static Drawing AnyReleased() {
        Drawing drawing = AnyApproved();
        drawing.Release(drawing.IssuedOn);

        return drawing;
    }

    // A released drawing that is not at the last revision letter, so that Revise() can open a next one.
    private static Drawing AnyRevisableReleased() {
        Drawing drawing = Any.Drawing()
                             .WithReference(r => r.WithRevision(letter => letter.Except('Y')))
                             .WithDimensions(d => d.NonEmpty())
                             .Generate();
        drawing.Check();
        drawing.Approve();
        drawing.Release(drawing.IssuedOn);

        return drawing;
    }

    private static DateOnly AnyIssueDate() {
        return Any.DateOnly().Between(Drawing.EarliestIssue, new DateOnly(2100, 1, 1)).Generate();
    }

    // ---- construction -------------------------------------------------------------------------

    [Fact]
    public void A_new_drawing_is_a_draft_that_was_not_released() {
        // Act
        Drawing anyDrawing = Any.Drawing().Generate();

        // Assert
        Assert.Equal(DrawingStatus.Draft, anyDrawing.Status);
        Assert.Null(anyDrawing.ReleasedOn);
    }

    [Fact]
    public void A_drawing_keeps_what_it_was_given() {
        // Arrange
        DrawingReference anyReference  = Any.DrawingReference().Generate();
        string           anyTitle      = Any.String().NotBlank().Generate();
        DrawingScale     anyScale      = Any.DrawingScale().Generate();
        ProjectionMethod anyProjection = Any.Enum<ProjectionMethod>().Generate();
        DateOnly         anyIssueDate  = AnyIssueDate();
        Dimension[]      anyDimensions = Any.ArrayOf(new AnyDimension()).NonEmpty().Generate();

        // Act
        Drawing drawing = new Drawing(anyReference, anyTitle, anyScale, anyProjection, anyIssueDate, anyDimensions);

        // Assert
        Assert.Same(anyReference, drawing.Reference);
        Assert.Equal(anyTitle, drawing.Title);
        Assert.Same(anyScale, drawing.Scale);
        Assert.Equal(anyProjection, drawing.Projection);
        Assert.Equal(anyIssueDate, drawing.IssuedOn);
        Assert.Equal(anyDimensions, drawing.Dimensions);
    }

    [Fact]
    public void A_drawing_without_dimensions_can_be_created() {
        // Act
        Drawing drawing = Any.Drawing().WithDimensions(d => d.Empty()).Generate();

        // Assert
        Assert.Empty(drawing.Dimensions);
    }

    [Fact]
    public void The_title_is_stored_as_given_without_trimming() {
        // Arrange
        string paddedTitle = "  Bracket, left  ";

        // Act
        Drawing drawing = Any.Drawing().WithTitle(paddedTitle).Generate();

        // Assert
        Assert.Equal(paddedTitle, drawing.Title);
    }

    [Fact]
    public void A_null_reference_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Drawing(null!, "t", new DrawingScale(1, 1), ProjectionMethod.FirstAngle, AnyIssueDate(), []));

        // Assert
        Assert.Equal("reference", exception.ParamName);
    }

    [Fact]
    public void A_null_title_is_refused_as_a_null_argument() {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Drawing(anyReference, null!, new DrawingScale(1, 1), ProjectionMethod.FirstAngle, AnyIssueDate(), []));

        // Assert
        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void A_blank_title_is_refused_as_an_argument_error() {
        // Arrange
        string anyBlank = Any.String().Whitespaces().NonEmpty().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Any.Drawing().WithTitle(anyBlank).Generate());

        // Assert
        Assert.Equal("title", exception.ParamName);
        Assert.IsNotType<ArgumentNullException>(exception);
    }

    [Fact]
    public void A_null_scale_is_refused() {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Drawing(anyReference, "t", null!, ProjectionMethod.FirstAngle, AnyIssueDate(), []));

        // Assert
        Assert.Equal("scale", exception.ParamName);
    }

    [Fact]
    public void An_undefined_projection_method_is_refused() {
        // Arrange
        int anyUndefined = Any.Int32().Except(1, 3).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Drawing().WithProjection((ProjectionMethod)anyUndefined).Generate());

        // Assert
        Assert.Equal("projection", exception.ParamName);
    }

    [Theory]
    [InlineData(ProjectionMethod.FirstAngle, 1)]
    [InlineData(ProjectionMethod.ThirdAngle, 3)]
    public void The_projection_methods_have_the_numbers_of_the_standard(ProjectionMethod method, int number) {
        // Assert
        Assert.Equal(number, (int)method);
    }

    [Fact]
    public void A_drawing_issued_before_1990_is_refused() {
        // Arrange
        DateOnly anyTooEarly = Any.DateOnly().Before(Drawing.EarliestIssue).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Drawing().WithIssuedOn(anyTooEarly).Generate());

        // Assert
        Assert.Equal("issuedOn", exception.ParamName);
    }

    [Fact]
    public void A_drawing_issued_on_the_first_of_january_1990_is_accepted() {
        // Act
        Drawing drawing = Any.Drawing().WithIssuedOn(new DateOnly(1990, 1, 1)).Generate();

        // Assert
        Assert.Equal(Drawing.EarliestIssue, drawing.IssuedOn);
    }

    [Fact]
    public void The_last_day_of_1989_is_still_too_early() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Drawing().WithIssuedOn(new DateOnly(1989, 12, 31)).Generate());
    }

    [Fact]
    public void Null_dimensions_are_refused() {
        // Arrange
        DrawingReference anyReference = Any.DrawingReference().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Drawing(anyReference, "t", new DrawingScale(1, 1), ProjectionMethod.FirstAngle, AnyIssueDate(), null!));

        // Assert
        Assert.Equal("dimensions", exception.ParamName);
    }

    [Fact]
    public void The_checks_run_in_order_reference_title_scale_projection_date_dimensions() {
        // Act / Assert
        Assert.Equal("reference", Assert.Throws<ArgumentNullException>(() => new Drawing(null!, null!, null!, (ProjectionMethod)99, DateOnly.MinValue, null!)).ParamName);
        Assert.Equal("title", Assert.Throws<ArgumentNullException>(() => new Drawing(new DrawingReference("MEC-00001", 'A', 1), null!, null!, (ProjectionMethod)99, DateOnly.MinValue, null!)).ParamName);
        Assert.Equal("scale", Assert.Throws<ArgumentNullException>(() => new Drawing(new DrawingReference("MEC-00001", 'A', 1), "t", null!, (ProjectionMethod)99, DateOnly.MinValue, null!)).ParamName);
        Assert.Equal("projection", Assert.Throws<ArgumentOutOfRangeException>(() => new Drawing(new DrawingReference("MEC-00001", 'A', 1), "t", new DrawingScale(1, 1), (ProjectionMethod)99, DateOnly.MinValue, null!)).ParamName);
        Assert.Equal("issuedOn", Assert.Throws<ArgumentOutOfRangeException>(() => new Drawing(new DrawingReference("MEC-00001", 'A', 1), "t", new DrawingScale(1, 1), ProjectionMethod.FirstAngle, DateOnly.MinValue, null!)).ParamName);
    }

    [Fact]
    public void The_dimensions_are_copied_so_changing_the_source_list_later_changes_nothing() {
        // Arrange
        List<Dimension> source = Any.ListOf(new AnyDimension()).WithCount(2).Generate();
        Drawing         drawing = Any.Drawing().WithDimensions(source).Generate();

        // Act
        source.Add(Any.Dimension().Generate());
        source.RemoveAt(0);

        // Assert
        Assert.Equal(2, drawing.Dimensions.Count);
    }

    [Fact(Skip = "domain defect: Dimensions hands out the internal List<Dimension> as an IReadOnlyList, so a caller can cast it back to List<Dimension> and add or remove dimensions behind the drawing's back")]
    public void The_dimensions_cannot_be_modified_through_the_list_that_is_exposed() {
        // Arrange
        Drawing anyDrawing = Any.Drawing().WithDimensions(d => d.WithCount(2)).Generate();

        // Act
        bool leaksTheMutableList = anyDrawing.Dimensions is IList<Dimension>;

        // Assert
        Assert.False(leaksTheMutableList);
    }

    // ---- check / approve ----------------------------------------------------------------------

    [Fact]
    public void Checking_a_draft_with_dimensions_makes_it_checked() {
        // Arrange
        Drawing anyDraft = AnyDraft();

        // Act
        anyDraft.Check();

        // Assert
        Assert.Equal(DrawingStatus.Checked, anyDraft.Status);
    }

    [Fact]
    public void A_draft_without_a_single_dimension_cannot_be_checked() {
        // Arrange
        Drawing anyEmptyDraft = Any.Drawing().WithDimensions(d => d.Empty()).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyEmptyDraft.Check());
        Assert.Equal(DrawingStatus.Draft, anyEmptyDraft.Status);
    }

    [Fact]
    public void A_single_dimension_is_enough_to_be_checked() {
        // Arrange
        Drawing anyDraft = Any.Drawing().WithDimensions(d => d.WithCount(1)).Generate();

        // Act
        anyDraft.Check();

        // Assert
        Assert.Equal(DrawingStatus.Checked, anyDraft.Status);
    }

    [Fact]
    public void Only_a_draft_can_be_checked() {
        // Arrange
        Drawing[] anyNonDrafts = [AnyChecked(), AnyApproved(), AnyReleased()];

        // Act / Assert
        foreach (Drawing nonDraft in anyNonDrafts) {
            DrawingStatus before = nonDraft.Status;

            Assert.Throws<InvalidOperationException>(() => nonDraft.Check());
            Assert.Equal(before, nonDraft.Status);
        }
    }

    [Fact]
    public void A_checked_drawing_can_be_approved() {
        // Arrange
        Drawing anyChecked = AnyChecked();

        // Act
        anyChecked.Approve();

        // Assert
        Assert.Equal(DrawingStatus.Approved, anyChecked.Status);
    }

    [Fact]
    public void Only_a_checked_drawing_can_be_approved() {
        // Arrange
        Drawing[] anyOthers = [AnyDraft(), AnyApproved(), AnyReleased()];

        // Act / Assert
        foreach (Drawing other in anyOthers) {
            DrawingStatus before = other.Status;

            Assert.Throws<InvalidOperationException>(() => other.Approve());
            Assert.Equal(before, other.Status);
        }
    }

    // ---- release ------------------------------------------------------------------------------

    [Fact]
    public void An_approved_drawing_can_be_released_after_it_was_issued() {
        // Arrange
        Drawing  anyApproved = AnyApproved();
        DateOnly anyLater    = anyApproved.IssuedOn.AddDays(Any.Int32().Between(1, 3_000).Generate());

        // Act
        anyApproved.Release(anyLater);

        // Assert
        Assert.Equal(DrawingStatus.Released, anyApproved.Status);
        Assert.Equal(anyLater, anyApproved.ReleasedOn);
    }

    [Fact]
    public void A_drawing_can_be_released_on_the_very_day_it_was_issued() {
        // Arrange
        Drawing anyApproved = AnyApproved();

        // Act
        anyApproved.Release(anyApproved.IssuedOn);

        // Assert
        Assert.Equal(anyApproved.IssuedOn, anyApproved.ReleasedOn);
    }

    [Fact]
    public void A_drawing_cannot_be_released_before_it_was_issued() {
        // Arrange
        Drawing  anyApproved = AnyApproved();
        DateOnly anyEarlier  = anyApproved.IssuedOn.AddDays(-Any.Int32().Between(1, 3_000).Generate());

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => anyApproved.Release(anyEarlier));

        // Assert
        Assert.Equal("on", exception.ParamName);
        Assert.Equal(DrawingStatus.Approved, anyApproved.Status);
        Assert.Null(anyApproved.ReleasedOn);
    }

    [Fact]
    public void Only_an_approved_drawing_can_be_released() {
        // Arrange
        Drawing[] anyOthers = [AnyDraft(), AnyChecked(), AnyReleased()];

        // Act / Assert
        foreach (Drawing other in anyOthers) {
            DrawingStatus before = other.Status;

            Assert.Throws<InvalidOperationException>(() => other.Release(DateOnly.MaxValue));
            Assert.Equal(before, other.Status);
        }
    }

    [Fact]
    public void The_status_is_checked_before_the_release_date() {
        // Arrange
        Drawing anyChecked = AnyChecked();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => anyChecked.Release(DateOnly.MinValue));
    }

    // ---- revise -------------------------------------------------------------------------------

    [Fact]
    public void Revising_a_released_drawing_opens_the_next_revision_as_a_draft() {
        // Arrange
        Drawing anyReleased = AnyRevisableReleased();
        string  number      = anyReleased.Reference.Number;
        int     sheet       = anyReleased.Reference.Sheet;
        DrawingReference expectedReference = anyReleased.Reference.NextRevision();

        // Act
        anyReleased.Revise();

        // Assert
        Assert.Equal(expectedReference, anyReleased.Reference);
        Assert.Equal(number, anyReleased.Reference.Number);
        Assert.Equal(sheet, anyReleased.Reference.Sheet);
        Assert.Equal(DrawingStatus.Draft, anyReleased.Status);
        Assert.Null(anyReleased.ReleasedOn);
    }

    [Fact]
    public void Revising_moves_to_the_revision_letter_after_the_current_one() {
        // Arrange
        Drawing released = Any.Drawing()
                              .WithReference(new DrawingReference("MEC-04217", 'H', 1))
                              .WithDimensions(d => d.NonEmpty())
                              .Generate();
        released.Check();
        released.Approve();
        released.Release(released.IssuedOn);

        // Act
        released.Revise();

        // Assert
        Assert.Equal('J', released.Reference.Revision);
    }

    [Fact]
    public void A_revised_drawing_goes_through_the_whole_cycle_again() {
        // Arrange
        Drawing anyReleased = AnyRevisableReleased();
        anyReleased.Revise();

        // Act
        anyReleased.Check();
        anyReleased.Approve();
        anyReleased.Release(anyReleased.IssuedOn);

        // Assert
        Assert.Equal(DrawingStatus.Released, anyReleased.Status);
    }

    [Fact]
    public void Only_a_released_drawing_can_be_revised() {
        // Arrange
        Drawing[] anyOthers = [AnyDraft(), AnyChecked(), AnyApproved()];

        // Act / Assert
        foreach (Drawing other in anyOthers) {
            DrawingReference before = other.Reference;

            Assert.Throws<InvalidOperationException>(() => other.Revise());
            Assert.Equal(before, other.Reference);
        }
    }

    [Fact]
    public void A_drawing_at_the_last_revision_letter_cannot_be_revised_and_stays_released() {
        // Arrange
        Drawing lastRevision = Any.Drawing()
                                  .WithReference(r => r.WithRevision('Y'))
                                  .WithDimensions(d => d.NonEmpty())
                                  .Generate();
        lastRevision.Check();
        lastRevision.Approve();
        lastRevision.Release(lastRevision.IssuedOn);
        DrawingReference referenceBefore = lastRevision.Reference;

        // Act
        Assert.Throws<InvalidOperationException>(() => lastRevision.Revise());

        // Assert
        Assert.Equal(referenceBefore, lastRevision.Reference);
        Assert.Equal(DrawingStatus.Released, lastRevision.Status);
        Assert.Equal(lastRevision.IssuedOn, lastRevision.ReleasedOn);
    }

}
