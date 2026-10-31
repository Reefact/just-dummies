// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BehaviouralAssumption --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BehaviouralAssumption" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBehaviouralAssumption : IAny<BehaviouralAssumption> {

    private readonly IAny<decimal> _stableShare;
    private readonly IAny<int>     _volatileRunOffDays;
    private readonly IAny<int>     _coreMaturityMonths;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBehaviouralAssumption()
        : this(stableShare:        AnyValidStableShare(),
               volatileRunOffDays: AnyValidVolatileRunOffDays(),
               coreMaturityMonths: AnyValidCoreMaturityMonths()) { }

    private static AnyDecimal AnyValidStableShare() {
        return Any.Decimal().Between(0m, 1m);
    }

    private static AnyInt32 AnyValidVolatileRunOffDays() {
        // The volatile horizon must be shorter than the core maturity (volatileRunOffDays < coreMaturityMonths * 30),
        // a rule between two parameters: the core maturity's base starts at 13 months so any horizon up to 365 days fits.
        return Any.Int32().Between(1, 365);
    }

    private static AnyInt32 AnyValidCoreMaturityMonths() {
        return Any.Int32().Between(13, 120);
    }

    private AnyBehaviouralAssumption(IAny<decimal> stableShare,
                                     IAny<int>     volatileRunOffDays,
                                     IAny<int>     coreMaturityMonths) {
        _stableShare        = stableShare;
        _volatileRunOffDays = volatileRunOffDays;
        _coreMaturityMonths = coreMaturityMonths;
    }

    /// <summary>Pins <c>stableShare</c> to a fixed value.</summary>
    public AnyBehaviouralAssumption WithStableShare(decimal value) {
        return new AnyBehaviouralAssumption(new FixedValue<decimal>(value), _volatileRunOffDays, _coreMaturityMonths);
    }

    /// <summary>Draws <c>stableShare</c> from <c>AnyValidStableShare()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBehaviouralAssumption WithStableShare(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyBehaviouralAssumption(configure(AnyValidStableShare()), _volatileRunOffDays, _coreMaturityMonths);
    }

    /// <summary>Pins <c>volatileRunOffDays</c> to a fixed value.</summary>
    public AnyBehaviouralAssumption WithVolatileRunOffDays(int value) {
        return new AnyBehaviouralAssumption(_stableShare, new FixedValue<int>(value), _coreMaturityMonths);
    }

    /// <summary>Draws <c>volatileRunOffDays</c> from <c>AnyValidVolatileRunOffDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBehaviouralAssumption WithVolatileRunOffDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBehaviouralAssumption(_stableShare, configure(AnyValidVolatileRunOffDays()), _coreMaturityMonths);
    }

    /// <summary>Pins <c>coreMaturityMonths</c> to a fixed value.</summary>
    public AnyBehaviouralAssumption WithCoreMaturityMonths(int value) {
        return new AnyBehaviouralAssumption(_stableShare, _volatileRunOffDays, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>coreMaturityMonths</c> from <c>AnyValidCoreMaturityMonths()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBehaviouralAssumption WithCoreMaturityMonths(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBehaviouralAssumption(_stableShare, _volatileRunOffDays, configure(AnyValidCoreMaturityMonths()));
    }

    /// <summary>Produces one arbitrary <see cref="BehaviouralAssumption" />.</summary>
    public BehaviouralAssumption Generate() {
        return new BehaviouralAssumption(_stableShare.Generate(),
                                         _volatileRunOffDays.Generate(),
                                         _coreMaturityMonths.Generate());
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
