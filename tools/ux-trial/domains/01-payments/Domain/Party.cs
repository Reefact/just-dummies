namespace Payments.Domain;

/// <summary>A debtor or a creditor: a name and an account, the agent's BIC being optional inside SEPA.</summary>
public sealed class Party {

    public Party(string name, Iban account, Bic? agent) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(account);
        name = name.Trim();
        if (name.Length is 0 or > 70) {
            throw new ArgumentException("A party name is 1 to 70 characters once trimmed.", nameof(name));
        }
        if (agent is not null && agent.CountryCode != account.CountryCode) {
            throw new ArgumentException($"The agent {agent} is not in the account's country {account.CountryCode}.", nameof(agent));
        }
        Name    = name;
        Account = account;
        Agent   = agent;
    }

    public string Name { get; }

    public Iban Account { get; }

    public Bic? Agent { get; }

}
