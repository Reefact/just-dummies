namespace LiquidityGap.Domain;

/// <summary>Whether a cash flow brings liquidity in or takes it out.</summary>
public enum FlowDirection
{
    Inflow,
    Outflow,
}

/// <summary>How a payment date falling on a weekend is moved to a business day.</summary>
public enum BusinessDayConvention
{
    Unadjusted,
    Following,
    ModifiedFollowing,
    Preceding,
}
