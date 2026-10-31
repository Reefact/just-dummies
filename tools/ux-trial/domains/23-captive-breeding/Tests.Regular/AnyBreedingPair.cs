// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BreedingPair --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BreedingPair" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBreedingPair : IAny<BreedingPair> {

    private readonly IAny<StudbookId>         _sireId;
    private readonly IAny<StudbookId>         _damId;
    private readonly IAny<KinshipCoefficient> _kinship;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBreedingPair()
        : this(sireId:  AnyValidSireId(),
               damId:   AnyValidDamId(),
               kinship: AnyValidKinship()) { }

    private static AnyStudbookId AnyValidSireId() {
        return new AnyStudbookId();
    }

    private static AnyStudbookId AnyValidDamId() {
        return new AnyStudbookId();
    }

    private static AnyKinshipCoefficient AnyValidKinship() {
        // Two distinct individuals are at most 0.5 akin.
        return new AnyKinshipCoefficient().WithValue(v => v.LessThanOrEqualTo(0.5));
    }

    private AnyBreedingPair(IAny<StudbookId>         sireId,
                            IAny<StudbookId>         damId,
                            IAny<KinshipCoefficient> kinship) {
        _sireId  = sireId;
        _damId   = damId;
        _kinship = kinship;
    }

    /// <summary>Pins <c>sireId</c> to a fixed value.</summary>
    public AnyBreedingPair WithSireId(StudbookId value) {
        return new AnyBreedingPair(new FixedValue<StudbookId>(value), _damId, _kinship);
    }

    /// <summary>Draws <c>sireId</c> from <c>AnyValidSireId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPair WithSireId(Func<AnyStudbookId, AnyStudbookId> configure) {
        return new AnyBreedingPair(configure(AnyValidSireId()), _damId, _kinship);
    }

    /// <summary>Pins <c>damId</c> to a fixed value.</summary>
    public AnyBreedingPair WithDamId(StudbookId value) {
        return new AnyBreedingPair(_sireId, new FixedValue<StudbookId>(value), _kinship);
    }

    /// <summary>Draws <c>damId</c> from <c>AnyValidDamId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPair WithDamId(Func<AnyStudbookId, AnyStudbookId> configure) {
        return new AnyBreedingPair(_sireId, configure(AnyValidDamId()), _kinship);
    }

    /// <summary>Pins <c>kinship</c> to a fixed value.</summary>
    public AnyBreedingPair WithKinship(KinshipCoefficient value) {
        return new AnyBreedingPair(_sireId, _damId, new FixedValue<KinshipCoefficient>(value));
    }

    /// <summary>Draws <c>kinship</c> from <c>AnyValidKinship()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPair WithKinship(Func<AnyKinshipCoefficient, AnyKinshipCoefficient> configure) {
        return new AnyBreedingPair(_sireId, _damId, configure(AnyValidKinship()));
    }

    /// <summary>Produces one arbitrary <see cref="BreedingPair" />.</summary>
    public BreedingPair Generate() {
        return new BreedingPair(_sireId.Generate(),
                                _damId.Generate(),
                                _kinship.Generate());
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
