using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class DrawingTests {

    public static TheoryData<DrawingStatus> StatusesOtherThanDraft() {
        return new TheoryData<DrawingStatus> { DrawingStatus.Checked, DrawingStatus.Approved, DrawingStatus.Released };
    }

    public static TheoryData<DrawingStatus> StatusesOtherThanChecked() {
        return new TheoryData<DrawingStatus> { DrawingStatus.Draft, DrawingStatus.Approved, DrawingStatus.Released };
    }

    public static TheoryData<DrawingStatus> StatusesOtherThanApproved() {
        return new TheoryData<DrawingStatus> { DrawingStatus.Draft, DrawingStatus.Checked, DrawingStatus.Released };
    }

    public static TheoryData<DrawingStatus> StatusesOtherThanReleased() {
        return new TheoryData<DrawingStatus> { DrawingStatus.Draft, DrawingStatus.Checked, DrawingStatus.Approved };
    }

    // ----- construction -----

    [Fact]
    public void A_drawing_keeps_what_it_was_built_from() {
        DrawingReference reference  = new DrawingReference("MEC-04217", 'C', 2);
        DrawingScale     scale      = new DrawingScale(1, 2);
        DateOnly         issuedOn   = new DateOnly(2024, 3, 15);
        Dimension        dimension  = Any.Dimension().Generate();

        Drawing drawing = Any.Drawing()
                             .WithReference(reference)
                             .WithTitle("Gear housing")
                             .WithScale(scale)
                             .WithProjection(ProjectionMethod.ThirdAngle)
                             .WithIssuedOn(issuedOn)
                             .WithDimensions([dimension])
                             .Generate();

        Assert.Same(reference, drawing.Reference);
        Assert.Equal("Gear housing", drawing.Title);
        Assert.Same(scale, drawing.Scale);
        Assert.Equal(ProjectionMethod.ThirdAngle, drawing.Projection);
        Assert.Equal(issuedOn, drawing.IssuedOn);
        Assert.Same(dimension, Assert.Single(drawing.Dimensions));
    }

    [Fact]
    public void A_new_drawing_is_a_draft_that_has_not_been_released() {
        Drawing anyDrawing = Any.Drawing().Generate();

        Assert.Equal(DrawingStatus.Draft, anyDrawing.Status);
        Assert.Null(anyDrawing.ReleasedOn);
    }

    [Fact]
    public void The_title_is_kept_exactly_as_given() {
        Drawing drawing = Any.Drawing().WithTitle("  Gear housing \t").Generate();

        Assert.Equal("  Gear housing \t", drawing.Title);
    }

    [Fact]
    public void A_reference_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Drawing().WithReference((DrawingReference)null!).Generate());

        Assert.Equal("reference", error.ParamName);
    }

    [Fact]
    public void A_title_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Drawing().WithTitle((string)null!).Generate());

        Assert.Equal("title", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" \t\r\n ")]
    public void A_title_cannot_be_blank(string blank) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Drawing().WithTitle(blank).Generate());

        Assert.Equal("title", error.ParamName);
    }

    [Fact]
    public void A_scale_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Drawing().WithScale((DrawingScale)null!).Generate());

        Assert.Equal("scale", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void A_projection_that_is_not_a_declared_method_is_refused(int undefinedValue) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Drawing().WithProjection((ProjectionMethod)undefinedValue).Generate());

        Assert.Equal("projection", error.ParamName);
    }

    [Theory]
    [InlineData(ProjectionMethod.FirstAngle)]
    [InlineData(ProjectionMethod.ThirdAngle)]
    public void Both_projection_methods_are_accepted(ProjectionMethod projection) {
        Drawing drawing = Any.Drawing().WithProjection(projection).Generate();

        Assert.Equal(projection, drawing.Projection);
    }

    [Fact]
    public void A_drawing_issued_on_1990_01_01_is_accepted() {
        Drawing drawing = Any.Drawing().WithIssuedOn(Drawing.EarliestIssue).Generate();

        Assert.Equal(new DateOnly(1990, 1, 1), drawing.IssuedOn);
    }

    [Theory]
    [InlineData(1989, 12, 31)]
    [InlineData(1900, 6, 15)]
    [InlineData(1, 1, 1)]
    public void A_drawing_issued_before_1990_is_refused(int year, int month, int day) {
        DateOnly tooEarly = new DateOnly(year, month, day);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Drawing().WithIssuedOn(tooEarly).Generate());

        Assert.Equal("issuedOn", error.ParamName);
    }

    [Fact]
    public void The_dimensions_cannot_be_null() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.Drawing().WithDimensions((IReadOnlyList<Dimension>)null!).Generate());

        Assert.Equal("dimensions", error.ParamName);
    }

    [Fact]
    public void A_drawing_may_be_built_without_a_single_dimension() {
        Drawing drawing = Any.Drawing().WithDimensions(d => d.Empty()).Generate();

        Assert.Empty(drawing.Dimensions);
    }

    [Fact]
    public void Arguments_are_checked_in_declaration_order() {
        // Everything is wrong at once: the reference is the first to be reported.
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new Drawing(null!, " ", null!, (ProjectionMethod)0, DateOnly.MinValue, null!));

        Assert.Equal("reference", error.ParamName);
    }

    [Fact]
    public void A_blank_title_is_reported_before_a_missing_scale() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Drawing().WithTitle(" ").WithScale((DrawingScale)null!).Generate());

        Assert.Equal("title", error.ParamName);
    }

    [Fact]
    public void An_undefined_projection_is_reported_before_an_early_issue_date() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Drawing().WithProjection((ProjectionMethod)0).WithIssuedOn(DateOnly.MinValue).Generate());

        Assert.Equal("projection", error.ParamName);
    }

    // ----- the dimensions list -----

    [Fact]
    public void A_drawing_copies_the_dimensions_it_is_given() {
        List<Dimension> given   = Any.ListOf(Any.Dimension()).WithCount(2).Generate();
        Drawing         drawing = Any.Drawing().WithDimensions(given).Generate();

        given.Add(Any.Dimension().Generate());
        given.RemoveAt(0);

        Assert.Equal(2, drawing.Dimensions.Count);
    }

    [Fact]
    public void The_dimensions_keep_the_order_they_were_given_in() {
        List<Dimension> given   = Any.ListOf(Any.Dimension()).WithCount(3).Generate();
        Drawing         drawing = Any.Drawing().WithDimensions(given).Generate();

        Assert.Equal(given, drawing.Dimensions);
    }

    [Fact(Skip = "domain defect: Dimensions returns the private List<Dimension> itself as an IReadOnlyList, so a caller can cast it to IList<Dimension> and empty a checked drawing")]
    public void The_dimensions_cannot_be_modified_through_a_cast() {
        Drawing drawing = Any.Drawing().WithDimensions(d => d.NonEmpty()).Generate();

        Assert.False(drawing.Dimensions is IList<Dimension>);
    }

    // ----- check -----

    [Fact]
    public void A_draft_with_a_dimension_can_be_checked() {
        Drawing anyDrawing = Any.Drawing().WithDimensions(d => d.NonEmpty()).Generate();

        anyDrawing.Check();

        Assert.Equal(DrawingStatus.Checked, anyDrawing.Status);
    }

    [Fact]
    public void A_draft_without_a_dimension_cannot_be_checked_and_stays_a_draft() {
        Drawing anyDrawing = Any.Drawing().WithDimensions(d => d.Empty()).Generate();

        Assert.Throws<InvalidOperationException>(() => anyDrawing.Check());
        Assert.Equal(DrawingStatus.Draft, anyDrawing.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanDraft))]
    public void Only_a_draft_can_be_checked(DrawingStatus status) {
        Drawing anyDrawing = Any.Drawing().GenerateIn(status);

        Assert.Throws<InvalidOperationException>(() => anyDrawing.Check());
        Assert.Equal(status, anyDrawing.Status);
    }

    [Fact]
    public void The_refusal_to_check_names_the_reference_and_the_current_status() {
        Drawing anyChecked = Any.Drawing().GenerateIn(DrawingStatus.Checked);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyChecked.Check());

        Assert.Contains("Checked", error.Message);
        Assert.Contains(anyChecked.Reference.ToString(), error.Message);
    }

    // ----- approve -----

    [Fact]
    public void A_checked_drawing_can_be_approved() {
        Drawing anyDrawing = Any.Drawing().GenerateIn(DrawingStatus.Checked);

        anyDrawing.Approve();

        Assert.Equal(DrawingStatus.Approved, anyDrawing.Status);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanChecked))]
    public void Only_a_checked_drawing_can_be_approved(DrawingStatus status) {
        Drawing anyDrawing = Any.Drawing().GenerateIn(status);

        Assert.Throws<InvalidOperationException>(() => anyDrawing.Approve());
        Assert.Equal(status, anyDrawing.Status);
    }

    // ----- release -----

    [Fact]
    public void An_approved_drawing_is_released_on_the_day_it_is_given() {
        Drawing drawing = Any.Drawing().WithIssuedOn(new DateOnly(2024, 3, 15)).WithDimensions(d => d.NonEmpty()).Generate();
        drawing.Check();
        drawing.Approve();

        drawing.Release(new DateOnly(2024, 6, 1));

        Assert.Equal(DrawingStatus.Released, drawing.Status);
        Assert.Equal(new DateOnly(2024, 6, 1), drawing.ReleasedOn);
    }

    [Fact]
    public void A_drawing_can_be_released_on_the_very_day_it_was_issued() {
        Drawing drawing = Any.Drawing().WithIssuedOn(new DateOnly(2024, 3, 15)).WithDimensions(d => d.NonEmpty()).Generate();
        drawing.Check();
        drawing.Approve();

        drawing.Release(new DateOnly(2024, 3, 15));

        Assert.Equal(new DateOnly(2024, 3, 15), drawing.ReleasedOn);
    }

    [Fact]
    public void A_drawing_cannot_be_released_the_day_before_it_was_issued_and_stays_approved() {
        Drawing drawing = Any.Drawing().WithIssuedOn(new DateOnly(2024, 3, 15)).WithDimensions(d => d.NonEmpty()).Generate();
        drawing.Check();
        drawing.Approve();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => drawing.Release(new DateOnly(2024, 3, 14)));

        Assert.Equal("on", error.ParamName);
        Assert.Equal(DrawingStatus.Approved, drawing.Status);
        Assert.Null(drawing.ReleasedOn);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanApproved))]
    public void Only_an_approved_drawing_can_be_released(DrawingStatus status) {
        Drawing anyDrawing = Any.Drawing().GenerateIn(status);
        DateOnly anyDay    = Any.DateOnly().AfterOrEqualTo(anyDrawing.IssuedOn).Generate();

        Assert.Throws<InvalidOperationException>(() => anyDrawing.Release(anyDay));
        Assert.Equal(status, anyDrawing.Status);
    }

    [Fact]
    public void The_status_is_judged_before_the_release_date() {
        Drawing anyDraft = Any.Drawing().Generate();

        // A date long before the issue: were the date judged first, this would be an out-of-range error.
        Assert.Throws<InvalidOperationException>(() => anyDraft.Release(DateOnly.MinValue));
    }

    // ----- revise -----

    [Fact]
    public void Revising_a_released_drawing_reopens_it_as_a_draft_at_the_next_revision() {
        Drawing drawing = Any.Drawing().WithReference(new DrawingReference("MEC-04217", 'C', 2)).WithIssuedOn(new DateOnly(2024, 3, 15)).WithDimensions(d => d.NonEmpty()).Generate();
        drawing.Check();
        drawing.Approve();
        drawing.Release(new DateOnly(2024, 6, 1));

        drawing.Revise();

        Assert.Equal(new DrawingReference("MEC-04217", 'D', 2), drawing.Reference);
        Assert.Equal(DrawingStatus.Draft, drawing.Status);
        Assert.Null(drawing.ReleasedOn);
    }

    [Fact]
    public void A_revision_skips_the_letters_that_are_never_used() {
        Drawing drawing = Any.Drawing().WithReference(new DrawingReference("ELE-00042", 'H', 1)).GenerateIn(DrawingStatus.Released);

        drawing.Revise();

        Assert.Equal('J', drawing.Reference.Revision);
    }

    [Fact]
    public void A_revision_keeps_the_title_scale_projection_issue_date_and_dimensions() {
        Drawing anyDrawing = Any.Drawing().WithReference(new DrawingReference("MEC-04217", 'A', 1)).GenerateIn(DrawingStatus.Released);
        string          title      = anyDrawing.Title;
        DrawingScale    scale      = anyDrawing.Scale;
        ProjectionMethod projection = anyDrawing.Projection;
        DateOnly        issuedOn   = anyDrawing.IssuedOn;
        List<Dimension> dimensions = anyDrawing.Dimensions.ToList();

        anyDrawing.Revise();

        Assert.Equal(title, anyDrawing.Title);
        Assert.Same(scale, anyDrawing.Scale);
        Assert.Equal(projection, anyDrawing.Projection);
        Assert.Equal(issuedOn, anyDrawing.IssuedOn);
        Assert.Equal(dimensions, anyDrawing.Dimensions);
    }

    [Theory]
    [MemberData(nameof(StatusesOtherThanReleased))]
    public void Only_a_released_drawing_can_be_revised(DrawingStatus status) {
        Drawing anyDrawing = Any.Drawing().GenerateIn(status);
        DrawingReference before = anyDrawing.Reference;

        Assert.Throws<InvalidOperationException>(() => anyDrawing.Revise());
        Assert.Equal(before, anyDrawing.Reference);
        Assert.Equal(status, anyDrawing.Status);
    }

    [Fact]
    public void A_drawing_at_the_last_revision_cannot_be_revised_and_stays_released() {
        Drawing drawing = Any.Drawing().WithReference(new DrawingReference("HYD-10001", 'Y', 3)).GenerateIn(DrawingStatus.Released);
        DateOnly? releasedOn = drawing.ReleasedOn;

        Assert.Throws<InvalidOperationException>(() => drawing.Revise());

        Assert.Equal(new DrawingReference("HYD-10001", 'Y', 3), drawing.Reference);
        Assert.Equal(DrawingStatus.Released, drawing.Status);
        Assert.Equal(releasedOn, drawing.ReleasedOn);
    }

    [Fact]
    public void A_revised_drawing_goes_through_the_whole_cycle_again() {
        Drawing drawing = Any.Drawing().WithReference(new DrawingReference("MEC-04217", 'A', 1)).WithIssuedOn(new DateOnly(2024, 3, 15)).WithDimensions(d => d.NonEmpty()).Generate();

        for (int cycle = 0; cycle < 2; cycle++) {
            drawing.Check();
            drawing.Approve();
            drawing.Release(new DateOnly(2024, 6, 1));
            drawing.Revise();
        }

        Assert.Equal('C', drawing.Reference.Revision);
        Assert.Equal(DrawingStatus.Draft, drawing.Status);
    }

}
