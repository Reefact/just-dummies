// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Accession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Accession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAccession : IAny<Accession> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAccession()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Accession.cs guards the value with ^[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}$; the same language, drawn.
        return Any.StringMatching(@"[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}");
    }

    private AnyAccession(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyAccession WithValue(string value) {
        return new AnyAccession(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAccession WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyAccession(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Accession" />.</summary>
    public Accession Generate() {
        return new Accession(_value.Generate());
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
