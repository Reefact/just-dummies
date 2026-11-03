using System.Globalization;

namespace GridLoadFlow.Domain.Tests.Expert;

/// <summary>Sets the culture of the current thread for the length of a <c>using</c>, then puts the previous one back.</summary>
internal sealed class CultureScope : IDisposable {

    private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

    internal CultureScope(CultureInfo culture) {
        CultureInfo.CurrentCulture = culture;
    }

    public void Dispose() {
        CultureInfo.CurrentCulture = _previous;
    }

}
