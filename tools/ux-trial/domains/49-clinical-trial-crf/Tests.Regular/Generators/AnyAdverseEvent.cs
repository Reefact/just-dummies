using System;

using ClinicalTrial.Domain;
using JustDummies;

namespace ClinicalTrial.Domain.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="AdverseEvent" />. Scaffolded by `any`, then completed by hand: the
///     grade, seriousness, outcome and end date hold together, and <see cref="Generate" /> settles them in that
///     order of precedence: an outcome of <c>Fatal</c> makes the grade 5, a grade 5 with an outcome makes it
///     <c>Fatal</c>, a grade 5 is serious, and the end date follows the outcome. A refusal is tested with the
///     constructor, not here.
/// </summary>
public sealed partial class AnyAdverseEvent : IAny<AdverseEvent> {

    private IAny<string>               _term;
    private IAny<DateOnly>             _onsetDate;
    private IAny<TimeOnly?>            _onsetTime;
    private IAny<int>                  _durationDays;
    private IAny<bool>                 _endsWhenOutcomeIsOpen;
    private IAny<int>                  _grade;
    private IAny<bool>                 _isSerious;
    private IAny<Causality?>           _relationship;
    private IAny<AdverseEventOutcome?> _outcome;
    private IAny<string?>              _comment;
    private bool                       _nonFatal;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdverseEvent() {
        _term                  = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxTermLength);
        _onsetDate             = TrialFields.Date();
        _onsetTime             = Any.TimeOnly().OrNull();
        _durationDays          = Any.Int32().Between(0, 90);
        _endsWhenOutcomeIsOpen = Any.Boolean();
        _grade                 = Any.Int32().Between(1, 5);
        _isSerious             = Any.Boolean();
        _relationship          = Any.Enum<Causality>().OrNull();
        _outcome               = Any.Enum<AdverseEventOutcome>().OrNull();
        _comment               = Any.String().NotBlank().WithMaxLength(AdverseEvent.MaxCommentLength).OrNull();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAdverseEvent(AnyAdverseEvent from) {
        _term                  = from._term;
        _onsetDate             = from._onsetDate;
        _onsetTime             = from._onsetTime;
        _durationDays          = from._durationDays;
        _endsWhenOutcomeIsOpen = from._endsWhenOutcomeIsOpen;
        _grade                 = from._grade;
        _isSerious             = from._isSerious;
        _relationship          = from._relationship;
        _outcome               = from._outcome;
        _comment               = from._comment;
        _nonFatal              = from._nonFatal;
    }

    public AnyAdverseEvent WithTerm(string value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._term = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithOnsetDate(DateOnly value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._onsetDate = Any.Fixed(value);

        return copy;
    }

    public AnyAdverseEvent WithGrade(int value) {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._grade = Any.Fixed(value);

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

    /// <summary>An event that does not end in death: grade 1 to 4, and any outcome but <c>Fatal</c>. Call it before <see cref="Resolved" /> or the pins.</summary>
    public AnyAdverseEvent NonFatal() {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._nonFatal = true;
        copy._grade    = Any.Int32().Between(1, 4);
        copy._outcome  = Any.Enum<AdverseEventOutcome>().DifferentFrom(AdverseEventOutcome.Fatal).OrNull();

        return copy;
    }

    /// <summary>Relationship assessed and outcome given: nothing left to resolve (a grade 5 ends in <c>Fatal</c>, hence with its end date).</summary>
    public AnyAdverseEvent Resolved() {
        AnyAdverseEvent copy = new AnyAdverseEvent(this);

        copy._relationship = Any.Enum<Causality>().AsNullable();
        copy._outcome      = copy._nonFatal ? Any.Enum<AdverseEventOutcome>().DifferentFrom(AdverseEventOutcome.Fatal).AsNullable() : Any.Enum<AdverseEventOutcome>().AsNullable();

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AdverseEvent" />.</summary>
    public AdverseEvent Generate() {
        DateOnly             onset   = _onsetDate.Generate();
        int                  grade   = _grade.Generate();
        AdverseEventOutcome? outcome = _outcome.Generate();

        if (outcome == AdverseEventOutcome.Fatal) { grade = 5; }
        else if (grade == 5 && outcome.HasValue) { outcome = AdverseEventOutcome.Fatal; }

        bool isSerious = grade == 5 || _isSerious.Generate();

        DateOnly? endDate = outcome switch {
            AdverseEventOutcome.Recovered or AdverseEventOutcome.RecoveredWithSequelae or AdverseEventOutcome.Fatal => onset.AddDays(_durationDays.Generate()),
            AdverseEventOutcome.Recovering or AdverseEventOutcome.NotRecovered                                      => null,
            _                                                                                                       => _endsWhenOutcomeIsOpen.Generate() ? onset.AddDays(_durationDays.Generate()) : null
        };

        return new AdverseEvent(_term.Generate(), onset, _onsetTime.Generate(), endDate, grade, isSerious, _relationship.Generate(), outcome, _comment.Generate());
    }

}
