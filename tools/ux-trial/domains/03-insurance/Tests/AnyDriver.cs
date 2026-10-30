// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Driver --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Driver" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDriver : IAny<Driver> {

    private readonly IAny<string>   _fullName;
    private readonly IAny<DateOnly> _birthDate;
    private readonly IAny<int>      _yearsLicensed;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDriver()
        : this(fullName:      AnyValidFullName(),
               birthDate:     AnyValidBirthDate(),
               yearsLicensed: AnyValidYearsLicensed()) { }

    private static AnyString AnyValidFullName() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly();
    }

    private static AnyInt32 AnyValidYearsLicensed() {
        return Any.Int32().Between(0, 80);
    }

    private AnyDriver(IAny<string>   fullName,
                      IAny<DateOnly> birthDate,
                      IAny<int>      yearsLicensed) {
        _fullName      = fullName;
        _birthDate     = birthDate;
        _yearsLicensed = yearsLicensed;
    }

    /// <summary>Pins <c>fullName</c> to a fixed value.</summary>
    public AnyDriver WithFullName(string value) {
        return new AnyDriver(new FixedValue<string>(value), _birthDate, _yearsLicensed);
    }

    /// <summary>Draws <c>fullName</c> from <c>AnyValidFullName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDriver WithFullName(Func<AnyString, AnyString> configure) {
        return new AnyDriver(configure(AnyValidFullName()), _birthDate, _yearsLicensed);
    }

    /// <summary>Pins <c>birthDate</c> to a fixed value.</summary>
    public AnyDriver WithBirthDate(DateOnly value) {
        return new AnyDriver(_fullName, new FixedValue<DateOnly>(value), _yearsLicensed);
    }

    /// <summary>Draws <c>birthDate</c> from <c>AnyValidBirthDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDriver WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyDriver(_fullName, configure(AnyValidBirthDate()), _yearsLicensed);
    }

    /// <summary>Pins <c>yearsLicensed</c> to a fixed value.</summary>
    public AnyDriver WithYearsLicensed(int value) {
        return new AnyDriver(_fullName, _birthDate, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>yearsLicensed</c> from <c>AnyValidYearsLicensed()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDriver WithYearsLicensed(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDriver(_fullName, _birthDate, configure(AnyValidYearsLicensed()));
    }

    /// <summary>Produces one arbitrary <see cref="Driver" />.</summary>
    public Driver Generate() {
        return new Driver(_fullName.Generate(),
                          _birthDate.Generate(),
                          _yearsLicensed.Generate());
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
