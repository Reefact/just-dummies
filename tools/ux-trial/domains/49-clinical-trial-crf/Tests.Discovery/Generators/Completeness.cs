using System;

using JustDummies;

namespace ClinicalTrial.Domain;

// Variants a form needs to be signed: nothing left blank.

public sealed partial class AnyVitalSigns {

    /// <summary>Every sign resolved (observed or missing with a reason); a time of measurement whenever one is allowed.</summary>
    public AnyVitalSigns FullyResolved() {
        AnyVitalSigns copy = new AnyVitalSigns(this);

        copy._bloodPressure = Any.Combine(Any.OrderedPair(Any.Int32().Between(20, 200), Any.Int32().Between(50, 300), strict: true),
                                          Any.Enum<MissingReason>(),
                                          Any.Enum<MissingReason>(),
                                          Any.Boolean(),
                                          (pair, reasonA, reasonB, observed) => observed
                                                                                    ? ((Observed<int>?)Observed<int>.Of(pair.Second), (Observed<int>?)Observed<int>.Of(pair.First))
                                                                                    : (Observed<int>.Missing(reasonA), Observed<int>.Missing(reasonB)));
        copy._heartRateBpm       = Observations.Resolved(Any.Int32().Between(20, 250)).As(sign => (Observed<int>?)sign);
        copy._temperatureCelsius = Observations.Resolved(Any.Decimal().Between(30m, 45m).WithScale(1)).As(sign => (Observed<decimal>?)sign);
        copy._weightKg           = Observations.Resolved(Any.Decimal().Between(1m, 400m).WithScale(1)).As(sign => (Observed<decimal>?)sign);
        copy._measuredAt         = Any.TimeOnly().AsNullable();

        return copy;
    }

}

public sealed partial class AnyLabResult {

    /// <summary>A result that is entered: observed or missing with a reason, never blank.</summary>
    public AnyLabResult Resolved() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = Observations.Resolved(Any.Decimal().Between(0m, 1_000m).WithScale(2)).As(result => (Observed<decimal>?)result);

        return copy;
    }

}

public sealed partial class AnyAdverseEvent {

    /// <summary>An event whose outcome is never <see cref="AdverseEventOutcome.Fatal" />: nobody dies in the scenario.</summary>
    public AnyAdverseEvent NonFatal() {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._outcome = Any.Enum<AdverseEventOutcome>().DifferentFrom(AdverseEventOutcome.Fatal).OrNull();

        return copy;
    }

    /// <summary>A non-serious event, graded 1 to 4, whose relationship is assessed: nothing is left to resolve.</summary>
    public AnyAdverseEvent Resolved() {
        AnyAdverseEvent copy = new AnyAdverseEvent(this).NonFatal();

        copy._gradeRecipe  = Any.Int32().Between(1, 4);
        copy._isSerious    = Any.Fixed(false);
        copy._relationship = Any.Enum<Causality>().AsNullable();

        return copy;
    }

}
