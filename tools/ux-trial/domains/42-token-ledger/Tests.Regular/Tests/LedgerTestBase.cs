using System.Text;

namespace TokenLedger.Domain.Tests.Regular;

/// <summary>Shared arrange/assert helpers for the ledger tests. Amounts stay literals in the tests; addresses, tokens and schedules are dummies.</summary>
public abstract class LedgerTestBase {

    protected static TokenAmount Units(long baseUnits) => new TokenAmount(baseUnits);

    /// <summary>A token whose cap is a number of base units (no decimals), for the tests where the cap matters.</summary>
    protected static Func<AnyTokenDefinition, AnyTokenDefinition> TokenCappedAt(long baseUnits) {
        return t => t.WithDecimals(0).WithSupplyCap(Units(baseUnits));
    }

    /// <summary>
    ///     A ledger where nothing costs anything unless a test says so: the cap is far above any literal amount, the minimum
    ///     gas price is zero, and the schedule is whatever was drawn.
    /// </summary>
    protected static AnyLedger FeelessLedger(Address minter, Address feeCollector, long initialSupply) {
        return Any.Ledger()
                  .WithToken(t => t.WithWholeTokens(w => w.Between(1_000_000_000_000, 1_000_000_000_000_000)))
                  .WithMinter(minter)
                  .WithFeeCollector(feeCollector)
                  .WithInitialSupply(Units(initialSupply))
                  .WithMinimumGasPrice(TokenAmount.Zero);
    }

    /// <summary>A ledger on the standard schedule, charging at least <paramref name="minimumGasPrice" /> per gas.</summary>
    protected static AnyLedger StandardLedger(Address minter, Address feeCollector, long initialSupply, long minimumGasPrice = 0) {
        return FeelessLedger(minter, feeCollector, initialSupply)
               .WithGasSchedule(GasSchedule.Standard)
               .WithMinimumGasPrice(Units(minimumGasPrice));
    }

    /// <summary>The next valid envelope of <paramref name="sender" />: its own next nonce, plenty of gas, no deadline, a free gas price.</summary>
    protected static TransactionEnvelope Tx(Ledger ledger, Address sender, long gasPrice = 0, ulong gasLimit = GasSchedule.BlockGasLimit, ulong deadlineBlock = ulong.MaxValue) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(ledger.NextNonceOf(sender))
                  .WithGasLimit(gasLimit)
                  .WithGasPrice(Units(gasPrice))
                  .WithDeadlineBlock(deadlineBlock)
                  .Generate();
    }

    protected static TransactionEnvelope TxWithNonce(Address sender, UInt128 nonce, long gasPrice = 0, ulong gasLimit = GasSchedule.BlockGasLimit, ulong deadlineBlock = ulong.MaxValue) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(nonce)
                  .WithGasLimit(gasLimit)
                  .WithGasPrice(Units(gasPrice))
                  .WithDeadlineBlock(deadlineBlock)
                  .Generate();
    }

    /// <summary>Everything observable about the ledger and the given addresses, as text, to prove a refusal changed nothing.</summary>
    protected static string Snapshot(Ledger ledger, params Address[] addresses) {
        StringBuilder text = new StringBuilder();

        text.Append($"block={ledger.CurrentBlock};supply={ledger.TotalSupply};receipts={ledger.Receipts.Count};transfers={ledger.Transfers.Count};accounts={ledger.Accounts.Count};");
        foreach (Address address in addresses) {
            text.Append($"{address}:balance={ledger.BalanceOf(address)},nonce={ledger.NextNonceOf(address)};");
        }

        foreach (Account account in ledger.Accounts.OrderBy(a => a.Address.Value, StringComparer.Ordinal)) {
            text.Append($"{account};");
        }

        return text.ToString();
    }

    protected static LedgerRefusedException AssertRefused(RefusalReason reason, Action transaction) {
        LedgerRefusedException refusal = Assert.Throws<LedgerRefusedException>(transaction);

        Assert.Equal(reason, refusal.Reason);

        return refusal;
    }

    /// <summary>Runs <paramref name="transaction" />, expects it to be refused for <paramref name="reason" />, and proves the ledger did not move.</summary>
    protected static void AssertRefusedAndNothingChanged(Ledger ledger, RefusalReason reason, Action transaction, params Address[] watched) {
        string before = Snapshot(ledger, watched);

        AssertRefused(reason, transaction);

        Assert.Equal(before, Snapshot(ledger, watched));
    }

    protected static TokenAmount SumOfBalances(Ledger ledger) {
        TokenAmount sum = TokenAmount.Zero;
        foreach (Account account in ledger.Accounts) {
            sum = sum.Add(account.Balance);
        }

        return sum;
    }

}
