// Started from `any generate ReleaseDecision`, then rewritten by hand. The outcome, the reasons and the quarantine bay
// hold one another up (released <=> no reason, rejected <=> a rejecting reason, a bay iff quarantined): drawn
// independently, the scaffolded recipe is refused by the constructor almost every time. Here the reasons follow the
// outcome (or the outcome follows pinned reasons), and the bay is drawn only for a quarantine.

using System;
using System.Collections.Generic;
using System.Linq;

using ColdChain.Domain;
using JustDummies;
using Warehouse.Domain;

namespace ColdChain.Tests.Expert.Generators;

public sealed partial class AnyReleaseDecision : IAny<ReleaseDecision> {

    private static readonly DecisionReason[] RejectingReasons = Enum.GetValues(typeof(DecisionReason)).Cast<DecisionReason>().Where(ReleaseDecision.IsRejecting).ToArray();
    private static readonly DecisionReason[] HoldingReasons   = Enum.GetValues(typeof(DecisionReason)).Cast<DecisionReason>().Where(reason => !ReleaseDecision.IsRejecting(reason)).ToArray();

    private IAny<Sscc>                           _pallet                = new AnySscc();
    private IAny<int>                            _budgetConsumedMinutes = Any.Int32().Between(0, StabilityBudget.MaxCumulativeMinutes * 2);
    private IAny<BinLocation>                    _quarantineBay         = new AnyBinLocation().WithLevel(level => level.LessThanOrEqualTo(4));
    private ReleaseOutcome?                      _outcome;
    private IReadOnlyCollection<DecisionReason>? _reasons;

    public AnyReleaseDecision() { }

    private AnyReleaseDecision(AnyReleaseDecision from) {
        _pallet                = from._pallet;
        _budgetConsumedMinutes = from._budgetConsumedMinutes;
        _quarantineBay         = from._quarantineBay;
        _outcome               = from._outcome;
        _reasons               = from._reasons;
    }

    public AnyReleaseDecision WithPallet(Sscc value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._pallet = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the outcome; the reasons are drawn to fit it, unless they are pinned too.</summary>
    public AnyReleaseDecision WithOutcome(ReleaseOutcome value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._outcome = value;

        return copy;
    }

    /// <summary>Pins the reasons; the outcome follows from them, unless it is pinned too.</summary>
    public AnyReleaseDecision WithReasons(params DecisionReason[] value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._reasons = value;

        return copy;
    }

    public AnyReleaseDecision WithBudgetConsumedMinutes(int value) {
        AnyReleaseDecision copy = new AnyReleaseDecision(this);

        copy._budgetConsumedMinutes = Any.Fixed(value);

        return copy;
    }

    public ReleaseDecision Generate() {
        ReleaseOutcome                    outcome = _outcome ?? (_reasons is null ? Any.Enum<ReleaseOutcome>().Generate() : ReleaseDecision.OutcomeFor(_reasons));
        IReadOnlyCollection<DecisionReason> reasons = _reasons ?? ReasonsFor(outcome);
        BinLocation?                      bay     = outcome == ReleaseOutcome.Quarantined ? _quarantineBay.Generate() : null;

        return new ReleaseDecision(_pallet.Generate(), outcome, reasons, _budgetConsumedMinutes.Generate(), bay);
    }

    private static IReadOnlyCollection<DecisionReason> ReasonsFor(ReleaseOutcome outcome) {
        switch (outcome) {
            case ReleaseOutcome.Released:
                return Array.Empty<DecisionReason>();
            case ReleaseOutcome.Quarantined:
                return Any.SetOf(Any.ElementOf(HoldingReasons)).WithCountBetween(1, HoldingReasons.Length).Generate();
            default:
                HashSet<DecisionReason> rejecting = Any.SetOf(Any.ElementOf(RejectingReasons)).WithCountBetween(1, RejectingReasons.Length).Generate();
                HashSet<DecisionReason> holding   = Any.SetOf(Any.ElementOf(HoldingReasons)).WithCountBetween(0, HoldingReasons.Length).Generate();

                rejecting.UnionWith(holding);

                return rejecting;
        }
    }

}
