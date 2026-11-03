using JustDummies;

namespace CaptiveBreeding.Domain;

public sealed partial class AnyStudbookId {

    /// <summary>Narrows the institution prefix to the given letter class, e.g. <c>"A-M"</c>.</summary>
    public AnyStudbookId WithInstitutionLetters(string letterClass) {
        return WithValue(_ => Any.StringMatching("[" + letterClass + "]{2,4}-[0-9]{4,6}"));
    }

}
