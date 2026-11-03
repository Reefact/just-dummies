// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VisitDefinition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VisitDefinition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVisitDefinition : IAny<VisitDefinition> {

    private IAny<string>              _code;
    private IAny<int>                 _targetDay;
    private IAny<int>                 _daysBefore;
    private IAny<int>                 _daysAfter;
    private IAny<IEnumerable<string>> _requiredAnalytes;

    private AnyPattern           _codeRecipe;
    private AnyInt32            _targetDayRecipe;
    private AnyInt32            _daysBeforeRecipe;
    private AnyInt32            _daysAfterRecipe;
    private AnySequence<string> _requiredAnalytesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVisitDefinition()
        : this(code:             AnyValidCode(),
               targetDay:        AnyValidTargetDay(),
               daysBefore:       AnyValidDaysBefore(),
               daysAfter:        AnyValidDaysAfter(),
               requiredAnalytes: AnyValidRequiredAnalytes()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching("[A-Z][A-Z0-9]{0,7}");
    }

    private static AnyInt32 AnyValidTargetDay() {
        return Any.Int32().Positive().LessThanOrEqualTo(VisitDefinition.MaxTargetDay);
    }

    private static AnyInt32 AnyValidDaysBefore() {
        return Any.Int32().Between(0, VisitDefinition.MaxToleranceDays);
    }

    private static AnyInt32 AnyValidDaysAfter() {
        return Any.Int32().Between(0, VisitDefinition.MaxToleranceDays);
    }

    private static AnySequence<string> AnyValidRequiredAnalytes() {
        return Any.SequenceOf(Any.StringMatching("[A-Z][A-Z0-9]{1,9}")).WithMaxCount(6).Distinct();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVisitDefinition(AnyPattern           code,
                               AnyInt32            targetDay,
                               AnyInt32            daysBefore,
                               AnyInt32            daysAfter,
                               AnySequence<string> requiredAnalytes) {
        _code                   = code;
        _codeRecipe             = code;
        _targetDay              = targetDay;
        _targetDayRecipe        = targetDay;
        _daysBefore             = daysBefore;
        _daysBeforeRecipe       = daysBefore;
        _daysAfter              = daysAfter;
        _daysAfterRecipe        = daysAfter;
        _requiredAnalytes       = requiredAnalytes;
        _requiredAnalytesRecipe = requiredAnalytes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVisitDefinition(AnyVisitDefinition from) {
        _code                   = from._code;
        _codeRecipe             = from._codeRecipe;
        _targetDay              = from._targetDay;
        _targetDayRecipe        = from._targetDayRecipe;
        _daysBefore             = from._daysBefore;
        _daysBeforeRecipe       = from._daysBeforeRecipe;
        _daysAfter              = from._daysAfter;
        _daysAfterRecipe        = from._daysAfterRecipe;
        _requiredAnalytes       = from._requiredAnalytes;
        _requiredAnalytesRecipe = from._requiredAnalytesRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitDefinition WithCode(string value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVisitDefinition WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>targetDay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitDefinition WithTargetDay(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._targetDay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>targetDay</c> is drawn from — <c>AnyValidTargetDay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVisitDefinition WithTargetDay(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._targetDayRecipe = configure(_targetDayRecipe);
        copy._targetDay       = copy._targetDayRecipe;

        return copy;
    }

    /// <summary>Pins <c>daysBefore</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitDefinition WithDaysBefore(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysBefore = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>daysBefore</c> is drawn from — <c>AnyValidDaysBefore()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVisitDefinition WithDaysBefore(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysBeforeRecipe = configure(_daysBeforeRecipe);
        copy._daysBefore       = copy._daysBeforeRecipe;

        return copy;
    }

    /// <summary>Pins <c>daysAfter</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitDefinition WithDaysAfter(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysAfter = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>daysAfter</c> is drawn from — <c>AnyValidDaysAfter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVisitDefinition WithDaysAfter(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysAfterRecipe = configure(_daysAfterRecipe);
        copy._daysAfter       = copy._daysAfterRecipe;

        return copy;
    }

    /// <summary>Pins <c>requiredAnalytes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVisitDefinition WithRequiredAnalytes(IEnumerable<string> value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._requiredAnalytes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>requiredAnalytes</c> is drawn from — <c>AnyValidRequiredAnalytes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;string&gt;, AnySequence&lt;string&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVisitDefinition WithRequiredAnalytes(Func<AnySequence<string>, AnySequence<string>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._requiredAnalytesRecipe = configure(_requiredAnalytesRecipe);
        copy._requiredAnalytes       = copy._requiredAnalytesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VisitDefinition" />.</summary>
    public VisitDefinition Generate() {
        // The window cannot open before day 1 (targetDay - daysBefore >= 1): the target is drawn first, then
        // daysBefore from its recipe kept under it. A WithDaysBefore(int) pin therefore does not reach the draw.
        int targetDay  = _targetDay.Generate();
        int daysBefore = _daysBeforeRecipe.LessThan(targetDay).Generate();

        return new VisitDefinition(_code.Generate(),
                                   targetDay,
                                   daysBefore,
                                   _daysAfter.Generate(),
                                   _requiredAnalytes.Generate());
    }

}
