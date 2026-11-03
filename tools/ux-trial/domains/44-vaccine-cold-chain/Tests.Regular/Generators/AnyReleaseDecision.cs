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
    private IAny<IReadOnlyCollection<DecisionReason>> _reasons;
    private IAny<int>                                 _budgetConsumedMinutes;
    private IAny<BinLocation?>                        _quarantineBay;

    private AnySscc                 _palletRecipe;
    private AnyEnum<ReleaseOutcome> _outcomeRecipe;
    private AnyList<DecisionReason> _reasonsRecipe;
    private AnyInt32                _budgetConsumedMinutesRecipe;
    private AnyBinLocation          _quarantineBayRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReleaseDecision()
        : this(pallet:                AnyValidPallet(),
               outcome:               AnyValidOutcome(),
               reasons:               AnyValidReasons(),
               budgetConsumedMinutes: AnyValidBudgetConsumedMinutes(),
               quarantineBay:         AnyValidQuarantineBay()) { }

    private static AnySscc AnyValidPallet() {
        return new AnySscc();
    }

    private static AnyEnum<ReleaseOutcome> AnyValidOutcome() {
        // Drawn in Generate() together with the reasons and the bay, which the outcome decides.
        return Any.Enum<ReleaseOutcome>();
    }

    private static AnyList<DecisionReason> AnyValidReasons() {
        // A reason is stated once.
        return Any.ListOf(Any.Enum<DecisionReason>()).WithCountBetween(0, 8).Distinct();
    }

    private static AnyInt32 AnyValidBudgetConsumedMinutes() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyBinLocation AnyValidQuarantineBay() {
        // Quarantined pallets are kept at floor levels, 1 to 4.
        return new AnyBinLocation().WithLevel(level => level.Between(1, 4));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReleaseDecision(AnySscc                 pallet,
                               AnyEnum<ReleaseOutcome> outcome,
                               AnyList<DecisionReason> reasons,
                               AnyInt32                budgetConsumedMinutes,
                               AnyBinLocation          quarantineBay) {
        _pallet                      = pallet;
        _palletRecipe                = pallet;
        _outcome                     = outcome;
        _outcomeRecipe               = outcome;
        _reasons                     = reasons;
        _reasonsRecipe               = reasons;
        _budgetConsumedMinutes       = budgetConsumedMinutes;
        _budgetConsumedMinutesRecipe = budgetConsumedMinutes;
        _quarantineBay               = quarantineBay.OrNull();
        _quarantineBayRecipe         = quarantineBay;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReleaseDecision(AnyReleaseDecision from) {
        _pallet                      = from._pallet;
        _palletRecipe                = from._palletRecipe;
        _outcome                     = from._outcome;
        _outcomeRecipe               = from._outcomeRecipe;
        _reasons                     = from._reasons;
        _reasonsRecipe               = from._reasonsRecipe;
        _budgetConsumedMinutes       = from._budgetConsumedMinutes;
        _budgetConsumedMinutesRecipe = from._budgetConsumedMinutesRecipe;
        _quarantineBay               = from._quarantineBay;
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

    /// <summary>Pins <c>reasons</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithReasons(IReadOnlyCollection<DecisionReason> value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._reasons = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reasons</c> is drawn from — <c>AnyValidReasons()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;DecisionReason&gt;, AnyList&lt;DecisionReason&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReleaseDecision WithReasons(Func<AnyList<DecisionReason>, AnyList<DecisionReason>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._reasonsRecipe = configure(_reasonsRecipe);
        copy._reasons       = copy._reasonsRecipe;

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

    /// <summary>Pins <c>quarantineBay</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithQuarantineBay(BinLocation? value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._quarantineBay = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quarantineBay</c> is drawn from — <c>AnyValidQuarantineBay()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBinLocation, AnyBinLocation&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReleaseDecision WithQuarantineBay(Func<AnyBinLocation, AnyBinLocation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._quarantineBayRecipe = configure(_quarantineBayRecipe);
        copy._quarantineBay       = copy._quarantineBayRecipe;

        return copy;
    }

    /// <summary>Pins <c>quarantineBay</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyReleaseDecision WithoutQuarantineBay() {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._quarantineBay = Any.Fixed<BinLocation?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReleaseDecision" />.</summary>
    public ReleaseDecision Generate() {
        bool reasonsPinned = !ReferenceEquals(_reasons, _reasonsRecipe);
        bool outcomePinned = !ReferenceEquals(_outcome, _outcomeRecipe);

        // Released <=> no reason; Rejected needs a rejecting one; Quarantined needs reasons, none rejecting.
        IReadOnlyCollection<DecisionReason> reasons;
        ReleaseOutcome                      outcome;
        if (reasonsPinned) {
            reasons = _reasons.Generate();
            outcome = outcomePinned ? _outcome.Generate() : ReleaseDecision.OutcomeFor(reasons);
        } else {
            outcome = _outcome.Generate();
            reasons = ReasonsFor(outcome);
        }

        // A quarantine bay is given if, and only if, the pallet is quarantined.
        BinLocation? quarantineBay = outcome == ReleaseOutcome.Quarantined ? _quarantineBay.Generate() : null;

        return new ReleaseDecision(_pallet.Generate(),
                                   outcome,
                                   reasons,
                                   _budgetConsumedMinutes.Generate(),
                                   quarantineBay);
    }

    private static IReadOnlyCollection<DecisionReason> ReasonsFor(ReleaseOutcome outcome) {
        DecisionReason[] rejecting = [DecisionReason.HardLimitBreached, DecisionReason.StabilityBudgetExhausted, DecisionReason.PackExpired];

        switch (outcome) {
            case ReleaseOutcome.Released:
                return [];
            case ReleaseOutcome.Rejected:
                DecisionReason first  = Any.ElementOf(rejecting).Generate();
                List<DecisionReason> others = Any.ListOf(Any.Enum<DecisionReason>().Except(first)).WithCountBetween(0, 4).Distinct().Generate();
                others.Add(first);
                return others;
            default:
                return Any.ListOf(Any.Enum<DecisionReason>().Except(rejecting)).WithCountBetween(1, 5).Distinct().Generate();
        }
    }

}
