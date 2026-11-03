using System.Numerics;
using System.Text;

using JustDummies;

using Xunit;

namespace TokenLedger.Domain.Tests;

/// <summary>
///     A ledger and its cast, built the way every ledger test starts: the token, the addresses and the genesis block are
///     dummies; the standard gas schedule and the gas price a test pays are on purpose, since fees are asserted.
/// </summary>
internal sealed class Chain {

    /// <summary>The gas price the transactions under test pay, in base units: the fees asserted are gas x this.</summary>
    public const ulong Price = 10;

    /// <summary>The supply cap, in base units. A whole number of tokens at any of the 0 to 12 decimals drawn.</summary>
    public const long Cap = 1_000_000_000_000;

    /// <summary>
    ///     What the minter holds from genesis when the ledger asks for a minimum gas price: funding an account is a mint,
    ///     and a mint has a fee the minter must be able to pay.
    /// </summary>
    public const long MinterReserve = 10_000_000_000;

    /// <summary>What a funded account holds: more than any maximum fee (at most 100 000 gas x <see cref="Price" />) the tests ask of it.</summary>
    public const long Funds = 10_000_000;

    private readonly Address[] _cast;

    private Chain(Ledger ledger, Address minter, Address collector, Address alice, Address bob, Address carol) {
        Ledger    = ledger;
        Minter    = minter;
        Collector = collector;
        Alice     = alice;
        Bob       = bob;
        Carol     = carol;
        _cast     = [minter, collector, alice, bob, carol];
    }

    public Ledger  Ledger    { get; }
    public Address Minter    { get; }
    public Address Collector { get; }
    public Address Alice     { get; }
    public Address Bob       { get; }
    public Address Carol     { get; }

    /// <summary>
    ///     A ledger with no supply yet, the standard gas schedule and a token capped at <see cref="Cap" /> base units. The five
    ///     addresses are distinct by construction. At a minimum gas price of zero (the default), funding an account costs nothing
    ///     and the minter starts with nothing; above zero, the minter starts with <see cref="MinterReserve" /> to pay the fees of its mints.
    /// </summary>
    public static Chain Start(ulong minimumGasPrice = 0, int? decimals = null) {
        Address[] cast = Any.SetOf(Any.Address().WithValue(v => v.DifferentFrom(Address.Zero.Value))).WithCount(5).Generate().ToArray();

        AnyLedger ledger = Any.Ledger()
                              .WithToken(t => t.WithSupplyCap(Amount.Of(Cap))
                                               .WithDecimals(d => decimals is { } fixedDecimals ? d.Between(fixedDecimals, fixedDecimals) : d.Between(0, 12)))
                              .WithMinter(cast[0])
                              .WithFeeCollector(cast[1])
                              .WithInitialSupply(Amount.Of(minimumGasPrice == 0 ? 0 : MinterReserve))
                              .WithGasSchedule(GasSchedule.Standard)
                              .WithMinimumGasPrice(Amount.Of(minimumGasPrice))
                              .WithGenesisBlock(g => g.Between(0, 1_000_000));

        return new Chain(ledger.Generate(), cast[0], cast[1], cast[2], cast[3], cast[4]);
    }

    /// <summary>A chain whose fees are collected by the minter itself.</summary>
    public static Chain StartWithTheMinterAsCollector() {
        Address[] cast = Any.SetOf(Any.Address().WithValue(v => v.DifferentFrom(Address.Zero.Value))).WithCount(4).Generate().ToArray();

        Ledger ledger = Any.Ledger()
                           .WithToken(t => t.WithSupplyCap(Amount.Of(Cap)).WithDecimals(d => d.Between(0, 12)))
                           .WithMinter(cast[0])
                           .WithFeeCollector(cast[0])
                           .WithInitialSupply(TokenAmount.Zero)
                           .WithGasSchedule(GasSchedule.Standard)
                           .WithMinimumGasPrice(TokenAmount.Zero)
                           .WithGenesisBlock(g => g.Between(0, 1_000_000))
                           .Generate();

        return new Chain(ledger, cast[0], cast[0], cast[1], cast[2], cast[3]);
    }

