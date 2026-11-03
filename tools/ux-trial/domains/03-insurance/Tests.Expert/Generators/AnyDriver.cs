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

    private IAny<string>   _fullName;
    private IAny<DateOnly> _birthDate;
    private IAny<int>      _yearsLicensed;

    private AnyString   _fullNameRecipe;
    private AnyDateOnly _birthDateRecipe;
    private AnyInt32    _yearsLicensedRecipe;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDriver(AnyString   fullName,
                      AnyDateOnly birthDate,
                      AnyInt32    yearsLicensed) {
        _fullName            = fullName;
        _fullNameRecipe      = fullName;
        _birthDate           = birthDate;
        _birthDateRecipe     = birthDate;
        _yearsLicensed       = yearsLicensed;
        _yearsLicensedRecipe = yearsLicensed;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDriver(AnyDriver from) {
        _fullName            = from._fullName;
        _fullNameRecipe      = from._fullNameRecipe;
        _birthDate           = from._birthDate;
        _birthDateRecipe     = from._birthDateRecipe;
        _yearsLicensed       = from._yearsLicensed;
        _yearsLicensedRecipe = from._yearsLicensedRecipe;
    }

    /// <summary>Pins <c>fullName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDriver WithFullName(string value) {
        AnyDriver copy = new AnyDriver(this);

        copy._fullName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fullName</c> is drawn from — <c>AnyValidFullName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDriver WithFullName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDriver copy = new AnyDriver(this);

        copy._fullNameRecipe = configure(_fullNameRecipe);
        copy._fullName       = copy._fullNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>birthDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDriver WithBirthDate(DateOnly value) {
        AnyDriver copy = new AnyDriver(this);

        copy._birthDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>birthDate</c> is drawn from — <c>AnyValidBirthDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDriver WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDriver copy = new AnyDriver(this);

        copy._birthDateRecipe = configure(_birthDateRecipe);
        copy._birthDate       = copy._birthDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>yearsLicensed</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDriver WithYearsLicensed(int value) {
        AnyDriver copy = new AnyDriver(this);

        copy._yearsLicensed = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yearsLicensed</c> is drawn from — <c>AnyValidYearsLicensed()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDriver WithYearsLicensed(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDriver copy = new AnyDriver(this);

        copy._yearsLicensedRecipe = configure(_yearsLicensedRecipe);
        copy._yearsLicensed       = copy._yearsLicensedRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Driver" />.</summary>
    public Driver Generate() {
        return new Driver(_fullName.Generate(),
                          _birthDate.Generate(),
                          _yearsLicensed.Generate());
    }

}
