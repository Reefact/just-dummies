namespace NetworkAcl.Domain;

public enum IpVersion
{
    V4 = 4,
    V6 = 6,
}

public enum RuleAction
{
    Allow,
    Deny,
}

/// <summary>The eight TCP control bits, at their position in the header's flags byte.</summary>
[Flags]
public enum TcpFlags : byte
{
    None = 0,
    Fin = 0x01,
    Syn = 0x02,
    Rst = 0x04,
    Psh = 0x08,
    Ack = 0x10,
    Urg = 0x20,
    Ece = 0x40,
    Cwr = 0x80,
}

/// <summary>Why a change to the policy was refused.</summary>
public enum PolicyRefusalReason
{
    DuplicateRuleId,
    RuleNotFound,
    PositionOutOfRange,
    CapacityReached,
    RuleWouldBeUnreachable,
    RuleWouldShadowAnother,
}

/// <summary>How a rule is useless in its policy.</summary>
public enum AnomalyKind
{
    /// <summary>An earlier rule covers it with the opposite action: it never matches, and its intent is lost.</summary>
    Shadowed,

    /// <summary>An earlier rule covers it with the same action: it never matches.</summary>
    RedundantToEarlier,

    /// <summary>A later rule with the same action covers it and nothing in between decides otherwise.</summary>
    RedundantToLater,

    /// <summary>It does what the default action would do, and no later rule decides otherwise.</summary>
    RedundantToDefault,
}
