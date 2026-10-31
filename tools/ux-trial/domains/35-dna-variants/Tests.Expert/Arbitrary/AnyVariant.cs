// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Variant --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace DnaVariants.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Variant" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVariant : IAny<Variant> {

    private readonly IAny<Accession> _accession;
    private readonly IAny<int>       _position;
    private readonly IAny<Allele>    _reference;
    private readonly IAny<Allele>    _alternate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVariant()
        : this(accession: AnyValidAccession(),
               position:  AnyValidPosition(),
               reference: AnyValidReference(),
               alternate: AnyValidAlternate()) { }

    private static AnyAccession AnyValidAccession() {
        return new AnyAccession();
    }

    private static AnyInt32 AnyValidPosition() {
        return Any.Int32().Positive();
    }

    private static AnyAllele AnyValidReference() {
        // The guard reference != alternate ties the two alleles: Generate() honours it, not this factory.
        return new AnyAllele();
    }

    private static AnyAllele AnyValidAlternate() {
        // See AnyValidReference(): the tie between the two alleles lives in Generate().
        return new AnyAllele();
    }

    private AnyVariant(IAny<Accession> accession,
                       IAny<int>       position,
                       IAny<Allele>    reference,
                       IAny<Allele>    alternate) {
        _accession = accession;
        _position  = position;
        _reference = reference;
        _alternate = alternate;
    }

    /// <summary>Pins <c>accession</c> to a fixed value.</summary>
    public AnyVariant WithAccession(Accession value) {
        return new AnyVariant(new FixedValue<Accession>(value), _position, _reference, _alternate);
    }

    /// <summary>Draws <c>accession</c> from <c>AnyValidAccession()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithAccession(Func<AnyAccession, AnyAccession> configure) {
        return new AnyVariant(configure(AnyValidAccession()), _position, _reference, _alternate);
    }

    /// <summary>Pins <c>position</c> to a fixed value.</summary>
    public AnyVariant WithPosition(int value) {
        return new AnyVariant(_accession, new FixedValue<int>(value), _reference, _alternate);
    }

    /// <summary>Draws <c>position</c> from <c>AnyValidPosition()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithPosition(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVariant(_accession, configure(AnyValidPosition()), _reference, _alternate);
    }

    /// <summary>Pins <c>reference</c> to a fixed value.</summary>
    public AnyVariant WithReference(Allele value) {
        return new AnyVariant(_accession, _position, new FixedValue<Allele>(value), _alternate);
    }

    /// <summary>Draws <c>reference</c> from <c>AnyValidReference()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithReference(Func<AnyAllele, AnyAllele> configure) {
        return new AnyVariant(_accession, _position, configure(AnyValidReference()), _alternate);
    }

    /// <summary>Pins <c>alternate</c> to a fixed value.</summary>
    public AnyVariant WithAlternate(Allele value) {
        return new AnyVariant(_accession, _position, _reference, new FixedValue<Allele>(value));
    }

    /// <summary>Draws <c>alternate</c> from <c>AnyValidAlternate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVariant WithAlternate(Func<AnyAllele, AnyAllele> configure) {
        return new AnyVariant(_accession, _position, _reference, configure(AnyValidAlternate()));
    }

    /// <summary>Produces one arbitrary <see cref="Variant" />.</summary>
    public Variant Generate() {
        // A variant changes something: its two alleles differ. Nothing in the library states "this draw differs
        // from that one" across two composed generators, so the tie is a bounded redraw. A pinned allele stays
        // pinned (its generator returns the same value), and two equal pins fail in Variant's own guard.
        Allele reference = _reference.Generate();
        Allele alternate = _alternate.Generate();
        for (int attempt = 0; attempt < MaxAlleleRedraws && reference.Equals(alternate); attempt++) {
            reference = _reference.Generate();
            alternate = _alternate.Generate();
        }

        return new Variant(_accession.Generate(),
                           _position.Generate(),
                           reference,
                           alternate);
    }

    private const int MaxAlleleRedraws = 16;

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
