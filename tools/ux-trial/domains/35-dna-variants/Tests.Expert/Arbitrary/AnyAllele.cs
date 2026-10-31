// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Allele --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Allele" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAllele : IAny<Allele> {

    private readonly IAny<string> _bases;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAllele()
        : this(bases: AnyValidBases()) { }

    private static AnyString AnyValidBases() {
        // Called bases only (no N), case-insensitive, and empty is a legal allele (an insertion's reference side).
        return Any.String().WithChars("ACGTacgt").WithMaxLength(Allele.MaxLength);
    }

    private AnyAllele(IAny<string> bases) {
        _bases = bases;
    }

    /// <summary>Pins <c>bases</c> to a fixed value.</summary>
    public AnyAllele WithBases(string value) {
        return new AnyAllele(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>bases</c> from <c>AnyValidBases()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAllele WithBases(Func<AnyString, AnyString> configure) {
        return new AnyAllele(configure(AnyValidBases()));
    }

    /// <summary>Produces one arbitrary <see cref="Allele" />.</summary>
    public Allele Generate() {
        return new Allele(_bases.Generate());
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
