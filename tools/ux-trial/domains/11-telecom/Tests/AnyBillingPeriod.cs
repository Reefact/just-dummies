// Scaffolded by `any` (JustDummies), then reshaped by hand: BillingPeriod's guards relate its two
// parameters (start < end, at most 31 days apart), which two independent draws cannot honour. The
// generator therefore draws a start and a length, and derives the end.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>A generator of arbitrary <see cref="BillingPeriod" /> values.</summary>
public sealed partial class AnyBillingPeriod : IAny<BillingPeriod> {

    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _lengthInDays;

    public AnyBillingPeriod()
        : this(start: AnyValidStart(), lengthInDays: AnyValidLengthInDays()) { }

    private static AnyDateOnly AnyValidStart() {
        // Keeps start + 31 days representable.
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 1));
    }

    private static AnyInt32 AnyValidLengthInDays() {
        return Any.Int32().Between(1, 31);
    }

    private AnyBillingPeriod(IAny<DateOnly> start, IAny<int> lengthInDays) {
        _start        = start;
        _lengthInDays = lengthInDays;
    }

    public AnyBillingPeriod WithStart(DateOnly value) {
        return new AnyBillingPeriod(new FixedValue<DateOnly>(value), _lengthInDays);
    }

    public AnyBillingPeriod WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyBillingPeriod(configure(AnyValidStart()), _lengthInDays);
    }

    public AnyBillingPeriod WithLengthInDays(int value) {
        return new AnyBillingPeriod(_start, new FixedValue<int>(value));
    }

    public AnyBillingPeriod WithLengthInDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBillingPeriod(_start, configure(AnyValidLengthInDays()));
    }

    public BillingPeriod Generate() {
        DateOnly start = _start.Generate();
        return new BillingPeriod(start, start.AddDays(_lengthInDays.Generate()));
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
