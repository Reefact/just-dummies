// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BreedingPlan --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BreedingPlan" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBreedingPlan : IAny<BreedingPlan> {

    private readonly IAny<Pedigree>      _pedigree;
    private readonly IAny<KinshipPolicy> _policy;
    private readonly IAny<DateOnly>      _seasonStart;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBreedingPlan()
        : this(pedigree:    AnyValidPedigree(),
               policy:      AnyValidPolicy(),
               seasonStart: AnyValidSeasonStart()) { }

    private static AnyPedigree AnyValidPedigree() {
        return new AnyPedigree();
    }

    private static AnyKinshipPolicy AnyValidPolicy() {
        return new AnyKinshipPolicy();
    }

    private static AnyDateOnly AnyValidSeasonStart() {
        return Any.DateOnly();
    }

    private AnyBreedingPlan(IAny<Pedigree>      pedigree,
                            IAny<KinshipPolicy> policy,
                            IAny<DateOnly>      seasonStart) {
        _pedigree    = pedigree;
        _policy      = policy;
        _seasonStart = seasonStart;
    }

    /// <summary>Pins <c>pedigree</c> to a fixed value.</summary>
    public AnyBreedingPlan WithPedigree(Pedigree value) {
        return new AnyBreedingPlan(new FixedValue<Pedigree>(value), _policy, _seasonStart);
    }

    /// <summary>Draws <c>pedigree</c> from <c>AnyValidPedigree()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPlan WithPedigree(Func<AnyPedigree, AnyPedigree> configure) {
        return new AnyBreedingPlan(configure(AnyValidPedigree()), _policy, _seasonStart);
    }

    /// <summary>Pins <c>policy</c> to a fixed value.</summary>
    public AnyBreedingPlan WithPolicy(KinshipPolicy value) {
        return new AnyBreedingPlan(_pedigree, new FixedValue<KinshipPolicy>(value), _seasonStart);
    }

    /// <summary>Draws <c>policy</c> from <c>AnyValidPolicy()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPlan WithPolicy(Func<AnyKinshipPolicy, AnyKinshipPolicy> configure) {
        return new AnyBreedingPlan(_pedigree, configure(AnyValidPolicy()), _seasonStart);
    }

    /// <summary>Pins <c>seasonStart</c> to a fixed value.</summary>
    public AnyBreedingPlan WithSeasonStart(DateOnly value) {
        return new AnyBreedingPlan(_pedigree, _policy, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>seasonStart</c> from <c>AnyValidSeasonStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBreedingPlan WithSeasonStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyBreedingPlan(_pedigree, _policy, configure(AnyValidSeasonStart()));
    }

    /// <summary>Produces one arbitrary <see cref="BreedingPlan" />.</summary>
    public BreedingPlan Generate() {
        return new BreedingPlan(_pedigree.Generate(),
                                _policy.Generate(),
                                _seasonStart.Generate());
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
