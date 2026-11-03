using System.Globalization;

namespace ColdChain.Tests.Expert.Support;

/// <summary>Attribute arguments cannot be decimals: theories spell a temperature as text and read it here.</summary>
internal static class Celsius {

    public static decimal Of(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

}
