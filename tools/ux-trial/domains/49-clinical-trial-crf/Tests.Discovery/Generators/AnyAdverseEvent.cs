// Scaffolded by `any` (JustDummies), then completed by hand: the outcome decides the grade, the seriousness
// and whether the event has ended; the end date follows the onset.

using System;

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>A generator of arbitrary, valid <see cref="AdverseEvent" />.</summary>
public sealed partial class AnyAdverseEvent : IAny<AdverseEvent> {

    private IAny<string>               _term;
    private IAny<DateOnly>             _onsetDate;
    private IAny<TimeOnly?>            _onsetTime;
    private IAny<int?>                 _daysToEnd;     // when the end date is free: null (ongoing) or days after onset
    private DateOnly?                  _pinnedEndDate;
    private bool                       _endDatePinned;
    private AnyInt32                   _gradeRecipe;
    private IAny<int?>                 _grade;
    private IAny<bool>                 _isSerious;
    private IAny<Causality?>           _relationship;
    private IAny<AdverseEventOutcome?> _outcome;
    private IAny<string?>              _comment;

    public AnyAdverseEvent() {
        _term          = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength);
        _onsetDate     = TrialCalendar.Date();
        _onsetTime     = Any.TimeOnly().OrNull();
        _daysToEnd     = Any.Int32().Between(0, 400).OrNull();
        _gradeRecipe   = Any.Int32().Between(1, 5);
        _grade         = Any.Fixed<int?>(null);   // null: not pinned, drawn from the recipe in Generate()
        _isSerious     = Any.Boolean();
        _relationship  = Any.Enum<Causality>().OrNull();
        _outcome       = Any.Enum<AdverseEventOutcome>().OrNull();
        _comment       = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxCommentLength).OrNull();
    }

    private AnyAdverseEvent(AnyAdverseEvent from) {
        _term          = from._term;
        _onsetDate     = from._onsetDate;
        _onsetTime     = from._onsetTime;
        _daysToEnd     = from._daysToEnd;
        _pinnedEndDate = from._pinnedEndDate;
        _endDatePinned = from._endDatePinned;
        _gradeRecipe   = from._gradeRecipe;
        _grade         = from._grade;
        _isSerious     = from._isSerious;
        _relationship  = from._relationship;
        _outcome       = from._outcome;
        _comment       = from._comment;
    }

    public AnyAdverseEvent WithOnsetDate(DateOnly value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._onsetDate = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithEndDate(DateOnly? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._pinnedEndDate = value;
        copy._endDatePinned = true;

        return copy;
    }

    public AnyAdverseEvent WithGrade(int value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._grade = Any.Fixed<int?>(value);

        return copy;
    }

    public AnyAdverseEvent WithSerious(bool value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._isSerious = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithRelationship(Causality? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._relationship = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithOutcome(AdverseEventOutcome? value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._outcome = Any.Fixed(value);

        return copy;
    }

    public AdverseEvent Generate() {
        DateOnly              onsetDate = _onsetDate.Generate();
        AdverseEventOutcome?  outcome   = _outcome.Generate();

        // Fatal <=> grade 5; a grade-5 event is serious; an outcome other than Fatal rules grade 5 out.
        int? pinnedGrade = _grade.Generate();
        int  grade       = outcome == AdverseEventOutcome.Fatal ? 5
                         : pinnedGrade ?? (outcome is null ? _gradeRecipe.Generate() : _gradeRecipe.LessThan(5).Generate());

        bool isSerious = grade == 5 || _isSerious.Generate();

        // Recovered, RecoveredWithSequelae, Fatal need an end date; Recovering and NotRecovered forbid one.
        int?     daysToEnd = _daysToEnd.Generate();
        DateOnly? endDate;
        if (_endDatePinned) {
            endDate = _pinnedEndDate;
        } else {
            endDate = outcome switch {
                AdverseEventOutcome.Recovered or AdverseEventOutcome.RecoveredWithSequelae or AdverseEventOutcome.Fatal
                    => onsetDate.AddDays(daysToEnd ?? 0),
                AdverseEventOutcome.Recovering or AdverseEventOutcome.NotRecovered
                    => null,
                _ => daysToEnd is int days ? onsetDate.AddDays(days) : null
            };
        }

        return new AdverseEvent(_term.Generate(),
                                onsetDate,
                                _onsetTime.Generate(),
                                endDate,
                                grade,
                                isSerious,
                                _relationship.Generate(),
                                outcome,
                                _comment.Generate());
    }

}
