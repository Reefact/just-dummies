// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PoolRules --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace ParimutuelBetting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PoolRules" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPoolRules : IAny<PoolRules> {

    private readonly IAny<decimal> _takeoutRate;
    private readonly IAny<decimal> _breakageStep;
    private readonly IAny<decimal> _minimumReturnRatio;
    private readonly IAny<int>     _placesPaid;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPoolRules()
        : this(takeoutRate:        AnyValidTakeoutRate(),
               breakageStep:       AnyValidBreakageStep(),
               minimumReturnRatio: AnyValidMinimumReturnRatio(),
               placesPaid:         AnyValidPlacesPaid()) { }

    private static AnyDecimal AnyValidTakeoutRate() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(0.5m).WithScale(3);
    }

    private static AnyDecimal AnyValidBreakageStep() {
        // A whole number of cents.
        return Any.Decimal().Between(0.01m, 1m).WithScale(2);
    }

    private static AnyDecimal AnyValidMinimumReturnRatio() {
        return Any.Decimal().Between(1m, 1.5m).WithScale(2);
    }

    private static AnyInt32 AnyValidPlacesPaid() {
        return Any.Int32().Between(1, 3);
    }

    private AnyPoolRules(IAny<decimal> takeoutRate,
                         IAny<decimal> breakageStep,
                         IAny<decimal> minimumReturnRatio,
                         IAny<int>     placesPaid) {
        _takeoutRate        = takeoutRate;
        _breakageStep       = breakageStep;
        _minimumReturnRatio = minimumReturnRatio;
        _placesPaid         = placesPaid;
    }

    /// <summary>Pins <c>takeoutRate</c> to a fixed value.</summary>
    public AnyPoolRules WithTakeoutRate(decimal value) {
        return new AnyPoolRules(new FixedValue<decimal>(value), _breakageStep, _minimumReturnRatio, _placesPaid);
    }

    /// <summary>Draws <c>takeoutRate</c> from <c>AnyValidTakeoutRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoolRules WithTakeoutRate(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPoolRules(configure(AnyValidTakeoutRate()), _breakageStep, _minimumReturnRatio, _placesPaid);
    }

    /// <summary>Pins <c>breakageStep</c> to a fixed value.</summary>
    public AnyPoolRules WithBreakageStep(decimal value) {
        return new AnyPoolRules(_takeoutRate, new FixedValue<decimal>(value), _minimumReturnRatio, _placesPaid);
    }

    /// <summary>Draws <c>breakageStep</c> from <c>AnyValidBreakageStep()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoolRules WithBreakageStep(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPoolRules(_takeoutRate, configure(AnyValidBreakageStep()), _minimumReturnRatio, _placesPaid);
    }

    /// <summary>Pins <c>minimumReturnRatio</c> to a fixed value.</summary>
    public AnyPoolRules WithMinimumReturnRatio(decimal value) {
        return new AnyPoolRules(_takeoutRate, _breakageStep, new FixedValue<decimal>(value), _placesPaid);
    }

    /// <summary>Draws <c>minimumReturnRatio</c> from <c>AnyValidMinimumReturnRatio()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoolRules WithMinimumReturnRatio(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPoolRules(_takeoutRate, _breakageStep, configure(AnyValidMinimumReturnRatio()), _placesPaid);
    }

    /// <summary>Pins <c>placesPaid</c> to a fixed value.</summary>
    public AnyPoolRules WithPlacesPaid(int value) {
        return new AnyPoolRules(_takeoutRate, _breakageStep, _minimumReturnRatio, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>placesPaid</c> from <c>AnyValidPlacesPaid()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPoolRules WithPlacesPaid(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPoolRules(_takeoutRate, _breakageStep, _minimumReturnRatio, configure(AnyValidPlacesPaid()));
    }

    /// <summary>Produces one arbitrary <see cref="PoolRules" />.</summary>
    public PoolRules Generate() {
        return new PoolRules(_takeoutRate.Generate(),
                             _breakageStep.Generate(),
                             _minimumReturnRatio.Generate(),
                             _placesPaid.Generate());
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
