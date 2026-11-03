// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftRequest --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ShiftPlanning.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShiftRequest" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShiftRequest : IAny<ShiftRequest> {

    private IAny<SiteId>       _site;
    private IAny<DateOnly>     _startDate;
    private IAny<TimeOnly>     _startTime;
    private IAny<TimeSpan>     _wallClockLength;
    private IAny<OffsetChoice> _startChoice;
    private IAny<OffsetChoice> _endChoice;

    private AnySiteId             _siteRecipe;
    private AnyDateOnly           _startDateRecipe;
    private AnyTimeOnly           _startTimeRecipe;
    private AnyTimeSpan           _wallClockLengthRecipe;
    private AnyEnum<OffsetChoice> _startChoiceRecipe;
    private AnyEnum<OffsetChoice> _endChoiceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShiftRequest()
        : this(site:        AnyValidSite(),
               startDate:   AnyValidStartDate(),
               startTime:   AnyValidStartTime(),
               wallClockLength: AnyValidWallClockLength(),
               startChoice: AnyValidStartChoice(),
               endChoice:   AnyValidEndChoice()) { }

    private static AnySiteId AnyValidSite() {
        return new AnySiteId();
    }

    private static AnyDateOnly AnyValidStartDate() {
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static AnyTimeOnly AnyValidStartTime() {
        return Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeSpan AnyValidWallClockLength() {
        return Any.TimeSpan().Between(ShiftRequest.MinWallClockLength, ShiftRequest.MaxWallClockLength).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyEnum<OffsetChoice> AnyValidStartChoice() {
        return Any.Enum<OffsetChoice>();
    }

    private static AnyEnum<OffsetChoice> AnyValidEndChoice() {
        return Any.Enum<OffsetChoice>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShiftRequest(AnySiteId             site,
                            AnyDateOnly           startDate,
                            AnyTimeOnly           startTime,
                            AnyTimeSpan           wallClockLength,
                            AnyEnum<OffsetChoice> startChoice,
                            AnyEnum<OffsetChoice> endChoice) {
        _site              = site;
        _siteRecipe        = site;
        _startDate         = startDate;
        _startDateRecipe   = startDate;
        _startTime         = startTime;
        _startTimeRecipe   = startTime;
        _wallClockLength           = wallClockLength;
        _wallClockLengthRecipe     = wallClockLength;
        _startChoice       = startChoice;
        _startChoiceRecipe = startChoice;
        _endChoice         = endChoice;
        _endChoiceRecipe   = endChoice;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShiftRequest(AnyShiftRequest from) {
        _site              = from._site;
        _siteRecipe        = from._siteRecipe;
        _startDate         = from._startDate;
        _startDateRecipe   = from._startDateRecipe;
        _startTime         = from._startTime;
        _startTimeRecipe   = from._startTimeRecipe;
        _wallClockLength           = from._wallClockLength;
        _wallClockLengthRecipe     = from._wallClockLengthRecipe;
        _startChoice       = from._startChoice;
        _startChoiceRecipe = from._startChoiceRecipe;
        _endChoice         = from._endChoice;
        _endChoiceRecipe   = from._endChoiceRecipe;
    }

    /// <summary>Pins <c>site</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithSite(SiteId value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._site = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>site</c> is drawn from — <c>AnyValidSite()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySiteId, AnySiteId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithSite(Func<AnySiteId, AnySiteId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._siteRecipe = configure(_siteRecipe);
        copy._site       = copy._siteRecipe;

        return copy;
    }

    /// <summary>Pins <c>startDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithStartDate(DateOnly value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startDate</c> is drawn from — <c>AnyValidStartDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithStartDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startDateRecipe = configure(_startDateRecipe);
        copy._startDate       = copy._startDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>startTime</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithStartTime(TimeOnly value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startTime = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startTime</c> is drawn from — <c>AnyValidStartTime()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithStartTime(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startTimeRecipe = configure(_startTimeRecipe);
        copy._startTime       = copy._startTimeRecipe;

        return copy;
    }

    /// <summary>Pins <c>wallClockLength</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithWallClockLength(TimeSpan value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._wallClockLength = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>wallClockLength</c> is drawn from — <c>AnyValidWallClockLength()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeOnly, AnyTimeOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithWallClockLength(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._wallClockLengthRecipe = configure(_wallClockLengthRecipe);
        copy._wallClockLength       = copy._wallClockLengthRecipe;

        return copy;
    }

    /// <summary>Pins <c>startChoice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithStartChoice(OffsetChoice value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startChoice = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>startChoice</c> is drawn from — <c>AnyValidStartChoice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;OffsetChoice&gt;, AnyEnum&lt;OffsetChoice&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithStartChoice(Func<AnyEnum<OffsetChoice>, AnyEnum<OffsetChoice>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._startChoiceRecipe = configure(_startChoiceRecipe);
        copy._startChoice       = copy._startChoiceRecipe;

        return copy;
    }

    /// <summary>Pins <c>endChoice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShiftRequest WithEndChoice(OffsetChoice value) {
        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._endChoice = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>endChoice</c> is drawn from — <c>AnyValidEndChoice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;OffsetChoice&gt;, AnyEnum&lt;OffsetChoice&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShiftRequest WithEndChoice(Func<AnyEnum<OffsetChoice>, AnyEnum<OffsetChoice>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftRequest copy = new AnyShiftRequest(this);

        copy._endChoiceRecipe = configure(_endChoiceRecipe);
        copy._endChoice       = copy._endChoiceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ShiftRequest" />.</summary>
    public ShiftRequest Generate() {
        TimeOnly startTime = _startTime.Generate();
        TimeOnly endTime   = startTime.Add(_wallClockLength.Generate());

        return new ShiftRequest(_site.Generate(),
                                _startDate.Generate(),
                                startTime,
                                endTime,
                                _startChoice.Generate(),
                                _endChoice.Generate());
    }

}
