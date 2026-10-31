namespace CarbonLedger.Domain;

/// <summary>The role an account plays in the registry.</summary>
public enum AccountKind
{
    /// <summary>The registry's own source account; its balance is minus the number of units issued.</summary>
    Issuance,

    /// <summary>An ordinary account holding transferable units.</summary>
    Holding,

    /// <summary>A terminal account: units credited to it are retired for good.</summary>
    Retirement,
}

/// <summary>The nature of a journal entry.</summary>
public enum EntryKind
{
    Issuance,
    Transfer,
    Retirement,
}
