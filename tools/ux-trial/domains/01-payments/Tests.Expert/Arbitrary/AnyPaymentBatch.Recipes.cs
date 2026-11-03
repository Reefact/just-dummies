using System;
using System.Collections.Generic;

using JustDummies;

namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyPaymentBatch.cs: `any generate PaymentBatch --force` leaves it alone.
public sealed partial class AnyPaymentBatch {

    // The domain keeps the limit of a hundred transfers in a literal of its constructor; the generator cannot reach it.
    private static class PaymentBatchLimits {

        public const int MaximumTransfers = 100;

    }

    /// <summary>
    ///     Transfers of one debtor, with distinct end-to-end ids: the two rules a batch holds that a transfer cannot know.
    ///     <paramref name="shape" /> says how many.
    /// </summary>
    internal static IAny<IReadOnlyList<CreditTransfer>> TransfersFromOneDebtor(Func<AnyList<CreditTransfer>, AnyList<CreditTransfer>> shape) {
        return new AnyParty()
                             .Then(debtor => shape(Any.ListOf(new AnyCreditTransfer().WithDebtor(debtor)))
                                                 .DistinctBy(transfer => transfer.EndToEndId)
                                                 .As(transfers => (IReadOnlyList<CreditTransfer>)transfers));
    }

    /// <summary>A batch of exactly <paramref name="count" /> transfers, all from one debtor.</summary>
    public AnyPaymentBatch WithTransferCount(int count) {
        AnyPaymentBatch copy = new AnyPaymentBatch(this);

        copy._transfers = TransfersFromOneDebtor(transfers => transfers.WithCount(count));

        return copy;
    }

}
