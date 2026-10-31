using System.Text.RegularExpressions;

namespace CarbonLedger.Domain;

/// <summary>
/// The aggregate: an append-only journal of balanced entries for one registry, plus the states derived from it
/// (who holds each unit, which units are retired, account balances).
/// </summary>
public sealed partial class CarbonRegistryLedger
{
    private readonly Dictionary<string, Account> _accounts = new(StringComparer.Ordinal);
    private readonly List<JournalEntry> _journal = [];
    private readonly Dictionary<CreditSerial, Account> _holders = [];
    private readonly HashSet<CreditSerial> _retired = [];
    private readonly Dictionary<string, int> _balances = new(StringComparer.Ordinal);

    public CarbonRegistryLedger(string registry, Account issuanceAccount)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(issuanceAccount);
        if (!RegistryPattern().IsMatch(registry))
        {
            throw new ArgumentException("A registry code is 2 to 5 upper-case ASCII letters.", nameof(registry));
        }

        if (issuanceAccount.Kind != AccountKind.Issuance)
        {
            throw new ArgumentException("A ledger is founded on its registry's issuance account.", nameof(issuanceAccount));
        }

        Registry = registry;
        IssuanceAccount = issuanceAccount;
        AddAccount(issuanceAccount);
    }

    public string Registry { get; }

    public Account IssuanceAccount { get; }

    public IReadOnlyList<JournalEntry> Journal => _journal;

    public IReadOnlyCollection<Account> Accounts => _accounts.Values;

    /// <summary>The number of units issued so far, retired ones included.</summary>
    public int IssuedQuantity => -Balance(IssuanceAccount.AccountId);

    /// <summary>The number of units retired so far.</summary>
    public int RetiredQuantity => _retired.Count;

    public void OpenAccount(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        if (account.Kind == AccountKind.Issuance)
        {
            throw new LedgerRuleViolationException($"Registry {Registry} already has its issuance account.");
        }

        if (_accounts.ContainsKey(account.AccountId))
        {
            throw new LedgerRuleViolationException($"Account {account.AccountId} is already open.");
        }

        AddAccount(account);
    }

    /// <summary>Issues new units into a holding account. A serial can be issued only once.</summary>
    public JournalEntry Issue(IReadOnlyCollection<CreditSerial> serials, string toAccountId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(serials);
        foreach (CreditSerial serial in serials)
        {
            if (serial is not null && serial.Registry != Registry)
            {
                throw new LedgerRuleViolationException($"Unit {serial} belongs to registry {serial.Registry}, not {Registry}.");
            }

            if (serial is not null && IsKnown(serial))
            {
                throw new LedgerRuleViolationException($"Unit {serial} has already been issued.");
            }
        }

        return Post(EntryKind.Issuance, IssuanceAccount, Find(toAccountId), serials, null, at);
    }

    /// <summary>Moves units between two holding accounts. Every unit must be active and held by the sender.</summary>
    public JournalEntry Transfer(IReadOnlyCollection<CreditSerial> serials, string fromAccountId, string toAccountId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(serials);
        Account from = Find(fromAccountId);
        EnsureHeldBy(serials, from);
        return Post(EntryKind.Transfer, from, Find(toAccountId), serials, null, at);
    }

    /// <summary>Retires units for good: they can never be transferred or retired again.</summary>
    public JournalEntry Retire(
        IReadOnlyCollection<CreditSerial> serials,
        string fromAccountId,
        string retirementAccountId,
        RetirementClaim claim,
        DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(serials);
        ArgumentNullException.ThrowIfNull(claim);
        Account from = Find(fromAccountId);
        EnsureHeldBy(serials, from);
        return Post(EntryKind.Retirement, from, Find(retirementAccountId), serials, claim, at);
    }

    /// <summary>Units held by an account; the issuance account's balance is minus the units issued.</summary>
    public int Balance(string accountId)
    {
        Find(accountId);
        return _balances[accountId];
    }

    public Account HolderOf(CreditSerial serial)
    {
        ArgumentNullException.ThrowIfNull(serial);
        return _holders.TryGetValue(serial, out Account? account)
            ? account
            : throw new LedgerRuleViolationException($"Unit {serial} has never been issued.");
    }

    public bool IsRetired(CreditSerial serial)
    {
        ArgumentNullException.ThrowIfNull(serial);
        return _retired.Contains(serial);
    }

    public IReadOnlyList<CreditSerial> UnitsHeldBy(string accountId)
    {
        Account account = Find(accountId);
        return _holders.Where(pair => pair.Value.AccountId == account.AccountId).Select(pair => pair.Key).ToArray();
    }

    /// <summary>The ownership history of one unit, rebuilt from the journal, oldest first.</summary>
    public IReadOnlyList<HoldingPeriod> HistoryOf(CreditSerial serial)
    {
        ArgumentNullException.ThrowIfNull(serial);
        List<HoldingPeriod> periods = [];
        string? holder = null;
        DateTimeOffset since = default;
        foreach (JournalEntry entry in _journal.Where(e => e.Serials.Contains(serial)))
        {
            if (holder is not null)
            {
                periods.Add(new HoldingPeriod(serial, holder, since, entry.RecordedAt));
            }

            holder = entry.Credit.AccountId;
            since = entry.RecordedAt;
        }

        if (holder is not null)
        {
            periods.Add(new HoldingPeriod(serial, holder, since, null));
        }

        return periods;
    }

    /// <summary>
    /// Checks the derived state against the journal: balances sum to zero (double-entry conservation), each
    /// account's balance equals the units it holds, and no unit is held by more than one account.
    /// </summary>
    public bool IsConsistent()
    {
        if (_balances.Values.Sum() != 0)
        {
            return false;
        }

        Dictionary<CreditSerial, string> replayed = [];
        foreach (JournalEntry entry in _journal)
        {
            foreach (CreditSerial serial in entry.Serials)
            {
                replayed[serial] = entry.Credit.AccountId;
            }
        }

        if (replayed.Count != _holders.Count
            || replayed.Any(pair => _holders[pair.Key].AccountId != pair.Value))
        {
            return false;
        }

        return _accounts.Values
            .Where(a => a.Kind != AccountKind.Issuance)
            .All(a => _balances[a.AccountId] == replayed.Values.Count(id => id == a.AccountId));
    }

    private JournalEntry Post(
        EntryKind kind,
        Account debit,
        Account credit,
        IReadOnlyCollection<CreditSerial> serials,
        RetirementClaim? claim,
        DateTimeOffset at)
    {
        if (_journal.Count > 0 && at < _journal[^1].RecordedAt)
        {
            throw new LedgerRuleViolationException(
                $"The journal is append-only: {at:O} is before the last entry at {_journal[^1].RecordedAt:O}.");
        }

        if (at < debit.OpenedOn || at < credit.OpenedOn)
        {
            throw new LedgerRuleViolationException("An entry cannot be recorded before both its accounts were opened.");
        }

        JournalEntry entry = new(_journal.Count + 1, at, kind, debit, credit, serials, claim);
        _journal.Add(entry);
        foreach (CreditSerial serial in entry.Serials)
        {
            _holders[serial] = credit;
            if (kind == EntryKind.Retirement)
            {
                _retired.Add(serial);
            }
        }

        _balances[debit.AccountId] -= entry.Quantity;
        _balances[credit.AccountId] += entry.Quantity;
        return entry;
    }

    private void EnsureHeldBy(IReadOnlyCollection<CreditSerial> serials, Account from)
    {
        foreach (CreditSerial serial in serials)
        {
            if (serial is null)
            {
                continue;
            }

            if (_retired.Contains(serial))
            {
                throw new LedgerRuleViolationException($"Unit {serial} is retired and can no longer be used.");
            }

            if (!_holders.TryGetValue(serial, out Account? holder))
            {
                throw new LedgerRuleViolationException($"Unit {serial} has never been issued.");
            }

            if (holder.AccountId != from.AccountId)
            {
                throw new LedgerRuleViolationException($"Unit {serial} is held by {holder.AccountId}, not {from.AccountId}.");
            }
        }
    }

    private bool IsKnown(CreditSerial serial) => _holders.ContainsKey(serial);

    private Account Find(string accountId)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        return _accounts.TryGetValue(accountId, out Account? account)
            ? account
            : throw new LedgerRuleViolationException($"Account {accountId} is not open in registry {Registry}.");
    }

    private void AddAccount(Account account)
    {
        _accounts.Add(account.AccountId, account);
        _balances.Add(account.AccountId, 0);
    }

    [GeneratedRegex("^[A-Z]{2,5}$")]
    private static partial Regex RegistryPattern();
}
