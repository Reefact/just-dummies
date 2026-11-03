// Started from `any generate VisitDefinition`, then rewritten by hand: four of its five parameters were marked
// `to verify` (a regex, and a rule tying targetDay to daysBefore), and per-parameter recipes cannot hold that tie.
// Here the window is drawn so that it cannot open before day 1: the target first, the lead after it.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary <see cref="VisitDefinition" /> values.</summary>
public sealed partial class AnyVisitDefinition : IAny<VisitDefinition> {

    private IAny<string>              _code;
    private int?                      _targetDay;
    private int?                      _daysBefore;
    private IAny<int>                  _daysAfter;
    private IAny<IEnumerable<string>> _requiredAnalytes;

    public AnyVisitDefinition() {
        _code             = AnyVisitCodes.Code();
        _targetDay        = null;
        _daysBefore       = null;
        _daysAfter        = Any.Int32().Between(0, VisitDefinition.MaxToleranceDays);
        _requiredAnalytes = Any.SetOf(AnyAnalyteCodes.Code()).WithCountBetween(0, 4).As(set => (IEnumerable<string>)set);
    }

    private AnyVisitDefinition(AnyVisitDefinition from) {
        _code             = from._code;
        _targetDay        = from._targetDay;
        _daysBefore       = from._daysBefore;
        _daysAfter        = from._daysAfter;
        _requiredAnalytes = from._requiredAnalytes;
    }

    public AnyVisitDefinition WithCode(string value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>A code different from every one of <paramref name="others" />: two visits of a schedule do not share one.</summary>
    public AnyVisitDefinition WithCodeOtherThan(params string[] others) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._code = AnyVisitCodes.Code().Except(others);

        return copy;
    }

    /// <summary>Pins the target day; a lead that was not pinned is then drawn inside what that day allows.</summary>
    public AnyVisitDefinition WithTargetDay(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._targetDay = value;

        return copy;
    }

    /// <summary>Pins the days the window opens before the target; a target day that was not pinned is drawn after them.</summary>
    public AnyVisitDefinition WithDaysBefore(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysBefore = value;

        return copy;
    }

    public AnyVisitDefinition WithDaysAfter(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysAfter = Any.Fixed(value);

        return copy;
    }

    public AnyVisitDefinition WithRequiredAnalytes(params string[] values) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._requiredAnalytes = Any.Fixed<IEnumerable<string>>(values);

        return copy;
    }

    public AnyVisitDefinition WithoutRequiredAnalytes() {
        return WithRequiredAnalytes();
    }

    public VisitDefinition Generate() {
        int targetDay  = _targetDay  ?? Any.Int32().Between((_daysBefore ?? 0) + 1, VisitDefinition.MaxTargetDay).Generate();
        int daysBefore = _daysBefore ?? Any.Int32().Between(0, Math.Min(VisitDefinition.MaxToleranceDays, targetDay - 1)).Generate();

        return new VisitDefinition(_code.Generate(), targetDay, daysBefore, _daysAfter.Generate(), _requiredAnalytes.Generate());
    }

}

/// <summary>Visit codes: an upper-case letter then up to seven upper-case letters or digits.</summary>
public static class AnyVisitCodes {

    public static AnyPattern Code() {
        return Any.StringMatching("[A-Z][A-Z0-9]{0,7}");
    }

}

/// <summary>Analyte codes: an upper-case letter then one to nine upper-case letters or digits.</summary>
public static class AnyAnalyteCodes {

    public static AnyPattern Code() {
        return Any.StringMatching("[A-Z][A-Z0-9]{1,9}");
    }

}
