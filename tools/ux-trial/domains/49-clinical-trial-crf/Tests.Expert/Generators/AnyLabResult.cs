// Started from `any generate LabResult`, then rewritten by hand: the result was a TODO (the tool cannot draw an
// Observed<decimal>?) and the unit, the limits and the result answer to one another. A lab result is drawn in one of
// three shapes — observed, missing, blank — and each shape fixes what the unit and the range may be.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="LabResult" /> values.</summary>
public sealed partial class AnyLabResult : IAny<LabResult> {

    private enum Shape { Observed, Missing, Blank }

    private IAny<string> _analyteCode;
    private IAny<Shape>  _shape;

    public AnyLabResult() {
        _analyteCode = AnyAnalyteCodes.Code();
        _shape       = Any.Enum<Shape>();
    }

    private AnyLabResult(AnyLabResult from) {
        _analyteCode = from._analyteCode;
        _shape       = from._shape;
    }

    public AnyLabResult WithAnalyteCode(string value) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._analyteCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>A reported value with its unit and, sometimes, a reference range.</summary>
    public AnyLabResult Observed() {
        return WithShape(Shape.Observed);
    }

    /// <summary>A result missing for a reason: no unit, no range.</summary>
    public AnyLabResult Missing() {
        return WithShape(Shape.Missing);
    }

    /// <summary>A result not entered yet.</summary>
    public AnyLabResult Blank() {
        return WithShape(Shape.Blank);
    }

    /// <summary>A result entered, observed or missing: it does not block signing.</summary>
    public AnyLabResult Resolved() {
        AnyLabResult copy = new AnyLabResult(this);

        copy._shape = Any.Enum<Shape>().Except(Shape.Blank);

        return copy;
    }

    public LabResult Generate() {
        string analyteCode = _analyteCode.Generate();

        switch (_shape.Generate()) {
            case Shape.Observed: {
                decimal  value = Any.Decimal().GreaterThanOrEqualTo(0m).Generate();
                string   unit  = AnyUnits().Generate();
                (decimal? lower, decimal? upper) = Range().Generate();

                return new LabResult(analyteCode, Observed<decimal>.Of(value), unit, lower, upper);
            }
            case Shape.Missing:
                return new LabResult(analyteCode, AnyObservedOf.Missing<decimal>().Generate(), null, null, null);
            default: {
                string? unit = AnyUnits().OrNull().Generate();
                (decimal? lower, decimal? upper) = unit is null ? ((decimal?)null, (decimal?)null) : Range().Generate();

                return new LabResult(analyteCode, null, unit, lower, upper);
            }
        }
    }

    private AnyLabResult WithShape(Shape shape) {
        AnyLabResult copy = new AnyLabResult(this);

        copy._shape = Any.Fixed(shape);

        return copy;
    }

    // 1 to 20 characters, no whitespace: the printable ASCII minus the space.
    private static AnyString AnyUnits() {
        return Any.String().Printable().WithoutChars(" ").WithLengthBetween(1, LabResult.MaxUnitLength);
    }

    // No range, a lower limit only, an upper limit only, or both (lower below upper).
    private static IAny<(decimal? Lower, decimal? Upper)> Range() {
        IAny<decimal> aLimit = Any.Decimal().GreaterThanOrEqualTo(0m);

        return Any.Int32().Between(0, 3).Then(kind => kind switch {
            0 => Any.Fixed(((decimal?)null, (decimal?)null)),
            1 => aLimit.As(lower => ((decimal?)lower, (decimal?)null)),
            2 => aLimit.As(upper => ((decimal?)null, (decimal?)upper)),
            _ => Any.Pair(Any.Decimal().GreaterThanOrEqualTo(0m), Any.Decimal().GreaterThanOrEqualTo(0m)).Having.Item1LessThanItem2()
                    .As(pair => ((decimal?)pair.Item1, (decimal?)pair.Item2))
        });
    }

}
