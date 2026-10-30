using System.Text.RegularExpressions;

namespace Payments.Domain;

/// <summary>Unstructured remittance information: up to 140 characters of the SEPA basic Latin set.</summary>
public sealed class RemittanceInformation {

    public const int MaximumLength = 140;

    private static readonly Regex SepaCharacterSet = new(@"^[A-Za-z0-9/\-?:().,'+ ]+$");

    public RemittanceInformation(string text) {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, MaximumLength);
        if (!SepaCharacterSet.IsMatch(text)) {
            throw new ArgumentException("Remittance information is restricted to the SEPA character set.", nameof(text));
        }
        Text = text;
    }

    public string Text { get; }

    public override string ToString() => Text;

}
