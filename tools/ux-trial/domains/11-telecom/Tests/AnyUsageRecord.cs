// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate UsageRecord --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="UsageRecord" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyUsageRecord : IAny<UsageRecord> {

    private readonly IAny<DateTime>  _occurredAt;
    private readonly IAny<UsageKind> _kind;
    private readonly IAny<long>      _quantity;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyUsageRecord()
        : this(occurredAt: AnyValidOccurredAt(),
               kind:       AnyValidKind(),
               quantity:   AnyValidQuantity()) { }

    private static AnyDateTime AnyValidOccurredAt() {
        return Any.DateTime();
    }

    private static AnyEnum<UsageKind> AnyValidKind() {
        return Any.Enum<UsageKind>();
    }

    private static AnyInt64 AnyValidQuantity() {
        return Any.Int64().Positive();
    }

    private AnyUsageRecord(IAny<DateTime>  occurredAt,
                           IAny<UsageKind> kind,
                           IAny<long>      quantity) {
        _occurredAt = occurredAt;
        _kind       = kind;
        _quantity   = quantity;
    }

    /// <summary>Pins <c>occurredAt</c> to a fixed value.</summary>
    public AnyUsageRecord WithOccurredAt(DateTime value) {
        return new AnyUsageRecord(new FixedValue<DateTime>(value), _kind, _quantity);
    }

    /// <summary>Draws <c>occurredAt</c> from <c>AnyValidOccurredAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyUsageRecord WithOccurredAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyUsageRecord(configure(AnyValidOccurredAt()), _kind, _quantity);
    }

    /// <summary>Pins <c>kind</c> to a fixed value.</summary>
    public AnyUsageRecord WithKind(UsageKind value) {
        return new AnyUsageRecord(_occurredAt, new FixedValue<UsageKind>(value), _quantity);
    }

    /// <summary>Draws <c>kind</c> from <c>AnyValidKind()</c>, refined by <paramref name="configure" />.</summary>
    public AnyUsageRecord WithKind(Func<AnyEnum<UsageKind>, AnyEnum<UsageKind>> configure) {
        return new AnyUsageRecord(_occurredAt, configure(AnyValidKind()), _quantity);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyUsageRecord WithQuantity(long value) {
        return new AnyUsageRecord(_occurredAt, _kind, new FixedValue<long>(value));
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyUsageRecord WithQuantity(Func<AnyInt64, AnyInt64> configure) {
        return new AnyUsageRecord(_occurredAt, _kind, configure(AnyValidQuantity()));
    }

    /// <summary>Produces one arbitrary <see cref="UsageRecord" />.</summary>
    public UsageRecord Generate() {
        DateTime  occurredAt = _occurredAt.Generate();
        UsageKind kind       = _kind.Generate();
        long      quantity   = _quantity.Generate();

        // UsageRecord's cross-field guard: an SMS record counts exactly one message.
        return new UsageRecord(occurredAt, kind, kind == UsageKind.Sms ? 1L : quantity);
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
