// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Scope --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Scope" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScope : IAny<Scope> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScope()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Mirrors Scope.ScopeFormat: resource, colon, one of three actions.
        return Any.StringMatching("^[a-z][a-z0-9_]{1,31}:(read|write|admin)$");
    }

    private AnyScope(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyScope WithValue(string value) {
        return new AnyScope(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyScope WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyScope(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Scope" />.</summary>
    public Scope Generate() {
        return new Scope(_value.Generate());
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
