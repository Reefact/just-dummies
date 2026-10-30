// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Role --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Role" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRole : IAny<Role> {

    private readonly IAny<string> _name;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRole()
        : this(name: AnyValidName()) { }

    private static AnyString AnyValidName() {
        return Any.String().NonEmpty();
    }

    private AnyRole(IAny<string> name) {
        _name = name;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyRole WithName(string value) {
        return new AnyRole(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRole WithName(Func<AnyString, AnyString> configure) {
        return new AnyRole(configure(AnyValidName()));
    }

    /// <summary>Produces one arbitrary <see cref="Role" />.</summary>
    public Role Generate() {
        return new Role(_name.Generate());
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
