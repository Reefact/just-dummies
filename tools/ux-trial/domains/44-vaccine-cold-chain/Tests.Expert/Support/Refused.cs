using ColdChain.Domain;
using Xunit;

namespace ColdChain.Tests.Expert.Support;

/// <summary>Asserts that the consignment refused an operation, and for which reason.</summary>
internal static class Refused {

    public static void Because(ColdChainRefusal expected, Action operation) {
        ColdChainRefusedException refusal = Assert.Throws<ColdChainRefusedException>(operation);

        Assert.Equal(expected, refusal.Refusal);
    }

}

/// <summary>
///     Asserts that a constructor refused its arguments for the reason a test is about: when several rules raise the
///     same exception type, the type alone would let a test pass for the wrong rule.
/// </summary>
internal static class Rejected {

    public static void For(string messagePart, Action operation) {
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(operation);

        Assert.Contains(messagePart, refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

}
