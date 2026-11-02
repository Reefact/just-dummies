namespace TokenLedger.Domain;

/// <summary>
/// The ledger of one token, kept off-chain in the manner of an ERC-20 contract: balances,
/// allowances, minting under the supply cap, burning, and a fee of gas used × gas price on
/// every accepted transaction, paid in the token itself to the fee collector.
/// <para>
/// Every transaction is checked completely before anything changes: a refused transaction
/// throws <see cref="LedgerRefusedException"/> and leaves the ledger as it was, the sender's
/// nonce included. The sum of all balances always equals <see cref="TotalSupply"/>.
/// </para>
/// </summary>
public sealed class Ledger
{
    private readonly Dictionary<Address, Account> accounts = [];
    private readonly Dictionary<(Address Owner, Address Spender), TokenAmount> allowances = [];
    private readonly List<Receipt> receipts = [];
    private readonly List<TransferRecord> transfers = [];

    public Ledger(
        TokenDefinition token,
        Address minter,
        Address feeCollector,
        TokenAmount initialSupply,
        GasSchedule gasSchedule,
        TokenAmount minimumGasPrice,
        ulong genesisBlock)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(minter);
        ArgumentNullException.ThrowIfNull(feeCollector);
        ArgumentNullException.ThrowIfNull(initialSupply);
        ArgumentNullException.ThrowIfNull(gasSchedule);
        ArgumentNullException.ThrowIfNull(minimumGasPrice);

        if (minter.IsZero)
        {
            throw new ArgumentException("The minter cannot be the zero address.", nameof(minter));
        }

        if (feeCollector.IsZero)
        {
            throw new ArgumentException("Fees cannot be collected by the zero address: that would burn them.", nameof(feeCollector));
        }

