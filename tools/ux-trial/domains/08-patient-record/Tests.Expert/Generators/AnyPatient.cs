using System;

using Clinical.Domain;
using JustDummies;

namespace Clinical.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Patient" /> values. Scaffolded by <c>any</c>, then adapted by hand
///     for the one rule it cannot see: the birth date and the NIR carry the same year and month.
///     By default the birth date is drawn first and the NIR is drawn to match it; a pinned NIR makes the
///     birth date follow it instead. <see cref="WithNir(Func{AnyNir, AnyNir})" /> refines the NIR over that
///     match (<c>n =&gt; n.OfSex(Sex.Female)</c>), it never replaces it.
/// </summary>
public sealed partial class AnyPatient : IAny<Patient> {

    private static readonly DateOnly LatestBirthDate = new DateOnly(2025, 12, 31);

    private readonly Nir?                     _pinnedNir;
    private readonly Func<AnyNir, AnyNir>     _nirRefinement;
    private readonly IAny<string>             _lastName;
    private readonly IAny<string>             _firstName;
    private readonly DateOnly?                _pinnedBirthDate;
    private readonly AnyDateOnly              _birthDateRecipe;
    private readonly bool                     _birthDateRefined;

    public AnyPatient()
        : this(null, n => n, AnyValidLastName(), AnyValidFirstName(), null, AnyValidBirthDate(), false) { }

    private AnyPatient(Nir? pinnedNir, Func<AnyNir, AnyNir> nirRefinement, IAny<string> lastName, IAny<string> firstName,
                       DateOnly? pinnedBirthDate, AnyDateOnly birthDateRecipe, bool birthDateRefined) {
        _pinnedNir        = pinnedNir;
        _nirRefinement    = nirRefinement;
        _lastName         = lastName;
        _firstName        = firstName;
        _pinnedBirthDate  = pinnedBirthDate;
        _birthDateRecipe  = birthDateRecipe;
        _birthDateRefined = birthDateRefined;
    }

    private static AnyString AnyValidLastName() {
        return Any.String().NotBlank().WithMaxLength(60);
    }

    private static AnyString AnyValidFirstName() {
        return Any.String().NotBlank().WithMaxLength(60);
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly().Between(Patient.EarliestBirthDate, LatestBirthDate);
    }

    public AnyPatient WithNir(Nir value) {
        return new AnyPatient(value, _nirRefinement, _lastName, _firstName, _pinnedBirthDate, _birthDateRecipe, _birthDateRefined);
    }

    public AnyPatient WithNir(Func<AnyNir, AnyNir> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        Func<AnyNir, AnyNir> previous = _nirRefinement;

        return new AnyPatient(null, n => configure(previous(n)), _lastName, _firstName, _pinnedBirthDate, _birthDateRecipe, _birthDateRefined);
    }

    public AnyPatient WithLastName(string value) {
        return new AnyPatient(_pinnedNir, _nirRefinement, Any.Fixed(value), _firstName, _pinnedBirthDate, _birthDateRecipe, _birthDateRefined);
    }

    public AnyPatient WithFirstName(string value) {
        return new AnyPatient(_pinnedNir, _nirRefinement, _lastName, Any.Fixed(value), _pinnedBirthDate, _birthDateRecipe, _birthDateRefined);
    }

    public AnyPatient WithBirthDate(DateOnly value) {
        return new AnyPatient(_pinnedNir, _nirRefinement, _lastName, _firstName, value, _birthDateRecipe, _birthDateRefined);
    }

    public AnyPatient WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        return new AnyPatient(_pinnedNir, _nirRefinement, _lastName, _firstName, null, configure(_birthDateRecipe), true);
    }

    /// <summary>Produces one arbitrary <see cref="Patient" />.</summary>
    public Patient Generate() {
        DateOnly birthDate;
        Nir      nir;
        if (_pinnedNir is not null && _pinnedBirthDate is null) {
            if (_birthDateRefined) {
                throw new InvalidOperationException("AnyPatient: a pinned NIR fixes the birth year and month; pin the birth date too, or let it be drawn from the NIR.");
            }
            nir       = _pinnedNir;
            birthDate = BirthDateCarriedBy(nir);
        } else {
            birthDate = _pinnedBirthDate ?? _birthDateRecipe.Generate();
            nir       = _pinnedNir ?? _nirRefinement(new AnyNir().BornOn(birthDate)).Generate();
        }

        return new Patient(nir, _lastName.Generate(), _firstName.Generate(), birthDate);
    }

    private static DateOnly BirthDateCarriedBy(Nir nir) {
        int[] centuries = [1900 + nir.BirthYearTwoDigits, 2000 + nir.BirthYearTwoDigits];
        int   year      = Any.ElementOf(Array.FindAll(centuries, y => y <= LatestBirthDate.Year)).Generate();
        int day  = Any.Int32().Between(1, DateTime.DaysInMonth(year, nir.BirthMonth)).Generate();

        return new DateOnly(year, nir.BirthMonth, day);
    }

}
