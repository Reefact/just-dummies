// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalendarInterval --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalendarInterval" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalendarInterval : IAny<CalendarInterval> {

    private AnyOrderedPair<int> _startYearAndEndYear;

    private AnyInt32 _startYearRecipe;
    private AnyInt32 _endYearRecipe;

    private int? _startYearPin;
    private int? _endYearPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalendarInterval()
        : this(startYear: AnyValidStartYear(),
               endYear:   AnyValidEndYear()) { }

    private static AnyInt32 AnyValidStartYear() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
    }

    private static AnyInt32 AnyValidEndYear() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalendarInterval(AnyInt32 startYear,
                                AnyInt32 endYear) {
        _startYearRecipe     = startYear;
        _endYearRecipe       = endYear;
        _startYearAndEndYear = StartYearAndEndYear();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCalendarInterval(AnyCalendarInterval from) {
        _startYearAndEndYear = from._startYearAndEndYear;
        _startYearRecipe     = from._startYearRecipe;
        _startYearPin        = from._startYearPin;
        _endYearRecipe       = from._endYearRecipe;
        _endYearPin          = from._endYearPin;
    }

    /// <summary>Draws <c>startYear</c> and <c>endYear</c> together, <c>startYear</c> never after <c>endYear</c> as the constructor requires, each from its current recipe and pin.</summary>
    private AnyOrderedPair<int> StartYearAndEndYear() {
        AnyOrderedPair<int> pair = Any.OrderedPair(_startYearRecipe, _endYearRecipe);

        if (_startYearPin is { } startYear) { pair = pair.WithFirst(startYear); }
        if (_endYearPin is { } endYear) { pair = pair.WithSecond(endYear); }

        return pair;
    }

    /// <summary>Pins <c>startYear</c> to a fixed value, which bounds <c>endYear</c>; its recipe is kept for a later refinement.</summary>
    public AnyCalendarInterval WithStartYear(int value) {
        AnyCalendarInterval copy = new AnyCalendarInterval(this);

        copy._startYearPin        = value;
        copy._startYearAndEndYear = copy.StartYearAndEndYear();

        return copy;
    }

    /// <summary>Refines the recipe <c>startYear</c> is drawn from — <c>AnyValidStartYear()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>endYear</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalendarInterval WithStartYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalendarInterval copy = new AnyCalendarInterval(this);

        copy._startYearRecipe     = configure(_startYearRecipe);
        copy._startYearPin        = null;
        copy._startYearAndEndYear = copy.StartYearAndEndYear();

        return copy;
    }

    /// <summary>Pins <c>endYear</c> to a fixed value, which bounds <c>startYear</c>; its recipe is kept for a later refinement.</summary>
    public AnyCalendarInterval WithEndYear(int value) {
        AnyCalendarInterval copy = new AnyCalendarInterval(this);

        copy._endYearPin          = value;
        copy._startYearAndEndYear = copy.StartYearAndEndYear();

        return copy;
    }

    /// <summary>Refines the recipe <c>endYear</c> is drawn from — <c>AnyValidEndYear()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>startYear</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalendarInterval WithEndYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalendarInterval copy = new AnyCalendarInterval(this);

        copy._endYearRecipe       = configure(_endYearRecipe);
        copy._endYearPin          = null;
        copy._startYearAndEndYear = copy.StartYearAndEndYear();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CalendarInterval" />.</summary>
    public CalendarInterval Generate() {
        (int startYear, int endYear) = _startYearAndEndYear.Generate();

        return new CalendarInterval(startYear,
                                    endYear);
    }

}
