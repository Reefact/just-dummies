// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Molecule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Clinical.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Molecule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMolecule : IAny<Molecule> {

    private readonly IAny<string> _name;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMolecule()
        : this(name: AnyValidName()) { }

    private static AnyPattern AnyValidName() {
        // An INN: lower-case letters, spaces and hyphens, starting with a letter, at most 80 characters.
        return Any.StringMatching("[a-z][a-z -]{0,79}");
    }

    private AnyMolecule(IAny<string> name) {
        _name = name;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyMolecule WithName(string value) {
        return new AnyMolecule(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMolecule WithName(Func<AnyPattern, AnyPattern> configure) {
        return new AnyMolecule(configure(AnyValidName()));
    }

    /// <summary>Produces one arbitrary <see cref="Molecule" />.</summary>
    public Molecule Generate() {
        return new Molecule(_name.Generate());
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
