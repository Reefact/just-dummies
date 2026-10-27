using Education.Domain;
using JustDummies;
using Xunit;

namespace Education.Tests;

// Deliberate edge cases: each test records what the library did, as observed.
public sealed class Probes {

    private static readonly decimal[] HalfPoints = Enumerable.Range(0, 41).Select(halfPoints => halfPoints / 2m).ToArray();

    private static int ScaleOf(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0xFF;

    [Fact]
    public void Crossing_decimal_bounds_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(0m, 20m).GreaterThan(25m));

        Assert.Equal("Cannot apply GreaterThan(25) because Between(0, 20) already requires values less than or equal to 20.", exception.Message);
    }

    [Fact]
    public void A_scale_with_no_value_left_in_the_range_is_refused() {
        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(0.21m, 0.29m).WithScale(1));

        Assert.Equal("Cannot apply WithScale(1) because no Decimal value WithScale(1) allows remains between 0.21 and 0.29.", exception.Message);
    }

    [Fact]
    public void WithScale_draws_exactly_that_many_decimal_places() {
        List<decimal> anyAmounts = Enumerable.Range(0, 2_000).Select(_ => Any.Decimal().Between(0m, 20m).WithScale(2).Generate()).ToList();

        Assert.All(anyAmounts, amount => Assert.Equal(2, ScaleOf(amount)));
    }

    [Fact]
    public void Bug_WithScale_is_not_applied_when_the_range_holds_a_single_value() {
        // Every ordinary draw of WithScale(2) carries exactly two decimal places (test above); a range of one value does not.
        decimal anyMaximumGrade = Any.Decimal().Between(20m, 20m).WithScale(1).Generate();

        Assert.Equal(1, ScaleOf(anyMaximumGrade));   // observed: 0, the value comes back as 20 rather than 20.0
    }

    [Fact]
    public void A_half_point_grid_has_no_constraint_of_its_own_so_it_is_written_as_a_pool() {
        // AnyDecimal has no MultipleOf(0.5m); WithScale(1) admits 12.3. The pool of 41 values is the only AnyDecimal spelling.
        List<decimal> anyGrades = Enumerable.Range(0, 500).Select(_ => Any.Decimal().OneOf(HalfPoints).Generate()).ToList();

        Assert.All(anyGrades, grade => Assert.Equal(0m, grade % 0.5m));
    }

    [Fact]
    public void Equal_bounds_draw_that_single_value() {
        Assert.Equal(20m, Any.Decimal().Between(20m, 20m).WithScale(1).Generate());
    }

    [Fact]
    public void The_whole_decimal_range_still_draws_an_ordinary_magnitude() {
        decimal anyValue = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).Generate();

        Assert.InRange(anyValue, -1_000_000m, 1_000_000m);
    }

    [Fact]
    public void A_scale_outside_0_to_28_is_an_argument_error() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().WithScale(29));
    }

    [Fact]
    public void A_set_of_more_distinct_grades_than_exist_is_refused_before_drawing() {
        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Decimal().OneOf(HalfPoints)).WithCount(42).Generate());

        Assert.Equal("The generator can produce only 41 distinct value(s), not enough for the 42 elements it must still supply.", exception.Message);
    }

    [Fact]
    public void All_41_distinct_grades_can_be_drawn_through_the_scaffolded_generator() {
        List<Grade> anyGrades = Any.ListOf(new AnyGrade()).WithCount(41).Distinct().Generate();

        Assert.Equal(HalfPoints, anyGrades.Select(grade => grade.Value).Order());
    }

    [Fact]
    public void A_42nd_distinct_grade_through_a_custom_generator_ends_in_a_bounded_redraw() {
        AnyGenerationException exception = Assert.Throws<AnyGenerationException>(
            () => Any.ListOf(new AnyGrade()).WithCount(42).Distinct().Generate());

        Assert.StartsWith("Could not generate a distinct collection of 42 elements: the element generator produced only 41 distinct value(s)", exception.Message);
    }

    [Fact]
    public void A_custom_generator_inside_a_collection_does_replay_from_the_scope_seed() {
        // The redraw message above says such values "are not reproducible from this seed alone". They are:
        // AnyGrade draws from the ambient source, which the scope pins.
        List<decimal> first  = new List<decimal>();
        List<decimal> second = new List<decimal>();

        Any.Reproducibly(1234, () => first.AddRange(Any.ListOf(new AnyGrade()).WithCount(20).Generate().Select(grade => grade.Value)));
        Any.Reproducibly(1234, () => second.AddRange(Any.ListOf(new AnyGrade()).WithCount(20).Generate().Select(grade => grade.Value)));

        Assert.Equal(first, second);
    }

    [Fact]
    public void The_last_DateOnly_can_be_drawn_and_nothing_after_it() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().After(DateOnly.MaxValue.AddDays(-1)).Generate());

        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateOnly().After(DateOnly.MaxValue));
        Assert.Equal("Cannot apply After(9999-12-31) because no DateOnly value satisfies it.", exception.Message);
    }

    [Fact]
    public void A_TimeOnly_range_across_midnight_is_an_argument_error() {
        Assert.Throws<ArgumentException>(() => Any.TimeOnly().Between(new TimeOnly(23, 0), new TimeOnly(1, 0)));
    }

    [Fact]
    public void A_granularity_with_no_slot_in_the_range_is_refused() {
        Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeOnly().Between(new TimeOnly(8, 1), new TimeOnly(8, 14)).WithGranularity(TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void The_student_number_pattern_is_accepted_with_its_anchors_and_named_group() {
        string anyNumber = Any.StringMatching(@"^STU-(?<year>19|20)\d{2}-\d{5}$").Generate();

        Assert.NotNull(new StudentNumber(anyNumber));
    }

    [Fact]
    public void A_lookahead_in_a_course_code_pattern_is_refused_by_name_and_position() {
        UnsupportedRegexException exception = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[A-Z]{3}(?!000)\d{3}"));

        Assert.Contains("negative lookahead '(?!…)' at position 8", exception.Message);
    }

    [Fact]
    public void A_seeded_context_replays_its_grades() {
        AnyContext once  = Any.WithSeed(42);
        AnyContext twice = Any.WithSeed(42);

        List<decimal> first  = Enumerable.Range(0, 10).Select(_ => once.Decimal().OneOf(HalfPoints).Generate()).ToList();
        List<decimal> second = Enumerable.Range(0, 10).Select(_ => twice.Decimal().OneOf(HalfPoints).Generate()).ToList();

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_seed_scope_replays_a_whole_enrolment_graph() {
        string first;
        string second;
        using (IDisposable scope = Any.UseSeed(7)) {
            first = Describe(Any.Enrolment().Generate());
        }
        using (IDisposable scope = Any.UseSeed(7)) {
            second = Describe(Any.Enrolment().Generate());
        }

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_reproducible_body_reports_a_seed_that_replays_its_draws() {
        string        report = string.Empty;
        List<decimal> failed = new List<decimal>();
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            failed.AddRange(Enumerable.Range(0, 5).Select(_ => new AnyGrade().Generate().Value));
            throw new InvalidOperationException("the body failed");
        }, report: message => report = message));

        int           seed     = int.Parse(System.Text.RegularExpressions.Regex.Match(report, @"seeded with (-?\d+)").Groups[1].Value);
        List<decimal> replayed = new List<decimal>();
        Any.Reproducibly(seed, () => replayed.AddRange(Enumerable.Range(0, 5).Select(_ => new AnyGrade().Generate().Value)));

        Assert.Equal(failed, replayed);
    }

    [Fact]
    public void A_refinement_that_ignores_its_base_silently_widens_it() {
        // The CLI page says a With… lambda "never widens the base". A lambda that ignores its argument does.
        AnyCourse anyCourseWithWidenedCredits = new AnyCourse().WithCredits(_ => Any.Int32());

        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => {
            for (int draw = 0; draw < 100; draw++) {
                anyCourseWithWidenedCredits.Generate();
            }
        });
    }

    [Fact]
    public void A_refinement_contradicting_the_base_is_refused_naming_both() {
        ConflictingAnyConstraintException exception = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enrolment().WithCourses(courses => courses.WithMinCount(7)));

        Assert.Equal("Cannot apply WithMinCount(7) because WithCountBetween(1, 6) already caps the count at 6.", exception.Message);
    }

    private static string Describe(Enrolment enrolment) =>
        $"{enrolment.Student.Number}|{enrolment.Student.FullName}|{enrolment.Student.BirthDate}|{enrolment.Semester.Start}|{enrolment.Semester.End}|"
      + string.Join(",", enrolment.Courses.Select(course => $"{course.Code}:{course.Credits}:{course.Title}"));
}
