using System.Globalization;

namespace Trading.Domain.Tests.Expert;

/// <summary>Attributes cannot hold a <see cref="decimal" />: a theory writes it as text, and this reads it back exactly, trailing zeros included.</summary>
public static class Literal {

    public static decimal Decimal(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

}
