namespace TokenLedger.Domain;

public enum TransactionKind
{
    Transfer,
    Approve,
    IncreaseAllowance,
    DecreaseAllowance,
    TransferFrom,
    Mint,
    Burn,
}

/// <summary>Why the ledger refused a transaction. A refused transaction changes nothing, its nonce included.</summary>
public enum RefusalReason
{
    NonceTooLow = 1,
    NonceTooHigh,
    NonceExhausted,
    DeadlinePassed,
    GasPriceTooLow,
    ZeroAddress,
    NotMinter,
    GasLimitTooLow,
    CapExceeded,
    AllowanceExceeded,
    AllowanceOverflow,
    InsufficientBalance,
    InsufficientFundsForFee,
}
