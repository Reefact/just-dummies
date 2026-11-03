// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReleaseDecision --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReleaseDecision" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReleaseDecision : IAny<ReleaseDecision> {

    private IAny<Sscc>                                _pallet;
    private IAny<ReleaseOutcome>                      _outcome;
    private IAny<int>                                 _budgetConsumedMinutes;

    private AnySscc                 _palletRecipe;
    private AnyEnum<ReleaseOutcome> _outcomeRecipe;
    private AnyInt32                _budgetConsumedMinutesRecipe;
    private AnyBinLocation          _quarantineBayRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReleaseDecision()
        : this(pallet:                AnyValidPallet(),
               outcome:               AnyValidOutcome(),
               budgetConsumedMinutes: AnyValidBudgetConsumedMinutes(),
               quarantineBay:         AnyValidQuarantineBay()) { }

    private static AnySscc AnyValidPallet() {
        return new AnySscc();
    }

    private static AnyEnum<ReleaseOutcome> AnyValidOutcome() {
        return Any.Enum<ReleaseOutcome>();
    }

    private static AnyInt32 AnyValidBudgetConsumedMinutes() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyBinLocation AnyValidQuarantineBay() {
        return new AnyBinLocation().WithLevel(l => l.Between(1, 4));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReleaseDecision(AnySscc                 pallet,
                               AnyEnum<ReleaseOutcome> outcome,
                               AnyInt32                budgetConsumedMinutes,
                               AnyBinLocation          quarantineBay) {
        _pallet                      = pallet;
        _palletRecipe                = pallet;
        _outcome                     = outcome;
        _outcomeRecipe               = outcome;
        _budgetConsumedMinutes       = budgetConsumedMinutes;
        _budgetConsumedMinutesRecipe = budgetConsumedMinutes;
        _quarantineBayRecipe         = quarantineBay;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReleaseDecision(AnyReleaseDecision from) {
        _pallet                      = from._pallet;
        _palletRecipe                = from._palletRecipe;
        _outcome                     = from._outcome;
        _outcomeRecipe               = from._outcomeRecipe;
        _budgetConsumedMinutes       = from._budgetConsumedMinutes;
        _budgetConsumedMinutesRecipe = from._budgetConsumedMinutesRecipe;
        _quarantineBayRecipe         = from._quarantineBayRecipe;
    }

    /// <summary>Pins <c>pallet</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithPallet(Sscc value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._pallet = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pallet</c> is drawn from — <c>AnyValidPallet()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySscc, AnySscc&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReleaseDecision WithPallet(Func<AnySscc, AnySscc> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._palletRecipe = configure(_palletRecipe);
        copy._pallet       = copy._palletRecipe;

        return copy;
    }

    /// <summary>Pins <c>outcome</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithOutcome(ReleaseOutcome value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._outcome = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>outcome</c> is drawn from — <c>AnyValidOutcome()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ReleaseOutcome&gt;, AnyEnum&lt;ReleaseOutcome&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReleaseDecision WithOutcome(Func<AnyEnum<ReleaseOutcome>, AnyEnum<ReleaseOutcome>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._outcomeRecipe = configure(_outcomeRecipe);
        copy._outcome       = copy._outcomeRecipe;

        return copy;
    }

    /// <summary>Pins <c>budgetConsumedMinutes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithBudgetConsumedMinutes(int value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._budgetConsumedMinutes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>budgetConsumedMinutes</c> is drawn from — <c>AnyValidBudgetConsumedMinutes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReleaseDecision WithBudgetConsumedMinutes(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._budgetConsumedMinutesRecipe = configure(_budgetConsumedMinutesRecipe);
        copy._budgetConsumedMinutes       = copy._budgetConsumedMinutesRecipe;

        return copy;
    }

    private static readonly DecisionReason[] RejectingReasons = [DecisionReason.HardLimitBreached, DecisionReason.StabilityBudgetExhausted, DecisionReason.PackExpired];

    private static readonly DecisionReason[] HoldingReasons = [DecisionReason.ShortShelfLife, DecisionReason.SingleExcursionTooLong, DecisionReason.ArrivedOutOfRange, DecisionReason.IncompleteTemperatureRecord, DecisionReason.LoggerCalibrationLapsed];

    /// <summary>Produces one arbitrary <see cref="ReleaseDecision" />.</summary>
    public ReleaseDecision Generate() {
        // The outcome, the reasons and the quarantine bay are tied by the constructor: the outcome (pinned or drawn)
        // decides the other two. Released has no reason; Rejected has at least one rejecting reason, holding ones
        // allowed beside it; Quarantined has holding reasons only, and is the one outcome with a bay.
        ReleaseOutcome outcome = _outcome.Generate();
        List<DecisionReason> reasons = new List<DecisionReason>();
        BinLocation? bay = null;
        switch (outcome) {
            case ReleaseOutcome.Rejected:
                reasons.AddRange(Any.SetOf(Any.ElementOf(RejectingReasons)).WithCountBetween(1, RejectingReasons.Length).Generate());
                reasons.AddRange(Any.SetOf(Any.ElementOf(HoldingReasons)).WithMaxCount(HoldingReasons.Length).Generate());
                break;
            case ReleaseOutcome.Quarantined:
                reasons.AddRange(Any.SetOf(Any.ElementOf(HoldingReasons)).WithCountBetween(1, HoldingReasons.Length).Generate());
                bay = _quarantineBayRecipe.Generate();
                break;
        }

        return new ReleaseDecision(_pallet.Generate(),
                                   outcome,
                                   reasons,
                                   _budgetConsumedMinutes.Generate(),
                                   bay);
    }

}
