namespace Payments.Domain;

// Written by hand, beside the scaffolded AnyRemittanceInformation.cs.
public sealed partial class AnyRemittanceInformation {

    // The SEPA basic Latin set of the domain's own character guard, spelled out because the guard is a regex.
    private const string SepaCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789/-?:().,'+ ";

}
