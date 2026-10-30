using System.Text.RegularExpressions;

namespace Trading.Domain;

/// <summary>An exchange symbol: one to five upper-case letters, optionally followed by a share-class suffix ("BRK.B").</summary>
public sealed class Ticker {

    private static readonly Regex Format = new(@"^[A-Z]{1,5}(\.[A-Z])?$", RegexOptions.Compiled);

    public Ticker(string symbol) {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (!Format.IsMatch(symbol)) {
            throw new ArgumentException($"'{symbol}' is not a valid ticker symbol.", nameof(symbol));
        }

        Symbol = symbol;
    }

    public string Symbol { get; }

    public override bool Equals(object? obj) {
        return obj is Ticker other && other.Symbol == Symbol;
    }

    public override int GetHashCode() {
        return Symbol.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString() {
        return Symbol;
    }

}
