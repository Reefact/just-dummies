// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RadiocarbonAge --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RadiocarbonAge" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRadiocarbonAge : IAny<RadiocarbonAge> {

    private readonly IAny<int> _yearsBp;
    private readonly IAny<int> _uncertainty;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRadiocarbonAge()
        : this(yearsBp:     AnyValidYearsBp(),
               uncertainty: AnyValidUncertainty()) { }

    private static AnyInt32 AnyValidYearsBp() {
        return Any.Int32().Between(0, RadiocarbonAge.MaximumYearsBp);
    }

    private static AnyInt32 AnyValidUncertainty() {
        return Any.Int32().Between(1, RadiocarbonAge.MaximumUncertainty);
    }

    private AnyRadiocarbonAge(IAny<int> yearsBp,
                              IAny<int> uncertainty) {
        _yearsBp     = yearsBp;
        _uncertainty = uncertainty;
    }

    /// <summary>Pins <c>yearsBp</c> to a fixed value.</summary>
    public AnyRadiocarbonAge WithYearsBp(int value) {
        return new AnyRadiocarbonAge(new FixedValue<int>(value), _uncertainty);
    }

    /// <summary>Draws <c>yearsBp</c> from <c>AnyValidYearsBp()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRadiocarbonAge WithYearsBp(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRadiocarbonAge(configure(AnyValidYearsBp()), _uncertainty);
    }

    /// <summary>Pins <c>uncertainty</c> to a fixed value.</summary>
    public AnyRadiocarbonAge WithUncertainty(int value) {
        return new AnyRadiocarbonAge(_yearsBp, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>uncertainty</c> from <c>AnyValidUncertainty()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRadiocarbonAge WithUncertainty(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRadiocarbonAge(_yearsBp, configure(AnyValidUncertainty()));
    }

    /// <summary>Produces one arbitrary <see cref="RadiocarbonAge" />.</summary>
    public RadiocarbonAge Generate() {
        return new RadiocarbonAge(_yearsBp.Generate(),
                                  _uncertainty.Generate());
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
