using JustDummies;

namespace Payments.Domain;

public sealed partial class AnyPaymentBatch {

    /// <summary>One debtor shared by every transfer, and end-to-end identifiers unique in the batch.</summary>
    private static IAny<IReadOnlyList<CreditTransfer>> AnyTransfersOfOneDebtor() {
        return Any.Combine(new AnyParty(),
                           Any.ListOf(Any.String().AlphaNumeric().WithLengthBetween(1, 35)).WithCountBetween(1, 100).Distinct(),
                           (debtor, endToEndIds) => (IReadOnlyList<CreditTransfer>)endToEndIds
                               .Select(id => new AnyCreditTransfer().WithDebtor(debtor).WithEndToEndId(id).Generate())
                               .ToList());
    }

}
