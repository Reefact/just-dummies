// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Patient --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Patient" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPatient : IAny<Patient> {

    private IAny<Nir>      _nir;
    private IAny<string>   _lastName;
    private IAny<string>   _firstName;
    private IAny<DateOnly> _birthDate;

    private Nir?        _fixedNir;
    private DateOnly?   _fixedBirthDate;

    private AnyNir      _nirRecipe;
    private AnyString   _lastNameRecipe;
    private AnyString   _firstNameRecipe;
    private AnyDateOnly _birthDateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPatient()
        : this(nir:       AnyValidNir(),
               lastName:  AnyValidLastName(),
               firstName: AnyValidFirstName(),
               birthDate: AnyValidBirthDate()) { }

    private static AnyNir AnyValidNir() {
        return new AnyNir();
    }

    private static AnyString AnyValidLastName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidFirstName() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly().AfterOrEqualTo(Patient.EarliestBirthDate).BeforeOrEqualTo(new DateOnly(2025, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPatient(AnyNir      nir,
                       AnyString   lastName,
                       AnyString   firstName,
                       AnyDateOnly birthDate) {
        _nir             = nir;
        _nirRecipe       = nir;
        _lastName        = lastName;
        _lastNameRecipe  = lastName;
        _firstName       = firstName;
        _firstNameRecipe = firstName;
        _birthDate       = birthDate;
        _birthDateRecipe = birthDate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPatient(AnyPatient from) {
        _nir             = from._nir;
        _nirRecipe       = from._nirRecipe;
        _lastName        = from._lastName;
        _lastNameRecipe  = from._lastNameRecipe;
        _firstName       = from._firstName;
        _firstNameRecipe = from._firstNameRecipe;
        _birthDate       = from._birthDate;
        _birthDateRecipe = from._birthDateRecipe;
        _fixedNir        = from._fixedNir;
        _fixedBirthDate  = from._fixedBirthDate;
    }

    /// <summary>Pins <c>nir</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatient WithNir(Nir value) {
        AnyPatient copy = new AnyPatient(this);

        copy._nir      = Any.Fixed(value);
        copy._fixedNir = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>nir</c> is drawn from — <c>AnyValidNir()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyNir, AnyNir&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPatient WithNir(Func<AnyNir, AnyNir> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatient copy = new AnyPatient(this);

        copy._nirRecipe = configure(_nirRecipe);
        copy._nir       = copy._nirRecipe;

        return copy;
    }

    /// <summary>Pins <c>lastName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatient WithLastName(string value) {
        AnyPatient copy = new AnyPatient(this);

        copy._lastName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lastName</c> is drawn from — <c>AnyValidLastName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPatient WithLastName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatient copy = new AnyPatient(this);

        copy._lastNameRecipe = configure(_lastNameRecipe);
        copy._lastName       = copy._lastNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>firstName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatient WithFirstName(string value) {
        AnyPatient copy = new AnyPatient(this);

        copy._firstName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstName</c> is drawn from — <c>AnyValidFirstName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPatient WithFirstName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatient copy = new AnyPatient(this);

        copy._firstNameRecipe = configure(_firstNameRecipe);
        copy._firstName       = copy._firstNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>birthDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatient WithBirthDate(DateOnly value) {
        AnyPatient copy = new AnyPatient(this);

        copy._birthDate      = Any.Fixed(value);
        copy._fixedBirthDate = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>birthDate</c> is drawn from — <c>AnyValidBirthDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPatient WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatient copy = new AnyPatient(this);

        copy._birthDateRecipe = configure(_birthDateRecipe);
        copy._birthDate       = copy._birthDateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Patient" />. The NIR carries the birth year and month, so the two are drawn together; whichever is pinned, the other follows it.</summary>
    public Patient Generate() {
        Nir      nir;
        DateOnly birthDate;

        if (_fixedNir is not null && _fixedBirthDate is not null) {
            nir       = _fixedNir;
            birthDate = _fixedBirthDate.Value;
        } else if (_fixedNir is not null) {
            nir       = _fixedNir;
            birthDate = BirthDatesMatching(_fixedNir).Generate();
        } else {
            birthDate = _birthDate.Generate();
            nir       = _nirRecipe.BornIn(birthDate.Year, birthDate.Month).Generate();
        }

        return new Patient(nir, _lastName.Generate(), _firstName.Generate(), birthDate);
    }

    private IAny<DateOnly> BirthDatesMatching(Nir nir) {
        List<int> years = new List<int> { 1900 + nir.BirthYearTwoDigits };
        if (2000 + nir.BirthYearTwoDigits <= 2025) { years.Add(2000 + nir.BirthYearTwoDigits); }

        return Any.ElementOf(years).Then(year => {
            DateOnly first = new DateOnly(year, nir.BirthMonth, 1);

            return _birthDateRecipe.AfterOrEqualTo(first).BeforeOrEqualTo(first.AddMonths(1).AddDays(-1));
        });
    }

}
