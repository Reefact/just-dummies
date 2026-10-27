// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PaymentBatch --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PaymentBatch" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPaymentBatch : IAny<PaymentBatch> {

    private readonly IAny<string>                        _messageId;
    private readonly IAny<DateTime>                      _createdAt;
    private readonly IAny<IReadOnlyList<CreditTransfer>> _transfers;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPaymentBatch()
        : this(messageId: AnyValidMessageId(),
               createdAt: AnyValidCreatedAt(),
               transfers: AnyValidTransfers()) { }

    private static AnyString AnyValidMessageId() {
        return Any.String().NotBlank();
    }

    private static AnyDateTime AnyValidCreatedAt() {
        return Any.DateTime();
    }

    private static IAny<IReadOnlyList<CreditTransfer>> AnyValidTransfers() {
        return AnyTransfersOfOneDebtor();
    }

    private AnyPaymentBatch(IAny<string>                        messageId,
                            IAny<DateTime>                      createdAt,
                            IAny<IReadOnlyList<CreditTransfer>> transfers) {
        _messageId = messageId;
        _createdAt = createdAt;
        _transfers = transfers;
    }

    /// <summary>Pins <c>messageId</c> to a fixed value.</summary>
    public AnyPaymentBatch WithMessageId(string value) {
        return new AnyPaymentBatch(new FixedValue<string>(value), _createdAt, _transfers);
    }

    /// <summary>Draws <c>messageId</c> from <c>AnyValidMessageId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPaymentBatch WithMessageId(Func<AnyString, AnyString> configure) {
        return new AnyPaymentBatch(configure(AnyValidMessageId()), _createdAt, _transfers);
    }

    /// <summary>Pins <c>createdAt</c> to a fixed value.</summary>
    public AnyPaymentBatch WithCreatedAt(DateTime value) {
        return new AnyPaymentBatch(_messageId, new FixedValue<DateTime>(value), _transfers);
    }

    /// <summary>Draws <c>createdAt</c> from <c>AnyValidCreatedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPaymentBatch WithCreatedAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyPaymentBatch(_messageId, configure(AnyValidCreatedAt()), _transfers);
    }

    /// <summary>Pins <c>transfers</c> to a fixed value.</summary>
    public AnyPaymentBatch WithTransfers(IReadOnlyList<CreditTransfer> value) {
        return new AnyPaymentBatch(_messageId, _createdAt, new FixedValue<IReadOnlyList<CreditTransfer>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="PaymentBatch" />.</summary>
    public PaymentBatch Generate() {
        return new PaymentBatch(_messageId.Generate(),
                                _createdAt.Generate(),
                                _transfers.Generate());
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
