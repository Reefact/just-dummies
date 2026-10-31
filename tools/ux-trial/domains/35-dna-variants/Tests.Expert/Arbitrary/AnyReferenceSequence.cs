// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReferenceSequence --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReferenceSequence" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReferenceSequence : IAny<ReferenceSequence> {

    private readonly IAny<Accession>          _accession;
    private readonly IAny<string>             _name;
    private readonly IAny<NucleotideSequence> _sequence;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReferenceSequence()
        : this(accession: AnyValidAccession(),
               name:      AnyValidName(),
               sequence:  AnyValidSequence()) { }

    private static AnyAccession AnyValidAccession() {
        return new AnyAccession();
    }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyNucleotideSequence AnyValidSequence() {
        return new AnyNucleotideSequence();
    }

    private AnyReferenceSequence(IAny<Accession>          accession,
                                 IAny<string>             name,
                                 IAny<NucleotideSequence> sequence) {
        _accession = accession;
        _name      = name;
        _sequence  = sequence;
    }

    /// <summary>Pins <c>accession</c> to a fixed value.</summary>
    public AnyReferenceSequence WithAccession(Accession value) {
        return new AnyReferenceSequence(new FixedValue<Accession>(value), _name, _sequence);
    }

    /// <summary>Draws <c>accession</c> from <c>AnyValidAccession()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceSequence WithAccession(Func<AnyAccession, AnyAccession> configure) {
        return new AnyReferenceSequence(configure(AnyValidAccession()), _name, _sequence);
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyReferenceSequence WithName(string value) {
        return new AnyReferenceSequence(_accession, new FixedValue<string>(value), _sequence);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceSequence WithName(Func<AnyString, AnyString> configure) {
        return new AnyReferenceSequence(_accession, configure(AnyValidName()), _sequence);
    }

    /// <summary>Pins <c>sequence</c> to a fixed value.</summary>
    public AnyReferenceSequence WithSequence(NucleotideSequence value) {
        return new AnyReferenceSequence(_accession, _name, new FixedValue<NucleotideSequence>(value));
    }

    /// <summary>Draws <c>sequence</c> from <c>AnyValidSequence()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReferenceSequence WithSequence(Func<AnyNucleotideSequence, AnyNucleotideSequence> configure) {
        return new AnyReferenceSequence(_accession, _name, configure(AnyValidSequence()));
    }

    /// <summary>Produces one arbitrary <see cref="ReferenceSequence" />.</summary>
    public ReferenceSequence Generate() {
        return new ReferenceSequence(_accession.Generate(),
                                     _name.Generate(),
                                     _sequence.Generate());
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
