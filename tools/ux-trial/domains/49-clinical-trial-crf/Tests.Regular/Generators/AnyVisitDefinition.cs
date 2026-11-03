using System;
using System.Collections.Generic;
using System.Linq;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="VisitDefinition" />. Scaffolded by `any`, then completed by hand:
///     the window cannot open before day 1, so <see cref="Generate" /> narrows the days-before tolerance to the
///     room there is. A refusal is tested with the constructor, not here.
/// </summary>
public sealed partial class AnyVisitDefinition : IAny<VisitDefinition> {

    private IAny<string>       _code;
    private IAny<int>          _targetDay;
    private IAny<int>          _daysBefore;
    private IAny<int>          _daysAfter;
    private IAny<List<string>> _requiredAnalytes;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVisitDefinition() {
        _code             = TrialFields.VisitCode();
        _targetDay        = Any.Int32().Between(1, VisitDefinition.MaxTargetDay);
        _daysBefore       = Any.Int32().Between(0, VisitDefinition.MaxToleranceDays);
        _daysAfter        = Any.Int32().Between(0, VisitDefinition.MaxToleranceDays);
        _requiredAnalytes = Any.ListOf(TrialFields.AnalyteCode()).Distinct().WithCountBetween(0, 4);
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
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

    public AnyVisitDefinition WithTargetDay(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._targetDay = Any.Fixed(value);

        return copy;
    }

    public AnyVisitDefinition WithDaysBefore(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysBefore = Any.Fixed(value);

        return copy;
    }

    public AnyVisitDefinition WithDaysAfter(int value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._daysAfter = Any.Fixed(value);

        return copy;
    }

    public AnyVisitDefinition WithRequiredAnalytes(params string[] value) {
        AnyVisitDefinition copy = new AnyVisitDefinition(this);

        copy._requiredAnalytes = Any.Fixed(value.ToList());

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VisitDefinition" />.</summary>
    public VisitDefinition Generate() {
        int targetDay = _targetDay.Generate();

        return new VisitDefinition(_code.Generate(),
                                   targetDay,
                                   Math.Min(_daysBefore.Generate(), targetDay - 1),
                                   _daysAfter.Generate(),
                                   _requiredAnalytes.Generate());
    }

}
