// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SubjectCasebook --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SubjectCasebook" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySubjectCasebook : IAny<SubjectCasebook> {

    private IAny<ScreeningNumber> _screeningNumber;
    private IAny<string?>         _initials;
    private IAny<DateOnly>        _consentDate;
    private IAny<VisitSchedule>   _schedule;

    private AnyScreeningNumber _screeningNumberRecipe;
    private AnyPattern          _initialsRecipe;
    private AnyDateOnly        _consentDateRecipe;
    private AnyVisitSchedule   _scheduleRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySubjectCasebook()
        : this(screeningNumber: AnyValidScreeningNumber(),
               initials:        AnyValidInitials(),
               consentDate:     AnyValidConsentDate(),
               schedule:        AnyValidSchedule()) { }

    private static AnyScreeningNumber AnyValidScreeningNumber() {
        return new AnyScreeningNumber();
    }

    private static AnyPattern AnyValidInitials() {
        return Any.StringMatching("[A-Z]{2,3}");
    }

    private static AnyDateOnly AnyValidConsentDate() {
        return TrialCalendar.Date();
    }

    private static AnyVisitSchedule AnyValidSchedule() {
        return new AnyVisitSchedule();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySubjectCasebook(AnyScreeningNumber screeningNumber,
                               AnyPattern          initials,
                               AnyDateOnly        consentDate,
                               AnyVisitSchedule   schedule) {
        _screeningNumber       = screeningNumber;
        _screeningNumberRecipe = screeningNumber;
        _initials              = initials.OrNull();
        _initialsRecipe        = initials;
        _consentDate           = consentDate;
        _consentDateRecipe     = consentDate;
        _schedule              = schedule;
        _scheduleRecipe        = schedule;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySubjectCasebook(AnySubjectCasebook from) {
        _screeningNumber       = from._screeningNumber;
        _screeningNumberRecipe = from._screeningNumberRecipe;
        _initials              = from._initials;
        _initialsRecipe        = from._initialsRecipe;
        _consentDate           = from._consentDate;
        _consentDateRecipe     = from._consentDateRecipe;
        _schedule              = from._schedule;
        _scheduleRecipe        = from._scheduleRecipe;
    }

    /// <summary>Pins <c>screeningNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySubjectCasebook WithScreeningNumber(ScreeningNumber value) {
        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._screeningNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>screeningNumber</c> is drawn from — <c>AnyValidScreeningNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyScreeningNumber, AnyScreeningNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySubjectCasebook WithScreeningNumber(Func<AnyScreeningNumber, AnyScreeningNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._screeningNumberRecipe = configure(_screeningNumberRecipe);
        copy._screeningNumber       = copy._screeningNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>initials</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySubjectCasebook WithInitials(string? value) {
        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._initials = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>initials</c> is drawn from — <c>AnyValidInitials()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySubjectCasebook WithInitials(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._initialsRecipe = configure(_initialsRecipe);
        copy._initials       = copy._initialsRecipe;

        return copy;
    }

    /// <summary>Pins <c>initials</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnySubjectCasebook WithoutInitials() {
        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._initials = Any.Fixed<string?>(null);

        return copy;
    }

    /// <summary>Pins <c>consentDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySubjectCasebook WithConsentDate(DateOnly value) {
        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._consentDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>consentDate</c> is drawn from — <c>AnyValidConsentDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySubjectCasebook WithConsentDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._consentDateRecipe = configure(_consentDateRecipe);
        copy._consentDate       = copy._consentDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>schedule</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySubjectCasebook WithSchedule(VisitSchedule value) {
        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._schedule = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>schedule</c> is drawn from — <c>AnyValidSchedule()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVisitSchedule, AnyVisitSchedule&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySubjectCasebook WithSchedule(Func<AnyVisitSchedule, AnyVisitSchedule> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySubjectCasebook copy = new AnySubjectCasebook(this);

        copy._scheduleRecipe = configure(_scheduleRecipe);
        copy._schedule       = copy._scheduleRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SubjectCasebook" />.</summary>
    public SubjectCasebook Generate() {
        return new SubjectCasebook(_screeningNumber.Generate(),
                                   _initials.Generate(),
                                   _consentDate.Generate(),
                                   _schedule.Generate());
    }

}
