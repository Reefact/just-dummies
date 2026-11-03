using System.Numerics;

using JustDummies;

using TokenLedger.Domain;

namespace TokenLedger.Tests.Discovery.Support;

/// <summary>Builds the ledgers and the signed envelopes the ledger tests act on.</summary>
internal static class Chain {

    /// <summary>The gas price the envelopes pay unless a test says otherwise.</summary>
    public const ulong GasPrice = 2;

    /// <summary>Enough gas for any operation of the standard schedule, a new-holder surcharge included.</summary>
    public const ulong AmpleGasFrom = 65_000;

    public const ulong AmpleGasTo = 100_000;

    public static TokenAmount Units(BigInteger baseUnits) {
        return new TokenAmount(baseUnits);
    }

    /// <summary>A ledger under the standard schedule, whose minter holds <paramref name="initialSupply" /> and whose cap is 10^12 base units.</summary>
    public static Ledger NewLedger(ulong initialSupply = 10_000_000_000, ulong minimumGasPrice = 1) {
        return NewLedger(GasSchedule.Standard, initialSupply, minimumGasPrice);
    }

    public static Ledger NewLedger(GasSchedule gasSchedule, ulong initialSupply = 10_000_000_000, ulong minimumGasPrice = 1) {
        return Any.Ledger()
                  .WithToken(token => token.WithSupplyCap(Units(1_000_000_000_000)).WithDecimals(decimals => decimals.Between(0, 12)))
                  .WithInitialSupply(Units(initialSupply))
                  .WithGasSchedule(gasSchedule)
                  .WithMinimumGasPrice(Units(minimumGasPrice))
                  .WithGenesisBlock(block => block.LessThanOrEqualTo(1_000_000))
                  .Generate();
    }

    /// <summary>An address other than the zero address, drawn for a test that needs a participant and does not care which.</summary>
    public static Address NewAddress() {
        return Any.Address().NonZero().Generate();
    }

    /// <summary>Several addresses that are pairwise different.</summary>
    public static List<Address> NewAddresses(int count) {
        return Any.ListOf(Any.Address().NonZero()).WithCount(count).Distinct().Generate();
    }

    /// <summary>The next envelope of <paramref name="sender" />: its nonce is the ledger's, its deadline is still ahead, and it carries ample gas (a maximum fee of at most 200 000 at the default price).</summary>
    public static TransactionEnvelope Next(Ledger ledger, Address sender, ulong gasPrice = GasPrice) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(ledger.NextNonceOf(sender))
                  .WithGasPrice(Units(gasPrice))
                  .WithGasLimit(limit => limit.Between(AmpleGasFrom, AmpleGasTo))
                  .WithDeadlineBlock(deadline => deadline.GreaterThanOrEqualTo(ledger.CurrentBlock))
                  .Generate();
    }

    /// <summary>An envelope with the gas limit the test is about.</summary>
    public static TransactionEnvelope NextWithGasLimit(Ledger ledger, Address sender, ulong gasLimit, ulong gasPrice = GasPrice) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(ledger.NextNonceOf(sender))
                  .WithGasPrice(Units(gasPrice))
                  .WithGasLimit(gasLimit)
                  .WithDeadlineBlock(deadline => deadline.GreaterThanOrEqualTo(ledger.CurrentBlock))
                  .Generate();
    }

    /// <summary>An envelope with the nonce the test is about.</summary>
    public static TransactionEnvelope WithNonce(Ledger ledger, Address sender, UInt128 nonce) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(nonce)
                  .WithGasPrice(Units(GasPrice))
                  .WithGasLimit(limit => limit.Between(AmpleGasFrom, AmpleGasTo))
                  .WithDeadlineBlock(deadline => deadline.GreaterThanOrEqualTo(ledger.CurrentBlock))
                  .Generate();
    }

    /// <summary>An envelope with the deadline the test is about.</summary>
    public static TransactionEnvelope WithDeadline(Ledger ledger, Address sender, ulong deadlineBlock) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(ledger.NextNonceOf(sender))
                  .WithGasPrice(Units(GasPrice))
                  .WithGasLimit(limit => limit.Between(AmpleGasFrom, AmpleGasTo))
                  .WithDeadlineBlock(deadlineBlock)
                  .Generate();
    }

    /// <summary>Gives <paramref name="to" /> the amount from the minter, at a price that leaves the fee out of the picture for the caller to count.</summary>
    public static void Fund(Ledger ledger, Address to, ulong amount) {
        ledger.Transfer(Next(ledger, ledger.Minter), to, Units(amount));
    }

    /// <summary>The sum of every account's balance.</summary>
    public static BigInteger SumOfBalances(Ledger ledger) {
        BigInteger sum = BigInteger.Zero;
        foreach (Account account in ledger.Accounts) { sum += account.Balance.BaseUnits; }

        return sum;
    }

    /// <summary>
    ///     Runs a transaction the ledger must refuse, and checks the refusal changed nothing: not a balance, not a nonce, not
    ///     the logs, not the supply. Returns the refusal so a test can say which one it expected.
    /// </summary>
    public static LedgerRefusedException Refused(Ledger ledger, Action transaction, params Address[] watched) {
        List<Address>      addresses = watched.Concat([ledger.Minter, ledger.FeeCollector]).Distinct().ToList();
        List<TokenAmount>  balances  = addresses.Select(ledger.BalanceOf).ToList();
        List<UInt128>      nonces    = addresses.Select(ledger.NextNonceOf).ToList();
        int                receipts  = ledger.Receipts.Count;
        int                transfers = ledger.Transfers.Count;
        TokenAmount        supply    = ledger.TotalSupply;
        ulong              block     = ledger.CurrentBlock;

        LedgerRefusedException refusal = Xunit.Assert.Throws<LedgerRefusedException>(transaction);

        Xunit.Assert.Equal(balances, addresses.Select(ledger.BalanceOf).ToList());
        Xunit.Assert.Equal(nonces, addresses.Select(ledger.NextNonceOf).ToList());
        Xunit.Assert.Equal(receipts, ledger.Receipts.Count);
        Xunit.Assert.Equal(transfers, ledger.Transfers.Count);
        Xunit.Assert.Equal(supply, ledger.TotalSupply);
        Xunit.Assert.Equal(block, ledger.CurrentBlock);

        return refusal;
    }

}
