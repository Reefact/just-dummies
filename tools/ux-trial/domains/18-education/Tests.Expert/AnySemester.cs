// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Semester --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Semester" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySemester : IAny<Semester> {

    private IAny<int>                _academicYear;
    private IAny<Term>               _term;

    private AnyInt32      _academicYearRecipe;
    private AnyEnum<Term> _termRecipe;
    private AnyDateOnly   _startRecipe;
    private AnyDateOnly   _endRecipe;

    private DateOnly? _startPin;
    private DateOnly? _endPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySemester()
        : this(academicYear: AnyValidAcademicYear(),
               term:         AnyValidTerm(),
               start:        AnyValidStart(),
               end:          AnyValidEnd()) { }

    private static AnyInt32 AnyValidAcademicYear() {
        return Any.Int32().Between(2000, 2100);
    }

    private static AnyEnum<Term> AnyValidTerm() {
        return Any.Enum<Term>();
    }

    private static AnyDateOnly AnyValidStart() {
        return Any.DateOnly();
    }

    private static AnyDateOnly AnyValidEnd() {
        return Any.DateOnly();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySemester(AnyInt32      academicYear,
                        AnyEnum<Term> term,
                        AnyDateOnly   start,
                        AnyDateOnly   end) {
        _academicYear       = academicYear;
        _academicYearRecipe = academicYear;
        _term               = term;
        _termRecipe         = term;
        _startRecipe        = start;
        _endRecipe          = end;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySemester(AnySemester from) {
        _academicYear       = from._academicYear;
        _academicYearRecipe = from._academicYearRecipe;
        _term               = from._term;
        _termRecipe         = from._termRecipe;
        _startRecipe        = from._startRecipe;
        _startPin           = from._startPin;
        _endRecipe          = from._endRecipe;
        _endPin             = from._endPin;
    }

    /// <summary>Draws <c>start</c> and <c>end</c> together, <c>start</c> always before <c>end</c> as the constructor requires, each from its current recipe and pin.</summary>
    private (DateOnly, DateOnly) StartAndEnd(int startYear) {
        // The constructor ties the start's year to the academic year and the term: an autumn semester starts in
        // the academic year itself, a spring one in the next.
        AnyDateOnly startInItsYear = _startRecipe.Between(new DateOnly(startYear, 1, 1), new DateOnly(startYear, 12, 31));

        (DateOnly, DateOnly) pair = _startPin is { } start ? (start, _endPin ?? _endRecipe.After(start).Generate())
                                    : _endPin is { } end ? (startInItsYear.Before(end).Generate(), end)
                                    : Any.Pair(startInItsYear, _endRecipe).Having.Item1BeforeItem2().Generate();

        return pair;
    }

    /// <summary>Pins <c>academicYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySemester WithAcademicYear(int value) {
        AnySemester copy = new AnySemester(this);

        copy._academicYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>academicYear</c> is drawn from — <c>AnyValidAcademicYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySemester WithAcademicYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySemester copy = new AnySemester(this);

        copy._academicYearRecipe = configure(_academicYearRecipe);
        copy._academicYear       = copy._academicYearRecipe;

        return copy;
    }

    /// <summary>Pins <c>term</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySemester WithTerm(Term value) {
        AnySemester copy = new AnySemester(this);

        copy._term = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>term</c> is drawn from — <c>AnyValidTerm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Term&gt;, AnyEnum&lt;Term&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySemester WithTerm(Func<AnyEnum<Term>, AnyEnum<Term>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySemester copy = new AnySemester(this);

        copy._termRecipe = configure(_termRecipe);
        copy._term       = copy._termRecipe;

        return copy;
    }

    /// <summary>Pins <c>start</c> to a fixed value, which bounds <c>end</c>; its recipe is kept for a later refinement.</summary>
    public AnySemester WithStart(DateOnly value) {
        AnySemester copy = new AnySemester(this);

        copy._startPin    = value;
        
        return copy;
    }

    /// <summary>Refines the recipe <c>start</c> is drawn from — <c>AnyValidStart()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>end</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySemester WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySemester copy = new AnySemester(this);

        copy._startRecipe = configure(_startRecipe);
        copy._startPin    = null;
        
        return copy;
    }

    /// <summary>Pins <c>end</c> to a fixed value, which bounds <c>start</c>; its recipe is kept for a later refinement.</summary>
    public AnySemester WithEnd(DateOnly value) {
        AnySemester copy = new AnySemester(this);

        copy._endPin      = value;
        
        return copy;
    }

    /// <summary>Refines the recipe <c>end</c> is drawn from — <c>AnyValidEnd()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>start</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySemester WithEnd(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySemester copy = new AnySemester(this);

        copy._endRecipe   = configure(_endRecipe);
        copy._endPin      = null;
        
        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Semester" />.</summary>
    public Semester Generate() {
        int  academicYear = _academicYear.Generate();
        Term term         = _term.Generate();
        int  startYear    = term == Term.Spring ? academicYear + 1 : academicYear;

        (DateOnly start, DateOnly end) = StartAndEnd(startYear);

        return new Semester(academicYear,
                            term,
                            start,
                            end);
    }

}
