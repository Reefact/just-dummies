// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Semester --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Semester" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySemester : IAny<Semester> {

    // Hand-edited after scaffolding: Semester ties its start to its academic year and term, and its end to its
    // start, so 'start' and 'end' cannot be drawn independently. The start's month and day are drawn, its year
    // is the one the term requires; the end is a drawn number of days after the start.

    private readonly IAny<int>      _academicYear;
    private readonly IAny<Term>     _term;
    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _lengthInDays;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySemester()
        : this(academicYear: AnyValidAcademicYear(),
               term:         AnyValidTerm(),
               start:        AnyValidStart(),
               lengthInDays: AnyValidLengthInDays()) { }

    private static AnyInt32 AnyValidAcademicYear() {
        // The guard is 2000..2100; a spring semester of 2100 starts in 2101, which DateOnly holds.
        return Any.Int32().Between(2000, 2100);
    }

    private static AnyEnum<Term> AnyValidTerm() {
        return Any.Enum<Term>();
    }

    private static AnyDateOnly AnyValidStart() {
        // Only the month and the day are kept: Generate() moves the date into the year the term requires.
        return Any.DateOnly().Between(new DateOnly(2001, 1, 1), new DateOnly(2001, 12, 31));
    }

    private static AnyInt32 AnyValidLengthInDays() {
        return Any.Int32().Between(1, 200);
    }

    private AnySemester(IAny<int>      academicYear,
                        IAny<Term>     term,
                        IAny<DateOnly> start,
                        IAny<int>      lengthInDays) {
        _academicYear = academicYear;
        _term         = term;
        _start        = start;
        _lengthInDays = lengthInDays;
    }

    /// <summary>Pins <c>academicYear</c> to a fixed value.</summary>
    public AnySemester WithAcademicYear(int value) {
        return new AnySemester(new FixedValue<int>(value), _term, _start, _lengthInDays);
    }

    /// <summary>Draws <c>academicYear</c> from <c>AnyValidAcademicYear()</c>, refined by <paramref name="configure" />.</summary>
    public AnySemester WithAcademicYear(Func<AnyInt32, AnyInt32> configure) {
        return new AnySemester(configure(AnyValidAcademicYear()), _term, _start, _lengthInDays);
    }

    /// <summary>Pins <c>term</c> to a fixed value.</summary>
    public AnySemester WithTerm(Term value) {
        return new AnySemester(_academicYear, new FixedValue<Term>(value), _start, _lengthInDays);
    }

    /// <summary>Draws <c>term</c> from <c>AnyValidTerm()</c>, refined by <paramref name="configure" />.</summary>
    public AnySemester WithTerm(Func<AnyEnum<Term>, AnyEnum<Term>> configure) {
        return new AnySemester(_academicYear, configure(AnyValidTerm()), _start, _lengthInDays);
    }

    /// <summary>Pins the length of the semester, in days after its start.</summary>
    public AnySemester WithLengthInDays(int value) {
        return new AnySemester(_academicYear, _term, _start, new FixedValue<int>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Semester" />.</summary>
    public Semester Generate() {
        int      academicYear = _academicYear.Generate();
        Term     term         = _term.Generate();
        int      startYear    = term == Term.Autumn ? academicYear : academicYear + 1;
        DateOnly template     = _start.Generate();
        DateOnly start        = new DateOnly(startYear, template.Month, Math.Min(template.Day, DateTime.DaysInMonth(startYear, template.Month)));

        return new Semester(academicYear, term, start, start.AddDays(_lengthInDays.Generate()));
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
