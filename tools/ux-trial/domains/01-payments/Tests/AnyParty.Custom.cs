namespace Payments.Domain;

public sealed partial class AnyParty {

    // Party requires the agent's country to be the account's: two parameters, one invariant. A per-parameter
    // base cannot relate them, so both default to the same country, and InCountry moves both together.
    private const string DefaultCountry = "DE";

    public AnyParty InCountry(string countryCode) {
        return WithAccount(a => a.InCountry(countryCode)).WithAgent(b => b.InCountry(countryCode));
    }

}
