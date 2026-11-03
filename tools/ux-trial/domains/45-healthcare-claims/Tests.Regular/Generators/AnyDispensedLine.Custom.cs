using HealthcareClaims.Domain;

using JustDummies;

using Payments.Domain;

namespace HealthcareClaims.Tests.Regular.Generators;

public sealed partial class AnyDispensedLine {

    // An unlisted medicine is never reimbursed by the statutory scheme; any other may be, by at most the price paid.
    private static Money? DrawStatutoryReimbursement(ReimbursementCategory category, decimal pricePaid) {
        if (category == ReimbursementCategory.Unlisted) { return null; }

        return Any.Decimal().Between(0.01m, pricePaid).WithScale(2)
                  .As(amount => new Money(amount, Currency.EUR))
                  .OrNull()
                  .Generate();
    }

}
