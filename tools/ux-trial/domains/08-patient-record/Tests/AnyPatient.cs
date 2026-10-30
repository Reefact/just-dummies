// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Patient --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Patient" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPatient : IAny<Patient> {

    private readonly IAny<Nir>      _nir;
    private readonly IAny<string>   _lastName;
    private readonly IAny<string>   _firstName;
    private readonly IAny<DateOnly> _birthDate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPatient()
        : this(nir:       AnyValidNir(),
               lastName:  AnyValidLastName(),
               firstName: AnyValidFirstName(),
               birthDate: AnyValidBirthDate()) { }

    private static AnyNir AnyValidNir() {
        // The NIR carries the birth year and month; Generate() aligns the default recipe with the drawn birth date.
        return new AnyNir();
    }

    private static AnyString AnyValidLastName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidFirstName() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidBirthDate() {
        // A plausible, bounded age: born no earlier than 1900 (the domain's floor) and no later than 2025.
        return Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31));
    }

    private AnyPatient(IAny<Nir>      nir,
                       IAny<string>   lastName,
                       IAny<string>   firstName,
                       IAny<DateOnly> birthDate) {
        _nir       = nir;
        _lastName  = lastName;
        _firstName = firstName;
        _birthDate = birthDate;
    }

    /// <summary>Pins <c>nir</c> to a fixed value.</summary>
    public AnyPatient WithNir(Nir value) {
        return new AnyPatient(new FixedValue<Nir>(value), _lastName, _firstName, _birthDate);
    }

    /// <summary>Draws <c>nir</c> from <c>AnyValidNir()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatient WithNir(Func<AnyNir, AnyNir> configure) {
        return new AnyPatient(configure(AnyValidNir()), _lastName, _firstName, _birthDate);
    }

    /// <summary>Pins <c>lastName</c> to a fixed value.</summary>
    public AnyPatient WithLastName(string value) {
        return new AnyPatient(_nir, new FixedValue<string>(value), _firstName, _birthDate);
    }

    /// <summary>Draws <c>lastName</c> from <c>AnyValidLastName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatient WithLastName(Func<AnyString, AnyString> configure) {
        return new AnyPatient(_nir, configure(AnyValidLastName()), _firstName, _birthDate);
    }

    /// <summary>Pins <c>firstName</c> to a fixed value.</summary>
    public AnyPatient WithFirstName(string value) {
        return new AnyPatient(_nir, _lastName, new FixedValue<string>(value), _birthDate);
    }

    /// <summary>Draws <c>firstName</c> from <c>AnyValidFirstName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatient WithFirstName(Func<AnyString, AnyString> configure) {
        return new AnyPatient(_nir, _lastName, configure(AnyValidFirstName()), _birthDate);
    }

    /// <summary>Pins <c>birthDate</c> to a fixed value.</summary>
    public AnyPatient WithBirthDate(DateOnly value) {
        return new AnyPatient(_nir, _lastName, _firstName, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>birthDate</c> from <c>AnyValidBirthDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatient WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyPatient(_nir, _lastName, _firstName, configure(AnyValidBirthDate()));
    }

    /// <summary>Produces one arbitrary <see cref="Patient" />.</summary>
    public Patient Generate() {
        DateOnly birthDate = _birthDate.Generate();
        // A pinned NIR is the test's own choice; a drawn one is made to agree with the birth date.
        Nir nir = _nir is AnyNir anyNir ? anyNir.BornIn(birthDate.Year, birthDate.Month).Generate() : _nir.Generate();

        return new Patient(nir,
                           _lastName.Generate(),
                           _firstName.Generate(),
                           birthDate);
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
