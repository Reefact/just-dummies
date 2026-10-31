namespace BatchGenealogy.Domain;

/// <summary>Quality disposition of a lot.</summary>
public enum LotStatus
{
    /// <summary>Awaiting quality release, or blocked by an investigation.</summary>
    Quarantined,

    /// <summary>Released by quality assurance: may be consumed.</summary>
    Released,

    /// <summary>Non-conforming: may never be consumed nor released again.</summary>
    Rejected,
}
