// Started from `any generate AdverseEvent`, then rewritten by hand: seven of its nine parameters were marked `to verify`
// because grade, seriousness, outcome and end date constrain one another. They are drawn here in the order the rules
// allow: grade, seriousness, outcome, then the end date the outcome asks for or forbids.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="AdverseEvent" /> values.</summary>
public sealed partial class AnyAdverseEvent : IAny<AdverseEvent> {

    private bool _resolved;

    private IAny<string>                  _term;
    private IAny<DateOnly>                _onsetDate;
    private IAny<TimeOnly?>               _onsetTime;
    private IAny<string?>                 _comment;
    private IAny<Causality?>              _relationship;

    private int?                  _grade;
    private bool?                 _isSerious;
    private Pin<AdverseEventOutcome?> _outcome;
    private Pin<DateOnly?>        _endDate;

    private readonly struct Pin<T> {

        public Pin(T value) {
            IsSet = true;
            Value = value;
        }

        public bool IsSet { get; }
        public T    Value { get; }

    }

    public AnyAdverseEvent() {
        _term         = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength);
        _onsetDate    = Any.DateOnly().Between(TrialCalendar.Earliest, TrialCalendar.Latest.AddDays(-400));
        _onsetTime    = Any.TimeOnly().OrNull();
        _comment      = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxCommentLength).OrNull();
        _relationship = Any.Enum<Causality>().OrNull();
    }

    private AnyAdverseEvent(AnyAdverseEvent from) {
        _resolved     = from._resolved;
        _term         = from._term;
        _onsetDate    = from._onsetDate;
        _onsetTime    = from._onsetTime;
        _comment      = from._comment;
        _relationship = from._relationship;
        _grade        = from._grade;
        _isSerious    = from._isSerious;
        _outcome      = from._outcome;
        _endDate      = from._endDate;
    }

    public AnyAdverseEvent WithOnsetDate(DateOnly value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._onsetDate = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithGrade(int value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._grade = value;

        return copy;
    }

    public AnyAdverseEvent WithSerious(bool value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._isSerious = value;

        return copy;
    }

    public AnyAdverseEvent WithOutcome(AdverseEventOutcome? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._outcome = new Pin<AdverseEventOutcome?>(value);

        return copy;
    }

    public AnyAdverseEvent WithEndDate(DateOnly? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._endDate = new Pin<DateOnly?>(value);

        return copy;
    }

    public AnyAdverseEvent WithRelationship(Causality? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._relationship = Any.Fixed(value);

        return copy;
    }

    /// <summary>A fatal event: grade 5, serious, outcome fatal, the death on the given date.</summary>
    public AnyAdverseEvent Fatal(DateOnly deathDate) {
        return WithGrade(5).WithSerious(true).WithOutcome(AdverseEventOutcome.Fatal).WithEndDate(deathDate);
    }

    /// <summary>Nothing left for the investigator to enter: relationship assessed, outcome of a serious event given, death date of a fatal one.</summary>
    public AnyAdverseEvent Resolved() {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._resolved     = true;
        copy._relationship = Any.Enum<Causality>().AsNullable();

        return copy;
    }

    public AdverseEvent Generate() {
        DateOnly onsetDate = _onsetDate.Generate();

        int  grade     = _grade ?? DrawGrade();
        bool isSerious = _isSerious ?? (grade == 5 || Any.Boolean().Generate());

        AdverseEventOutcome? outcome = _outcome.IsSet ? _outcome.Value : DrawOutcome(grade, isSerious);

        DateOnly? endDate;
        if (_endDate.IsSet) {
            endDate = _endDate.Value;
        } else if (outcome is AdverseEventOutcome.Recovered or AdverseEventOutcome.RecoveredWithSequelae or AdverseEventOutcome.Fatal) {
            endDate = onsetDate.AddDays(Any.Int32().Between(0, 365).Generate());
        } else if (outcome is AdverseEventOutcome.Recovering or AdverseEventOutcome.NotRecovered) {
            endDate = null;
        } else if (_resolved && grade == 5) {
            endDate = onsetDate.AddDays(Any.Int32().Between(0, 365).Generate());
        } else {
            endDate = Any.Boolean().Generate() ? onsetDate.AddDays(Any.Int32().Between(0, 365).Generate()) : null;
        }

        return new AdverseEvent(_term.Generate(), onsetDate, _onsetTime.Generate(), endDate, grade, isSerious,
                                _relationship.Generate(), outcome, _comment.Generate());
    }

    private int DrawGrade() {
        // A pinned fatal outcome is a grade 5 event; a pinned other outcome is not.
        if (_outcome is { IsSet: true, Value: AdverseEventOutcome.Fatal }) { return 5; }
        if (_outcome.IsSet && _outcome.Value is not null)                  { return Any.Int32().Between(1, 4).Generate(); }

        return Any.Int32().Between(1, 5).Generate();
    }

    private AdverseEventOutcome? DrawOutcome(int grade, bool isSerious) {
        if (grade == 5) {
            return _resolved || Any.Boolean().Generate() ? AdverseEventOutcome.Fatal : null;
        }

        IAny<AdverseEventOutcome> notFatal = Any.Enum<AdverseEventOutcome>().DifferentFrom(AdverseEventOutcome.Fatal);

        return _resolved && isSerious ? notFatal.Generate() : notFatal.OrNull().Generate();
    }

}
