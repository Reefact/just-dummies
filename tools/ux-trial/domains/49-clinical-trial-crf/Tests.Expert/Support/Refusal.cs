using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

internal static class Refusal {

    /// <summary>Asserts the action is refused by the casebook for the given reason, and hands the refusal back.</summary>
    public static CasebookRefusedException Of(CasebookRefusal expected, Action action) {
        CasebookRefusedException refusal = Assert.Throws<CasebookRefusedException>(action);

        Assert.Equal(expected, refusal.Reason);

        return refusal;
    }

}
