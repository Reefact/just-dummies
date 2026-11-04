using System;
using System.Collections.Generic;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="LabResult" />. Scaffolded by `any`, then completed by hand: the unit
///     and the reference range depend on what became of the result, and <see cref="Generate" /> repairs them, so
///     a drawn value is always one the constructor accepts. A refusal is tested with the constructor, not here.
/// </summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private IAny<string>                              _analyteCode;
    private IAny<Observed<decimal>?>                  _result;
    private IAny<string>                              _unit;
    private IAny<(decimal? Lower, decimal? Upper)>    _range;
    private bool                                      _rangeAbsent;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLabResult() {
        _analyteCode = TrialFields.AnalyteCode();
        _result      = TrialFields.Entered(Any.Decimal().Between(0m, 1_000m).WithScale(2));
        _unit        = Any.StringMatching("[a-zA-Z0-9/%]{1,12}");
        _range       = Any.Pair(Any.Decimal().Between(0m, 500m).WithScale(2),
                                       Any.Decimal().Between(0m, 500m).WithScale(2)).Having.Item1LessThanItem2()
                          .Then(pair => Any.Int32().Between(0, 9).As(k => k < 5 ? (Lower: (decimal?)pair.Item1, Upper: (decimal?)pair.Item2)
                                                                          : k < 7 ? (Lower: (decimal?)pair.Item1, Upper: (decimal?)null)
                                                                          : k < 9 ? (Lower: (decimal?)null, Upper: (decimal?)pair.Item2)
                                                                                  : (Lower: (decimal?)null, Upper: (decimal?)null)));
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLabResult(AnyLabResult from) {
        _analyteCode = from._analyteCode;
        _result      = from._result;
        _unit        = from._unit;
        _range       = from._range;
        _rangeAbsent = from._rangeAbsent;
    }

    public AnyLabResult WithAnalyteCode(string value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._analyteCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the result: an observed value, a missing reason, or <c>null</c> for a result not entered yet.</summary>
    public AnyLabResult WithResult(Observed<decimal>? value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = Any.Fixed(value);

        return copy;
    }

    /// <summary>A result with a value: observed, hence with its unit.</summary>
    public AnyLabResult Measured() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = TrialFields.Observation(Any.Decimal().Between(0m, 1_000m).WithScale(2));

        return copy;
    }

    /// <summary>A result entered, one way or the other.</summary>
    public AnyLabResult Resolved() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = Any.Int32().Between(0, 9).Then(k => k < 8
                                                              ? TrialFields.Observation(Any.Decimal().Between(0m, 1_000m).WithScale(2))
                                                              : TrialFields.Absence<decimal>());

        return copy;
    }

    /// <summary>A result not entered yet.</summary>
    public AnyLabResult Blank() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._result = TrialFields.Blank<decimal>();

        return copy;
    }

    /// <summary>Whatever is observed carries a unit already; this one has no reference range.</summary>
    public AnyLabResult WithoutRange() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._rangeAbsent = true;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LabResult" />.</summary>
    public LabResult Generate() {
        string             code   = _analyteCode.Generate();
        Observed<decimal>? result = _result.Generate();
        string?            unit   = null;
        decimal?           lower  = null;
        decimal?           upper  = null;

        if (result is not { IsObserved: false }) {
            // observed -> unit required; blank -> unit optional; a range needs a unit
            bool hasUnit = result is { IsObserved: true } || Any.Boolean().Generate();
            if (hasUnit) {
                unit = _unit.Generate();
                if (!_rangeAbsent) { (lower, upper) = _range.Generate(); }
            }
        }

        return new LabResult(code, result, unit, lower, upper);
    }

}