    /// <summary>
    ///     An envelope for the next transaction of <paramref name="sender" />, valid on this ledger as it stands: the next nonce, a
    ///     gas limit that covers any operation, a deadline not yet passed, and <see cref="Price" /> per gas. A test narrows it with
    ///     the <c>With…</c> of the fact it is about.
    /// </summary>
    public AnyTransactionEnvelope Envelope(Address sender) {
        return Any.TransactionEnvelope()
                  .WithSender(sender)
                  .WithNonce(Ledger.NextNonceOf(sender))
                  .WithGasLimit(g => g.Between(65_000, 100_000))
                  .WithGasPrice(Amount.Of(Price))
                  .WithDeadlineBlock(d => d.GreaterThanOrEqualTo(Ledger.CurrentBlock));
    }

    /// <summary>Gives <paramref name="who" /> exactly <paramref name="amount" />, by a mint of the minter at the minimum gas price.</summary>
    public Chain Fund(Address who, BigInteger amount) {
        if (amount.IsZero) { return this; }

        TransactionEnvelope envelope = Envelope(Minter).WithGasPrice(Ledger.MinimumGasPrice).WithGasLimit(65_000).Generate();

        Ledger.Mint(envelope, who, Amount.Of(amount));

        return this;
    }

    public Chain FundAlice(BigInteger amount = default) {
        return Fund(Alice, amount.IsZero ? Funds : amount);
    }

    public Chain FundBob(BigInteger amount = default) {
        return Fund(Bob, amount.IsZero ? Funds : amount);
    }

    public Chain FundCarol(BigInteger amount = default) {
        return Fund(Carol, amount.IsZero ? Funds : amount);
    }

    public BigInteger BalanceOf(Address who) {
        return Ledger.BalanceOf(who).BaseUnits;
    }

    public BigInteger AllowanceOf(Address owner, Address spender) {
        return Ledger.AllowanceOf(owner, spender).BaseUnits;
    }

    public Receipt LastReceipt => Ledger.Receipts[^1];

    /// <summary>How many transfer records the ledger holds now; pass it to <see cref="TransfersSince" /> after the transaction under test.</summary>
    public int Mark() {
        return Ledger.Transfers.Count;
    }

    public IReadOnlyList<TransferRecord> TransfersSince(int mark) {
        return Ledger.Transfers.Skip(mark).ToList();
    }

    /// <summary>Everything a refused transaction must leave alone, as one comparable text.</summary>
    public string Fingerprint() {
        StringBuilder text = new();

        text.Append(Ledger.CurrentBlock).Append('|').Append(Ledger.TotalSupply).Append('|').Append(Ledger.Receipts.Count).Append('|').Append(Ledger.Transfers.Count).Append('|').Append(Ledger.Accounts.Count);

        foreach (Address owner in _cast) {
            text.Append('|').Append(Ledger.BalanceOf(owner)).Append('/').Append(Ledger.NextNonceOf(owner));

            foreach (Address spender in _cast) {
                text.Append(':').Append(Ledger.AllowanceOf(owner, spender));
            }
        }

        return text.ToString();
    }

    /// <summary>
    ///     Runs <paramref name="transaction" />, which must be refused for <paramref name="expected" />, and checks that the ledger is
    ///     exactly as it was: balances, nonces (the sender's included), allowances, supply, block, receipts and transfers.
    /// </summary>
    public void Refuses(RefusalReason expected, Action transaction) {
        string before = Fingerprint();

        LedgerRefusedException refusal = Assert.Throws<LedgerRefusedException>(transaction);

        Assert.Equal(expected, refusal.Reason);
        Assert.Equal(before, Fingerprint());
    }

}
