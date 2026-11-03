using CarbonLedger.Domain;
using CarbonLedger.Tests.Expert.Generators;

using JustDummies;

namespace CarbonLedger.Tests.Expert.Support;

internal static class TestAccounts {

    /// <summary>Two accounts of the given kinds, which do not share a number.</summary>
    public static (Account Debit, Account Credit) Pair(AccountKind debitKind, AccountKind creditKind) {
        Account debit  = Any.Account().WithKind(debitKind).Generate();
        Account credit = Any.Account().WithKind(creditKind).WithAccountNumber(number => number.DifferentFrom(debit.AccountId[3..])).Generate();

        return (debit, credit);
    }

}
