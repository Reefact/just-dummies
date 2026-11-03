using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace Payments.Domain;

// Written by hand: the transfers of one batch share their debtor and carry distinct end-to-end ids.
public sealed class AnyBatchTransfers : IAny<IReadOnlyList<CreditTransfer>> {

    public IReadOnlyList<CreditTransfer> Generate() {
        Party        debtor = new AnyParty().Generate();
        int          count  = Any.Int32().Between(1, 20).Generate();
        List<string> ids    = Any.ListOf(Any.String().NonEmpty().WithMaxLength(35)).WithCount(count).Distinct().Generate();

        return ids.Select(id => new AnyCreditTransfer().WithDebtor(debtor).WithEndToEndId(id).Generate()).ToList();
    }

}
