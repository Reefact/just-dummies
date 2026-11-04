// Scaffolded by `any` (JustDummies), then completed by hand: the result decides whether a unit and a
// reference range are carried.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="LabResult" />.</summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private IAny<string>             _analyteCode;
    private IAny<Observed<decimal>?> _result;
    private IAny<string>             _unit;
    private IAny<(decimal? Lower, decimal? Upper)> _range;

    public AnyLabResult() {
        _analyteCode = Any.StringMatching("[A-Z][A-Z0-9]{1,9}");
        _result      = Observations.Entered(Any.Decimal().Between(0m, 1_000m).WithScale(2));
        _unit        = Any.String().Printable().WithoutChars(" ").WithLengthBetween(1, LabResult.MaxUnitLength);
        _range       = AnyValidRange();
    }

    private AnyLabResult(AnyLabResult from) {
        _analyteCode = from._analyteCode;
        _result      = from._result;
        _unit        = from._unit;
        _range       = from._range;
    }

    // None, one side, or both sides (lower strictly below upper), all not negative.
    private static IAny<(decimal? Lower, decimal? Upper)> AnyValidRange() {
        return Any.Combine(Any.Pair(Any.Decimal().Between(0m, 500m).WithScale(2), Any.Decimal().Between(0m, 500m).WithScale(2)).Having.Item1LessThanItem2(),
                           Any.Int32().Between(0, 3),
                           (pair, shape) => shape switch {
                               0 => ((decimal?)null, (decimal?)null),
                               1 => (pair.Item1, null),
                               2 => (null, pair.Item2),
                               _ => (pair.Item1, pair.Item2)
                           });
    }

    public AnyLabResult WithAnalyteCode(string value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._analyteCode = Any.Fixed(value);

        return copy;
    }

    public AnyLabResult WithResult(Observed<decimal>? value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = Any.Fixed(value);

        return copy;
    }

    public LabResult Generate() {
        string             analyteCode = _analyteCode.Generate();
        Observed<decimal>? result      = _result.Generate();

        string? unit  = null;
        decimal? lower = null;
        decimal? upper = null;

        // observed: unit required; missing: neither unit nor range; blank: unit optional. A range needs a unit.
        bool carriesUnit = result is null ? Any.Boolean().Generate() : result.IsObserved;
        if (carriesUnit) {
            unit = _unit.Generate();
            (lower, upper) = _range.Generate();
        }

        return new LabResult(analyteCode, result, unit, lower, upper);
    }

}
