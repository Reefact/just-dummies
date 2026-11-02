namespace FxSettlement.Domain;

/// <summary>What the bank does with the base currency of the pair.</summary>
public enum TradeSide
{
    BuyBase,
    SellBase,
}

/// <summary>The direction of a settlement instruction, seen from the bank.</summary>
public enum SettlementDirection
{
    Receive,
    Pay,
}

public enum BatchStatus
{
    Open,
    CutOff,
}

/// <summary>Why the settlement batch refused an operation.</summary>
public enum SettlementRefusalReason
{
    BatchCutOff,
    ValueDateMismatch,
    DuplicateTrade,
    NoReferenceRate,
    RateOffMarket,
    UnknownTrade,
    NotCutOff,
    AlreadyCutOff,
}
