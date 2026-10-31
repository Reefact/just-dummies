// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Pedigree --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Pedigree" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPedigree : IAny<Pedigree> {

    private readonly IAny<string> _programmeName;
    private readonly IAny<string> _species;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPedigree()
        : this(programmeName: AnyValidProgrammeName(),
               species:       AnyValidSpecies()) { }

    private static AnyString AnyValidProgrammeName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidSpecies() {
        return Any.String().NotBlank();
    }

    private AnyPedigree(IAny<string> programmeName,
                        IAny<string> species) {
        _programmeName = programmeName;
        _species       = species;
    }

    /// <summary>Pins <c>programmeName</c> to a fixed value.</summary>
    public AnyPedigree WithProgrammeName(string value) {
        return new AnyPedigree(new FixedValue<string>(value), _species);
    }

    /// <summary>Draws <c>programmeName</c> from <c>AnyValidProgrammeName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPedigree WithProgrammeName(Func<AnyString, AnyString> configure) {
        return new AnyPedigree(configure(AnyValidProgrammeName()), _species);
    }

    /// <summary>Pins <c>species</c> to a fixed value.</summary>
    public AnyPedigree WithSpecies(string value) {
        return new AnyPedigree(_programmeName, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>species</c> from <c>AnyValidSpecies()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPedigree WithSpecies(Func<AnyString, AnyString> configure) {
        return new AnyPedigree(_programmeName, configure(AnyValidSpecies()));
    }

    /// <summary>Produces one arbitrary <see cref="Pedigree" />.</summary>
    public Pedigree Generate() {
        return new Pedigree(_programmeName.Generate(),
                            _species.Generate());
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