        if (initialSupply.CompareTo(token.SupplyCap) > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialSupply), initialSupply, $"The initial supply cannot exceed the cap of {token.SupplyCap} base units.");
        }

        if (minimumGasPrice.BaseUnits * gasSchedule.Transfer > token.SupplyCap.BaseUnits)
        {
            throw new ArgumentException(
                "At the minimum gas price a single transfer would cost more than the whole supply cap: no holder could ever pay it.",
                nameof(minimumGasPrice));
        }

        Token = token;
        Minter = minter;
        FeeCollector = feeCollector;
        GasSchedule = gasSchedule;
        MinimumGasPrice = minimumGasPrice;
        CurrentBlock = genesisBlock;
        TotalSupply = TokenAmount.Zero;

        AccountFor(minter);
        AccountFor(feeCollector);
        if (!initialSupply.IsZero)
        {
            AccountFor(minter).Credit(initialSupply);
            TotalSupply = initialSupply;
            transfers.Add(new TransferRecord(Address.Zero, minter, initialSupply, genesisBlock));
        }
    }

    public TokenDefinition Token { get; }

    /// <summary>The only account allowed to mint.</summary>
    public Address Minter { get; }

    public Address FeeCollector { get; }

    public GasSchedule GasSchedule { get; }

    public TokenAmount MinimumGasPrice { get; }

    public ulong CurrentBlock { get; private set; }

    public TokenAmount TotalSupply { get; private set; }

    /// <summary>How much can still be minted: supply cap − total supply.</summary>
    public TokenAmount RemainingMintable => Token.SupplyCap.Subtract(TotalSupply);

    public IReadOnlyList<Receipt> Receipts => receipts;

    public IReadOnlyList<TransferRecord> Transfers => transfers;

    public IReadOnlyCollection<Account> Accounts => accounts.Values;

    public TokenAmount BalanceOf(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return accounts.TryGetValue(address, out Account? account) ? account.Balance : TokenAmount.Zero;
    }

    /// <summary>The nonce the next transaction from <paramref name="address"/> must carry (0 for an unseen account).</summary>
    public UInt128 NextNonceOf(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return accounts.TryGetValue(address, out Account? account) ? account.NextNonce : UInt128.Zero;
    }

    public TokenAmount AllowanceOf(Address owner, Address spender)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(spender);
        return allowances.TryGetValue((owner, spender), out TokenAmount? allowance) ? allowance : TokenAmount.Zero;
    }

    public ExactDecimal HumanBalanceOf(Address address) => Token.ToHuman(BalanceOf(address));

    public ExactDecimal ValueOf(Address address, TokenPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        return price.ValueOf(BalanceOf(address), Token);
    }

    public ExactDecimal MarketCapitalisation(TokenPrice price)
    {
        ArgumentNullException.ThrowIfNull(price);
        return price.ValueOf(TotalSupply, Token);
    }

    /// <summary>Seals the current block and moves on by <paramref name="count"/> blocks.</summary>
    public void AdvanceBlocks(ulong count)
    {
        if (count == 0 || count > ulong.MaxValue - CurrentBlock)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"The chain advances by at least one block, without passing block {ulong.MaxValue}.");
        }

        CurrentBlock += count;
    }

    public Receipt Transfer(TransactionEnvelope envelope, Address to, TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(amount);

        CheckEnvelope(envelope);
        RequireNonZero(to, "recipient");
        ulong gasUsed = CheckGas(envelope, TransactionKind.Transfer, to, amount);
        RequireBalance(envelope.Sender, amount);
        RequireFeeFunds(envelope, amount);

        Move(envelope.Sender, to, amount);
        return Commit(envelope, TransactionKind.Transfer, gasUsed);
    }

    /// <summary>Sets the allowance of <paramref name="spender"/> over the sender's balance, replacing any previous one.</summary>
    public Receipt Approve(TransactionEnvelope envelope, Address spender, TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(spender);
        ArgumentNullException.ThrowIfNull(amount);

        CheckEnvelope(envelope);
        RequireNonZero(spender, "spender");
        ulong gasUsed = CheckGas(envelope, TransactionKind.Approve, null, amount);
        RequireFeeFunds(envelope, TokenAmount.Zero);

        SetAllowance(envelope.Sender, spender, amount);
        return Commit(envelope, TransactionKind.Approve, gasUsed);
    }

    public Receipt IncreaseAllowance(TransactionEnvelope envelope, Address spender, TokenAmount addedValue)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(spender);
        ArgumentNullException.ThrowIfNull(addedValue);

        CheckEnvelope(envelope);
        RequireNonZero(spender, "spender");
        ulong gasUsed = CheckGas(envelope, TransactionKind.IncreaseAllowance, null, addedValue);
        TokenAmount current = AllowanceOf(envelope.Sender, spender);
        if (!current.CanAdd(addedValue))
        {
            throw new LedgerRefusedException(
                RefusalReason.AllowanceOverflow,
                $"Allowance {current} + {addedValue} of {spender} would pass 2^256 - 1 base units.");
        }

        RequireFeeFunds(envelope, TokenAmount.Zero);

        SetAllowance(envelope.Sender, spender, current.Add(addedValue));
        return Commit(envelope, TransactionKind.IncreaseAllowance, gasUsed);
    }

    public Receipt DecreaseAllowance(TransactionEnvelope envelope, Address spender, TokenAmount subtractedValue)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(spender);
        ArgumentNullException.ThrowIfNull(subtractedValue);

        CheckEnvelope(envelope);
        RequireNonZero(spender, "spender");
        ulong gasUsed = CheckGas(envelope, TransactionKind.DecreaseAllowance, null, subtractedValue);
        TokenAmount current = AllowanceOf(envelope.Sender, spender);
        if (subtractedValue.CompareTo(current) > 0)
        {
            throw new LedgerRefusedException(
                RefusalReason.AllowanceExceeded,
                $"Cannot decrease the allowance of {spender} by {subtractedValue}: it is {current}.");
        }

        RequireFeeFunds(envelope, TokenAmount.Zero);

        SetAllowance(envelope.Sender, spender, current.Subtract(subtractedValue));
        return Commit(envelope, TransactionKind.DecreaseAllowance, gasUsed);
    }

    /// <summary>
    /// Moves <paramref name="amount"/> from <paramref name="from"/> to <paramref name="to"/> on the
    /// sender's allowance, which decreases by the amount unless it is unlimited (2^256 − 1).
    /// The sender pays the fee.
    /// </summary>
    public Receipt TransferFrom(TransactionEnvelope envelope, Address from, Address to, TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(amount);

        CheckEnvelope(envelope);
        RequireNonZero(from, "owner");
        RequireNonZero(to, "recipient");
        ulong gasUsed = CheckGas(envelope, TransactionKind.TransferFrom, to, amount);
        TokenAmount allowance = AllowanceOf(from, envelope.Sender);
        if (amount.CompareTo(allowance) > 0)
        {
            throw new LedgerRefusedException(
                RefusalReason.AllowanceExceeded,
                $"{envelope.Sender} may spend {allowance} of {from}'s balance, not {amount}.");
        }

        RequireBalance(from, amount);
        RequireFeeFunds(envelope, from.Equals(envelope.Sender) ? amount : TokenAmount.Zero);

        Move(from, to, amount);
        if (!allowance.Equals(TokenAmount.Max))
        {
            SetAllowance(from, envelope.Sender, allowance.Subtract(amount));
        }

        return Commit(envelope, TransactionKind.TransferFrom, gasUsed);
    }

    /// <summary>Creates <paramref name="amount"/> for <paramref name="to"/>; only the minter may, and never past the supply cap.</summary>
    public Receipt Mint(TransactionEnvelope envelope, Address to, TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(amount);

        CheckEnvelope(envelope);
        RequireNonZero(to, "recipient");
        if (!envelope.Sender.Equals(Minter))
        {
            throw new LedgerRefusedException(RefusalReason.NotMinter, $"{envelope.Sender} is not the minter of {Token.Symbol}.");
        }

        ulong gasUsed = CheckGas(envelope, TransactionKind.Mint, to, amount);
        if (amount.CompareTo(RemainingMintable) > 0)
        {
            throw new LedgerRefusedException(
                RefusalReason.CapExceeded,
                $"Minting {Token.Format(amount)} would pass the cap: only {Token.Format(RemainingMintable)} remain.");
        }

        RequireFeeFunds(envelope, TokenAmount.Zero);

        AccountFor(to).Credit(amount);
        TotalSupply = TotalSupply.Add(amount);
        transfers.Add(new TransferRecord(Address.Zero, to, amount, CurrentBlock));
        return Commit(envelope, TransactionKind.Mint, gasUsed);
    }

    /// <summary>Destroys <paramref name="amount"/> of the sender's own balance.</summary>
    public Receipt Burn(TransactionEnvelope envelope, TokenAmount amount)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(amount);

        CheckEnvelope(envelope);
        ulong gasUsed = CheckGas(envelope, TransactionKind.Burn, null, amount);
        RequireBalance(envelope.Sender, amount);
        RequireFeeFunds(envelope, amount);

        AccountFor(envelope.Sender).Debit(amount);
        TotalSupply = TotalSupply.Subtract(amount);
        transfers.Add(new TransferRecord(envelope.Sender, Address.Zero, amount, CurrentBlock));
        return Commit(envelope, TransactionKind.Burn, gasUsed);
    }

    private void CheckEnvelope(TransactionEnvelope envelope)
    {
        UInt128 expected = NextNonceOf(envelope.Sender);
        if (envelope.Nonce < expected)
        {
            throw new LedgerRefusedException(RefusalReason.NonceTooLow, $"Nonce {envelope.Nonce} of {envelope.Sender} is already used; next is {expected}.");
        }

        if (envelope.Nonce > expected)
        {
            throw new LedgerRefusedException(RefusalReason.NonceTooHigh, $"Nonce {envelope.Nonce} of {envelope.Sender} skips ahead; next is {expected}.");
        }

        if (expected == UInt128.MaxValue)
        {
            throw new LedgerRefusedException(RefusalReason.NonceExhausted, $"{envelope.Sender} has no nonce left.");
        }

        if (CurrentBlock > envelope.DeadlineBlock)
        {
            throw new LedgerRefusedException(RefusalReason.DeadlinePassed, $"The transaction was valid until block {envelope.DeadlineBlock}; the ledger is at {CurrentBlock}.");
        }

        if (envelope.GasPrice.CompareTo(MinimumGasPrice) < 0)
        {
            throw new LedgerRefusedException(RefusalReason.GasPriceTooLow, $"Gas price {envelope.GasPrice} is below the minimum of {MinimumGasPrice}.");
        }
    }

    private ulong CheckGas(TransactionEnvelope envelope, TransactionKind kind, Address? credited, TokenAmount amount)
    {
        bool createsHolder = credited is not null && !amount.IsZero && BalanceOf(credited).IsZero;
        ulong gas = GasSchedule.GasFor(kind, createsHolder);
        if (gas > envelope.GasLimit)
        {
            throw new LedgerRefusedException(RefusalReason.GasLimitTooLow, $"{kind} needs {gas} gas; the limit is {envelope.GasLimit}.");
        }

        return gas;
    }

    private static void RequireNonZero(Address address, string role)
    {
        if (address.IsZero)
        {
            throw new LedgerRefusedException(RefusalReason.ZeroAddress, $"The {role} cannot be the zero address.");
        }
    }

    private void RequireBalance(Address owner, TokenAmount amount)
    {
        TokenAmount balance = BalanceOf(owner);
        if (amount.CompareTo(balance) > 0)
        {
            throw new LedgerRefusedException(RefusalReason.InsufficientBalance, $"{owner} holds {balance}, not {amount}.");
        }
    }

    /// <summary>
    /// The sender must be able to pay the maximum fee (gas limit × gas price) out of what remains
    /// of its balance once <paramref name="debitedFromSender"/> has left it; credits do not count.
    /// </summary>
    private void RequireFeeFunds(TransactionEnvelope envelope, TokenAmount debitedFromSender)
    {
        TokenAmount remaining = BalanceOf(envelope.Sender).Subtract(debitedFromSender);
        TokenAmount maxFee = envelope.MaxFee;
        if (maxFee.CompareTo(remaining) > 0)
        {
            throw new LedgerRefusedException(
                RefusalReason.InsufficientFundsForFee,
                $"{envelope.Sender} keeps {remaining} after the operation; the maximum fee is {maxFee}.");
        }
    }

    private void Move(Address from, Address to, TokenAmount amount)
    {
        AccountFor(from).Debit(amount);
        AccountFor(to).Credit(amount);
        transfers.Add(new TransferRecord(from, to, amount, CurrentBlock));
    }

    private void SetAllowance(Address owner, Address spender, TokenAmount amount)
    {
        if (amount.IsZero)
        {
            allowances.Remove((owner, spender));
        }
        else
        {
            allowances[(owner, spender)] = amount;
        }
    }

    private Receipt Commit(TransactionEnvelope envelope, TransactionKind kind, ulong gasUsed)
    {
        Account sender = AccountFor(envelope.Sender);
        Receipt receipt = new(kind, envelope.Sender, envelope.Nonce, CurrentBlock, gasUsed, envelope.GasPrice);
        if (!receipt.Fee.IsZero)
        {
            sender.Debit(receipt.Fee);
            AccountFor(FeeCollector).Credit(receipt.Fee);
            transfers.Add(new TransferRecord(envelope.Sender, FeeCollector, receipt.Fee, CurrentBlock));
        }

        sender.ConsumeNonce();
        receipts.Add(receipt);
        return receipt;
    }

    private Account AccountFor(Address address)
    {
        if (!accounts.TryGetValue(address, out Account? account))
        {
            account = new Account(address);
            accounts.Add(address, account);
        }

        return account;
    }
}
