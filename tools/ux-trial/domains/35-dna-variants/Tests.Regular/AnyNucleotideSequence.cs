// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NucleotideSequence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="NucleotideSequence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNucleotideSequence : IAny<NucleotideSequence> {

    private readonly IAny<string> _bases;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNucleotideSequence()
        : this(bases: AnyValidBases()) { }

    private static AnyString AnyValidBases() {
        return Any.String().WithChars(NucleotideSequence.Alphabet).WithLengthBetween(1, 200);
    }

    private AnyNucleotideSequence(IAny<string> bases) {
        _bases = bases;
    }

    /// <summary>Pins <c>bases</c> to a fixed value.</summary>
    public AnyNucleotideSequence WithBases(string value) {
        return new AnyNucleotideSequence(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>bases</c> from <c>AnyValidBases()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNucleotideSequence WithBases(Func<AnyString, AnyString> configure) {
        return new AnyNucleotideSequence(configure(AnyValidBases()));
    }

    /// <summary>Produces one arbitrary <see cref="NucleotideSequence" />.</summary>
    public NucleotideSequence Generate() {
        return new NucleotideSequence(_bases.Generate());
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
