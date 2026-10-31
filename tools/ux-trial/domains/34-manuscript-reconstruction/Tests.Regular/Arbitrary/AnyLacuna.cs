// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Lacuna --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Lacuna" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLacuna : IAny<Lacuna> {

    private readonly IAny<WitnessSiglum>    _witness;
    private readonly IAny<PassageReference> _from;
    private readonly IAny<PassageReference> _to;
    private readonly IAny<string>           _cause;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLacuna()
        : this(witness: AnyValidWitness(),
               from:    AnyValidFrom(),
               to:      AnyValidTo(),
               cause:   AnyValidCause()) { }

    private static AnyWitnessSiglum AnyValidWitness() {
        return new AnyWitnessSiglum();
    }

    private static AnyPassageReference AnyValidFrom() {
        // from <= to: 'from' lies in books 1-49 and 'to' in books 50-99.
        return new AnyPassageReference().WithBook(b => b.LessThanOrEqualTo(49));
    }

    private static AnyPassageReference AnyValidTo() {
        return new AnyPassageReference().WithBook(b => b.GreaterThanOrEqualTo(50));
    }

    private static AnyString AnyValidCause() {
        return Any.String().NotBlank();
    }

    private AnyLacuna(IAny<WitnessSiglum>    witness,
                      IAny<PassageReference> from,
                      IAny<PassageReference> to,
                      IAny<string>           cause) {
        _witness = witness;
        _from    = from;
        _to      = to;
        _cause   = cause;
    }

    /// <summary>Pins <c>witness</c> to a fixed value.</summary>
    public AnyLacuna WithWitness(WitnessSiglum value) {
        return new AnyLacuna(new FixedValue<WitnessSiglum>(value), _from, _to, _cause);
    }

    /// <summary>Draws <c>witness</c> from <c>AnyValidWitness()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLacuna WithWitness(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        return new AnyLacuna(configure(AnyValidWitness()), _from, _to, _cause);
    }

    /// <summary>Pins <c>from</c> to a fixed value.</summary>
    public AnyLacuna WithFrom(PassageReference value) {
        return new AnyLacuna(_witness, new FixedValue<PassageReference>(value), _to, _cause);
    }

    /// <summary>Draws <c>from</c> from <c>AnyValidFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLacuna WithFrom(Func<AnyPassageReference, AnyPassageReference> configure) {
        return new AnyLacuna(_witness, configure(AnyValidFrom()), _to, _cause);
    }

    /// <summary>Pins <c>to</c> to a fixed value.</summary>
    public AnyLacuna WithTo(PassageReference value) {
        return new AnyLacuna(_witness, _from, new FixedValue<PassageReference>(value), _cause);
    }

    /// <summary>Draws <c>to</c> from <c>AnyValidTo()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLacuna WithTo(Func<AnyPassageReference, AnyPassageReference> configure) {
        return new AnyLacuna(_witness, _from, configure(AnyValidTo()), _cause);
    }

    /// <summary>Pins <c>cause</c> to a fixed value.</summary>
    public AnyLacuna WithCause(string value) {
        return new AnyLacuna(_witness, _from, _to, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>cause</c> from <c>AnyValidCause()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLacuna WithCause(Func<AnyString, AnyString> configure) {
        return new AnyLacuna(_witness, _from, _to, configure(AnyValidCause()));
    }

    /// <summary>Produces one arbitrary <see cref="Lacuna" />.</summary>
    public Lacuna Generate() {
        return new Lacuna(_witness.Generate(),
                          _from.Generate(),
                          _to.Generate(),
                          _cause.Generate());
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
